using System;
using System.Linq;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class NumberTests
    {
        static readonly int mask = 0xfffff;
        static int Unchecked()
        {
            for (var i = 0; i < 32; ++i) Console.WriteLine(i * 104395303 & mask);
            return 0;
        }
        static readonly int max = 2147483647;
        static int CheckedBinary()
        {
            try
            {
                Console.WriteLine(checked(max + 10));
                return 1;
            }
            catch (OverflowException)
            {
                return 0;
            }
        }
        static int CheckedCast()
        {
            try
            {
                Console.WriteLine(checked((short)max));
                return 1;
            }
            catch (OverflowException)
            {
                return 0;
            }
        }
        static readonly uint umax = 4294967295u;
        static int CheckedBinaryUnsigned()
        {
            try
            {
                Console.WriteLine(checked(umax + 10u));
                return 1;
            }
            catch (OverflowException)
            {
                return 0;
            }
        }
        static int CheckedCastUnsigned()
        {
            try
            {
                Console.WriteLine(checked((ushort)umax));
                return 1;
            }
            catch (OverflowException)
            {
                return 0;
            }
        }
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
        static int ToInt32()
        {
            var x = new IntPtr(32);
            return x.ToInt32() == 32 ? 0 : 1;
        }
        static unsafe int ToPointer()
        {
            var x = new IntPtr(32);
            return new IntPtr(x.ToPointer()) == x ? 0 : 1;
        }
        enum Names
        {
            Foo, Bar, Zot
        }
        static int EnumGetNames() => Enum.GetNames(typeof(Names)).SequenceEqual(new[] { "Foo", "Bar", "Zot" }) ? 0 : 1;
        static int EnumGetValues() => Enum.GetValues(typeof(Names)).Cast<Names>().SequenceEqual(new[] { Names.Foo, Names.Bar, Names.Zot }) ? 0 : 1;
        static int EnumToStringDefault() => Names.Bar.ToString() == "Bar" ? 0 : 1;
        static int EnumToStringG() => Names.Bar.ToString("g") == "Bar" ? 0 : 1;

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(Unchecked) => Unchecked(),
            nameof(CheckedBinary) => CheckedBinary(),
            nameof(CheckedCast) => CheckedCast(),
            nameof(CheckedBinaryUnsigned) => CheckedBinaryUnsigned(),
            nameof(CheckedCastUnsigned) => CheckedCastUnsigned(),
            nameof(Single) => Single(),
            nameof(Double) => Double(),
            nameof(Unordered) => Unordered(),
            nameof(ToInt32) => ToInt32(),
            nameof(ToPointer) => ToPointer(),
            nameof(EnumGetNames) => EnumGetNames(),
            nameof(EnumGetValues) => EnumGetValues(),
            nameof(EnumToStringDefault) => EnumToStringDefault(),
            nameof(EnumToStringG) => EnumToStringG(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run);
        [TestCase(nameof(Unchecked))]
        [TestCase(nameof(CheckedBinary))]
        [TestCase(nameof(CheckedCast))]
        [TestCase(nameof(CheckedBinaryUnsigned))]
        [TestCase(nameof(CheckedCastUnsigned))]
        [TestCase(nameof(Single))]
        [TestCase(nameof(Double))]
        [TestCase(nameof(Unordered))]
        [TestCase(nameof(ToInt32))]
        [TestCase(nameof(ToPointer))]
        [TestCase(nameof(EnumGetNames))]
        [TestCase(nameof(EnumGetValues))]
        [TestCase(nameof(EnumToStringDefault))]
        [TestCase(nameof(EnumToStringG))]
        public void Test(string name) => Utilities.Run(build, name);
    }
}
