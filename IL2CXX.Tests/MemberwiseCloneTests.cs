using System;
using System.Linq;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    class MemberwiseCloneTests
    {
        struct Foo : ICloneable
        {
            public string X;
            public int Y;

            public object Clone() => MemberwiseClone();
        }
        class Bar : ICloneable
        {
            public string X;
            public int Y;
            public Foo Z;

            public object Clone() => MemberwiseClone();
        }
        static int CloneValue()
        {
            var x = new Foo { X = "foo", Y = 1 };
            var y = (Foo)x.Clone();
            return y.X == "foo" && y.Y == 1 ? 0 : 1;
        }
        [Test]
        public void TestCloneValue() => Utilities.Test(CloneValue);
        static int CloneObject()
        {
            var x = new Bar { X = "foo", Y = 1, Z = { X = "bar", Y = 2 } };
            var y = (Bar)x.Clone();
            return y.X == "foo" && y.Y == 1 && y.Z.X == "bar" && y.Z.Y == 2 ? 0 : 1;
        }
        [Test]
        public void TestCloneObject() => Utilities.Test(CloneObject);
    }
}
