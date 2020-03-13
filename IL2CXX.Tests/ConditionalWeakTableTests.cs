using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    using static Utilities;

    class ConditionalWeakTableTests
    {
        static int Default()
        {
            var table = new ConditionalWeakTable<string, string>();
            string x = null;
            string y = null;
            WithPadding(() =>
            {
                x = "Hello";
                y = "World";
                table.Add(x, y);
            });
            if (WithPadding(() => !table.TryGetValue(x, out var z) || z != y)) return 1;
            var (wx, wy) = WithPadding(() => (
                new WeakReference<string>(x), new WeakReference<string>(y)
            ));
            WithPadding(() => x = y = null);
            GC.Collect();
            return wx.TryGetTarget(out _) || wy.TryGetTarget(out _) ? 2 : 0;
        }
        [Test]
        public void TestDefault() => Utilities.Test(Default);
        static int AddOrUpdate()
        {
            var table = new ConditionalWeakTable<string, string>();
            var x = "Hello";
            table.Add(x, "World");
            var y = "Again";
            table.AddOrUpdate(x, y);
            return table.TryGetValue(x, out var z) && z == y ? 0 : 1;
        }
        [Test]
        public void TestAddOrUpdate() => Utilities.Test(AddOrUpdate);
        static int Clear()
        {
            var table = new ConditionalWeakTable<string, string>();
            var x = "Hello";
            table.Add(x, "World");
            table.Clear();
            return table.TryGetValue(x, out _) ? 1 : 0;
        }
        [Test]
        public void TestClear() => Utilities.Test(Clear);
        static int GetOrCreateValue()
        {
            var table = new ConditionalWeakTable<string, string>();
            var x = "Hello";
            var y = "World";
            table.Add(x, y);
            if (table.GetOrCreateValue(x) != y) return 1;
            return table.GetOrCreateValue("Good") == null ? 0 : 2;
        }
        [Test, Ignore("Requires Activator")]
        public void TestGetOrCreateValue() => Utilities.Test(GetOrCreateValue);
        static int GetValue()
        {
            var table = new ConditionalWeakTable<string, string>();
            var x = "Hello";
            var y = "World";
            if (table.GetValue(x, k => y) != y) return 1;
            return table.TryGetValue(x, out var z) && z == y ? 0 : 2;
        }
        [Test]
        public void TestGetValue() => Utilities.Test(GetValue);
        static int Remove()
        {
            var table = new ConditionalWeakTable<string, string>();
            var x = "Hello";
            table.Add(x, "World");
            if (!table.Remove(x)) return 1;
            if (table.Remove(x)) return 2;
            return table.TryGetValue(x, out _) ? 3 : 0;
        }
        [Test]
        public void TestRemove() => Utilities.Test(Remove);
    }
}
