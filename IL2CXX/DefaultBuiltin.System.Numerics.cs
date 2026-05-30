using System.Numerics;
using System.Reflection;

namespace IL2CXX;

partial class DefaultBuiltin
{
    private static bool VectorIsSupported(Transpiler transpiler, Type type) =>
        type == transpiler.typeofByte ||
        type == transpiler.typeofSByte ||
        type == transpiler.typeofInt16 ||
        type == transpiler.typeofUInt16 ||
        type == transpiler.typeofInt32 ||
        type == transpiler.typeofUInt32 ||
        type == transpiler.typeofInt64 ||
        type == transpiler.typeofUInt64 ||
        type == transpiler.typeofIntPtr ||
        type == transpiler.typeofUIntPtr ||
        type == transpiler.typeofDouble ||
        type == transpiler.typeofSingle;
    private static (string body, int inline) VectorOfTIfSupported(Transpiler transpiler, Type[] types, Func<(string, int)> action) => VectorIsSupported(transpiler, types[0]) ? action() : ($"\t{transpiler.GenerateThrow("NotSupported")};\n", 1);
    private static string VectorOfTNative(Transpiler transpiler, Type type) => type == transpiler.typeofIntPtr ? "intptr_t" : type == transpiler.typeofUIntPtr ? "uintptr_t" : transpiler.EscapeForStacked(type);
    private static (string body, int inline) VectorOfTUnary(Type type, Transpiler transpiler, Type[] types, Func<string, string, string> action) => VectorOfTIfSupported(transpiler, types, () =>
    {
        var e = VectorOfTNative(transpiler, types[0]);
        return ($@"{'\t'}{transpiler.EscapeForStacked(type.MakeGenericType(types))} value;
{'\t'}auto p = reinterpret_cast<{e}*>(&value);
{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}for (size_t i = 0; i < sizeof(value) / sizeof({e}); ++i) {action("p[i]", "p0[i]")};
{'\t'}return value;
", 1);
    });
    private static (string body, int inline) VectorOfTBinary(Type type, Transpiler transpiler, Type[] types, Func<string, string, string, string> action) => VectorOfTIfSupported(transpiler, types, () =>
    {
        var e = VectorOfTNative(transpiler, types[0]);
        return ($@"{'\t'}{transpiler.EscapeForStacked(type.MakeGenericType(types))} value;
{'\t'}auto p = reinterpret_cast<{e}*>(&value);
{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}auto p1 = reinterpret_cast<{e}*>(&a_1);
{'\t'}for (size_t i = 0; i < sizeof(value) / sizeof({e}); ++i) {action("p[i]", "p0[i]", "p1[i]")};
{'\t'}return value;
", 1);
    });
    private static void SetupVector(Func<Type, Type> get, Type type, Builtin.Code code, Type typeofVectorOfT, string squareRoot)
    {
        code.For(
            type.GetProperty(nameof(Vector.IsHardwareAccelerated))!.GetMethod,
            transpiler => ("\treturn false;\n", 1)
        );
#if IL2CXX_OVERRIDE_NUMERICS
        var typeofVectorOfT0 = typeofVectorOfT.MakeGenericType(Type.MakeGenericMethodParameter(0));
        code.ForGeneric(
            type.GetMethod(nameof(Vector.Create), 1, [Type.MakeGenericMethodParameter(0)]),
            (transpiler, types) => VectorOfTIfSupported(transpiler, types, () =>
            {
                var e = VectorOfTNative(transpiler, types[0]);
                return ($@"{'\t'}{transpiler.EscapeForStacked(typeofVectorOfT.MakeGenericType(types))} value;
{'\t'}auto p = reinterpret_cast<{e}*>(&value);
{'\t'}for (size_t i = 0; i < sizeof(value) / sizeof({e}); ++i) p[i] = a_0;
{'\t'}return value;
", 1);
            })
        );
        void relation(string name, string @operator) => code.ForGeneric(
            type.GetMethod(name, 1, [typeofVectorOfT0, typeofVectorOfT0]),
            (transpiler, types) => VectorOfTBinary(typeofVectorOfT, transpiler, types, (value, x, y) => $"std::memset(&{value}, {x} {@operator} {y} ? 0xff : 0, sizeof({transpiler.EscapeForStacked(types[0])}))")
        );
        void all(string name, string @operator) => code.ForGeneric(
            type.GetMethod(name, 1, [typeofVectorOfT0, typeofVectorOfT0]),
            (transpiler, types) => VectorOfTIfSupported(transpiler, types, () =>
            {
                var e = VectorOfTNative(transpiler, types[0]);
                return ($@"{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}auto p1 = reinterpret_cast<{e}*>(&a_1);
{'\t'}for (size_t i = 0; i < sizeof(a_0) / sizeof({e}); ++i) if (!(p0[i] {@operator} p1[i])) return false;
{'\t'}return true;
", 1);
            })
        );
        void any(string name, string @operator) => code.ForGeneric(
            type.GetMethod(name, 1, [typeofVectorOfT0, typeofVectorOfT0]),
            (transpiler, types) => VectorOfTIfSupported(transpiler, types, () =>
            {
                var e = VectorOfTNative(transpiler, types[0]);
                return ($@"{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}auto p1 = reinterpret_cast<{e}*>(&a_1);
{'\t'}for (size_t i = 0; i < sizeof(a_0) / sizeof({e}); ++i) if (p0[i] {@operator} p1[i]) return true;
{'\t'}return false;
", 1);
            })
        );
        relation(nameof(Vector.Equals), "==");
        any(nameof(Vector.EqualsAny), "==");
        relation(nameof(Vector.LessThan), "<");
        all(nameof(Vector.LessThanAll), "<");
        any(nameof(Vector.LessThanAny), "<");
        relation(nameof(Vector.LessThanOrEqual), "<=");
        all(nameof(Vector.LessThanOrEqualAll), "<=");
        any(nameof(Vector.LessThanOrEqualAny), "<=");
        relation(nameof(Vector.GreaterThan), ">");
        all(nameof(Vector.GreaterThanAll), ">");
        any(nameof(Vector.GreaterThanAny), ">");
        relation(nameof(Vector.GreaterThanOrEqual), ">=");
        all(nameof(Vector.GreaterThanOrEqualAll), ">=");
        any(nameof(Vector.GreaterThanOrEqualAny), ">=");
        code.ForGeneric(
            type.GetMethod(nameof(Vector.Abs)),
            (transpiler, types) =>
            {
                var t = types[0];
                return
                    t == transpiler.typeofByte ||
                    t == transpiler.typeofUInt16 ||
                    t == transpiler.typeofUInt32 ||
                    t == transpiler.typeofUInt64 ||
                    t == transpiler.typeofUIntPtr
                        ? ("\treturn a_0;\n", 1)
                        : VectorOfTUnary(typeofVectorOfT, transpiler, types, (value, x) => $"{value} = std::abs({x})");
            }
        );
        void binary(string name) => code.ForGeneric(
            type.GetMethod(name, 1, [typeofVectorOfT0, typeofVectorOfT0]),
            (transpiler, types) => VectorOfTIfSupported(transpiler, types, () =>
            {
                var m = types[0].GetMethod(name) ?? throw new Exception();
                transpiler.Enqueue(m);
                return VectorOfTBinary(typeofVectorOfT, transpiler, types, (value, x, y) => $"{value} = {transpiler.Escape(m)}({x}, {y})");
            })
        );
        binary(nameof(Vector.Min));
        binary(nameof(Vector.Max));
        code.ForGeneric(
            type.GetMethod(nameof(Vector.Dot)),
            (transpiler, types) => VectorOfTIfSupported(transpiler, types, () =>
            {
                var e = VectorOfTNative(transpiler, types[0]);
                return ($@"{'\t'}{transpiler.EscapeForStacked(typeofVectorOfT.MakeGenericType(types))} value;
{'\t'}auto p = reinterpret_cast<{e}*>(&value);
{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}auto p1 = reinterpret_cast<{e}*>(&a_1);
{'\t'}for (size_t i = 0; i < sizeof(value) / sizeof({e}); ++i) p[i] = p0[i] * p1[i];
{'\t'}{e} x{{}};
{'\t'}for (size_t i = 0; i < sizeof(value) / sizeof({e}); ++i) x += p[i];
{'\t'}return x;
", 1);
            })
        );
        code.ForGeneric(
            type.GetMethod(nameof(Vector.Sum)),
            (transpiler, types) => VectorOfTIfSupported(transpiler, types, () =>
            {
                var e = VectorOfTNative(transpiler, types[0]);
                return ($@"{'\t'}{e} value{{}};
{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}for (size_t i = 0; i < sizeof(a_0) / sizeof({e}); ++i) value += p0[i];
{'\t'}return value;
", 1);
            })
        );
        code.ForGeneric(
            type.GetMethod(squareRoot),
            (transpiler, types) => VectorOfTUnary(typeofVectorOfT, transpiler, types, (value, x) =>
            {
                var t = types[0];
                return t == transpiler.typeofSingle || t == transpiler.typeofDouble ? $"{value} = std::sqrt({x})" : $"{value} = il2cxx::f_saturate<{VectorOfTNative(transpiler, t)}>(std::sqrt({x}))";
            })
        );
        void unary0(string name, Func<string, string, string> action)
        {
            void unary<T>(Func<string, string, string> action) => code.For(
                type.GetMethod(name, [typeofVectorOfT.MakeGenericType(get(typeof(T)))]),
                transpiler => VectorOfTUnary(typeofVectorOfT, transpiler, [get(typeof(T))], action)
            );
            unary<double>(action);
            unary<float>(action);
        }
        unary0(nameof(Vector.Ceiling), (value, x) => $"{value} = std::ceil({x})");
        unary0(nameof(Vector.Floor), (value, x) => $"{value} = std::floor({x})");
        var methods = type.GetMethods();
        var saturatings = new HashSet<string>
        {
            "ConvertToInt32",
            "ConvertToInt64",
            "ConvertToUInt32",
            "ConvertToUInt64"
        };
        foreach (var x in methods.Where(x => x.Name.StartsWith("ConvertTo"))) code.For(x, transpiler =>
        {
            var e = transpiler.EscapeForStacked(x.GetParameters()[0].ParameterType.GenericTypeArguments[0]);
            var t = transpiler.EscapeForStacked(x.ReturnType.GenericTypeArguments[0]);
            return ($@"{'\t'}{transpiler.EscapeForStacked(x.ReturnType)} value;
{'\t'}auto p = reinterpret_cast<{t}*>(&value);
{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}for (size_t i = 0; i < sizeof(a_0) / sizeof({e}); ++i) p[i] = {(saturatings.Contains(x.Name) ? $"il2cxx::f_saturate<{t}>(p0[i])" : "p0[i]")};
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
        void shift(string name, bool unsigned, string @operator)
        {
            foreach (var x in methods.Where(x => x.Name == name)) code.For(x, transpiler =>
            {
                var t = x.GetParameters()[0].ParameterType.GenericTypeArguments[0];
                var e =
                    t == get(typeof(nint)) ? unsigned ? "uintptr_t" : "intptr_t" :
                    t == get(typeof(nuint)) ? "uintptr_t" :
                    string.Format(unsigned ? "std::make_unsigned_t<{0}>" : "{0}", transpiler.EscapeForStacked(t));
                return ($@"{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}for (size_t i = 0; i < sizeof(a_0) / sizeof({e}); ++i) p0[i] {@operator}= a_1;
{'\t'}return a_0;
", 1);
            });
        }
        shift(nameof(Vector.ShiftLeft), false, "<<");
        shift(nameof(Vector.ShiftRightArithmetic), false, ">>");
        shift(nameof(Vector.ShiftRightLogical), true, ">>");
        void widen(MethodInfo method, string prefix) => code.For(method, transpiler =>
        {
            var e = transpiler.EscapeForStacked(method.ReturnType.GenericTypeArguments[0]);
            return ($@"{'\t'}{transpiler.EscapeForStacked(method.ReturnType)} value;
{'\t'}auto p = reinterpret_cast<{e}*>(&value);
{'\t'}auto p0 = reinterpret_cast<{transpiler.EscapeForStacked(method.GetParameters()[0].ParameterType.GenericTypeArguments[0])}*>(&a_0);
{'\t'}auto n = sizeof(value) / sizeof({e});
{'\t'}for (size_t i = 0; i < n; ++i) p[i] = p0[{prefix}i];
{'\t'}return value;
", 1);
        });
        foreach (var x in methods.Where(x => x.Name == nameof(Vector.WidenLower))) widen(x, string.Empty);
        foreach (var x in methods.Where(x => x.Name == nameof(Vector.WidenUpper))) widen(x, "n + ");
#endif
    }
#if IL2CXX_OVERRIDE_NUMERICS
    private static void SetupVectorOfT(Type type, Builtin.Code code)
    {
        code.ForGeneric(
            type.GetProperty(nameof(Vector<>.IsSupported))!.GetMethod,
            (transpiler, types) => ($"\treturn {(VectorIsSupported(transpiler, types[0]) ? "true" : "false")};\n", 1)
        );
        code.ForGeneric(
            type.GetProperty(nameof(Vector<>.Indices))!.GetMethod,
            (transpiler, types) => VectorOfTIfSupported(transpiler, types, () =>
            {
                var e = VectorOfTNative(transpiler, types[0]);
                return ($@"{'\t'}{transpiler.EscapeForStacked(type.MakeGenericType(types))} value;
{'\t'}auto p = reinterpret_cast<{e}*>(&value);
{'\t'}for (size_t i = 0; i < sizeof(value) / sizeof({e}); ++i) p[i] = i;
{'\t'}return value;
", 1);
            })
        );
        void additive(string name, string @operator) => code.ForGeneric(
            type.GetMethod(name),
            (transpiler, types) => VectorOfTBinary(type, transpiler, types, (value, x, y) => $"{value} = {x} {@operator} {y}")
        );
        additive("op_Addition", "+");
        additive("op_Subtraction", "-");
        code.ForGeneric(
            type.GetMethod("op_Multiply", [type, type]),
            (transpiler, types) => VectorOfTBinary(type, transpiler, types, (value, x, y) => $"{value} = {x} * {y}")
        );
        code.ForGeneric(
            type.GetMethod("op_Multiply", [type, type.GetGenericArguments()[0]]),
            (transpiler, types) => VectorOfTUnary(type, transpiler, types, (value, x) => $"{value} = {x} * a_1")
        );
        code.ForGeneric(
            type.GetMethod("op_Division", [type, type]),
            (transpiler, types) => VectorOfTIfSupported(transpiler, types, () =>
            {
                var t = types[0];
                var e = VectorOfTNative(transpiler, t);
                return ($@"{'\t'}auto p1 = reinterpret_cast<{e}*>(&a_1);
{(
    t == transpiler.typeofSingle || t == transpiler.typeofDouble ? string.Empty : $"\tfor (size_t i = 0; i < sizeof(a_1) / sizeof({e}); ++i) if (p1[i] == 0) [[unlikely]] {transpiler.GenerateThrow("DivideByZero")};\n"
)}{'\t'}{transpiler.EscapeForStacked(type.MakeGenericType(types))} value;
{'\t'}auto p = reinterpret_cast<{e}*>(&value);
{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}for (size_t i = 0; i < sizeof(value) / sizeof({e}); ++i) p[i] = p0[i] / p1[i];
{'\t'}return value;
", 1);
            })
        );
        code.ForGeneric(
            type.GetMethod("op_Division", [type, type.GetGenericArguments()[0]]),
            (transpiler, types) => VectorOfTIfSupported(transpiler, types, () =>
            {
                var t = types[0];
                var e = VectorOfTNative(transpiler, t);
                return ($@"{(
    t == transpiler.typeofSingle || t == transpiler.typeofDouble ? string.Empty : $"\tif (a_1 == 0) [[unlikely]] {transpiler.GenerateThrow("DivideByZero")};\n"
)}{'\t'}{transpiler.EscapeForStacked(type.MakeGenericType(types))} value;
{'\t'}auto p = reinterpret_cast<{e}*>(&value);
{'\t'}auto p0 = reinterpret_cast<{e}*>(&a_0);
{'\t'}for (size_t i = 0; i < sizeof(value) / sizeof({e}); ++i) p[i] = p0[i] / a_1;
{'\t'}return value;
", 1);
            })
        );
        void bitwise(string name, string @operator) => code.ForGeneric(
            type.GetMethod(name),
            (transpiler, types) => VectorOfTIfSupported(transpiler, types, () => ($@"{'\t'}{transpiler.EscapeForStacked(type.MakeGenericType(types))} value;
{'\t'}auto p = reinterpret_cast<uint8_t*>(&value);
{'\t'}auto p0 = reinterpret_cast<uint8_t*>(&a_0);
{'\t'}auto p1 = reinterpret_cast<uint8_t*>(&a_1);
{'\t'}for (size_t i = 0; i < sizeof(value); ++i) p[i] = p0[i] {@operator} p1[i];
{'\t'}return value;
", 1))
        );
        bitwise("op_BitwiseAnd", "&");
        bitwise("op_BitwiseOr", "|");
        bitwise("op_ExclusiveOr", "^");
        code.ForGeneric(
            type.GetMethod("op_OnesComplement"),
            (transpiler, types) => VectorOfTIfSupported(transpiler, types, () => ($@"{'\t'}{transpiler.EscapeForStacked(type.MakeGenericType(types))} value;
{'\t'}auto p = reinterpret_cast<uint8_t*>(&value);
{'\t'}auto p0 = reinterpret_cast<uint8_t*>(&a_0);
{'\t'}for (size_t i = 0; i < sizeof(value); ++i) p[i] = ~p0[i];
{'\t'}return value;
", 1))
        );
        (string, int) equality(Transpiler transpiler, Type[] types, string prefix, string and) => VectorOfTIfSupported(transpiler, types, () =>
        {
            var e = transpiler.EscapeForStacked(types[0]);
            return ($@"{'\t'}auto p0 = reinterpret_cast<{e}*>({prefix}a_0);
{'\t'}auto p1 = reinterpret_cast<{e}*>(&a_1);
{'\t'}for (size_t i = 0; i < sizeof(a_1) / sizeof({e}); ++i) if (p0[i] != p1[i]{and}) return false;
{'\t'}return true;
", 1);
        });
        code.ForGeneric(type.GetMethod("op_Equality"), (transpiler, types) => equality(transpiler, types, "&", string.Empty));
        code.ForGeneric(type.GetMethod(nameof(Equals), [type]), (transpiler, types) => equality(transpiler, types, string.Empty,
            types[0] == transpiler.typeofSingle || types[0] == transpiler.typeofDouble ? " && !(std::isnan(p0[i]) && std::isnan(p1[i]))" : string.Empty)
        );
    }
#endif
    private static Builtin SetupSystemNumerics(this Builtin @this, Func<Type, Type> get) => @this
    .For(get(typeof(BitOperations)), (type, code) =>
    {
        string native0(MethodInfo x)
        {
            var t = x.GetParameters()[0].ParameterType;
            return t == get(typeof(nint)) ? "static_cast<intptr_t>(a_0)" : t == get(typeof(nuint)) ? "static_cast<uintptr_t>(a_0)" : "a_0";
        }
        string unsigned0(MethodInfo x)
        {
            var t = x.GetParameters()[0].ParameterType;
            return t == get(typeof(nint)) || t == get(typeof(nuint)) ? "static_cast<uintptr_t>(a_0)" : "static_cast<std::make_unsigned_t<decltype(a_0)>>(a_0)";
        }
        var methods = type.GetMethods();
        foreach (var x in methods.Where(x => x.Name == nameof(BitOperations.IsPow2))) code.For(x, transpiler => ($"\treturn std::has_single_bit({unsigned0(x)});\n", 1));
        foreach (var x in methods.Where(x => x.Name == nameof(BitOperations.LeadingZeroCount))) code.For(x, transpiler => ($"\treturn std::countl_zero({native0(x)});\n", 1));
        foreach (var x in methods.Where(x => x.Name == nameof(BitOperations.Log2))) code.For(x, transpiler => ($"\treturn a_0 == 0 ? 0 : std::bit_width({native0(x)}) - 1;\n", 1));
        foreach (var x in methods.Where(x => x.Name == nameof(BitOperations.PopCount))) code.For(x, transpiler => ($"\treturn std::popcount({native0(x)});\n", 1));
        foreach (var x in methods.Where(x => x.Name == nameof(BitOperations.RotateLeft))) code.For(x, transpiler => ($"\treturn std::rotl({native0(x)}, a_1);\n", 1));
        foreach (var x in methods.Where(x => x.Name == nameof(BitOperations.RotateRight))) code.For(x, transpiler => ($"\treturn std::rotr({native0(x)}, a_1);\n", 1));
        foreach (var x in methods.Where(x => x.Name == nameof(BitOperations.RoundUpToPowerOf2))) code.For(x, transpiler => ($"\treturn std::bit_ceil({native0(x)});\n", 1));
        foreach (var x in methods.Where(x => x.Name == nameof(BitOperations.TrailingZeroCount))) code.For(x, transpiler => ($"\treturn std::countr_zero({unsigned0(x)});\n", 1));
    })
    .For(get(typeof(Vector)), (type, code) =>
    {
        SetupVector(get, type, code, get(typeof(Vector<>)), nameof(Vector.SquareRoot));
    })
#if IL2CXX_OVERRIDE_NUMERICS
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
            type.GetConstructor([type.GetGenericArguments()[0]]),
            (transpiler, types) => VectorOfTIfSupported(transpiler, types, () =>
            {
                var e = transpiler.EscapeForStacked(types[0]);
                return ($@"{'\t'}auto p = reinterpret_cast<{e}*>(a_0);
{'\t'}for (size_t i = 0; i < sizeof(*a_0) / sizeof({e}); ++i) p[i] = a_1;
", 1);
            })
        );
        SetupVectorOfT(type, code);
    })
    .For(get(typeof(Vector3)), (type, code) =>
    {
        void binary(string name) => code.For(
            type.GetMethod(name),
            transpiler =>
            {
                var m = transpiler.typeofSingle.GetMethod(name) ?? throw new Exception();
                transpiler.Enqueue(m);
                return ($@"{'\t'}{transpiler.EscapeForStacked(type)} value;
{'\t'}value.v_X = {transpiler.Escape(m)}(a_0.v_X, a_1.v_X);
{'\t'}value.v_Y = {transpiler.Escape(m)}(a_0.v_Y, a_1.v_Y);
{'\t'}value.v_Z = {transpiler.Escape(m)}(a_0.v_Z, a_1.v_Z);
{'\t'}return value;
", 1);
            }
        );
        binary(nameof(Vector3.Max));
        binary(nameof(Vector3.Min));
    })
#endif
    ;
}
