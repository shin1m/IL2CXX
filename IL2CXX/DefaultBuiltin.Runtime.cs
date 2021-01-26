using System;
using System.Globalization;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupRuntime(this Builtin @this) => @this
        .For(typeof(RuntimeAssembly), (type, code) =>
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
        .For(typeof(RuntimeConstructorInfo), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(MethodBase.Invoke), new[] { typeof(BindingFlags), typeof(Binder), typeof(object[]), typeof(CultureInfo) }),
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__invoke();\n", 0)
            );
        })
        .For(typeof(RuntimeMethodInfo), (type, code) =>
        {
            code.For(
                type.GetProperty(nameof(MemberInfo.DeclaringType)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__declaring_type;\n", 0)
            );
        })
        .For(typeof(RuntimeType), (type, code) =>
        {
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
            code.For(
                type.GetMethod(nameof(Type.GetEnumNames)),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}auto type = static_cast<t__type*>(a_0);
{'\t'}if (!type->v__enum) throw std::runtime_error(""not enum"");
{'\t'}auto p = f__new_array<{transpiler.Escape(typeof(string[]))}, {transpiler.Escape(typeof(string))}>(type->v__enum_count);
{'\t'}for (size_t i = 0; i < type->v__enum_count; ++i) p->f_data()[i] = f__new_string(type->v__enum_pairs[i].second);
{'\t'}return p;
", 0)
            );
            code.For(
                type.GetMethod(nameof(Type.GetEnumValues)),
                transpiler =>
                {
                    var array = transpiler.Escape(typeof(Array));
                    return (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}auto type = static_cast<t__type*>(a_0);
{'\t'}if (!type->v__enum) throw std::runtime_error(""not enum"");
{'\t'}auto a = sizeof({array}) + sizeof({array}::t__bound);
{'\t'}auto n = type->v__enum_count;
{'\t'}auto p = static_cast<{array}*>(f_engine()->f_object__allocate(a + type->v__size * n));
{'\t'}p->v__length = n;
{'\t'}p->f_bounds()[0] = {{n, 0}};
{'\t'}auto copy = [&](auto q)
{'\t'}{{
{'\t'}{'\t'}for (size_t i = 0; i < n; ++i) q[i] = type->v__enum_pairs[i].first;
{'\t'}}};
{'\t'}switch (type->v__size) {{
{'\t'}case 1:
{'\t'}{'\t'}copy(reinterpret_cast<uint8_t*>(reinterpret_cast<char*>(p) + a));
{'\t'}{'\t'}break;
{'\t'}case 2:
{'\t'}{'\t'}copy(reinterpret_cast<uint16_t*>(reinterpret_cast<char*>(p) + a));
{'\t'}{'\t'}break;
{'\t'}case 4:
{'\t'}{'\t'}copy(reinterpret_cast<uint32_t*>(reinterpret_cast<char*>(p) + a));
{'\t'}{'\t'}break;
{'\t'}default:
{'\t'}{'\t'}copy(reinterpret_cast<uint64_t*>(reinterpret_cast<char*>(p) + a));
{'\t'}}}
{'\t'}type->v__szarray->f_finish(p);
{'\t'}return p;
", 0);
                }
            );
            code.For(
                type.GetMethod(nameof(Type.IsAssignableFrom)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_1) return false;
{'\t'}auto p = static_cast<t__type*>(a_0);
{'\t'}auto q = static_cast<t__type*>(a_1);
{'\t'}return q->f_is(p) || q->f_implementation(p) || p->v__nullable_value == q;
", 0)
            );
            code.For(
                type.GetMethod("IsArrayImpl", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler =>
                {
                    var array = $"&t__type_of<{transpiler.Escape(typeof(Array))}>::v__instance";
                    return (transpiler.GenerateCheckNull("a_0") + $"\treturn a_0 != {array} && a_0->f_is({array});\n", 0);
                }
            );
            code.For(
                type.GetMethod("GetConstructorImpl", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckArgumentNull("a_4") + "\treturn a_4->v__length > 0 ? nullptr : a_0->v__default_constructor;\n", 0)
            );
            code.For(
                type.GetProperty(nameof(Type.Namespace)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->v__namespace);\n", 0)
            );
            code.For(
                type.GetProperty(nameof(MemberInfo.Name)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->v__name);\n", 0)
            );
            code.For(
                type.GetMethod(nameof(object.ToString)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->v__display_name);\n", 0)
            );
            code.For(
                type.GetProperty(nameof(Type.TypeHandle)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn {a_0};\n", 0)
            );
        });
    }
}
