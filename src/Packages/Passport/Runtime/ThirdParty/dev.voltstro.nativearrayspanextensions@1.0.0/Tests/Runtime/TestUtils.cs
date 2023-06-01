using System;
using NUnit.Framework;
using Unity.Collections;

namespace VoltstroStudios.NativeArraySpanExtensions.Tests
{
    internal static class TestUtils
    {
        internal static void ValidateArrays<T>(NativeArray<T> nativeArray, ReadOnlySpan<T> span)
            where T : unmanaged
        {
            Assert.AreEqual(nativeArray.Length, span.Length);
            for (int i = 0; i < nativeArray.Length; i++) Assert.AreEqual(nativeArray[i], span[i]);
        }
    }
}