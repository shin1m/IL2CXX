using System.Reflection;
using System.Resources;

namespace IL2CXX;

partial class DefaultBuiltin
{
    private static Builtin SetupSystemResources(this Builtin @this, Func<Type, Type> get) => @this
    .For(get(typeof(ResourceManager)), (type, code) =>
    {
        code.For(
            type.GetConstructor([get(typeof(string)), get(typeof(Assembly))]),
            transpiler => ($@"{'\t'}auto RECYCLONE__SPILL p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}return p;
", 0)
        );
    })
    // TODO
    .For(get(typeof(ResourceReader)), (type, code) =>
    {
        code.For(
            type.GetMethod("_LoadObjectV1", declaredAndInstance),
            transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
        );
    })
    // TODO
    .For(get(Type.GetType("System.Resources.RuntimeResourceSet")), (type, code) =>
    {
        code.For(
            type.GetMethod("GetString", [get(typeof(string)), get(typeof(bool))]),
            transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
        );
    });
}
