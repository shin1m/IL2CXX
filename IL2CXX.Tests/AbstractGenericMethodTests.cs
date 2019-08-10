using System;
using NUnit.Framework;

namespace IL2CXX.Tests
{
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
}
