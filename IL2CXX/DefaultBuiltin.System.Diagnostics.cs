using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;

namespace IL2CXX
{
    // TODO
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemDiagnostics(this Builtin @this) => @this
        .For(typeof(Debug), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(Debug.Assert), new[] { typeof(bool) }),
                transpiler => ("\tif (!a_0) throw std::logic_error(\"Debug.Assert failed.\");\n", 0)
            );
        })
        .For(typeof(Debugger), (type, code) =>
        {
            code.For(
                type.GetProperty(nameof(Debugger.IsAttached)).GetMethod,
                transpiler => ("\treturn false;\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Debugger.Log)),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod(nameof(Debugger.NotifyOfCrossThreadDependency)),
                transpiler => (string.Empty, 0)
            );
        })
        .For(Type.GetType("System.Diagnostics.Tracing.EventPipeEventDispatcher"), (type, code) =>
        {
            code.For(
                type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null),
                transpiler => ($"\treturn f__new_zerod<{transpiler.Escape(type)}>();\n", 0)
            );
            code.For(
                type.GetMethod("RemoveEventListener", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod("SendCommand", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => (string.Empty, 0)
            );
        })
        .For(typeof(EventSource), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(EventSource.GetGuid)),
                transpiler => ("\treturn {};\n", 0)
            );
            code.For(
                type.GetMethod("GetCustomAttributeHelper", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn {};\n", 0)
            );
            code.For(
                type.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod("ReportOutOfBandMessage", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod("SendCommand", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod(nameof(EventSource.SetCurrentThreadActivityId), new[] { typeof(Guid) }),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod(nameof(EventSource.SetCurrentThreadActivityId), new[] { typeof(Guid), typeof(Guid).MakeByRefType() }),
                transpiler => ("\t*a_1 = {};\n", 0)
            );
            code.For(
                type.GetMethod("WriteEventCore", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod("WriteEventWithRelatedActivityIdCore", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => (string.Empty, 0)
            );
        })
        .For(Type.GetType("System.Diagnostics.Tracing.FrameworkEventSource"), (type, code) =>
        {
            code.For(
                type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null),
                transpiler => ($"\treturn f__new_zerod<{transpiler.Escape(type)}>();\n", 0)
            );
            code.For(
                type.GetMethod("ThreadPoolEnqueueWorkObject"),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod("ThreadTransferSendObj"),
                transpiler => (string.Empty, 0)
            );
        });
    }
}
