using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace IL2CXX
{
    public class Builtin : IBuiltin
    {
        private const BindingFlags declaredAndInstance = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public class Code
        {
            public Func<Transpiler, (string, bool)> Members;
            public Func<Transpiler, string> Initialize;
            public Dictionary<RuntimeMethodHandle, Func<Transpiler, string>> MethodToBody = new Dictionary<RuntimeMethodHandle, Func<Transpiler, string>>();
            public Dictionary<RuntimeMethodHandle, Func<Transpiler, Type[], string>> GenericMethodToBody = new Dictionary<RuntimeMethodHandle, Func<Transpiler, Type[], string>>();
            public Dictionary<RuntimeMethodHandle, Func<Transpiler, Type, string>> MethodTreeToBody = new Dictionary<RuntimeMethodHandle, Func<Transpiler, Type, string>>();

            public void For(MethodBase method, Func<Transpiler, string> body) => MethodToBody.Add(method.MethodHandle, body);
            public void ForGeneric(MethodBase method, Func<Transpiler, Type[], string> body) => GenericMethodToBody.Add(method.MethodHandle, body);
            public void ForTree(MethodInfo method, Func<Transpiler, Type, string> body) => MethodTreeToBody.Add(method.MethodHandle, body);
        }

        public Dictionary<Type, Code> TypeToCode = new Dictionary<Type, Code>();
        public Dictionary<string, Dictionary<string, Func<Transpiler, MethodBase, string>>> TypeNameToMethodNameToBody = new Dictionary<string, Dictionary<string, Func<Transpiler, MethodBase, string>>>();
        public Dictionary<string, Func<Transpiler, MethodBase, string>> MethodNameToBody = new Dictionary<string, Func<Transpiler, MethodBase, string>>();

        public Builtin For(Type type, Action<Type, Code> action)
        {
            var code = new Code();
            TypeToCode.Add(type, code);
            action(type, code);
            return this;
        }

        public (string members, bool managed) GetMembers(Transpiler transpiler, Type type) => TypeToCode.TryGetValue(type, out var code) ? code.Members?.Invoke(transpiler) ?? (null, false) : (null, false);
        public string GetInitialize(Transpiler transpiler, Type type) => TypeToCode.TryGetValue(type, out var code) ? code.Initialize?.Invoke(transpiler) : null;
        public string GetBody(Transpiler transpiler, MethodBase method)
        {
            var type = method.DeclaringType;
            var handle = method.MethodHandle;
            if (type.IsSubclassOf(typeof(Delegate)) && type != typeof(MulticastDelegate))
            {
                if (!TypeToCode.TryGetValue(type, out var @delegate))
                {
                    @delegate = new Code();
                    TypeToCode.Add(type, @delegate);
                }
                if (@delegate.Initialize == null)
                {
                    var invoke = (MethodInfo)type.GetMethod("Invoke");
                    transpiler.Enqueue(invoke);
                    @delegate.Initialize = _ =>
                    {
                        var @return = invoke.ReturnType;
                        var parameters = invoke.GetParameters().Select(x => x.ParameterType);
                        string call(string x) => $"{transpiler.Escape(invoke)}({string.Join(", ", parameters.Select((__, i) => $"a_{i + 1}").Prepend(x))});";
                        return $@"{'\t'}{'\t'}t__type_of<{transpiler.Escape(type)}>::v__instance.v__multicast_invoke = reinterpret_cast<void*>(static_cast<{transpiler.EscapeForVariable(@return)}(*)({string.Join(",", parameters.Prepend(typeof(MulticastDelegate)).Select(transpiler.EscapeForScoped))})>([]({string.Join(",", parameters.Prepend(typeof(MulticastDelegate)).Select((x, i) => $"\n\t\t\t{transpiler.EscapeForScoped(x)} a_{i}"))}
{'\t'}{'\t'}) -> {transpiler.EscapeForVariable(@return)}
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}auto xs = static_cast<{transpiler.Escape(typeof(object[]))}*>(a_0->v__5finvocationList)->f__data();
{'\t'}{'\t'}{'\t'}auto n = static_cast<intptr_t>(a_0->v__5finvocationCount) - 1;
{'\t'}{'\t'}{'\t'}for (intptr_t i = 0; i < n; ++i) {call("xs[i]")}
{'\t'}{'\t'}{'\t'}{(@return == typeof(void) ? string.Empty : "return ")}{call("xs[n]")}
{'\t'}{'\t'}}}));";
                    };
                }
                if (handle == type.GetConstructor(new[] { typeof(object), typeof(IntPtr) }).MethodHandle)
                {
                    return $@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}p->v__5ftarget = std::move(a_0);
{'\t'}p->v__5fmethodPtr = a_1;
{'\t'}return p;
";
                }
                else if (handle == type.GetMethod("Invoke").MethodHandle)
                {
                    var @return = ((MethodInfo)method).ReturnType;
                    var parameters = method.GetParameters().Select(x => x.ParameterType);
                    return $"\t{(@return == typeof(void) ? string.Empty : "return ")}reinterpret_cast<{(@return == typeof(void) ? "void" : transpiler.EscapeForScoped(@return))}(*)({string.Join(", ", parameters.Prepend(typeof(object)).Select(transpiler.EscapeForScoped))})>(a_0->v__5fmethodPtr.v__5fvalue)({string.Join(", ", parameters.Select((x, i) => transpiler.FormatMove(x, $"a_{i + 1}")).Prepend("a_0->v__5ftarget"))});\n";
                }
            }
            if (TypeToCode.TryGetValue(type, out var code))
            {
                if (code.MethodToBody.TryGetValue(handle, out var body0)) return body0(transpiler);
                if (method.IsGenericMethod && code.GenericMethodToBody.TryGetValue(((MethodInfo)method).GetGenericMethodDefinition().MethodHandle, out var body1)) return body1(transpiler, method.GetGenericArguments());
            }
            if (type.IsGenericType)
            {
                var gt = type.GetGenericTypeDefinition();
                if (TypeToCode.TryGetValue(gt, out var gc) && gc.GenericMethodToBody.TryGetValue(MethodBase.GetMethodFromHandle(handle, gt.TypeHandle).MethodHandle, out var body)) return body(transpiler, type.GetGenericArguments());
            }
            if (method is MethodInfo mi)
            {
                if (mi.IsGenericMethod) mi = mi.GetGenericMethodDefinition();
                var origin = mi.GetBaseDefinition().MethodHandle;
                for (var t = mi.DeclaringType;;)
                {
                    if (TypeToCode.TryGetValue(t, out var c) && c.MethodTreeToBody.TryGetValue(mi.MethodHandle, out var body)) return body(transpiler, type);
                    if (mi.MethodHandle == origin) break;
                    do
                    {
                        t = t.BaseType;
                        mi = t.GetMethods(declaredAndInstance).FirstOrDefault(x => x.GetBaseDefinition().MethodHandle == origin);
                    } while (mi == null);
                }
            }
            if (method.DeclaringType.FullName != null && TypeNameToMethodNameToBody.TryGetValue(method.DeclaringType.FullName, out var name2body) && name2body.TryGetValue(method.ToString(), out var body2)) return body2(transpiler, method);
            return MethodNameToBody.TryGetValue(method.ToString(), out var body3) ? body3(transpiler, method) : null;
        }
    }
    public static class DefaultBuiltin
    {
        private const BindingFlags declaredAndInstance = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static IEnumerable<MethodBase> GenericMethods(Type type) => type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(x => x.IsGenericMethodDefinition);
        private static Action<Type, Builtin.Code> ForIntPtr(string native) => (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}{'\t'}void* v__5fvalue;
{'\t'}{'\t'}void f__destruct()
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
{'\t'}{'\t'}void f__scan(t_scan a_scan)
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
{'\t'}{'\t'}t_value() = default;
{'\t'}{'\t'}t_value(void* a_value) : v__5fvalue(a_value)
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
{'\t'}{'\t'}t_value({native} a_value) : v__5fvalue(reinterpret_cast<void*>(a_value))
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
{'\t'}{'\t'}operator void*() const
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}return v__5fvalue;
{'\t'}{'\t'}}}
{'\t'}{'\t'}operator {native}() const
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}return reinterpret_cast<{native}>(v__5fvalue);
{'\t'}{'\t'}}}
", false);
        };

        public static Builtin Create() => new Builtin {
            TypeNameToMethodNameToBody = {
                ["System.SR"] = new Dictionary<string, Func<Transpiler, MethodBase, string>> {
                    ["System.String GetResourceString(System.String, System.String)"] = (transpiler, method) => "\treturn a_0;\n"
                }
            },
            MethodNameToBody = {
                ["System.String ToString(System.String, System.IFormatProvider)"] = (transpiler, method) => $"\treturn f__new_string(u\"{method.ReflectedType}\"sv);\n",
                ["Boolean TryFormat(System.Span`1[System.Char], Int32 ByRef, System.ReadOnlySpan`1[System.Char], System.IFormatProvider)"] = (transpiler, method) => $@"{'\t'}*a_2 = 0;
{'\t'}return false;
",
                ["Boolean System.ISpanFormattable.TryFormat(System.Span`1[System.Char], Int32 ByRef, System.ReadOnlySpan`1[System.Char], System.IFormatProvider)"] = (transpiler, method) => $@"{'\t'}*a_2 = 0;
{'\t'}return false;
",
                ["Boolean get_IsSupported()"] = (transpiler, method) => method.DeclaringType.Namespace == "System.Runtime.Intrinsics.X86" ? "\treturn false;\n" : null,
            }
        }
        .For(Type.GetType("System.Marvin"), (type, code) =>
        {
            code.For(
                type.GetMethod("GenerateSeed", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}union
{'\t'}{{
{'\t'}{'\t'}uint32_t v_32s[2];
{'\t'}{'\t'}uint64_t v_64;
{'\t'}}} seed;
{'\t'}std::seed_seq().generate(seed.v_32s, seed.v_32s + 2);
{'\t'}return seed.v_64;
"
            );
        })
        .For(typeof(object), (type, code) =>
        {
            code.For(
                type.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\treturn a_0->f_type()->f_clone(a_0);\n"
            );
            code.For(
                type.GetMethod(nameof(object.GetType)),
                transpiler => "\treturn a_0->f_type();\n"
            );
            code.For(
                type.GetMethod(nameof(object.ToString)),
                transpiler => "\treturn f__new_string(u\"object\"sv);\n"
            );
            code.ForTree(
                type.GetMethod(nameof(object.Equals), new[] { type }),
                (transpiler, actual) => "\treturn a_0 == a_1;\n"
            );
            /*code.ForTree(
                type.GetMethod(nameof(object.GetHashCode)),
                (transpiler, actual) => "\treturn reinterpret_cast<intptr_t>(a_0);\n"
            );*/
            code.ForTree(
                type.GetMethod(nameof(object.ToString)),
                (transpiler, actual) => $"\treturn f__new_string(u\"{actual}\"sv);\n"
            );
        })
        .For(typeof(ValueType), (type, code) =>
        {
            code.For(
                type.GetMethod("GetHashCodeOfPtr", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn reinterpret_cast<intptr_t>(a_0.v__5fvalue);\n"
            );
            code.For(
                type.GetMethod(nameof(object.Equals)),
                transpiler => "\treturn a_0 == a_1;\n"
            );
            code.For(
                type.GetMethod(nameof(object.GetHashCode), Type.EmptyTypes),
                transpiler =>
                {
                    var marvin = Type.GetType("System.Marvin");
                    var seed = marvin.GetProperty("DefaultSeed").GetMethod;
                    var compute = marvin.GetMethod("ComputeHash32", new[] { typeof(byte).MakeByRefType(), typeof(uint), typeof(uint), typeof(uint) });
                    transpiler.Enqueue(seed);
                    transpiler.Enqueue(compute);
                    return $@"{'\t'}if (!a_0) return 0;
{'\t'}auto seed = {transpiler.Escape(seed)}();
{'\t'}return {transpiler.Escape(compute)}(reinterpret_cast<uint8_t*>(static_cast<t_object*>(a_0)), a_0->f_type()->v__size, seed, seed >> 32);
";
                }
            );
            code.For(
                type.GetMethod(nameof(object.ToString)),
                transpiler => "\treturn f__new_string(u\"struct\"sv);\n"
            );
            code.ForTree(
                type.GetMethod(nameof(object.Equals)),
                (transpiler, actual) =>
                {
                    var identifier = transpiler.Escape(actual);
                    return $"\treturn a_1 && a_1->f_type()->f__is(&t__type_of<{identifier}>::v__instance) && std::memcmp(a_0, &static_cast<{identifier}*>(a_1)->v__value, sizeof({identifier})) == 0;\n";
                }
            );
        })
        .For(typeof(IntPtr), ForIntPtr("intptr_t"))
        .For(typeof(UIntPtr), ForIntPtr("uintptr_t"))
        .For(typeof(Type), (type, code) =>
        {
            code.For(
                type.TypeInitializer,
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod(nameof(Type.GetTypeFromHandle)),
                transpiler => "\treturn a_0.v__type;\n"
            );
            code.For(
                type.GetMethod("op_Equality"),
                transpiler => "\treturn a_0 == a_1;\n"
            );
            code.For(
                type.GetMethod("op_Inequality"),
                transpiler => "\treturn a_0 != a_1;\n"
            );
            code.For(
                type.GetProperty(nameof(Type.IsInterface)).GetMethod,
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
            code.For(
                type.GetMethod("IsRuntimeImplemented", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\treturn true;\n"
            );
        })
        .For(typeof(RuntimeFieldHandle), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}{'\t'}void* v__field;
{'\t'}{'\t'}void f__destruct()
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
{'\t'}{'\t'}void f__scan(t_scan a_scan)
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
", false);
            code.For(
                type.GetProperty(nameof(RuntimeFieldHandle.Value)).GetMethod,
                transpiler => $"\treturn {transpiler.EscapeForVariable(typeof(IntPtr))}{{a_0->v__field}};\n"
            );
        })
        .For(typeof(RuntimeTypeHandle), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}{'\t'}{transpiler.EscapeForVariable(typeof(Type))} v__type;
{'\t'}{'\t'}void f__destruct()
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}v__type.f__destruct();
{'\t'}{'\t'}}}
{'\t'}{'\t'}void f__scan(t_scan a_scan)
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}a_scan(v__type);
{'\t'}{'\t'}}}
", true);
            code.For(
                type.GetMethod(nameof(object.GetHashCode)),
                transpiler => "\treturn reinterpret_cast<intptr_t>(static_cast<t_object*>(a_0->v__type));\n"
            );
            code.For(
                type.GetProperty(nameof(RuntimeTypeHandle.Value)).GetMethod,
                transpiler => "\treturn static_cast<t_object*>(a_0->v__type);\n"
            );
        })
        .For(typeof(Array), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}struct t__bound
{'\t'}{{
{'\t'}{'\t'}size_t v_length;
{'\t'}{'\t'}int v_lower;
{'\t'}}};
{'\t'}size_t v__length;
{'\t'}t__bound* f__bounds()
{'\t'}{{
{'\t'}{'\t'}return reinterpret_cast<t__bound*>(this + 1);
{'\t'}}}
", true);
            code.For(
                type.GetMethod(nameof(Array.Copy), BindingFlags.Static | BindingFlags.NonPublic, null, new[] { type, typeof(int), type, typeof(int), typeof(int), typeof(bool) }, null),
                transpiler => $@"{'\t'}if (a_5) throw std::runtime_error(""NotImplementedException"");
{'\t'}if (a_0->f_type() == a_2->f_type()) {{
{'\t'}{'\t'}auto rank = a_0->f_type()->v__rank;
{'\t'}{'\t'}auto n = a_0->f_type()->v__element->v__size;
{'\t'}{'\t'}a_0->f_type()->v__element->f_copy(reinterpret_cast<char*>(a_0->f__bounds() + rank) + a_1 * n, a_4, reinterpret_cast<char*>(a_2->f__bounds() + rank) + a_3 * n);
{'\t'}}} else {{
{'\t'}{'\t'}throw std::runtime_error(""NotImplementedException"");
{'\t'}}}
"
            );
            code.For(
                type.GetMethod(nameof(Array.GetLength)),
                transpiler => $@"{'\t'}if (a_1 < 0 || a_1 >= a_0->f_type()->v__rank) throw std::out_of_range(""IndexOutOfRangeException"");
{'\t'}return a_0->f__bounds()[a_1].v_length;
"
            );
            code.For(
                type.GetMethod(nameof(Array.GetLowerBound)),
                transpiler => $@"{'\t'}if (a_1 < 0 || a_1 >= a_0->f_type()->v__rank) throw std::out_of_range(""IndexOutOfRangeException"");
{'\t'}return a_0->f__bounds()[a_1].v_lower;
"
            );
            code.For(
                type.GetMethod(nameof(Array.GetUpperBound)),
                transpiler => $@"{'\t'}if (a_1 < 0 || a_1 >= a_0->f_type()->v__rank) throw std::out_of_range(""IndexOutOfRangeException"");
{'\t'}auto& bound = a_0->f__bounds()[a_1];
{'\t'}return bound.v_lower + bound.v_length - 1;
"
            );
            code.For(
                type.GetProperty(nameof(Array.Length)).GetMethod,
                transpiler => "\treturn a_0->v__length;\n"
            );
            code.For(
                type.GetProperty(nameof(Array.Rank)).GetMethod,
                transpiler => "\treturn a_0->f_type()->v__rank;\n"
            );
            code.For(
                type.GetMethod("TrySZIndexOf", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn false;\n"
            );
            code.For(
                type.GetMethod("TrySZSort", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn false;\n"
            );
            code.For(
                type.GetMethod("InternalGetReference", declaredAndInstance),
                transpiler => $@"{'\t'}auto bounds = a_0->f__bounds();
{'\t'}size_t n = 0;
{'\t'}for (size_t i = 0; i < a_2; ++i) n = n * bounds[i].v_length + (a_3[i] - bounds[i].v_lower);
{'\t'}auto type = a_0->f_type();
{'\t'}auto p = reinterpret_cast<{transpiler.EscapeForVariable(typeof(TypedReference))}*>(a_1);
{'\t'}p->v_Type = {{type->v__element}};
{'\t'}p->v_Value = {{reinterpret_cast<char*>(bounds + type->v__rank) + n * type->v__element->v__size}};
"
            );
            code.For(
                type.GetMethod("GetRawArrayData", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\treturn reinterpret_cast<uint8_t*>(a_0->f__bounds() + a_0->f_type()->v__rank);\n"
            );
            code.For(
                type.GetMethod("GetRawArrayGeometry", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}*a_1 = a_0->v__length;
{'\t'}auto type = a_0->f_type();
{'\t'}*a_2 = type->v__element->v__size;
{'\t'}*a_3 = a_0->f__bounds()[0].v_lower;
{'\t'}*a_4 = type->v__element->v__managed;
{'\t'}return reinterpret_cast<uint8_t*>(a_0->f__bounds() + type->v__rank);
"
            );
        })
        .For(typeof(SZArrayHelper<>), (type, code) =>
        {
            code.ForGeneric(
                type.GetProperty(nameof(SZArrayHelper<object>.Count)).GetMethod,
                (transpiler, types) => "\treturn a_0->v__length;\n"
            );
            code.ForGeneric(
                type.GetProperty("Item").GetMethod,
                (transpiler, types) => $@"{'\t'}if (a_1 < 0 || a_1 >= a_0->v__length) throw std::out_of_range(""IndexOutOfRangeException"");
{'\t'}return a_0->f__data()[a_1];
"
            );
            code.ForGeneric(
                type.GetProperty("Item").SetMethod,
                (transpiler, types) => $@"{'\t'}if (a_1 < 0 || a_1 >= a_0->v__length) throw std::out_of_range(""IndexOutOfRangeException"");
{'\t'}a_0->f__data()[a_1] = std::move(a_2);
"
            );
            code.ForGeneric(
                type.GetMethod(nameof(SZArrayHelper<object>.CopyTo)),
                (transpiler, types) => $@"{'\t'}if (!a_1) throw std::runtime_error(""ArgumentNullException"");
{'\t'}if (a_2 < 0) throw std::out_of_range(""IndexOutOfRangeException"");
{'\t'}if (a_2 + a_0->v__length > a_1->v__length) throw std::out_of_range(""ArgumentException"");
{'\t'}std::copy_n(a_0->f__data(), a_0->v__length, a_1->f__data() + a_2);
"
            );
            code.ForGeneric(
                type.GetMethod(nameof(SZArrayHelper<object>.GetEnumerator)),
                (transpiler, types) => $@"{'\t'}auto p = t_object::f_allocate<{transpiler.Escape(typeof(SZArrayHelper<>).GetNestedType(nameof(SZArrayHelper<object>.Enumerator)).MakeGenericType(types))}>();
{'\t'}p->v_array = a_0;
{'\t'}p->v_index = -1;
{'\t'}return p;
"
            );
            code.ForGeneric(
                type.GetMethod(nameof(SZArrayHelper<object>.IndexOf)),
                (transpiler, types) =>
                {
                    var method = typeof(Array).GetMethod(nameof(Array.IndexOf), new[] { types[0].MakeArrayType(), types[0] });
                    transpiler.Enqueue(method);
                    return $"\treturn {transpiler.Escape(method)}(a_0, a_1);\n";
                }
            );
        })
        .For(typeof(Exception), (type, code) =>
        {
            code.ForTree(
                type.GetMethod(nameof(object.ToString)),
                (transpiler, actual) => $"\treturn f__new_string(u\"{actual}\"sv);\n"
            );
            code.For(
                type.GetMethod("GetMessageFromNativeResources", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { type.GetNestedType("ExceptionMessageKind", BindingFlags.NonPublic) }, null),
                transpiler => "\treturn f__new_string(u\"message from native resources\"sv);\n"
            );
            code.For(
                type.GetMethod("RestoreDispatchState", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        })
        .For(typeof(ExceptionDispatchInfo), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(ExceptionDispatchInfo.Capture)),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        })
        .For(typeof(GC), (type, code) =>
        {
            code.For(
                type.GetMethod("_Collect", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}if (!(a_1 & 2)) {{
{'\t'}{'\t'}f_engine()->f_tick();
{'\t'}}} else if (a_1 & 4) {{
{'\t'}{'\t'}f_engine()->f_wait();
{'\t'}{'\t'}f_engine()->f_wait();
{'\t'}{'\t'}if (uint32_t(a_0) > 1) {{
{'\t'}{'\t'}{'\t'}f_engine()->f_wait();
{'\t'}{'\t'}{'\t'}f_engine()->f_wait();
{'\t'}{'\t'}}}
{'\t'}}} else {{
{'\t'}{'\t'}f_engine()->f_collect();
{'\t'}}}
"
            );
            code.For(
                type.GetMethod(nameof(GC.SuppressFinalize)),
                transpiler => "\ta_0->f_type()->f_suppress_finalize(a_0);\n"
            );
            code.For(
                type.GetMethod(nameof(GC.ReRegisterForFinalize)),
                transpiler => "\ta_0->f_type()->f_register_finalize(a_0);\n"
            );
            code.For(
                type.GetMethod(nameof(GC.WaitForPendingFinalizers)),
                transpiler => "\tf_engine()->f_finalize();\n"
            );
            code.For(
                type.GetMethod("AllocateNewArray", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler =>
                {
                    var array = transpiler.Escape(typeof(Array));
                    return $@"{'\t'}auto type = static_cast<t__type*>(static_cast<void*>(a_0));
{'\t'}auto n = type->v__element->v__size * a_1;
{'\t'}{transpiler.EscapeForVariable(typeof(Array))} p = type->f__allocate(sizeof({array}) + sizeof({array}::t__bound) + n);
{'\t'}p->v__length = a_1;
{'\t'}p->f__bounds()[0] = {{size_t(a_1), 0}};
{'\t'}if (!a_2) std::fill_n(reinterpret_cast<char*>(p->f__bounds() + 1), n, '\0');
{'\t'}return p;
";
                }
            );
            code.For(
                type.GetMethod("GetMemoryInfo", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}auto load = f_engine()->f_load_count();
{'\t'}*a_0 = load * 2;
{'\t'}*a_1 = load * 8;
{'\t'}*a_2 = load;
{'\t'}*a_3 = load / 4;
{'\t'}*a_4 = {{load * 4}};
{'\t'}*a_5 = {{}};
"
            );
        })
        .For(typeof(GCHandle), (type, code) =>
        {
            code.For(
                type.GetMethod("InternalAlloc", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $"\treturn {transpiler.EscapeForVariable(typeof(IntPtr))}{{a_1 < 2 ? static_cast<t__handle*>(new t__weak_handle(std::move(a_0), a_1)) : new t__normal_handle(std::move(a_0))}};\n"
            );
            code.For(
                type.GetMethod("InternalFree", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\tdelete static_cast<t__handle*>(a_0.v__5fvalue);\n"
            );
            code.For(
                type.GetMethod("InternalGet", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn static_cast<t__handle*>(a_0.v__5fvalue)->f_target();\n"
            );
        })
        .For(Type.GetType("System.Runtime.CompilerServices.DependentHandle"), (type, code) =>
        {
            code.For(
                type.GetConstructor(new[] { typeof(object), typeof(object) }),
                transpiler => $"\treturn {transpiler.EscapeForVariable(type)}{{new t__dependent_handle(std::move(a_0), std::move(a_1))}};\n"
            );
            code.For(
                type.GetMethod("GetPrimary"),
                transpiler => "\treturn static_cast<t__dependent_handle*>(a_0->v__5fhandle.v__5fvalue)->f_target();\n"
            );
            code.For(
                type.GetMethod("GetPrimaryAndSecondary"),
                transpiler => $@"{'\t'}auto p = static_cast<t__dependent_handle*>(a_0->v__5fhandle.v__5fvalue);
{'\t'}auto primary = p->f_target();
{'\t'}*a_1 = primary ? p->v_secondary : nullptr;
{'\t'}return std::move(primary);
"
            );
            code.For(
                type.GetMethod("SetPrimary"),
                transpiler => "\tstatic_cast<t__dependent_handle*>(a_0->v__5fhandle.v__5fvalue)->f_target__(std::move(a_1));\n"
            );
            code.For(
                type.GetMethod("SetSecondary"),
                transpiler => $@"{'\t'}auto p = static_cast<t__dependent_handle*>(a_0->v__5fhandle.v__5fvalue);
{'\t'}if (auto primary = p->f_target()) p->v_secondary = a_1;
"
            );
            code.For(
                type.GetMethod("Free"),
                transpiler => $@"{'\t'}if (auto p = static_cast<t__dependent_handle*>(a_0->v__5fhandle.v__5fvalue)) {{
{'\t'}{'\t'}a_0->v__5fhandle.v__5fvalue = nullptr;
{'\t'}{'\t'}delete p;
{'\t'}}}
"
            );
        })
        .For(typeof(WeakReference), (type, code) =>
        {
            code.For(
                type.GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\ta_0->v_m_5fhandle = {new t__weak_handle(std::move(a_1), a_2)};\n"
            );
            code.For(
                type.GetMethod("Finalize", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\tdelete static_cast<t__weak_handle*>(a_0->v_m_5fhandle.v__5fvalue);\n"
            );
            code.For(
                type.GetProperty(nameof(WeakReference.IsAlive)).GetMethod,
                transpiler => "\treturn static_cast<t__weak_handle*>(a_0->v_m_5fhandle.v__5fvalue)->f_target();\n"
            );
            code.For(
                type.GetProperty(nameof(WeakReference.Target)).GetMethod,
                transpiler => "\treturn static_cast<t__weak_handle*>(a_0->v_m_5fhandle.v__5fvalue)->f_target();\n"
            );
            code.For(
                type.GetProperty(nameof(WeakReference.Target)).SetMethod,
                transpiler => "\tstatic_cast<t__weak_handle*>(a_0->v_m_5fhandle.v__5fvalue)->f_target__(std::move(a_1));\n"
            );
        })
        .For(typeof(WeakReference<>), (type, code) =>
        {
            code.ForGeneric(
                type.GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic),
                (transpiler, types) => "\ta_0->v_m_5fhandle = {new t__weak_handle(std::move(a_1), a_2)};\n"
            );
            code.ForGeneric(
                type.GetMethod("Finalize", BindingFlags.Instance | BindingFlags.NonPublic),
                (transpiler, types) => "\tdelete static_cast<t__weak_handle*>(a_0->v_m_5fhandle.v__5fvalue);\n"
            );
            code.ForGeneric(
                type.GetProperty("Target", BindingFlags.Instance | BindingFlags.NonPublic).GetMethod,
                (transpiler, types) => "\treturn static_cast<t__weak_handle*>(a_0->v_m_5fhandle.v__5fvalue)->f_target();\n"
            );
            code.ForGeneric(
                type.GetProperty("Target", BindingFlags.Instance | BindingFlags.NonPublic).SetMethod,
                (transpiler, types) => "\tstatic_cast<t__weak_handle*>(a_0->v_m_5fhandle.v__5fvalue)->f_target__(std::move(a_1));\n"
            );
        })
        .For(typeof(Delegate), (type, code) =>
        {
            code.For(
                type.GetMethod("InternalEqualTypes", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn a_0->f_type() == a_1->f_type();\n"
            );
            code.For(
                type.GetMethod("InternalAllocLike", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}auto type = a_0->f_type();
{'\t'}auto p = type->f__allocate(type->v__size);
{'\t'}std::fill_n(reinterpret_cast<char*>(static_cast<t_object*>(p) + 1), type->v__size - sizeof(t_object), '\0');
{'\t'}return p;
"
            );
            code.For(
                type.GetMethod("GetInvokeMethod", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
            code.For(
                type.GetMethod("GetMulticastInvoke", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\treturn a_0->f_type()->v__multicast_invoke;\n"
            );
            code.For(
                type.GetMethod("GetMethodImpl", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        })
        .For(typeof(MulticastDelegate), (type, code) =>
        {
            code.For(
                type.GetMethod("GetTarget", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
            code.For(
                type.GetMethod("GetMethodImpl", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        })
        .For(typeof(Interlocked), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(Interlocked.CompareExchange), new[] { typeof(int).MakeByRefType(), typeof(int), typeof(int) }),
                transpiler => $@"{'\t'}reinterpret_cast<std::atomic_int32_t*>(a_0)->compare_exchange_strong(a_2, a_1);
{'\t'}return a_2;
"
            );
            code.For(
                type.GetMethod(nameof(Interlocked.CompareExchange), new[] { typeof(long).MakeByRefType(), typeof(long), typeof(long) }),
                transpiler => $@"{'\t'}reinterpret_cast<std::atomic_int64_t*>(a_0)->compare_exchange_strong(a_2, a_1);
{'\t'}return a_2;
"
            );
            code.For(
                type.GetMethod(nameof(Interlocked.CompareExchange), new[] { typeof(IntPtr).MakeByRefType(), typeof(IntPtr), typeof(IntPtr) }),
                transpiler => $@"{'\t'}void* p = a_2;
{'\t'}reinterpret_cast<std::atomic<void*>&>(a_0->v__5fvalue).compare_exchange_strong(p, a_1);
{'\t'}return {transpiler.EscapeForVariable(typeof(IntPtr))}{{p}};
"
            );
            code.For(
                type.GetMethod(nameof(Interlocked.CompareExchange), new[] { typeof(object).MakeByRefType(), typeof(object), typeof(object) }),
                transpiler => $@"{'\t'}a_0->f_compare_exchange(a_2, std::move(a_1));
{'\t'}return a_2;
"
            );
            code.For(
                type.GetMethod(nameof(Interlocked.Exchange), new[] { typeof(int).MakeByRefType(), typeof(int) }),
                transpiler => "\treturn reinterpret_cast<std::atomic_int32_t*>(a_0)->exchange(a_1);\n"
            );
            code.For(
                type.GetMethod(nameof(Interlocked.Exchange), new[] { typeof(long).MakeByRefType(), typeof(long) }),
                transpiler => "\treturn reinterpret_cast<std::atomic_int64_t*>(a_0)->exchange(a_1);\n"
            );
            code.For(
                type.GetMethod(nameof(Interlocked.Exchange), new[] { typeof(IntPtr).MakeByRefType(), typeof(IntPtr) }),
                transpiler => $"\treturn {transpiler.EscapeForVariable(typeof(IntPtr))}{{reinterpret_cast<std::atomic<void*>&>(a_0->v__5fvalue).exchange(a_1)}};\n"
            );
            code.For(
                type.GetMethod(nameof(Interlocked.Exchange), new[] { typeof(object).MakeByRefType(), typeof(object) }),
                transpiler => "\treturn a_0->f_exchange(std::move(a_1));\n"
            );
            code.For(
                type.GetMethod("ExchangeAdd", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(int).MakeByRefType(), typeof(int) }, null),
                transpiler => "\treturn reinterpret_cast<std::atomic_int32_t*>(a_0)->fetch_add(a_1);\n"
            );
            code.For(
                type.GetMethod("ExchangeAdd", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(long).MakeByRefType(), typeof(long) }, null),
                transpiler => "\treturn reinterpret_cast<std::atomic_int64_t*>(a_0)->fetch_add(a_1);\n"
            );
            code.For(
                type.GetMethod(nameof(Interlocked.MemoryBarrier)),
                transpiler => "\tstd::atomic_thread_fence(std::memory_order_seq_cst);\n"
            );
        })
        .For(typeof(Thread), (type, code) =>
        {
            code.Members = transpiler => ($@"
{'\t'}static IL2CXX__PORTABLE__THREAD {transpiler.Escape(type)}* v__current;

{'\t'}static {transpiler.Escape(type)}* f__current()
{'\t'}{{
{'\t'}{'\t'}return v__current;
{'\t'}}}

{'\t'}{transpiler.EscapeForVariable(typeof(ExecutionContext))} v__5fexecutionContext;
{'\t'}{transpiler.EscapeForVariable(typeof(SynchronizationContext))} v__5fsynchronizationContext;
{'\t'}{transpiler.EscapeForVariable(typeof(Delegate))} v__5fdelegate;
{'\t'}{transpiler.EscapeForVariable(typeof(object))} v__5fthreadStartArg;
{'\t'}t_thread* v__internal;
{'\t'}{transpiler.EscapeForVariable(Type.GetType("System.Runtime.Serialization.DeserializationTracker"))} v__deserialization_tracker;

{'\t'}void f__scan(t_scan a_scan)
{'\t'}{{
{'\t'}{'\t'}t_System_2eObject::f__scan(a_scan);
{'\t'}{'\t'}a_scan(v__5fexecutionContext);
{'\t'}{'\t'}a_scan(v__5fsynchronizationContext);
{'\t'}{'\t'}a_scan(v__5fdelegate);
{'\t'}{'\t'}a_scan(v__5fthreadStartArg);
{'\t'}{'\t'}a_scan(v__deserialization_tracker);
{'\t'}}}
{'\t'}template<typename T>
{'\t'}void f__start(T a_do);
{'\t'}void f__start();
{'\t'}void f__join();
", true);
            code.For(
                type.GetConstructor(new[] { typeof(ThreadStart) }),
                transpiler => $@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}p->v__5fdelegate = std::move(a_0);
{'\t'}return p;
"
            );
            code.For(
                type.GetConstructor(new[] { typeof(ParameterizedThreadStart) }),
                transpiler => $@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}p->v__5fdelegate = std::move(a_0);
{'\t'}return p;
"
            );
            code.For(
                type.GetMethod(nameof(object.GetHashCode)),
                transpiler => "\treturn reinterpret_cast<intptr_t>(static_cast<t_object*>(a_0));\n"
            );
            code.For(
                type.GetMethod(nameof(Thread.Start), Type.EmptyTypes),
                transpiler => "\treturn a_0->f__start();\n"
            );
            code.For(
                type.GetMethod(nameof(Thread.Join), Type.EmptyTypes),
                transpiler => "\ta_0->f__join();\n"
            );
            code.For(
                type.GetMethod(nameof(Thread.Sleep), new[] { typeof(int) }),
                transpiler => "\tstd::this_thread::sleep_for(std::chrono::milliseconds(a_0));\n"
            );
            code.For(
                type.GetMethod(nameof(Thread.SpinWait)),
                transpiler => "\tfor (; a_0 > 0; --a_0) std::this_thread::yield();\n"
            );
            code.For(
                type.GetProperty(nameof(Thread.IsBackground)).SetMethod,
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
            code.For(
                type.GetProperty(nameof(Thread.IsThreadPoolThread)).GetMethod,
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
            code.For(
                type.GetProperty(nameof(Thread.ManagedThreadId)).GetMethod,
                transpiler => "\treturn reinerpret_cast<intptr_t>(static_cast<t_object*>(a_0));\n"
            );
            code.For(
                type.GetMethod("InternalFinalize", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod("GetCurrentProcessorNumber", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn sched_getcpu();\n"
            );
            code.For(
                type.GetMethod("GetCurrentThreadNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $"\treturn {transpiler.Escape(typeof(Thread))}::f__current();\n"
            );
            code.For(
                type.GetMethod("GetThreadDeserializationTracker", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}auto p = {transpiler.Escape(typeof(Thread))}::f__current();
{'\t'}if (!p->v__deserialization_tracker) p->v__deserialization_tracker = f__new_zerod<{transpiler.Escape(Type.GetType("System.Runtime.Serialization.DeserializationTracker"))}>();
{'\t'}return p->v__deserialization_tracker;
"
            );
        })
        .For(typeof(ThreadPool), (type, code) =>
        {
            code.For(
                type.GetMethod("NotifyWorkItemProgressNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        })
        .For(typeof(Monitor), (type, code) =>
        {
            code.For(
                type.GetMethod("ReliableEnter", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}t_epoch_region region;
{'\t'}a_0->f_extension()->v_mutex.lock();
{'\t'}*a_1 = true;
"
            );
            code.For(
                type.GetMethod("ReliableEnterTimeout", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}t_epoch_region region;
{'\t'}*a_2 = a_0->f_extension()->v_mutex.try_lock_for(std::chrono::milliseconds(a_1));
"
            );
            code.For(
                type.GetMethod(nameof(Monitor.Enter), new[] { typeof(object) }),
                transpiler => $@"{'\t'}t_epoch_region region;
{'\t'}a_0->f_extension()->v_mutex.lock();
"
            );
            code.For(
                type.GetMethod(nameof(Monitor.Exit)),
                transpiler => "\ta_0->f_extension()->v_mutex.unlock();\n"
            );
            code.For(
                type.GetMethod("IsEnteredNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}auto p = a_0->f_extension();
{'\t'}return p->v_mutex.locked();
"
            );
            code.For(
                type.GetMethod("ObjPulse", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\ta_0->f_extension()->v_condition.notify_one();\n"
            );
            code.For(
                type.GetMethod("ObjPulseAll", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\ta_0->f_extension()->v_condition.notify_all();\n"
            );
            code.For(
                type.GetMethod("ObjWait", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}if (a_0) throw std::runtime_error(""NotSupportedException"");
{'\t'}t_epoch_region region;
{'\t'}auto p = a_2->f_extension();
{'\t'}std::unique_lock<std::recursive_timed_mutex> lock(p->v_mutex, std::adopt_lock);
{'\t'}auto finally = f__finally([&]
{'\t'}{{
{'\t'}{'\t'}lock.release();
{'\t'}}});
{'\t'}if (a_1 != -1) return p->v_condition.wait_for(lock, std::chrono::milliseconds(a_1)) == std::cv_status::no_timeout;
{'\t'}p->v_condition.wait(lock);
{'\t'}return true;
"
            );
        })
        .For(typeof(Activator), (type, code) =>
        {
            var methods = GenericMethods(type);
            code.ForGeneric(
                methods.First(x => x.Name == nameof(Activator.CreateInstance)),
                (transpiler, types) => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
            code.For(
                type.GetMethod(nameof(Activator.CreateInstance), new[] { typeof(Type) }),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
            code.For(
                type.GetMethod(nameof(Activator.CreateInstance), new[] { typeof(Type), typeof(bool) }),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
            code.For(
                type.GetMethod(nameof(Activator.CreateInstance), new[] { typeof(Type), typeof(object[]) }),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
            code.For(
                type.GetMethod(nameof(Activator.CreateInstance), new[] { typeof(Type), typeof(BindingFlags), typeof(Binder), typeof(object[]), typeof(CultureInfo), typeof(object[]) }),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        })
        .For(typeof(string), (type, code) =>
        {
            code.Initialize = transpiler => $"\t\t{transpiler.Escape(typeof(string).GetField(nameof(string.Empty)))} = f__new_string(u\"\"sv);";
            code.For(
                type.GetMethod("FastAllocateString", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn f__new_string(a_0);\n"
            );
            code.For(
                type.GetMethod("wstrcpy", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\tstd::copy_n(a_1, a_2, a_0);\n"
            );
            code.For(
                type.GetConstructor(new[] { typeof(char*) }),
                transpiler => "\treturn f__new_string(std::u16string_view(a_0));\n"
            );
            code.For(
                type.GetConstructor(new[] { typeof(char*), typeof(int), typeof(int) }),
                transpiler => "\treturn f__new_string(std::u16string_view(a_0 + a_1, a_2));\n"
            );
            code.For(
                type.GetConstructor(new[] { typeof(char), typeof(int) }),
                transpiler => $@"{'\t'}auto p = f__new_string(a_1);
{'\t'}std::fill_n(&p->v__5ffirstChar, a_1, a_0);
{'\t'}return p;
"
            );
            code.For(
                type.GetConstructor(new[] { typeof(char[]) }),
                transpiler => "\treturn f__new_string(std::u16string_view(a_0->f__data(), a_0->v__length));\n"
            );
            code.For(
                type.GetConstructor(new[] { typeof(char[]), typeof(int), typeof(int) }),
                transpiler => "\treturn f__new_string(std::u16string_view(a_0->f__data() + a_1, a_2));\n"
            );
            code.For(
                type.GetConstructor(new[] { typeof(ReadOnlySpan<char>) }),
                transpiler => "\treturn f__new_string(std::u16string_view(static_cast<char16_t*>(a_0.v__5fpointer.v__5fvalue.v__5fvalue), a_0.v__5flength));\n"
            );
            code.For(
                type.GetConstructor(new[] { typeof(sbyte*) }),
                transpiler => "\treturn f__new_string(f__u16string(reinterpret_cast<char*>(a_0)));\n"
            );
            code.For(
                type.GetConstructor(new[] { typeof(sbyte*), typeof(int), typeof(int) }),
                transpiler => "\treturn f__new_string(f__u16string({reinterpret_cast<char*>(a_0) + a_1, a_2}));\n"
            );
            code.For(
                type.GetProperty(nameof(string.Length)).GetMethod,
                transpiler => "\treturn a_0->v__5fstringLength;\n"
            );
            code.For(
                type.GetProperty("Chars").GetMethod,
                transpiler => "\treturn (&a_0->v__5ffirstChar)[a_1];\n"
            );
            code.For(
                type.GetMethod(nameof(object.Equals), new[] { typeof(object) }),
                transpiler =>
                {
                    var method = typeof(string).GetMethod(nameof(string.Equals), new[] { typeof(string) });
                    transpiler.Enqueue(method);
                    return $"\treturn a_1 && a_1->f_type()->f__is(&t__type_of<{transpiler.Escape(typeof(string))}>::v__instance) && {transpiler.Escape(method)}(a_0, static_cast<{transpiler.EscapeForVariable(typeof(string))}>(a_1));\n";
                }
            );
            code.For(
                type.GetMethod(nameof(string.Equals), new[] { typeof(string), typeof(StringComparison) }),
                transpiler =>
                {
                    var method = typeof(string).GetMethod(nameof(string.Equals), new[] { typeof(string) });
                    transpiler.Enqueue(method);
                    return $"\treturn {transpiler.Escape(method)}(a_0, a_1);\n";
                }
            );
            code.For(
                type.GetMethod("IsAscii", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\treturn false;\n"
            );
            code.For(
                type.GetMethod("IsFastSort", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\treturn false;\n"
            );
            code.For(
                type.GetMethod(nameof(object.ToString), Type.EmptyTypes),
                transpiler => null
            );
            code.For(
                type.GetMethod(nameof(string.Join), new[] { typeof(string), typeof(object[]) }),
                transpiler => "\treturn f__new_string(u\"join\"sv);\n"
            );
            code.For(
                type.GetMethod(nameof(string.Split), new[] { typeof(char), typeof(StringSplitOptions) }),
                transpiler => $"\treturn f__new_array<{transpiler.Escape(typeof(string[]))}, {transpiler.EscapeForVariable(typeof(string))}>(0);\n"
            );
            code.For(
                type.GetMethod(nameof(string.ToLowerInvariant), Type.EmptyTypes),
                transpiler => $@"{'\t'}auto n = a_0->v__5fstringLength;
{'\t'}auto p = f__new_string(n);
{'\t'}auto q = &a_0->v__5ffirstChar;
{'\t'}std::transform(q, q + n, &p->v__5ffirstChar, [](auto x)
{'\t'}{{
{'\t'}{'\t'}return x >= u'A' && x <= u'Z' ? x + (u'a' - u'A') : x;
{'\t'}}});
{'\t'}return p;
"
            );
        })
        .For(typeof(StringBuilder), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(object.ToString), Type.EmptyTypes),
                transpiler => null
            );
        })
        .For(typeof(char), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(object.ToString), Type.EmptyTypes),
                transpiler => null
            );
        })
        .For(typeof(int), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(object.ToString), Type.EmptyTypes),
                transpiler => "\treturn f__new_string(f__u16string(std::to_string(*a_0)));\n"
            );
            code.For(
                type.GetMethod(nameof(object.ToString), new[] { typeof(string), typeof(IFormatProvider) }),
                transpiler => "\treturn f__new_string(f__u16string(std::to_string(*a_0)));\n"
            );
        })
        .For(typeof(float), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(object.GetHashCode)),
                transpiler => "\treturn reinterpret_cast<intptr_t>(a_0);\n"
            );
        })
        .For(typeof(double), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(object.GetHashCode)),
                transpiler => "\treturn reinterpret_cast<intptr_t>(a_0);\n"
            );
        })
        .For(typeof(Enum), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(object.Equals)),
                transpiler => "\treturn a_0 == a_1;\n"
            );
            code.For(
                type.GetMethod(nameof(object.GetHashCode)),
                transpiler => "\treturn reinterpret_cast<intptr_t>(static_cast<t_object*>(a_0));\n"
            );
            code.For(
                type.GetMethod(nameof(Enum.ToString), new[] { typeof(string) }),
                transpiler => "\treturn f__new_string(u\"enum\"sv);\n"
            );
            code.For(
                type.GetMethod("InternalCompareTo", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
            code.For(
                type.GetMethod("InternalGetCorElementType", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
            code.For(
                type.GetMethod("TryParse", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(Type), typeof(string), typeof(bool), typeof(bool), typeof(object).MakeByRefType() }, null),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        })
        .For(typeof(TypedReference), (type, code) =>
        {
            code.For(
                type.GetMethod("InternalToObject", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}auto p = static_cast<{transpiler.EscapeForVariable(type)}*>(a_0);
{'\t'}auto type = static_cast<t__type*>(p->v_Type.v__5fvalue);
{'\t'}auto value = p->v_Value.v__5fvalue;
{'\t'}if (type->f__is(&t__type_of<{transpiler.Escape(typeof(ValueType))}>::v__instance)) {{
{'\t'}{'\t'}auto p = type->f_allocate(sizeof(t_object) + type->v__size);
{'\t'}{'\t'}type->f_copy(reinterpret_cast<char*>(value), 1, reinterpret_cast<char*>(p + 1));
{'\t'}{'\t'}return p;
{'\t'}}} else {{
{'\t'}{'\t'}return *static_cast<{transpiler.EscapeForVariable(typeof(object))}*>(value);
{'\t'}}}
"
            );
        })
        .For(typeof(Environment), (type, code) =>
        {
            code.For(
                type.GetProperty(nameof(Environment.CurrentManagedThreadId)).GetMethod,
                transpiler => $"\treturn reinterpret_cast<intptr_t>({transpiler.Escape(typeof(Thread))}::f__current());\n"
            );
            code.For(
                type.GetProperty(nameof(Environment.HasShutdownStarted)).GetMethod,
                transpiler => "\treturn f_engine()->f_shuttingdown();\n"
            );
            string tick(Transpiler transpiler) => $@"{'\t'}timespec ts;
{'\t'}if (clock_gettime(CLOCK_MONOTONIC, &ts) != 0) throw std::runtime_error(""clock_gettime"");
{'\t'}return ts.tv_sec * 1000 + ts.tv_nsec / 1000000;
";
            code.For(type.GetProperty(nameof(Environment.TickCount)).GetMethod, tick);
            code.For(type.GetProperty("TickCount64").GetMethod, tick);
            code.For(
                type.GetMethod(nameof(Environment.FailFast), new[] { typeof(string) }),
                transpiler => $@"{'\t'}std::cerr << f__string({{&a_0->v__5ffirstChar, static_cast<size_t>(a_0->v__5fstringLength)}}) << std::endl;
{'\t'}std::abort();
"
            );
            code.For(
                type.GetMethod(nameof(Environment.FailFast), new[] { typeof(string), typeof(Exception) }),
                transpiler => $@"{'\t'}std::cerr << f__string({{&a_0->v__5ffirstChar, static_cast<size_t>(a_0->v__5fstringLength)}}) << std::endl;
{transpiler.GenerateVirtualCall(typeof(object).GetMethod(nameof(object.ToString)), "a_1", Enumerable.Empty<string>(), "auto s = ")}
{'\t'}std::cerr << f__string({{&s->v__5ffirstChar, static_cast<size_t>(s->v__5fstringLength)}}) << std::endl;
{'\t'}std::abort();
"
            );
            code.For(
                type.GetMethod(nameof(Environment.GetEnvironmentVariable), new[] { typeof(string) }),
                transpiler => $@"{'\t'}auto p = std::getenv(f__string({{&a_0->v__5ffirstChar, static_cast<size_t>(a_0->v__5fstringLength)}}).c_str());
{'\t'}return p ? f__new_string(f__u16string(p)) : nullptr;
"
            );
        })
        .For(Type.GetType("System.Runtime.CompilerServices.JitHelpers"), (type, code) =>
        {
            code.For(
                type.GetMethod("GetRawSzArrayData", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn reinterpret_cast<uint8_t*>(a_0->f__bounds() + 1);\n"
            );
        })
        .For(Type.GetType("System.Runtime.Versioning.CompatibilitySwitch"), (type, code) =>
        {
            code.For(
                type.GetMethod("GetValueInternal", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        })
        .For(typeof(RuntimeHelpers), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(RuntimeHelpers.GetHashCode), BindingFlags.Static | BindingFlags.Public),
                transpiler => "\treturn reinterpret_cast<intptr_t>(static_cast<t_object*>(a_0));\n"
            );
            code.For(
                type.GetMethod(nameof(RuntimeHelpers.InitializeArray)),
                transpiler => "\tstd::copy_n(static_cast<char*>(a_1.v__field), a_0->f_type()->v__element->v__size * a_0->v__length, reinterpret_cast<char*>(a_0->f__bounds() + a_0->f_type()->v__rank));\n"
            );
            code.ForGeneric(
                type.GetMethod("IsReferenceOrContainsReferences"),
                (transpiler, types) => $"\treturn {(transpiler.Define(types[0]).IsManaged ? "true" : "false")};\n"
            );
            code.For(
                type.GetProperty(nameof(RuntimeHelpers.OffsetToStringData)).GetMethod,
                transpiler => $"\treturn offsetof({transpiler.Escape(typeof(string))}, v__5ffirstChar);\n"
            );
            code.For(
                type.GetMethod("TryEnsureSufficientExecutionStack"),
                transpiler => "\treturn true;\n"
            );
            code.For(
                type.GetMethod("ObjectHasComponentSize", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}auto type = a_0->f_type();
{'\t'}return type == &t__type_of<{transpiler.Escape(typeof(string))}>::v__instance || type->f__is(&t__type_of<{transpiler.Escape(typeof(Array))}>::v__instance);
"
            );
        })
        .For(Type.GetType("System.ByReference`1"), (type, code) =>
        {
            code.ForGeneric(
                type.GetConstructor(new[] { type.GetGenericArguments()[0].MakeByRefType() }),
                (transpiler, types) => $"\treturn {transpiler.EscapeForVariable(type.MakeGenericType(types))}{{{{a_0}}}};\n"
            );
            code.ForGeneric(
                type.GetProperty("Value").GetMethod,
                (transpiler, types) => $"\treturn static_cast<{transpiler.EscapeForVariable(types[0].MakeByRefType())}>(a_0->v__5fvalue.v__5fvalue);\n"
            );
        })
        .For(typeof(DateTime), (type, code) =>
        {
            code.For(
                type.GetMethod("GetSystemTimeAsFileTime", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        })
        .For(typeof(Math), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(Math.Abs), new[] { typeof(double) }),
                transpiler => "\treturn std::abs(a_0);\n"
            );
            code.For(
                type.GetMethod(nameof(Math.Ceiling), new[] { typeof(double) }),
                transpiler => "\treturn std::ceil(a_0);\n"
            );
            code.For(
                type.GetMethod(nameof(Math.Floor), new[] { typeof(double) }),
                transpiler => "\treturn std::floor(a_0);\n"
            );
            code.For(
                type.GetMethod(nameof(Math.Log10)),
                transpiler => "\treturn std::log10(a_0);\n"
            );
            code.For(
                type.GetMethod("ModF", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn std::modf(a_0, a_1);\n"
            );
            code.For(
                type.GetMethod(nameof(Math.Pow)),
                transpiler => "\treturn std::pow(a_0, a_1);\n"
            );
            code.For(
                type.GetMethod(nameof(Math.Sqrt)),
                transpiler => "\treturn std::sqrt(a_0);\n"
            );
        })
        .For(typeof(Random), (type, code) =>
        {
            code.For(
                type.GetMethod("GenerateGlobalSeed", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}uint32_t seed;
{'\t'}std::seed_seq().generate(&seed, &seed + 1);
{'\t'}return seed;
"
            );
            code.For(
                type.GetMethod("GenerateSeed", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}uint32_t seed;
{'\t'}std::seed_seq().generate(&seed, &seed + 1);
{'\t'}return seed;
"
            );
        })
        .For(typeof(Buffer), (type, code) =>
        {
            var methods = GenericMethods(type);
            code.For(
                type.GetMethod(nameof(Buffer.BlockCopy)),
                transpiler => "\tf__copy(reinterpret_cast<char*>(a_0->f__bounds() + a_0->f_type()->v__rank) + a_1, a_4, reinterpret_cast<char*>(a_2->f__bounds() + a_2->f_type()->v__rank) + a_3);\n"
            );
            code.ForGeneric(
                methods.First(x => x.Name == "Memmove" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => "\tf__move(a_1, a_2, a_0);\n"
            );
        })
        .For(typeof(Marshal), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(Marshal.Copy), new[] { typeof(IntPtr), typeof(byte[]), typeof(int), typeof(int) }),
                transpiler => "\tstd::memcpy(a_1->f__data() + a_2, a_0, a_3);\n"
            );
            code.For(
                type.GetMethod(nameof(Marshal.GetLastWin32Error)),
                transpiler => "\treturn errno;\n"
            );
            code.For(
                type.GetMethod("SetLastWin32Error", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\terrno = a_0;\n"
            );
            code.For(
                type.GetMethod(nameof(Marshal.GetExceptionForHR), new[] { typeof(int), typeof(IntPtr) }),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
            code.For(
                type.GetMethod("IsPinnable", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn true;\n"
            );
            code.For(
                type.GetMethod("SizeOfHelper", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}if (a_1 && a_0->v__managed) throw std::runtime_error(""not marshalable"");
{'\t'}return a_0->v__size;
"
            );
        })
        .For(typeof(EqualityComparer<>), (type, code) =>
        {
            code.ForGeneric(type.TypeInitializer, (transpiler, types) =>
            {
                var gt = type.MakeGenericType(types);
                var concrete = gt.GetProperty(nameof(EqualityComparer<object>.Default)).GetValue(null).GetType();
                var identifier = transpiler.Escape(concrete);
                var constructor = concrete.GetConstructor(Type.EmptyTypes);
                transpiler.Enqueue(constructor);
                return $@"{'\t'}auto p = f__new_zerod<{identifier}>();
{'\t'}{transpiler.Escape(constructor)}(p);
{'\t'}t_static::v_instance->v_{transpiler.Escape(gt)}->v__3cDefault_3ek_5f_5fBackingField = std::move(p);
";
            });
        })
        .For(typeof(Console), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(Console.WriteLine), new[] { typeof(string) }),
                transpiler => $@"{'\t'}f_epoch_point();
{'\t'}if (auto p = static_cast<{transpiler.Escape(typeof(string))}*>(a_0)) std::cout << f__string({{&p->v__5ffirstChar, static_cast<size_t>(p->v__5fstringLength)}});
{'\t'}std::cout << std::endl;
"
            );
        })
        .For(Type.GetType("Internal.Runtime.CompilerServices.Unsafe"), (type, code) =>
        {
            var methods = GenericMethods(type);
            code.ForGeneric(
                methods.First(x => x.Name == "Add" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => "\treturn a_0 + a_1;\n"
            );
            code.ForGeneric(
                methods.First(x => x.Name == "AddByteOffset" && x.GetGenericArguments().Length == 1 && x.GetParameters()[1].ParameterType == typeof(ulong)),
                (transpiler, types) => $"\treturn reinterpret_cast<{transpiler.EscapeForVariable(types[0])}*>(reinterpret_cast<char*>(a_0) + a_1);\n"
            );
            code.ForGeneric(
                methods.First(x => x.Name == "AddByteOffset" && x.GetGenericArguments().Length == 1 && x.GetParameters()[1].ParameterType == typeof(IntPtr)),
                (transpiler, types) => $"\treturn reinterpret_cast<{transpiler.EscapeForVariable(types[0])}*>(reinterpret_cast<char*>(a_0) + reinterpret_cast<intptr_t>(a_1.v__5fvalue));\n"
            );
            code.ForGeneric(
                type.GetMethod("AreSame"),
                (transpiler, types) => "\treturn a_0 == a_1;\n"
            );
            code.ForGeneric(
                methods.First(x => x.Name == "As" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => "\treturn std::move(a_0);\n"
            );
            code.ForGeneric(
                methods.First(x => x.Name == "As" && x.GetGenericArguments().Length == 2),
                (transpiler, types) => $"\treturn reinterpret_cast<{transpiler.EscapeForVariable(types[1])}*>(a_0);\n"
            );
            code.ForGeneric(
                methods.First(x => x.Name == "AsPointer" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => "\treturn a_0;\n"
            );
            foreach (var m in methods.Where(x => x.Name == "ReadUnaligned" && x.GetGenericArguments().Length == 1))
                code.ForGeneric(m,
                    (transpiler, types) => $"\treturn *reinterpret_cast<{transpiler.EscapeForVariable(types[0])}*>(a_0);\n"
                );
            foreach (var m in methods.Where(x => x.Name == "WriteUnaligned" && x.GetGenericArguments().Length == 1))
                code.ForGeneric(m,
                    (transpiler, types) => $"\t*reinterpret_cast<{transpiler.EscapeForVariable(types[0])}*>(a_0) = a_1;\n"
                );
            code.ForGeneric(
                methods.First(x => x.Name == "SizeOf" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => $"\treturn sizeof({transpiler.EscapeForVariable(types[0])});\n"
            );
        })
        .For(typeof(ModuleHandle), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}{transpiler.EscapeForVariable(typeof(Module))} v_m_5fptr;
", true);
        })
        .For(typeof(LocalVariableInfo), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}{transpiler.EscapeForVariable(typeof(Type))} v_m_5ftype;
{'\t'}int32_t v_m_5fisPinned;
{'\t'}int32_t v_m_5flocalIndex;
", true);
        })
        .For(typeof(CustomAttributeData), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}t_slot_of<t_System_2eReflection_2eConstructorInfo> v_m_5fctor;
{'\t'}{transpiler.EscapeForVariable(typeof(Module))} v_m_5fscope;
{'\t'}//t_slot_of<t_System_2eReflection_2eMemberInfo_5b_5d> v_m_5fmembers;
{'\t'}//t_slot_of<t_System_2eReflection_2eCustomAttributeCtorParameter_5b_5d> v_m_5fctorParams;
{'\t'}//t_slot_of<t_System_2eReflection_2eCustomAttributeNamedParameter_5b_5d> v_m_5fnamedParams;
{'\t'}t_slot_of<t_System_2eObject> v_m_5ftypedCtorArgs;
{'\t'}t_slot_of<t_System_2eObject> v_m_5fnamedArgs;
", true);
            code.For(
                type.GetProperty(nameof(CustomAttributeData.ConstructorArguments)).GetMethod,
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
            code.For(
                type.GetProperty(nameof(CustomAttributeData.NamedArguments)).GetMethod,
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        })
        .For(typeof(ResourceManager), (type, code) =>
        {
            code.For(
                type.GetConstructor(new[] { typeof(string), typeof(Assembly) }),
                transpiler => $@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}return p;
"
            );
        })
        .For(typeof(ResourceReader), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}t_slot_of<t_System_2eIO_2eBinaryReader> v__5fstore;
{'\t'}t_slot_of<t_System_2eCollections_2eGeneric_2eDictionary_602_5bSystem_2eString_2cSystem_2eResources_2eResourceLocator_5d> v__5fresCache;
{'\t'}int64_t v__5fnameSectionOffset;
{'\t'}int64_t v__5fdataSectionOffset;
{'\t'}t_slot_of<t_System_2eInt32_5b_5d> v__5fnameHashes;
{'\t'}int32_t* v__5fnameHashesPtr;
{'\t'}t_slot_of<t_System_2eInt32_5b_5d> v__5fnamePositions;
{'\t'}int32_t* v__5fnamePositionsPtr;
{'\t'}{transpiler.EscapeForVariable(typeof(Type[]))} v__5ftypeTable;
{'\t'}t_slot_of<t_System_2eInt32_5b_5d> v__5ftypeNamePositions;
{'\t'}int32_t v__5fnumResources;
{'\t'}t_slot_of<t_System_2eIO_2eUnmanagedMemoryStream> v__5fums;
{'\t'}int32_t v__5fversion;
", true);
            code.For(
                type.GetMethod("_LoadObjectV1", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        })
        .For(Type.GetType("System.Resources.RuntimeResourceSet"), (type, code) =>
        {
            code.For(
                type.GetMethod("GetString", new[] { typeof(string), typeof(bool) }),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        })
        .For(Type.GetType("System.Collections.Generic.ArraySortHelper`1"), (type, code) =>
        {
            code.ForGeneric(
                type.GetMethod("CreateArraySortHelper", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => "\treturn {};\n"
            );
        })
        .For(Type.GetType("System.Collections.Generic.ComparerHelpers"), (type, code) =>
        {
            code.For(
                type.GetMethod("CreateDefaultComparer", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn {};\n"
            );
        })
        .For(typeof(MissingMemberException), (type, code) =>
        {
            code.For(
                type.GetMethod("FormatSignature", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        })
        .For(typeof(Debugger), (type, code) =>
        {
            code.For(
                type.GetProperty(nameof(Debugger.IsAttached)).GetMethod,
                transpiler => "\treturn false;\n"
            );
            code.For(
                type.GetMethod(nameof(Debugger.Log)),
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod(nameof(Debugger.NotifyOfCrossThreadDependency)),
                transpiler => string.Empty
            );
        })
        .For(Type.GetType("System.Diagnostics.Tracing.EventPipeEventDispatcher"), (type, code) =>
        {
            code.For(
                type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null),
                transpiler => $"\treturn f__new_zerod<{transpiler.Escape(type)}>();\n"
            );
            code.For(
                type.GetMethod("RemoveEventListener", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod("SendCommand", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => string.Empty
            );
        })
        .For(Type.GetType("System.Diagnostics.Tracing.FrameworkEventSource"), (type, code) =>
        {
            code.For(
                type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null),
                transpiler => $"\treturn f__new_zerod<{transpiler.Escape(type)}>();\n"
            );
            code.For(
                type.GetMethod("ThreadPoolEnqueueWorkObject"),
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod("ThreadTransferSendObj"),
                transpiler => string.Empty
            );
        })
        .For(Type.GetType("System.Buffers.ArrayPoolEventSource"), (type, code) =>
        {
            code.For(
                type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null),
                transpiler => $"\treturn f__new_zerod<{transpiler.Escape(type)}>();\n"
            );
            code.For(
                type.GetMethod("BufferAllocated", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod("BufferRented", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod("BufferReturned", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod("BufferTrimPoll", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod("BufferTrimmed", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => string.Empty
            );
        })
        .For(Type.GetType("System.CLRConfig"), (type, code) =>
        {
            code.For(
                type.GetMethod("GetBoolValue", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}*a_1 = false;
{'\t'}return false;
"
            );
        });
    }
}
