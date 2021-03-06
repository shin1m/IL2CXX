using System;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class ActivatorTests
    {
        class Foo { }
        static int CreateInstance()
        {
            var o = Activator.CreateInstance(typeof(Foo));
            return o == null ? 1 : 0;
        }
        static int CreateInstanceOfT()
        {
            var o = Activator.CreateInstance<Foo>();
            return o == null ? 1 : 0;
        }
        struct Bar { }
        static int CreateValue()
        {
            var o = Activator.CreateInstance(typeof(Bar));
            return o == null ? 1 : 0;
        }
        static int CreateValueOfT()
        {
            Activator.CreateInstance<Bar>();
            return 0;
        }

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(CreateInstance) => CreateInstance(),
            nameof(CreateInstanceOfT) => CreateInstanceOfT(),
            nameof(CreateValue) => CreateValue(),
            nameof(CreateValueOfT) => CreateValueOfT(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run);
        [TestCase(nameof(CreateInstance))]
        [TestCase(nameof(CreateInstanceOfT))]
        [TestCase(nameof(CreateValue))]
        [TestCase(nameof(CreateValueOfT))]
        public void Test(string name) => Utilities.Run(build, name);
    }
}
