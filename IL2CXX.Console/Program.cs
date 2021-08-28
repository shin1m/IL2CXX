using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace IL2CXX.Console
{
    using System;
    using System.Runtime.InteropServices;

    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 3) return 1;
            var target = Enum.Parse<PlatformID>(args[1], true);
            var is64 = args[2] != "32";
            var @out = args.Length < 4 ? "out" : args[3];
            var names = new SortedSet<string>();
            var type2path = new Dictionary<Type, string>();
            using (var load = new MetadataLoadContext(new PathAssemblyResolver(Directory.EnumerateFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll").Append(typeof(Builtin).Assembly.Location).Concat(Directory.EnumerateFiles(Path.GetDirectoryName(args[0]), "*.dll")))))
            {
                var assembly = load.LoadFromAssemblyPath(args[0]);
                var entry = assembly.EntryPoint ?? throw new InvalidOperationException();
                if (Directory.Exists(@out)) Directory.Delete(@out, true);
                Directory.CreateDirectory(@out);
                Type get(Type x) => load.LoadFromAssemblyName(x.Assembly.FullName).GetType(x.FullName, true);
                var transpiler = new Transpiler(get, DefaultBuiltin.Create(get, target), /*Console.Error.WriteLine*/_ => { }, target, is64, false);
                transpiler.IsInvalid = type =>
                {
                    if (type.Name == "InternalModuleBuilder") return true;
                    if (type.Name == "QCallAssembly") return true;
                    if (type.Name == "QCallTypeHandle") return true;
                    if (type.FullName == "System.Reflection.RuntimeAssembly") return true;
                    if (type.FullName == "System.RuntimeType") return true;
                    return false;
                };
                var definition = TextWriter.Null;
                try
                {
                    using var declarations = File.CreateText(Path.Combine(@out, "declarations.h"));
                    declarations.WriteLine(@"#ifndef DECLARATIONS_H
#define DECLARATIONS_H");
                    using var inlines = new StringWriter();
                    using var others = new StringWriter();
                    using var main = File.CreateText(Path.Combine(@out, "main.cc"));
                    main.WriteLine("#include \"declarations.h\"\n");
                    transpiler.Do(entry, declarations, main, (type, inline) =>
                    {
                        if (inline) return inlines;
                        if (type.IsInterface || type.IsSubclassOf(transpiler.typeofMulticastDelegate)) return others;
                        definition.Dispose();
                        if (type2path.TryGetValue(type, out var path)) return definition = new StreamWriter(path, true);
                        var escaped = transpiler.EscapeType(type);
                        if (escaped.Length > 240) escaped = escaped.Substring(0, 240);
                        var name = $"{escaped}.cc";
                        for (var i = 0; !names.Add(name); ++i) name = $"{escaped}__{i}.cc";
                        path = Path.Combine(@out, name);
                        type2path.Add(type, path);
                        definition = new StreamWriter(path);
                        definition.WriteLine(@"#include ""declarations.h""

namespace il2cxx
{");
                        return definition;
                    }, Path.Combine(@out, "resources"));
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
                var destination = Path.Combine(@out, path);
                Directory.CreateDirectory(destination);
                var source = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                foreach (var x in Directory.EnumerateDirectories(source)) copy(Path.Combine(path, Path.GetFileName(x)));
                foreach (var x in Directory.EnumerateFiles(source)) File.Copy(x, Path.Combine(destination, Path.GetFileName(x)));
            }
            copy("src");
            var name = Path.GetFileNameWithoutExtension(args[0]);
            File.WriteAllText(Path.Combine(@out, "CMakeLists.txt"), $@"cmake_minimum_required(VERSION 3.16)
project({name})

add_subdirectory(src/recyclone EXCLUDE_FROM_ALL)

add_executable({name}
{'\t'}src/types.cc
{'\t'}src/engine.cc
{'\t'}src/handles.cc
{(target == PlatformID.Win32NT ? string.Empty : "\tsrc/waitables.cc\n")
}{string.Join(string.Empty, names.Select(x => $"\t{x}\n"))
}{'\t'}main.cc
{'\t'})
target_include_directories({name} PRIVATE src)
target_link_libraries({name} recyclone dl)
target_precompile_headers({name} PRIVATE declarations.h)
file(COPY resources DESTINATION .)
");
            return 0;
        }
    }
}
