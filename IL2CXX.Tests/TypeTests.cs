using System.Collections.ObjectModel;
using System.Reflection;

namespace IL2CXX.Tests;

public class TypeTestsExported { }
[Parallelizable]
class TypeTests
{
    interface IFoo
    {
        void Do();
    }
    abstract class Bar
    {
        public abstract void Be();
    }
    class Foo : Bar, IFoo
    {
        public void Do()
        {
        }
        public override void Be()
        {
        }
    }
    class Bar<T> where T : Bar, IFoo
    {
        public T? Foo;
        public void Do()
        {
            Foo?.Do();
            Foo?.Be();
        }
    }

    static int Generic()
    {
        new Bar<Foo> { Foo = new Foo() }.Do();
        return 0;
    }
    static int IsGenericTypeDefinition() => typeof(Bar<>).IsGenericTypeDefinition ? 0 : 1;
    static int IsNotConstructedGenericType() => typeof(Bar<>).IsConstructedGenericType ? 1 : 0;
    static int IsConstructedGenericType() => typeof(Bar<Foo>).IsConstructedGenericType ? 0 : 1;
    static int GetGenericTypeDefinition() => typeof(Bar<Foo>).GetGenericTypeDefinition() == typeof(Bar<>) ? 0 : 1;
    static int GetGenericArguments()
    {
        var xs = typeof(Bar<Foo>).GetGenericArguments();
        if (xs.Length != 1) return 1;
        return xs[0] == typeof(Foo) ? 0 : 2;
    }
    static int IsGenericTypeParameter()
    {
        var xs = typeof(Bar<>).GetGenericArguments();
        if (xs.Length != 1) return 1;
        return xs[0].IsGenericTypeParameter ? 0 : 2;
    }
    static int MakeGenericType() => typeof(Bar<>).MakeGenericType(typeof(Foo)) == typeof(Bar<Foo>) ? 0 : 1;
    static int Covariant()
    {
        IEnumerable<object> xs = new[] { new Foo() };
        foreach (var x in xs) Console.WriteLine(x);
        return 0;
    }

    enum Answer { Yes, No }
    class FooAttribute : Attribute
    {
        public readonly Answer C0;
        public readonly string[] C1;
        public readonly Type C2;
        public FooAttribute(Answer c0, string[] c1, Type c2)
        {
            C0 = c0;
            C1 = c1;
            C2 = c2;
        }
        public Answer[]? N0 { get; set; }
        public string? N1 { get; set; }
        public Type[]? N2 { get; set; }
    }
    [Foo(Answer.Yes, ["foo"], typeof(IFoo), N0 = [Answer.No], N1 = "bar", N2 = [typeof(Bar)])]
    class Zot
    {
        public static Zot Be(string x, string y) => new Zot($"{x}, {y}!");

