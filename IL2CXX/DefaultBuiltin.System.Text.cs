using System;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemText(this Builtin @this) => @this
        .For(typeof(Regex), (type, code) =>
        {
            code.For(
                type.GetMethod("UseOptionC", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => ("\treturn false;\n", 0)
            );
            code.For(
                type.GetMethod("Compile", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(typeof(StringBuilder), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(object.ToString), Type.EmptyTypes),
                transpiler => default
            );
        })
        .For(Type.GetType("System.Text.ValueStringBuilder, System.Private.CoreLib"), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(object.ToString), Type.EmptyTypes),
                transpiler => default
            );
        })
        .For(Type.GetType("System.Text.ValueStringBuilder, System.Text.RegularExpressions"), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(object.ToString), Type.EmptyTypes),
                transpiler => default
            );
        });
    }
}
