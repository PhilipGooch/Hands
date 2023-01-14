using System.Collections.Generic;

namespace NBG.MeshGeneration
{
    public class DataPool<T> where T : new()
    {
        private List<T> pool;

        public DataPool(int size = 16)
        {
            pool = new List<T>(size);
            for (int i = 0; i < size; i++)
            {
                pool.Add(new T());
            }
        }

        public T Get()
        {
            if (pool.Count > 0)
            {
                int lastIndex = pool.Count - 1;
                var element = pool[lastIndex];
                pool.RemoveAt(lastIndex);
                return element;
            }
            else
            {
                return new T();
            }
        }

        public void Recycle(T element)
        {
            pool.Add(element);
        }
    }
}