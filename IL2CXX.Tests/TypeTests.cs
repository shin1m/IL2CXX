using System;
using System.Reflection;
using NUnit.Framework;

namespace IL2CXX.Tests
{
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
            public T Foo;
            public void Do()
            {
                Foo.Do();
                Foo.Be();
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
        static int MakeGenericType() => typeof(Bar<>).MakeGenericType(typeof(Foo)) == typeof(Bar<Foo>) ? 0 : 1;

        class Zot
        {
            public string X;
            public int Y;
            public string Z { get; set; }
            public int W { get; set; }
        }
        static int GetField()
        {
            var zot = new Zot { X = "foo", Y = 1 };
            if (!(typeof(Zot).GetField(nameof(Zot.X)).GetValue(zot) is string x && x == "foo")) return 1;
            return typeof(Zot).GetField(nameof(Zot.Y)).GetValue(zot) is int y && y == 1 ? 0 : 2;
        }
        static int SetField()
        {
            var zot = new Zot();
            typeof(Zot).GetField(nameof(Zot.X)).SetValue(zot, "foo");
            if (zot.X != "foo") return 1;
            typeof(Zot).GetField(nameof(Zot.Y)).SetValue(zot, 1);
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
        static int GetProperty()
        {
            var zot = new Zot { Z = "foo", W = 1 };
            if (!(typeof(Zot).GetProperty(nameof(Zot.Z)).GetValue(zot) is string x && x == "foo")) return 1;
            return typeof(Zot).GetProperty(nameof(Zot.W)).GetValue(zot) is int y && y == 1 ? 0 : 2;
        }
        static int SetProperty()
        {
            var zot = new Zot();
            typeof(Zot).GetProperty(nameof(Zot.Z)).SetValue(zot, "foo");
            if (zot.Z != "foo") return 1;
            typeof(Zot).GetProperty(nameof(Zot.W)).SetValue(zot, 1);
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

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(Generic) => Generic(),
            nameof(IsGenericTypeDefinition) => IsGenericTypeDefinition(),
            nameof(IsNotConstructedGenericType) => IsNotConstructedGenericType(),
            nameof(IsConstructedGenericType) => IsConstructedGenericType(),
            nameof(GetGenericTypeDefinition) => GetGenericTypeDefinition(),
            nameof(GetGenericArguments) => GetGenericArguments(),
            nameof(MakeGenericType) => MakeGenericType(),
            nameof(GetField) => GetField(),
            nameof(SetField) => SetField(),
            nameof(GetFields) => GetFields(),
            nameof(GetProperty) => GetProperty(),
            nameof(SetProperty) => SetProperty(),
            nameof(GetProperties) => GetProperties(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run, null, new[] {
            typeof(Bar<>),
            typeof(Zot)
        });
        [TestCase(nameof(Generic))]
        [TestCase(nameof(IsGenericTypeDefinition))]
        [TestCase(nameof(IsNotConstructedGenericType))]
        [TestCase(nameof(IsConstructedGenericType))]
        [TestCase(nameof(GetGenericTypeDefinition))]
        [TestCase(nameof(GetGenericArguments))]
        [TestCase(nameof(MakeGenericType))]
        [TestCase(nameof(GetField))]
        [TestCase(nameof(SetField))]
        [TestCase(nameof(GetFields))]
        [TestCase(nameof(GetProperty))]
        [TestCase(nameof(SetProperty))]
        [TestCase(nameof(GetProperties))]
        public void Test(string name) => Utilities.Run(build, name);
    }
}
