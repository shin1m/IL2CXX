using System;
using System.Reflection;
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
        });
    }
}
