using System;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    //[Parallelizable]
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
        static int ConstrainedCallInterface()
        {
            string f<T>(T x, T y) where T : IEquatable<T> => x.Equals(y) ? "yes" : "no";
            Console.WriteLine(f(0, 1));
            return 0;
        }
        [Test]
        public void TestConstrainedCallInterface() => Utilities.Test(ConstrainedCallInterface);
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
}
