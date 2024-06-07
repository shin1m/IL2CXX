using System.Runtime.InteropServices;

namespace IL2CXX.Tests;
using static Utilities;

[Parallelizable]
class GCHandleTests
{
    class Foo
    {
        public static Foo Resurrected;

        bool resurrected;

        ~Foo()
        {
            if (resurrected)
            {
                Console.WriteLine("~Foo: finalize");
            }
            else
            {
                Console.WriteLine("~Foo: resurrect");
                Resurrected = this;
                resurrected = true;
                GC.ReRegisterForFinalize(this);
            }
        }
        public override string ToString() => "Foo";
    }
    static int Weak()
    {
        Foo x = null;
        var h = WithPadding(() =>
        {
            x = new Foo();
            return GCHandle.Alloc(x, GCHandleType.Weak);
        });
        try
        {
            WithPadding(() =>
            {
                Console.WriteLine($"h: {h.Target}");
                x = null;
            });
            GC.Collect();
            Console.WriteLine($"h: {h.Target}");
            return h.Target == null ? 0 : 2;
        }
        finally
        {
            h.Free();
        }
    }
    static int WeakTrackResurrection()
    {
        Foo x = null;
        var h = WithPadding(() =>
        {
            x = new Foo();
            return GCHandle.Alloc(x, GCHandleType.WeakTrackResurrection);
        });
        try
        {
            WithPadding(() =>
            {
                Console.WriteLine($"x: {x}, h: {h.Target}, resurrected: {Foo.Resurrected}");
                x = null;
            });
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (!WithPadding(() =>
            {
                Console.WriteLine($"x: {x}, h: {h.Target}, resurrected: {Foo.Resurrected}");
                return h.Target != null && Foo.Resurrected != null;
            })) return 1;
            WithPadding(() => Foo.Resurrected = null);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.Collect();
            Console.WriteLine($"x: {x}, h: {h.Target}, resurrected: {Foo.Resurrected}");
            return h.Target == null ? 0 : 2;
        }
        finally
        {
            h.Free();
        }
    }
    static int WeakHandles()
    {
        Foo x = null;
        var (w, wtr) = WithPadding(() =>
        {
            x = new Foo();
            return (
                GCHandle.Alloc(x, GCHandleType.Weak),
                GCHandle.Alloc(x, GCHandleType.WeakTrackResurrection)
            );
        });
        try
        {
            WithPadding(() =>
            {
                Console.WriteLine($"w: {w.Target}, wtr: {wtr.Target}, resurrected: {Foo.Resurrected}");
                x = null;
            });
            GC.Collect();
            if (!WithPadding(() =>
            {
                Console.WriteLine($"w: {w.Target}, wtr: {wtr.Target}, resurrected: {Foo.Resurrected}");
                return w.Target == null;
            })) return 1;
            GC.WaitForPendingFinalizers();
            if (!WithPadding(() =>
            {
                Console.WriteLine($"w: {w.Target}, wtr: {wtr.Target}, resurrected: {Foo.Resurrected}");
                return wtr.Target != null && Foo.Resurrected != null;
            })) return 2;
            WithPadding(() => Foo.Resurrected = null);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.Collect();
            Console.WriteLine($"w: {w.Target}, wtr: {wtr.Target}, resurrected: {Foo.Resurrected}");
            return wtr.Target == null ? 0 : 3;
        }
        finally
        {
            w.Free();
            wtr.Free();
        }
    }
    static int Normal()
    {
        var x = "foo";
        var h = GCHandle.Alloc(x);
        try
        {
            Console.WriteLine($"h: {h.Target}");
            x = null;
            GC.Collect();
            Console.WriteLine($"h: {h.Target}");
            return h.Target == null ? 1 : 0;
        }
        finally
        {
            h.Free();
        }
    }
    static int Pinned()
    {
        var x = "foo";
        var h = GCHandle.Alloc(x, GCHandleType.Pinned);
        try
        {
            Console.WriteLine($"Target: {h.Target}, Address: {h.AddrOfPinnedObject()}");
            x = null;
            GC.Collect();
            Console.WriteLine($"Target: {h.Target}, Address: {h.AddrOfPinnedObject()}");
            return h.Target == null ? 1 : 0;
        }
        finally
        {
            h.Free();
        }
    }
    static int IsAllocated()
    {
        var h = new GCHandle();
        Console.WriteLine($"allocated: {h.IsAllocated}");
        if (h.IsAllocated) return 1;
        h = GCHandle.Alloc(null);
        try
        {
            Console.WriteLine($"allocated: {h.IsAllocated}");
            if (!h.IsAllocated) return 2;
        }
        finally
        {
            h.Free();
        }
        return h.IsAllocated ? 3 : 0;
    }
    static int IntPtr()
    {
        object x = "foo";
        var h = GCHandle.Alloc(x);
        try
        {
            var p = GCHandle.ToIntPtr(h);
            var y = GCHandle.FromIntPtr(p).Target;
            Console.WriteLine($"x: {x}, y: {y}");
            return x == y ? 0 : 1;
        }
        finally
        {
            h.Free();
        }
    }

    static int Run(string[] arguments) => arguments[1] switch
    {
        nameof(Weak) => Weak(),
        nameof(WeakTrackResurrection) => WeakTrackResurrection(),
        nameof(WeakHandles) => WeakHandles(),
        nameof(Normal) => Normal(),
        nameof(Pinned) => Pinned(),
        nameof(IsAllocated) => IsAllocated(),
        nameof(IntPtr) => IntPtr(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run);
    [Test]
    public void Test(
        [Values(
            nameof(Weak),
            nameof(WeakTrackResurrection),
            nameof(WeakHandles),
            nameof(Normal),
            nameof(Pinned),
            nameof(IsAllocated),
            nameof(IntPtr)
        )] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
}
