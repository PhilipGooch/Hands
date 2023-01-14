using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace NBG.Unsafe
{
    public unsafe struct UnsafeArray<T> :IDisposable, IEnumerable, IEnumerable<T>
        where T:unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        public IntPtr ptr;
        public int Length;
        private int _allocatorInt;
        Allocator allocator { get => (Allocator)_allocatorInt; set => _allocatorInt = (int)value; }

        public T* AsPointer() => (T*)ptr;

        public UnsafeArray(int count, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            this.Length = count;
            this._allocatorInt = (int)allocator;
            this.ptr = (IntPtr)NBG.Unsafe.Unsafe.Malloc<T>(count, allocator, options);
        }
        public UnsafeArray(T[]src, Allocator allocator): this(src.Length, allocator)
        {
            int size = UnsafeUtility.SizeOf<T>() * Length;

            var handle = GCHandle.Alloc(src, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();
            UnsafeUtility.MemCpy(
                (byte*)ptr,
                (byte*)addr,
                size);
            handle.Free();
        }
        public UnsafeArray(UnsafeArray<T> src, Allocator allocator) : this(src.Length, allocator)
        {
            int size = UnsafeUtility.SizeOf<T>() * Length;

            UnsafeUtility.MemCpy(
                (byte*)ptr,
                (byte*)src.ptr,
                size);
        }

        public void Dispose()
        {
            NBG.Unsafe.Unsafe.Free((T*)ptr, allocator);
            ptr = IntPtr.Zero;
        }
        public T this[int index]
        {
            get { return ((T*)ptr)[index]; }
            set { ((T*)ptr)[index] = value; }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ref T ElementAt(int index)
        {
            NBG.Unsafe.Unsafe.CheckIndex(index, Length);
            return ref ((T*)ptr)[index];
        }
        /// <summary>
        /// Returns an IEnumerator interface for the container.
        /// </summary>
        /// <returns>An IEnumerator interface for the container.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator { m_Ptr = (T*)ptr, m_Length = Length, m_Index = -1 };
        }

        /// <summary>
        /// This method is not implemented. It will throw NotImplementedException if it is used.
        /// </summary>
        /// <remarks>Use Enumerator GetEnumerator() instead.</remarks>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is not implemented. It will throw NotImplementedException if it is used.
        /// </summary>
        /// <remarks>Use Enumerator GetEnumerator() instead.</remarks>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implements iterator over the container.
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            internal T* m_Ptr;
            internal int m_Length;
            internal int m_Index;

            /// <summary>
            /// Disposes enumerator.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element of the container.
            /// </summary>
            /// <returns>Returns true if the iterator is successfully moved to the next element, otherwise it returns false.</returns>
            public bool MoveNext() => ++m_Index < m_Length;

            /// <summary>
            /// Resets the enumerator to the first element of the container.
            /// </summary>
            public void Reset() => m_Index = -1;

            /// <summary>
            /// Gets the element at the current position of the enumerator in the container.
            /// </summary>
            public T Current => m_Ptr[m_Index];

            object IEnumerator.Current => Current;
        }

    }
}
