using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IL2CXX
{
    public static partial class DefaultBuiltin
    {
        private const BindingFlags declaredAndInstance = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static IEnumerable<MethodBase> GenericMethods(Type type) => type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(x => x.IsGenericMethodDefinition);

        public static Builtin Create() => new Builtin {
            TypeNameToMethodNameToBody = {
                ["System.SR"] = new Dictionary<string, Func<Transpiler, MethodBase, (string body, bool inline)>> {
                    ["System.String GetResourceString(System.String, System.String)"] = (transpiler, method) => ("\treturn a_0;\n", false)
                }
            },
            MethodNameToBody = {
                ["System.String ToString(System.String, System.IFormatProvider)"] = (transpiler, method) => ($"\treturn f__new_string(u\"{method.ReflectedType}\"sv);\n", false),
                ["Boolean TryFormat(System.Span`1[System.Char], Int32 ByRef, System.ReadOnlySpan`1[System.Char], System.IFormatProvider)"] = (transpiler, method) => ($@"{'\t'}*a_2 = 0;
{'\t'}return false;
", false),
                ["Boolean System.ISpanFormattable.TryFormat(System.Span`1[System.Char], Int32 ByRef, System.ReadOnlySpan`1[System.Char], System.IFormatProvider)"] = (transpiler, method) => ($@"{'\t'}*a_2 = 0;
{'\t'}return false;
", false),
                ["Boolean get_IsSupported()"] = (transpiler, method) => method.DeclaringType.Namespace == "System.Runtime.Intrinsics.X86" ? ("\treturn false;\n", false) : default,
            }
        }
        .SetupSystem()
        .SetupSystemBuffers()
        .SetupSystemCollections()
        .SetupSystemDiagnostics()
        .SetupSystemReflection()
        .SetupSystemResources()
        .SetupSystemRuntime()
        .SetupSystemText()
        .SetupSystemThreading();
    }
}
