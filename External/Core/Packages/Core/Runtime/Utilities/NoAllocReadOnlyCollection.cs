using System;
using System.Collections;
using System.Collections.Generic;

namespace NBG.Core
{
    /// <summary>
    /// Read only collection that doesn't generate garbage when used in a foreach.
    /// </summary>
    public struct NoAllocReadOnlyCollection<T> : IEnumerable<T>
    {
        readonly List<T> m_Source;

        public NoAllocReadOnlyCollection(List<T> source) => m_Source = source;

        public int Count => m_Source.Count;

        public T this[int index] => m_Source[index];

        public List<T>.Enumerator GetEnumerator() => m_Source.GetEnumerator();

        public bool Contains(T item) => m_Source.Contains(item);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => throw new NotSupportedException($"To avoid boxing, do not cast {nameof(NoAllocReadOnlyCollection<T>)} to IEnumerable<T>.");
        IEnumerator IEnumerable.GetEnumerator()
            => throw new NotSupportedException($"To avoid boxing, do not cast {nameof(NoAllocReadOnlyCollection<T>)} to IEnumerable.");
    }
}
