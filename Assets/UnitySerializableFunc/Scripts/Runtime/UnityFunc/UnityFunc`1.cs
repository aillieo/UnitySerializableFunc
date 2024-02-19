// -----------------------------------------------------------------------
// <copyright file="UnityFunc`1.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.SerializableFunc
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEventCallState = UnityEngine.Events.UnityEventCallState;

    [Serializable]
    public class UnityFunc<TResult> : UnityFuncBase
    {
        private object[] invokeArray = null;

        public UnityFunc()
        {
        }

        public void AddListener(Func<TResult> call)
        {
            this.AddCall(GetDelegate(call));
        }

        public void RemoveListener(Func<TResult> call)
        {
            this.RemoveListener(call.Target, call.Method);
        }

        public TResult Invoke()
        {
            List<BaseInvokableCall> calls = this.PrepareInvoke();
            TResult result = default;

            for (var i = 0; i < calls.Count; i++)
            {
                if (calls[i] is InvokableCall<TResult> curCall)
                {
                    result = curCall.Invoke();
                }
                else
                {
                    if (calls[i] is InvokableCall<TResult> staticCurCall)
                    {
                        result = staticCurCall.Invoke();
                    }
                    else
                    {
                        var cachedCurCall = calls[i];
                        if (this.invokeArray == null)
                        {
                            this.invokeArray = Array.Empty<object>();
                        }

                        result = (TResult)cachedCurCall.Invoke(this.invokeArray);
                    }
                }
            }

            return result;
        }

        internal override BaseInvokableCall GetDelegate(object target, MethodInfo theFunction)
        {
            return new InvokableCall<TResult>(target, theFunction);
        }

        internal void AddPersistentListener(Func<TResult> call)
        {
            this.AddPersistentListener(call, UnityEventCallState.RuntimeOnly);
        }

        internal void AddPersistentListener(Func<TResult> call, UnityEventCallState callState)
        {
            var count = this.GetPersistentEventCount();
            this.AddPersistentListener();
            this.RegisterPersistentListener(count, call);
            this.SetPersistentListenerState(count, callState);
        }

        internal void RegisterPersistentListener(int index, Func<TResult> call)
        {
            if (call == null)
            {
                UnityEngine.Debug.LogWarning("Registering a Listener requires an action");
                return;
            }

            this.RegisterPersistentListener(index, call.Target as UnityEngine.Object, call.Method);
        }

        protected override MethodInfo FindMethod_Impl(string name, object targetObj)
        {
            return GetValidMethodInfo(targetObj, name, Array.Empty<Type>());
        }

        private static BaseInvokableCall GetDelegate(Func<TResult> action)
        {
            return new InvokableCall<TResult>(action);
        }
    }
}
