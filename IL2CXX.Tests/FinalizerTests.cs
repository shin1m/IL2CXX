namespace IL2CXX.Tests;
using static Utilities;

[Parallelizable]
class FinalizerTests
{
    class Foo : IDisposable
    {
        public static int Finalized;

        string message;
        Foo? foo;

        public Foo(bool cyclic)
        {
            if (cyclic)
            {
                message = "cyclic";
                foo = this;
            }
            else
            {
                message = "acyclic";
            }
        }
        ~Foo()
        {
            Console.WriteLine($"~Foo: {message}");
            ++Finalized;
        }
        public void Dispose() => GC.SuppressFinalize(this);
    }
    static int CollectAndWait()
    {
        WithPadding(() =>
        {
            new Foo(true);
            new Foo(false);
        });
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine($"finalized: {Foo.Finalized}");
        return Foo.Finalized == 2 ? 0 : 1;
    }
    static int Suppress()
    {
        WithPadding(() =>
        {
            using (new Foo(false)) { }
        });
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine($"finalized: {Foo.Finalized}");
        return Foo.Finalized == 0 ? 0 : 1;
    }

    class Bar
    {
        public static Bar? Resurrected;
        public static int Finalized;

        bool resurrected;

        ~Bar()
        {
            if (resurrected)
            {
                Console.WriteLine("~Bar: finalize");
                ++Finalized;
            }
            else
            {
                Console.WriteLine("~Bar: resurrect");
                Resurrected = this;
                resurrected = true;
                GC.ReRegisterForFinalize(this);
            }
        }
    }
    static int Resurrect()
    {
        WithPadding(() =>
        {
            new Bar();
        });
        GC.Collect();
        GC.WaitForPendingFinalizers();
        WithPadding(() => Console.WriteLine($"resurrected: {Bar.Resurrected}"));
        if (WithPadding(() => Bar.Resurrected == null)) return 1;
        WithPadding(() => Bar.Resurrected = null);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine($"finalized: {Bar.Finalized}");
        return Bar.Finalized == 1 ? 0 : 2;
    }

    static int Run(string[] arguments) => arguments[1] switch
    {
        nameof(CollectAndWait) => CollectAndWait(),
        nameof(Suppress) => Suppress(),
        nameof(Resurrect) => Resurrect(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run);
    [Test]
    public void Test(
        [Values(
            nameof(CollectAndWait),
            nameof(Suppress),
            nameof(Resurrect)
        )] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
}
