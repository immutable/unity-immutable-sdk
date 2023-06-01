using System;
using NUnit.Framework;
using Unity.Collections;

namespace VoltstroStudios.NativeArraySpanExtensions.Tests
{
    public class NativeArrayExtensionsTests
    {
        [GenericTestCase(typeof(byte), new byte[] { 1, 4, 6, 7, 54, 98 })]
        [GenericTestCase(typeof(int), new[] { 54, 76, 129, 7000, 438, 57, 192, 69 })]
        [GenericTestCase(typeof(float), new[] { 0.0002456f, 69.420f, 23f, 90032.2f, 47.6f })]
        public void CopyFromTest<T>(T[] testData)
            where T : unmanaged
        {
            NativeArray<T> testNativeArray =
                new(testData.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            try
            {
                Assert.IsTrue(testNativeArray.IsCreated);

                Span<T> testSpan = testData;
                testNativeArray.CopyFrom(testSpan);

                TestUtils.ValidateArrays(testNativeArray, testSpan);
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
            try
            {
                Assert.IsTrue(testNativeArray.IsCreated);

                Span<T> testSpan = new T[testData.Length];
                testNativeArray.CopyTo(testSpan);

                TestUtils.ValidateArrays(testNativeArray, testSpan);
            }
            finally
            {
                testNativeArray.Dispose();
            }
        }
    }
}