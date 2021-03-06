﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgument() => throw new ArgumentException();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentNull() => throw new ArgumentNullException();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentOutOfRange() => throw new ArgumentOutOfRangeException();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowIndexOutOfRange() => throw new IndexOutOfRangeException();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidCast() => throw new InvalidCastException();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNullReference() => throw new NullReferenceException();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowOverflow() => throw new OverflowException();
    }
}
