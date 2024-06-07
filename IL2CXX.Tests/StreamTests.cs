namespace IL2CXX.Tests;

[Parallelizable]
class StreamTests
{
    const string FileName = nameof(StreamTests) + "-file";
    static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);

    static void DeleteFile()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
    }
    static bool BytesEquals(ReadOnlySpan<byte> xs, params byte[] ys)
    {
        foreach (var x in xs) Console.WriteLine($"{(int)x}");
        var n = xs.Length;
        if (n != ys.Length) return false;
        for (var i = 0; i < n; ++i) if (xs[i] != ys[i]) return false;
        return true;
    }
    [SetUp]
    public void SetUp() => DeleteFile();
    [TearDown]
    public void TearDown() => DeleteFile();

    static int ReadMemory()
    {
        using var stream = new MemoryStream([0, 1]);
        var xs = new byte[4];
        return stream.Read(xs) == 2 && BytesEquals(xs.AsSpan(0, 2), 0, 1) ? 0 : 1;
    }
    static int WriteMemory()
    {
        using var stream = new MemoryStream();
        stream.Write([0, 1]);
        return stream.TryGetBuffer(out var xs) && BytesEquals(xs, 0, 1) ? 0 : 1;
    }
    static int ReadFile()
    {
        using var stream = File.OpenRead(Path.Combine(Path.GetDirectoryName(Environment.CurrentDirectory), FileName));
        var xs = new byte[4];
        return stream.Read(xs) == 2 && BytesEquals(xs.AsSpan(0, 2), 0, 1) ? 0 : 1;
    }
    static int WriteFile()
    {
        using (var stream = File.OpenWrite(Path.Combine(Path.GetDirectoryName(Environment.CurrentDirectory), FileName))) stream.Write([0, 1]);
        return 0;
    }
    static int ReadTextFile()
    {
        using var reader = File.OpenText(Path.Combine(Path.GetDirectoryName(Environment.CurrentDirectory), FileName));
        if (reader.ReadLine() != "Hello, World!") return 1;
        if (reader.ReadLine() != "Good bye.") return 2;
        return reader.ReadLine() == null ? 0 : 3;
    }
    static int WriteTextFile()
    {
        using (var writer = File.CreateText(Path.Combine(Path.GetDirectoryName(Environment.CurrentDirectory), FileName)))
        {
            writer.WriteLine("Hello, World!");
            writer.WriteLine("Good bye.");
        }
        return 0;
    }

    static int Run(string[] arguments) => arguments[1] switch
    {
        nameof(ReadMemory) => ReadMemory(),
        nameof(WriteMemory) => WriteMemory(),
        nameof(ReadFile) => ReadFile(),
        nameof(WriteFile) => WriteFile(),
        nameof(ReadTextFile) => ReadTextFile(),
        nameof(WriteTextFile) => WriteTextFile(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run);
    [Test]
    public void Test(
        [Values(
            nameof(ReadMemory),
            nameof(WriteMemory)
        )] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
    [Test]
    public void TestReadFile([Values] bool cooperative)
    {
        File.WriteAllBytes(FilePath, [0, 1]);
        Utilities.Run(build, cooperative, nameof(ReadFile));
    }
    [Test]
    public void TestWriteFile([Values] bool cooperative)
    {
        Utilities.Run(build, cooperative, nameof(WriteFile));
        BytesEquals(File.ReadAllBytes(FilePath), 0, 1);
    }
    [Test]
    public void TestReadTextFile([Values] bool cooperative)
    {
        File.WriteAllLines(FilePath, [
            "Hello, World!",
            "Good bye."
        ]);
        Utilities.Run(build, cooperative, nameof(ReadTextFile));
    }
    [Test]
    public void TestWriteTextFile([Values] bool cooperative)
    {
        Utilities.Run(build, cooperative, nameof(WriteTextFile));
        Assert.That(File.ReadLines(FilePath), Is.EqualTo(new[]
        {
            "Hello, World!",
            "Good bye."
        }));
    }
}
