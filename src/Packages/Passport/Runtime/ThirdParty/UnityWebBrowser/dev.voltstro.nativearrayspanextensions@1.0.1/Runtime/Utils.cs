using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

namespace VoltstroStudios.NativeArraySpanExtensions
{
    internal static class Utils
    {
        internal static unsafe void Copy<T>(void* src, int srcIndex,
            void* dst, int dstIndex,
            int length)
            where T : unmanaged
        {
            UnsafeUtility.MemCpy((void*)((IntPtr)dst + dstIndex * UnsafeUtility.SizeOf<T>()),
                (void*)((IntPtr)src + srcIndex * UnsafeUtility.SizeOf<T>()),
                length * UnsafeUtility.SizeOf<T>());
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckCopyLengths(int srcLength, int dstLength)
        {
            if (srcLength != dstLength)
                throw new ArgumentException("source and destination length must be the same");
        }
    }
}