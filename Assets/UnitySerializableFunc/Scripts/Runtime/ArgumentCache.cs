// -----------------------------------------------------------------------
// <copyright file="ArgumentCache.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.SerializableFunc
{
    using System;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [Serializable]
    internal class ArgumentCache : ISerializationCallbackReceiver
    {
        [SerializeField]
        private Object m_ObjectArgument;
        [SerializeField]
        private string m_ObjectArgumentAssemblyTypeName;
        [SerializeField]
        private int m_IntArgument;
        [SerializeField]
        private float m_FloatArgument;
        [SerializeField]
        private string m_StringArgument;
        [SerializeField]
        private bool m_BoolArgument;

        public Object unityObjectArgument
        {
            get
            {
                return this.m_ObjectArgument;
            }

            set
            {
                this.m_ObjectArgument = value;
                this.m_ObjectArgumentAssemblyTypeName = value != null ? value.GetType().AssemblyQualifiedName : string.Empty;
            }
        }

        public string unityObjectArgumentAssemblyTypeName
        {
            get { return this.m_ObjectArgumentAssemblyTypeName; }
        }

        public int intArgument
        {
            get { return this.m_IntArgument; }
            set { this.m_IntArgument = value; }
        }

        public float floatArgument
        {
            get { return this.m_FloatArgument; }
            set { this.m_FloatArgument = value; }
        }

        public string stringArgument
        {
            get { return this.m_StringArgument; }
            set { this.m_StringArgument = value; }
        }

        public bool boolArgument
        {
            get { return this.m_BoolArgument; }
            set { this.m_BoolArgument = value; }
        }

        public void OnBeforeSerialize()
        {
            this.TidyAssemblyTypeName();
        }

        public void OnAfterDeserialize()
        {
            this.TidyAssemblyTypeName();
        }

        // Fix for assembly type name containing version / culture. We don't care about this for UI.
        // we need to fix this here, because there is old data in existing projects.
        // Typically, we're looking for .net Assembly Qualified Type Names and stripping everything after '<namespaces>.<typename>, <assemblyname>'
        // Example: System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' -> 'System.String, mscorlib'
        private void TidyAssemblyTypeName()
        {
            if (string.IsNullOrEmpty(this.m_ObjectArgumentAssemblyTypeName))
            {
                return;
            }

            var min = int.MaxValue;
            var i = this.m_ObjectArgumentAssemblyTypeName.IndexOf(", Version=", StringComparison.Ordinal);
            if (i != -1)
            {
                min = Math.Min(i, min);
            }

            i = this.m_ObjectArgumentAssemblyTypeName.IndexOf(", Culture=", StringComparison.Ordinal);
            if (i != -1)
            {
                min = Math.Min(i, min);
            }

            i = this.m_ObjectArgumentAssemblyTypeName.IndexOf(", PublicKeyToken=", StringComparison.Ordinal);
            if (i != -1)
            {
                min = Math.Min(i, min);
            }

            if (min != int.MaxValue)
            {
                this.m_ObjectArgumentAssemblyTypeName = this.m_ObjectArgumentAssemblyTypeName.Substring(0, min);
            }

            // Strip module assembly name.
            // The non-modular version will always work, due to type forwarders.
            // This way, when a type gets moved to a differnet module, previously serialized UnityEvents still work.
            i = this.m_ObjectArgumentAssemblyTypeName.IndexOf(", UnityEngine.", StringComparison.Ordinal);
            if (i != -1 && this.m_ObjectArgumentAssemblyTypeName.EndsWith("Module", StringComparison.Ordinal))
            {
                this.m_ObjectArgumentAssemblyTypeName = this.m_ObjectArgumentAssemblyTypeName.Substring(0, i) + ", UnityEngine";
            }
        }
    }
}
