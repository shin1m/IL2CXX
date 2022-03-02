using System;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class BoxTests
    {
        enum Foo { X }
        static object Box<T>(T x) => x;
        static int BoxObject() => Box(string.Empty) is string ? 0 : 1;
        static int BoxValue() => Box(true) is bool ? 0 : 1;
        static int BoxNullable() => Box((int?)0) is int ? 0 : 1;
        static int BoxNull() => Box((int?)null) is null ? 0 : 1;
        static T Unbox<T>(object x) => (T)x;
        static int UnboxObject() => Unbox<string>(string.Empty) != null ? 0 : 1;
        static int UnboxValue() => Unbox<int>(1) == 1 ? 0 : 1;
        static int UnboxEnum() => Unbox<int>(Foo.X) == 0 ? 0 : 1;
        static int UnboxNullable() => Unbox<int?>(1) == 1 ? 0 : 1;
        static int UnboxNull() => Unbox<int?>(null) == null ? 0 : 1;
        static U BoxUnbox<T, U>(T x) => (U)(object)x;
        static int BoxUnboxSame() => BoxUnbox<bool, bool>(true) ? 0 : 1;
        static int BoxEnumUnboxInt() => BoxUnbox<Foo, int>(Foo.X) == 0 ? 0 : 1;
        static int BoxValueUnboxNullable() => BoxUnbox<int, int?>(1) == 1 ? 0 : 1;
        static int BoxNullUnboxNullable() => BoxUnbox<object, int?>(null) == null ? 0 : 1;
        static int BoxObjectUnboxNullable() => BoxUnbox<object, int?>(1) == 1 ? 0 : 1;
        static int BoxValueUnboxAssignable() => BoxUnbox<int, IComparable>(1) != null ? 0 : 1;
        static int BoxObjectUnboxAssignable() => BoxUnbox<string, IComparable>(string.Empty) != null ? 0 : 1;

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(BoxObject) => BoxObject(),
            nameof(BoxValue) => BoxValue(),
            nameof(BoxNullable) => BoxNullable(),
            nameof(BoxNull) => BoxNull(),
            nameof(UnboxObject) => UnboxObject(),
            nameof(UnboxValue) => UnboxValue(),
            nameof(UnboxEnum) => UnboxEnum(),
            nameof(UnboxNullable) => UnboxNullable(),
            nameof(UnboxNull) => UnboxNull(),
            nameof(BoxUnboxSame) => BoxUnboxSame(),
            nameof(BoxEnumUnboxInt) => BoxEnumUnboxInt(),
            nameof(BoxValueUnboxNullable) => BoxValueUnboxNullable(),
            nameof(BoxNullUnboxNullable) => BoxNullUnboxNullable(),
            nameof(BoxObjectUnboxNullable) => BoxObjectUnboxNullable(),
            nameof(BoxValueUnboxAssignable) => BoxValueUnboxAssignable(),
            nameof(BoxObjectUnboxAssignable) => BoxObjectUnboxAssignable(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run);
        [Test]
        public void Test(
            [Values(
                nameof(BoxObject),
                nameof(BoxValue),
                nameof(BoxNullable),
                nameof(BoxNull),
                nameof(UnboxObject),
                nameof(UnboxValue),
                nameof(UnboxEnum),
                nameof(UnboxNullable),
                nameof(UnboxNull),
                nameof(BoxUnboxSame),
                nameof(BoxEnumUnboxInt),
                nameof(BoxValueUnboxNullable),
                nameof(BoxNullUnboxNullable),
                nameof(BoxObjectUnboxNullable),
                nameof(BoxValueUnboxAssignable),
                nameof(BoxObjectUnboxAssignable)
            )] string name,
            [Values] bool cooperative
        ) => Utilities.Run(build, cooperative, name);
    }
}
