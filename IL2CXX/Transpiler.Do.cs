﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace IL2CXX
{
    using static MethodKey;

    partial class Transpiler
    {
        private readonly StringWriter functionDeclarations = new StringWriter();
        private readonly Queue<Type> queuedTypes = new Queue<Type>();
        private readonly HashSet<MethodKey> visitedMethods = new HashSet<MethodKey>();
        private readonly Queue<MethodBase> queuedMethods = new Queue<MethodBase>();
        private MethodBase method;
        private byte[] bytes;
        private SortedDictionary<string, (string Prefix, int Index)> definedIndices;
        private Dictionary<int, Stack> indexToStack;
        private TextWriter writer;
        private readonly Stack<ExceptionHandlingClause> tries = new Stack<ExceptionHandlingClause>();
        private Type constrained;
        private bool @volatile;

        private void ProcessNextMethod(Func<Type, bool, TextWriter> writerForType)
        {
            method = queuedMethods.Dequeue();
            var key = ToKey(method);
            if (!visitedMethods.Add(key) || method.IsAbstract) return;
            var builtin = this.builtin.GetBody(this, method);
            var description = new StringWriter();
            description.Write($@"
// {method.DeclaringType.AssemblyQualifiedName}
// {method}
// {(method.IsPublic ? "public " : string.Empty)}{(method.IsPrivate ? "private " : string.Empty)}{(method.IsStatic ? "static " : string.Empty)}{(method.IsFinal ? "final " : string.Empty)}{(method.IsVirtual ? "virtual " : string.Empty)}{method.MethodImplementationFlags}");
            string attributes(string prefix, ICustomAttributeProvider cap) => string.Join(string.Empty, cap.GetCustomAttributes(false).Select(x => $"\n{prefix}// [{x}]"));
            var parameters = method.GetParameters().Select(x => (
                Prefix: $"{attributes("\t", x)}\n\t// {x}", Type: x.ParameterType
            ));
            if (!method.IsStatic && !(method.IsConstructor && builtin.body != null)) parameters = parameters.Prepend((string.Empty, GetThisType(method)));
            string argument(Type t, int i) => $"\n\t{EscapeForStacked(t)} a_{i}";
            var arguments = parameters.Select((x, i) => $"{x.Prefix}{argument(x.Type, i)}").ToList();
            if (method is MethodInfo) description.Write(attributes(string.Empty, ((MethodInfo)method).ReturnParameter));
            var returns = method is MethodInfo m ? EscapeForStacked(m.ReturnType) : method.IsStatic || builtin.body == null ? "void" : EscapeForStacked(method.DeclaringType);
            var identifier = Escape(method);
            var prototype = $@"{returns}
{identifier}({string.Join(",", arguments)}
)";
            functionDeclarations.WriteLine(description);
            functionDeclarations.Write(prototype);
            functionDeclarations.WriteLine(';');
            if (method.DeclaringType.IsValueType && !method.IsStatic && !method.IsConstructor) functionDeclarations.WriteLine($@"
inline {returns}
{identifier}__v({string.Join(",", arguments.Skip(1).Prepend($"\n\t{Escape(method.DeclaringType)}* a_0"))}
)
{{
{'\t'}{(returns == "void" ? string.Empty : "return ")}{identifier}({
    string.Join(", ", arguments.Skip(1).Select((x, i) => $"a_{i + 1}").Prepend($"&a_0->v__value"))
});
}}");
            if (builtin.body != null)
            {
                writer = writerForType(method.DeclaringType, builtin.inline > 0);
                writer.WriteLine(description);
                if (builtin.inline < 0)
                {
                    writer.Write("IL2CXX__PORTABLE__NOINLINE ");
                }
                else
                {
                    if (builtin.inline > 1) writer.Write("IL2CXX__PORTABLE__ALWAYS_INLINE ");
                    if (builtin.inline > 0) writer.Write("inline ");
                }
                writer.WriteLine($"{prototype}\n{{\n{builtin.body}}}");
                return;
            }
            var body = method.GetMethodBody();
            bytes = body?.GetILAsByteArray();
            if (method.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining))
            {
                writer = writerForType(method.DeclaringType, false);
                writer.WriteLine(description);
                writer.Write("IL2CXX__PORTABLE__NOINLINE ");
            }
            else
            {
                var aggressive = method.MethodImplementationFlags.HasFlag(MethodImplAttributes.AggressiveInlining);
                var inline = aggressive || bytes?.Length <= 64;
                writer = writerForType(method.DeclaringType, inline);
                writer.WriteLine(description);
                if (aggressive) writer.Write("IL2CXX__PORTABLE__ALWAYS_INLINE ");
                if (inline) writer.Write("inline ");
            }
            var dllimport = method.GetCustomAttribute<DllImportAttribute>();
            if (dllimport != null)
            {
                functionDeclarations.WriteLine("// DLL import:");
                functionDeclarations.WriteLine($"//\tValue: {dllimport.Value}");
                functionDeclarations.WriteLine($"//\tEntryPoint: {dllimport.EntryPoint}");
                functionDeclarations.WriteLine($"//\tSetLastError: {dllimport.SetLastError}");
                writer.WriteLine(prototype);
                writer.WriteLine('{');
                writer.WriteLine($"\tstatic t_library library(\"{dllimport.Value}\"s, \"{dllimport.EntryPoint ?? method.Name}\");");
                GenerateInvokeUnmanaged(GetReturnType(method), method.GetParameters().Select((x, i) => (x, i)), "library.f_symbol()", writer, dllimport.CharSet);
                writer.WriteLine('}');
                return;
            }
            if (bytes == null)
            {
                functionDeclarations.WriteLine("// TO BE PROVIDED");
                return;
            }
            writer.Write(prototype);
            writer.WriteLine($@"
{{{string.Join(string.Empty, body.ExceptionHandlingClauses.Select(x => $"\n\t// {x}"))}");
            definedIndices = new SortedDictionary<string, (string, int)>();
            indexToStack = new Dictionary<int, Stack>();
            log($"{method.DeclaringType}::[{method}]");
            foreach (var x in body.ExceptionHandlingClauses)
            {
                log($"{x.Flags}");
                log($"\ttry: {x.TryOffset:x04} to {x.TryOffset + x.TryLength:x04}");
                log($"\thandler: {x.HandlerOffset:x04} to {x.HandlerOffset + x.HandlerLength:x04}");
                switch (x.Flags)
                {
                    case ExceptionHandlingClauseOptions.Clause:
                        log($"\tcatch: {x.CatchType}");
                        break;
                    case ExceptionHandlingClauseOptions.Filter:
                        log($"\tfilter: {x.FilterOffset:x04}");
                        break;
                }
            }
            Estimate(0, new Stack(this));
            foreach (var x in body.ExceptionHandlingClauses)
                switch (x.Flags)
                {
                    case ExceptionHandlingClauseOptions.Clause:
                        Estimate(x.HandlerOffset, new Stack(this).Push(x.CatchType));
                        break;
                    case ExceptionHandlingClauseOptions.Filter:
                        Estimate(x.FilterOffset, new Stack(this).Push(typeof(Exception)));
                        break;
                    default:
                        Estimate(x.HandlerOffset, new Stack(this));
                        break;
                }
            log("\n");
            writer.WriteLine($"\t// init locals: {body.InitLocals}");
            foreach (var x in body.LocalVariables)
                writer.WriteLine($"\t{EscapeForStacked(x.LocalType)} l{x.LocalIndex}{(body.InitLocals ? "{}" : string.Empty)};");
            foreach (var x in definedIndices)
                for (var i = 0; i < x.Value.Index; ++i)
                    writer.WriteLine($"\t{x.Key} {x.Value.Prefix}{i};");
            var writers = new Stack<TextWriter>();
            var tryBegins = new Queue<ExceptionHandlingClause>(body.ExceptionHandlingClauses.OrderBy(x => x.TryOffset).ThenByDescending(x => x.HandlerOffset + x.HandlerLength));
            var index = 0;
            while (index < bytes.Length)
            {
                while (tryBegins.Count > 0)
                {
                    var clause = tryBegins.Peek();
                    if (index < clause.TryOffset) break;
                    tryBegins.Dequeue();
                    tries.Push(clause);
                    if (clause.Flags == ExceptionHandlingClauseOptions.Finally)
                    {
                        writer.WriteLine("{auto finally = f__finally([&]\n{");
                        writers.Push(writer);
                        writer = new StringWriter();
                        writer.WriteLine("});");
                    }
                    else
                    {
                        writer.WriteLine("try {");
                    }
                }
                if (tries.Count > 0)
                {
                    var clause = tries.Peek();
                    if (index == (clause.Flags == ExceptionHandlingClauseOptions.Filter ? clause.FilterOffset : clause.HandlerOffset))
                    {
                        var s = indexToStack[index];
                        switch (clause.Flags)
                        {
                            case ExceptionHandlingClauseOptions.Clause:
                                writer.WriteLine($@"// catch {clause.CatchType}
}} catch (t_object* e) {{
{'\t'}if (!(e && e->f_type()->{(clause.CatchType.IsInterface ? "f__implementation" : "f__is")}(&t__type_of<{Escape(clause.CatchType)}>::v__instance))) throw;
{'\t'}{s.Variable} = e;");
                                break;
                            case ExceptionHandlingClauseOptions.Filter:
                                writer.WriteLine($@"// filter
}} catch (t_object* e) {{
{'\t'}{s.Variable} = e;");
                                break;
                            case ExceptionHandlingClauseOptions.Finally:
                                writers.Push(writer);
                                writer = new StringWriter();
                                break;
                            case ExceptionHandlingClauseOptions.Fault:
                                writer.WriteLine(@"// fault
} catch (...) {");
                                break;
                        }
                    }
                }
                writer.Write($"L_{index:x04}: // ");
                var stack = indexToStack[index];
                var instruction = instructions1[bytes[index++]];
                if (instruction.OpCode == OpCodes.Prefix1) instruction = instructions2[bytes[index++]];
                writer.Write(instruction.OpCode.Name);
                index = instruction.Generate(index, stack);
                if (tries.Count > 0)
                {
                    var clause = tries.Peek();
                    if (index >= clause.HandlerOffset + clause.HandlerLength)
                    {
                        tries.Pop();
                        if (clause.Flags == ExceptionHandlingClauseOptions.Finally)
                        {
                            var f = writer;
                            var t = writers.Pop();
                            writer = writers.Pop();
                            writer.Write(f);
                            writer.Write(t);
                        }
                        writer.WriteLine('}');
                    }
                }
            }
            writer.WriteLine('}');
        }

        public void Do(MethodInfo method, TextWriter writerForDeclarations, TextWriter writerForDefinitions, Func<Type, bool, TextWriter> writerForType, string resources)
        {
            Define(typeof(RuntimeAssembly));
            Define(typeof(RuntimeConstructorInfo));
            Define(typeof(RuntimeMethodInfo));
            Define(typeof(RuntimeType));
            Escape(finalizeOfObject);
            Define(typeof(Thread));
            Enqueue(typeof(ThreadStart).GetMethod("Invoke"));
            Enqueue(typeof(ParameterizedThreadStart).GetMethod("Invoke"));
            Define(typeof(void));
            Define(typeof(string));
            Define(typeof(StringBuilder));
            Define(method.DeclaringType);
            Enqueue(method);
            do
            {
                ProcessNextMethod(writerForType);
                while (queuedTypes.Count > 0) Define(queuedTypes.Dequeue());
            }
            while (queuedMethods.Count > 0);
            processed = true;
            writerForDeclarations.WriteLine(@"#include <il2cxx/base.h>

namespace il2cxx
{
");
            writerForDeclarations.Write(typeDeclarations);
            writerForDeclarations.Write(typeDefinitions);
            writerForDeclarations.WriteLine(@"
extern t__runtime_assembly* const v__entry_assembly;

extern std::map<std::string_view, t__type*> v__name_to_type;");
            writerForDeclarations.Write(functionDeclarations);
            var assemblyIdentifiers = new HashSet<string>();
            var assemblyToIdentifier = new Dictionary<Assembly, string>();
            foreach (var definition in runtimeDefinitions)
            {
                var writer = writerForType(definition.Type, false);
                var assembly = definition.Type.Assembly;
                if (!assemblyToIdentifier.TryGetValue(assembly, out var name))
                {
                    var escaped = name = Escape(assembly.GetName().Name);
                    for (var i = 0; !assemblyIdentifiers.Add(name); ++i) name = $"{escaped}__{i}";
                    assemblyToIdentifier.Add(assembly, name);
                    var entry = assembly.EntryPoint;
                    if (entry == method)
                    {
                        writerForDeclarations.WriteLine($"\nextern t__runtime_method_info v__assembly_{name}__entry_point;");
                        writer.WriteLine($"\nt__runtime_method_info v__assembly_{name}__entry_point{{&t__type_of<t__runtime_method_info>::v__instance, &t__type_of<{Escape(entry.DeclaringType)}>::v__instance}};");
                    }
                    writerForDeclarations.WriteLine($"\nextern t__runtime_assembly v__assembly_{name};");
                    writer.WriteLine($"\nt__runtime_assembly v__assembly_{name}{{&t__type_of<t__runtime_assembly>::v__instance, u\"{assembly.FullName}\"sv, u\"{name}\"sv, {(entry == method ? $"&v__assembly_{name}__entry_point" : "nullptr")}}};");
                    var names = assembly.GetManifestResourceNames();
                    if (names.Length > 0)
                    {
                        var path = Path.Combine(resources, name);
                        Directory.CreateDirectory(path);
                        foreach (var x in names)
                            using (var source = assembly.GetManifestResourceStream(x))
                            using (var destination = File.Create(Path.Combine(path, x)))
                                source.CopyTo(destination);
                    }
                }
                WriteRuntimeDefinition(definition, $"v__assembly_{name}", writerForDeclarations, writer);
            }
            writerForDeclarations.WriteLine("\n#include \"utilities.h\"");
            writerForDeclarations.Write(staticDefinitions);
            writerForDeclarations.WriteLine(@"
struct t_static
{");
            writerForDeclarations.Write(staticMembers);
            writerForDeclarations.WriteLine($@"
{'\t'}static t_static* v_instance;
{'\t'}t_static()
{'\t'}{{
{'\t'}{'\t'}v_instance = this;
{'\t'}}}
{'\t'}~t_static()
{'\t'}{{
{'\t'}{'\t'}v_instance = nullptr;
{'\t'}}}
}};

struct t_thread_static
{{");
            writerForDeclarations.Write(threadStaticMembers);
            writerForDeclarations.WriteLine($@"
{'\t'}static IL2CXX__PORTABLE__THREAD t_thread_static* v_instance;
{'\t'}t_thread_static()
{'\t'}{{
{'\t'}{'\t'}v_instance = this;
{'\t'}}}
{'\t'}~t_thread_static()
{'\t'}{{
{'\t'}{'\t'}v_instance = nullptr;
{'\t'}}}
}};
");
            writerForDeclarations.WriteLine(fieldDeclarations);
            writerForDeclarations.WriteLine('}');
            writerForDefinitions.WriteLine($@"namespace il2cxx
{{

#include ""utilities.cc""

t__runtime_assembly* const v__entry_assembly = &v__assembly_{assemblyToIdentifier[method.DeclaringType.Assembly]};

std::map<std::string_view, t__type*> v__name_to_type{{
{string.Join(",\n", runtimeDefinitions.Select(x => $"\t{{\"{x.Type.AssemblyQualifiedName}\"sv, &t__type_of<{Escape(x.Type)}>::v__instance}}"))}
}};
");
            writerForDefinitions.Write(fieldDefinitions);
            writerForDefinitions.WriteLine($@"
t_static* t_static::v_instance;

IL2CXX__PORTABLE__THREAD t_thread_static* t_thread_static::v_instance;

}}

int main(int argc, char* argv[])
{{
{'\t'}using namespace il2cxx;
{'\t'}std::setlocale(LC_ALL, """");
{'\t'}t_engine::t_options options;
{'\t'}options.v_verbose = std::getenv(""IL2CXX_VERBOSE"");
{'\t'}options.v_verify = std::getenv(""IL2CXX_VERIFY_LEAKS"");
{'\t'}t_engine engine(options, argc, argv);
{'\t'}return engine.f_run<{Escape(typeof(Thread))}, t_static, t_thread_static>([](auto a_p)
{'\t'}{{
{'\t'}{'\t'}reinterpret_cast<void(*)(t_object*)>(reinterpret_cast<void**>(a_p->f_type() + 1)[{typeToRuntime[typeof(object)].GetIndex(finalizeOfObject)}])(a_p);
{'\t'}}}, []
{'\t'}{{
{'\t'}{'\t'}{(method.ReturnType == typeof(void) ? $"{Escape(method)}();\n\t\treturn 0" : $"return {Escape(method)}()")};
{'\t'}}});
}}");
        }
    }
}
