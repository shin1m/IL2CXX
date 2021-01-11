using System.Numerics;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class NumericsTests
    {
        static int VectorAdd()
        {
            var x = new Vector<float>(1f) + new Vector<float>(2f);
            return x[0] == 3f ? 0 : 1;
        }
        static int VectorSubtract()
        {
            var x = new Vector<float>(1f) - new Vector<float>(2f);
            return x[0] == -1f ? 0 : 1;
        }
        static int VectorMultiply()
        {
            var x = new Vector<float>(2f) * new Vector<float>(3f);
            return x[0] == 6f ? 0 : 1;
        }
        static int VectorDivide()
        {
            var x = new Vector<float>(3f) / new Vector<float>(2f);
            return x[0] == 1.5f ? 0 : 1;
        }
        static int VectorEquals()
        {
            var x = Vector.Equals<float>(new Vector<float>(1f), new Vector<float>(1f));
            return float.IsNaN(x[0]) ? 0 : 1;
        }
        static int VectorComplement()
        {
            var x = ~new Vector<uint>(0);
            return x[0] == uint.MaxValue ? 0 : 1;
        }
        static int VectorAbs()
        {
            var x = Vector.Abs(new Vector<float>(-1f));
            return x[0] == 1f ? 0 : 1;
        }

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(VectorAdd) => VectorAdd(),
            nameof(VectorSubtract) => VectorSubtract(),
            nameof(VectorMultiply) => VectorMultiply(),
            nameof(VectorDivide) => VectorDivide(),
            nameof(VectorEquals) => VectorEquals(),
            nameof(VectorComplement) => VectorComplement(),
            nameof(VectorAbs) => VectorAbs(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run);
        [TestCase(nameof(VectorAdd))]
        [TestCase(nameof(VectorSubtract))]
        [TestCase(nameof(VectorMultiply))]
        [TestCase(nameof(VectorDivide))]
        [TestCase(nameof(VectorEquals))]
        [TestCase(nameof(VectorComplement))]
        [TestCase(nameof(VectorAbs))]
        public void Test(string name) => Utilities.Run(build, name);
    }
}
