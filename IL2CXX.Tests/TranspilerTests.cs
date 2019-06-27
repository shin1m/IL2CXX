using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    class Tests
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

        class Foo
        {
            public object Value;

            public Foo(object value) => Value = value;
            public override string ToString() => Value.ToString();
        }
        struct Bar
        {
            public string Value;

            public Bar(string value) => Value = value;
            public override string ToString() => Value.ToString();
        }

        static int HelloWorld()
        {
            Console.WriteLine("Hello World!");
            return 0;
        }
        static int CallVirtual()
        {
            Console.WriteLine(new Foo("Hello Foo!").ToString());
            return 0;
        }
        static int ConstrainedCallVirtual()
        {
            Console.WriteLine(new Bar("Hello Bar!").ToString());
            return 0;
        }
        static int Box()
        {
            Console.WriteLine(new Foo(new Bar("Hello Foo Bar!")).ToString());
            return 0;
        }
        static int Concatination()
        {
            string f(string name) => $"Hello {name}!";
            Console.WriteLine(f("World"));
            return 0;
        }
        [TestCase(nameof(HelloWorld))]
        [TestCase(nameof(CallVirtual))]
        [TestCase(nameof(ConstrainedCallVirtual))]
        [TestCase(nameof(Box))]
        [TestCase(nameof(Concatination))]
        public void DoShouldTranspile(string method)
        {
            var build = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{method}-build");
            if (Directory.Exists(build)) Directory.Delete(build, true);
            Directory.CreateDirectory(build);
            using (var writer = File.CreateText(Path.Combine(build, "run.cc")))
                new Transpiler(_ => { }).Do(GetType().GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic), writer);
            Assert.AreEqual(0, Spawn("make", "run", build, new[] {
                ("CXXFLAGS", "-std=c++17 -g")
            }, Console.Error.WriteLine, Console.Error.WriteLine));
            Assert.AreEqual(0, Spawn(Path.Combine(build, "run"), "", "", Enumerable.Empty<(string, string)>(), Console.Error.WriteLine, Console.Error.WriteLine));
        }
    }
}
