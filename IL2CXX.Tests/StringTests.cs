using System;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class StringTests
    {
        static int AssertEquals(string x, string y)
        {
            Console.WriteLine(x);
            return x == y ? 0 : 1;
        }
        static int Equality() => AssertEquals("Hello, World!", "Hello, World!");
        static int Concatenation()
        {
            string f(string name) => $"Hello, {name}!";
            return AssertEquals(f("World"), "Hello, World!");
        }
        static int Format()
        {
            string f(object x, object y) => $"Hello, {x} and {y}!";
            return AssertEquals(f("World", 0), "Hello, World and 0!");
        }
        static int Substring() => AssertEquals("Hello, World!".Substring(7, 5), "World");
        static int ToLowerInvariant() => AssertEquals("Hello, World!".ToLowerInvariant(), "hello, world!");

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(Equality) => Equality(),
            nameof(Concatenation) => Concatenation(),
            nameof(Format) => Format(),
            nameof(Substring) => Substring(),
            nameof(ToLowerInvariant) => ToLowerInvariant(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run);
        [Test]
        public void Test(
            [Values(
                nameof(Equality),
                nameof(Concatenation),
                nameof(Format),
                nameof(Substring),
                nameof(ToLowerInvariant)
            )] string name,
            [Values] bool cooperative
        ) => Utilities.Run(build, cooperative, name);
    }
}
