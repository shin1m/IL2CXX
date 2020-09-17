using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    class MonitorTests
    {
        static int Default()
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
        [Test]
        public void TestDefault() => Utilities.Test(Default);
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
        [Test]
        public void TestWaitAndPulse() => Utilities.Test(WaitAndPulse);
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
        [Test]
        public void TestWaitAndPulseAll() => Utilities.Test(WaitAndPulseAll);
        static int WaitTimeout()
        {
            var monitor = new object();
            lock (monitor) return Monitor.Wait(monitor, 1) ? 1 : 0;
        }
        [Test]
        public void TestWaitTimeout() => Utilities.Test(WaitTimeout);
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
        [Test]
        public void TestTryEnter() => Utilities.Test(TryEnter);
        static int IsEntered()
        {
            var monitor = new object();
            lock (monitor) if (!Monitor.IsEntered(monitor)) return 1;
            return Monitor.IsEntered(monitor) ? 2 : 0;
        }
        [Test, Ignore("Not implemented")]
        public void TestIsEntered() => Utilities.Test(IsEntered);
    }
}
