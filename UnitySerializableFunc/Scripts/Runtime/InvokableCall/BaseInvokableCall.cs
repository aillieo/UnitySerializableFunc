// -----------------------------------------------------------------------
// <copyright file="BaseInvokableCall.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.SerializableFunc
{
    using System;
    using System.Reflection;
    using Object = UnityEngine.Object;

    internal abstract class BaseInvokableCall
    {
        protected BaseInvokableCall()
        {
        }

        protected BaseInvokableCall(object target, MethodInfo function)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            if (function == null)
            {
                throw new ArgumentNullException("function");
            }
        }

        public abstract bool Find(object targetObj, MethodInfo method);

        public abstract object Invoke(object[] args);

        protected static void ThrowOnInvalidArg<T>(object arg)
        {
            if (arg != null && !(arg is T))
            {
                throw new ArgumentException($"Passed argument 'args[0]' is of the wrong type. Type:{arg.GetType()} Expected:{typeof(T)}");
            }
        }

        protected static bool AllowInvoke(Delegate @delegate)
        {
            var target = @delegate.Target;

            // static
            if (target == null)
            {
                return true;
            }

            // UnityEngine object
            var unityObj = target as Object;
            if (!ReferenceEquals(unityObj, null))
            {
                return unityObj != null;
            }

            // Normal object
            return true;
        }
    }
}
