// -----------------------------------------------------------------------
// <copyright file="InvokableCall`4.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.SerializableFunc
{
    using System;
    using System.Reflection;

    internal class InvokableCall<T1, T2, T3, TResult> : BaseInvokableCall
    {
        public InvokableCall(object target, MethodInfo theFunction)
            : base(target, theFunction)
        {
            this.Delegate = (System.Func<T1, T2, T3, TResult>)System.Delegate.CreateDelegate(typeof(System.Func<T1, T2, T3, TResult>), target, theFunction);
        }

        public InvokableCall(System.Func<T1, T2, T3, TResult> action)
        {
            this.Delegate += action;
        }

        protected event System.Func<T1, T2, T3, TResult> Delegate;

        public override object Invoke(object[] args)
        {
            if (args.Length != 3)
            {
                throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 1");
            }

            ThrowOnInvalidArg<T1>(args[0]);
            ThrowOnInvalidArg<T2>(args[1]);
            ThrowOnInvalidArg<T3>(args[2]);

            if (AllowInvoke(this.Delegate))
            {
                return this.Delegate((T1)args[0], (T2)args[1], (T3)args[2]);
            }

            return default;
        }

        public TResult Invoke(T1 args0, T2 args1, T3 args2)
        {
            if (AllowInvoke(this.Delegate))
            {
                return this.Delegate(args0, args1, args2);
            }

            return default;
        }

        public override bool Find(object targetObj, MethodInfo method)
        {
            return this.Delegate.Target == targetObj && this.Delegate.Method.Equals(method);
        }
    }
}
