using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace IL2CXX
{
    public class Builtin : IBuiltin
    {
        private const BindingFlags declaredAndInstance = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public class Code
        {
            public Func<Transpiler, string> Fields;
            public Func<Transpiler, string> Initialize;
            public Dictionary<RuntimeMethodHandle, Func<Transpiler, string>> MethodToBody = new Dictionary<RuntimeMethodHandle, Func<Transpiler, string>>();
            public Dictionary<RuntimeMethodHandle, Func<Transpiler, Type[], string>> GenericMethodToBody = new Dictionary<RuntimeMethodHandle, Func<Transpiler, Type[], string>>();
            public Dictionary<RuntimeMethodHandle, Func<Transpiler, Type, string>> MethodTreeToBody = new Dictionary<RuntimeMethodHandle, Func<Transpiler, Type, string>>();

            public void For(MethodBase method, Func<Transpiler, string> body) => MethodToBody.Add(method.MethodHandle, body);
            public void ForGeneric(MethodBase method, Func<Transpiler, Type[], string> body) => GenericMethodToBody.Add(method.MethodHandle, body);
            public void ForTree(MethodInfo method, Func<Transpiler, Type, string> body) => MethodTreeToBody.Add(method.MethodHandle, body);
        }

        public Dictionary<Type, Code> TypeToCode = new Dictionary<Type, Code>();
        public Dictionary<string, Func<Transpiler, MethodBase, string>> MethodNameToBody = new Dictionary<string, Func<Transpiler, MethodBase, string>>();

        public Builtin For(Type type, Action<Type, Code> action)
        {
            var code = new Code();
            TypeToCode.Add(type, code);
            action(type, code);
            return this;
        }

        public string GetFields(Transpiler transpiler, Type type) => TypeToCode.TryGetValue(type, out var code) ? code.Fields?.Invoke(transpiler) : null;
        public string GetInitialize(Transpiler transpiler, Type type) => TypeToCode.TryGetValue(type, out var code) ? code.Initialize?.Invoke(transpiler) : null;
        public string GetBody(Transpiler transpiler, MethodBase method)
        {
            var type = method.DeclaringType;
            var handle = method.MethodHandle;
            if (type.IsSubclassOf(typeof(Delegate)) && type != typeof(MulticastDelegate))
            {
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
                    return $"\t{(@return == typeof(void) ? string.Empty : "return ")}reinterpret_cast<{(@return == typeof(void) ? "void" : transpiler.EscapeForScoped(@return))}(*)({string.Join(", ", parameters.Select(x => transpiler.EscapeForScoped(x)).Prepend(transpiler.EscapeForScoped(typeof(object))))})>(a_0->v__5fmethodPtr.v__5fvalue)({string.Join(", ", parameters.Select((x, i) => transpiler.FormatMove(x, $"a_{i + 1}")).Prepend("a_0->v__5ftarget"))});\n";
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
            return MethodNameToBody.TryGetValue(method.ToString(), out var body2) ? body2(transpiler, method) : null;
        }
    }
    public static class DefaultBuiltin
    {
        private const BindingFlags declaredAndInstance = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static Builtin Create() => new Builtin {
            MethodNameToBody = {
                ["System.String ToString(System.String, System.IFormatProvider)"] = (transpiler, method) => $"\treturn f__string(u\"{method.ReflectedType}\"sv);\n",
                ["Boolean TryFormat(System.Span`1[System.Char], Int32 ByRef, System.ReadOnlySpan`1[System.Char], System.IFormatProvider)"] = (transpiler, method) => $@"*a_2 = 0;
{'\t'}return false;
",
                ["Boolean System.ISpanFormattable.TryFormat(System.Span`1[System.Char], Int32 ByRef, System.ReadOnlySpan`1[System.Char], System.IFormatProvider)"] = (transpiler, method) => $@"*a_2 = 0;
{'\t'}return false;
"
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
                transpiler => "\treturn f__string(u\"object\"sv);\n"
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
                (transpiler, actual) => $"\treturn f__string(u\"{actual}\"sv);\n"
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
                    var compute = marvin.GetMethod("ComputeHash32", new[] { typeof(byte).MakeByRefType(), typeof(int), typeof(ulong) });
                    transpiler.Enqueue(seed);
                    transpiler.Enqueue(compute);
                    return $"\treturn a_0 ? {transpiler.Escape(compute)}(reinterpret_cast<uint8_t*>(static_cast<t_object*>(a_0)), a_0->f_type()->v__size, {transpiler.Escape(seed)}()) : 0;\n";
                }
            );
            code.For(
                type.GetMethod(nameof(object.ToString)),
                transpiler => "\treturn f__string(u\"struct\"sv);\n"
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
        })
        .For(typeof(RuntimeFieldHandle), (type, code) =>
        {
            code.Fields = transpiler => $@"{'\t'}{'\t'}void* v__field;
{'\t'}{'\t'}void f__destruct()
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
{'\t'}{'\t'}void f__scan(t_scan a_scan)
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
";
            code.For(
                type.GetProperty(nameof(RuntimeFieldHandle.Value)).GetMethod,
                transpiler => $"\treturn {transpiler.EscapeForVariable(typeof(IntPtr))}{{a_0->v__field}};\n"
            );
        })
        .For(typeof(RuntimeTypeHandle), (type, code) =>
        {
            code.Fields = transpiler => $@"{'\t'}{'\t'}{transpiler.EscapeForVariable(typeof(Type))} v__type;
{'\t'}{'\t'}void f__destruct()
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}v__type.f__destruct();
{'\t'}{'\t'}}}
{'\t'}{'\t'}void f__scan(t_scan a_scan)
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}a_scan(v__type);
{'\t'}{'\t'}}}
";
            code.For(
                type.GetMethod(nameof(object.GetHashCode)),
                transpiler => "\treturn reinterpret_cast<intptr_t>(static_cast<t_object*>(a_0->v__type));\n"
            );
        })
        .For(typeof(Array), (type, code) =>
        {
            code.Fields = transpiler => $@"{'\t'}struct t__bound
{'\t'}{{
{'\t'}{'\t'}size_t v_length;
{'\t'}{'\t'}int v_lower;
{'\t'}}};
{'\t'}size_t v__length;
{'\t'}t__bound* f__bounds()
{'\t'}{{
{'\t'}{'\t'}return reinterpret_cast<t__bound*>(this + 1);
{'\t'}}}
";
            code.For(
                type.GetMethod(nameof(Array.Copy), new[] { type, typeof(int), type, typeof(int), typeof(int) }),
                transpiler => $@"{'\t'}if (a_0->f_type() == a_2->f_type()) {{
{'\t'}{'\t'}auto rank = a_0->f_type()->v__rank;
{'\t'}{'\t'}auto n = a_0->f_type()->v__element->v__size;
{'\t'}{'\t'}a_0->f_type()->v__element->f_copy(reinterpret_cast<char*>(a_0->f__bounds() + rank) + a_1 * n, a_4, reinterpret_cast<char*>(a_2->f__bounds() + rank) + a_3 * n);
{'\t'}}} else {{
{'\t'}{'\t'}throw std::runtime_error(""NotImplementedException"");
{'\t'}}}
"
            );
            code.For(
                type.GetMethod(nameof(Array.GetLowerBound)),
                transpiler => $@"
{'\t'}if (a_1 < 0 || a_1 >= a_0->f_type()->v__rank) throw std::out_of_range(""IndexOutOfRangeException"");
{'\t'}return a_0->f__bounds()[a_1].v_lower;
"
            );
            code.For(
                type.GetMethod(nameof(Array.GetUpperBound)),
                transpiler => $@"
{'\t'}if (a_1 < 0 || a_1 >= a_0->f_type()->v__rank) throw std::out_of_range(""IndexOutOfRangeException"");
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
                type.GetMethod("InternalGetReference", declaredAndInstance),
                transpiler => $@"
{'\t'}auto bounds = a_0->f__bounds();
{'\t'}size_t n = 0;
{'\t'}for (size_t i = 0; i < a_2; ++i) n = n * bounds[i].v_length + (a_3[i] - bounds[i].v_lower);
{'\t'}auto type = a_0->f_type();
{'\t'}auto p = reinterpret_cast<{transpiler.EscapeForVariable(typeof(TypedReference))}*>(a_1);
{'\t'}p->v_Type = {{type->v__element}};
{'\t'}p->v_Value = {{reinterpret_cast<char*>(bounds + type->v__rank) + n * type->v__element->v__size}};
"
            );
        })
        .For(typeof(Exception), (type, code) =>
        {
            code.Fields = transpiler => $@"{'\t'}t_slot_of<t_System_2eString> v__5fclassName;
{'\t'}t_slot_of<t_System_2eString> v__5fmessage;
{'\t'}t_slot_of<t_System_2eObject> v__5fdata;
{'\t'}t_slot_of<t_System_2eException> v__5finnerException;
{'\t'}t_slot_of<t_System_2eString> v__5fhelpURL;
{'\t'}t_slot_of<t_System_2eObject> v__5fstackTrace;
{'\t'}t_slot_of<t_System_2eObject> v__5fwatsonBuckets;
{'\t'}t_slot_of<t_System_2eString> v__5fstackTraceString;
{'\t'}t_slot_of<t_System_2eString> v__5fremoteStackTraceString;
{'\t'}int32_t v__5fremoteStackIndex;
{'\t'}t_slot_of<t_System_2eObject> v__5fdynamicMethods;
{'\t'}int32_t v__5fHResult;
{'\t'}t_slot_of<t_System_2eString> v__5fsource;
{'\t'}t_System_2eIntPtr::t_value v__5fxptrs;
{'\t'}int32_t v__5fxcode;
{'\t'}t_System_2eUIntPtr::t_value v__5fipForWatsonBuckets;
";
            code.For(
                type.GetMethod("GetMessageFromNativeResources", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { type.GetNestedType("ExceptionMessageKind", BindingFlags.NonPublic) }, null),
                transpiler => "\treturn f__string(u\"message from native resources\"sv);\n"
            );
            code.ForTree(
                type.GetMethod(nameof(object.ToString)),
                (transpiler, actual) => $"\treturn f__string(u\"{actual}\"sv);\n"
            );
        })
        .For(typeof(Thread), (type, code) =>
        {
            code.Fields = transpiler => $@"
{'\t'}static IL2CXX__PORTABLE__THREAD {transpiler.Escape(type)}* v__current;

{'\t'}static {transpiler.Escape(type)}* f__current()
{'\t'}{{
{'\t'}{'\t'}return v__current;
{'\t'}}}

{'\t'}t_slot_of<t_System_2eDelegate> v__start;
{'\t'}t_thread* v__internal;

{'\t'}void f__scan(t_scan a_scan)
{'\t'}{{
{'\t'}{'\t'}t_System_2eObject::f__scan(a_scan);
{'\t'}{'\t'}a_scan(v__start);
{'\t'}}}
{'\t'}void f__start();
{'\t'}void f__join();
";
            code.For(
                type.GetConstructor(new[] { typeof(ThreadStart) }),
                transpiler => $@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}p->v__start = std::move(a_0);
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
        })
        .For(typeof(string), (type, code) =>
        {
            code.Fields = transpiler => $@"{'\t'}size_t v__length;
{'\t'}char16_t* f__data()
{'\t'}{{
{'\t'}{'\t'}return reinterpret_cast<char16_t*>(this + 1);
{'\t'}}}
";
            code.Initialize = transpiler => $"\tt_static::v_instance->v_{transpiler.Escape(typeof(string))}.{transpiler.Escape(typeof(string).GetField(nameof(string.Empty)))} = f__string(u\"\"sv);\n";
            code.For(
                type.GetMethod("FastAllocateString", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn f__new_string(a_0);\n"
            );
            code.For(
                type.GetMethod("FillStringChecked", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}if (a_1 < 0 || a_1 + a_2->v__length > a_0->v__length) throw std::out_of_range(""IndexOutOfRangeException"");
{'\t'}std::copy_n(a_2->f__data(), a_2->v__length, a_0->f__data() + a_1);
"
            );
            code.For(
                type.GetMethod("wstrcpy", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $"\tstd::copy_n(a_1, a_2, a_0);\n"
            );
            code.For(
                type.GetMethod("GetRawStringData", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => $"\treturn a_0->f__data();\n"
            );
            code.For(
                type.GetMethod("EqualsHelper", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}auto p = a_0->f__data();
{'\t'}return std::equal(p, p + a_0->v__length, a_1->f__data());
"
            );
            code.For(
                type.GetMethod("InternalSubString", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}auto p = f__new_string(a_2);
{'\t'}std::copy_n(a_0->f__data() + a_1, a_2, p->f__data());
{'\t'}return p;
"
            );
            code.For(
                type.GetConstructor(new[] { typeof(ReadOnlySpan<char>) }),
                transpiler => $"\treturn f__string(std::u16string_view(static_cast<char16_t*>(a_0.v__5fpointer.v__5fvalue.v__5fvalue), a_0.v__5flength));\n"
            );
            code.For(
                type.GetProperty(nameof(string.Length)).GetMethod,
                transpiler => "\treturn a_0->v__length;\n"
            );
            code.For(
                type.GetProperty("Chars").GetMethod,
                transpiler => "\treturn a_0->f__data()[a_1];\n"
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
                type.GetMethod(nameof(object.GetHashCode), Type.EmptyTypes),
                transpiler =>
                {
                    var marvin = Type.GetType("System.Marvin");
                    var seed = marvin.GetProperty("DefaultSeed").GetMethod;
                    var compute = marvin.GetMethod("ComputeHash32", new[] { typeof(byte).MakeByRefType(), typeof(int), typeof(ulong) });
                    transpiler.Enqueue(seed);
                    transpiler.Enqueue(compute);
                    return $"\treturn {transpiler.Escape(compute)}(reinterpret_cast<uint8_t*>(a_0->f__data()), a_0->v__length * sizeof(char16_t), {transpiler.Escape(seed)}());\n";
                }
            );
            code.For(
                type.GetMethod("GetLegacyNonRandomizedHashCode", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler =>
                {
                    var marvin = Type.GetType("System.Marvin");
                    var compute = marvin.GetMethod("ComputeHash32", new[] { typeof(byte).MakeByRefType(), typeof(int), typeof(ulong) });
                    transpiler.Enqueue(compute);
                    return $"\treturn {transpiler.Escape(compute)}(reinterpret_cast<uint8_t*>(a_0->f__data()), a_0->v__length * sizeof(char16_t), 0);\n";
                }
            );
            code.For(
                type.GetMethod("CreateFromChar", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn f__string({&a_0, 1});\n"
            );
            code.For(
                type.GetMethod(nameof(object.ToString), Type.EmptyTypes),
                transpiler => null
            );
            code.For(
                type.GetMethod(nameof(string.Join), new[] { typeof(string), typeof(object[]) }),
                transpiler => $"\treturn f__string(u\"join\"sv);\n"
            );
            code.For(
                type.GetMethod(nameof(string.ToLowerInvariant), Type.EmptyTypes),
                transpiler => $@"{'\t'}auto n = a_0->v__length;
{'\t'}auto p = f__new_string(n);
{'\t'}auto q = a_0->f__data();
{'\t'}std::transform(q, q + n, p->f__data(), [](auto x)
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
                type.TypeInitializer,
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod(nameof(object.ToString), Type.EmptyTypes),
                transpiler => null
            );
        })
        .For(typeof(int), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(object.ToString), Type.EmptyTypes),
                transpiler => $"\treturn f__string(f__u16string(std::to_string(*a_0)));\n"
            );
            code.For(
                type.GetMethod(nameof(object.ToString), new[] { typeof(string), typeof(IFormatProvider) }),
                transpiler => $"\treturn f__string(f__u16string(std::to_string(*a_0)));\n"
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
                transpiler => "\treturn f__string(u\"enum\"sv);\n"
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
{'\t'}{'\t'}auto p = t_object::f_allocate(type, sizeof(t_object) + type->v__size);
{'\t'}{'\t'}type->f_copy(reinterpret_cast<char*>(value), 1, reinterpret_cast<char*>(p + 1));
{'\t'}{'\t'}return p;
{'\t'}}} else {{
{'\t'}{'\t'}return *static_cast<{transpiler.EscapeForVariable(typeof(object))}*>(value);
{'\t'}}}
"
            );
        })
        .For(typeof(SZArrayHelper<>), (type, code) =>
        {
            code.ForGeneric(
                type.GetProperty(nameof(SZArrayHelper<object>.Count)).GetMethod,
                (transpiler, types) => $"\treturn a_0->v__length;\n"
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
        .For(Type.GetType("System.Runtime.CompilerServices.JitHelpers"), (type, code) =>
        {
            code.For(
                type.GetMethod("GetRawSzArrayData", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn reinterpret_cast<uint8_t*>(static_cast<t_object*>(a_0) + 1);\n"
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
            code.For(
                type.GetMethod("get_OffsetToStringData"),
                transpiler => $"\treturn sizeof({transpiler.Escape(typeof(string))});\n"
            );
        })
        .For(Type.GetType("System.ByReference`1[System.Char]"), (type, code) =>
        {
            code.For(
                type.GetConstructor(new[] { typeof(char).MakeByRefType() }),
                transpiler => $"\treturn {transpiler.Escape(type)}::t_value{{{{a_0}}}};\n"
            );
        })
        .For(typeof(Math), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(Math.Sqrt)),
                transpiler => $"\treturn std::sqrt(a_0);\n"
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
        .For(Type.GetType("System.SR, System.Private.CoreLib"), (type, code) =>
        {
            code.For(
                type.GetMethod("InternalGetResourceString", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn a_0;\n"
            );
        })
        .For(Type.GetType("System.SR, System.Collections"), (type, code) =>
        {
            code.For(
                type.GetMethod("GetResourceString", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(string) }, null),
                transpiler => "\treturn a_0;\n"
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
{'\t'}t_static::v_instance->v_{transpiler.Escape(gt)}.v__3cDefault_3ek_5f_5fBackingField = std::move(p);
";
            });
        })
        .For(typeof(Environment), (type, code) =>
        {
            code.For(
                type.GetProperty(nameof(Environment.CurrentManagedThreadId)).GetMethod,
                transpiler => "\treturn 0;\n"
            );
        })
        .For(typeof(Console), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(Console.WriteLine), new[] { typeof(string) }),
                transpiler => $@"{'\t'}f_epoch_point();
{'\t'}if (auto p = static_cast<{transpiler.Escape(typeof(string))}*>(a_0)) {{
{'\t'}{'\t'}std::mbstate_t state{{}};
{'\t'}{'\t'}char cs[MB_LEN_MAX];
{'\t'}{'\t'}for (auto c : std::u16string_view(p->f__data(), p->v__length)) {{
{'\t'}{'\t'}{'\t'}auto n = std::c16rtomb(cs, c, &state);
{'\t'}{'\t'}{'\t'}if (n != size_t(-1)) std::cout << std::string_view(cs, n);
{'\t'}{'\t'}}}
{'\t'}{'\t'}auto n = std::c16rtomb(cs, u'\0', &state);
{'\t'}{'\t'}if (n != size_t(-1) && n > 1) std::cout << std::string_view(cs, n - 1);
{'\t'}}}
{'\t'}std::cout << std::endl;
"
            );
        })
        .For(Type.GetType("Internal.Runtime.CompilerServices.Unsafe"), (type, code) =>
        {
            var methods = type.GetMethods().Where(x => x.IsGenericMethodDefinition);
            code.For(
                methods.First(x => x.Name == "Add" && x.GetGenericArguments().Length == 1).MakeGenericMethod(typeof(char)),
                transpiler => "\treturn a_0 + a_1;\n"
            );
            code.ForGeneric(
                methods.First(x => x.Name == "AddByteOffset" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => $"\treturn reinterpret_cast<{transpiler.EscapeForVariable(types[0])}*>(reinterpret_cast<char*>(a_0) + reinterpret_cast<intptr_t>(a_1.v__5fvalue));\n"
            );
            code.For(
                methods.First(x => x.Name == "As" && x.GetGenericArguments().Length == 2).MakeGenericMethod(typeof(byte), typeof(char)),
                transpiler => "\treturn reinterpret_cast<char16_t*>(a_0);\n"
            );
            var byteByRefType = typeof(byte).MakeByRefType();
            code.ForGeneric(
                methods.First(x => x.Name == "ReadUnaligned" && x.GetGenericArguments().Length == 1 && x.GetParameters()[0].ParameterType == byteByRefType),
                (transpiler, types) => $"\treturn *reinterpret_cast<{transpiler.EscapeForVariable(types[0])}*>(a_0);\n"
            );
        });
    }
}
