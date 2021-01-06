using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class WaitHandleTests
    {
        static int Mutex()
        {
            using (var mutex = new Mutex())
            {
                var i = 0;
                var ts = Enumerable.Range(0, 10).Select(x => new Thread(() =>
                {
                    for (var j = 0; j < 10; ++j)
                    {
                        mutex.WaitOne();
                        ++i;
                        mutex.ReleaseMutex();
                    }
                })).ToList();
                foreach (var x in ts) x.Start();
                foreach (var x in ts) x.Join();
                return i == 100 ? 0 : 1;
            }
        }
        static int Event()
        {
            using (var ready = new EventWaitHandle(false, EventResetMode.AutoReset))
            using (var next = new EventWaitHandle(false, EventResetMode.AutoReset))
            {
                var i = 0;
                var t = new Thread(() =>
                {
                    ++i;
                    ready.Set();
                    next.WaitOne();
                    ++i;
                });
                t.Start();
                ready.WaitOne();
                if (i != 1) return 1;
                next.Set();
                t.Join();
                return i == 2 ? 0 : 2;
            }
        }
        static int AutoResetEvent()
        {
            using (var @event = new EventWaitHandle(true, EventResetMode.AutoReset))
            {
                if (!@event.WaitOne(0)) return 1;
                return @event.WaitOne(0) ? 1 : 0;
            }
        }
        static int ManualResetEvent()
        {
            using (var @event = new EventWaitHandle(true, EventResetMode.ManualReset))
            {
                if (!@event.WaitOne(0)) return 1;
                if (!@event.WaitOne(0)) return 2;
                @event.Reset();
                return @event.WaitOne(0) ? 3 : 0;
            }
        }
        static int Semaphore()
        {
            using (var semaphore = new Semaphore(0, 1))
            {
                var i = 0;
                var ts = Enumerable.Range(0, 10).Select(x => new Thread(() =>
                {
                    for (var j = 0; j < 10; ++j)
                    {
                        semaphore.WaitOne();
                        ++i;
                        semaphore.Release();
                    }
                })).ToList();
                foreach (var x in ts) x.Start();
                if (semaphore.Release() != 0) return 1;
                foreach (var x in ts) x.Join();
                return i == 100 ? 0 : 2;
            }
        }
        static int WaitAll()
        {
            using (var mutex = new Mutex(true))
            using (var auto = new EventWaitHandle(false, EventResetMode.AutoReset))
            using (var manual = new EventWaitHandle(false, EventResetMode.ManualReset))
            using (var semaphore = new Semaphore(0, 1))
            {
                new Thread(() =>
                {
                    WaitHandle.WaitAll(new WaitHandle[] { mutex, auto });
                    semaphore.Release();
                    manual.Set();
                }).Start();
                mutex.ReleaseMutex();
                auto.Set();
                WaitHandle.WaitAll(new WaitHandle[] { manual, semaphore });
                return 0;
            }
        }
        static int WaitAny()
        {
            using (var ready = new EventWaitHandle(false, EventResetMode.AutoReset))
            using (var done = new EventWaitHandle(false, EventResetMode.AutoReset))
            using (var other = new EventWaitHandle(false, EventResetMode.AutoReset))
            {
                new Thread(() =>
                {
                    if (WaitHandle.WaitAny(new WaitHandle[] { other, done, ready }) != 2) throw new Exception();
                    done.Set();
                }).Start();
                ready.Set();
                return WaitHandle.WaitAny(new WaitHandle[] { other, done }) == 1 ? 0 : 1;
            }
        }
        static int SignalAndWait()
        {
            using (var ready = new EventWaitHandle(false, EventResetMode.AutoReset))
            using (var done = new EventWaitHandle(false, EventResetMode.AutoReset))
            {
                new Thread(() =>
                {
                    ready.WaitOne();
                    done.Set();
                }).Start();
                return WaitHandle.SignalAndWait(ready, done) ? 0 : 1;
            }
        }

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(Mutex) => Mutex(),
            nameof(Event) => Event(),
            nameof(AutoResetEvent) => AutoResetEvent(),
            nameof(ManualResetEvent) => ManualResetEvent(),
            nameof(Semaphore) => Semaphore(),
            nameof(WaitAll) => WaitAll(),
            nameof(WaitAny) => WaitAny(),
            nameof(SignalAndWait) => SignalAndWait(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run);
        [TestCase(nameof(Mutex))]
        [TestCase(nameof(Event))]
        [TestCase(nameof(AutoResetEvent))]
        [TestCase(nameof(ManualResetEvent))]
        [TestCase(nameof(Semaphore))]
        [TestCase(nameof(WaitAll))]
        [TestCase(nameof(WaitAny))]
        [TestCase(nameof(SignalAndWait))]
        public void Test(string name) => Utilities.Run(build, name);
    }
}
