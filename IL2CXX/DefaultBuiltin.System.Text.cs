using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemText(this Builtin @this, Func<Type, Type> get) => @this
        .For(get(typeof(Regex)), (type, code) =>
        {
            code.For(
                type.GetMethod("UseOptionC", declaredAndInstance),
                transpiler => ("\treturn false;\n", 0)
            );
            code.For(
                type.GetMethod("Compile", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        });
    }
}
