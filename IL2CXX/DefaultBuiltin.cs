using System.Reflection;

namespace IL2CXX;

public static partial class DefaultBuiltin
{
    private const BindingFlags declaredAndInstance = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static IEnumerable<MethodBase> GenericMethods(Type type) => type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(x => x.IsGenericMethodDefinition);

    public static Builtin Create(Func<Type, Type> get, PlatformID target) => new Builtin
    {
        TypeNameToMethodNameToBody =
        {
            ["System.SR"] = new()
            {
                ["System.String GetResourceString(System.String)"] = (transpiler, method) => ("\treturn a_0;\n", 0),
                ["System.Boolean UsingResourceKeys()"] = (transpiler, method) => ("\treturn false;\n", 1)
            }
        },
        MethodNameToBody =
        {
            ["System.Boolean get_IsSupported()"] = (transpiler, method) => method.DeclaringType.Namespace == "System.Runtime.Intrinsics.X86" ? ("\treturn false;\n", 1) : default
        }
    }
    .SetupInterop(get, target)
    .SetupSystem(get)
    .SetupSystemBuffers(get)
    .SetupSystemCollections(get)
    .SetupSystemDiagnostics(get)
    .SetupSystemNumerics(get)
    .SetupSystemReflection(get, target)
    .SetupSystemResources(get)
    .SetupSystemRuntime(get)
    .SetupSystemText(get)
    .SetupSystemThreading(get, target)
    .SetupRuntime(get);
}
