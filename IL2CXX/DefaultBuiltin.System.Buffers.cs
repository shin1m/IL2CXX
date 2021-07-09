using System;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemBuffers(this Builtin @this) => @this
        .For(Type.GetType("System.Buffers.ArrayPoolEventSource"), (type, code) =>
        {
            code.For(
                type.GetConstructor(declaredAndInstance, null, Type.EmptyTypes, null),
                transpiler => ($"\treturn f__new_zerod<{transpiler.Escape(type)}>();\n", 0)
            );
            code.For(
                type.GetMethod("BufferAllocated", declaredAndInstance),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod("BufferRented", declaredAndInstance),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod("BufferReturned", declaredAndInstance),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod("BufferTrimPoll", declaredAndInstance),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod("BufferTrimmed", declaredAndInstance),
                transpiler => (string.Empty, 0)
            );
        });
    }
}
