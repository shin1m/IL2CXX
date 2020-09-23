using System;
using System.Text;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Category("Heavy")]
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
        [Test]
        public void TestGetBytes() => Utilities.Test(GetBytes);
        static int GetString()
        {
            var x = Encoding.UTF8.GetString(new[] { (byte)0xce, (byte)0xa0 });
            Console.WriteLine(x);
            return x == "\u03a0" ? 0 : 1;
        }
        [Test]
        public void TestGetString() => Utilities.Test(GetString);
        static int Convert()
        {
            var utf8 = Encoding.UTF8.GetBytes("\u03a0");
            var ascii = Encoding.Convert(Encoding.UTF8, Encoding.ASCII, utf8);
            if (ascii.Length != 1) return 1;
            if (ascii[0] != '?') return 2;
            return 0;
        }
        [Test]
        public void TestConvert() => Utilities.Test(Convert);
    }
}
