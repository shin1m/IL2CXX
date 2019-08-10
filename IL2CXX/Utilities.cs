using System;
using System.Collections.Generic;

namespace IL2CXX
{
    static class Utilities
    {
        public static void For<T>(this T x, Action<T> action) => action(x);
        public static void ForEach<T>(this IEnumerable<T> xs, Action<T> action)
        {
            foreach (var x in xs) action(x);
        }
        public static void ForEach<T>(this IEnumerable<T> xs, Action<T, int> action)
        {
            var i = 0;
            foreach (var x in xs) action(x, i++);
        }
    }
}
