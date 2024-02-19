// -----------------------------------------------------------------------
// <copyright file="InvokableCall`2.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.SerializableFunc
{
    using System;
    using System.Reflection;

    internal class InvokableCall<T1, TResult> : BaseInvokableCall
    {
        public InvokableCall(object target, MethodInfo theFunction)
            : base(target, theFunction)
        {
            this.Delegate += (System.Func<T1, TResult>)System.Delegate.CreateDelegate(typeof(System.Func<T1, TResult>), target, theFunction);
        }

        public InvokableCall(System.Func<T1, TResult> action)
        {
            this.Delegate += action;
        }

        protected event System.Func<T1, TResult> Delegate;

        public override object Invoke(object[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 1");
            }

            ThrowOnInvalidArg<T1>(args[0]);

            if (AllowInvoke(this.Delegate))
            {
                return this.Delegate((T1)args[0]);
            }

            return default;
        }

        public virtual TResult Invoke(T1 args0)
        {
            if (AllowInvoke(this.Delegate))
            {
                return this.Delegate(args0);
            }

            return default;
        }

        public override bool Find(object targetObj, MethodInfo method)
        {
            return this.Delegate.Target == targetObj && this.Delegate.Method.Equals(method);
        }
    }
}
