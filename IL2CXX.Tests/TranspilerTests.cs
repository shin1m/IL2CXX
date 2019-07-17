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
            if (Directory.Exists(build)) Directory.Delete(build, true);
            Directory.CreateDirectory(build);
            using (var writer = File.CreateText(Path.Combine(build, "run.cc")))
                new Transpiler(_ => { }).Do(method, writer);
            Assert.AreEqual(0, Spawn("make", "run", build, new[] {
                ("CXXFLAGS", "-std=c++17 -g")
            }, Console.Error.WriteLine, Console.Error.WriteLine));
            Assert.AreEqual(0, Spawn(Path.Combine(build, "run"), "", "", Enumerable.Empty<(string, string)>(), Console.Error.WriteLine, Console.Error.WriteLine));
        }
        public static void Test(Func<int> method) => Test(method.Method);
    }
    class CallTests
    {
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
        class Bar<T>
        {
            public T Value;

            public Bar(T value) => Value = value;
            public string AsString() => Value.ToString();
        }

        static int CallVirtual()
        {
            Console.WriteLine(new Foo("Hello, Foo!").ToString());
            return 0;
        }
        [Test]
        public void TestCallVirtual() => Utilities.Test(CallVirtual);
        static int ConstrainedCallVirtual()
        {
            Console.WriteLine(new Bar("Hello, Bar!").ToString());
            return 0;
        }
        [Test]
        public void TestConstrainedCallVirtual() => Utilities.Test(ConstrainedCallVirtual);
        static int ConstrainedCallVirtualReference()
        {
            Console.WriteLine(new Bar<string>("Hello, Bar!").AsString());
            return 0;
        }
        [Test]
        public void TestConstrainedCallVirtualReference() => Utilities.Test(ConstrainedCallVirtualReference);
        static int Box()
        {
            Console.WriteLine(new Foo(new Bar("Hello, Foo Bar!")).ToString());
            return 0;
        }
        [Test]
        public void TestBox() => Utilities.Test(Box);
    }
    class AbstractMethodTests
    {
        abstract class Foo
        {
            public abstract string AsString(object x);
        }
        class Bar : Foo
        {
            public override string AsString(object x) => x.ToString();
        }

        static int CallVirtual()
        {
            Console.WriteLine(new Bar().AsString("Hello, World!"));
            return 0;
        }
        [Test]
        public void TestCallVirtual() => Utilities.Test(CallVirtual);
    }
    class AbstractGenericMethodTests
    {
        abstract class Foo
        {
            public abstract string AsString<T>(T x);
        }
        class Bar : Foo
        {
            public override string AsString<T>(T x) => x.ToString();
        }

        static int CallVirtual()
        {
            Console.WriteLine(new Bar().AsString("Hello, World!"));
            Console.WriteLine(new Bar().AsString(0));
            return 0;
        }
        [Test]
        public void TestCallVirtual() => Utilities.Test(CallVirtual);
    }
    class InterfaceMethodTests
    {
        interface IFoo
        {
            string AsString(object x);
        }
        class Foo : IFoo
        {
            public string AsString(object x) => x.ToString();
        }

        static string Bar(IFoo x, object y) => x.AsString(y);

        static int CallVirtual()
        {
            Console.WriteLine(Bar(new Foo(), "Hello, World!"));
            return 0;
        }
        [Test]
        public void TestCallVirtual() => Utilities.Test(CallVirtual);
    }
    class InterfaceGenericMethodTests
    {
        interface IFoo
        {
            string AsString<T>(T x);
        }
        class Foo : IFoo
        {
            public string AsString<T>(T x) => x.ToString();
        }

        static string Bar<T>(IFoo x, T y) => x.AsString(y);

        static int CallVirtual()
        {
            Console.WriteLine(Bar(new Foo(), "Hello, World!"));
            return 0;
        }
        [Test]
        public void TestCallVirtual() => Utilities.Test(CallVirtual);
    }
    class StringTests
    {
        static int HelloWorld()
        {
            Console.WriteLine("Hello, World!");
            return 0;
        }
        [Test]
        public void TestHelloWorld() => Utilities.Test(HelloWorld);
        static int Concatination()
        {
            string f(string name) => $"Hello, {name}!";
            Console.WriteLine(f("World"));
            return 0;
        }
        [Test]
        public void TestConcatination() => Utilities.Test(Concatination);
        static int Format()
        {
            string f(object x, object y) => $"Hello, {x} and {y}!";
            Console.WriteLine(f("World", 0));
            return 0;
        }
        [Test]
        public void TestFormat() => Utilities.Test(Format);
    }
    class GenericBuiltinTests
    {
        static int Count()
        {
            var n = Enumerable.Range(0, 128).Count(x => x >= 'A' && x <= 'Z');
            Console.WriteLine($"# of alphabets: {n}");
            return n == 26 ? 0 : 1;
        }
        [Test]
        public void Test() => Utilities.Test(Count);
    }
}
