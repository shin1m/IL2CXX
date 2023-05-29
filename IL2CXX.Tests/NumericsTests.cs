using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class NumericsTests
    {
        static int BitIsPow2()
        {
            if (!BitOperations.IsPow2(64)) return 1;
            if (!BitOperations.IsPow2((nint)64)) return 2;
            if (!BitOperations.IsPow2((nuint)64)) return 3;
            return 0;
        }
        static int BitRoundUpToPowerOf2()
        {
            if (BitOperations.RoundUpToPowerOf2(63) != 64) return 1;
            if (BitOperations.RoundUpToPowerOf2((nuint)63) != 64) return 2;
            return 0;
        }
        static int BitLeadingZeroCount()
        {
            if (BitOperations.LeadingZeroCount(256) != 23) return 1;
            if (BitOperations.LeadingZeroCount(~(nuint)0 >> 8) != 8) return 2;
            return 0;
        }
        static int BitLog2()
        {
            if (BitOperations.Log2(0) != 0) return 1;
            if (BitOperations.Log2(1) != 0) return 2;
            if (BitOperations.Log2(2) != 1) return 3;
            if (BitOperations.Log2(3) != 1) return 4;
            if (BitOperations.Log2(4) != 2) return 5;
            if (BitOperations.Log2(0x80000000) != 31) return 6;
            if (BitOperations.Log2((nuint)0x80000000) != 31) return 7;
            return 0;
        }
        static int BitPopCount()
        {
            if (BitOperations.PopCount(255) != 8) return 1;
            if (BitOperations.PopCount((nuint)255) != 8) return 2;
            return 0;
        }
        static int BitTrailingZeroCount()
        {
            if (BitOperations.TrailingZeroCount(256) != 8) return 1;
            if (BitOperations.TrailingZeroCount((nint)256) != 8) return 2;
            if (BitOperations.TrailingZeroCount((nuint)256) != 8) return 3;
            return 0;
        }
        static int BitRotateLeft()
        {
            if (BitOperations.RotateLeft(0x89abcdef, 8) != 0xabcdef89) return 1;
            if ((BitOperations.RotateLeft((nuint)0x89abcdef, 8) & 0xffffffff) != 0xabcdef00) return 2;
            return 0;
        }
        static int BitRotateRight()
        {
            if (BitOperations.RotateRight(0x89abcdef, 8) != 0xef89abcd) return 1;
            if ((BitOperations.RotateRight((nuint)0x89abcdef, 8) & 0xffffffff) != 0x0089abcd) return 2;
            return 0;
        }
        static int VectorConditionalSelect()
        {
            var x = Vector.ConditionalSelect(new Vector<int>(Enumerable.Range(0, Vector<int>.Count).Select(x => x % 2 == 0 ? ~0 : 0).ToArray()), new Vector<float>(1f), new Vector<float>(2f));
            return x == new Vector<float>(Enumerable.Range(0, Vector<float>.Count).Select(x => x % 2 == 0 ? 1f : 2f).ToArray()) ? 0 : 1;
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
            {
                var x = Vector.Min(new Vector<float>(1f), new Vector<float>(2f));
                if (x[0] != 1f) return 1;
            }
            {
                var x = Vector.Min(new Vector<nint>(1), new Vector<nint>(2));
                if (x[0] != 1) return 2;
            }
            return 0;
        }
        static int VectorMax()
        {
            {
                var x = Vector.Max(new Vector<float>(1f), new Vector<float>(2f));
                if (x[0] != 2f) return 1;
            }
            {
                var x = Vector.Max(new Vector<nint>(1), new Vector<nint>(2));
                if (x[0] != 2) return 2;
            }
            return 0;
        }
        static int VectorDot()
        {
            {
                var x = Vector.Dot(new Vector<float>(1f), new Vector<float>(2f));
                if (x != 2f * Vector<float>.Count) return 1;
            }
            {
                var x = Vector.Dot(new Vector<nint>(1), new Vector<nint>(2));
                if (x != 2 * Vector<nint>.Count) return 2;
            }
            return 0;
        }
        static int VectorSum()
        {
            {
                var x = Vector.Sum(new Vector<float>(1f));
                if (x != Vector<float>.Count) return 1;
            }
            {
                var x = Vector.Sum(new Vector<nint>(1));
                if (x != Vector<nint>.Count) return 2;
            }
            return 0;
        }
        static int VectorSquareRoot()
        {
            {
                var x = Vector.SquareRoot(new Vector<float>(2f));
                if (x[0] != MathF.Sqrt(2f)) return 1;
            }
            {
                var x = Vector.SquareRoot(new Vector<nint>(2));
                if (x[0] != (nint)MathF.Sqrt(2)) return 2;
            }
            return 0;
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
        static int VectorShiftLeft()
        {
            var x = Vector.ShiftLeft(new Vector<int>(~0), 1);
            return (uint)x[0] == uint.MaxValue - 1 ? 0 : 1;
        }
        static int VectorShiftRightArithmetic()
        {
            var x = Vector.ShiftRightArithmetic(new Vector<int>(~3), 1);
            return x[0] == -2 ? 0 : 1;
        }
        static int VectorShiftRightLogical()
        {
            {
                var x = Vector.ShiftRightLogical(new Vector<int>(~0), 1);
                if (x[0] != int.MaxValue) return 1;
            }
            {
                var x = Vector.ShiftRightLogical(new Vector<nint>(~(nint)0), 1);
                if (x[0] != nint.MaxValue) return 2;
            }
            {
                var x = Vector.ShiftRightLogical(new Vector<nuint>(~(nuint)0), 1);
                if (x[0] != nuint.MaxValue / 2) return 3;
            }
            return 0;
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
            nameof(VectorShiftLeft) => VectorShiftLeft(),
            nameof(VectorShiftRightArithmetic) => VectorShiftRightArithmetic(),
            nameof(VectorShiftRightLogical) => VectorShiftRightLogical(),
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
                nameof(VectorShiftLeft),
                nameof(VectorShiftRightArithmetic),
                nameof(VectorShiftRightLogical),
                nameof(Vector3Max),
                nameof(Vector3Min)
            )] string name,
            [Values] bool cooperative
        ) => Utilities.Run(build, cooperative, name);
    }
}
