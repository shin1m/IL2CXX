using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class NumberTests
    {
        static int Single()
        {
            if (!float.IsPositiveInfinity(float.PositiveInfinity)) return 1;
            if (!float.IsNegativeInfinity(float.NegativeInfinity)) return 2;
            if (!float.IsNaN(float.NaN)) return 3;
            return 0;
        }
        [Test]
        public void TestSingle() => Utilities.Test(Single);
        static int Double()
        {
            if (!double.IsPositiveInfinity(double.PositiveInfinity)) return 1;
            if (!double.IsNegativeInfinity(double.NegativeInfinity)) return 2;
            if (!double.IsNaN(double.NaN)) return 3;
            return 0;
        }
        [Test]
        public void TestDouble() => Utilities.Test(Double);
        static int Unordered()
        {
            int ne(float x, float y) => x != y ? 1 : 0;
            if (ne(float.NaN, float.NaN) == 0) return 1;
            int lt(float x, float y) => !(x >= y) ? 1 : 0;
            if (lt(float.NaN, float.NaN) == 0) return 2;
            int le(float x, float y) => !(x > y) ? 1 : 0;
            if (le(float.NaN, float.NaN) == 0) return 3;
            int gt(float x, float y) => !(x <= y) ? 1 : 0;
            if (gt(float.NaN, float.NaN) == 0) return 4;
            int ge(float x, float y) => !(x < y) ? 1 : 0;
            if (ge(float.NaN, float.NaN) == 0) return 5;
            return 0;
        }
        [Test]
        public void TestUnordered() => Utilities.Test(Unordered);
    }
}
