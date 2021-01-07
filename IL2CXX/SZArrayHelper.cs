using System;
using System.Collections;
using System.Collections.Generic;

namespace IL2CXX
{
    class SZArrayHelper<T> : IList<T>, IReadOnlyList<T>
    {
        public class Enumerator : IEnumerator<T>
        {
            private T[] array;
            private int index = -1;

            public Enumerator(T[] array) => this.array = array;
            public void Dispose() { }
            public bool MoveNext() => ++index < array.Length;
            public void Reset() => index = -1;
            public T Current => array[index];
            object IEnumerator.Current => Current;
        }

        public int Count => throw new NotImplementedException();
        public bool IsReadOnly => true;
        public T this[int index]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(T item) => IndexOf(item) >= 0;
        public void CopyTo(T[] array, int index) => throw new NotImplementedException();
        public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int IndexOf(T item) => throw new NotImplementedException();
        public void Insert(int index, T item) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();
    }
}