        public string? X;
        public int Y;
        public string? Z { get; set; }
        public int W { get; set; }
        public Zot() { }
        public Zot(string x) => X = x;
        public string Do(string x) => $"{X}, {x}!";
        public int Do(int x) => Y + x;
        public string Do<T, U>(T x, U y) => $"{X}, {x} {y}!";
    }
    static int GetField()
    {
        var zot = new Zot { X = "foo", Y = 1 };
        if (!(typeof(Zot).GetField(nameof(Zot.X))!.GetValue(zot) is string x && x == "foo")) return 1;
        return typeof(Zot).GetField(nameof(Zot.Y))!.GetValue(zot) is int y && y == 1 ? 0 : 2;
    }
    static int SetField()
    {
        var zot = new Zot();
        typeof(Zot).GetField(nameof(Zot.X))!.SetValue(zot, "foo");
        if (zot.X != "foo") return 1;
        typeof(Zot).GetField(nameof(Zot.Y))!.SetValue(zot, 1);
        return zot.Y == 1 ? 0 : 2;
    }
    static int GetFields()
    {
        if (typeof(Zot).GetFields().Length != 2) return 1;
        if (typeof(Zot).GetFields(BindingFlags.Instance | BindingFlags.Public).Length != 2) return 2;
        if (typeof(Zot).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Length != 2) return 3;
        if (typeof(Zot).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length != 4) return 4;
        if (typeof(Zot).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Length != 0) return 5;
        return 0;
    }
    static int GetConstructor()
    {
        var c = typeof(Zot).GetConstructor([typeof(string)]);
        if (c == null) return 1;
        return c.Invoke(["foo"]) is Zot zot && zot.X == "foo" ? 0 : 2;
    }
    static int GetConstructorOfString()
    {
        var c = typeof(string).GetConstructor([typeof(char[])]);
        if (c == null) return 1;
        return c.Invoke(["foo".ToCharArray()]) is string x && x == "foo" ? 0 : 2;
    }
    static int GetConstructorOfArray()
    {
        var c = typeof(string[]).GetConstructor([typeof(int)]);
        if (c == null) return 1;
        return c.Invoke([10]) is string[] xs && xs.Length == 10 ? 0 : 2;
    }
    static int GetConstructorOfArrayOfArrays()
    {
        var c = typeof(string[][]).GetConstructor([typeof(int), typeof(int)]);
        if (c == null) return 1;
        if (!(c.Invoke([2, 3]) is string[][] xs)) return 2;
        if (xs.Length != 2) return 3;
        return xs[0].Length == 3 && xs[1].Length == 3 ? 0 : 4;
    }
    static int GetConstructors()
    {
        if (typeof(Zot).GetConstructors().Length != 2) return 1;
        if (typeof(Zot).GetConstructors(BindingFlags.Instance | BindingFlags.Public).Length != 2) return 2;
        if (typeof(Zot).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Length != 0) return 3;
        if (typeof(Zot).GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Length != 0) return 4;
        return 0;
    }
    static int GetMethod()
    {
        var zot = new Zot { X = "Hello", Y = 1 };
        if (!(typeof(Zot).GetMethod(nameof(Zot.Do), [typeof(string)])!.Invoke(zot, ["World"]) is string x && x == "Hello, World!")) return 1;
        return typeof(Zot).GetMethod(nameof(Zot.Do), [typeof(int)])!.Invoke(zot, [2]) is int y && y == 3 ? 0 : 2;
    }
    static int GetMethods()
    {
        if (typeof(Zot).GetMethods().Length != 8) return 1;
        if (typeof(Zot).GetMethods(BindingFlags.Instance | BindingFlags.Public).Length != 7) return 2;
        if (typeof(Zot).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Length != 0) return 3;
        if (typeof(Zot).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Length != 1) return 4;
        return 0;
    }
    static int CreateDelegate()
    {
        var f = typeof(Zot).GetMethod(nameof(Zot.Be))!.CreateDelegate<Func<string, string, Zot>>();
        return f("Hello", "World").X == "Hello, World!" ? 0 : 1;
    }
    static int CreateDelegateWithNull()
    {
        var f = typeof(Zot).GetMethod(nameof(Zot.Be))!.CreateDelegate<Func<string, string, Zot>>(null);
        return f("Hello", "World").X == "Hello, World!" ? 0 : 1;
    }
    static int CreateDelegateWithTarget()
    {
        var f = typeof(Zot).GetMethod(nameof(Zot.Do), [typeof(string)])!.CreateDelegate<Func<string, string>>(new Zot("Hello"));
        return f("World") == "Hello, World!" ? 0 : 1;
    }
    static int CreateDelegateAsStatic()
    {
        var f = typeof(Zot).GetMethod(nameof(Zot.Do), [typeof(string)])!.CreateDelegate<Func<Zot, string, string>>();
        return f(new Zot("Hello"), "World") == "Hello, World!" ? 0 : 1;
    }
    static int DynamicInvoke()
    {
        Delegate f = (Func<string, string>)(x => $"Hello, {x}!");
        return f.DynamicInvoke("World") is string x && x == "Hello, World!" ? 0 : 1;
    }
    static int MakeGenericMethod()
    {
        var f = typeof(Zot).GetMethod(nameof(Zot.Do), 2, [Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(1)])!.MakeGenericMethod(typeof(string), typeof(int)).CreateDelegate<Func<string, int, string>>(new Zot("Hello"));
        return f("World", 1) == "Hello, World 1!" ? 0 : 1;
    }
    static int GetProperty()
    {
        var zot = new Zot { Z = "foo", W = 1 };
        if (!(typeof(Zot).GetProperty(nameof(Zot.Z))!.GetValue(zot) is string x && x == "foo")) return 1;
        return typeof(Zot).GetProperty(nameof(Zot.W))!.GetValue(zot) is int y && y == 1 ? 0 : 2;
    }
    static int SetProperty()
    {
        var zot = new Zot();
        typeof(Zot).GetProperty(nameof(Zot.Z))!.SetValue(zot, "foo");
        if (zot.Z != "foo") return 1;
        typeof(Zot).GetProperty(nameof(Zot.W))!.SetValue(zot, 1);
        return zot.W == 1 ? 0 : 2;
    }
    static int GetProperties()
    {
        if (typeof(Zot).GetProperties().Length != 2) return 1;
        if (typeof(Zot).GetProperties(BindingFlags.Instance | BindingFlags.Public).Length != 2) return 2;
        if (typeof(Zot).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic).Length != 0) return 3;
        if (typeof(Zot).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Length != 0) return 4;
        return 0;
    }
    static int GetCustomAttributesData()
    {
        var cas = typeof(Zot).GetCustomAttributesData();
        if (cas.Count != 3) return 3;
        var ca = cas[2];
        if (ca.AttributeType != typeof(FooAttribute)) return 2;
        if (ca.ConstructorArguments.Count != 3) return 3;
        {
            var a = ca.ConstructorArguments[0];
            if (a.ArgumentType != typeof(Answer)) return 4;
            if (!(a.Value is int x && x == (int)Answer.Yes)) return 5;
        }
        {
            var a = ca.ConstructorArguments[1];
            if (a.ArgumentType != typeof(string[])) return 6;
            if (!(a.Value is ReadOnlyCollection<CustomAttributeTypedArgument> xs && xs.Count == 1 && (string?)xs[0].Value == "foo")) return 7;
        }
        {
            var a = ca.ConstructorArguments[2];
            if (a.ArgumentType != typeof(Type)) return 8;
            if (!(a.Value is Type x && x == typeof(IFoo))) return 9;
        }
        if (ca.NamedArguments.Count != 3) return 10;
        {
            var a = ca.NamedArguments[0];
            if (a.MemberName != nameof(FooAttribute.N0)) return 11;
            if (a.TypedValue.ArgumentType != typeof(Answer[])) return 12;
            if (!(a.TypedValue.Value is ReadOnlyCollection<CustomAttributeTypedArgument> xs && xs.Count == 1 && (int?)xs[0].Value == (int)Answer.No)) return 13;
        }
        {
            var a = ca.NamedArguments[1];
            if (a.MemberName != nameof(FooAttribute.N1)) return 14;
            if (a.TypedValue.ArgumentType != typeof(string)) return 15;
            if (!(a.TypedValue.Value is string x && x == "bar")) return 16;
        }
        {
            var a = ca.NamedArguments[2];
            if (a.MemberName != nameof(FooAttribute.N2)) return 17;
            if (a.TypedValue.ArgumentType != typeof(Type[])) return 18;
            if (!(a.TypedValue.Value is ReadOnlyCollection<CustomAttributeTypedArgument> xs && xs.Count == 1 && (Type?)xs[0].Value == typeof(Bar))) return 19;
        }
        return 0;
    }
    static int GetCustomAttributes()
    {
        var cas = typeof(Zot).GetCustomAttributes().ToList();
        if (cas.Count != 3) return 3;
        if (!(cas[2] is FooAttribute foo)) return 2;
        if (foo.C0 != Answer.Yes) return 3;
        if (!(foo.C1.Length == 1 && foo.C1[0] == "foo")) return 4;
        if (foo.C2 != typeof(IFoo)) return 5;
        if (!(foo.N0?.Length == 1 && foo.N0[0] == Answer.No)) return 6;
        if (foo.N1 != "bar") return 7;
        if (!(foo.N2?.Length == 1 && foo.N2[0] == typeof(Bar))) return 8;
        return 0;
    }
    static int GetCustomAttributesOfT() => typeof(Zot).GetCustomAttributes<FooAttribute>().Count() == 1 ? 0 : 1;
    static int IsDefined() => typeof(Zot).IsDefined(typeof(FooAttribute)) ? 0 : 1;
    static int ExportedTypes()
    {
        var exported = typeof(Zot).Assembly.ExportedTypes;
        if (exported.Contains(typeof(Zot))) return 1;
        return exported.Contains(typeof(TypeTestsExported)) ? 0 : 2;
    }
    static int AssemblyGetName() => typeof(Zot).Assembly.GetName().Name == "IL2CXX.Tests" ? 0 : 1;
    static int AssemblyGetType() => typeof(Zot).Assembly.GetType(typeof(Zot).FullName ?? throw new Exception()) == typeof(Zot) ? 0 : 1;

    static int Run(string[] arguments) => arguments[1] switch
    {
        nameof(Generic) => Generic(),
        nameof(IsGenericTypeDefinition) => IsGenericTypeDefinition(),
        nameof(IsNotConstructedGenericType) => IsNotConstructedGenericType(),
        nameof(IsConstructedGenericType) => IsConstructedGenericType(),
        nameof(GetGenericTypeDefinition) => GetGenericTypeDefinition(),
        nameof(GetGenericArguments) => GetGenericArguments(),
        nameof(IsGenericTypeParameter) => IsGenericTypeParameter(),
        nameof(MakeGenericType) => MakeGenericType(),
        nameof(Covariant) => Covariant(),
        nameof(GetField) => GetField(),
        nameof(SetField) => SetField(),
        nameof(GetFields) => GetFields(),
        nameof(GetConstructor) => GetConstructor(),
        nameof(GetConstructorOfString) => GetConstructorOfString(),
        nameof(GetConstructorOfArray) => GetConstructorOfArray(),
        nameof(GetConstructorOfArrayOfArrays) => GetConstructorOfArrayOfArrays(),
        nameof(GetConstructors) => GetConstructors(),
        nameof(GetMethod) => GetMethod(),
        nameof(GetMethods) => GetMethods(),
        nameof(CreateDelegate) => CreateDelegate(),
        nameof(CreateDelegateWithNull) => CreateDelegateWithNull(),
        nameof(CreateDelegateWithTarget) => CreateDelegateWithTarget(),
        nameof(CreateDelegateAsStatic) => CreateDelegateAsStatic(),
        nameof(DynamicInvoke) => DynamicInvoke(),
        nameof(MakeGenericMethod) => MakeGenericMethod(),
        nameof(GetProperty) => GetProperty(),
        nameof(SetProperty) => SetProperty(),
        nameof(GetProperties) => GetProperties(),
        nameof(GetCustomAttributesData) => GetCustomAttributesData(),
        nameof(GetCustomAttributes) => GetCustomAttributes(),
        nameof(GetCustomAttributesOfT) => GetCustomAttributesOfT(),
        nameof(IsDefined) => IsDefined(),
        nameof(ExportedTypes) => ExportedTypes(),
        nameof(AssemblyGetName) => AssemblyGetName(),
        nameof(AssemblyGetType) => AssemblyGetType(),
        _ => -1
    };

    string build;

    [OneTimeSetUp]
    public void OneTimeSetUp() => build = Utilities.Build(Run, null, [
        typeof(string),
        typeof(string[]),
        typeof(string[][]),
        typeof(Zot),
        typeof(Func<string, string>)
    ], [
        typeof(Zot).GetMethod(nameof(Zot.Do), 2, [Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(1)])!.MakeGenericMethod(typeof(string), typeof(int))
    ]);
    [Test]
    public void Test(
        [Values(
            nameof(Generic),
            nameof(IsGenericTypeDefinition),
            nameof(IsNotConstructedGenericType),
            nameof(IsConstructedGenericType),
            nameof(GetGenericTypeDefinition),
            nameof(GetGenericArguments),
            nameof(IsGenericTypeParameter),
            nameof(MakeGenericType),
            nameof(Covariant),
            nameof(GetField),
            nameof(SetField),
            nameof(GetFields),
            nameof(GetConstructor),
            nameof(GetConstructorOfString),
            nameof(GetConstructorOfArray),
            nameof(GetConstructorOfArrayOfArrays),
            nameof(GetConstructors),
            nameof(GetMethod),
            nameof(GetMethods),
            nameof(CreateDelegate),
            nameof(CreateDelegateWithNull),
            nameof(CreateDelegateWithTarget),
            nameof(CreateDelegateAsStatic),
            nameof(DynamicInvoke),
            nameof(MakeGenericMethod),
            nameof(GetProperty),
            nameof(SetProperty),
            nameof(GetProperties),
            nameof(GetCustomAttributesData),
            nameof(GetCustomAttributes),
            nameof(GetCustomAttributesOfT),
            nameof(IsDefined),
            nameof(ExportedTypes),
            nameof(AssemblyGetName),
            nameof(AssemblyGetType)
        )] string name,
        [Values] bool cooperative
    ) => Utilities.Run(build, cooperative, name);
}
