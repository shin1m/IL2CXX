using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    class DelegateTests
    {
        static event Action<string> Log;
        static int Event()
        {
            var logs = new List<string>();
            Log += x =>
            {
                Console.WriteLine($"Hello, {x}!");
                logs.Add($"Hello, {x}!");
            };
            Log += x =>
            {
                Console.WriteLine($"Good bye, {x}!");
                logs.Add($"Good bye, {x}!");
            };
            Log?.Invoke("World");
            if (logs.Count != 2) return 1;
            if (logs[0] != "Hello, World!") return 2;
            if (logs[1] != "Good bye, World!") return 3;
            return 0;
        }
        [Test]
        public void TestEvent() => Utilities.Test(Event);
        static string Greet(string x) => $"Hello, {x}!";
        static int Static()
        {
            Func<string, string> greet = Greet;
            var x = greet("World");
            Console.WriteLine(x);
            return x == "Hello, World!" ? 0 : 1;
        }
        [Test]
        public void TestStatic() => Utilities.Test(Static);
    }
}
