using System.Runtime.Intrinsics;

namespace IL2CXX.Tests;

[Parallelizable]
class IntrinsicsTests
{
    static int VectorConditionalSelect()
    {
        var x = Vector256.ConditionalSelect(Vector256.Create(Enumerable.Range(0, Vector256<int>.Count).Select(x => x % 2 == 0 ? ~0 : 0).ToArray()), Vector256.Create(1), Vector256.Create(2));
        return x == Vector256.Create(Enumerable.Range(0, Vector256<int>.Count).Select(x => x % 2 == 0 ? 1 : 2).ToArray()) ? 0 : 1;
    }
    static int VectorAdd()
    {
        var x = Vector256.Create(1f) + Vector256.Create(2f);
        return x[0] == 3f ? 0 : 1;
    }
    static int VectorSubtract()
    {
        var x = Vector256.Create(1f) - Vector256.Create(2f);
        return x[0] == -1f ? 0 : 1;
    }
    static int VectorMultiply()
    {
        var x = Vector256.Create(2f) * Vector256.Create(3f);
        return x[0] == 6f ? 0 : 1;
    }
    static int VectorMultiplyValue()
    {
        var x = Vector256.Create(2f) * 3f;
        return x[0] == 6f ? 0 : 1;
    }
    static int VectorDivide()
    {
        var x = Vector256.Create(3f) / Vector256.Create(2f);
        return x[0] == 1.5f ? 0 : 1;
    }
    static int VectorBitwiseAnd()
    {
        var x = Vector256.BitwiseAnd(Vector256.Create(3), Vector256.Create(6));
        return x[0] == 2 ? 0 : 1;
    }
    static int VectorBitwiseOr()
    {
        var x = Vector256.BitwiseOr(Vector256.Create(3), Vector256.Create(6));
        return x[0] == 7 ? 0 : 1;
    }
    static int VectorXor()
    {
        var x = Vector256.Xor(Vector256.Create(3), Vector256.Create(6));
        return x[0] == 5 ? 0 : 1;
    }
    static int VectorOnesComplement()
    {
        var x = Vector256.OnesComplement(Vector256.Create(0));
        return x[0] == ~0 ? 0 : 1;
    }
    static int VectorEquality()
    {
        var x = Vector256.Create(1f);
        var y = Vector256.Create(1f);
        return x == y ? 0 : 1;
    }
    static int VectorEquals()
    {
        var x = Vector256.Equals<float>(Vector256.Create(1f), Vector256.Create(1f));
        return float.IsNaN(x[0]) ? 0 : 1;
    }
    static int VectorLessThan()
    {
        var x = Vector256.LessThan<float>(Vector256.Create(1f), Vector256.Create(2f));
        return float.IsNaN(x[0]) ? 0 : 1;
    }
    static int VectorLessThanOrEqual()
    {
        var x = Vector256.LessThanOrEqual<float>(Vector256.Create(1f), Vector256.Create(1f));
        return float.IsNaN(x[0]) ? 0 : 1;
    }
    static int VectorGreaterThan()
    {
        var x = Vector256.GreaterThan<float>(Vector256.Create(2f), Vector256.Create(1f));
        return float.IsNaN(x[0]) ? 0 : 1;
    }
    static int VectorGreaterThanOrEqual()
    {
        var x = Vector256.GreaterThanOrEqual<float>(Vector256.Create(1f), Vector256.Create(1f));
        return float.IsNaN(x[0]) ? 0 : 1;
    }
    static int VectorComplement()
    {
        var x = ~Vector256.Create(0u);
        return x[0] == uint.MaxValue ? 0 : 1;
    }
    static int VectorAbs()
    {
        var x = Vector256.Abs(Vector256.Create(-1f));
        return x[0] == 1f ? 0 : 1;
    }
    static int VectorMin()
    {
        var x = Vector256.Min(Vector256.Create(1f), Vector256.Create(2f));
        return x[0] == 1f ? 0 : 1;
    }
    static int VectorMax()
    {
        var x = Vector256.Max(Vector256.Create(1f), Vector256.Create(2f));
        return x[0] == 2f ? 0 : 1;
    }
    static int VectorDot()
    {
        var x = Vector256.Dot(Vector256.Create(1f), Vector256.Create(2f));
        return x == 2f * Vector256<float>.Count ? 0 : 1;
    }
    static int VectorSum()
    {
        var x = Vector256.Sum(Vector256.Create(1f));
        return x == Vector256<float>.Count ? 0 : 1;
    }
    static int VectorSqrt()
    {
        var x = Vector256.Sqrt(Vector256.Create(2f));
        return x[0] == MathF.Sqrt(2f) ? 0 : 1;
    }
    static int VectorCeiling()
    {
        var x = Vector256.Ceiling(Vector256.Create(1.5f));
        return x[0] == 2f ? 0 : 1;
    }
    static int VectorFloor()
    {
        var x = Vector256.Floor(Vector256.Create(1.5f));
        return x[0] == 1f ? 0 : 1;
    }
    static int VectorWiden()
    {
        var (x, y) = Vector256.Widen(Vector256.Create(-1));
        return x[0] == -1 && y[0] == -1 ? 0 : 1;
    }
    static int VectorNarrow()
    {
        var x = Vector256.Narrow(Vector256.Create(-1), Vector256.Create(-1));
        return x[0] == (short)-1 ? 0 : 1;
    }
    static int VectorConvertToSingle()
    {
        var x = Vector256.ConvertToSingle(Vector256.Create(-1));
        return x[0] == -1f ? 0 : 1;
    }
    static int VectorConvertToDouble()
    {
        var x = Vector256.ConvertToDouble(Vector256.Create(-1L));
        return x[0] == -1.0 ? 0 : 1;
    }
    static int VectorConvertToInt32()
    {
        var x = Vector256.ConvertToInt32(Vector256.Create(-1f));
        return x[0] == -1 ? 0 : 1;
    }
    static int VectorConvertToInt64()
    {
        var x = Vector256.ConvertToInt64(Vector256.Create(-1.0));
        return x[0] == (long)-1 ? 0 : 1;
    }
    static int VectorExtractMostSignificantBits()
    {
        var x = Vector256.ExtractMostSignificantBits(Vector256.Create(Enumerable.Range(0, Vector256<int>.Count).Select(x => x % 2 == 0 ? 0 : ~0).ToArray()));
        return x == 0xaa ? 0 : 1;
    }
    static int VectorExtractMostSignificantBitsSingle()
    {
        var x = Vector256.ExtractMostSignificantBits(Vector256.Create(Enumerable.Range(0, Vector256<float>.Count).Select(x => x % 2 == 0 ? 0f : -1f).ToArray()));
        return x == 0xaa ? 0 : 1;
    }
    static int VectorShiftLeft()
    {
        var x = Vector256.ShiftLeft(Vector256.Create(~0), 1);
        return (uint)x[0] == uint.MaxValue - 1 ? 0 : 1;
    }
    static int VectorShiftRightArithmetic()
    {
        var x = Vector256.ShiftRightArithmetic(Vector256.Create(~3), 1);
        return x[0] == -2 ? 0 : 1;
    }
    static int VectorShiftRightLogical()
    {
        var x = Vector256.ShiftRightLogical(Vector256.Create(~0), 1);
        return x[0] == int.MaxValue ? 0 : 1;
    }

