using System;
using System.Numerics;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class NumericsTests
    {
        static int VectorAdd()
        {
            var x = new Vector<float>(1f) + new Vector<float>(2f);
            return x[0] == 3f ? 0 : 1;
        }
        static int VectorSubtract()
        {
            var x = new Vector<float>(1f) - new Vector<float>(2f);
            return x[0] == -1f ? 0 : 1;
        }
        static int VectorMultiply()
        {
            var x = new Vector<float>(2f) * new Vector<float>(3f);
            return x[0] == 6f ? 0 : 1;
        }
        static int VectorMultiplyValue()
        {
            var x = new Vector<float>(2f) * 3f;
            return x[0] == 6f ? 0 : 1;
        }
        static int VectorDivide()
        {
            var x = new Vector<float>(3f) / new Vector<float>(2f);
            return x[0] == 1.5f ? 0 : 1;
        }
        static int VectorEquality()
        {
            var x = new Vector<float>(1f);
            var y = new Vector<float>(1f);
            return x == y ? 0 : 1;
        }
        static int VectorEquals()
        {
            var x = Vector.Equals<float>(new Vector<float>(1f), new Vector<float>(1f));
            return float.IsNaN(x[0]) ? 0 : 1;
        }
        static int VectorLessThan()
        {
            var x = Vector.LessThan<float>(new Vector<float>(1f), new Vector<float>(2f));
            return float.IsNaN(x[0]) ? 0 : 1;
        }
        static int VectorLessThanOrEqual()
        {
            var x = Vector.LessThanOrEqual<float>(new Vector<float>(1f), new Vector<float>(1f));
            return float.IsNaN(x[0]) ? 0 : 1;
        }
        static int VectorGreaterThan()
        {
            var x = Vector.GreaterThan<float>(new Vector<float>(2f), new Vector<float>(1f));
            return float.IsNaN(x[0]) ? 0 : 1;
        }
        static int VectorGreaterThanOrEqual()
        {
            var x = Vector.GreaterThanOrEqual<float>(new Vector<float>(1f), new Vector<float>(1f));
            return float.IsNaN(x[0]) ? 0 : 1;
        }
        static int VectorComplement()
        {
            var x = ~new Vector<uint>(0);
            return x[0] == uint.MaxValue ? 0 : 1;
        }
        static int VectorAbs()
        {
            var x = Vector.Abs(new Vector<float>(-1f));
            return x[0] == 1f ? 0 : 1;
        }
        static int VectorMin()
        {
            var x = Vector.Min(new Vector<float>(1f), new Vector<float>(2f));
            return x[0] == 1f ? 0 : 1;
        }
        static int VectorMax()
        {
            var x = Vector.Max(new Vector<float>(1f), new Vector<float>(2f));
            return x[0] == 2f ? 0 : 1;
        }
        static int VectorDot()
        {
            var x = Vector.Dot(new Vector<float>(1f), new Vector<float>(2f));
            return x == 2f * Vector<float>.Count ? 0 : 1;
        }
        static int VectorSum()
        {
            var x = Vector.Sum(new Vector<float>(1f));
            return x == Vector<float>.Count ? 0 : 1;
        }
        static int VectorSquareRoot()
        {
            var x = Vector.SquareRoot(new Vector<float>(2f));
            return x[0] == MathF.Sqrt(2f) ? 0 : 1;
        }
        static int VectorCeiling()
        {
            var x = Vector.Ceiling(new Vector<float>(1.5f));
            return x[0] == 2f ? 0 : 1;
        }
        static int VectorFloor()
        {
            var x = Vector.Floor(new Vector<float>(1.5f));
            return x[0] == 1f ? 0 : 1;
        }
        static int VectorWiden()
        {
            Vector.Widen(new Vector<short>(-1), out var x, out var y);
            return x[0] == -1 && y[0] == -1 ? 0 : 1;
        }
        static int VectorNarrow()
        {
            var x = Vector.Narrow(new Vector<int>(-1), new Vector<int>(-1));
            return x[0] == (short)-1 ? 0 : 1;
        }
        static int VectorConvertToSingle()
        {
            var x = Vector.ConvertToSingle(new Vector<int>(-1));
            return x[0] == -1f ? 0 : 1;
        }
        static int VectorConvertToDouble()
        {
            var x = Vector.ConvertToDouble(new Vector<long>(-1));
            return x[0] == -1.0 ? 0 : 1;
        }
        static int VectorConvertToInt32()
        {
            var x = Vector.ConvertToInt32(new Vector<float>(-1f));
            return x[0] == -1 ? 0 : 1;
        }
        static int VectorConvertToInt64()
        {
            var x = Vector.ConvertToInt64(new Vector<double>(-1.0));
            return x[0] == (long)-1 ? 0 : 1;
        }

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(VectorAdd) => VectorAdd(),
            nameof(VectorSubtract) => VectorSubtract(),
            nameof(VectorMultiply) => VectorMultiply(),
            nameof(VectorMultiplyValue) => VectorMultiplyValue(),
            nameof(VectorDivide) => VectorDivide(),
            nameof(VectorEquality) => VectorEquality(),
            nameof(VectorEquals) => VectorEquals(),
            nameof(VectorLessThan) => VectorLessThan(),
            nameof(VectorLessThanOrEqual) => VectorLessThanOrEqual(),
            nameof(VectorGreaterThan) => VectorGreaterThan(),
            nameof(VectorGreaterThanOrEqual) => VectorGreaterThanOrEqual(),
            nameof(VectorComplement) => VectorComplement(),
            nameof(VectorAbs) => VectorAbs(),
            nameof(VectorMin) => VectorMin(),
            nameof(VectorMax) => VectorMax(),
            nameof(VectorDot) => VectorDot(),
            nameof(VectorSum) => VectorSum(),
            nameof(VectorSquareRoot) => VectorSquareRoot(),
            nameof(VectorCeiling) => VectorCeiling(),
            nameof(VectorFloor) => VectorFloor(),
            nameof(VectorWiden) => VectorWiden(),
            nameof(VectorNarrow) => VectorNarrow(),
            nameof(VectorConvertToSingle) => VectorConvertToSingle(),
            nameof(VectorConvertToDouble) => VectorConvertToDouble(),
            nameof(VectorConvertToInt32) => VectorConvertToInt32(),
            nameof(VectorConvertToInt64) => VectorConvertToInt64(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run);
        [Test]
        public void Test(
            [Values(
                nameof(VectorAdd),
                nameof(VectorSubtract),
                nameof(VectorMultiply),
                nameof(VectorMultiplyValue),
                nameof(VectorDivide),
                nameof(VectorEquality),
                nameof(VectorEquals),
                nameof(VectorLessThan),
                nameof(VectorLessThanOrEqual),
                nameof(VectorGreaterThan),
                nameof(VectorGreaterThanOrEqual),
                nameof(VectorComplement),
                nameof(VectorAbs),
                nameof(VectorMin),
                nameof(VectorMax),
                nameof(VectorDot),
                nameof(VectorSum),
                nameof(VectorSquareRoot),
                nameof(VectorCeiling),
                nameof(VectorFloor),
                nameof(VectorWiden),
                nameof(VectorNarrow),
                nameof(VectorConvertToSingle),
                nameof(VectorConvertToDouble),
                nameof(VectorConvertToInt32),
                nameof(VectorConvertToInt64)
            )] string name,
            [Values] bool cooperative
        ) => Utilities.Run(build, cooperative, name);
    }
}
