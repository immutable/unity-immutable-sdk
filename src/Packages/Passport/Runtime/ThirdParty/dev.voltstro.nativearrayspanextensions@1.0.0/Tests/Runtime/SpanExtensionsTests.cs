using System;
using NUnit.Framework;
using Unity.Collections;

namespace VoltstroStudios.NativeArraySpanExtensions.Tests
{
    public class SpanExtensionsTests
    {
        [GenericTestCase(typeof(byte), new byte[] { 1, 4, 6, 7, 54, 98 })]
        [GenericTestCase(typeof(int), new[] { 54, 76, 129, 7000, 438, 57, 192, 69 })]
        [GenericTestCase(typeof(float), new[] { 0.0002456f, 69.420f, 23f, 90032.2f, 47.6f })]
        public void CopyToReadOnlyTest<T>(T[] testData)
            where T : unmanaged
        {
            NativeArray<T> testNativeArray =
                new(testData.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            try
            {
                Assert.IsTrue(testNativeArray.IsCreated);

                ReadOnlySpan<T> testReadOnlySpan = testData;
                testReadOnlySpan.CopyTo(testNativeArray);

                TestUtils.ValidateArrays(testNativeArray, testReadOnlySpan);
            }
            finally
            {
                testNativeArray.Dispose();
            }
        }

        [GenericTestCase(typeof(byte), new byte[] { 1, 4, 6, 7, 54, 98 })]
        [GenericTestCase(typeof(int), new[] { 54, 76, 129, 7000, 438, 57, 192, 69 })]
        [GenericTestCase(typeof(float), new[] { 0.0002456f, 69.420f, 23f, 90032.2f, 47.6f })]
        public void CopyToTest<T>(T[] testData)
            where T : unmanaged
        {
            NativeArray<T> testNativeArray =
                new(testData.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            try
            {
                Assert.IsTrue(testNativeArray.IsCreated);

                Span<T> testReadOnlySpan = testData;
                testReadOnlySpan.CopyTo(testNativeArray);

                TestUtils.ValidateArrays(testNativeArray, testReadOnlySpan);
            }
            finally
            {
                testNativeArray.Dispose();
            }
        }

        [GenericTestCase(typeof(byte), new byte[] { 1, 4, 6, 7, 54, 98 })]
        [GenericTestCase(typeof(int), new[] { 54, 76, 129, 7000, 438, 57, 192, 69 })]
        [GenericTestCase(typeof(float), new[] { 0.0002456f, 69.420f, 23f, 90032.2f, 47.6f })]
        public void ToNativeArrayReadOnlyTest<T>(T[] testData)
            where T : unmanaged
        {
            ReadOnlySpan<T> testDataSpan = testData;
            NativeArray<T> nativeArray = testDataSpan.ToNativeArray(Allocator.Temp);
            try
            {
                TestUtils.ValidateArrays(nativeArray, testDataSpan);
            }
            finally
            {
                nativeArray.Dispose();
            }
        }

        [GenericTestCase(typeof(byte), new byte[] { 1, 4, 6, 7, 54, 98 })]
        [GenericTestCase(typeof(int), new[] { 54, 76, 129, 7000, 438, 57, 192, 69 })]
        [GenericTestCase(typeof(float), new[] { 0.0002456f, 69.420f, 23f, 90032.2f, 47.6f })]
        public void ToNativeArrayTest<T>(T[] testData)
            where T : unmanaged
        {
            Span<T> testDataSpan = testData;
            NativeArray<T> nativeArray = testDataSpan.ToNativeArray(Allocator.Temp);
            try
            {
                TestUtils.ValidateArrays(nativeArray, testDataSpan);
            }
            finally
            {
                nativeArray.Dispose();
            }
        }
    }
}