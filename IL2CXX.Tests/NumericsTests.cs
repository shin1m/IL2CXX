using System;
using System.Numerics;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class NumericsTests
    {
        static int BitIsPow2() => BitOperations.IsPow2(64) ? 0 : 1;
        static int BitRoundUpToPowerOf2() => BitOperations.RoundUpToPowerOf2(63) == 64 ? 0 : 1;
        static int BitLeadingZeroCount() => BitOperations.LeadingZeroCount(256) == 23 ? 0 : 1;
        static int BitLog2()
        {
            if (BitOperations.Log2(0) != 0) return 1;
            if (BitOperations.Log2(1) != 0) return 2;
            if (BitOperations.Log2(2) != 1) return 3;
            if (BitOperations.Log2(3) != 1) return 4;
            if (BitOperations.Log2(4) != 2) return 5;
            if (BitOperations.Log2(0x80000000) != 31) return 6;
            return 0;
        }
        static int BitPopCount() => BitOperations.PopCount(255) == 8 ? 0 : 1;
        static int BitTrailingZeroCount() => BitOperations.TrailingZeroCount(256) == 8 ? 0 : 1;
        static int BitRotateLeft() => BitOperations.RotateLeft(0x89abcdef, 8) == 0xabcdef89 ? 0 : 1;
        static int BitRotateRight() => BitOperations.RotateRight(0x89abcdef, 8) == 0xef89abcd ? 0 : 1;
        static int VectorConditionalSelect()
        {
            var x = Vector.ConditionalSelect(new Vector<int>(new[] { ~0, 0, ~0, 0 }), new Vector<float>(1f), new Vector<float>(2f));
            return x == new Vector<float>(new[] { 1f, 2f, 1f, 2f }) ? 0 : 1;
        }
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
        static int VectorBitwiseAnd()
        {
            var x = Vector.BitwiseAnd(new Vector<int>(3), new Vector<int>(6));
            return x[0] == 2 ? 0 : 1;
        }
        static int VectorBitwiseOr()
        {
            var x = Vector.BitwiseOr(new Vector<int>(3), new Vector<int>(6));
            return x[0] == 7 ? 0 : 1;
        }
        static int VectorXor()
        {
            var x = Vector.Xor(new Vector<int>(3), new Vector<int>(6));
            return x[0] == 5 ? 0 : 1;
        }
        static int VectorOnesComplement()
        {
            var x = Vector.OnesComplement(new Vector<int>(0));
            return x[0] == ~0 ? 0 : 1;
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
        static int Vector3Max()
        {
            var x = Vector3.Max(new Vector3(-1f), new Vector3(1f));
            return x.X == 1f ? 0 : 1;
        }
        static int Vector3Min()
        {
            var x = Vector3.Min(new Vector3(-1f), new Vector3(1f));
            return x.X == -1f ? 0 : 1;
        }

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(BitIsPow2) => BitIsPow2(),
            nameof(BitRoundUpToPowerOf2) => BitRoundUpToPowerOf2(),
            nameof(BitLeadingZeroCount) => BitLeadingZeroCount(),
            nameof(BitLog2) => BitLog2(),
            nameof(BitPopCount) => BitPopCount(),
            nameof(BitTrailingZeroCount) => BitTrailingZeroCount(),
            nameof(BitRotateLeft) => BitRotateLeft(),
            nameof(BitRotateRight) => BitRotateRight(),
            nameof(VectorConditionalSelect) => VectorConditionalSelect(),
            nameof(VectorAdd) => VectorAdd(),
            nameof(VectorSubtract) => VectorSubtract(),
            nameof(VectorMultiply) => VectorMultiply(),
            nameof(VectorMultiplyValue) => VectorMultiplyValue(),
            nameof(VectorDivide) => VectorDivide(),
            nameof(VectorBitwiseAnd) => VectorBitwiseAnd(),
            nameof(VectorBitwiseOr) => VectorBitwiseOr(),
            nameof(VectorXor) => VectorXor(),
            nameof(VectorOnesComplement) => VectorOnesComplement(),
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
            nameof(Vector3Max) => Vector3Max(),
            nameof(Vector3Min) => Vector3Min(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run);
        [Test]
        public void Test(
            [Values(
                nameof(BitIsPow2),
                nameof(BitRoundUpToPowerOf2),
                nameof(BitLeadingZeroCount),
                nameof(BitLog2),
                nameof(BitPopCount),
                nameof(BitTrailingZeroCount),
                nameof(BitRotateLeft),
                nameof(BitRotateRight),
                nameof(VectorConditionalSelect),
                nameof(VectorAdd),
                nameof(VectorSubtract),
                nameof(VectorMultiply),
                nameof(VectorMultiplyValue),
                nameof(VectorDivide),
                nameof(VectorBitwiseAnd),
                nameof(VectorBitwiseOr),
                nameof(VectorXor),
                nameof(VectorOnesComplement),
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
                nameof(VectorConvertToInt64),
                nameof(Vector3Max),
                nameof(Vector3Min)
            )] string name,
            [Values] bool cooperative
        ) => Utilities.Run(build, cooperative, name);
    }
}
