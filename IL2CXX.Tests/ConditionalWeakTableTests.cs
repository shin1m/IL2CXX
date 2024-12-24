using System.Runtime.CompilerServices;

namespace IL2CXX.Tests;
using static Utilities;

[Parallelizable]
class ConditionalWeakTableTests
{
    static int Default()
    {
        var table = new ConditionalWeakTable<string, string>();
        string? x = null;
        string? y = null;
        WithPadding(() =>
        {
            x = "Hello";
            y = "World";
            table.Add(x, y);
        });
        if (WithPadding(() => !table.TryGetValue(x!, out var z) || z != y)) return 1;
        var (wx, wy) = WithPadding(() => (
            new WeakReference<string>(x!), new WeakReference<string>(y!)
        ));
        WithPadding(() => x = y = null);
        GC.Collect();
        return wx.TryGetTarget(out _) || wy.TryGetTarget(out _) ? 2 : 0;
    }
    static int AddOrUpdate()
    {
        var table = new ConditionalWeakTable<string, string>();
        var x = "Hello";
        table.Add(x, "World");
        var y = "Again";
        table.AddOrUpdate(x, y);
        return table.TryGetValue(x, out var z) && z == y ? 0 : 1;
    }
    static int Clear()
    {
        var table = new ConditionalWeakTable<string, string>();
        var x = "Hello";
        table.Add(x, "World");
        table.Clear();
        return table.TryGetValue(x, out _) ? 1 : 0;
    }
    class Foo
    {
        public string Value = "Bye";
    }
    static int GetOrCreateValue()
    {
        var table = new ConditionalWeakTable<string, Foo>();
        var x = "Hello";
        var y = new Foo { Value = "World" };
        table.Add(x, y);
        if (table.GetOrCreateValue(x) != y) return 1;
        return table.GetOrCreateValue("Good").Value == "Bye" ? 0 : 2;
    }
    static int GetValue()
    {
        var table = new ConditionalWeakTable<string, string>();
        var x = "Hello";
        var y = "World";
        if (table.GetValue(x, k => y) != y) return 1;
        return table.TryGetValue(x, out var z) && z == y ? 0 : 2;
    }
    static int Remove()
    {
        var table = new ConditionalWeakTable<string, string>();
        var x = "Hello";
        table.Add(x, "World");
        if (!table.Remove(x)) return 1;
        if (table.Remove(x)) return 2;
        return table.TryGetValue(x, out _) ? 3 : 0;
    }

    static int Run(string[] arguments) => arguments[0] switch
    {
        nameof(Default) => Default(),
        nameof(AddOrUpdate) => AddOrUpdate(),
        nameof(Clear) => Clear(),
        nameof(GetOrCreateValue) => GetOrCreateValue(),
        nameof(GetValue) => GetValue(),
        nameof(Remove) => Remove(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run);
    [Test]
    public void Test(
        [Values(
            nameof(Default),
            nameof(AddOrUpdate),
            nameof(Clear),
            nameof(GetOrCreateValue),
            nameof(GetValue),
            nameof(Remove)
        )] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
}
