using System;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    //[Parallelizable]
    class ActivatorTests
    {
        class Foo { }
        static int CreateInstance()
        {
            var o = Activator.CreateInstance(typeof(Foo));
            return o == null ? 1 : 0;
        }
        [Test]
        public void TestCreateInstance() => Utilities.Test(CreateInstance);
        static int CreateInstanceOfT()
        {
            var o = Activator.CreateInstance<Foo>();
            return o == null ? 1 : 0;
        }
        [Test]
        public void TestCreateInstanceOfT() => Utilities.Test(CreateInstanceOfT);
        struct Bar { }
        static int CreateValue()
        {
            var o = Activator.CreateInstance(typeof(Bar));
            return o == null ? 1 : 0;
        }
        [Test]
        public void TestCreateValue() => Utilities.Test(CreateValue);
        static int CreateValueOfT()
        {
            Activator.CreateInstance<Bar>();
            return 0;
        }
        [Test]
        public void TestCreateValueOfT() => Utilities.Test(CreateValueOfT);
    }
}
