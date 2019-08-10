using System;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    class StringTests
    {
        static int AssertEquals(string x, string y)
        {
            Console.WriteLine(x);
            return x == y ? 0 : 1;
        }
        static int Equality() => AssertEquals("Hello, World!", "Hello, World!");
        [Test]
        public void TestEquality() => Utilities.Test(Equality);
        static int Concatination()
        {
            string f(string name) => $"Hello, {name}!";
            return AssertEquals(f("World"), "Hello, World!");
        }
        [Test]
        public void TestConcatination() => Utilities.Test(Concatination);
        static int Format()
        {
            string f(object x, object y) => $"Hello, {x} and {y}!";
            return AssertEquals(f("World", 0), "Hello, World and 0!");
        }
        [Test]
        public void TestFormat() => Utilities.Test(Format);
        static int Substring() => AssertEquals("Hello, World!".Substring(7, 5), "World");
        [Test]
        public void TestSubstring() => Utilities.Test(Substring);
        static int ToLowerInvariant() => AssertEquals("Hello, World!".ToLowerInvariant(), "hello, world!");
        [Test]
        public void TestToLowerInvariant() => Utilities.Test(ToLowerInvariant);
    }
}
