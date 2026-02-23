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
            transpiler =>
            {
                var cai = type.GetMethod("CommonAssemblyInit", declaredAndInstance)!;
                transpiler.Enqueue(cai);
                return (transpiler.GenerateCheckArgumentNull("a_0") + transpiler.GenerateCheckArgumentNull("a_1") + $@"{'\t'}auto RECYCLONE__SPILL p = f__new_zeroed<{transpiler.Escape(type)}>();
{'\t'}p->v_MainAssembly = a_1;
{'\t'}p->v_BaseNameField = a_0;
{'\t'}{transpiler.Escape(cai)}(p);
{'\t'}return p;
", 0);
            }
        );
    })
    // TODO
    .For(get(typeof(ResourceReader)), (type, code) =>
    {
        code.For(
            type.GetMethod("_LoadObjectV1", declaredAndInstance),
            transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
        );
        code.For(
            type.GetMethod("CreateBinaryFormatter", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
        );
        code.For(
            type.GetMethod("Deserialize", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
        );
    })
    // TODO
    .For(get(Type.GetType("System.Resources.ManifestBasedResourceGroveler", true)!), (type, code) =>
    {
        code.For(
            type.GetMethod("InternalGetSatelliteAssembly", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\treturn nullptr;\n", 0)
        );
    });
}
