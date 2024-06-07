using System.Numerics;
using System.Runtime.InteropServices;

namespace IL2CXX.Tests;

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
        var n = Marshal.SizeOf(typeof(Point));
        Console.WriteLine($"{n}");
        return n == 8 ? 0 : 1;
    }
    static int SizeOfTypeOfT()
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
    static int StructureToPtr(Func<Name, Name> f)
    {
        var name = f(new()
        {
            First = "abcdefgh",
            Last = "ABCDEFGH"
        });
        return name.First == "abcdefgh" && name.Last == "ABC" ? 0 : 1;
    }
    static int StructureToPtr() => StructureToPtr(x =>
    {
        var p = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Name)));
        try
        {
            Marshal.StructureToPtr((object)x, p, false);
            return (Name)Marshal.PtrToStructure(p, typeof(Name));
        }
        finally
        {
            Marshal.DestroyStructure(p, typeof(Name));
            Marshal.FreeHGlobal(p);
        }
    });
    static int StructureToPtrOfT() => StructureToPtr(x =>
    {
        var p = Marshal.AllocHGlobal(Marshal.SizeOf<Name>());
        try
        {
            Marshal.StructureToPtr(x, p, false);
            return Marshal.PtrToStructure<Name>(p);
        }
        finally
        {
            Marshal.DestroyStructure<Name>(p);
            Marshal.FreeHGlobal(p);
        }
    });

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

    [StructLayout(LayoutKind.Explicit)]
    struct UnionWithReference
    {
        [FieldOffset(8)]
        public string X;
        [FieldOffset(8)]
        public string Y;
    }
    static int ExplicitWithReference()
    {
        var x = new UnionWithReference { X = "foo" };
        x.Y = "bar";
        return x.X == "bar" ? 0 : 1;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct Child
    {
        [FieldOffset(0)]
        public Vector3 Min;
        [FieldOffset(12)]
        public int Index;
        [FieldOffset(16)]
        public Vector3 Max;
        [FieldOffset(28)]
        public int Count;
    }
    [StructLayout(LayoutKind.Explicit)]
    struct Node
    {
        [FieldOffset(0)]
        public Child A;
        [FieldOffset(32)]
        public Child B;
    }
    static int ExplicitComposite()
    {
        var x = new Node { B = { Count = 1 } };
        var y = x;
        return x.B.Count == 1 ? 0 : 1;
    }

    static void Foo(IntPtr x, IntPtr y) { }
    static int GetFunctionPointerForDelegate() => Marshal.GetFunctionPointerForDelegate((Action<IntPtr, IntPtr>)Foo) == IntPtr.Zero ? 1 : 0;
    delegate IntPtr BarDelegate(IntPtr x, ref IntPtr y);
    static IntPtr Bar(IntPtr x, ref IntPtr y) => new IntPtr((int)x + (int)y);
    static int GetDelegateForFunctionPointer(Func<IntPtr, BarDelegate> get)
    {
        var p = Marshal.GetFunctionPointerForDelegate((BarDelegate)Bar);
        var d = get(p);
        var y = new IntPtr(2);
        if (d(new IntPtr(1), ref y) != new IntPtr(3)) return 1;
        return p == Marshal.GetFunctionPointerForDelegate(d) ? 0 : 1;
    }
    static int GetDelegateForFunctionPointer() => GetDelegateForFunctionPointer(x => (BarDelegate)Marshal.GetDelegateForFunctionPointer(x, typeof(BarDelegate)));
    static int GetDelegateForFunctionPointerOfT() => GetDelegateForFunctionPointer(Marshal.GetDelegateForFunctionPointer<BarDelegate>);

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
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return 0;
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
        nameof(SizeOfTypeOfT) => SizeOfTypeOfT(),
        nameof(SizeOfInstance) => SizeOfInstance(),
        nameof(SizeOfByValTStr) => SizeOfByValTStr(),
        nameof(StructureToPtr) => StructureToPtr(),
        nameof(StructureToPtrOfT) => StructureToPtrOfT(),
        nameof(Explicit) => Explicit(),
        nameof(ExplicitWithReference) => ExplicitWithReference(),
        nameof(ExplicitComposite) => ExplicitComposite(),
        nameof(GetFunctionPointerForDelegate) => GetFunctionPointerForDelegate(),
        nameof(GetDelegateForFunctionPointer) => GetDelegateForFunctionPointer(),
        nameof(GetDelegateForFunctionPointerOfT) => GetDelegateForFunctionPointerOfT(),
        nameof(Parameter) => Parameter(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run);
    [Test]
    public void Test(
        [Values(
            nameof(SizeOfType),
            nameof(SizeOfTypeOfT),
            nameof(SizeOfInstance),
            nameof(SizeOfByValTStr),
            nameof(StructureToPtr),
            nameof(StructureToPtrOfT),
            nameof(Explicit),
            nameof(ExplicitWithReference),
            nameof(ExplicitComposite),
            nameof(GetFunctionPointerForDelegate),
            nameof(GetDelegateForFunctionPointer),
            nameof(GetDelegateForFunctionPointerOfT),
            nameof(Parameter)
        )] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
}
