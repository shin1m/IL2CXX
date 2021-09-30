using System;
using System.Globalization;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static void SetupMemberInfo(Func<Type, Type> get, Type type, Builtin.Code code)
        {
            code.For(
                type.GetProperty(nameof(MemberInfo.DeclaringType)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__declaring_type;\n", 0)
            );
            // TODO
            code.For(
                type.GetMethod(nameof(MemberInfo.GetCustomAttributes), new[] { get(typeof(Type)), get(typeof(bool)) }),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}
{'\t'}auto p = f__new_array<{transpiler.Escape(get(typeof(object[])))}, {transpiler.Escape(get(typeof(object)))}>(0);
{'\t'}return p;
", 0)
            );
            code.For(
                type.GetProperty(nameof(MemberInfo.Name)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->v__name);\n", 0)
            );
        }
        private static void SetupMethodBase(Func<Type, Type> get, Type type, Builtin.Code code)
        {
            SetupMemberInfo(get, type, code);
            // TODO
            code.For(
                type.GetMethod(nameof(MethodBase.GetParameters)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}
{'\t'}auto p = f__new_array<{transpiler.Escape(get(typeof(ParameterInfo[])))}, {transpiler.Escape(get(typeof(ParameterInfo)))}>(0);
{'\t'}return p;
", 0)
            );
        }
        private static Builtin SetupRuntime(this Builtin @this, Func<Type, Type> get) => @this
        .For(get(typeof(RuntimeAssembly)), (type, code) =>
        {
            code.For(
                type.GetProperty(nameof(Assembly.EntryPoint)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__entry_point;\n", 0)
            );
            code.For(
                type.GetProperty(nameof(Assembly.FullName)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->v__full_name);\n", 0)
            );
            code.For(
                type.GetProperty(nameof(RuntimeAssembly.Name)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->v__name);\n", 1)
            );
        })
        .For(get(typeof(RuntimeConstructorInfo)), (type, code) =>
        {
            SetupMethodBase(get, type, code);
            code.For(
                type.GetMethod(nameof(MethodBase.Invoke), new[] { get(typeof(BindingFlags)), get(typeof(Binder)), get(typeof(object[])), get(typeof(CultureInfo)) }),
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__invoke();\n", 0)
            );
        })
        .For(get(typeof(RuntimeFieldInfo)), (type, code) =>
        {
            SetupMemberInfo(get, type, code);
            code.For(
                type.GetMethod(nameof(FieldInfo.GetValue)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (a_1 && !a_1->f_type()->f_is(a_0->v__declaring_type)) [[unlikely]] {transpiler.GenerateThrow("Argument")};
{'\t'}return a_0->v__type->f_box(a_0->f_address(a_0->v__declaring_type->f_unbox(a_1)));
", 0)
            );
            code.For(
                type.GetMethod(nameof(FieldInfo.SetValue), new[] { get(typeof(object)), get(typeof(object)), get(typeof(BindingFlags)), get(typeof(Binder)), get(typeof(CultureInfo)) }),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (a_1 && !a_1->f_type()->f_is(a_0->v__declaring_type)) [[unlikely]] {transpiler.GenerateThrow("Argument")};
{'\t'}if (a_2 && !a_2->f_type()->f_is(a_0->v__type)) [[unlikely]] {transpiler.GenerateThrow("Argument")};
{'\t'}a_0->v__type->f_copy(a_0->v__type->f_unbox(a_2), 1, a_0->f_address(a_0->v__declaring_type->f_unbox(a_1)));
", 0)
            );
        })
        .For(get(typeof(RuntimeMethodInfo)), (type, code) =>
        {
            SetupMethodBase(get, type, code);
        })
        .For(get(typeof(RuntimePropertyInfo)), (type, code) =>
        {
            SetupMemberInfo(get, type, code);
        })
        .For(get(typeof(RuntimeType)), (type, code) =>
        {
            SetupMemberInfo(get, type, code);
            code.For(
                type.GetProperty(nameof(Type.Assembly)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__assembly;\n", 0)
            );
            code.For(
                type.GetProperty(nameof(Type.BaseType)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__base;\n", 0)
            );
            code.For(
                type.GetProperty(nameof(Type.FullName)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->v__full_name);\n", 0)
            );
            // TODO
            code.For(
                type.GetMethod(nameof(Type.GetConstructors), new[] { get(typeof(BindingFlags)) }),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}auto p = f__new_array<{transpiler.Escape(get(typeof(ConstructorInfo[])))}, {transpiler.Escape(get(typeof(ConstructorInfo)))}>(a_0->v__default_constructor ? 1 : 0);
{'\t'}if (a_0->v__default_constructor) p->f_data()[0] = a_0->v__default_constructor;
{'\t'}return p;
", 0)
            );
            code.For(
                type.GetMethod(nameof(Type.GetEnumNames)),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}if (!a_0->v__enum) throw std::runtime_error(""not enum"");
{'\t'}size_t n = 0;
{'\t'}for (auto p = a_0->v__fields; *p; ++p) ++n;
{'\t'}auto p = f__new_array<{transpiler.Escape(get(typeof(string[])))}, {transpiler.Escape(get(typeof(string)))}>(n);
{'\t'}for (size_t i = 0; i < n; ++i) p->f_data()[i] = f__new_string(a_0->v__fields[i]->v__name);
{'\t'}return p;
", 0)
            );
            code.For(
                type.GetMethod(nameof(Type.GetEnumValues)),
                transpiler =>
                {
                    var array = transpiler.Escape(get(typeof(Array)));
                    return (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}if (!a_0->v__enum) throw std::runtime_error(""not enum"");
{'\t'}auto a = sizeof({array}) + sizeof({array}::t__bound);
{'\t'}size_t n = 0;
{'\t'}for (auto p = a_0->v__fields; *p; ++p) ++n;
{'\t'}auto p = static_cast<{array}*>(f_engine()->f_allocate(a + a_0->v__size * n));
{'\t'}p->v__length = n;
{'\t'}p->f_bounds()[0] = {{n, 0}};
{'\t'}auto q = reinterpret_cast<char*>(p) + a;
{'\t'}for (size_t i = 0; i < n; ++i) {{
{'\t'}{'\t'}std::memcpy(q, a_0->v__fields[i]->f_address(nullptr), a_0->v__size);
{'\t'}{'\t'}q += a_0->v__size;
{'\t'}}}
{'\t'}a_0->v__szarray->f_finish(p);
{'\t'}return p;
", 0);
                }
            );
            // TODO
            code.For(
                type.GetMethod(nameof(Type.GetField), new[] { get(typeof(string)), get(typeof(BindingFlags)) }),
                transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckArgumentNull("a_1") + $@"{'\t'}std::u16string_view name = {{&a_1->v__5ffirstChar, static_cast<size_t>(a_1->v__5fstringLength)}};
{'\t'}for (auto p = a_0->v__fields; *p; ++p) if ((*p)->v__name == name) return *p;
{'\t'}return {{}};
", 0)
            );
            code.For(
                type.GetMethod(nameof(Type.GetGenericArguments)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_0->v__generic_type_definition) throw std::runtime_error(""not generic type"");
{'\t'}size_t n = 0;
{'\t'}for (auto p = a_0->v__generic_arguments; *p; ++p) ++n;
{'\t'}auto p = f__new_array<{transpiler.Escape(get(typeof(Type[])))}, {transpiler.Escape(get(typeof(Type)))}>(n);
{'\t'}for (size_t i = 0; i < n; ++i) p->f_data()[i] = a_0->v__generic_arguments[i];
{'\t'}return p;
", 0)
            );
            code.For(
                type.GetMethod(nameof(Type.GetGenericTypeDefinition)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_0->v__generic_type_definition) throw std::runtime_error(""not generic type"");
{'\t'}return a_0->v__generic_type_definition;
", 0)
            );
            code.For(
                type.GetMethod(nameof(Type.GetInterfaces)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}auto p = f__new_array<{transpiler.Escape(get(typeof(Type[])))}, {transpiler.Escape(get(typeof(Type)))}>(a_0->v__interface_to_methods.size());
{'\t'}size_t i = 0;
{'\t'}for (const auto& x : a_0->v__interface_to_methods) p->f_data()[i++] = x.first;
{'\t'}return p;
", 0)
            );
            code.For(
                type.GetMethod(nameof(Type.IsAssignableFrom)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_1) return false;
{'\t'}auto p = static_cast<t__type*>(a_1);
{'\t'}return p->f_is(a_0) || p->f_implementation(a_0) || a_0->v__nullable_value == p;
", 0)
            );
            code.For(
                type.GetProperty(nameof(Type.IsByRefLike)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__by_ref_like;\n", 0)
            );
            code.For(
                type.GetProperty(nameof(Type.IsConstructedGenericType)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__generic_type_definition && a_0->v__generic_type_definition != a_0;\n", 0)
            );
            code.For(
                type.GetProperty(nameof(Type.IsGenericTypeDefinition)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__generic_type_definition && a_0->v__generic_type_definition == a_0;\n", 0)
            );
            code.For(
                type.GetMethod(nameof(Type.MakeGenericType)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckArgumentNull("a_1") + $@"{'\t'}if (!a_0->v__generic_type_definition) throw std::runtime_error(""not generic type"");
{'\t'}size_t n = 0;
{'\t'}for (auto p = a_0->v__generic_arguments; *p; ++p) ++n;
{'\t'}if (a_1->v__length != n) throw std::runtime_error(""not same number of types"");
{'\t'}for (auto p = a_0->v__constructed_generic_types; *p; ++p) {{
{'\t'}{'\t'}auto q = (*p)->v__generic_arguments;
{'\t'}{'\t'}if (std::equal(q, q + n, a_1->f_data())) return *p;
{'\t'}}}
{'\t'}throw std::runtime_error(""not supported"");
", 0)
            );
            code.For(
                type.GetProperty(nameof(Type.Namespace)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->v__namespace);\n", 0)
            );
            code.For(
                type.GetMethod(nameof(object.ToString)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->v__display_name);\n", 0)
            );
            code.For(
                type.GetProperty(nameof(Type.TypeHandle)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn {a_0};\n", 0)
            );
            code.For(
                type.GetMethod("HasElementTypeImpl", declaredAndInstance),
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__has_element_type;\n", 0)
            );
            code.For(
                type.GetMethod("IsArrayImpl", declaredAndInstance),
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__array;\n", 0)
            );
            code.For(
                type.GetMethod("IsPointerImpl", declaredAndInstance),
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__pointer;\n", 0)
            );
            code.For(
                type.GetMethod("GetAttributeFlagsImpl", declaredAndInstance),
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__attribute_flags;\n", 0)
            );
            code.For(
                type.GetMethod("GetConstructorImpl", declaredAndInstance),
                transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckArgumentNull("a_4") + "\treturn a_4->v__length > 0 ? nullptr : a_0->v__default_constructor;\n", 0)
            );
        });
    }
}
