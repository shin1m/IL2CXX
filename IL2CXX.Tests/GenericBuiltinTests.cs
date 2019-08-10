using System;
using System.Linq;
using NUnit.Framework;

namespace IL2CXX.Tests
{
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
}
