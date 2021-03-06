using System;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class MarshalTests
    {
        struct Point
        {
            public int X;
            public int Y;
        }
        static int SizeOfType()
        {
            var n = Marshal.SizeOf<Point>();
            Console.WriteLine($"{n}");
            return n == 8 ? 0 : 1;
        }
        static int SizeOfInstance()
        {
            var n = Marshal.SizeOf(new Point { X = 0, Y = 1 });
            Console.WriteLine($"{n}");
            return n == 8 ? 0 : 1;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct Name
        {
            public string First;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
            public string Last;
        }
        static int SizeOfByValTStr()
        {
            var n = Marshal.SizeOf<Name>();
            Console.WriteLine($"{n}");
            return n == Marshal.SizeOf<IntPtr>() + 4 ? 0 : 1;
        }
        static int StructureToPtr()
        {
            var p = Marshal.AllocHGlobal(Marshal.SizeOf<Name>());
            try
            {
                Marshal.StructureToPtr(new Name
                {
                    First = "abcdefgh",
                    Last = "ABCDEFGH"
                }, p, false);
                var name = Marshal.PtrToStructure<Name>(p);
                return name.First == "abcdefgh" && name.Last == "ABC" ? 0 : 1;
            }
            finally
            {
                Marshal.DestroyStructure<Name>(p);
                Marshal.FreeHGlobal(p);
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        struct Union
        {
            [FieldOffset(4)]
            public int X;
            [FieldOffset(4)]
            public int Y;
        }
        static int Explicit()
        {
            var x = new Union { X = 1 };
            x.Y = 2;
            return x.X == 2 ? 0 : 1;
        }

        static void Foo(IntPtr x, IntPtr y) { }
        static int GetFunctionPointerForDelegate() =>
            Marshal.GetFunctionPointerForDelegate((Action<IntPtr, IntPtr>)Foo) == IntPtr.Zero ? 1 : 0;
        delegate IntPtr BarDelegate(IntPtr x, ref IntPtr y);
        static IntPtr Bar(IntPtr x, ref IntPtr y) => new IntPtr((int)x + (int)y);
        static int GetDelegateForFunctionPointer()
        {
            var p = Marshal.GetFunctionPointerForDelegate((BarDelegate)Bar);
            var d = Marshal.GetDelegateForFunctionPointer<BarDelegate>(p);
            var y = new IntPtr(2);
            if (d(new IntPtr(1), ref y) != new IntPtr(3)) return 1;
            return p == Marshal.GetFunctionPointerForDelegate(d) ? 0 : 1;
        }

        struct utsname
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string sysname;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string nodename;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string release;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string version;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string machine;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string extra;
        }
        [DllImport("libc")]
        static extern void uname(out utsname name);
        static int Parameter()
        {
            uname(out var name);
            Console.WriteLine($"sysname: {name.sysname}");
            Console.WriteLine($"nodename: {name.nodename}");
            Console.WriteLine($"release: {name.release}");
            Console.WriteLine($"version: {name.version}");
            Console.WriteLine($"machine: {name.machine}");
            return 0;
        }

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(SizeOfType) => SizeOfType(),
            nameof(SizeOfInstance) => SizeOfInstance(),
            nameof(SizeOfByValTStr) => SizeOfByValTStr(),
            nameof(StructureToPtr) => StructureToPtr(),
            nameof(Explicit) => Explicit(),
            nameof(GetFunctionPointerForDelegate) => GetFunctionPointerForDelegate(),
            nameof(GetDelegateForFunctionPointer) => GetDelegateForFunctionPointer(),
            nameof(Parameter) => Parameter(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run);
        [TestCase(nameof(SizeOfType))]
        [TestCase(nameof(SizeOfInstance))]
        [TestCase(nameof(SizeOfByValTStr))]
        [TestCase(nameof(StructureToPtr))]
        [TestCase(nameof(Explicit))]
        [TestCase(nameof(GetFunctionPointerForDelegate))]
        [TestCase(nameof(GetDelegateForFunctionPointer))]
        [TestCase(nameof(Parameter))]
        public void Test(string name) => Utilities.Run(build, name);
    }
}
