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

        public static string Build(MethodInfo method)
        {
            Console.Error.WriteLine($"{method.DeclaringType.Name}::[{method}]");
            var build = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{method.DeclaringType.Name}-{method.Name}-build");
            if (Directory.Exists(build)) Directory.Delete(build, true);
            Directory.CreateDirectory(build);
            using (var load = new MetadataLoadContext(new PathAssemblyResolver(Directory.EnumerateFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll").Append(typeof(Builtin).Assembly.Location).Append(method.Module.Assembly.Location))))
            using (var header = File.CreateText(Path.Combine(build, "run.h")))
            using (var main = File.CreateText(Path.Combine(build, "run.cc")))
            using (var body = new StringWriter())
            {
                var assembly = load.LoadFromAssemblyPath(method.Module.Assembly.Location);
                var type = assembly.GetType(method.DeclaringType.FullName, true);
                method = type.GetMethod(method.Name, BindingFlags.Static | BindingFlags.NonPublic);
                main.WriteLine("#include \"run.h\"\n");
                Type get(Type x) => load.LoadFromAssemblyName(x.Assembly.FullName).GetType(x.FullName, true);
                var target = Environment.OSVersion.Platform;
                new Transpiler(get, DefaultBuiltin.Create(get, target), _ => { }, target, Environment.Is64BitOperatingSystem).Do(method, header, main, (_, _) => body, Path.Combine(build, "resources"));
                main.WriteLine("\nnamespace il2cxx\n{");
                main.Write(body);
                main.WriteLine(@"
}

#include ""types.cc""
#include ""engine.cc""
#include ""handles.cc""");
                if (target != PlatformID.Win32NT) main.WriteLine("#include \"waitables.cc\"");
            }
            File.WriteAllText(Path.Combine(build, "CMakeLists.txt"), @"cmake_minimum_required(VERSION 3.16)
project(run)
add_subdirectory(../src/recyclone recyclone-build EXCLUDE_FROM_ALL)
add_executable(run run.cc)
target_include_directories(run PRIVATE ../src)
target_compile_options(run PRIVATE $<$<CXX_COMPILER_ID:MSVC>:/bigobj>)
target_link_libraries(run recyclone $<$<NOT:$<PLATFORM_ID:Windows>>:dl>)
");
            var cmake = Environment.GetEnvironmentVariable("CMAKE_PATH") ?? "cmake";
            Assert.AreEqual(0, Spawn(cmake, ". -DCMAKE_BUILD_TYPE=Debug", build, Enumerable.Empty<(string, string)>(), Console.Error.WriteLine, Console.Error.WriteLine));
            Assert.AreEqual(0, Spawn(cmake, "--build .", build, Enumerable.Empty<(string, string)>(), Console.Error.WriteLine, Console.Error.WriteLine));
            return build;
        }
        public static string Build(Func<int> method) => Build(method.Method);
        public static string Build(Func<string[], int> method) => Build(method.Method);
        public static void Run(string build, string arguments, bool verify = true)
        {
            IEnumerable<(string, string)> environment = new[]
            {
                ("IL2CXX_VERBOSE", string.Empty),
            };
            if (verify) environment = environment.Append(("IL2CXX_VERIFY_LEAKS", string.Empty));
            var path = Path.Combine(build, "run");
            if (!File.Exists(path)) path = Path.Combine(build, "Debug", "run");
            Assert.AreEqual(0, Spawn(path, arguments, build, environment, Console.Error.WriteLine, Console.Error.WriteLine));
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
