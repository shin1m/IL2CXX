using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;

namespace IL2CXX
{
    using static MethodKey;

    partial class Transpiler
    {
        private readonly StringWriter functionDeclarations = new();
        private readonly Queue<Type> queuedTypes = new();
        private readonly HashSet<MethodKey> visitedMethods = new();
        private readonly Queue<MethodBase> queuedMethods = new();
        private MethodBase method;
        private byte[] bytes;
        private SortedDictionary<string, (string Prefix, int Index)> definedIndices;
        private bool hasReturn;
        private Dictionary<int, Stack> indexToStack;
        private TextWriter writer;
        private readonly Stack<ExceptionHandlingClause> tries = new();
        private Type constrained;
        private bool @volatile;

        private void ProcessNextMethod(Func<Type, bool, TextWriter> writerForType)
        {
            var key = ToKey(queuedMethods.Dequeue());
            method = key.Method;
            if (!visitedMethods.Add(key) || method.IsAbstract) return;
            var builtin = this.builtin.GetBody(this, key);
            var description = new StringWriter();
            description.Write($@"
// {method.DeclaringType.AssemblyQualifiedName}
// {method}
// {(method.IsPublic ? "public " : string.Empty)}{(method.IsPrivate ? "private " : string.Empty)}{(method.IsStatic ? "static " : string.Empty)}{(method.IsFinal ? "final " : string.Empty)}{(method.IsVirtual ? "virtual " : string.Empty)}{method.MethodImplementationFlags}");
            string attributes(string prefix, ParameterInfo pi) => string.Join(string.Empty, pi.GetCustomAttributesData().Select(x => $"\n{prefix}// [{x}]"));
            var parameters = method.GetParameters().Select(x => (
                Prefix: $"{attributes("\t", x)}\n\t// {x}", Type: x.ParameterType
            ));
            if (!method.IsStatic && !(method.IsConstructor && builtin.body != null)) parameters = parameters.Prepend((string.Empty, GetThisType(method)));
            string argument(Type t, int i) => $"\n\t{EscapeForArgument(t)} a_{i}";
            var arguments = parameters.Select((x, i) => $"{x.Prefix}{argument(x.Type, i)}").ToList();
            string returns;
            if (method is MethodInfo m)
            {
                description.Write(attributes(string.Empty, m.ReturnParameter));
                returns = EscapeForStacked(m.ReturnType);
            }
            else
            {
                returns = method.IsStatic || builtin.body == null ? "void" : EscapeForStacked(method.DeclaringType);
            }
            var identifier = Escape(method);
            var prototype = $@"{returns}
{identifier}({string.Join(",", arguments)}
)";
            void writeDeclaration(string attributes)
            {
                functionDeclarations.WriteLine($"{description}\n{attributes}{prototype};");
                if (method.DeclaringType.IsValueType && !method.IsStatic && !method.IsConstructor) functionDeclarations.WriteLine($@"
inline {returns}
{identifier}__v({string.Join(",", arguments.Skip(1).Prepend($"\n\t{Escape(method.DeclaringType)}* RECYCLONE__SPILL a_0"))}
)
{{
{'\t'}{(returns == "void" ? string.Empty : "return ")}{identifier}({
    string.Join(", ", arguments.Skip(1).Select((x, i) => $"a_{i + 1}").Prepend($"&a_0->v__value"))
});
}}");
            }
            if (builtin.body != null)
            {
                writeDeclaration(string.Empty);
                writer = writerForType(method.DeclaringType, builtin.inline > 0);
                writer.WriteLine(description);
                if (builtin.inline < 0)
                {
                    writer.Write("RECYCLONE__NOINLINE ");
                }
                else
                {
                    if (builtin.inline > 1) writer.Write("RECYCLONE__ALWAYS_INLINE ");
                    if (builtin.inline > 0) writer.Write("inline ");
                }
                writer.WriteLine($"{prototype}\n{{\n{builtin.body}}}");
                return;
            }
            var body = method.GetMethodBody();
            bytes = body?.GetILAsByteArray();
            var inline = false;
            if (method.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining))
            {
                writer = writerForType(method.DeclaringType, false);
                writer.WriteLine(description);
                writer.Write("RECYCLONE__NOINLINE ");
            }
            else
            {
                var aggressive = method.MethodImplementationFlags.HasFlag(MethodImplAttributes.AggressiveInlining);
                inline = aggressive || bytes?.Length <= 64;
                writer = writerForType(method.DeclaringType, inline);
                writer.WriteLine(description);
                if (aggressive && bytes?.Length <= 128) writer.Write("RECYCLONE__ALWAYS_INLINE ");
                if (inline) writer.Write("inline ");
            }
            var dllimport = method.GetCustomAttributesData().FirstOrDefault(x => x.AttributeType == typeofDllImportAttribute);
            if (dllimport != null)
            {
                writeDeclaration(string.Empty);
                var value = (string)dllimport.ConstructorArguments[0].Value;
                T named<T>(string name, T @default) => (T)dllimport.NamedArguments.FirstOrDefault(x => x.MemberName == name).TypedValue.Value ?? @default;
                var entryPoint = named(nameof(DllImportAttribute.EntryPoint), method.Name);
                var callingConvention = named(nameof(DllImportAttribute.CallingConvention), CallingConvention.Winapi);
                var charSet = named(nameof(DllImportAttribute.CharSet), CharSet.Ansi);
                var setLastError = named(nameof(DllImportAttribute.SetLastError), false);
                functionDeclarations.WriteLine($@"// DLL import:
//{'\t'}Value: {value}
//{'\t'}EntryPoint: {entryPoint}
//{'\t'}CallingConvention: {callingConvention}
//{'\t'}CharSet: {charSet}
//{'\t'}SetLastError: {setLastError}");
                writer.WriteLine($@"{prototype}
{{
{'\t'}static auto symbol = f_load_symbol(""{value}""s, ""{entryPoint}"");");
                GenerateInvokeUnmanaged(GetReturnType(method), method.GetParameters().Select((x, i) => (x, i)), "symbol", writer, callingConvention, charSet, setLastError);
                writer.WriteLine('}');
                return;
            }
            if (bytes == null)
            {
                writeDeclaration(string.Empty);
                functionDeclarations.WriteLine("// TO BE PROVIDED");
                return;
            }
            writer.WriteLine($@"{prototype}
{{");
            definedIndices = new SortedDictionary<string, (string, int)>();
            indexToStack = new Dictionary<int, Stack>();
            log($"{method.DeclaringType}::[{method}]");
            foreach (var x in body.ExceptionHandlingClauses) log($@"{x.Flags}
{'\t'}try: {x.TryOffset:x04} to {x.TryOffset + x.TryLength:x04}
{'\t'}handler: {x.HandlerOffset:x04} to {x.HandlerOffset + x.HandlerLength:x04}{ x.Flags switch
{
    ExceptionHandlingClauseOptions.Clause => $"\n\tcatch: {x.CatchType}",
    ExceptionHandlingClauseOptions.Filter => $"\n\tfilter: {x.FilterOffset:x04}",
    _ => string.Empty
}}");
            hasReturn = false;
            Estimate(0, new Stack(this));
            foreach (var x in body.ExceptionHandlingClauses)
                switch (x.Flags)
                {
                    case ExceptionHandlingClauseOptions.Clause:
                        Estimate(x.HandlerOffset, new Stack(this).Push(x.CatchType));
                        break;
                    case ExceptionHandlingClauseOptions.Filter:
                        Estimate(x.FilterOffset, new Stack(this).Push(typeofException));
                        break;
                    default:
                        Estimate(x.HandlerOffset, new Stack(this));
                        break;
                }
            writeDeclaration(hasReturn ? string.Empty : "[[noreturn]] ");
            log("\n");
            writer.WriteLine($"\t// init locals: {body.InitLocals}");
            foreach (var x in body.LocalVariables)
                writer.WriteLine($"\t{EscapeForArgument(x.LocalType)} l{x.LocalIndex}{(body.InitLocals ? "{}" : string.Empty)};");
            foreach (var x in definedIndices)
                for (var i = 0; i < x.Value.Index; ++i)
                    writer.WriteLine($"\t{x.Key} {x.Value.Prefix}{i};");
            //if (!method.DeclaringType.Name.StartsWith("AllowedBmpCodePointsBitmap")) writer.WriteLine($"\tprintf(\"{Escape(method)}\\n\");");
            if (!inline) writer.WriteLine("\tf_epoch_point();");
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
}} catch (t__object* e) {{
{'\t'}if (!e->f_type()->f_is(&t__type_of<{Escape(clause.CatchType)}>::v__instance)) throw;
{'\t'}{s.Variable} = e;
{'\t'}f_epoch_point();");
                                break;
                            case ExceptionHandlingClauseOptions.Filter:
                                writer.WriteLine($@"// filter
}} catch (t__object* e) {{
{'\t'}{s.Variable} = e;
{'\t'}f_epoch_point();");
                                break;
                            case ExceptionHandlingClauseOptions.Finally:
                                writers.Push(writer);
                                writer = new StringWriter();
                                break;
                            case ExceptionHandlingClauseOptions.Fault:
                                writer.WriteLine($@"// fault
}} catch (...) {{
{'\t'}f_epoch_point();");
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
            if (method.IsGenericMethod && ShouldGenerateReflection(method.DeclaringType))
            {
                var definition = Define(method.DeclaringType);
                definition.Definitions.WriteLine($@"static t__abstract_type* v__generic_arguments_{identifier}[] = {{
{string.Join(string.Empty, method.GetGenericArguments().Select(x => $"\t&t__type_of<{Escape(x)}>::v__instance,\n"))}{'\t'}nullptr
}};
t__runtime_method_info v__method_{identifier}{{&t__type_of<t__runtime_method_info>::v__instance, &t__type_of<{Escape(method.DeclaringType)}>::v__instance, u""{method.Name}""sv, {(int)method.Attributes}, {WriteAttributes(method, identifier, definition.Definitions)}, {WriteParameters(method.GetParameters(), identifier, definition.Definitions)}, {GenerateInvokeFunction(method)}, reinterpret_cast<void*>({identifier}),
#ifdef __EMSCRIPTEN__
{'\t'}{GenerateWASMInvokeFunction(method)},
#endif
{'\t'}&v__method_{identifier}, v__generic_arguments_{identifier}, nullptr
}};");
            }
        }

        public void Do(MethodInfo method, TextWriter writerForDeclarations, TextWriter writerForDefinitions, Func<Type, bool, TextWriter> writerForType, string resources)
        {
            Define(typeofRuntimeAssembly);
            Define(typeofRuntimeFieldInfo);
            Define(typeofRuntimeConstructorInfo);
            Define(typeofRuntimeMethodInfo);
            Define(typeofRuntimePropertyInfo);
            Define(typeofRuntimeType);
            Escape(finalizeOfObject);
            var typeofThread = getType(typeof(Thread));
            Define(typeofThread);
            Enqueue(getType(typeof(ThreadStart)).GetMethod("Invoke"));
            Enqueue(getType(typeof(ParameterizedThreadStart)).GetMethod("Invoke"));
            Define(typeofVoid);
            Define(typeofString.MakeArrayType());
            Define(typeofStringBuilder);
            Define(method.DeclaringType);
            Enqueue(method);
            foreach (var x in Bundle) Enqueue(x);
            foreach (var x in BundleMethods) Enqueue(x);
            do
            {
                ProcessNextMethod(writerForType);
                while (queuedTypes.Count > 0) Define(queuedTypes.Dequeue());
            }
            while (queuedMethods.Count > 0);
            processed = true;
            writerForDeclarations.WriteLine("#include \"base.h\"");
            if (Target != PlatformID.Win32NT) writerForDeclarations.WriteLine("#include \"waitables.h\"");
            writerForDeclarations.WriteLine(@"
namespace il2cxx
{
");
            writerForDeclarations.Write(typeDeclarations);
            writerForDeclarations.Write(typeDefinitions);
            writerForDeclarations.WriteLine(@"
extern t__runtime_assembly* const v__entry_assembly;

extern const std::map<std::string_view, t__type*> v__name_to_type;
extern const std::map<void*, void*> v__managed_method_to_unmanaged;");
            writerForDeclarations.Write(functionDeclarations);
            var assemblyToIdentifier = new Dictionary<Assembly, string>();
            var genericTypeDefinitionToConstructeds = runtimeDefinitions.Select(x => x.Type).Where(x => x.IsGenericType).GroupBy(x => x.GetGenericTypeDefinition()).ToDictionary(x => x.Key, xs => xs.AsEnumerable());
            foreach (var definition in runtimeDefinitions)
            {
                var writer = writerForType(definition.Type, false);
                var assembly = definition.Type.Assembly;
                if (!assemblyToIdentifier.TryGetValue(assembly, out var name))
                {
                    name = Identifier(Escape(assembly.GetName().Name));
                    assemblyToIdentifier.Add(assembly, name);
                    writerForDeclarations.WriteLine($"\nextern t__runtime_assembly v__assembly_{name};");
                    var exportedTypes = assembly.ExportedTypes.Where(x => typeToRuntime.ContainsKey(x)).ToList();
                    if (exportedTypes.Count > 0) writer.Write($@"
static t__type* v__exported_{name}[] = {{
{string.Join(string.Empty, exportedTypes.Select(x => $"\t&t__type_of<{Escape(x)}>::v__instance,\n"))}{'\t'}nullptr
}};");
                    writer.WriteLine($"\nt__runtime_assembly v__assembly_{name}{{&t__type_of<t__runtime_assembly>::v__instance, u\"{assembly.FullName}\"sv, u\"{name}\"sv, {(method != assembly.EntryPoint ? "nullptr" : ShouldGenerateReflection(method.DeclaringType) ? $"&v__method_{Escape(method)}" : "reinterpret_cast<t__runtime_method_info*>(-1)")}, {(exportedTypes.Count > 0 ? $"v__exported_{name}" : "t__type::v__empty_types")}}};");
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
                WriteRuntimeDefinition(definition, $"v__assembly_{name}", genericTypeDefinitionToConstructeds, writerForDeclarations, writer);
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
{'\t'}static RECYCLONE__THREAD t_thread_static* v_instance;
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

const std::map<std::string_view, t__type*> v__name_to_type{{{
    string.Join(",", runtimeDefinitions.Where(x => !x.Type.IsGenericParameter).Select(x => $"\n\t{{\"{x.Type.AssemblyQualifiedName}\"sv, &t__type_of<{Escape(x.Type)}>::v__instance}}"))
}
}};

const std::map<void*, void*> v__managed_method_to_unmanaged{{{
    string.Join(",", ldftnMethods.OfType<MethodInfo>().Where(m =>
    {
        if (!m.IsStatic) return false;
        var types = m.GetParameters().Select(x => x.ParameterType);
        var @return = m.ReturnType;
        return (@return == typeofVoid ? types : types.Prepend(@return)).All(x => !IsComposite(x) || x == typeofString || Define(x).IsMarshallable);
    }).Select(m => $@"
{'\t'}{{reinterpret_cast<void*>({Escape(m)}), reinterpret_cast<void*>(+[]({
        string.Join(",", UnmanagedSignature(m.GetParameters(), CharSet.Auto).Select((x, i) => $"\n\t\t{x} a_{i}"))
}
{'\t'}) -> {UnmanagedReturn(m.ReturnType)}
{'\t'}{{
{'\t'}{'\t'}return f_epoch_noiger([&]
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}return {Escape(m)}({string.Join(",", m.GetParameters().Select((x, i) =>
        {
            if (x.ParameterType.IsByRef) return $"reinterpret_cast<{EscapeForStacked(x.ParameterType)}>(a_{i})";
            if (x.ParameterType == typeofString) return $"f__new_string(a_{i})";
            return $"a_{i}";
        }).Select(x => $"\n\t\t\t\t{x}"))
}
{'\t'}{'\t'}{'\t'});
{'\t'}{'\t'}}});
{'\t'}}})}}"))
}
}};");
            var arguments0 = string.Empty;
            var arguments1 = string.Empty;
            if (method.GetParameters().Select(x => x.ParameterType).SequenceEqual(new[] { typeofString.MakeArrayType() }))
            {
                arguments0 = $@"
{'\t'}{'\t'}auto RECYCLONE__SPILL arguments = f__new_array<{Escape(typeofString.MakeArrayType())}, il2cxx::{EscapeForMember(typeofString)}>(argc);
{'\t'}{'\t'}for (int i = 0; i < argc; ++i) arguments->f_data()[i] = f__new_string(argv[i]);";
                arguments1 = "arguments";
            }
            writerForDefinitions.WriteLine($@"
t_static* t_static::v_instance;

RECYCLONE__THREAD t_thread_static* t_thread_static::v_instance;

void f__finalize(t_object<t__type>* a_p)
{{
{'\t'}try {{
{'\t'}{'\t'}try {{
{'\t'}{'\t'}{'\t'}{GenerateVirtualCall(finalizeOfObject, "a_p", Enumerable.Empty<string>(), x => x)};
{'\t'}{'\t'}}} catch (t__object* p) {{
{'\t'}{'\t'}{'\t'}throw std::runtime_error(f__string(f__to_string(p)));
{'\t'}{'\t'}}}
{'\t'}}} catch (std::exception& e) {{
{'\t'}{'\t'}std::cerr << ""caught: "" << e.what() << std::endl;
{'\t'}}} catch (...) {{
{'\t'}{'\t'}std::cerr << ""caught unknown"" << std::endl;
{'\t'}}}
}}

void f__startup(void* a_bottom)
{{
{'\t'}std::setlocale(LC_ALL, """");
{'\t'}auto options = new t_engine::t_options;
{'\t'}options->v_verbose = std::getenv(""IL2CXX_VERBOSE"");
{'\t'}options->v_verify = std::getenv(""IL2CXX_VERIFY_LEAKS"");
{'\t'}t_slot thread((new t_engine(*options, a_bottom))->f_initialize<{Escape(typeofThread)}, t_thread_static>(f__finalize));
{'\t'}new t_static;
{'\t'}new t_thread_static;
}}

{EscapeForStacked(typeofString)} f__to_string(t__object* a_p)
{{
{'\t'}{GenerateVirtualCall(typeofObject.GetMethod(nameof(object.ToString)), "a_p", Enumerable.Empty<string>(), x => $"return {x};")}
}}

}}

#ifndef __EMSCRIPTEN__
int main(int argc, char* argv[])
{{
{'\t'}using namespace il2cxx;
{'\t'}std::setlocale(LC_ALL, """");
{'\t'}il2cxx::t_engine::t_options options;
{'\t'}options.v_verbose = std::getenv(""IL2CXX_VERBOSE"");
{'\t'}options.v_verify = std::getenv(""IL2CXX_VERIFY_LEAKS"");
{'\t'}il2cxx::t_engine engine(options);
{'\t'}return [&]() RECYCLONE__NOINLINE
{'\t'}{{
{'\t'}{'\t'}// Preventing optimized out.
{'\t'}{'\t'}auto volatile thread = engine.f_initialize<{Escape(typeofThread)}, t_thread_static>(f__finalize);
{'\t'}{'\t'}auto s = std::make_unique<t_static>();
{'\t'}{'\t'}auto ts = std::make_unique<t_thread_static>();{arguments0}
{'\t'}{'\t'}try {{
{'\t'}{'\t'}{'\t'}{(method.ReturnType == typeofVoid
    ? $"{Escape(method)}({arguments1});\n\t\t\treturn engine.f_exit(0)"
    : $"return engine.f_exit({Escape(method)}({arguments1}))")};
{'\t'}{'\t'}}} catch (t__object* p) {{
{'\t'}{'\t'}{'\t'}throw std::runtime_error(f__string(f__to_string(p)));
{'\t'}{'\t'}}}
{'\t'}}}();
}}
#endif");
        }
    }
}
