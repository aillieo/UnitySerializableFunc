// -----------------------------------------------------------------------
// <copyright file="UnityFunc`5.cs" company="AillieoTech">
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
    public class UnityFunc<T0, T1, T2, T3, TResult> : UnityFuncBase
    {
        private object[] invokeArray = null;

        public UnityFunc()
        {
        }

        public void AddListener(System.Func<T0, T1, T2, T3, TResult> call)
        {
            this.AddCall(GetDelegate(call));
        }

        public void RemoveListener(System.Func<T0, T1, T2, T3, TResult> call)
        {
            this.RemoveListener(call.Target, call.Method);
        }

        public TResult Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            List<BaseInvokableCall> calls = this.PrepareInvoke();
            TResult result = default;
            for (var i = 0; i < calls.Count; i++)
            {
                if (calls[i] is InvokableCall<T0, T1, T2, T3, TResult> curCall)
                {
                    result = curCall.Invoke(arg0, arg1, arg2, arg3);
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
                            this.invokeArray = new object[4];
                        }

                        this.invokeArray[0] = arg0;
                        this.invokeArray[1] = arg1;
                        this.invokeArray[2] = arg2;
                        this.invokeArray[3] = arg3;
                        result = (TResult)cachedCurCall.Invoke(this.invokeArray);
                    }
                }
            }

            return result;
        }

        internal override BaseInvokableCall GetDelegate(object target, MethodInfo theFunction)
        {
            return new InvokableCall<T0, T1, T2, T3, TResult>(target, theFunction);
        }

        internal void AddPersistentListener(System.Func<T0, T1, T2, T3, TResult> call)
        {
            this.AddPersistentListener(call, UnityEventCallState.RuntimeOnly);
        }

        internal void AddPersistentListener(System.Func<T0, T1, T2, T3, TResult> call, UnityEventCallState callState)
        {
            var count = this.GetPersistentEventCount();
            this.AddPersistentListener();
            this.RegisterPersistentListener(count, call);
            this.SetPersistentListenerState(count, callState);
        }

        internal void RegisterPersistentListener(int index, System.Func<T0, T1, T2, T3, TResult> call)
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
            return GetValidMethodInfo(targetObj, name, new Type[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3) });
        }

        private static BaseInvokableCall GetDelegate(System.Func<T0, T1, T2, T3, TResult> action)
        {
            return new InvokableCall<T0, T1, T2, T3, TResult>(action);
        }
    }
}
