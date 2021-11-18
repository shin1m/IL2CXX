using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;

namespace IL2CXX.Console
{
    using System;
    using System.Runtime.InteropServices;

    class Program
    {
        class Options
        {
            [Option(Required = true)]
            public PlatformID Target { get; set; }
            [Option(Group = "is32", Default = false)]
            public bool Is32 { get; set; }
            public bool Is64 => !Is32;
            [Option(Default = "out")]
            public string Out { get; set; }
            [Value(0, Required = true)]
            public string Source { get; set; }
            [Option]
            public IEnumerable<string> Assemblies { get; set; }
            [Option]
            public IEnumerable<string> Bundle { get; set; }
            [Option]
            public IEnumerable<string> Reflection { get; set; }
        }
        static int Main(string[] args) => Parser.Default.ParseArguments<Options>(args).MapResult(options =>
        {
            var names = new SortedSet<string>();
            var type2path = new Dictionary<Type, string>();
            using (var context = new MetadataLoadContext(new PathAssemblyResolver(options.Assemblies.Prepend(RuntimeEnvironment.GetRuntimeDirectory()).Append(Path.GetDirectoryName(options.Source)).SelectMany(x => Directory.EnumerateFiles(x, "*.dll")).Prepend(typeof(Builtin).Assembly.Location))))
            {
                var assembly = context.LoadFromAssemblyPath(options.Source);
                var entry = assembly.EntryPoint ?? throw new InvalidOperationException();
                if (Directory.Exists(options.Out)) Directory.Delete(options.Out, true);
                Directory.CreateDirectory(options.Out);
                Type get(Type x) => context.LoadFromAssemblyName(x.Assembly.FullName).GetType(x.FullName, true);
                var builtin = DefaultBuiltin.Create(get, options.Target);
                try
                {
                    builtin.For(context.LoadFromAssemblyName("Microsoft.JSInterop.WebAssembly").GetType("WebAssembly.JSInterop.InternalCalls", true), (type, code) => code.ForGeneric(
                        type.GetMethod("InvokeJS"),
                        (transpiler, types) => ($@"{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}std::cerr << ""InvokeJS<{string.Join(", ", types.Select(x => x.ToString()))}>:"" << std::endl;
{'\t'}{'\t'}auto s = a_1->v_FunctionIdentifier.v;
{'\t'}{'\t'}std::cerr << ""\tFunctionIdentifier: "" << f__string({{&s->v__5ffirstChar, static_cast<size_t>(s->v__5fstringLength)}}) << std::endl;
{'\t'}{'\t'}std::cerr << ""\tResultType: "" << a_1->v_ResultType.v << std::endl;
{'\t'}{'\t'}s = a_1->v_MarshalledCallArgsJson.v;
{'\t'}{'\t'}std::cerr << ""\tMarshalledCallArgsJson: "" << f__string({{&s->v__5ffirstChar, static_cast<size_t>(s->v__5fstringLength)}}) << std::endl;
{'\t'}{'\t'}std::cerr << ""\tMarshalledCallAsyncHandle: "" << a_1->v_MarshalledCallAsyncHandle.v << std::endl;
{'\t'}{'\t'}std::cerr << ""\tTargetInstanceId: "" << a_1->v_TargetInstanceId.v << std::endl;
{'\t'}{'\t'}return ({transpiler.EscapeForStacked(types[3])}){{}};
{'\t'}}});
", 0)
                    ));
                }
                catch (FileNotFoundException) { }
                Type load(string x)
                {
                    var xs = x.Split(':');
                    return context.LoadFromAssemblyName(xs[0]).GetType(xs[1], true);
                }
                var reflection = options.Reflection.Select(load).ToHashSet();
                var transpiler = new Transpiler(get, builtin, /*Console.Error.WriteLine*/_ => { }, options.Target, options.Is64, false)
                {
                    Bundle = options.Bundle.Select(load),
                    GenerateReflection = reflection.Contains
                };
                var definition = TextWriter.Null;
                try
                {
                    using var declarations = File.CreateText(Path.Combine(options.Out, "declarations.h"));
                    declarations.WriteLine(@"#ifndef DECLARATIONS_H
#define DECLARATIONS_H");
                    using var inlines = new StringWriter();
                    using var others = new StringWriter();
                    using var main = File.CreateText(Path.Combine(options.Out, "main.cc"));
                    main.WriteLine("#include \"declarations.h\"\n");
                    transpiler.Do(entry, declarations, main, (type, inline) =>
                    {
                        if (inline) return inlines;
                        if (type.IsInterface || type.IsSubclassOf(transpiler.typeofMulticastDelegate) || type.IsGenericParameter) return others;
                        definition.Dispose();
                        while ((Nullable.GetUnderlyingType(type) ?? type.GetElementType()) is Type t) type = t;
                        while (type.IsNested) type = type.DeclaringType;
                        if (type.IsGenericType) type = type.GetGenericTypeDefinition();
                        if (type2path.TryGetValue(type, out var path)) return definition = new StreamWriter(path, true);
                        var escaped = transpiler.EscapeType(type);
                        if (escaped.Length > 240) escaped = escaped.Substring(0, 240);
                        var name = $"{escaped}.cc";
                        for (var i = 0; !names.Add(name); ++i) name = $"{escaped}__{i}.cc";
                        path = Path.Combine(options.Out, name);
                        type2path.Add(type, path);
                        definition = new StreamWriter(path);
                        definition.WriteLine(@"#include ""declarations.h""

namespace il2cxx
{");
                        return definition;
                    }, Path.Combine(options.Out, "resources"));
                    declarations.WriteLine("\nnamespace il2cxx\n{");
                    declarations.Write(inlines);
                    declarations.WriteLine(@"
}

#endif");
                    main.WriteLine("\nnamespace il2cxx\n{");
                    main.Write(others);
                    main.WriteLine("\n}");
                }
                finally
                {
                    definition.Dispose();
                }
            }
            foreach (var path in type2path.Values) File.AppendAllText(path, "\n}\n");
            void copy(string path)
            {
                var destination = Path.Combine(options.Out, path);
                Directory.CreateDirectory(destination);
                var source = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                foreach (var x in Directory.EnumerateDirectories(source)) copy(Path.Combine(path, Path.GetFileName(x)));
                foreach (var x in Directory.EnumerateFiles(source)) File.Copy(x, Path.Combine(destination, Path.GetFileName(x)));
            }
            copy("src");
            var name = Path.GetFileNameWithoutExtension(options.Source);
            File.WriteAllText(Path.Combine(options.Out, "CMakeLists.txt"), $@"cmake_minimum_required(VERSION 3.16)
project({name})

add_subdirectory(src/recyclone EXCLUDE_FROM_ALL)

add_executable({name}
{'\t'}src/types.cc
{'\t'}src/engine.cc
{'\t'}src/handles.cc
{(options.Target == PlatformID.Win32NT ? string.Empty : "\tsrc/waitables.cc\n")
}{string.Join(string.Empty, names.Select(x => $"\t{x}\n"))
}{'\t'}main.cc
{'\t'})
target_include_directories({name} PRIVATE src)
target_compile_options({name} PRIVATE ""-fno-rtti"")
target_link_libraries({name} recyclone dl)
target_precompile_headers({name} PRIVATE declarations.h)
if(EMSCRIPTEN)
{'\t'}target_compile_definitions({name} PRIVATE RECYCLONE__STACK_LIMIT=0x400000)
{'\t'}target_link_options({name} PRIVATE ""-O0"")
endif()
file(COPY resources DESTINATION .)
");
            return 0;
        }, _ => 1);
    }
}
