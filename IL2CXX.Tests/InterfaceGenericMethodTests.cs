using System;
using NUnit.Framework;

namespace IL2CXX.Tests
{
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
}
