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
            [Option(Group = "is64", Default = true)]
            public bool Is64 { get; set; }
            [Option(Group = "is32", Default = false)]
            public bool Is32 { get => !Is64; set => Is64 = !value; }
            [Option(Default = "out")]
            public string Out { get; set; }
            [Value(0, Required = true)]
            public string Source { get; set; }
            [Option]
            public IEnumerable<string> Bundle { get; set; }
            [Option]
            public IEnumerable<string> Reflection { get; set; }
        }
        static int Main(string[] args) => Parser.Default.ParseArguments<Options>(args).MapResult(options =>
        {
            var names = new SortedSet<string>();
            var type2path = new Dictionary<Type, string>();
            using (var context = new MetadataLoadContext(new PathAssemblyResolver(Directory.EnumerateFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll").Append(typeof(Builtin).Assembly.Location).Concat(Directory.EnumerateFiles(Path.GetDirectoryName(options.Source), "*.dll")))))
            {
                var assembly = context.LoadFromAssemblyPath(options.Source);
                var entry = assembly.EntryPoint ?? throw new InvalidOperationException();
                if (Directory.Exists(options.Out)) Directory.Delete(options.Out, true);
                Directory.CreateDirectory(options.Out);
                Type get(Type x) => context.LoadFromAssemblyName(x.Assembly.FullName).GetType(x.FullName, true);
                Type load(string x)
                {
                    var xs = x.Split(':');
                    return context.LoadFromAssemblyName(xs[0]).GetType(xs[1], true);
                }
                var reflection = options.Reflection?.Select(load).ToHashSet() ?? new HashSet<Type>();
                var transpiler = new Transpiler(get, DefaultBuiltin.Create(get, options.Target), /*Console.Error.WriteLine*/_ => { }, options.Target, options.Is64, false)
                {
                    Bundle = options.Bundle?.Select(load) ?? Enumerable.Empty<Type>(),
                    GenerateReflection = reflection.Contains
                };
                var definition = TextWriter.Null;
                try
                {
                    using var declarations = File.CreateText(Path.Combine(options.Out, "declarations.h"));
                    declarations.WriteLine(@"#ifndef DECLARATIONS_H
#define DECLARATIONS_H");
                    using var inlines = new StringWriter();
                    using var main = File.CreateText(Path.Combine(options.Out, "main.cc"));
                    main.WriteLine("#include \"declarations.h\"\n");
                    transpiler.Do(entry, declarations, main, (type, inline) =>
                    {
                        if (inline) return inlines;
                        definition.Dispose();
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
target_link_libraries({name} recyclone dl)
target_precompile_headers({name} PRIVATE declarations.h)
file(COPY resources DESTINATION .)
");
            return 0;
        }, _ => 1);
    }
}
