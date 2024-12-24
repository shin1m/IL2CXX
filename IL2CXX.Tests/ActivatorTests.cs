namespace IL2CXX.Tests;

[Parallelizable]
class ActivatorTests
{
    class Foo { }
    class Fooo
    {
        public Fooo() { }
        public Fooo(int _) { }
    }
    static int CreateInstance()
    {
        var o = Activator.CreateInstance(typeof(Fooo));
        return o == null ? 1 : 0;
    }
    static int CreateInstance1()
    {
        var o = Activator.CreateInstance(typeof(Fooo), [0]);
        return o == null ? 1 : 0;
    }
    static int CreateInstanceOfT()
    {
        var o = Activator.CreateInstance<Foo>();
        return o == null ? 1 : 0;
    }
    struct Bar { }
    struct Baar
    {
        public Baar(int _) { }
    }
    static int CreateValue()
    {
        var o = Activator.CreateInstance(typeof(Bar));
        return o == null ? 1 : 0;
    }
    static int CreateValue1()
    {
        var o = Activator.CreateInstance(typeof(Baar), [0]);
        return o == null ? 1 : 0;
    }
    static int CreateValueOfT()
    {
        Activator.CreateInstance<Bar>();
        return 0;
    }

    static int Run(string[] arguments) => arguments[0] switch
    {
        nameof(CreateInstance) => CreateInstance(),
        nameof(CreateInstance1) => CreateInstance1(),
        nameof(CreateInstanceOfT) => CreateInstanceOfT(),
        nameof(CreateValue) => CreateValue(),
        nameof(CreateValue1) => CreateValue1(),
        nameof(CreateValueOfT) => CreateValueOfT(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run, null, [
        typeof(Fooo),
        typeof(Baar)
    ]);
    [Test]
    public void Test(
        [Values(
            nameof(CreateInstance),
            nameof(CreateInstance1),
            nameof(CreateInstanceOfT),
            nameof(CreateValue),
            nameof(CreateValue1),
            nameof(CreateValueOfT)
        )] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
}
