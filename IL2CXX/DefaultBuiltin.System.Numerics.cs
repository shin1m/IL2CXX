using System;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static (string body, int inline) VectorOfTUnary(Type type, Transpiler transpiler, Type[] types, Func<string, string ,string> action)
        {
            var e = transpiler.EscapeForStacked(types[0]);
            return ($@"{'\t'}{transpiler.EscapeForStacked(type.MakeGenericType(types))} value;
{'\t'}auto p = reinterpret_cast<{e}*>(&value);
{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}for (size_t i = 0; i < sizeof(value) / sizeof({e}); ++i) {action("p[i]", "p0[i]")};
{'\t'}return value;
", 1);
        }
        private static (string body, int inline) VectorOfTBinary(Type type, Transpiler transpiler, Type[] types, Func<string, string, string, string> action)
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
            var typeofVectorOfT = get(typeof(Vector<>));
            var typeofVectorOfT0 = typeofVectorOfT.MakeGenericType(Type.MakeGenericMethodParameter(0));
            void relation(string name, string @operator) => code.ForGeneric(
                type.GetMethod(name, 1, new[] { typeofVectorOfT0, typeofVectorOfT0 }),
                (transpiler, types) => VectorOfTBinary(typeofVectorOfT, transpiler, types, (value, x, y) => $"std::memset(&{value}, {x} {@operator} {y} ? 0xff : 0, sizeof({transpiler.EscapeForStacked(types[0])}))")
            );
            relation(nameof(Vector.Equals), "==");
            relation(nameof(Vector.LessThan), "<");
            relation(nameof(Vector.LessThanOrEqual), "<=");
            relation(nameof(Vector.GreaterThan), ">");
            relation(nameof(Vector.GreaterThanOrEqual), ">=");
            void unary0(string name, Func<string, string ,string> action) => code.ForGeneric(
                type.GetMethod(name),
                (transpiler, types) => VectorOfTUnary(typeofVectorOfT, transpiler, types, action)
            );
            unary0(nameof(Vector.Abs), (value, x) => $"{value} = std::abs({x})");
            void binary(string name, Func<string, string, string, string> action) => code.ForGeneric(
                type.GetMethod(name, 1, new[] { typeofVectorOfT0, typeofVectorOfT0 }),
                (transpiler, types) => VectorOfTBinary(typeofVectorOfT, transpiler, types, action)
            );
            binary(nameof(Vector.Min), (value, x, y) => $"{value} = std::min({x}, {y})");
            binary(nameof(Vector.Max), (value, x, y) => $"{value} = std::max({x}, {y})");
            code.ForGeneric(
                type.GetMethod(nameof(Vector.Dot)),
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
                type.GetMethod(nameof(Vector.Sum)),
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
            unary0(nameof(Vector.SquareRoot), (value, x) => $"{value} = std::sqrt({x})");
            void unary1(string name, Func<string, string ,string> action)
            {
                void unary<T>(Func<string, string ,string> action) => code.For(
                    type.GetMethod(name, new[] { typeofVectorOfT.MakeGenericType(get(typeof(T))) }),
                    transpiler => VectorOfTUnary(typeofVectorOfT, transpiler, new[] { get(typeof(T)) }, action)
                );
                unary<double>(action);
                unary<float>(action);
            }
            unary1(nameof(Vector.Ceiling), (value, x) => $"{value} = std::ceil({x})");
            unary1(nameof(Vector.Floor), (value, x) => $"{value} = std::floor({x})");
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
            void binary(string name, string function) => code.For(
                type.GetMethod(name),
                transpiler => ($@"{'\t'}{transpiler.EscapeForStacked(type)} value;
{'\t'}value.v_X = std::{function}(a_0.v_X, a_1.v_X);
{'\t'}value.v_Y = std::{function}(a_0.v_Y, a_1.v_Y);
{'\t'}value.v_Z = std::{function}(a_0.v_Z, a_1.v_Z);
{'\t'}return value;
", 1)
            );
            binary(nameof(Vector3.Max), "max");
            binary(nameof(Vector3.Min), "min");
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
                type.GetConstructor(new[] { type.GetGenericArguments()[0] }),
                (transpiler, types) =>
                {
                    var e = transpiler.EscapeForStacked(types[0]);
                    return ($@"{'\t'}auto p = reinterpret_cast<{e}*>(a_0);
{'\t'}for (size_t i = 0; i < sizeof(*a_0) / sizeof({e}); ++i) p[i] = a_1;
", 1);
                }
            );
            void binary(string name, Func<string, string, string, string> action) => code.ForGeneric(
                type.GetMethod(name),
                (transpiler, types) => VectorOfTBinary(type, transpiler, types, action)
            );
            binary("op_Addition", (value, x, y) => $"{value} = {x} + {y}");
            binary("op_Subtraction", (value, x, y) => $"{value} = {x} - {y}");
            code.ForGeneric(
                type.GetMethod("op_Multiply", new[] { type, type }),
                (transpiler, types) => VectorOfTBinary(type, transpiler, types, (value, x, y) => $"{value} = {x} * {y}")
            );
            code.ForGeneric(
                type.GetMethod("op_Multiply", new[] { type, type.GetGenericArguments()[0] }),
                (transpiler, types) => VectorOfTUnary(type, transpiler, types, (value, x) => $"{value} = {x} * a_1")
            );
            binary("op_Division", (value, x, y) => $"{value} = {x} / {y}");
            void bitwise(string name, string @operator) => code.ForGeneric(
                type.GetMethod(name),
                (transpiler, types) => ($@"{'\t'}{transpiler.EscapeForStacked(type.MakeGenericType(types))} value;
{'\t'}auto p = reinterpret_cast<uint8_t*>(&value);
{'\t'}auto p0 = reinterpret_cast<uint8_t*>(&a_0);
{'\t'}auto p1 = reinterpret_cast<uint8_t*>(&a_1);
{'\t'}for (size_t i = 0; i < sizeof(value); ++i) p[i] = p0[i] {@operator} p1[i];
{'\t'}return value;
", 1)
            );
            bitwise("op_BitwiseAnd", "&");
            bitwise("op_BitwiseOr", "|");
            bitwise("op_ExclusiveOr", "^");
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
        });
    }
}
