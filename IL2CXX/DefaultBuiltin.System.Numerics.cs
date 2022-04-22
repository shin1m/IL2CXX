using System;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemNumerics(this Builtin @this, Func<Type, Type> get) => @this
        .For(get(typeof(BitOperations)), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(BitOperations.RotateLeft), new[] { get(typeof(uint)), get(typeof(int)) }),
                transpiler => ("\treturn (a_0 << (a_1 & 31)) | (a_0 >> ((32 - a_1) & 31));\n", 2)
            );
            code.For(
                type.GetMethod(nameof(BitOperations.RotateRight), new[] { get(typeof(uint)), get(typeof(int)) }),
                transpiler => ("\treturn (a_0 >> (a_1 & 31)) | (a_0 << ((32 - a_1) & 31));\n", 2)
            );
            /*var methods = type.GetMethods();
            foreach (var x in methods.Where(x => x.Name == nameof(BitOperations.IsPow2))) code.For(x, transpiler => ("\treturn std::has_single_bit(static_cast<std::make_unsigned_t<decltype(a_0)>>(a_0));\n", 1));
            foreach (var x in methods.Where(x => x.Name == nameof(BitOperations.LeadingZeroCount))) code.For(x, transpiler => ("\treturn std::countl_zero(a_0);\n", 1));
            foreach (var x in methods.Where(x => x.Name == nameof(BitOperations.Log2))) code.For(x, transpiler => ("\treturn a_0 == 0 ? 0 : std::bit_width(a_0) - 1;\n", 1));
            foreach (var x in methods.Where(x => x.Name == nameof(BitOperations.PopCount))) code.For(x, transpiler => ("\treturn std::popcount(a_0);\n", 1));
            foreach (var x in methods.Where(x => x.Name == nameof(BitOperations.RotateLeft))) code.For(x, transpiler => ("\treturn std::rotl(a_0, a_1);\n", 1));
            foreach (var x in methods.Where(x => x.Name == nameof(BitOperations.RotateRight))) code.For(x, transpiler => ("\treturn std::rotr(a_0, a_1);\n", 1));
            foreach (var x in methods.Where(x => x.Name == nameof(BitOperations.RoundUpToPowerOf2))) code.For(x, transpiler => ("\treturn std::bit_ceil(a_0);\n", 1));
            foreach (var x in methods.Where(x => x.Name == nameof(BitOperations.TrailingZeroCount))) code.For(x, transpiler => ("\treturn std::countr_zero(static_cast<std::make_unsigned_t<decltype(a_0)>>(a_0));\n", 1));*/
        })
        .For(get(typeof(Vector)), (type, code) =>
        {
            var methods = type.GetMethods();
            foreach (var x in methods.Where(x => x.Name.StartsWith("ConvertTo"))) code.For(x, transpiler =>
            {
                var e = transpiler.EscapeForStacked(x.GetParameters()[0].ParameterType.GenericTypeArguments[0]);
                return ($@"{'\t'}{transpiler.EscapeForStacked(x.ReturnType)} value;
{'\t'}auto p = reinterpret_cast<{transpiler.EscapeForStacked(x.ReturnType.GenericTypeArguments[0])}*>(&value);
{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}for (size_t i = 0; i < sizeof(a_0) / sizeof({e}); ++i) p[i] = p0[i];
{'\t'}return value;
", 1);
            });
            foreach (var x in methods.Where(x => x.Name == nameof(Vector.Narrow))) code.For(x, transpiler =>
            {
                var e = transpiler.EscapeForStacked(x.GetParameters()[0].ParameterType.GenericTypeArguments[0]);
                return ($@"{'\t'}{transpiler.EscapeForStacked(x.ReturnType)} value;
{'\t'}auto p = reinterpret_cast<{transpiler.EscapeForStacked(x.ReturnType.GenericTypeArguments[0])}*>(&value);
{'\t'}auto n = sizeof(a_0) / sizeof({e});
{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}for (size_t i = 0; i < n; ++i) p[i] = p0[i];
{'\t'}auto p1 = reinterpret_cast<{e}*>(&a_1);
{'\t'}for (size_t i = 0; i < n; ++i) p[n + i] = p1[i];
{'\t'}return value;
", 1);
            });
            foreach (var x in methods.Where(x => x.Name == nameof(Vector.Widen))) code.For(x, transpiler =>
            {
                var ps = x.GetParameters().Select(x => x.ParameterType).ToList();
                var e = transpiler.EscapeForStacked(ps[1].GetElementType().GenericTypeArguments[0]);
                return ($@"{'\t'}auto p0 = reinterpret_cast<{transpiler.EscapeForStacked(ps[0].GenericTypeArguments[0])}*>(&a_0);
{'\t'}auto n = sizeof(a_1) / sizeof({e});
{'\t'}auto p1 = reinterpret_cast<{e}*>(a_1);
{'\t'}for (size_t i = 0; i < n; ++i) p1[i] = p0[i];
{'\t'}auto p2 = reinterpret_cast<{e}*>(a_2);
{'\t'}for (size_t i = 0; i < n; ++i) p2[i] = p0[n + i];
", 1);
            });
        })
        .For(get(typeof(Vector3)), (type, code) =>
        {
            (string body, int inline) binary(Transpiler transpiler, string function) => ($@"{'\t'}{transpiler.EscapeForStacked(type)} value;
{'\t'}value.v_X = std::{function}(a_0.v_X, a_1.v_X);
{'\t'}value.v_Y = std::{function}(a_0.v_Y, a_1.v_Y);
{'\t'}value.v_Z = std::{function}(a_0.v_Z, a_1.v_Z);
{'\t'}return value;
", 1);
            code.For(
                type.GetMethod(nameof(Vector3.Max)),
                transpiler => binary(transpiler, "max")
            );
            code.For(
                type.GetMethod(nameof(Vector3.Min)),
                transpiler => binary(transpiler, "min")
            );
        })
        .For(get(typeof(Vector<>)), (type, code) =>
        {
            code.GenericMembers = (transpiler, types) => ($@"{'\t'}{'\t'}double _[{(transpiler.Is64Bit ? 4 : 2)}];
{'\t'}{'\t'}void f_destruct()
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
{'\t'}{'\t'}void f__scan(t_scan<t__type> a_scan)
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
", false, null);
            code.ForGeneric(
                type.GetMethod("GetOneValue", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => ("\treturn 1;\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("GetAllBitsSetValue", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) =>
                {
                    var e = transpiler.EscapeForStacked(types[0]);
                    return ($@"{'\t'}{e} value;
{'\t'}std::memset(&value, 0xff, sizeof({e}));
{'\t'}return value;
", 1);
                }
            );
            code.ForGeneric(
                type.GetConstructor(new[] { type.GetGenericArguments()[0] }),
                (transpiler, types) =>
                {
                    var e = transpiler.EscapeForStacked(types[0]);
                    return ($@"{'\t'}auto p = reinterpret_cast<{e}*>(a_0);
{'\t'}for (size_t i = 0; i < sizeof(*a_0) / sizeof({e}); ++i) p[i] = a_1;
", 1);
                }
            );
            code.ForGeneric(
                type.GetMethod("ConditionalSelect", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => ($@"{'\t'}{transpiler.EscapeForStacked(type.MakeGenericType(types))} value;
{'\t'}auto p = reinterpret_cast<uint8_t*>(&value);
{'\t'}auto p0 = reinterpret_cast<uint8_t*>(&a_0);
{'\t'}auto p1 = reinterpret_cast<uint8_t*>(&a_1);
{'\t'}auto p2 = reinterpret_cast<uint8_t*>(&a_2);
{'\t'}for (size_t i = 0; i < sizeof(value); ++i) p[i] = p0[i] & p1[i] | ~p0[i] & p2[i];
{'\t'}return value;
", 1)
            );
            (string body, int inline) unary(Transpiler transpiler, Type[] types, Func<string, string ,string> action)
            {
                var e = transpiler.EscapeForStacked(types[0]);
                return ($@"{'\t'}{transpiler.EscapeForStacked(type.MakeGenericType(types))} value;
{'\t'}auto p = reinterpret_cast<{e}*>(&value);
{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}for (size_t i = 0; i < sizeof(value) / sizeof({e}); ++i) {action("p[i]", "p0[i]")};
{'\t'}return value;
", 1);
            }
            (string body, int inline) binary(Transpiler transpiler, Type[] types, Func<string, string, string, string> action)
            {
                var e = transpiler.EscapeForStacked(types[0]);
                return ($@"{'\t'}{transpiler.EscapeForStacked(type.MakeGenericType(types))} value;
{'\t'}auto p = reinterpret_cast<{e}*>(&value);
{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}auto p1 = reinterpret_cast<{e}*>(&a_1);
{'\t'}for (size_t i = 0; i < sizeof(value) / sizeof({e}); ++i) {action("p[i]", "p0[i]", "p1[i]")};
{'\t'}return value;
", 1);
            }
            code.ForGeneric(
                type.GetMethod("op_Addition"),
                (transpiler, types) => binary(transpiler, types, (value, x, y) => $"{value} = {x} + {y}")
            );
            code.ForGeneric(
                type.GetMethod("op_Subtraction"),
                (transpiler, types) => binary(transpiler, types, (value, x, y) => $"{value} = {x} - {y}")
            );
            code.ForGeneric(
                type.GetMethod("op_Multiply", new[] { type, type }),
                (transpiler, types) => binary(transpiler, types, (value, x, y) => $"{value} = {x} * {y}")
            );
            code.ForGeneric(
                type.GetMethod("op_Multiply", new[] { type, type.GetGenericArguments()[0] }),
                (transpiler, types) => unary(transpiler, types, (value, x) => $"{value} = {x} * a_1")
            );
            code.ForGeneric(
                type.GetMethod("op_Division"),
                (transpiler, types) => binary(transpiler, types, (value, x, y) => $"{value} = {x} / {y}")
            );
            (string body, int inline) bitwise(Transpiler transpiler, Type[] types, string @operator) => ($@"{'\t'}{transpiler.EscapeForStacked(type.MakeGenericType(types))} value;
{'\t'}auto p = reinterpret_cast<uint8_t*>(&value);
{'\t'}auto p0 = reinterpret_cast<uint8_t*>(&a_0);
{'\t'}auto p1 = reinterpret_cast<uint8_t*>(&a_1);
{'\t'}for (size_t i = 0; i < sizeof(value); ++i) p[i] = p0[i] {@operator} p1[i];
{'\t'}return value;
", 1);
            code.ForGeneric(
                type.GetMethod("op_BitwiseAnd"),
                (transpiler, types) => bitwise(transpiler, types, "&")
            );
            code.ForGeneric(
                type.GetMethod("op_BitwiseOr"),
                (transpiler, types) => bitwise(transpiler, types, "|")
            );
            code.ForGeneric(
                type.GetMethod("op_ExclusiveOr"),
                (transpiler, types) => bitwise(transpiler, types, "^")
            );
            code.ForGeneric(
                type.GetMethod("op_OnesComplement"),
                (transpiler, types) => ($@"{'\t'}{transpiler.EscapeForStacked(type.MakeGenericType(types))} value;
{'\t'}auto p = reinterpret_cast<uint8_t*>(&value);
{'\t'}auto p0 = reinterpret_cast<uint8_t*>(&a_0);
{'\t'}for (size_t i = 0; i < sizeof(value); ++i) p[i] = ~p0[i];
{'\t'}return value;
", 1)
            );
            code.ForGeneric(
                type.GetMethod("op_Equality"),
                (transpiler, types) =>
                {
                    var e = transpiler.EscapeForStacked(types[0]);
                    return ($@"{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}auto p1 = reinterpret_cast<{e}*>(&a_1);
{'\t'}for (size_t i = 0; i < sizeof(a_0) / sizeof({e}); ++i) if (p0[i] != p1[i]) return false;
{'\t'}return true;
", 1);
                }
            );
            (string body, int inline) relation(Transpiler transpiler, Type[] types, string @operator) => binary(transpiler, types, (value, x, y) => $"std::memset(&{value}, {x} {@operator} {y} ? 0xff : 0, sizeof({transpiler.EscapeForStacked(types[0])}))");
            code.ForGeneric(
                type.GetMethod("Equals", BindingFlags.Static | BindingFlags.NonPublic, new[] { type, type }),
                (transpiler, types) => relation(transpiler, types, "==")
            );
            code.ForGeneric(
                type.GetMethod("LessThan", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => relation(transpiler, types, "<")
            );
            code.ForGeneric(
                type.GetMethod("LessThanOrEqual", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => relation(transpiler, types, "<=")
            );
            code.ForGeneric(
                type.GetMethod("GreaterThan", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => relation(transpiler, types, ">")
            );
            code.ForGeneric(
                type.GetMethod("GreaterThanOrEqual", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => relation(transpiler, types, ">=")
            );
            code.ForGeneric(
                type.GetMethod("Abs", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => unary(transpiler, types, (value, x) => $"{value} = std::abs({x})")
            );
            code.ForGeneric(
                type.GetMethod("Min", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => binary(transpiler, types, (value, x, y) => $"{value} = std::min({x}, {y})")
            );
            code.ForGeneric(
                type.GetMethod("Max", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => binary(transpiler, types, (value, x, y) => $"{value} = std::max({x}, {y})")
            );
            code.ForGeneric(
                type.GetMethod("Dot", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) =>
                {
                    var e = transpiler.EscapeForStacked(types[0]);
                    return ($@"{'\t'}{e} value{{}};
{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}auto p1 = reinterpret_cast<{e}*>(&a_1);
{'\t'}for (size_t i = 0; i < sizeof(a_0) / sizeof({e}); ++i) value += p0[i] * p1[i];
{'\t'}return value;
", 1);
                }
            );
            code.ForGeneric(
                type.GetMethod("Sum", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) =>
                {
                    var e = transpiler.EscapeForStacked(types[0]);
                    return ($@"{'\t'}{e} value{{}};
{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}for (size_t i = 0; i < sizeof(a_0) / sizeof({e}); ++i) value += p0[i];
{'\t'}return value;
", 1);
                }
            );
            code.ForGeneric(
                type.GetMethod("SquareRoot", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => unary(transpiler, types, (value, x) => $"{value} = std::sqrt({x})")
            );
            code.ForGeneric(
                type.GetMethod("Ceiling", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => unary(transpiler, types, (value, x) => $"{value} = std::ceil({x})")
            );
            code.ForGeneric(
                type.GetMethod("Floor", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => unary(transpiler, types, (value, x) => $"{value} = std::floor({x})")
            );
        });
    }
}
