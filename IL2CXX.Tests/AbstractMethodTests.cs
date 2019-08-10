using System;
using NUnit.Framework;

namespace IL2CXX.Tests
{
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
}
