using System.Reflection;

namespace IL2CXX.Tests;

[Parallelizable]
class AttributeTests
{
    class FooAttribute : Attribute { }
    class BarAttribute : Attribute { }
    [Foo]
    class Foo
    {
        [Foo]
        public virtual int X { get; set; }
        [Foo]
        public virtual void Do() { }
    }
    [Bar]
    class Bar : Foo
    {
        [Bar]
        public override int X { set { } }
        [Bar]
        public override void Do() { }
    }
    static int Get(MemberInfo member)
    {
        var cas = Attribute.GetCustomAttributes(member);
        if (cas.Length != 2) return 1;
        if (!(cas[0] is BarAttribute)) return 2;
        if (!(cas[1] is FooAttribute)) return 3;
        return 0;
    }
    static int GetForType() => Get(typeof(Bar));
    static int GetForMethod() => Get(typeof(Bar).GetMethod(nameof(Foo.Do)) ?? throw new Exception());
    static int GetForProperty() => Get(typeof(Bar).GetProperty(nameof(Foo.X)) ?? throw new Exception());
    static int IsDefined(MemberInfo member) => Attribute.IsDefined(member, typeof(FooAttribute)) ? 0 : 1;
    static int IsDefinedForType() => IsDefined(typeof(Bar));
    static int IsDefinedForMethod() => IsDefined(typeof(Bar).GetMethod(nameof(Foo.Do)) ?? throw new Exception());
    static int IsDefinedForProperty() => IsDefined(typeof(Bar).GetProperty(nameof(Foo.X)) ?? throw new Exception());

    class ZotAttribute : Attribute
    {
        public ZotAttribute(string? s0, string? s1, Type t, int i, object? o0, object? o1, string?[] ss, Type[] ts, int[] @is, object?[] os)
        {
            String0 = s0;
            String1 = s1;
            Type = t;
            Int32 = i;
            Object0 = o0;
            Object1 = o1;
            Strings = ss;
            Types = ts;
            Int32s = @is;
            Objects = os;
        }
        public readonly string? String0;
        public readonly string? String1;
        public readonly Type Type;
        public readonly int Int32;
        public readonly object? Object0;
        public readonly object? Object1;
        public readonly string?[] Strings;
        public readonly Type[] Types;
        public readonly int[] Int32s;
        public readonly object?[] Objects;
    }
    [Zot(null, "foo", typeof(Foo), 32, null, 64, ["foo", null, "bar"], [typeof(Foo), typeof(Bar)], [32, 64], [null, "foo", typeof(Foo), 32])]
    class Zot { }
    static int GetData()
    {
        var zot = typeof(Zot).GetCustomAttribute<ZotAttribute>();
        if (zot == null) return 1;
        if (zot.String0 != null) return 2;
        if (zot.String1 != "foo") return 3;
        if (zot.Type != typeof(Foo)) return 4;
        if (zot.Int32 != 32) return 5;
        if (zot.Object0 != null) return 6;
        if (zot.Object1 is not int x || x != 64) return 7;
        if (!zot.Strings.SequenceEqual(["foo", null, "bar"])) return 8;
        if (!zot.Types.SequenceEqual([typeof(Foo), typeof(Bar)])) return 9;
        if (!zot.Int32s.SequenceEqual([32, 64])) return 10;
        if (!zot.Objects.SequenceEqual([null, "foo", typeof(Foo), 32])) return 11;
        return 0;
    }

    static int Run(string[] arguments) => arguments[0] switch
    {
        nameof(GetForType) => GetForType(),
        nameof(GetForMethod) => GetForMethod(),
        nameof(GetForProperty) => GetForProperty(),
        nameof(IsDefinedForType) => IsDefinedForType(),
        nameof(IsDefinedForMethod) => IsDefinedForMethod(),
        nameof(IsDefinedForProperty) => IsDefinedForProperty(),
        nameof(GetData) => GetData(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run, null, [
        typeof(Foo),
        typeof(Bar),
        typeof(Zot)
    ], null);
    [Test]
    public void Test(
        [Values(
            nameof(GetForType),
            nameof(GetForMethod),
            nameof(GetForProperty),
            nameof(IsDefinedForType),
            nameof(IsDefinedForMethod),
            nameof(IsDefinedForProperty),
            nameof(GetData)
        )] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
}
