using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class SpinLockTests
    {
        static int Enter()
        {
            var spin = new SpinLock();
            var i = 0;
            var ts = Enumerable.Range(0, 10).Select(x => new Thread(() =>
            {
                for (var j = 0; j < 10; ++j)
                {
                    var got = false;
                    spin.Enter(ref got);
                    if (got)
                    {
                        ++i;
                        spin.Exit();
                    }
                }
            })).ToList();
            foreach (var x in ts) x.Start();
            foreach (var x in ts) x.Join();
            return i == 100 ? 0 : 1;
        }
        [Test]
        public void TestEnter() => Utilities.Test(Enter);
    }
}
