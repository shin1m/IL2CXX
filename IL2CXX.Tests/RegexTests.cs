using System.Text.RegularExpressions;

namespace IL2CXX.Tests;

[Parallelizable]
class RegexTests
{
    static int Compiled() => new Regex(@"^[a-zA-Z0-9]\d{2}[a-zA-Z0-9](-\d{3}){2}[a-zA-Z0-9]$", RegexOptions.Compiled).IsMatch("A08Z-931-468A") ? 0 : 1;

    static int Run(string[] arguments) => arguments[0] switch
    {
        nameof(Compiled) => Compiled(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run);
    [Test]
    public void Test(
        [Values(
            nameof(Compiled)
        )] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
}
