using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Text;

namespace IL2CXX
{
    // TODO
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemDiagnostics(this Builtin @this, Func<Type, Type> get) => @this
        .For(get(typeof(Debug)), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(Debug.Assert), new[] { get(typeof(bool)) }),
                transpiler => ("\tif (!a_0) throw std::logic_error(\"Debug.Assert failed.\");\n", 0)
            );
        })
        .For(get(typeof(Debugger)), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(Debugger.Break)),
                transpiler => (string.Empty, 1)
            );
            code.For(
                type.GetProperty(nameof(Debugger.IsAttached)).GetMethod,
                transpiler => ("\treturn false;\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Debugger.Log)),
                transpiler => (string.Empty, 1)
            );
            code.For(
                type.GetMethod(nameof(Debugger.NotifyOfCrossThreadDependency)),
                transpiler => (string.Empty, 1)
            );
        })
        .For(get(typeof(StackFrame)), (type, code) =>
        {
            // TODO
            code.For(
                type.GetMethod("BuildStackFrame", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => (string.Empty, 0)
            );
        })
        .For(get(typeof(StackTrace)), (type, code) =>
        {
            // TODO
            code.For(
                type.GetMethod("CaptureStackTrace", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => (string.Empty, 0)
            );
            // TODO
            code.For(
                type.GetMethod(nameof(ToString), BindingFlags.Instance | BindingFlags.NonPublic, new[] { type.GetNestedType("TraceFormat", BindingFlags.NonPublic), get(typeof(StringBuilder)) }),
                transpiler => (string.Empty, 0)
            );
        })
        .For(get(Type.GetType("System.Diagnostics.Tracing.EventPipeEventDispatcher")), (type, code) =>
        {
            code.For(
                type.GetConstructor(declaredAndInstance, null, Type.EmptyTypes, null),
                transpiler => ($"\treturn f__new_zerod<{transpiler.Escape(type)}>();\n", 0)
            );
            code.For(
                type.GetMethod("RemoveEventListener", declaredAndInstance),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod("SendCommand", declaredAndInstance),
                transpiler => (string.Empty, 0)
            );
        })
        .For(get(typeof(EventSource)), (type, code) =>
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
                type.GetProperty("IsSupported", BindingFlags.Static | BindingFlags.NonPublic).GetMethod,
                transpiler => ("\treturn false;\n", 0)
            );
            code.For(
                type.GetMethod("Initialize", declaredAndInstance),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod("ReportOutOfBandMessage", declaredAndInstance),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod("SendCommand", declaredAndInstance),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod(nameof(EventSource.SetCurrentThreadActivityId), new[] { get(typeof(Guid)) }),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod(nameof(EventSource.SetCurrentThreadActivityId), new[] { get(typeof(Guid)), get(typeof(Guid)).MakeByRefType() }),
                transpiler => ("\t*a_1 = {};\n", 0)
            );
            code.For(
                type.GetMethod("WriteEventCore", declaredAndInstance),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod("WriteEventWithRelatedActivityIdCore", declaredAndInstance),
                transpiler => (string.Empty, 0)
            );
        })
        .For(get(Type.GetType("System.Diagnostics.Tracing.FrameworkEventSource")), (type, code) =>
        {
            code.For(
                type.GetConstructor(declaredAndInstance, null, Type.EmptyTypes, null),
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