    static int Run(string[] arguments) => arguments[1] switch
    {
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
        nameof(VectorSqrt) => VectorSqrt(),
        nameof(VectorCeiling) => VectorCeiling(),
        nameof(VectorFloor) => VectorFloor(),
        nameof(VectorWiden) => VectorWiden(),
        nameof(VectorNarrow) => VectorNarrow(),
        nameof(VectorConvertToSingle) => VectorConvertToSingle(),
        nameof(VectorConvertToDouble) => VectorConvertToDouble(),
        nameof(VectorConvertToInt32) => VectorConvertToInt32(),
        nameof(VectorConvertToInt64) => VectorConvertToInt64(),
        nameof(VectorExtractMostSignificantBits) => VectorExtractMostSignificantBits(),
        nameof(VectorExtractMostSignificantBitsSingle) => VectorExtractMostSignificantBitsSingle(),
        nameof(VectorShiftLeft) => VectorShiftLeft(),
        nameof(VectorShiftRightArithmetic) => VectorShiftRightArithmetic(),
        nameof(VectorShiftRightLogical) => VectorShiftRightLogical(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run);
    [Test]
    public void Test(
        [Values(
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
            nameof(VectorSqrt),
            nameof(VectorCeiling),
            nameof(VectorFloor),
            nameof(VectorWiden),
            nameof(VectorNarrow),
            nameof(VectorConvertToSingle),
            nameof(VectorConvertToDouble),
            nameof(VectorConvertToInt32),
            nameof(VectorConvertToInt64),
            nameof(VectorExtractMostSignificantBits),
            nameof(VectorExtractMostSignificantBitsSingle),
            nameof(VectorShiftLeft),
            nameof(VectorShiftRightArithmetic),
            nameof(VectorShiftRightLogical)
        )] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
}
