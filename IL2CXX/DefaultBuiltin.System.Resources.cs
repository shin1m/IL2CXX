using System;
using System.Reflection;
using System.Resources;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemResources(this Builtin @this) => @this
        .For(typeof(ResourceManager), (type, code) =>
        {
            code.For(
                type.GetConstructor(new[] { typeof(string), typeof(Assembly) }),
                transpiler => ($@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}return p;
", 0)
            );
        })
        // TODO
        .For(typeof(ResourceReader), (type, code) =>
        {
            code.For(
                type.GetMethod("_LoadObjectV1", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        // TODO
        .For(Type.GetType("System.Resources.RuntimeResourceSet"), (type, code) =>
        {
            code.For(
                type.GetMethod("GetString", new[] { typeof(string), typeof(bool) }),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        });
    }
}
