using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    class ArrayTests
    {
        static int IsReadOnly()
        {
            string[] xs = { "Hello, World!" };
            return xs.IsReadOnly ? 1 : 0;
        }
        [Test]
        public void TestIsReadOnly() => Utilities.Test(IsReadOnly);
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
            foreach (var x in ys) Console.WriteLine(x);
            return ys[1] == "World" ? 0 : 1;
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
    }
}
