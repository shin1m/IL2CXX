using System;
using System.Text;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemText(this Builtin @this) => @this
        .For(typeof(StringBuilder), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(object.ToString), Type.EmptyTypes),
                transpiler => null
            );
        });
    }
}
