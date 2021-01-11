using System.Numerics;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemNumerics(this Builtin @this) => @this
        .For(typeof(Vector<>), (type, code) =>
        {
            code.ForGeneric(
                type.GetMethod("ScalarAdd", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => ("\treturn a_0 + a_1;\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("ScalarSubtract", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => ("\treturn a_0 - a_1;\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("ScalarMultiply", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => ("\treturn a_0 * a_1;\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("ScalarDivide", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => ("\treturn a_0 / a_1;\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("ScalarEquals", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => ("\treturn a_0 == a_1;\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("ScalarLessThan", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => ("\treturn a_0 < a_1;\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("ScalarLessThanOrEqual", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => ("\treturn a_0 <= a_1;\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("ScalarGreaterThan", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => ("\treturn a_0 > a_1;\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("ScalarGreaterThanOrEqual", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => ("\treturn a_0 >= a_1;\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("GetOneValue", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => ("\treturn 1;\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("GetAllBitsSetValue", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => (types[0] == typeof(float) ? $@"{'\t'}union
{'\t'}{{
{'\t'}{'\t'}int32_t i = -1;
{'\t'}{'\t'}float f;
{'\t'}}};
{'\t'}return f;
" : types[0] == typeof(double) ? $@"{'\t'}union
{'\t'}{{
{'\t'}{'\t'}int64_t i = -1;
{'\t'}{'\t'}double f;
{'\t'}}};
{'\t'}return f;
" : "\treturn -1;\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("ScalarAbs", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => ("\treturn std::abs(a_0);\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("ScalarSqrt", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => ("\treturn std::sqrt(a_0);\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("ScalarCeiling", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => ("\treturn std::ceiling(a_0);\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("ScalarFloor", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => ("\treturn std::floor(a_0);\n", 1)
            );
        })
        .For(typeof(BitOperations), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(BitOperations.RotateLeft), new[] { typeof(uint), typeof(int) }),
                transpiler => ("\treturn (a_0 << (a_1 & 31)) | (a_0 >> ((32 - a_1) & 31));\n", 2)
            );
            code.For(
                type.GetMethod(nameof(BitOperations.RotateRight), new[] { typeof(uint), typeof(int) }),
                transpiler => ("\treturn (a_0 >> (a_1 & 31)) | (a_0 << ((32 - a_1) & 31));\n", 2)
            );
        });
    }
}
