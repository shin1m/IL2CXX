using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    static class Utilities
    {
        class Disposer : IDisposable
        {
            private readonly Action dispose;

            public Disposer(Action dispose) => this.dispose = dispose;
            public void Dispose() => dispose();
        }

        static int Spawn(string command, string arguments, string workingDirectory, IEnumerable<(string, string)> environment, Action<string> output, Action<string> error)
        {
            var si = new ProcessStartInfo(command)
            {
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            };
            foreach (var (name, value) in environment) si.Environment.Add(name, value);
            using var process = Process.Start(si);
            static void forward(StreamReader reader, Action<string> write)
            {
                while (!reader.EndOfStream) write(reader.ReadLine());
            }
            var task = Task.WhenAll(
                Task.Run(() => forward(process.StandardOutput, output)),
                Task.Run(() => forward(process.StandardError, error))
            );
            process.WaitForExit();
            task.Wait();
            return process.ExitCode;
        }

        public static string Build(MethodInfo method, IEnumerable<Type> bundle = null, IEnumerable<Type> generateReflection = null, IEnumerable<MethodInfo> bundleMethods = null)
        {
            Console.Error.WriteLine($"{method.DeclaringType.Name}::[{method}]");
            var build = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{method.DeclaringType.Name}-{method.Name}-build");
            if (Directory.Exists(build)) Directory.Delete(build, true);
            Directory.CreateDirectory(build);
            var definitionsCount = 0;
            using (var context = new MetadataLoadContext(new PathAssemblyResolver(Directory.EnumerateFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll").Append(typeof(Builtin).Assembly.Location).Append(method.Module.Assembly.Location))))
            using (var declarations = File.CreateText(Path.Combine(build, "declarations.h")))
            using (var inlines = new StringWriter())
            using (var main = File.CreateText(Path.Combine(build, "main.cc")))
            {
                var assembly = context.LoadFromAssemblyPath(method.Module.Assembly.Location);
                var type = assembly.GetType(method.DeclaringType.FullName, true);
                method = type.GetMethod(method.Name, BindingFlags.Static | BindingFlags.NonPublic);
                declarations.WriteLine(@"#ifndef DECLARATIONS_H
#define DECLARATIONS_H");
                StreamWriter newDefinitions()
                {
                    var x = File.CreateText(Path.Combine(build, $"definitions{definitionsCount++}.cc"));
                    x.WriteLine(@"#include ""declarations.h""

namespace il2cxx
{");
                    return x;
                }
                var definitions = newDefinitions();
                using var disposer = new Disposer(() => definitions.Dispose());
                main.WriteLine("#include \"declarations.h\"\n");
                Type get(Type x) => context.LoadFromAssemblyName(x.Assembly.FullName).GetType(x.FullName, true);
                var target = Environment.OSVersion.Platform;
                var reflection = generateReflection?.Select(get).ToHashSet() ?? new HashSet<Type>();
                new Transpiler(get, DefaultBuiltin.Create(get, target), _ => { }, target, Environment.Is64BitOperatingSystem)
                {
                    Bundle = bundle?.Select(get) ?? Enumerable.Empty<Type>(),
                    BundleMethods = bundleMethods?.Select(x =>
                    {
                        var gd = x.GetGenericMethodDefinition();
                        return get(x.DeclaringType).GetMethod(
                            gd.Name, gd.GetGenericArguments().Length,
                            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                            gd.GetParameters().Select(x => x.ParameterType).Select(x => x.IsGenericMethodParameter ? Type.MakeGenericMethodParameter(x.GenericParameterPosition) : get(x)).ToArray(), null
                        ).MakeGenericMethod(x.GetGenericArguments().Select(get).ToArray());
                    }) ?? Enumerable.Empty<MethodInfo>(),
                    GenerateReflection = reflection.Contains
                }.Do(method, declarations, main, (type, inline) =>
                {
                    if (inline) return inlines;
                    if (definitions.BaseStream.Position > 2 * 1024 * 1024)
                    {
                        definitions.WriteLine("\n}");
                        definitions.Dispose();
                        definitions = newDefinitions();
                    }
                    return definitions;
                });
                declarations.WriteLine("\nnamespace il2cxx\n{");
                declarations.Write(inlines);
                declarations.WriteLine(@"
}

#endif");
                definitions.WriteLine("\n}");
                main.WriteLine(@"
#include ""types.cc""
#include ""engine.cc""
#include ""handles.cc""");
                if (target != PlatformID.Win32NT) main.WriteLine("#include \"waitables.cc\"");
            }
            File.WriteAllText(Path.Combine(build, "CMakeLists.txt"), $@"cmake_minimum_required(VERSION 3.16)
project(run)
add_subdirectory(../src/recyclone recyclone-build EXCLUDE_FROM_ALL)
function(add name)
{'\t'}add_executable(${{name}} {string.Join(" ", Enumerable.Range(0, definitionsCount).Select(i => $"definitions{i}.cc"))} main.cc)
{'\t'}target_include_directories(${{name}} PRIVATE ../src)
{'\t'}target_compile_options(${{name}} PRIVATE $<$<CXX_COMPILER_ID:MSVC>:/bigobj>)
{'\t'}target_link_libraries(${{name}} recyclone $<$<NOT:$<PLATFORM_ID:Windows>>:dl>)
{'\t'}target_precompile_headers(${{name}} PRIVATE declarations.h)
endfunction()
add(run)
add(runco)
target_compile_definitions(runco PRIVATE RECYCLONE__COOPERATIVE)
");
            var cmake = Environment.GetEnvironmentVariable("CMAKE_PATH") ?? "cmake";
            Assert.That(Spawn(cmake, ". -DCMAKE_BUILD_TYPE=Debug", build, Enumerable.Empty<(string, string)>(), Console.Error.WriteLine, Console.Error.WriteLine), Is.Zero);
            Assert.That(Spawn(cmake, "--build .", build, Enumerable.Empty<(string, string)>(), Console.Error.WriteLine, Console.Error.WriteLine), Is.Zero);
            return build;
        }
        public static string Build(Func<int> method, IEnumerable<Type> bundle = null, IEnumerable<Type> generateReflection = null, IEnumerable<MethodInfo> bundleMethods = null) => Build(method.Method, bundle, generateReflection, bundleMethods);
        public static string Build(Func<string[], int> method, IEnumerable<Type> bundle = null, IEnumerable<Type> generateReflection = null, IEnumerable<MethodInfo> bundleMethods = null) => Build(method.Method, bundle, generateReflection, bundleMethods);
        public static void Run(string build, bool cooperative, string arguments, bool verify = true)
        {
            IEnumerable<(string, string)> environment = new[]
            {
                ("IL2CXX_VERBOSE", string.Empty),
            };
            if (verify) environment = environment.Append(("IL2CXX_VERIFY_LEAKS", string.Empty));
            var name = cooperative ? "runco" : "run";
            var path = Path.Combine(build, name);
            if (!File.Exists(path)) path = Path.Combine(build, "Debug", name);
            Assert.That(Spawn(path, arguments, build, environment, Console.Error.WriteLine, Console.Error.WriteLine), Is.Zero);
        }

        [StructLayout(LayoutKind.Sequential, Size = 4096)]
        struct Padding
        { }
#pragma warning disable CS0219
        public static void WithPadding(Action x)
        {
            var p = new Padding();
            x();
        }
        public static T WithPadding<T>(Func<T> x)
        {
            var p = new Padding();
            return x();
        }
#pragma warning restore CS0219
    }
}
