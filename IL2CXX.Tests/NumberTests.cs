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
        [Flags]
        enum Flags
        {
            None = 0,
            X = 1,
            Y = 2,
            XY = 3
        }
        static int EnumGetName() => Enum.GetName(typeof(Names), Names.Foo) == "Foo" ? 0 : 1;
        static int EnumGetNameOfT() => Enum.GetName(Names.Foo) == "Foo" ? 0 : 1;
        static int EnumGetNames() => Enum.GetNames(typeof(Names)).SequenceEqual(["Foo", "Bar", "Zot"]) ? 0 : 1;
        static int EnumGetNamesOfT() => Enum.GetNames<Names>().SequenceEqual(["Foo", "Bar", "Zot"]) ? 0 : 1;
        static int EnumGetValues() => Enum.GetValues(typeof(Names)).Cast<Names>().SequenceEqual([Names.Foo, Names.Bar, Names.Zot]) ? 0 : 1;
        static int EnumGetValuesOfT() => Enum.GetValues<Names>().Cast<Names>().SequenceEqual([Names.Foo, Names.Bar, Names.Zot]) ? 0 : 1;
        static int EnumHasFlag() => Flags.XY.HasFlag(Flags.Y) ? 0 : 1;
        static int EnumToStringDefault() => Names.Bar.ToString() == "Bar" ? 0 : 1;
        static int EnumToStringG() => Names.Bar.ToString("g") == "Bar" ? 0 : 1;
        static int EnumISpanFormattableTryFormat()
        {
            var cs = new char[8];
            if (!((ISpanFormattable)Names.Bar).TryFormat(cs, out var n, "", null)) return 1;
            if (n != 3) return 2;
            return new string(cs, 0, n) == "Bar" ? 0 : 3;
        }
        static int EnumTryFormat()
        {
            var cs = new char[8];
            if (!Enum.TryFormat(Names.Bar, cs, out var n)) return 1;
            if (n != 3) return 2;
            return new string(cs, 0, n) == "Bar" ? 0 : 3;
        }

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
            nameof(EnumGetName) => EnumGetName(),
            nameof(EnumGetNameOfT) => EnumGetNameOfT(),
            nameof(EnumGetNames) => EnumGetNames(),
            nameof(EnumGetNamesOfT) => EnumGetNamesOfT(),
            nameof(EnumGetValues) => EnumGetValues(),
            nameof(EnumGetValuesOfT) => EnumGetValuesOfT(),
            nameof(EnumHasFlag) => EnumHasFlag(),
            nameof(EnumToStringDefault) => EnumToStringDefault(),
            nameof(EnumToStringG) => EnumToStringG(),
            nameof(EnumISpanFormattableTryFormat) => EnumISpanFormattableTryFormat(),
            nameof(EnumTryFormat) => EnumTryFormat(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run, null, [
            typeof(Names)
        ]);
        [Test]
        public void Test(
            [Values(
                nameof(Unchecked),
                nameof(CheckedBinary),
                nameof(CheckedCast),
                nameof(CheckedBinaryUnsigned),
                nameof(CheckedCastUnsigned),
                nameof(Single),
                nameof(Double),
                nameof(Unordered),
                nameof(ToInt32),
                nameof(ToPointer),
                nameof(EnumGetName),
                nameof(EnumGetNameOfT),
                nameof(EnumGetNames),
                nameof(EnumGetNamesOfT),
                nameof(EnumGetValues),
                nameof(EnumGetValuesOfT),
                nameof(EnumHasFlag),
                nameof(EnumToStringDefault),
                nameof(EnumToStringG),
                nameof(EnumISpanFormattableTryFormat),
                nameof(EnumTryFormat)
            )] string name,
            [Values] bool cooperative
        ) => Utilities.Run(build, cooperative, name);
    }
}
