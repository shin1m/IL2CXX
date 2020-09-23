using System;
using System.Numerics;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemNumerics(this Builtin @this) => @this
        .For(typeof(Vector<>), (type, code) =>
        {
            // TODO
            code.ForGeneric(
                type.GetMethod(nameof(object.ToString), Type.EmptyTypes),
                (transpiler, types) => ($"\treturn f__new_string(u\"{type.MakeGenericType(types)}\"sv);\n", 0)
            );
        })
        .For(Type.GetType("System.Numerics.BitOperations"), (type, code) =>
        {
            code.For(
                type.GetMethod("RotateLeft", new[] { typeof(uint), typeof(int) }),
                transpiler => ("\treturn (a_0 << (a_1 & 31)) | (a_0 >> ((32 - a_1) & 31));\n", 2)
            );
            code.For(
                type.GetMethod("RotateRight", new[] { typeof(uint), typeof(int) }),
                transpiler => ("\treturn (a_0 >> (a_1 & 31)) | (a_0 << ((32 - a_1) & 31));\n", 2)
            );
        });
    }
}
