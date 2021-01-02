using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    //[Parallelizable]
    class ThreadTests
    {
        static int Run()
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
        [Test]
        public void TestRun() => Utilities.Test(Run);
        static int Background()
        {
            new Thread(() => Thread.Sleep(Timeout.Infinite)) {
                IsBackground = true
            }.Start();
            return 0;
        }
        [Test]
        public void TestBackground() => Utilities.Test(Background, false);
    }
}
