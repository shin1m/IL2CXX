using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class EnumTests
    {
        enum Names
        {
            Foo, Bar, Zot
        }
        static int GetNames() => Enum.GetNames(typeof(Names)).SequenceEqual(new[] { "Foo", "Bar", "Zot" }) ? 0 : 1;
        [Test]
        public void TestGetNames() => Utilities.Test(GetNames);
        static int GetValues() => Enum.GetValues(typeof(Names)).Cast<Names>().SequenceEqual(new[] { Names.Foo, Names.Bar, Names.Zot }) ? 0 : 1;
        [Test]
        public void TestGetValues() => Utilities.Test(GetValues);
        static int ToStringDefault() => Names.Foo.ToString() == "Foo" ? 0 : 1;
        [Test]
        public void TestToStringDefault() => Utilities.Test(ToStringDefault);
        static int ToStringG() => Names.Foo.ToString("g") == "Foo" ? 0 : 1;
        [Test]
        public void TestToStringG() => Utilities.Test(ToStringG);
    }
}
