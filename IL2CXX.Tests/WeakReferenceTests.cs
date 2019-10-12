using System;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    class WeakReferenceTests
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
        }
        static int Default()
        {
            var x = new Foo();
            var w = new WeakReference(x);
            if (w.Target != x) return 1;
            x = null;
            GC.Collect();
            return w.Target == null ? 0 : 2;
        }
        [Test]
        public void TestDefault() => Utilities.Test(Default);
        static int TrackResurrection()
        {
            var x = new Foo();
            var w = new WeakReference(x, true);
            if (w.Target != x) return 1;
            x = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (w.Target == null) return 2;
            Foo.Resurrected = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return w.Target == null ? 0 : 3;
        }
        [Test]
        public void TestTrackResurrection() => Utilities.Test(TrackResurrection);
        static int SetTarget()
        {
            var x = new Foo();
            var w = new WeakReference(x);
            var y = new Foo();
            w.Target = y;
            return w.Target == y ? 0 : 1;
        }
        [Test]
        public void TestSetTarget() => Utilities.Test(SetTarget);
        static int DefaultOfT()
        {
            var x = new Foo();
            var w = new WeakReference<Foo>(x);
            if (!w.TryGetTarget(out var y) || y != x) return 1;
            x = y = null;
            GC.Collect();
            return w.TryGetTarget(out y) ? 2 : 0;
        }
        [Test]
        public void TestDefaultOfT() => Utilities.Test(DefaultOfT);
        static int TrackResurrectionOfT()
        {
            var x = new Foo();
            var w = new WeakReference<Foo>(x, true);
            if (!w.TryGetTarget(out var y) || y != x) return 1;
            x = y = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (!w.TryGetTarget(out y)) return 2;
            Foo.Resurrected = y = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return w.TryGetTarget(out y) ? 3 : 0;
        }
        [Test]
        public void TestTrackResurrectionOfT() => Utilities.Test(TrackResurrectionOfT);
        static int SetTargetOfT()
        {
            var x = new Foo();
            var w = new WeakReference<Foo>(x);
            var y = new Foo();
            w.SetTarget(y);
            return w.TryGetTarget(out var z) && z == y ? 0 : 1;
        }
        [Test]
        public void TestSetTargetOfT() => Utilities.Test(SetTargetOfT);
    }
}
