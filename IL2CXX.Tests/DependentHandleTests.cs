using System.Runtime;

namespace IL2CXX.Tests;
using static Utilities;

[Parallelizable]
class DependentHandleTests
{
    static int Default()
    {
        object? x = null;
        using var handle = WithPadding(() =>
        {
            x = "Hello";
            return new DependentHandle(x, "World");
        });
        if (!WithPadding(() =>
        {
            var (target, dependent) = handle.TargetAndDependent;
            return target == x && dependent is string y && y == "World";
        })) return 1;
        WithPadding(() => x = null);
        GC.Collect();
        return handle.TargetAndDependent == default ? 0 : 2;
    }

    static int Run(string[] arguments) => arguments[0] switch
    {
        nameof(Default) => Default(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run);
    [Test]
    public void Test(
        [Values(nameof(Default))] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
}
