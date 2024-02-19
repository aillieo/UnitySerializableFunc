// -----------------------------------------------------------------------
// <copyright file="InvokableCallList.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.SerializableFunc
{
    using System.Collections.Generic;
    using System.Reflection;

    internal class InvokableCallList
    {
        private readonly List<BaseInvokableCall> persistentCalls = new List<BaseInvokableCall>();
        private readonly List<BaseInvokableCall> runtimeCalls = new List<BaseInvokableCall>();

        private readonly List<BaseInvokableCall> executingCalls = new List<BaseInvokableCall>();

        private bool needsUpdate = true;

        public int Count
        {
            get { return this.persistentCalls.Count + this.runtimeCalls.Count; }
        }

        public void AddPersistentInvokableCall(BaseInvokableCall call)
        {
            this.persistentCalls.Add(call);
            this.needsUpdate = true;
        }

        public void AddListener(BaseInvokableCall call)
        {
            this.runtimeCalls.Add(call);
            this.needsUpdate = true;
        }

        public void RemoveListener(object targetObj, MethodInfo method)
        {
            var toRemove = new List<BaseInvokableCall>();
            for (var index = 0; index < this.runtimeCalls.Count; index++)
            {
                if (this.runtimeCalls[index].Find(targetObj, method))
                {
                    toRemove.Add(this.runtimeCalls[index]);
                }
            }

            this.runtimeCalls.RemoveAll(toRemove.Contains);
            this.needsUpdate = true;
        }

        public void Clear()
        {
            this.runtimeCalls.Clear();
            this.needsUpdate = true;
        }

        public void ClearPersistent()
        {
            this.persistentCalls.Clear();
            this.needsUpdate = true;
        }

        public List<BaseInvokableCall> PrepareInvoke()
        {
            if (this.needsUpdate)
            {
                this.executingCalls.Clear();
                this.executingCalls.AddRange(this.persistentCalls);
                this.executingCalls.AddRange(this.runtimeCalls);
                this.needsUpdate = false;
            }

            return this.executingCalls;
        }
    }
}
