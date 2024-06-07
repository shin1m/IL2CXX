using System.Text;

namespace IL2CXX.Tests;

[Parallelizable]
class EncodingTests
{
    static int GetBytes()
    {
        var xs = Encoding.UTF8.GetBytes("\u03a0");
        if (xs.Length != 2) return 1;
        if (xs[0] != 0xce) return 2;
        if (xs[1] != 0xa0) return 3;
        return 0;
    }
    static int GetString()
    {
        var x = Encoding.UTF8.GetString([(byte)0xce, (byte)0xa0]);
        Console.WriteLine(x);
        return x == "\u03a0" ? 0 : 1;
    }
    static int Convert()
    {
        var utf8 = Encoding.UTF8.GetBytes("\u03a0");
        var ascii = Encoding.Convert(Encoding.UTF8, Encoding.ASCII, utf8);
        if (ascii.Length != 1) return 1;
        if (ascii[0] != '?') return 2;
        return 0;
    }

    static int Run(string[] arguments) => arguments[1] switch
    {
        nameof(GetBytes) => GetBytes(),
        nameof(GetString) => GetString(),
        nameof(Convert) => Convert(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run);
    [Test]
    public void Test(
        [Values(
            nameof(GetBytes),
            nameof(GetString),
            nameof(Convert)
        )] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
}
