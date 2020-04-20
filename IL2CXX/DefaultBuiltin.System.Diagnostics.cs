using System;
using System.Diagnostics;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemDiagnostics(this Builtin @this) => @this
        .For(typeof(Debug), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(Debug.Assert), new[] { typeof(bool) }),
                transpiler => ("\tif (!a_0) throw std::logic_error(\"Debug.Assert failed.\");\n", false)
            );
        })
        .For(typeof(Debugger), (type, code) =>
        {
            code.For(
                type.GetProperty(nameof(Debugger.IsAttached)).GetMethod,
                transpiler => ("\treturn false;\n", true)
            );
            code.For(
                type.GetMethod(nameof(Debugger.Log)),
                transpiler => (string.Empty, false)
            );
            code.For(
                type.GetMethod(nameof(Debugger.NotifyOfCrossThreadDependency)),
                transpiler => (string.Empty, false)
            );
        })
        .For(Type.GetType("System.Diagnostics.Tracing.EventPipeEventDispatcher"), (type, code) =>
        {
            code.For(
                type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null),
                transpiler => ($"\treturn f__new_zerod<{transpiler.Escape(type)}>();\n", false)
            );
            code.For(
                type.GetMethod("RemoveEventListener", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => (string.Empty, false)
            );
            code.For(
                type.GetMethod("SendCommand", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => (string.Empty, false)
            );
        })
        .For(Type.GetType("System.Diagnostics.Tracing.FrameworkEventSource"), (type, code) =>
        {
            code.For(
                type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null),
                transpiler => ($"\treturn f__new_zerod<{transpiler.Escape(type)}>();\n", false)
            );
            code.For(
                type.GetMethod("ThreadPoolEnqueueWorkObject"),
                transpiler => (string.Empty, false)
            );
            code.For(
                type.GetMethod("ThreadTransferSendObj"),
                transpiler => (string.Empty, false)
            );
        });
    }
}
