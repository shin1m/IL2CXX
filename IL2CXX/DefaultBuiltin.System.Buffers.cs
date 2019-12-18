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
                type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null),
                transpiler => $"\treturn f__new_zerod<{transpiler.Escape(type)}>();\n"
            );
            code.For(
                type.GetMethod("BufferAllocated", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod("BufferRented", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod("BufferReturned", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod("BufferTrimPoll", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod("BufferTrimmed", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => string.Empty
            );
        });
    }
}
