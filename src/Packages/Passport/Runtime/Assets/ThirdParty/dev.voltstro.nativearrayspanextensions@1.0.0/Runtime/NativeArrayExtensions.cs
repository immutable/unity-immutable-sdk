using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace VoltstroStudios.NativeArraySpanExtensions
{
    /// <summary>
    ///     Provides <see cref="Span{T}" /> copying utils to <see cref="NativeArray{T}" />
    /// </summary>
    public static class NativeArrayExtensions
    {
        /// <summary>
        ///     Copy data from a <see cref="ReadOnlySpan{T}" /> to a <see cref="NativeArray{T}" />
        /// </summary>
        /// <param name="array"></param>
        /// <param name="source"></param>
        /// <typeparam name="T"></typeparam>
        public static unsafe void CopyFrom<T>(this NativeArray<T> array, ReadOnlySpan<T> source)
            where T : unmanaged
        {
            Utils.CheckCopyLengths(source.Length, array.Length);

            //Calling GetUnsafePtr will check if the array is valid for us
            //(if checks are enabled)
            void* dstPtr = array.GetUnsafePtr();

            fixed (void* srcPtr = source)
            {
                Utils.Copy<T>(srcPtr, 0, dstPtr, 0, array.Length);
            }
        }

        /// <summary>
        ///     Copy data from a <see cref="NativeArray{T}" /> to a <see cref="Span{T}" />
        /// </summary>
        /// <param name="array"></param>
        /// <param name="dst"></param>
        /// <typeparam name="T"></typeparam>
        public static unsafe void CopyTo<T>(this NativeArray<T> array, Span<T> dst)
            where T : unmanaged
        {
            Utils.CheckCopyLengths(array.Length, dst.Length);

            //Calling GetUnsafePtr will check if the array is valid for us
            //(if checks are enabled)
            void* srcPtr = array.GetUnsafePtr();

            fixed (void* dstPtr = dst)
            {
                Utils.Copy<T>(srcPtr, 0, dstPtr, 0, array.Length);
            }
        }
    }
}