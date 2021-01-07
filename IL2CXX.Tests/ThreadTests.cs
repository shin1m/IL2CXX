using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class ThreadTests
    {
        static int Default()
        {
            var s = "|";
            var ts = Enumerable.Range(0, 10).Select(x => new Thread(() =>
            {
                for (var i = 0; i < 10; ++i)
                {
                    s += $"{x}|";
                    Console.WriteLine($"Thread {x}: {s}");
                }
            })).ToList();
            foreach (var x in ts) x.Start();
            foreach (var x in ts) x.Join();
            Console.WriteLine(s);
            return 0;
        }
        static int Background()
        {
            new Thread(() => Thread.Sleep(Timeout.Infinite))
            {
                IsBackground = true
            }.Start();
            return 0;
        }
        static int SpinLockEnter()
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
        static int ParallelFor()
        {
            var n = 0;
            if (!Parallel.For(0, 100, i => Interlocked.Add(ref n, i + 1)).IsCompleted) return 1;
            return n == 5050 ? 0 : 2;
        }

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(Default) => Default(),
            nameof(Background) => Background(),
            nameof(SpinLockEnter) => SpinLockEnter(),
            nameof(ParallelFor) => ParallelFor(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run);
        [TestCase(nameof(Default))]
        [TestCase(nameof(SpinLockEnter))]
        public void Test(string name) => Utilities.Run(build, name);
        [TestCase(nameof(Background))]
        [TestCase(nameof(ParallelFor))]
        public void TestNoVerify(string name) => Utilities.Run(build, name, false);
    }
}
