using System;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace IL2CXX.Tests
{
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
        [Test]
        public void TestWeak() => Utilities.Test(Weak);
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
                    Console.WriteLine($"x: {x}, h: {h.Target}");
                    x = null;
                });
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (WithPadding(() =>
                {
                    Console.WriteLine($"x: {x}, h: {h.Target}");
                    return h.Target == null;
                })) return 1;
                WithPadding(() => Foo.Resurrected = null);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Console.WriteLine($"x: {x}, h: {h.Target}");
                return h.Target == null ? 0 : 2;
            }
            finally
            {
                h.Free();
            }
        }
        [Test]
        public void TestWeakTrackResurrection() => Utilities.Test(WeakTrackResurrection);
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
                    Console.WriteLine($"w: {w.Target}, wtr: {wtr.Target}");
                    x = null;
                });
                GC.Collect();
                if (WithPadding(() =>
                {
                    Console.WriteLine($"w: {w.Target}, wtr: {wtr.Target}");
                    return w.Target != null;
                })) return 1;
                GC.WaitForPendingFinalizers();
                if (WithPadding(() => wtr.Target == null)) return 2;
                WithPadding(() => Foo.Resurrected = null);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Console.WriteLine($"w: {w.Target}, wtr: {wtr.Target}");
                return wtr.Target == null ? 0 : 3;
            }
            finally
            {
                w.Free();
                wtr.Free();
            }
        }
        [Test]
        public void TestWeakHandles() => Utilities.Test(WeakHandles);
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
        [Test]
        public void TestNormal() => Utilities.Test(Normal);
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
        [Test]
        public void TestPinned() => Utilities.Test(Pinned);
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
        [Test]
        public void TestIsAllocated() => Utilities.Test(IsAllocated);
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
        [Test]
        public void TestIntPtr() => Utilities.Test(IntPtr);
    }
}
