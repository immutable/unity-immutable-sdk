using System;
using Unity.Collections;

namespace VoltstroStudios.NativeArraySpanExtensions
{
    /// <summary>
    ///     Provides <see cref="NativeArray{T}" /> copying utils to <see cref="Span{T}" />
    /// </summary>
    public static class SpanExtensions
    {
        /// <summary>
        ///     Copy data from a <see cref="Span{T}" /> to a <see cref="NativeArray{T}" />
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dst"></param>
        /// <typeparam name="T"></typeparam>
        public static void CopyTo<T>(this Span<T> source, NativeArray<T> dst)
            where T : unmanaged
        {
            CopyTo((ReadOnlySpan<T>)source, dst);
        }

        /// <summary>
        ///     Copy data from a <see cref="ReadOnlySpan{T}" /> to a <see cref="NativeArray{T}" />
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dst"></param>
        /// <typeparam name="T"></typeparam>
        public static void CopyTo<T>(this ReadOnlySpan<T> source, NativeArray<T> dst)
            where T : unmanaged
        {
            dst.CopyFrom(source);
        }

        /// <summary>
        ///     Creates a <see cref="NativeArray{T}" /> from a <see cref="Span{T}" />
        /// </summary>
        /// <param name="source"></param>
        /// <param name="allocator"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static NativeArray<T> ToNativeArray<T>(this Span<T> source, Allocator allocator)
            where T : unmanaged
        {
            return ToNativeArray((ReadOnlySpan<T>)source, allocator);
        }

        /// <summary>
        ///     Creates a <see cref="NativeArray{T}" /> from a <see cref="ReadOnlySpan{T}" />
        /// </summary>
        /// <param name="source"></param>
        /// <param name="allocator"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static NativeArray<T> ToNativeArray<T>(this ReadOnlySpan<T> source, Allocator allocator)
            where T : unmanaged
        {
            NativeArray<T> newArray = new NativeArray<T>(source.Length, allocator, NativeArrayOptions.UninitializedMemory);
            newArray.CopyFrom(source);
            return newArray;
        }
    }
}
