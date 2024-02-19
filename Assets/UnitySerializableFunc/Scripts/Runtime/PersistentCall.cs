// -----------------------------------------------------------------------
// <copyright file="PersistentCall.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.SerializableFunc
{
    using System;
    using System.Reflection;
    using UnityEngine;
    using Object = UnityEngine.Object;
    using PersistentListenerMode = UnityEngine.Events.PersistentListenerMode;
    using UnityEventCallState = UnityEngine.Events.UnityEventCallState;

    [Serializable]
    internal class PersistentCall
    {
        // keep the layout of this class in sync with MonoPersistentCall in PersistentCallCollection.cpp
        [SerializeField]
        private Object m_Target;

        [SerializeField]
        private string m_MethodName;

        [SerializeField]
        private PersistentListenerMode m_Mode = PersistentListenerMode.EventDefined;

        [SerializeField]
        private ArgumentCache m_Arguments = new ArgumentCache();

        [SerializeField]
        private UnityEventCallState m_CallState = UnityEventCallState.RuntimeOnly;

        public Object target
        {
            get { return this.m_Target; }
        }

        public string methodName
        {
            get { return this.m_MethodName; }
        }

        public PersistentListenerMode mode
        {
            get { return this.m_Mode; }
            set { this.m_Mode = value; }
        }

        public ArgumentCache arguments
        {
            get { return this.m_Arguments; }
        }

        public UnityEventCallState callState
        {
            get { return this.m_CallState; }
            set { this.m_CallState = value; }
        }

        public bool IsValid()
        {
            // We need to use the same logic found in PersistentCallCollection.cpp, IsPersistentCallValid
            return this.target != null && !string.IsNullOrEmpty(this.methodName);
        }

        public BaseInvokableCall GetRuntimeCall(UnityFuncBase theEvent)
        {
            if (this.m_CallState == UnityEventCallState.RuntimeOnly && !Application.isPlaying)
            {
                return null;
            }

            if (this.m_CallState == UnityEventCallState.Off || theEvent == null)
            {
                return null;
            }

            var method = theEvent.FindMethod(this);
            if (method == null)
            {
                return null;
            }

            switch (this.m_Mode)
            {
                case PersistentListenerMode.EventDefined:
                    return theEvent.GetDelegate(this.target, method);
                case PersistentListenerMode.Object:
                    return GetObjectCall(this.target, method, this.m_Arguments);
                case PersistentListenerMode.Float:
                    return GetOneParameterCall(this.target, method, typeof(float), this.m_Arguments.floatArgument);
                case PersistentListenerMode.Int:
                    return GetOneParameterCall(this.target, method, typeof(int), this.m_Arguments.intArgument);
                case PersistentListenerMode.String:
                    return GetOneParameterCall(this.target, method, typeof(string), this.m_Arguments.stringArgument);
                case PersistentListenerMode.Bool:
                    return GetOneParameterCall(this.target, method, typeof(bool), this.m_Arguments.boolArgument);
                case PersistentListenerMode.Void:
                    return GetParameterlessCall(this.target, method);
            }

            return null;
        }

        public void RegisterPersistentListener(Object ttarget, string mmethodName)
        {
            this.m_Target = ttarget;
            this.m_MethodName = mmethodName;
        }

        public void UnregisterPersistentListener()
        {
            this.m_MethodName = string.Empty;
            this.m_Target = null;
        }

        private static BaseInvokableCall GetParameterlessCall(Object target, MethodInfo method)
        {
            var tp = typeof(InvokableCall<>).MakeGenericType(method.ReturnType);
            var ctor = tp.GetConstructor(new[] { typeof(object), typeof(MethodInfo) });
            var ins = ctor.Invoke(new object[] { target, method });
            return ins as BaseInvokableCall;
        }

        private static BaseInvokableCall GetOneParameterCall(Object target, MethodInfo method, Type firstParamType, object firstParamValue)
        {
            var tp = typeof(CachedInvokableCall<,>).MakeGenericType(firstParamType, method.ReturnType);
            var ctor = tp.GetConstructor(new[] { typeof(object), typeof(MethodInfo), firstParamType });
            var ins = ctor.Invoke(new object[] { target, method, firstParamValue });
            return ins as BaseInvokableCall;
        }

        // need to generate a generic typed version of the call here
        // this is due to the fact that we allow binding of 'any'
        // functions that extend object.
        private static BaseInvokableCall GetObjectCall(Object target, MethodInfo method, ArgumentCache arguments)
        {
            var type = typeof(Object);
            if (!string.IsNullOrEmpty(arguments.unityObjectArgumentAssemblyTypeName))
            {
                type = Type.GetType(arguments.unityObjectArgumentAssemblyTypeName, false) ?? typeof(Object);
            }

            var generic = typeof(CachedInvokableCall<,>);
            var specific = generic.MakeGenericType(type, method.ReturnType);
            var ci = specific.GetConstructor(new[] { typeof(Object), typeof(MethodInfo), type });

            var castedObject = arguments.unityObjectArgument;
            if (castedObject != null && !type.IsAssignableFrom(castedObject.GetType()))
            {
                castedObject = null;
            }

            // need to pass explicit null here!
            return ci.Invoke(new object[] { target, method, castedObject }) as BaseInvokableCall;
        }
    }
}
