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
            var si = new ProcessStartInfo(command) {
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            };
            foreach (var (name, value) in environment) si.Environment.Add(name, value);
            using (var process = Process.Start(si))
            {
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
        }

        public static void Test(MethodInfo method)
        {
            Console.Error.WriteLine($"{method.DeclaringType.Name}::[{method}]");
            var build = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{method.DeclaringType.Name}-{method.Name}-build");
            var include = File.ReadLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CXXIncludePath")).First();
            var src = File.ReadLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CXXSourcePath")).First();
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

#include ""slot.cc""
#include ""object.cc""
#include ""type.cc""
#include ""thread.cc""
#include ""engine.cc""");
            }
            Assert.AreEqual(0, Spawn("make", "run", build, new[] {
                ("CXXFLAGS", $"'-I{include}' '-I{src}' -std=c++17 -g"),
                ("LDFLAGS", $"-lpthread -ldl -lunwind -lunwind-x86_64")
            }, Console.Error.WriteLine, Console.Error.WriteLine));
            Assert.AreEqual(0, Spawn(Path.Combine(build, "run"), "", build, Enumerable.Empty<(string, string)>(), Console.Error.WriteLine, Console.Error.WriteLine));
        }
        public static void Test(Func<int> method) => Test(method.Method);

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
