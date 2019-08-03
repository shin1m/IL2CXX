using System;
using System.Collections;
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
        static int AssertEquals(string x, string y)
        {
            Console.WriteLine(x);
            return x == y ? 0 : 1;
        }
        static int Equality() => AssertEquals("Hello, World!", "Hello, World!");
        [Test]
        public void TestEquality() => Utilities.Test(Equality);
        static int Concatination()
        {
            string f(string name) => $"Hello, {name}!";
            return AssertEquals(f("World"), "Hello, World!");
        }
        [Test]
        public void TestConcatination() => Utilities.Test(Concatination);
        static int Format()
        {
            string f(object x, object y) => $"Hello, {x} and {y}!";
            return AssertEquals(f("World", 0), "Hello, World and 0!");
        }
        [Test]
        public void TestFormat() => Utilities.Test(Format);
        static int Substring() => AssertEquals("Hello, World!".Substring(7, 5), "World");
        [Test]
        public void TestSubstring() => Utilities.Test(Substring);
        static int ToLowerInvariant() => AssertEquals("Hello, World!".ToLowerInvariant(), "hello, world!");
        [Test]
        public void TestToLowerInvariant() => Utilities.Test(ToLowerInvariant);
    }
    class ArrayTests
    {
        static int IsReadOnly()
        {
            string[] xs = { "Hello, World!" };
            return xs.IsReadOnly ? 1 : 0;
        }
        [Test]
        public void TestIsReadOnly() => Utilities.Test(IsReadOnly);
        static int IListIsReadOnly()
        {
            IList xs = new[] { "Hello, World!" };
            return xs.IsReadOnly ? 1 : 0;
        }
        [Test]
        public void TestIListIsReadOnly() => Utilities.Test(IListIsReadOnly);
        static int IListTIsReadOnly()
        {
            IList<string> xs = new[] { "Hello, World!" };
            return xs.IsReadOnly ? 0 : 1;
        }
        [Test]
        public void TestIListTIsReadOnly() => Utilities.Test(IListTIsReadOnly);
        static int IListTCount()
        {
            IList<string> xs = new[] { "Hello, World!" };
            return xs.Count == 1 ? 0 : 1;
        }
        [Test]
        public void TestIListTCount() => Utilities.Test(IListTCount);
        static int IListTGetItem()
        {
            IList<string> xs = new[] { "foo" };
            return xs[0] == "foo" ? 0 : 1;
        }
        [Test]
        public void TestIListTGetItem() => Utilities.Test(IListTGetItem);
        static int IListTSetItem()
        {
            IList<string> xs = new[] { "foo" };
            xs[0] = "bar";
            return xs[0] == "bar" ? 0 : 1;
        }
        [Test]
        public void TestIListTSetItem() => Utilities.Test(IListTSetItem);
        static int IListTCopyTo()
        {
            IList<string> xs = new[] { "World" };
            string[] ys = { "Hello", null };
            xs.CopyTo(ys, 1);
            foreach (var x in ys) Console.WriteLine(x);
            return ys[1] == "World" ? 0 : 1;
        }
        [Test]
        public void TestIListTCopyTo() => Utilities.Test(IListTCopyTo);
        static int GetEnumerator()
        {
            foreach (var x in (IEnumerable<string>)new[] {
                "Hello, World!",
                "Good bye."
            }) Console.WriteLine(x);
            return 0;
        }
        [Test]
        public void TestGetEnumerator() => Utilities.Test(GetEnumerator);
        static int IListTIndexOf()
        {
            IList<string> xs = new[] { "foo", "bar" };
            return xs.IndexOf("bar") == 1 ? 0 : 1;
        }
        [Test]
        public void TestIListTIndexOf() => Utilities.Test(IListTIndexOf);
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
    class LinqTests
    {
        static IEnumerable<string> EnumerateWords(IEnumerable<string> lines)
        {
            var isWords = Enumerable.Range(0, 128).Select(c => c >= 48 && c < 58 || c >= 65 && c < 91 || c == 95 || c >= 97 && c < 123).ToArray();
            bool isWord(char c) => c < 128 && isWords[c];
            IEnumerable<string> matches(string line)
            {
                for (var i = 0; i < line.Length;)
                {
                    while (!isWord(line[i])) if (++i >= line.Length) yield break;
                    var j = i;
                    do ++j; while (j < line.Length && isWord(line[j]));
                    yield return line.Substring(i, j - i).ToLowerInvariant();
                    i = j;
                }
            }
            return lines.SelectMany(matches);
        }
        static int CountWords()
        {
            var word2count = EnumerateWords(new[] {
                "Hello, World!",
                "Hello, this is shin!",
                "Good bye, World!",
                "Bye bye."
            }).GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
            Console.WriteLine($"# of words: {word2count.Count}");
            foreach (var x in word2count) Console.WriteLine($"\t{x.Key}: {x.Value}");
            return word2count.Count == 7 ? 0 : 1;
        }
        [Test]
        public void Test() => Utilities.Test(CountWords);
    }
}
