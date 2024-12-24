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

    static int Run(string[] arguments) => arguments[0] switch
    {
        nameof(GetForType) => GetForType(),
        nameof(GetForMethod) => GetForMethod(),
        nameof(GetForProperty) => GetForProperty(),
        nameof(IsDefinedForType) => IsDefinedForType(),
        nameof(IsDefinedForMethod) => IsDefinedForMethod(),
        nameof(IsDefinedForProperty) => IsDefinedForProperty(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run, null, [
        typeof(Foo),
        typeof(Bar)
    ], null);
    [Test]
    public void Test(
        [Values(
            nameof(GetForType),
            nameof(GetForMethod),
            nameof(GetForProperty),
            nameof(IsDefinedForType),
            nameof(IsDefinedForMethod),
            nameof(IsDefinedForProperty)
        )] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
}
