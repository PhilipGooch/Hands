using System;

namespace NBG.Core
{
    public static class ArrayUtilities
    {
        //No garbage array sort
        //No garbage comparer example -> static Func<RaycastHit, RaycastHit, bool> Comparer = (a, b) => a.distance > b.distance;
        public static void InsertionSort<T>(T[] items, int length, Func<T, T, bool> compare)
        {
            for (int i = 0; i < length; ++i)
            {
                var current = items[i];
                for (int j = i - 1; j >= 0 && !compare(current, items[j]); --j)
                {
                    items[j + 1] = items[j];
                    items[j] = current;
                }
            }
        }
    }
}
