using System;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    //[Parallelizable]
    class IntPtrTests
    {
        static int Int32()
        {
            var x = new IntPtr(32);
            return x.ToInt32() == 32 ? 0 : 1;
        }
        [Test]
        public void TestInt32() => Utilities.Test(Int32);
        static unsafe int Pointer()
        {
            var x = new IntPtr(32);
            return new IntPtr(x.ToPointer()) == x ? 0 : 1;
        }
        [Test]
        public void TestPointer() => Utilities.Test(Pointer);
    }
}
