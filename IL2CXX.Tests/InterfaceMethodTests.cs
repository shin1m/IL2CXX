using System;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
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
}
