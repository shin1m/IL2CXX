using System.Collections;

namespace IL2CXX.Tests;

[Parallelizable]
class ArrayTests
{
    static int AssertEquals(string?[] xs, string?[] ys)
    {
        foreach (var x in xs) Console.WriteLine(x ?? "(null)");
        var n = xs.Length;
        if (n != ys.Length) return 1;
        for (var i = 0; i < n; ++i) if (xs[i] != ys[i]) return 2;
        return 0;
    }
    static int IsReadOnly()
    {
        string[] xs = { "Hello, World!" };
        return xs.IsReadOnly ? 1 : 0;
    }
    static int Clear()
    {
        string?[] xs = { "Hello", "World", "Good", "Bye" };
        Array.Clear(xs, 1, 2);
        return AssertEquals(xs, ["Hello", null, null, "Bye"]);
    }
    static int ClearAll()
    {
        string[] xs = { "Hello", "World" };
        Array.Clear(xs);
        return AssertEquals(xs, [null, null]);
    }
    static int Copy()
    {
        string[] xs = { "Hello", "World", "Good", "Bye" };
        var ys = new string[6];
        Array.Copy(xs, 1, ys, 2, 3);
        return AssertEquals(ys, [null, null, "World", "Good", "Bye", null]);
    }
    static int ResizeLarger()
    {
        string[] xs = { "Hello", "World", "Good", "Bye" };
        Array.Resize(ref xs, 6);
        return AssertEquals(xs, ["Hello", "World", "Good", "Bye", null, null]);
    }
    static int ResizeSmaller()
    {
        string[] xs = { "Hello", "World", "Good", "Bye" };
        Array.Resize(ref xs, 3);
        return AssertEquals(xs, ["Hello", "World", "Good"]);
    }
    static int IListIsReadOnly()
    {
        IList xs = new[] { "Hello, World!" };
        return xs.IsReadOnly ? 1 : 0;
    }
    static int IListTIsReadOnly()
    {
        IList<string> xs = new[] { "Hello, World!" };
        return xs.IsReadOnly ? 0 : 1;
    }
    static int IListTCount()
    {
        IList<string> xs = new[] { "Hello, World!" };
        return xs.Count == 1 ? 0 : 1;
    }
    static int IListTGetItem()
    {
        IList<string> xs = new[] { "foo" };
        return xs[0] == "foo" ? 0 : 1;
    }
    static int IListTSetItem()
    {
        IList<string> xs = new[] { "foo" };
        xs[0] = "bar";
        return xs[0] == "bar" ? 0 : 1;
    }
    static int IListTCopyTo()
    {
        IList<string?> xs = new[] { "World" };
        string?[] ys = { "Hello", null };
        xs.CopyTo(ys, 1);
        return AssertEquals(ys, ["Hello", "World"]);
    }
    static int GetEnumerator()
    {
        foreach (var x in (IEnumerable<string>)new[]
        {
            "Hello, World!",
            "Good bye."
        }) Console.WriteLine(x);
        return 0;
    }
    static int IListTIndexOf()
    {
        IList<string> xs = new[] { "foo", "bar" };
        return xs.IndexOf("bar") == 1 ? 0 : 1;
    }
    static int Clone()
    {
        string[] xs = { "foo", "bar" };
        return AssertEquals((string[])xs.Clone(), xs);
    }
    static int Reverse()
    {
        string[] xs = { "foo", "bar" };
        Array.Reverse((Array)xs);
        return AssertEquals(xs, ["bar", "foo"]);
    }
    static int ReverseT()
    {
        string[] xs = { "foo", "bar" };
        Array.Reverse(xs);
        return AssertEquals(xs, ["bar", "foo"]);
    }
    static int New1() => AssertEquals(new string[1], [null]);
    static int New2() => (new string[1, 1])[0, 0] == null ? 0 : 1;
    static int BufferBlockCopy()
    {
        int[] xs = { 0x00010203, 0x04050607 };
        int[] ys = { 0x08090a0b, 0x0c0d0e0f };
        Buffer.BlockCopy(xs, 3, ys, 1, 2);
        if (ys[0] != 0x0807000b) return 1;
        if (ys[1] != 0x0c0d0e0f) return 2;
        return 0;
    }
    static int BufferBlockCopyNull()
    {
        try
        {
            Buffer.BlockCopy(null!, 0, Array.Empty<int>(), 0, 0);
            return 1;
        }
        catch (ArgumentNullException) { }
        try
        {
            Buffer.BlockCopy(Array.Empty<int>(), 0, null!, 0, 0);
            return 2;
        }
        catch (ArgumentNullException) { }
        return 0;
    }
    static int BufferBlockCopyInvalid()
    {
        string[] xs = { "foo", "bar" };
        int[] ys = { 0, 1 };
        try
        {
            Buffer.BlockCopy(xs, 0, ys, 0, 0);
            return 1;
        }
        catch (ArgumentException) { }
        try
        {
            Buffer.BlockCopy(ys, 0, xs, 0, 0);
            return 2;
        }
        catch (ArgumentException) { }
        try
        {
            Buffer.BlockCopy(ys, 4, ys, 0, 5);
            return 3;
        }
        catch (ArgumentException) { }
        try
        {
            Buffer.BlockCopy(ys, 0, ys, 4, 5);
            return 4;
        }
        catch (ArgumentException) { }
        return 0;
    }
    static int BufferBlockCopyOutOfRange()
    {
        try
        {
            Buffer.BlockCopy(Array.Empty<int>(), -1, Array.Empty<int>(), 0, 0);
            return 1;
        }
        catch (ArgumentException) { }
        try
        {
            Buffer.BlockCopy(Array.Empty<int>(), 0, Array.Empty<int>(), -1, 0);
            return 2;
        }
        catch (ArgumentException) { }
        try
        {
            Buffer.BlockCopy(Array.Empty<int>(), 0, Array.Empty<int>(), 0, -1);
            return 3;
        }
        catch (ArgumentException) { }
        return 0;
    }

