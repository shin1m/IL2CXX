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
                transpiler => ("\treturn a_0->v__entry_point;\n", 0)
            );
            code.For(
                type.GetProperty(nameof(Assembly.FullName)).GetMethod,
                transpiler => ("\treturn f__new_string(a_0->v__full_name);\n", 0)
            );
            code.For(
                type.GetProperty(nameof(RuntimeAssembly.Name)).GetMethod,
                transpiler => ("\treturn f__new_string(a_0->v__name);\n", 1)
            );
        })
        .For(typeof(RuntimeConstructorInfo), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(MethodBase.Invoke), new[] { typeof(BindingFlags), typeof(Binder), typeof(object[]), typeof(CultureInfo) }),
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__invoke(a_3);\n", 0)
            );
        })
        .For(typeof(RuntimeMethodInfo), (type, code) =>
        {
            code.For(
                type.GetProperty(nameof(MemberInfo.DeclaringType)).GetMethod,
                transpiler => ("\treturn a_0->v__declaring_type;\n", 0)
            );
        })
        .For(typeof(RuntimeType), (type, code) =>
        {
            code.For(
                type.GetProperty(nameof(Type.Assembly)).GetMethod,
                transpiler => ("\treturn a_0->v__assembly;\n", 0)
            );
            code.For(
                type.GetProperty(nameof(Type.BaseType)).GetMethod,
                transpiler => ("\treturn a_0->v__base;\n", 0)
            );
            code.For(
                type.GetMethod(nameof(Type.IsAssignableFrom)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_1) return false;
{'\t'}auto p = static_cast<t__type*>(a_0);
{'\t'}auto q = static_cast<t__type*>(a_1);
{'\t'}return q->f__is(p) || q->f__implementation(p) || p->v__nullable_value == q;
", 0)
            );
            code.For(
                type.GetMethod("IsArrayImpl", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler =>
                {
                    var array = $"&t__type_of<{transpiler.Escape(typeof(Array))}>::v__instance";
                    return (transpiler.GenerateCheckNull("a_0") + $"\treturn a_0 != {array} && a_0->f__is({array});\n", 0);
                }
            );
            code.For(
                type.GetMethod("GetConstructorImpl", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckArgumentNull("a_4") + "\treturn a_4->v__length > 0 ? nullptr : a_0->v__default_constructor;\n", 0)
            );
            code.For(
                type.GetProperty(nameof(Type.Namespace)).GetMethod,
                transpiler => ("\treturn f__new_string(a_0->v__namespace);\n", 0)
            );
            code.For(
                type.GetProperty(nameof(MemberInfo.Name)).GetMethod,
                transpiler => ("\treturn f__new_string(a_0->v__name);\n", 0)
            );
            code.For(
                type.GetMethod(nameof(object.ToString)),
                transpiler => ("\treturn f__new_string(a_0->v__display_name);\n", 0)
            );
            code.For(
                type.GetProperty(nameof(Type.TypeHandle)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn {a_0};\n", 0)
            );
        });
    }
}
