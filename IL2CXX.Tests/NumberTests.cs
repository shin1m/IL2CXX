using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class NumberTests
    {
        static int Single()
        {
            if (!float.IsPositiveInfinity(float.PositiveInfinity)) return 1;
            if (!float.IsNegativeInfinity(float.NegativeInfinity)) return 2;
            if (!float.IsNaN(float.NaN)) return 3;
            {
                var x = float.MinValue;
                if (x != float.MinValue) return 4;
            }
            {
                var x = float.MaxValue;
                if (x != float.MaxValue) return 5;
            }
            {
                var x = 257;
                if (x != 257.0f) return 6;
            }
            return 0;
        }
        static int Double()
        {
            if (!double.IsPositiveInfinity(double.PositiveInfinity)) return 1;
            if (!double.IsNegativeInfinity(double.NegativeInfinity)) return 2;
            if (!double.IsNaN(double.NaN)) return 3;
            {
                var x = double.MinValue;
                if (x != double.MinValue) return 4;
            }
            {
                var x = double.MaxValue;
                if (x != double.MaxValue) return 5;
            }
            {
                var x = 257;
                if (x != 257.0) return 6;
            }
            return 0;
        }
        static int Unordered()
        {
            int ne(float x, float y) => x != y ? 1 : 0;
            if (ne(float.NaN, float.NaN) == 0) return 1;
            int lt(float x, float y) => !(x >= y) ? 1 : 0;
            if (lt(float.NaN, float.NaN) == 0) return 2;
            int le(float x, float y) => !(x > y) ? 1 : 0;
            if (le(float.NaN, float.NaN) == 0) return 3;
            int gt(float x, float y) => !(x <= y) ? 1 : 0;
            if (gt(float.NaN, float.NaN) == 0) return 4;
            int ge(float x, float y) => !(x < y) ? 1 : 0;
            if (ge(float.NaN, float.NaN) == 0) return 5;
            return 0;
        }
        static int Int32()
        {
            var x = new IntPtr(32);
            return x.ToInt32() == 32 ? 0 : 1;
        }
        static unsafe int Pointer()
        {
            var x = new IntPtr(32);
            return new IntPtr(x.ToPointer()) == x ? 0 : 1;
        }
        enum Names
        {
            Foo, Bar, Zot
        }
        static int GetNames() => Enum.GetNames(typeof(Names)).SequenceEqual(new[] { "Foo", "Bar", "Zot" }) ? 0 : 1;
        static int GetValues() => Enum.GetValues(typeof(Names)).Cast<Names>().SequenceEqual(new[] { Names.Foo, Names.Bar, Names.Zot }) ? 0 : 1;
        static int ToStringDefault() => Names.Foo.ToString() == "Foo" ? 0 : 1;
        static int ToStringG() => Names.Foo.ToString("g") == "Foo" ? 0 : 1;

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(Single) => Single(),
            nameof(Double) => Double(),
            nameof(Unordered) => Unordered(),
            nameof(Int32) => Int32(),
            nameof(Pointer) => Pointer(),
            nameof(GetNames) => GetNames(),
            nameof(GetValues) => GetValues(),
            nameof(ToStringDefault) => ToStringDefault(),
            nameof(ToStringG) => ToStringG(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run);
        [TestCase(nameof(Single))]
        [TestCase(nameof(Double))]
        [TestCase(nameof(Unordered))]
        [TestCase(nameof(Int32))]
        [TestCase(nameof(Pointer))]
        [TestCase(nameof(GetNames))]
        [TestCase(nameof(GetValues))]
        [TestCase(nameof(ToStringDefault))]
        [TestCase(nameof(ToStringG))]
        public void Test(string name) => Utilities.Run(build, name);
    }
}
