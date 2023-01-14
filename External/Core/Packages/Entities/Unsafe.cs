using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.Unsafe
{

    public unsafe static class Unsafe
    {
        [Conditional("DEBUG")]
        public static unsafe void CheckIndex(int index, int length)
        {
            if (index < 0 || index >= length)
            {
                UnityEngine.Debug.LogError($"Index '{index}' is out of range of '{length}' length.");
                throw new IndexOutOfRangeException("Index is out of range.");
            }
        }

        [Conditional("DEBUG")]
        public static void CheckType<T1, T2>()
        {
            if (BurstRuntime.GetHashCode32<T1>() != BurstRuntime.GetHashCode32<T2>())
                throw new InvalidOperationException("Types don't match");
        }

        public static void Resize<T>(ref T* array, int count, int newCount, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) where T : unmanaged
        {
            var newArray = Malloc<T>(newCount, allocator, newCount > count ? options : NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpy(newArray, array, math.min(count, newCount) * UnsafeUtility.SizeOf<T>());
            Free(array, allocator);
            array = newArray;
        }
        public static void Resize<T>(ref NativeArray<T> array, int newCount, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) where T : unmanaged
        {
            var newArray = new NativeArray<T>(newCount, allocator, newCount > array.Length ? options : NativeArrayOptions.UninitializedMemory);

            NativeArray<T>.Copy(array, newArray, math.min(array.Length, newCount));
            array.Dispose();
            array = newArray;
        }
        public static void Resize<T>(ref T[] array, int newCount)
        {
            var newArray = new T[newCount];
            Array.Copy(array, newArray, math.min(array.Length, newCount));
            array = newArray;
        }
        public static T* Malloc<T>(Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) where T : unmanaged
        {
            return Malloc<T>(1, allocator, options);
        }
       
        public static T* Malloc<T>(int count, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>() * count;
            var ret = (T*)UnsafeUtility.Malloc(size, 4, allocator);
            if (options == NativeArrayOptions.ClearMemory)
                UnsafeUtility.MemClear(ret, size);
            return ret;
        }
        public static T* MallocCopy<T>(in T src, Allocator allocator) where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>();
            var ret = (T*)UnsafeUtility.Malloc(size, 4, allocator);

            var handle = GCHandle.Alloc(src, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();
            UnsafeUtility.MemCpy(
                (byte*)ret,
                (byte*)addr,
                size);
            handle.Free();

            return ret;
        }

        public static T* Malloc<T>(T[] src, Allocator allocator) where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>() * src.Length;
            var ret = (T*)UnsafeUtility.Malloc(size, 4, allocator);

            var handle = GCHandle.Alloc(src, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();
            UnsafeUtility.MemCpy(
                (byte*)ret,
                (byte*)addr,
                size);
            handle.Free();

            return ret;
        }
        public static T* Malloc<T>(UnsafeList<T> src, Allocator allocator) where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>() * src.Length;
            var ret = (T*)UnsafeUtility.Malloc(size, 4, allocator);
            UnsafeUtility.MemCpy(
                (byte*)ret,
                (byte*)src.Ptr,
                size);

            return ret;
        }
        public static T* Malloc<T>(NativeList<T> src, Allocator allocator) where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>() * src.Length;
            var ret = (T*)UnsafeUtility.Malloc(size, 4, allocator);
            UnsafeUtility.MemCpy(
                (byte*)ret,
                (byte*)src.GetUnsafeReadOnlyPtr(),
                size);

            return ret;
        }
        public static void Free<T>(T* ptr, Allocator allocator) where T : unmanaged
        {
            UnsafeUtility.Free(ptr, allocator);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static T* AsPointer<T>(ref this T reference) where T : unmanaged
        {
            return (T*)UnsafeUtility.AddressOf(ref reference);
        }
        public unsafe static ref T AsRef<T>(this IntPtr ptr) where T : unmanaged
        {
            return ref (*(T*)ptr);
            //return (T*)UnsafeUtility.AddressOf(ref pointer);
        }

        public static void CopyTo<T>(T* src, T[] dst) where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>() * dst.Length;
            var handle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();
            UnsafeUtility.MemCpy(
                (byte*)addr,
                (byte*)src,
                size);
            handle.Free();
        }
        public static void CopyTo<T>(T[] src, T* dst) where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>() * src.Length;
            var handle = GCHandle.Alloc(src, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();
            UnsafeUtility.MemCpy(

                (byte*)dst,
                (byte*)addr,
                size);
            handle.Free();
        }

   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ref T ItemAsRef<T>(this NativeArray<T> array, int idx) where T : unmanaged
        {
            Unsafe.CheckIndex(idx, array.Length);
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), idx);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T* ItemAsPointer<T>(this NativeArray<T> array, int idx) where T : unmanaged
        {
            Unsafe.CheckIndex(idx, array.Length);
            return (T*)array.GetUnsafePtr() + idx;
        }

    }
}
