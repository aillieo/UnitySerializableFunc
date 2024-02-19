// -----------------------------------------------------------------------
// <copyright file="PersistentCallGroup.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.SerializableFunc
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using Object = UnityEngine.Object;
    using PersistentListenerMode = UnityEngine.Events.PersistentListenerMode;

    [Serializable]
    internal class PersistentCallGroup
    {
        [SerializeField]
        private List<PersistentCall> calls;

        public PersistentCallGroup()
        {
            this.calls = new List<PersistentCall>();
        }

        public int Count
        {
            get { return this.calls.Count; }
        }

        public PersistentCall GetListener(int index)
        {
            return this.calls[index];
        }

        public IEnumerable<PersistentCall> GetListeners()
        {
            return this.calls;
        }

        public void AddListener()
        {
            this.calls.Add(new PersistentCall());
        }

        public void AddListener(PersistentCall call)
        {
            this.calls.Add(call);
        }

        public void RemoveListener(int index)
        {
            this.calls.RemoveAt(index);
        }

        public void Clear()
        {
            this.calls.Clear();
        }

        public void RegisterEventPersistentListener(int index, Object targetObj, string methodName)
        {
            var listener = this.GetListener(index);
            listener.RegisterPersistentListener(targetObj, methodName);
            listener.mode = PersistentListenerMode.EventDefined;
        }

        public void RegisterVoidPersistentListener(int index, Object targetObj, string methodName)
        {
            var listener = this.GetListener(index);
            listener.RegisterPersistentListener(targetObj, methodName);
            listener.mode = PersistentListenerMode.Void;
        }

        public void RegisterObjectPersistentListener(int index, Object targetObj, Object argument, string methodName)
        {
            var listener = this.GetListener(index);
            listener.RegisterPersistentListener(targetObj, methodName);
            listener.mode = PersistentListenerMode.Object;
            listener.arguments.unityObjectArgument = argument;
        }

        public void RegisterIntPersistentListener(int index, Object targetObj, int argument, string methodName)
        {
            var listener = this.GetListener(index);
            listener.RegisterPersistentListener(targetObj, methodName);
            listener.mode = PersistentListenerMode.Int;
            listener.arguments.intArgument = argument;
        }

        public void RegisterFloatPersistentListener(int index, Object targetObj, float argument, string methodName)
        {
            var listener = this.GetListener(index);
            listener.RegisterPersistentListener(targetObj, methodName);
            listener.mode = PersistentListenerMode.Float;
            listener.arguments.floatArgument = argument;
        }

        public void RegisterStringPersistentListener(int index, Object targetObj, string argument, string methodName)
        {
            var listener = this.GetListener(index);
            listener.RegisterPersistentListener(targetObj, methodName);
            listener.mode = PersistentListenerMode.String;
            listener.arguments.stringArgument = argument;
        }

        public void RegisterBoolPersistentListener(int index, Object targetObj, bool argument, string methodName)
        {
            var listener = this.GetListener(index);
            listener.RegisterPersistentListener(targetObj, methodName);
            listener.mode = PersistentListenerMode.Bool;
            listener.arguments.boolArgument = argument;
        }

        public void UnregisterPersistentListener(int index)
        {
            var evt = this.GetListener(index);
            evt.UnregisterPersistentListener();
        }

        public void RemoveListeners(Object target, string methodName)
        {
            var toRemove = new List<PersistentCall>();
            for (var index = 0; index < this.calls.Count; index++)
            {
                if (this.calls[index].target == target && this.calls[index].methodName == methodName)
                {
                    toRemove.Add(this.calls[index]);
                }
            }

            this.calls.RemoveAll(toRemove.Contains);
        }

        public void Initialize(InvokableCallList invokableList, UnityFuncBase unityEventBase)
        {
            foreach (var persistentCall in this.calls)
            {
                if (!persistentCall.IsValid())
                {
                    continue;
                }

                var call = persistentCall.GetRuntimeCall(unityEventBase);
                if (call != null)
                {
                    invokableList.AddPersistentInvokableCall(call);
                }
            }
        }
    }
}
