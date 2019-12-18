using System;
using System.Diagnostics;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemDiagnostics(this Builtin @this) => @this
        .For(typeof(Debugger), (type, code) =>
        {
            code.For(
                type.GetProperty(nameof(Debugger.IsAttached)).GetMethod,
                transpiler => "\treturn false;\n"
            );
            code.For(
                type.GetMethod(nameof(Debugger.Log)),
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod(nameof(Debugger.NotifyOfCrossThreadDependency)),
                transpiler => string.Empty
            );
        })
        .For(Type.GetType("System.Diagnostics.Tracing.EventPipeEventDispatcher"), (type, code) =>
        {
            code.For(
                type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null),
                transpiler => $"\treturn f__new_zerod<{transpiler.Escape(type)}>();\n"
            );
            code.For(
                type.GetMethod("RemoveEventListener", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod("SendCommand", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => string.Empty
            );
        })
        .For(Type.GetType("System.Diagnostics.Tracing.FrameworkEventSource"), (type, code) =>
        {
            code.For(
                type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null),
                transpiler => $"\treturn f__new_zerod<{transpiler.Escape(type)}>();\n"
            );
            code.For(
                type.GetMethod("ThreadPoolEnqueueWorkObject"),
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod("ThreadTransferSendObj"),
                transpiler => string.Empty
            );
        });
    }
}
