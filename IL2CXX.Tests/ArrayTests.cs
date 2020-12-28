using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    //[Parallelizable]
    class ArrayTests
    {
        static int AssertEquals(string[] xs, string[] ys)
        {
            foreach (var x in xs) Console.WriteLine(x ?? "(null)");
            var n = xs.Length;
            if (n != ys.Length) return 1;
            for (var i = 0; i < n; ++i) if (xs[i] != ys[i]) return 2;
            return 0;
        }
        static int IsReadOnly()
        {
            string[] xs = { "Hello, World!" };
            return xs.IsReadOnly ? 1 : 0;
        }
        [Test]
        public void TestIsReadOnly() => Utilities.Test(IsReadOnly);
        static int Copy()
        {
            string[] xs = { "Hello", "World", "Good", "Bye" };
            var ys = new string[6];
            Array.Copy(xs, 1, ys, 2, 3);
            return AssertEquals(ys, new[] { null, null, "World", "Good", "Bye", null });
        }
        [Test]
        public void TestCopy() => Utilities.Test(Copy);
        static int ResizeLarger()
        {
            string[] xs = { "Hello", "World", "Good", "Bye" };
            Array.Resize(ref xs, 6);
            return AssertEquals(xs, new[] { "Hello", "World", "Good", "Bye", null, null });
        }
        [Test]
        public void TestResizeLarger() => Utilities.Test(ResizeLarger);
        static int ResizeSmaller()
        {
            string[] xs = { "Hello", "World", "Good", "Bye" };
            Array.Resize(ref xs, 3);
            return AssertEquals(xs, new[] { "Hello", "World", "Good" });
        }
        [Test]
        public void TestResizeSmaller() => Utilities.Test(ResizeSmaller);
        static int IListIsReadOnly()
        {
            IList xs = new[] { "Hello, World!" };
            return xs.IsReadOnly ? 1 : 0;
        }
        [Test]
        public void TestIListIsReadOnly() => Utilities.Test(IListIsReadOnly);
        static int IListTIsReadOnly()
        {
            IList<string> xs = new[] { "Hello, World!" };
            return xs.IsReadOnly ? 0 : 1;
        }
        [Test]
        public void TestIListTIsReadOnly() => Utilities.Test(IListTIsReadOnly);
        static int IListTCount()
        {
            IList<string> xs = new[] { "Hello, World!" };
            return xs.Count == 1 ? 0 : 1;
        }
        [Test]
        public void TestIListTCount() => Utilities.Test(IListTCount);
        static int IListTGetItem()
        {
            IList<string> xs = new[] { "foo" };
            return xs[0] == "foo" ? 0 : 1;
        }
        [Test]
        public void TestIListTGetItem() => Utilities.Test(IListTGetItem);
        static int IListTSetItem()
        {
            IList<string> xs = new[] { "foo" };
            xs[0] = "bar";
            return xs[0] == "bar" ? 0 : 1;
        }
        [Test]
        public void TestIListTSetItem() => Utilities.Test(IListTSetItem);
        static int IListTCopyTo()
        {
            IList<string> xs = new[] { "World" };
            string[] ys = { "Hello", null };
            xs.CopyTo(ys, 1);
            return AssertEquals(ys, new[] { "Hello", "World" });
        }
        [Test]
        public void TestIListTCopyTo() => Utilities.Test(IListTCopyTo);
        static int GetEnumerator()
        {
            foreach (var x in (IEnumerable<string>)new[] {
                "Hello, World!",
                "Good bye."
            }) Console.WriteLine(x);
            return 0;
        }
        [Test]
        public void TestGetEnumerator() => Utilities.Test(GetEnumerator);
        static int IListTIndexOf()
        {
            IList<string> xs = new[] { "foo", "bar" };
            return xs.IndexOf("bar") == 1 ? 0 : 1;
        }
        [Test]
        public void TestIListTIndexOf() => Utilities.Test(IListTIndexOf);
        static int Clone()
        {
            string[] xs = { "foo", "bar" };
            return AssertEquals((string[])xs.Clone(), xs);
        }
        [Test]
        public void TestClone() => Utilities.Test(Clone);
    }
}
