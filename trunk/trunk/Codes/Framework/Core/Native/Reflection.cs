////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//
// Uncomment to debug the serialization.
//

using System;
using System.Collections;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.SPOT
{
    [Flags()]
    public enum SerializationFlags
    {
        //
        // Keep in sync with Microsoft.SPOT.Debugger.SerializationHints!!!!
        //

        Encrypted = 0x00000001,
        Compressed = 0x00000002, // Value uses range compression (max 2^30 values).
        Optional = 0x00000004, // If the value cannot be deserialized, skip it.

        PointerNeverNull = 0x00000010,
        ElementsNeverNull = 0x00000020,

        FixedType = 0x00000100,

        DemandTrusted = 0x00010000,
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = true)]
    public class SerializationHintsAttribute : Attribute
    {
        //
        // Keep in sync with Microsoft.SPOT.Debugger.SerializationHintsAttribute!!!!
        //

        public SerializationFlags Flags;

        public int ArraySize; // -1 == extend to the end of the stream.

        public int BitPacked;     // In bits.
        public long RangeBias;
        public ulong Scale;         // For time, it's in ticks.
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    sealed public class FieldNoReflectionAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    sealed public class GloballySynchronizedAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    sealed public class PublishInApplicationDirectoryAttribute : Attribute
    {
    }

    public class UnknownTypeException : Exception
    {
        public Type m_type;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////
    public static class Reflection
    {
#if TINYCLR_TRACE_DOWNLOAD
        [Serializable] // Used in some logging.
#endif
        public class AssemblyInfo
        {
            public const uint c_Flags_NeedReboot = 0x00000001;

            public string m_name;
            public uint m_flags;
            public int m_size;
            public uint m_hash;
            public uint[] m_refs;
        }

        //--//

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern static public Type[] GetTypesImplementingInterface(Type itf);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern static public bool IsTypeLoaded(Type t);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern static public uint GetTypeHash(Type t);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern static public uint GetAssemblyHash(Assembly assm);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern static public Assembly[] GetAssemblies();
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern static public bool GetAssemblyInfo(byte[] assm, AssemblyInfo ai);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern static public Type GetTypeFromHash(uint hash);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern static public Assembly GetAssemblyFromHash(uint hash);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern static public byte[] Serialize(object o, Type t);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern static public object Deserialize(byte[] v, Type t);
    }
}


