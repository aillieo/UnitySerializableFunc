// -----------------------------------------------------------------------
// <copyright file="CachedInvokableCall`2.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.SerializableFunc
{
    using System;
    using System.Reflection;

    internal class CachedInvokableCall<T, TResult> : InvokableCall<T, TResult>
    {
        private readonly T arg1;

        public CachedInvokableCall(object target, MethodInfo theFunction, T argument)
            : base(target, theFunction)
        {
            this.arg1 = argument;
        }

        public override object Invoke(object[] args)
        {
            return base.Invoke(this.arg1);
        }

        public override TResult Invoke(T arg0)
        {
            return base.Invoke(this.arg1);
        }
    }
}
