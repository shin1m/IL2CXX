using System;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
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
        static int ConstrainedCallVirtual()
        {
            Console.WriteLine(new Bar("Hello, Bar!").ToString());
            return 0;
        }
        static int ConstrainedCallInterface()
        {
            string f<T>(T x, T y) where T : IEquatable<T> => x.Equals(y) ? "yes" : "no";
            Console.WriteLine(f(0, 1));
            return 0;
        }
        static int ConstrainedCallVirtualReference()
        {
            Console.WriteLine(new Bar<string>("Hello, Bar!").AsString());
            return 0;
        }
        static int Box()
        {
            Console.WriteLine(new Foo(new Bar("Hello, Foo Bar!")).ToString());
            return 0;
        }

        abstract class Foo1
        {
            public abstract string AsString(object x);
        }
        class Bar1 : Foo1
        {
            public override string AsString(object x) => x.ToString();
        }
        static int CallAbstract()
        {
            Console.WriteLine(new Bar1().AsString("Hello, World!"));
            return 0;
        }

        abstract class Foo2
        {
            public abstract string AsString<T>(T x);
        }
        class Bar2 : Foo2
        {
            public override string AsString<T>(T x) => x.ToString();
        }
        static int CallAbstractGeneric()
        {
            Console.WriteLine(new Bar2().AsString("Hello, World!"));
            Console.WriteLine(new Bar2().AsString(0));
            return 0;
        }

        interface IFoo3
        {
            string AsString(object x);
        }
        class Foo3 : IFoo3
        {
            public string AsString(object x) => x.ToString();
        }
        static string Bar3(IFoo3 x, object y) => x.AsString(y);
        static int CallInterface()
        {
            Console.WriteLine(Bar3(new Foo3(), "Hello, World!"));
            return 0;
        }

        interface IFoo4
        {
            string AsString<T>(T x);
        }
        class Foo4 : IFoo4
        {
            public string AsString<T>(T x) => x.ToString();
        }
        static string Bar4<T>(IFoo4 x, T y) => x.AsString(y);
        static int CallInterfaceGeneric()
        {
            Console.WriteLine(Bar4(new Foo4(), "Hello, World!"));
            return 0;
        }

        static event Action<string> Log;
        static int Event()
        {
            var logs = string.Empty;
            Log += x =>
            {
                Console.WriteLine($"Hello, {x}!");
                logs += $"Hello, {x}!\n";
            };
            Log += x =>
            {
                Console.WriteLine($"Good bye, {x}!");
                logs += $"Good bye, {x}!\n";
            };
            Log?.Invoke("World");
            return logs == "Hello, World!\nGood bye, World!\n" ? 0 : 1;
        }
        static string Greet(string x) => $"Hello, {x}!";
        static int Static()
        {
            Func<string, string> greet = Greet;
            var x = greet("World");
            Console.WriteLine(x);
            return x == "Hello, World!" ? 0 : 1;
        }

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(CallVirtual) => CallVirtual(),
            nameof(ConstrainedCallVirtual) => ConstrainedCallVirtual(),
            nameof(ConstrainedCallInterface) => ConstrainedCallInterface(),
            nameof(ConstrainedCallVirtualReference) => ConstrainedCallVirtualReference(),
            nameof(Box) => Box(),
            nameof(CallAbstract) => CallAbstract(),
            nameof(CallAbstractGeneric) => CallAbstractGeneric(),
            nameof(CallInterface) => CallInterface(),
            nameof(CallInterfaceGeneric) => CallInterfaceGeneric(),
            nameof(Event) => Event(),
            nameof(Static) => Static(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run);
        [Test]
        public void Test(
            [Values(
                nameof(CallVirtual),
                nameof(ConstrainedCallVirtual),
                nameof(ConstrainedCallInterface),
                nameof(ConstrainedCallVirtualReference),
                nameof(Box),
                nameof(CallAbstract),
                nameof(CallAbstractGeneric),
                nameof(CallInterface),
                nameof(CallInterfaceGeneric),
                nameof(Event),
                nameof(Static)
            )] string name,
            [Values] bool cooperative
        ) => Utilities.Run(build, cooperative, name);
    }
}
