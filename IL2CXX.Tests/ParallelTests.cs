using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Category("Heavy")]
    class ParallelTests
    {
        static int For()
        {
            var n = 0;
            if (!Parallel.For(0, 100, i => Interlocked.Add(ref n, i + 1)).IsCompleted) return 1;
            return n == 5050 ? 0 : 2;
        }
        [Test]
        public void TestFor() => Utilities.Test(For, false);
    }
}
