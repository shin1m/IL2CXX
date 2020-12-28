using System;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    //[Parallelizable]
    class TryTests
    {
        static int Catch()
        {
            try
            {
                throw new Exception("foo");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return e.Message == "foo" ? 0 : 1;
            }
        }
        [Test]
        public void TestCatch() => Utilities.Test(Catch);
        static int Filter()
        {
            try
            {
                throw new Exception("foo");
            }
            catch (Exception e) when (e.Message == "bar")
            {
                return 1;
            }
            catch (Exception e) when (e.Message == "foo")
            {
                Console.WriteLine(e.Message);
                return 0;
            }
            catch (Exception)
            {
                return 2;
            }
        }
        [Test]
        public void TestFilter() => Utilities.Test(Filter);
    }
}
