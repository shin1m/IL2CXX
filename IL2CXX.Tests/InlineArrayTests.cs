using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class InlineArrayTests
    {
        static int One() => ((ReadOnlySpan<string>)["foo"])[0] == "foo" ? 0 : 1;
        static int Three() => ((ReadOnlySpan<string>)["foo", "bar", "zot"]).SequenceEqual(["foo", "bar", "zot"]) ? 0 : 1;
        [InlineArray(4)]
        struct Foo
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string Name;
        }
        static int Unmanaged() => Marshal.SizeOf<Foo>() == 64 ? 0 : 1;

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(One) => One(),
            nameof(Three) => Three(),
            nameof(Unmanaged) => Unmanaged(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run);
        [Test]
        public void Test(
            [Values(
                nameof(One),
                nameof(Three),
                nameof(Unmanaged)
            )] string name,
            [Values] bool cooperative
        ) => Utilities.Run(build, cooperative, name);
    }
}
