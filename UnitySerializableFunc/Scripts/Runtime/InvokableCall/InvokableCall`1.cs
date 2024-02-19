// -----------------------------------------------------------------------
// <copyright file="InvokableCall`1.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.SerializableFunc
{
    using System.Reflection;

    internal class InvokableCall<TResult> : BaseInvokableCall
    {
        public InvokableCall(object target, MethodInfo theFunction)
            : base(target, theFunction)
        {
            this.Delegate += (System.Func<TResult>)System.Delegate.CreateDelegate(typeof(System.Func<TResult>), target, theFunction);
        }

        public InvokableCall(System.Func<TResult> action)
        {
            this.Delegate += action;
        }

        private event System.Func<TResult> Delegate;

        public override object Invoke(object[] args)
        {
            if (AllowInvoke(this.Delegate))
            {
                return this.Delegate();
            }

            return default;
        }

        public TResult Invoke()
        {
            if (AllowInvoke(this.Delegate))
            {
                return this.Delegate();
            }

            return default;
        }

        public override bool Find(object targetObj, MethodInfo method)
        {
            // Case 827748: You can't compare Delegate.GetMethodInfo() == method, because sometimes it will not work, that's why we're using Equals instead, because it will compare that actual method inside.
            //              Comment from Microsoft:
            //              Desktop behavior regarding identity has never really been guaranteed. The desktop aggressively caches and reuses MethodInfo objects so identity checks often work by accident.
            //              .Net Native doesn’t guarantee identity and caches a lot less
            return this.Delegate.Target == targetObj && this.Delegate.Method.Equals(method);
        }
    }
}