    static int Run(string[] arguments) => arguments[0] switch
    {
        nameof(IsReadOnly) => IsReadOnly(),
        nameof(Clear) => Clear(),
        nameof(ClearAll) => ClearAll(),
        nameof(Copy) => Copy(),
        nameof(ResizeLarger) => ResizeLarger(),
        nameof(ResizeSmaller) => ResizeSmaller(),
        nameof(IListIsReadOnly) => IListIsReadOnly(),
        nameof(IListTIsReadOnly) => IListTIsReadOnly(),
        nameof(IListTCount) => IListTCount(),
        nameof(IListTGetItem) => IListTGetItem(),
        nameof(IListTSetItem) => IListTSetItem(),
        nameof(IListTCopyTo) => IListTCopyTo(),
        nameof(GetEnumerator) => GetEnumerator(),
        nameof(IListTIndexOf) => IListTIndexOf(),
        nameof(Clone) => Clone(),
        nameof(Reverse) => Reverse(),
        nameof(ReverseT) => ReverseT(),
        nameof(New1) => New1(),
        nameof(New2) => New2(),
        nameof(BufferBlockCopy) => BufferBlockCopy(),
        nameof(BufferBlockCopyNull) => BufferBlockCopyNull(),
        nameof(BufferBlockCopyInvalid) => BufferBlockCopyInvalid(),
        nameof(BufferBlockCopyOutOfRange) => BufferBlockCopyOutOfRange(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run);
    [Test]
    public void Test(
        [Values(
            nameof(IsReadOnly),
            nameof(Clear),
            nameof(ClearAll),
            nameof(Copy),
            nameof(ResizeLarger),
            nameof(ResizeSmaller),
            nameof(IListIsReadOnly),
            nameof(IListTIsReadOnly),
            nameof(IListTCount),
            nameof(IListTGetItem),
            nameof(IListTSetItem),
            nameof(IListTCopyTo),
            nameof(GetEnumerator),
            nameof(IListTIndexOf),
            nameof(Clone),
            nameof(Reverse),
            nameof(ReverseT),
            nameof(New1),
            nameof(New2),
            nameof(BufferBlockCopy),
            nameof(BufferBlockCopyNull),
            nameof(BufferBlockCopyInvalid),
            nameof(BufferBlockCopyOutOfRange)
        )] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
}
