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
            void forward(StreamReader reader, Action<string> write)
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
            using (var header = File.CreateText(Path.Combine(build, "run.h")))
            using (var main = File.CreateText(Path.Combine(build, "run.cc")))
            using (var body = new StringWriter())
            {
                main.WriteLine("#include \"run.h\"\n");
                new Transpiler(DefaultBuiltin.Create(), _ => { }).Do(method, header, main, (_, __) => body, Path.Combine(build, "resources"));
                main.WriteLine("\nnamespace il2cxx\n{");
                main.Write(body);
                main.WriteLine(@"
}

#include ""recyclone/src/object.cc""
#include ""recyclone/src/thread.cc""
#include ""recyclone/src/engine.cc""
#include ""types.cc""
#include ""engine.cc""
#include ""handles.cc""
#include ""waitables.cc""");
            }
            Assert.AreEqual(0, Spawn("make", "run", build, new[]
            {
                ("CXXFLAGS", "-I../src/recyclone/include -I../src -std=c++17 -g"),
                ("LDFLAGS", "-lpthread -ldl")
            }, Console.Error.WriteLine, Console.Error.WriteLine));
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
            Assert.AreEqual(0, Spawn(Path.Combine(build, "run"), arguments, build, environment, Console.Error.WriteLine, Console.Error.WriteLine));
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
