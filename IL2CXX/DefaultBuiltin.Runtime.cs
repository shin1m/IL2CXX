using System;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupRuntime(this Builtin @this) => @this
        .For(typeof(RuntimeType), (type, code) =>
        {
            code.For(
                type.GetProperty(nameof(Type.BaseType)).GetMethod,
                transpiler => ("\treturn a_0->v__base;\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Type.IsAssignableFrom)),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_1) return false;
{'\t'}auto p = static_cast<t__type*>(a_0);
{'\t'}auto q = static_cast<t__type*>(a_1);
{'\t'}return q->f__is(p) || q->f__implementation(p) || p->v__nullable_value == q;
", 1)
            );
            code.For(
                type.GetProperty(nameof(Type.TypeHandle)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn {a_0};\n", 1)
            );
        });
    }
}
