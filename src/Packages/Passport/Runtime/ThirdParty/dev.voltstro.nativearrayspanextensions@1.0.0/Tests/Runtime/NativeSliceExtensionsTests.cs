using System;
using NUnit.Framework;
using Unity.Collections;

namespace VoltstroStudios.NativeArraySpanExtensions.Tests
{
    public class NativeSliceExtensionsTests
    {
        [GenericTestCase(typeof(byte), new byte[] { 1, 4, 6, 7, 54, 98 })]
        [GenericTestCase(typeof(int), new[] { 54, 76, 129, 7000, 438, 57, 192, 69 })]
        [GenericTestCase(typeof(float), new[] { 0.0002456f, 69.420f, 23f, 90032.2f, 47.6f })]
        public void CopyFromTest<T>(T[] testData)
            where T : unmanaged
        {
            NativeArray<T> testNativeArray =
                new(testData.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            NativeSlice<T> testNativeSlice = testNativeArray.Slice();

            try
            {
                Assert.IsTrue(testNativeArray.IsCreated);

                Span<T> testSpan = testData;
                testNativeSlice.CopyFrom(testSpan);

                ValidateArrays(testNativeSlice, testSpan);
            }
            finally
            {
                testNativeArray.Dispose();
            }
        }

        [GenericTestCase(typeof(byte), new byte[] { 1, 4, 6, 7, 54, 98 })]
        [GenericTestCase(typeof(int), new[] { 54, 76, 129, 7000, 438, 57, 192, 69 })]
        [GenericTestCase(typeof(float), new[] { 0.0002456f, 69.420f, 23f, 90032.2f, 47.6f })]
        public void CopyTo<T>(T[] testData)
            where T : unmanaged
        {
            NativeArray<T> testNativeArray = new(testData, Allocator.Temp);
            NativeSlice<T> testNativeSlice = testNativeArray.Slice();

            try
            {
                Assert.IsTrue(testNativeArray.IsCreated);

                Span<T> testSpan = new T[testData.Length];
                testNativeSlice.CopyTo(testSpan);

                ValidateArrays(testNativeSlice, testSpan);
            }
            finally
            {
                testNativeArray.Dispose();
            }
        }

        private static void ValidateArrays<T>(NativeSlice<T> nativeArray, ReadOnlySpan<T> span)
            where T : unmanaged
        {
            Assert.AreEqual(nativeArray.Length, span.Length);
            for (int i = 0; i < nativeArray.Length; i++) Assert.AreEqual(nativeArray[i], span[i]);
        }
    }
}