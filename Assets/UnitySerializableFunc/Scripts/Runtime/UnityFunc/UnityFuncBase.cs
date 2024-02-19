// -----------------------------------------------------------------------
// <copyright file="UnityFuncBase.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.SerializableFunc
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;
    using Object = UnityEngine.Object;
    using PersistentListenerMode = UnityEngine.Events.PersistentListenerMode;
    using UnityEventCallState = UnityEngine.Events.UnityEventCallState;

    [Serializable]
    public abstract class UnityFuncBase : ISerializationCallbackReceiver
    {
        private InvokableCallList calls;

        [SerializeField]
        private PersistentCallGroup persistentCalls;

        // Dirtying can happen outside of MainThread, but we need to rebuild on the MainThread.
        private bool callsDirty = true;

        protected UnityFuncBase()
        {
            this.calls = new InvokableCallList();
            this.persistentCalls = new PersistentCallGroup();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            this.DirtyPersistentCalls();
        }

        public int GetPersistentEventCount()
        {
            return this.persistentCalls.Count;
        }

        public Object GetPersistentTarget(int index)
        {
            var listener = this.persistentCalls.GetListener(index);
            return listener != null ? listener.target : null;
        }

        public string GetPersistentMethodName(int index)
        {
            var listener = this.persistentCalls.GetListener(index);
            return listener != null ? listener.methodName : string.Empty;
        }

        public void SetPersistentListenerState(int index, UnityEventCallState state)
        {
            var listener = this.persistentCalls.GetListener(index);
            if (listener != null)
            {
                listener.callState = state;
            }

            this.DirtyPersistentCalls();
        }

        public void RemoveAllListeners()
        {
            this.calls.Clear();
        }

        public override string ToString()
        {
            return base.ToString() + " " + this.GetType().FullName;
        }

        // Find a valid method that can be bound to an event with a given name
        internal static MethodInfo GetValidMethodInfo(object obj, string functionName, Type[] argumentTypes)
        {
            var type = obj.GetType();
            while (type != typeof(object) && type != null)
            {
                var method = type.GetMethod(functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, argumentTypes, null);
                if (method != null)
                {
                    // We need to make sure the Arguments are sane. When using the Type.DefaultBinder like we are above,
                    // it is possible to receive a method that takes a System.Object enve though we requested a float, int or bool.
                    // This can be an issue when the user changes the signature of a function that he had already set up via inspector.
                    // When changing a float parameter to a System.Object the getMethod would still bind to the cahnged version, but
                    // the PersistentListenerMode would still be kept as Float.
                    // TODO: Should we allow anything else besides Primitive types and types derived from UnityEngine.Object?
                    var parameterInfos = method.GetParameters();
                    var methodValid = true;
                    var i = 0;
                    foreach (ParameterInfo pi in parameterInfos)
                    {
                        var requestedType = argumentTypes[i];
                        var receivedType = pi.ParameterType;
                        methodValid = requestedType.IsPrimitive == receivedType.IsPrimitive;

                        if (!methodValid)
                        {
                            break;
                        }

                        i++;
                    }

                    if (methodValid)
                    {
                        return method;
                    }
                }

                type = type.BaseType;
            }

            return null;
        }

        internal abstract BaseInvokableCall GetDelegate(object target, MethodInfo theFunction);

        internal MethodInfo FindMethod(PersistentCall call)
        {
            var type = typeof(Object);
            if (!string.IsNullOrEmpty(call.arguments.unityObjectArgumentAssemblyTypeName))
            {
                type = Type.GetType(call.arguments.unityObjectArgumentAssemblyTypeName, false) ?? typeof(Object);
            }

            return this.FindMethod(call.methodName, call.target, call.mode, type);
        }

        internal MethodInfo FindMethod(string name, object listener, PersistentListenerMode mode, Type argumentType)
        {
            switch (mode)
            {
                case PersistentListenerMode.EventDefined:
                    return this.FindMethod_Impl(name, listener);
                case PersistentListenerMode.Void:
                    return GetValidMethodInfo(listener, name, Array.Empty<Type>());
                case PersistentListenerMode.Float:
                    return GetValidMethodInfo(listener, name, new[] { typeof(float) });
                case PersistentListenerMode.Int:
                    return GetValidMethodInfo(listener, name, new[] { typeof(int) });
                case PersistentListenerMode.Bool:
                    return GetValidMethodInfo(listener, name, new[] { typeof(bool) });
                case PersistentListenerMode.String:
                    return GetValidMethodInfo(listener, name, new[] { typeof(string) });
                case PersistentListenerMode.Object:
                    return GetValidMethodInfo(listener, name, new[] { argumentType ?? typeof(Object) });
                default:
                    return null;
            }
        }

        internal void AddCall(BaseInvokableCall call)
        {
            this.calls.AddListener(call);
        }

        internal List<BaseInvokableCall> PrepareInvoke()
        {
            this.RebuildPersistentCallsIfNeeded();
            return this.calls.PrepareInvoke();
        }

        internal void AddPersistentListener()
        {
            this.persistentCalls.AddListener();
        }

        internal void RemovePersistentListener(Object target, MethodInfo method)
        {
            if (method == null || method.IsStatic || target == null || target.GetInstanceID() == 0)
            {
                return;
            }

            this.persistentCalls.RemoveListeners(target, method.Name);
            this.DirtyPersistentCalls();
        }

        internal void RemovePersistentListener(int index)
        {
            this.persistentCalls.RemoveListener(index);
            this.DirtyPersistentCalls();
        }

        internal void UnregisterPersistentListener(int index)
        {
            this.persistentCalls.UnregisterPersistentListener(index);
            this.DirtyPersistentCalls();
        }

        internal void AddVoidPersistentListener<TResult>(Func<TResult> call)
        {
            var count = this.GetPersistentEventCount();
            this.AddPersistentListener();
            this.RegisterVoidPersistentListener(count, call);
        }

        internal void RegisterVoidPersistentListener<TResult>(int index, Func<TResult> call)
        {
            if (call == null)
            {
                Debug.LogWarning("Registering a Listener requires an action");
                return;
            }

            if (!this.ValidateRegistration(call.Method, call.Target, PersistentListenerMode.Void))
            {
                return;
            }

            this.persistentCalls.RegisterVoidPersistentListener(index, call.Target as Object, call.Method.Name);
            this.DirtyPersistentCalls();
        }

        internal void RegisterVoidPersistentListenerWithoutValidation(int index, Object target, string methodName)
        {
            this.persistentCalls.RegisterVoidPersistentListener(index, target, methodName);
            this.DirtyPersistentCalls();
        }

        internal void AddIntPersistentListener<TResult>(System.Func<int, TResult> call, int argument)
        {
            var count = this.GetPersistentEventCount();
            this.AddPersistentListener();
            this.RegisterIntPersistentListener(count, call, argument);
        }

        internal void RegisterIntPersistentListener<TResult>(int index, System.Func<int, TResult> call, int argument)
        {
            if (call == null)
            {
                Debug.LogWarning("Registering a Listener requires an action");
                return;
            }

            if (!this.ValidateRegistration(call.Method, call.Target, PersistentListenerMode.Int))
            {
                return;
            }

            this.persistentCalls.RegisterIntPersistentListener(index, call.Target as Object, argument, call.Method.Name);
            this.DirtyPersistentCalls();
        }

        internal void AddFloatPersistentListener<TResult>(System.Func<float, TResult> call, float argument)
        {
            var count = this.GetPersistentEventCount();
            this.AddPersistentListener();
            this.RegisterFloatPersistentListener(count, call, argument);
        }

        internal void RegisterFloatPersistentListener<TResult>(int index, System.Func<float, TResult> call, float argument)
        {
            if (call == null)
            {
                Debug.LogWarning("Registering a Listener requires an action");
                return;
            }

            if (!this.ValidateRegistration(call.Method, call.Target, PersistentListenerMode.Float))
            {
                return;
            }

            this.persistentCalls.RegisterFloatPersistentListener(index, call.Target as Object, argument, call.Method.Name);
            this.DirtyPersistentCalls();
        }

        internal void AddBoolPersistentListener<TResult>(System.Func<bool, TResult> call, bool argument)
        {
            var count = this.GetPersistentEventCount();
            this.AddPersistentListener();
            this.RegisterBoolPersistentListener(count, call, argument);
        }

        internal void RegisterBoolPersistentListener<TResult>(int index, System.Func<bool, TResult> call, bool argument)
        {
            if (call == null)
            {
                Debug.LogWarning("Registering a Listener requires an action");
                return;
            }

            if (!this.ValidateRegistration(call.Method, call.Target, PersistentListenerMode.Bool))
            {
                return;
            }

            this.persistentCalls.RegisterBoolPersistentListener(index, call.Target as Object, argument, call.Method.Name);
            this.DirtyPersistentCalls();
        }

        internal void AddStringPersistentListener<TResult>(System.Func<string, TResult> call, string argument)
        {
            var count = this.GetPersistentEventCount();
            this.AddPersistentListener();
            this.RegisterStringPersistentListener(count, call, argument);
        }

        internal void RegisterStringPersistentListener<TResult>(int index, System.Func<string, TResult> call, string argument)
        {
            if (call == null)
            {
                Debug.LogWarning("Registering a Listener requires an action");
                return;
            }

            if (!this.ValidateRegistration(call.Method, call.Target, PersistentListenerMode.String))
            {
                return;
            }

            this.persistentCalls.RegisterStringPersistentListener(index, call.Target as Object, argument, call.Method.Name);
            this.DirtyPersistentCalls();
        }

        internal void AddObjectPersistentListener<T, TResult>(System.Func<T, TResult> call, T argument)
            where T : Object
        {
            var count = this.GetPersistentEventCount();
            this.AddPersistentListener();
            this.RegisterObjectPersistentListener(count, call, argument);
        }

        internal void RegisterObjectPersistentListener<T, TResult>(int index, System.Func<T, TResult> call, T argument)
            where T : Object
        {
            if (call == null)
            {
                throw new ArgumentNullException("call", "Registering a Listener requires a non null call");
            }

            if (!this.ValidateRegistration(call.Method, call.Target, PersistentListenerMode.Object, argument == null ? typeof(Object) : argument.GetType()))
            {
                return;
            }

            this.persistentCalls.RegisterObjectPersistentListener(index, call.Target as Object, argument, call.Method.Name);
            this.DirtyPersistentCalls();
        }

        protected void Invoke(object[] parameters)
        {
            List<BaseInvokableCall> calls = this.PrepareInvoke();

            for (var i = 0; i < calls.Count; i++)
            {
                calls[i].Invoke(parameters);
            }
        }

        protected abstract MethodInfo FindMethod_Impl(string name, object targetObj);

        protected void AddListener(object targetObj, MethodInfo method)
        {
            this.calls.AddListener(this.GetDelegate(targetObj, method));
        }

        protected void RemoveListener(object targetObj, MethodInfo method)
        {
            this.calls.RemoveListener(targetObj, method);
        }

        protected bool ValidateRegistration(MethodInfo method, object targetObj, PersistentListenerMode mode)
        {
            return this.ValidateRegistration(method, targetObj, mode, typeof(Object));
        }

        protected bool ValidateRegistration(MethodInfo method, object targetObj, PersistentListenerMode mode, Type argumentType)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method", $"Can not register null method on {targetObj} for callback!");
            }

            var obj = targetObj as Object;
            if (obj == null || obj.GetInstanceID() == 0)
            {
                throw new ArgumentException(
                    $"Could not register callback {method.Name} on {targetObj}. The class {(targetObj == null ? "null" : targetObj.GetType().ToString())} does not derive from UnityEngine.Object");
            }

            if (method.IsStatic)
            {
                throw new ArgumentException($"Could not register listener {method} on {this.GetType()} static functions are not supported.");
            }

            if (this.FindMethod(method.Name, targetObj, mode, argumentType) == null)
            {
                Debug.LogWarning($"Could not register listener {targetObj}.{method} on {this.GetType()} the method could not be found.");
                return false;
            }

            return true;
        }

        protected void RegisterPersistentListener(int index, object targetObj, MethodInfo method)
        {
            if (!this.ValidateRegistration(method, targetObj, PersistentListenerMode.EventDefined))
            {
                return;
            }

            this.persistentCalls.RegisterEventPersistentListener(index, targetObj as Object, method.Name);
            this.DirtyPersistentCalls();
        }

        private void DirtyPersistentCalls()
        {
            this.calls.ClearPersistent();
            this.callsDirty = true;
        }

        // Can only run on MainThread
        private void RebuildPersistentCallsIfNeeded()
        {
            if (this.callsDirty)
            {
                this.persistentCalls.Initialize(this.calls, this);
                this.callsDirty = false;
            }
        }
    }
}
