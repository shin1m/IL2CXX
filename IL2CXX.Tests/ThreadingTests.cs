using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class ThreadingTests
    {
        static int Foreground()
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
        static int QueueUserWorkItem()
        {
            using (var @event = new EventWaitHandle(false, EventResetMode.AutoReset))
            {
                ThreadPool.QueueUserWorkItem(_ => @event.Set());
                if (!@event.WaitOne()) return 1;
                Thread.Sleep(1000);
                ThreadPool.QueueUserWorkItem(_ => @event.Set());
                return @event.WaitOne() ? 0 : 2;
            }
        }
        static int ParallelFor()
        {
            var n = 0;
            if (!Parallel.For(0, 100, i => Interlocked.Add(ref n, i + 1)).IsCompleted) return 1;
            return n == 5050 ? 0 : 2;
        }
        static int Lock()
        {
            var monitor = new object();
            var log = string.Empty;
            var ts = Enumerable.Range(0, 10).Select((x, i) => new Thread(() =>
            {
                for (var j = 0; j < 5; ++j) lock (monitor) log += $"|{i}";
            })).ToList();
            foreach (var x in ts) x.Start();
            foreach (var x in ts) x.Join();
            Console.WriteLine(log);
            return log.Length == 100 ? 0 : 1;
        }
        class QuitException : Exception { }
        static int WaitAndPulse()
        {
            var monitor = new object();
            Action action = () => { };
            var t = new Thread(() =>
            {
                Console.WriteLine("start");
                try
                {
                    while (true)
                    {
                        lock (monitor)
                        {
                            action = null;
                            Monitor.Pulse(monitor);
                            do Monitor.Wait(monitor); while (action == null);
                        }
                        action();
                    }
                }
                catch (QuitException) { }
                Console.WriteLine("exit");
            });
            lock (monitor)
            {
                t.Start();
                do Monitor.Wait(monitor); while (action != null);
            }
            void send(Action x)
            {
                lock (monitor)
                {
                    action = x;
                    Monitor.Pulse(monitor);
                    do Monitor.Wait(monitor); while (action != null);
                }
            }
            var log = string.Empty;
            send(() => log += "Hello, ");
            send(() => log += "World.");
            lock (monitor)
            {
                action = () => throw new QuitException();
                Monitor.Pulse(monitor);
            }
            t.Join();
            Console.WriteLine(log);
            return log == "Hello, World." ? 0 : 1;
        }
        static int WaitAndPulseAll()
        {
            var monitor = new object();
            var done = 0;
            Action action = null;
            void send(Action x)
            {
                lock (monitor)
                {
                    while (action != null) Monitor.Wait(monitor);
                    action = x;
                    Monitor.PulseAll(monitor);
                }
            }
            var log = string.Empty;
            var ts = Enumerable.Range(0, 10).Select((x, i) => new Thread(() =>
            {
                for (var j = 0; j < 5; ++j) send(() => log += $"|{i}");
                send(() => ++done);
            })).ToList();
            lock (monitor)
            {
                foreach (var x in ts) x.Start();
                while (done < ts.Count)
                {
                    do Monitor.Wait(monitor); while (action == null);
                    action();
                    action = null;
                    Monitor.PulseAll(monitor);
                }
            }
            foreach (var x in ts) x.Join();
            Console.WriteLine(log);
            return log.Length == 100 ? 0 : 1;
        }
        static int WaitTimeout()
        {
            var monitor = new object();
            lock (monitor) return Monitor.Wait(monitor, 1) ? 1 : 0;
        }
        static int TryEnter()
        {
            var monitor = new object();
            var ready = false;
            var @lock = new object();
            var t = new Thread(() =>
            {
                lock (monitor)
                {
                    ready = true;
                    Monitor.Pulse(monitor);
                    lock (@lock) do Monitor.Wait(monitor); while (ready);
                    ready = true;
                    Monitor.Pulse(monitor);
                }
            });
            t.Start();
            try
            {
                lock (monitor) while (!ready) Monitor.Wait(monitor);
                if (Monitor.TryEnter(@lock)) return 1;
                if (Monitor.TryEnter(@lock, 1)) return 2;
                lock (monitor)
                {
                    ready = false;
                    Monitor.Pulse(monitor);
                    while (!ready) Monitor.Wait(monitor);
                }
                if (!Monitor.TryEnter(@lock)) return 3;
                Monitor.Exit(@lock);
                if (!Monitor.TryEnter(@lock, 1)) return 4;
                Monitor.Exit(@lock);
                return 0;
            }
            finally
            {
                t.Join();
            }
        }
        static int IsEntered()
        {
            var monitor = new object();
            lock (monitor) if (!Monitor.IsEntered(monitor)) return 1;
            return Monitor.IsEntered(monitor) ? 2 : 0;
        }

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(Foreground) => Foreground(),
            nameof(Background) => Background(),
            nameof(SpinLockEnter) => SpinLockEnter(),
            nameof(Mutex) => Mutex(),
            nameof(Event) => Event(),
            nameof(AutoResetEvent) => AutoResetEvent(),
            nameof(ManualResetEvent) => ManualResetEvent(),
            nameof(Semaphore) => Semaphore(),
            nameof(WaitAll) => WaitAll(),
            nameof(WaitAny) => WaitAny(),
            nameof(SignalAndWait) => SignalAndWait(),
            nameof(QueueUserWorkItem) => QueueUserWorkItem(),
            nameof(ParallelFor) => ParallelFor(),
            nameof(Lock) => Lock(),
            nameof(WaitAndPulse) => WaitAndPulse(),
            nameof(WaitAndPulseAll) => WaitAndPulseAll(),
            nameof(WaitTimeout) => WaitTimeout(),
            nameof(TryEnter) => TryEnter(),
            //nameof(IsEntered) => IsEntered(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run);
        [TestCase(nameof(Foreground))]
        [TestCase(nameof(SpinLockEnter))]
        [TestCase(nameof(Mutex))]
        [TestCase(nameof(Event))]
        [TestCase(nameof(AutoResetEvent))]
        [TestCase(nameof(ManualResetEvent))]
        [TestCase(nameof(Semaphore))]
        [TestCase(nameof(WaitAll))]
        [TestCase(nameof(WaitAny))]
        [TestCase(nameof(SignalAndWait))]
        [TestCase(nameof(Lock))]
        [TestCase(nameof(WaitAndPulse))]
        [TestCase(nameof(WaitAndPulseAll))]
        [TestCase(nameof(WaitTimeout))]
        [TestCase(nameof(TryEnter))]
        [TestCase(nameof(IsEntered), Ignore="Not implemented")]
        public void Test(string name) => Utilities.Run(build, name);
        [TestCase(nameof(Background))]
        [TestCase(nameof(QueueUserWorkItem))]
        [TestCase(nameof(ParallelFor))]
        public void TestNoVerify(string name) => Utilities.Run(build, name, false);
    }
}
