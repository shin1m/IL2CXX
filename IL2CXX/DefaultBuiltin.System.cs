using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
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

        private static Builtin SetupSystem(this Builtin @this) => @this
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
{'\t'}auto n = sizeof({transpiler.Escape(typeof(MulticastDelegate))});
{'\t'}auto p = type->f__allocate(n);
{'\t'}std::fill_n(reinterpret_cast<char*>(static_cast<t_object*>(p) + 1), n - sizeof(t_object), '\0');
{'\t'}return p;
"
            );
            code.For(
                type.GetMethod("GetInvokeMethod", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => "\treturn {};\n"
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
        .For(typeof(ModuleHandle), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}{transpiler.EscapeForVariable(typeof(Module))} v_m_5fptr;
", true);
        })
        .For(typeof(MissingMemberException), (type, code) =>
        {
            code.For(
                type.GetMethod("FormatSignature", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
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
