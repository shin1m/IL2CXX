namespace IL2CXX.Tests;
using static Utilities;

[Parallelizable]
class WeakReferenceTests
{
    class Foo
    {
        public static Foo? Resurrected;

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
        Foo? x = null;
        var w = WithPadding(() =>
        {
            x = new Foo();
            return new WeakReference(x);
        });
        if (WithPadding(() => w.Target != x)) return 1;
        WithPadding(() => x = null);
        GC.Collect();
        return w.Target == null ? 0 : 2;
    }
    static int TrackResurrection()
    {
        Foo? x = null;
        var w = WithPadding(() =>
        {
            x = new Foo();
            return new WeakReference(x, true);
        });
        if (WithPadding(() => w.Target != x)) return 1;
        WithPadding(() => x = null);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (WithPadding(() => w.Target == null)) return 2;
        WithPadding(() => Foo.Resurrected = null);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        return w.Target == null ? 0 : 3;
    }
    static int SetTarget()
    {
        var x = new Foo();
        var w = new WeakReference(x);
        var y = new Foo();
        w.Target = y;
        return w.Target == y ? 0 : 1;
    }
    static int DefaultOfT()
    {
        Foo? x = null;
        var w = WithPadding(() =>
        {
            x = new Foo();
            return new WeakReference<Foo>(x);
        });
        if (WithPadding(() => !w.TryGetTarget(out var y) || y != x)) return 1;
        WithPadding(() => x = null);
        GC.Collect();
        return w.TryGetTarget(out _) ? 2 : 0;
    }
    static int TrackResurrectionOfT()
    {
        Foo? x = null;
        var w = WithPadding(() =>
        {
            x = new Foo();
            return new WeakReference<Foo>(x, true);
        });
        if (WithPadding(() => !w.TryGetTarget(out var y) || y != x)) return 1;
        WithPadding(() => x = null);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (WithPadding(() => !w.TryGetTarget(out _))) return 2;
        WithPadding(() => Foo.Resurrected = null);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        return w.TryGetTarget(out _) ? 3 : 0;
    }
    static int SetTargetOfT()
    {
        var x = new Foo();
        var w = new WeakReference<Foo>(x);
        var y = new Foo();
        w.SetTarget(y);
        return w.TryGetTarget(out var z) && z == y ? 0 : 1;
    }

    static int Run(string[] arguments) => arguments[0] switch
    {
        nameof(Default) => Default(),
        nameof(TrackResurrection) => TrackResurrection(),
        nameof(SetTarget) => SetTarget(),
        nameof(DefaultOfT) => DefaultOfT(),
        nameof(TrackResurrectionOfT) => TrackResurrectionOfT(),
        nameof(SetTargetOfT) => SetTargetOfT(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run);
    [Test]
    public void Test(
        [Values(
            nameof(Default),
            nameof(TrackResurrection),
            nameof(SetTarget),
            nameof(DefaultOfT),
            nameof(TrackResurrectionOfT),
            nameof(SetTargetOfT)
        )] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
}
