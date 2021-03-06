using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static void SetupPrimitive(Type type, Builtin.Code code)
        {
            code.For(
                type.GetMethod(nameof(object.GetHashCode)),
                transpiler => ("\treturn static_cast<int32_t>(*a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(object.ToString), Type.EmptyTypes),
                transpiler => ("\treturn f__new_string(std::to_string(*a_0));\n", 0)
            );
            // TODO
            code.For(
                type.GetMethod(nameof(object.ToString), new[] { typeof(string), typeof(IFormatProvider) }),
                transpiler => ("\treturn f__new_string(std::to_string(*a_0));\n", 0)
            );
        }
        private static Action<Type, Builtin.Code> ForIntPtr(string native) => (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}{'\t'}void* v__5fvalue;
{'\t'}{'\t'}t_value() = default;
{'\t'}{'\t'}t_value(void* a_value) : v__5fvalue(a_value)
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
{'\t'}{'\t'}t_value({native} a_value) : v__5fvalue(reinterpret_cast<void*>(a_value))
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
{'\t'}{'\t'}void f_destruct()
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
{'\t'}{'\t'}void f__scan(t_scan<t__type> a_scan)
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
", false, null);
            code.For(
                type.GetMethod(nameof(object.ToString), Type.EmptyTypes),
                transpiler => ("\treturn f__new_string(std::to_string(*a_0));\n", 0)
            );
        };

        private static Builtin SetupSystem(this Builtin @this) => @this
        .For(typeof(object), (type, code) =>
        {
            code.For(
                type.GetMethod("MemberwiseClone", declaredAndInstance),
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->f_type()->f_clone(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(object.GetType)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->f_type();\n", 1)
            );
            code.ForTree(
                type.GetMethod(nameof(object.Equals), new[] { type }),
                (transpiler, actual) => ("\treturn a_0 == a_1;\n", 0)
            );
            code.For(
                type.GetMethod(nameof(object.ToString)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->f_type()->v__display_name);\n", 0)
            );
            // TODO
            /*code.ForTree(
                type.GetMethod(nameof(object.ToString)),
                (transpiler, actual) =>
                {
                    Console.Error.WriteLine($"ToString: {actual}");
                    return ($"\treturn f__new_string(u\"{actual}\"sv);\n", 0);
                }
            );*/
        })
        .For(typeof(ValueType), (type, code) =>
        {
            code.For(
                type.GetMethod("GetHashCodeOfPtr", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn static_cast<intptr_t>(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(object.Equals)),
                transpiler => ("\treturn a_0 == a_1;\n", 1)
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
                    return (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}auto seed = {transpiler.Escape(seed)}();
{'\t'}return {transpiler.Escape(compute)}(reinterpret_cast<uint8_t*>(a_0 + 1), a_0->f_type()->v__size, seed, seed >> 32);
", 0);
                }
            );
            code.For(
                type.GetMethod(nameof(object.ToString)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->f_type()->v__display_name);\n", 0)
            );
            code.ForTree(
                type.GetMethod(nameof(object.Equals)),
                (transpiler, actual) =>
                {
                    var identifier = transpiler.Escape(actual);
                    return ($"\treturn a_1 && a_1->f_type()->f_is(&t__type_of<{identifier}>::v__instance) && std::memcmp(a_0, &static_cast<{identifier}*>(a_1)->v__value, sizeof({identifier})) == 0;\n", 1);
                }
            );
        })
        .For(typeof(IntPtr), ForIntPtr("intptr_t"))
        .For(typeof(UIntPtr), ForIntPtr("uintptr_t"))
        .For(typeof(Type), (type, code) =>
        {
            code.For(
                type.TypeInitializer,
                transpiler => (string.Empty, 1)
            );
            code.For(
                type.GetMethod(nameof(Type.GetType), new[] { typeof(string) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\treturn f__find_type(v__name_to_type, {&a_0->v__5ffirstChar, static_cast<size_t>(a_0->v__5fstringLength)});\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Type.GetTypeFromHandle)),
                transpiler => ("\treturn a_0.v__type;\n", 1)
            );
            code.For(
                type.GetMethod("op_Equality"),
                transpiler => ("\treturn a_0 == a_1;\n", 1)
            );
            code.For(
                type.GetMethod("op_Inequality"),
                transpiler => ("\treturn a_0 != a_1;\n", 1)
            );
            // TODO
            code.For(
                type.GetProperty(nameof(Type.IsInterface)).GetMethod,
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
            code.For(
                type.GetMethod("IsRuntimeImplemented", declaredAndInstance),
                transpiler => ("\treturn true;\n", 1)
            );
        })
        .For(typeof(RuntimeFieldHandle), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}{'\t'}void* v__field;
{'\t'}{'\t'}t_value() = default;
{'\t'}{'\t'}t_value(void* a_field) : v__field(a_field)
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
{'\t'}{'\t'}void f_destruct()
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
{'\t'}{'\t'}void f__scan(t_scan<t__type> a_scan)
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
", false, null);
            code.For(
                type.GetProperty(nameof(RuntimeFieldHandle.Value)).GetMethod,
                transpiler => ($"\treturn {transpiler.EscapeForValue(typeof(IntPtr))}{{a_0->v__field}};\n", 1)
            );
        })
        .For(typeof(RuntimeTypeHandle), (type, code) =>
        {
            code.Members = transpiler => ($@"{'\t'}{'\t'}t__type* v__type;
{'\t'}{'\t'}t_value() = default;
{'\t'}{'\t'}t_value(t__type* a_type) : v__type(a_type)
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
{'\t'}{'\t'}void f_destruct()
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
{'\t'}{'\t'}void f__scan(t_scan<t__type> a_scan)
{'\t'}{'\t'}{{
{'\t'}{'\t'}}}
", false, null);
            code.For(
                type.GetMethod(nameof(object.GetHashCode)),
                transpiler => ("\treturn reinterpret_cast<intptr_t>(a_0->v__type);\n", 1)
            );
            code.For(
                type.GetProperty(nameof(RuntimeTypeHandle.Value)).GetMethod,
                transpiler => ("\treturn a_0->v__type;\n", 1)
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
{'\t'}t__bound* f_bounds()
{'\t'}{{
{'\t'}{'\t'}return reinterpret_cast<t__bound*>(this + 1);
{'\t'}}}
", true, null);
            code.For(
                type.GetMethod(nameof(Array.Clear), BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(Array) }, null),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}auto type = a_0->f_type();
{'\t'}auto element = type->v__element;
{'\t'}element->f_clear(a_0->f_bounds() + type->v__rank, a_0->v__length);
", 1)
            );
            code.For(
                type.GetMethod(nameof(Array.Clear), new[] { typeof(Array), typeof(int), typeof(int) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}auto type = a_0->f_type();
{'\t'}auto element = type->v__element;
{'\t'}element->f_clear(reinterpret_cast<char*>(a_0->f_bounds() + type->v__rank) + a_1 * element->v__size, a_2);
", 0)
            );
            var copy = type.GetMethod(nameof(Array.Copy), new[] { type, typeof(int), type, typeof(int), typeof(int) });
            code.For(
                type.GetMethod(nameof(Array.Copy), new[] { type, type, typeof(int) }),
                transpiler =>
                {
                    transpiler.Enqueue(copy);
                    return ($"\t{transpiler.Escape(copy)}(a_0, 0, a_1, 0, a_2);\n", 1);
                }
            );
            // TODO
            code.For(
                copy,
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + transpiler.GenerateCheckArgumentNull("a_2") + $@"{'\t'}auto type = a_0->f_type();
{'\t'}if (type == a_2->f_type()) {{
{'\t'}{'\t'}auto rank = type->v__rank;
{'\t'}{'\t'}auto element = type->v__element;
{'\t'}{'\t'}auto n = element->v__size;
{'\t'}{'\t'}element->f_copy(reinterpret_cast<char*>(a_0->f_bounds() + rank) + a_1 * n, a_4, reinterpret_cast<char*>(a_2->f_bounds() + rank) + a_3 * n);
{'\t'}}} else {{
{'\t'}{'\t'}throw std::runtime_error(""NotImplementedException"");
{'\t'}}}
", 0)
            );
            // TODO
            code.For(
                type.GetMethod(nameof(Array.CreateInstance), new[] { typeof(Type), typeof(int) }),
                transpiler =>
                {
                    var array = transpiler.Escape(typeof(Array));
                    return (transpiler.GenerateCheckArgumentNull("a_0") + (transpiler.CheckRange ? $"\tif (a_1 < 0) [[unlikely]] {transpiler.GenerateThrow("ArgumentOutOfRange")};\n" : string.Empty) + $@"{'\t'}auto a = sizeof({array}) + sizeof({array}::t__bound);
{'\t'}auto element = static_cast<t__type*>(a_0);
{'\t'}auto n = element->v__size * a_1;
{'\t'}auto p = static_cast<{array}*>(f_engine()->f_allocate(a + n));
{'\t'}p->v__length = a_1;
{'\t'}p->f_bounds()[0] = {{a_1, 0}};
{'\t'}std::memset(reinterpret_cast<char*>(p) + a, 0, n);
{'\t'}element->v__szarray->f_finish(p);
{'\t'}return p;
", 0);
                }
            );
            code.For(
                type.GetMethod(nameof(Array.GetLength)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckRange("a_1", "a_0->f_type()->v__rank") + "\treturn a_0->f_bounds()[a_1].v_length;\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Array.GetLowerBound)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckRange("a_1", "a_0->f_type()->v__rank") + "\treturn a_0->f_bounds()[a_1].v_lower;\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Array.GetUpperBound)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckRange("a_1", "a_0->f_type()->v__rank") + $@"{'\t'}auto& bound = a_0->f_bounds()[a_1];
{'\t'}return bound.v_lower + bound.v_length - 1;
", 1)
            );
            code.For(
                type.GetProperty(nameof(Array.Length)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__length;\n", 1)
            );
            code.For(
                type.GetProperty(nameof(Array.Rank)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->f_type()->v__rank;\n", 1)
            );
            code.For(
                type.GetMethod("InternalGetValue", declaredAndInstance),
                transpiler => ($@"{'\t'}auto type = a_0->f_type();
{'\t'}auto element = type->v__element;
{'\t'}auto value = reinterpret_cast<char*>(a_0->f_bounds()+ type->v__rank) + a_1 * element->v__size;
{'\t'}if (element->v__value_type) {{
{'\t'}{'\t'}auto p = f_engine()->f_allocate(sizeof(t__object) + element->v__size);
{'\t'}{'\t'}element->f_initialize(value, 1, p + 1);
{'\t'}{'\t'}element->f_finish(p);
{'\t'}{'\t'}return p;
{'\t'}}} else {{
{'\t'}{'\t'}return *reinterpret_cast<{transpiler.EscapeForValue(typeof(object))}*>(value);
{'\t'}}}
", 0)
            );
            // TODO
            code.For(
                type.GetMethod("InternalSetValue", declaredAndInstance),
                transpiler => ($@"{'\t'}auto type = a_0->f_type();
{'\t'}auto element = type->v__element;
{'\t'}auto value = reinterpret_cast<char*>(a_0->f_bounds()+ type->v__rank) + a_2 * element->v__size;
{'\t'}if (!a_1) {{
{'\t'}{'\t'}if (element->v__value_type)
{'\t'}{'\t'}{'\t'}element->f_clear(value, 1);
{'\t'}{'\t'}else
{'\t'}{'\t'}{'\t'}*reinterpret_cast<{transpiler.EscapeForValue(typeof(object))}*>(value) = {{}};
{'\t'}}} else if (element->v__value_type) {{
{'\t'}{'\t'}if (a_1->f_type()->f_is(element)) {{
{'\t'}{'\t'}{'\t'}element->f_copy(a_1 + 1, 1, value);
{'\t'}{'\t'}}} else {{
{'\t'}{'\t'}{'\t'}throw std::runtime_error(""NotImplementedException"");
{'\t'}{'\t'}}}
{'\t'}}} else {{
{'\t'}{'\t'}if (!a_1->f_type()->f_is(element) && !a_1->f_type()->f_implementation(element)) throw std::runtime_error(""InvalidCastException"");
{'\t'}{'\t'}*reinterpret_cast<{transpiler.EscapeForValue(typeof(object))}*>(value) = a_1;
{'\t'}}}
", 0)
            );
            code.For(
                type.GetMethod("GetCorElementTypeOfElementType", declaredAndInstance),
                transpiler => ("\treturn a_0->f_type()->v__cor_element_type;\n", 1)
            );
        })
        .For(typeof(SZArrayHelper<>), (type, code) =>
        {
            code.ForGeneric(
                type.GetProperty(nameof(SZArrayHelper<object>.Count)).GetMethod,
                (transpiler, types) => ("\treturn a_0->v__length;\n", 1)
            );
            code.ForGeneric(
                type.GetProperty("Item").GetMethod,
                (transpiler, types) => (transpiler.GenerateCheckRange("a_1", "a_0->v__length") + "\treturn a_0->f_data()[a_1];\n", 1)
            );
            code.ForGeneric(
                type.GetProperty("Item").SetMethod,
                (transpiler, types) => (transpiler.GenerateCheckRange("a_1", "a_0->v__length") + "\ta_0->f_data()[a_1] = a_2;\n", 1)
            );
            code.ForGeneric(
                type.GetMethod(nameof(SZArrayHelper<object>.CopyTo)),
                (transpiler, types) => (transpiler.GenerateCheckArgumentNull("a_1") + (transpiler.CheckRange ? $@"{'\t'}if (a_2 < 0) [[unlikely]] {transpiler.GenerateThrow("IndexOutOfRange")};
{'\t'}if (a_2 + a_0->v__length > a_1->v__length) [[unlikely]] {transpiler.GenerateThrow("Argument")};
" : string.Empty) + "\tstd::copy_n(a_0->f_data(), a_0->v__length, a_1->f_data() + a_2);\n", 0)
            );
            code.ForGeneric(
                type.GetMethod(nameof(SZArrayHelper<object>.GetEnumerator)),
                (transpiler, types) => ($@"{'\t'}t__new<{transpiler.Escape(typeof(SZArrayHelper<>).GetNestedType(nameof(SZArrayHelper<object>.Enumerator)).MakeGenericType(types))}> p(0);
{'\t'}new(&p->v_array) decltype(p->v_array)(a_0);
{'\t'}p->v_index = -1;
{'\t'}return p;
", 1)
            );
            code.ForGeneric(
                type.GetMethod(nameof(SZArrayHelper<object>.IndexOf)),
                (transpiler, types) =>
                {
                    var t = Type.MakeGenericMethodParameter(0);
                    var method = typeof(Array).GetMethod(nameof(Array.IndexOf), 1, new[] { t.MakeArrayType(), t }).MakeGenericMethod(types[0]);
                    transpiler.Enqueue(method);
                    return ($"\treturn {transpiler.Escape(method)}(a_0, a_1);\n", 1);
                }
            );
        })
        .For(typeof(Attribute), (type, code) =>
        {
            // TODO
            code.For(
                type.GetMethod(nameof(Attribute.GetCustomAttributes), new[] { typeof(MemberInfo), typeof(Type), typeof(bool) }),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(typeof(Exception), (type, code) =>
        {
            // TODO
            code.ForTree(
                type.GetMethod(nameof(object.ToString)),
                (transpiler, actual) => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}std::u16string s{{a_0->f_type()->v__display_name}};
{'\t'}if (a_0->v__5fmessage) s = s + u"": "" + &a_0->v__5fmessage->v__5ffirstChar;
{'\t'}return f__new_string(s);
", 0)
            );
            // TODO
            code.For(
                type.GetMethod("GetMessageFromNativeResources", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { type.GetNestedType("ExceptionMessageKind", BindingFlags.NonPublic) }, null),
                transpiler => ("\treturn f__new_string(u\"message from native resources\"sv);\n", 0)
            );
            // TODO
            code.For(
                type.GetMethod("RestoreDispatchState", declaredAndInstance),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
            // TODO
            code.For(
                type.GetMethod("SetCurrentStackTrace", declaredAndInstance),
                transpiler => (string.Empty, 0)
            );
        })
        .For(typeof(GC), (type, code) =>
        {
            code.For(
                type.GetMethod("_Collect", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}if (!(a_1 & 2)) {{
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
", 0)
            );
            code.For(
                type.GetMethod(nameof(GC.SuppressFinalize)),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\ta_0->f_type()->f_suppress_finalize(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(GC.ReRegisterForFinalize)),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\ta_0->f_type()->f_register_finalize(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(GC.WaitForPendingFinalizers)),
                transpiler => ("\tf_engine()->f_finalize();\n", 1)
            );
            code.For(
                type.GetMethod("AllocateNewArray", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler =>
                {
                    var array = transpiler.Escape(typeof(Array));
                    return ($@"{'\t'}auto type = static_cast<t__type*>(static_cast<void*>(a_0));
{'\t'}auto n = type->v__element->v__size * a_1;
{'\t'}auto p = static_cast<{transpiler.EscapeForValue(typeof(Array))}>(f_engine()->f_allocate(sizeof({array}) + sizeof({array}::t__bound) + n));
{'\t'}p->v__length = a_1;
{'\t'}p->f_bounds()[0] = {{size_t(a_1), 0}};
{'\t'}if (!a_2) std::memset(p->f_bounds() + 1, 0, n);
{'\t'}type->f_finish(p);
{'\t'}return p;
", 1);
                }
            );
            // TODO
            code.For(
                type.GetMethod("GetMemoryInfo", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}auto load = f_engine()->f_load_count();
{'\t'}//a_0->... = load;
", 0)
            );
        })
        .For(typeof(WeakReference), (type, code) =>
        {
            code.For(
                type.GetMethod("Create", declaredAndInstance),
                transpiler => ($"\ta_0->v_m_5fhandle = {transpiler.EscapeForValue(typeof(IntPtr))}{{new t__weak_handle(a_1, a_2)}};\n", 1)
            );
            code.For(
                type.GetMethod("Finalize", declaredAndInstance),
                transpiler => ("\tdelete static_cast<t__weak_handle*>(a_0->v_m_5fhandle.v__5fvalue);\n", 1)
            );
            code.For(
                type.GetProperty(nameof(WeakReference.IsAlive)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn static_cast<t__weak_handle*>(a_0->v_m_5fhandle.v__5fvalue)->f_target();\n", 1)
            );
            code.For(
                type.GetProperty(nameof(WeakReference.Target)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn static_cast<t__weak_handle*>(a_0->v_m_5fhandle.v__5fvalue)->f_target();\n", 1)
            );
            code.For(
                type.GetProperty(nameof(WeakReference.Target)).SetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\tstatic_cast<t__weak_handle*>(a_0->v_m_5fhandle.v__5fvalue)->f_target__(a_1);\n", 1)
            );
        })
        .For(typeof(WeakReference<>), (type, code) =>
        {
            code.ForGeneric(
                type.GetMethod("Create", declaredAndInstance),
                (transpiler, types) => ($"\ta_0->v_m_5fhandle = {transpiler.EscapeForValue(typeof(IntPtr))}{{new t__weak_handle(a_1, a_2)}};\n", 1)
            );
            code.ForGeneric(
                type.GetMethod("Finalize", declaredAndInstance),
                (transpiler, types) => ("\tdelete static_cast<t__weak_handle*>(a_0->v_m_5fhandle.v__5fvalue);\n", 1)
            );
            code.ForGeneric(
                type.GetProperty("Target", declaredAndInstance).GetMethod,
                (transpiler, types) => ($"\treturn static_cast<{transpiler.EscapeForValue(types[0])}>(static_cast<t__weak_handle*>(a_0->v_m_5fhandle.v__5fvalue)->f_target());\n", 1)
            );
            code.ForGeneric(
                type.GetProperty("Target", declaredAndInstance).SetMethod,
                (transpiler, types) => ("\tstatic_cast<t__weak_handle*>(a_0->v_m_5fhandle.v__5fvalue)->f_target__(a_1);\n", 1)
            );
        })
        .For(typeof(Delegate), (type, code) =>
        {
            code.For(
                type.GetMethod("InternalEqualTypes", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn a_0->f_type() == a_1->f_type();\n", 1)
            );
            code.For(
                type.GetMethod("InternalAllocLike", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}auto type = a_0->f_type();
{'\t'}auto n = sizeof({transpiler.Escape(typeof(MulticastDelegate))});
{'\t'}auto p = f_engine()->f_allocate(n);
{'\t'}std::memset(p + 1, 0, n - sizeof(t__object));
{'\t'}type->f_finish(p);
{'\t'}return static_cast<{transpiler.EscapeForValue(typeof(MulticastDelegate))}>(p);
", 1)
            );
            code.For(
                type.GetMethod("GetInvokeMethod", declaredAndInstance),
                transpiler => ("\treturn {};\n", 1)
            );
            code.For(
                type.GetMethod("GetMulticastInvoke", declaredAndInstance),
                transpiler => ("\treturn a_0->f_type()->v__multicast_invoke;\n", 1)
            );
            // TODO
            code.For(
                type.GetMethod("GetMethodImpl", declaredAndInstance),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(typeof(MulticastDelegate), (type, code) =>
        {
            // TODO
            code.For(
                type.GetMethod("GetTarget", declaredAndInstance),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
            // TODO
            code.For(
                type.GetMethod("GetMethodImpl", declaredAndInstance),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(typeof(Activator), (type, code) =>
        {
            var methods = GenericMethods(type);
            code.ForGeneric(
                methods.First(x => x.Name == nameof(Activator.CreateInstance)),
                (transpiler, types) =>
                {
                    var t = types[0];
                    if (t.IsValueType) return ("\treturn {};\n", 1);
                    var constructor = t.GetConstructor(Type.EmptyTypes);
                    return (constructor == null
                        ? "\tthrow std::runtime_error(\"no parameterless constructor\");\n"
                        : $@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(t)}>();
{'\t'}{transpiler.Escape(constructor)}(p);
{'\t'}return p;
", 0);
                }
            );
            code.For(
                type.GetMethod(nameof(Activator.CreateInstance), new[] { typeof(Type) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}if (a_0->f_type() != &t__type_of<t__type>::v__instance) throw std::runtime_error(""must be t__type"");
{'\t'}auto type = static_cast<t__type*>(a_0);
{'\t'}if (type->v__value_type) {{
{'\t'}{'\t'}auto p = f_engine()->f_allocate(type->v__managed_size);
{'\t'}{'\t'}std::memset(p + 1, 0, type->v__managed_size - sizeof(t__object));
{'\t'}{'\t'}type->f_finish(p);
{'\t'}{'\t'}return p;
{'\t'}}}
{'\t'}if (!type->v__default_constructor) throw std::runtime_error(""no parameterless constructor"");
{'\t'}return type->v__default_constructor->v__invoke();
", 0)
            );
            // TODO
            code.For(
                type.GetMethod(nameof(Activator.CreateInstance), new[] { typeof(Type), typeof(bool) }),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
            // TODO
            code.For(
                type.GetMethod(nameof(Activator.CreateInstance), new[] { typeof(Type), typeof(object[]) }),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
            // TODO
            code.For(
                type.GetMethod(nameof(Activator.CreateInstance), new[] { typeof(Type), typeof(BindingFlags), typeof(Binder), typeof(object[]), typeof(CultureInfo), typeof(object[]) }),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(typeof(string), (type, code) =>
        {
            code.Initialize = transpiler => $"\t\t{Transpiler.Escape(typeof(string).GetField(nameof(string.Empty)))} = f__new_string(u\"\"sv);";
            code.For(
                type.GetMethod("FastAllocateString", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn f__new_string(a_0);\n", 2)
            );
            code.For(
                type.GetConstructor(new[] { typeof(char*) }),
                transpiler => ("\treturn f__new_string(std::u16string_view(a_0));\n", 1)
            );
            code.For(
                type.GetConstructor(new[] { typeof(char*), typeof(int), typeof(int) }),
                transpiler => ("\treturn f__new_string(std::u16string_view(a_0 + a_1, a_2));\n", 1)
            );
            code.For(
                type.GetConstructor(new[] { typeof(char), typeof(int) }),
                transpiler => ($@"{'\t'}auto p = f__new_string(a_1);
{'\t'}std::fill_n(&p->v__5ffirstChar, a_1, a_0);
{'\t'}return p;
", 1)
            );
            code.For(
                type.GetConstructor(new[] { typeof(char[]) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\treturn f__new_string(std::u16string_view(a_0->f_data(), a_0->v__length));\n", 1)
            );
            code.For(
                type.GetConstructor(new[] { typeof(char[]), typeof(int), typeof(int) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\treturn f__new_string(std::u16string_view(a_0->f_data() + a_1, a_2));\n", 1)
            );
            code.For(
                type.GetConstructor(new[] { typeof(ReadOnlySpan<char>) }),
                transpiler => ("\treturn f__new_string(std::u16string_view(static_cast<char16_t*>(a_0.v__5fpointer.v__5fvalue.v__5fvalue), a_0.v__5flength));\n", 1)
            );
            code.For(
                type.GetConstructor(new[] { typeof(sbyte*) }),
                transpiler => ("\treturn f__new_string(reinterpret_cast<char*>(a_0));\n", 1)
            );
            code.For(
                type.GetConstructor(new[] { typeof(sbyte*), typeof(int), typeof(int) }),
                transpiler => ("\treturn f__new_string(std::string_view(reinterpret_cast<char*>(a_0) + a_1, a_2));\n", 1)
            );
            code.For(
                type.GetProperty(nameof(string.Length)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__5fstringLength;\n", 1)
            );
            code.For(
                type.GetProperty("Chars").GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckRange("a_1", "a_0->v__5fstringLength") + "\treturn (&a_0->v__5ffirstChar)[a_1];\n", 1)
            );
            // TODO
            code.For(
                type.GetMethod(nameof(object.Equals), new[] { typeof(object) }),
                transpiler => default
            );
            // TODO
            code.For(
                type.GetMethod(nameof(string.Equals), new[] { typeof(string), typeof(StringComparison) }),
                transpiler =>
                {
                    var method = typeof(string).GetMethod(nameof(string.Equals), new[] { typeof(string) });
                    transpiler.Enqueue(method);
                    return ($"\treturn {transpiler.Escape(method)}(a_0, a_1);\n", 1);
                }
            );
            // TODO
            code.For(
                type.GetMethod("IsAscii", declaredAndInstance),
                transpiler => ("\treturn false;\n", 1)
            );
            // TODO
            code.For(
                type.GetMethod(nameof(string.Join), new[] { typeof(string), typeof(object[]) }),
                transpiler => ("\treturn f__new_string(u\"join\"sv);\n", 0)
            );
            // TODO
            code.For(
                type.GetMethod(nameof(string.Split), new[] { typeof(char), typeof(StringSplitOptions) }),
                transpiler => ($"\treturn f__new_array<{transpiler.Escape(typeof(string[]))}, {transpiler.EscapeForMember(typeof(string))}>(0);\n", 0)
            );
            code.For(
                type.GetMethod(nameof(string.ToLowerInvariant), Type.EmptyTypes),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}auto n = a_0->v__5fstringLength;
{'\t'}auto p = f__new_string(n);
{'\t'}auto q = &a_0->v__5ffirstChar;
{'\t'}std::transform(q, q + n, &p->v__5ffirstChar, [](auto x)
{'\t'}{{
{'\t'}{'\t'}return x >= u'A' && x <= u'Z' ? x + (u'a' - u'A') : x;
{'\t'}}});
{'\t'}return p;
", 0)
            );
        })
        .For(typeof(sbyte), SetupPrimitive)
        .For(typeof(short), SetupPrimitive)
        .For(typeof(byte), SetupPrimitive)
        .For(typeof(ushort), SetupPrimitive)
        .For(typeof(int), SetupPrimitive)
        .For(typeof(uint), SetupPrimitive)
        .For(typeof(long), SetupPrimitive)
        .For(typeof(ulong), SetupPrimitive)
        .For(typeof(float), SetupPrimitive)
        .For(typeof(double), SetupPrimitive)
        .For(typeof(Enum), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(object.Equals)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_1 || a_0->f_type() != a_1->f_type()) return false;
{'\t'}switch (a_0->f_type()->v__size) {{
{'\t'}case 1:
{'\t'}{'\t'}return *reinterpret_cast<uint8_t*>(a_0 + 1) == *reinterpret_cast<uint8_t*>(a_1 + 1);
{'\t'}case 2:
{'\t'}{'\t'}return *reinterpret_cast<uint16_t*>(a_0 + 1) == *reinterpret_cast<uint16_t*>(a_1 + 1);
{'\t'}case 4:
{'\t'}{'\t'}return *reinterpret_cast<uint32_t*>(a_0 + 1) == *reinterpret_cast<uint32_t*>(a_1 + 1);
{'\t'}default:
{'\t'}{'\t'}return *reinterpret_cast<uint64_t*>(a_0 + 1) == *reinterpret_cast<uint64_t*>(a_1 + 1);
{'\t'}}}
", 0)
            );
            code.For(
                type.GetMethod(nameof(object.GetHashCode)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}switch (a_0->f_type()->v__size) {{
{'\t'}case 1:
{'\t'}{'\t'}return *reinterpret_cast<uint8_t*>(a_0 + 1);
{'\t'}case 2:
{'\t'}{'\t'}return *reinterpret_cast<uint16_t*>(a_0 + 1);
{'\t'}case 4:
{'\t'}{'\t'}return *reinterpret_cast<uint32_t*>(a_0 + 1);
{'\t'}default:
{'\t'}{'\t'}return *reinterpret_cast<uint64_t*>(a_0 + 1);
{'\t'}}}
", 0)
            );
            // TODO
            code.For(
                type.GetMethod(nameof(object.ToString), Type.EmptyTypes),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}uint64_t i;
{'\t'}switch (a_0->f_type()->v__size) {{
{'\t'}case 1:
{'\t'}{'\t'}i = *reinterpret_cast<uint8_t*>(a_0 + 1);
{'\t'}{'\t'}break;
{'\t'}case 2:
{'\t'}{'\t'}i = *reinterpret_cast<uint16_t*>(a_0 + 1);
{'\t'}{'\t'}break;
{'\t'}case 4:
{'\t'}{'\t'}i = *reinterpret_cast<uint32_t*>(a_0 + 1);
{'\t'}{'\t'}break;
{'\t'}default:
{'\t'}{'\t'}i = *reinterpret_cast<uint64_t*>(a_0 + 1);
{'\t'}}}
{'\t'}auto p = a_0->f_type()->v__enum_pairs;
{'\t'}auto q = p + a_0->f_type()->v__enum_count;
{'\t'}auto j = std::lower_bound(p, q, i, [](auto x, auto y)
{'\t'}{{
{'\t'}{'\t'}return x.first < y;
{'\t'}}});
{'\t'}return j != q && j->first == i ? f__new_string(j->second) : f__new_string(std::to_string(i));
", 0)
            );
            // TODO
            code.For(
                type.GetMethod(nameof(Enum.ToString), new[] { typeof(string) }),
                transpiler =>
                {
                    var method = type.GetMethod(nameof(object.ToString), Type.EmptyTypes);
                    transpiler.Enqueue(method);
                    return ($@"{'\t'}if (!a_1) return {transpiler.Escape(method)}(a_0);
{'\t'}if (a_1->v__5fstringLength != 1) throw std::runtime_error(""FormatException"");
{'\t'}switch (a_1->v__5ffirstChar) {{
{'\t'}case u'G':
{'\t'}case u'g':
{'\t'}{'\t'}return {transpiler.Escape(method)}(a_0);
{'\t'}default:
{'\t'}{'\t'}throw std::runtime_error(""FormatException"");
{'\t'}}}
", 0);
                }
            );
            code.For(
                type.GetMethod("InternalGetCorElementType", declaredAndInstance),
                transpiler => ("\treturn a_0->f_type()->v__cor_element_type;\n", 1)
            );
            // TODO
            code.For(
                type.GetMethod("TryParse", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(Type), typeof(string), typeof(bool), typeof(bool), typeof(object).MakeByRefType() }, null),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(typeof(TypedReference), (type, code) =>
        {
            code.For(
                type.GetMethod("InternalToObject", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}auto p = static_cast<{transpiler.EscapeForValue(type)}*>(a_0);
{'\t'}auto type = static_cast<t__type*>(p->v__5ftype.v__5fvalue);
{'\t'}auto value = p->v__5fvalue.v__5fvalue.v__5fvalue;
{'\t'}if (type->v__value_type) {{
{'\t'}{'\t'}auto p = f_engine()->f_allocate(sizeof(t__object) + type->v__size);
{'\t'}{'\t'}type->f_initialize(value, 1, p + 1);
{'\t'}{'\t'}type->f_finish(p);
{'\t'}{'\t'}return p;
{'\t'}}} else {{
{'\t'}{'\t'}return *static_cast<{transpiler.EscapeForValue(typeof(object))}*>(value);
{'\t'}}}
", 1)
            );
        })
        .For(typeof(Environment), (type, code) =>
        {
            code.For(
                type.GetProperty(nameof(Environment.CurrentManagedThreadId)).GetMethod,
                transpiler => ($@"#ifdef __unix__
{'\t'}return gettid();
#endif
#ifdef _WIN32
{'\t'}return GetCurrentThreadId();
#endif
", 1)
            );
            code.For(
                type.GetMethod("GetProcessorCount", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn std::thread::hardware_concurrency();\n", 1)
            );
            code.For(
                type.GetProperty(nameof(Environment.HasShutdownStarted)).GetMethod,
                transpiler => ("\treturn f_engine()->f_exiting();\n", 1)
            );
            static (string body, int inline) tick(Transpiler transpiler, string suffix) => ($@"#ifdef __unix__
{'\t'}timespec ts;
{'\t'}if (clock_gettime(CLOCK_MONOTONIC, &ts) != 0) throw std::runtime_error(""clock_gettime"");
{'\t'}return ts.tv_sec * 1000 + ts.tv_nsec / 1000000;
#endif
#ifdef _WIN32
{'\t'}return GetTickCount{suffix}();
#endif
", 0);
            code.For(type.GetProperty(nameof(Environment.TickCount)).GetMethod, transpiler => tick(transpiler, string.Empty));
            code.For(type.GetProperty(nameof(Environment.TickCount64)).GetMethod, transpiler => tick(transpiler, "64"));
            code.For(
                type.GetMethod(nameof(Environment.FailFast), new[] { typeof(string) }),
                transpiler => ($@"{'\t'}std::cerr << f__string({{&a_0->v__5ffirstChar, static_cast<size_t>(a_0->v__5fstringLength)}}) << std::endl;
{'\t'}std::abort();
", 0)
            );
            code.For(
                type.GetMethod(nameof(Environment.FailFast), new[] { typeof(string), typeof(Exception) }),
                transpiler => ($@"{'\t'}std::cerr << f__string({{&a_0->v__5ffirstChar, static_cast<size_t>(a_0->v__5fstringLength)}}) << std::endl;
{transpiler.GenerateVirtualCall(typeof(object).GetMethod(nameof(object.ToString)), "a_1", Enumerable.Empty<string>(), x => $"auto s = {x};")}
{'\t'}std::cerr << f__string({{&s->v__5ffirstChar, static_cast<size_t>(s->v__5fstringLength)}}) << std::endl;
{'\t'}std::abort();
", 0)
            );
            code.For(
                type.GetMethod(nameof(Environment.GetEnvironmentVariable), new[] { typeof(string) }),
                transpiler => ($@"{'\t'}auto p = std::getenv(f__string({{&a_0->v__5ffirstChar, static_cast<size_t>(a_0->v__5fstringLength)}}).c_str());
{'\t'}return p ? f__new_string(p) : nullptr;
", 0)
            );
        })
        .For(Type.GetType("System.ByReference`1"), (type, code) =>
        {
            code.ForGeneric(
                type.GetConstructor(new[] { type.GetGenericArguments()[0].MakeByRefType() }),
                (transpiler, types) => ($@"{'\t'}{transpiler.EscapeForValue(type.MakeGenericType(types))} a;
{'\t'}a.v__5fvalue = {{a_0}};
{'\t'}return a;
", 1)
            );
            code.ForGeneric(
                type.GetProperty("Value").GetMethod,
                (transpiler, types) => ($"\treturn static_cast<{transpiler.EscapeForValue(types[0].MakeByRefType())}>(a_0->v__5fvalue.v__5fvalue);\n", 1)
            );
        })
        .For(typeof(Math), (type, code) =>
        {
            foreach (var t in new[] { typeof(double), typeof(float) })
                code.For(
                    type.GetMethod(nameof(Math.Abs), new[] { t }),
                    transpiler => ("\treturn std::abs(a_0);\n", 1)
                );
            code.For(
                type.GetMethod(nameof(Math.Acos)),
                transpiler => ("\treturn std::acos(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Math.Asin)),
                transpiler => ("\treturn std::asin(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Math.Atan)),
                transpiler => ("\treturn std::atan(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Math.Atan2)),
                transpiler => ("\treturn std::atan2(a_0, a_1);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Math.Ceiling), new[] { typeof(double) }),
                transpiler => ("\treturn std::ceil(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Math.Cos)),
                transpiler => ("\treturn std::cos(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Math.Exp)),
                transpiler => ("\treturn std::exp(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Math.Floor), new[] { typeof(double) }),
                transpiler => ("\treturn std::floor(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Math.Log), new[] { typeof(double) }),
                transpiler => ("\treturn std::log(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Math.Log10)),
                transpiler => ("\treturn std::log10(a_0);\n", 1)
            );
            code.For(
                type.GetMethod("ModF", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn std::modf(a_0, a_1);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Math.Pow)),
                transpiler => ("\treturn std::pow(a_0, a_1);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Math.Sin)),
                transpiler => ("\treturn std::sin(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Math.Sqrt)),
                transpiler => ("\treturn std::sqrt(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Math.Tan)),
                transpiler => ("\treturn std::tan(a_0);\n", 1)
            );
        })
        .For(typeof(MathF), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(MathF.Abs)),
                transpiler => ("\treturn std::abs(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(MathF.Acos)),
                transpiler => ("\treturn std::acos(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(MathF.Asin)),
                transpiler => ("\treturn std::asin(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(MathF.Atan)),
                transpiler => ("\treturn std::atan(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(MathF.Atan2)),
                transpiler => ("\treturn std::atan2(a_0, a_1);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(MathF.Ceiling)),
                transpiler => ("\treturn std::ceil(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(MathF.Cos)),
                transpiler => ("\treturn std::cos(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(MathF.Exp)),
                transpiler => ("\treturn std::exp(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(MathF.Floor)),
                transpiler => ("\treturn std::floor(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(MathF.Log), new[] { typeof(float) }),
                transpiler => ("\treturn std::log(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(MathF.Log10)),
                transpiler => ("\treturn std::log10(a_0);\n", 1)
            );
            code.For(
                type.GetMethod("ModF", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn std::modf(a_0, a_1);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(MathF.Pow)),
                transpiler => ("\treturn std::pow(a_0, a_1);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(MathF.Sin)),
                transpiler => ("\treturn std::sin(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(MathF.Sqrt)),
                transpiler => ("\treturn std::sqrt(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(MathF.Tan)),
                transpiler => ("\treturn std::tan(a_0);\n", 1)
            );
        })
        .For(typeof(Buffer), (type, code) =>
        {
            var methods = GenericMethods(type);
            code.For(
                type.GetMethod(nameof(Buffer.BlockCopy)),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + transpiler.GenerateCheckArgumentNull("a_2") + "\tf__copy(reinterpret_cast<char*>(a_0->f_bounds() + a_0->f_type()->v__rank) + a_1, a_4, reinterpret_cast<char*>(a_2->f_bounds() + a_2->f_type()->v__rank) + a_3);\n", 1)
            );
            code.ForGeneric(
                methods.First(x => x.Name == "Memmove" && x.GetGenericArguments().Length == 1),
                (transpiler, types) => ($"\tt__type_of<{transpiler.Escape(types[0])}>::f_do_copy(a_1, a_2, a_0);\n", 1)
            );
            var @byte = typeof(byte).MakeByRefType();
            code.For(
                type.GetMethod("_Memmove", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { @byte, @byte, typeof(UIntPtr) }, null),
                transpiler => ("\tstd::memmove(a_0, a_1, a_2);\n", -1)
            );
        })
        .For(typeof(MissingMemberException), (type, code) =>
        {
            // TODO
            code.For(
                type.GetMethod("FormatSignature", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(Type.GetType("System.Marvin"), (type, code) =>
        {
            code.For(
                type.GetMethod("GenerateSeed", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}union
{'\t'}{{
{'\t'}{'\t'}uint32_t v_32s[2];
{'\t'}{'\t'}uint64_t v_64;
{'\t'}}} seed;
{'\t'}std::seed_seq().generate(seed.v_32s, seed.v_32s + 2);
{'\t'}return seed.v_64;
", 0)
            );
        })
        .For(Type.GetType("System.SpanHelpers"), (type, code) =>
        {
            var @byte = typeof(byte).MakeByRefType();
            code.For(
                type.GetMethod("SequenceEqual", new[] { @byte, @byte, typeof(UIntPtr) }),
                transpiler => ("\treturn std::memcmp(a_0, a_1, a_2) == 0;\n", 1)
            );
        })
        .For(Type.GetType("System.ThrowHelper"), (type, code) =>
        {
            code.ForGeneric(
                type.GetMethod("ThrowForUnsupportedNumericsVectorBaseType", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => (string.Empty, 1)
            );
            code.ForGeneric(
                type.GetMethod("ThrowForUnsupportedIntrinsicsVectorBaseType", BindingFlags.Static | BindingFlags.NonPublic),
                (transpiler, types) => (string.Empty, 1)
            );
        })
        .For(Type.GetType("System.CLRConfig"), (type, code) =>
        {
            code.For(
                type.GetMethod("GetBoolValue", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}*a_1 = false;
{'\t'}return false;
", 0)
            );
        });
    }
}
