namespace IL2CXX.Tests;

[Parallelizable]
class StringTests
{
    static int AssertEquals(string x, string y)
    {
        Console.WriteLine(x);
        return x == y ? 0 : 1;
    }
    static int Equality() => AssertEquals("Hello, World!", "Hello, World!");
    static int Concatenation()
    {
        string f(string name) => $"Hello, {name}!";
        return AssertEquals(f("World"), "Hello, World!");
    }
    static int EqualsIgnoreCase() => "Hello, World!".Equals("hello, world!", StringComparison.InvariantCultureIgnoreCase) ? 0 : 1;
    static int Format()
    {
        string f(object x, object y) => $"Hello, {x} and {y}!";
        return AssertEquals(f("World", 0), "Hello, World and 0!");
    }
    static int IsNormalized() => "abc".IsNormalized() ? 0 : 1;
    static int Join() => AssertEquals(string.Join("/", 0, 1), "0/1");
    static int Split()
    {
        var xs = "a/b".Split('/');
        return xs.Length != 2 ? 1 : xs[0] != "a" ? 2 : xs[1] != "b" ? 3 : 0;
    }
    static int Substring() => AssertEquals("Hello, World!".Substring(7, 5), "World");
    static int ToLowerInvariant() => AssertEquals("Hello, World!".ToLowerInvariant(), "hello, world!");
    static int Surrogate() => "\uD8000\uDB800\uDC00".Length == 5 ? 0 : 1;

    static int Run(string[] arguments) => arguments[0] switch
    {
        nameof(Equality) => Equality(),
        nameof(Concatenation) => Concatenation(),
        nameof(EqualsIgnoreCase) => EqualsIgnoreCase(),
        nameof(Format) => Format(),
        nameof(IsNormalized) => IsNormalized(),
        nameof(Join) => Join(),
        nameof(Split) => Split(),
        nameof(Substring) => Substring(),
        nameof(ToLowerInvariant) => ToLowerInvariant(),
        nameof(Surrogate) => Surrogate(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run);
    [Test]
    public void Test(
        [Values(
            nameof(Equality),
            nameof(Concatenation),
            nameof(EqualsIgnoreCase),
            nameof(Format),
            nameof(IsNormalized),
            nameof(Join),
            nameof(Split),
            nameof(Substring),
            nameof(ToLowerInvariant),
            nameof(Surrogate)
        )] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
}
