using System;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
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

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(Catch) => Catch(),
            nameof(Filter) => Filter(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run);
        [TestCase(nameof(Catch))]
        [TestCase(nameof(Filter))]
        public void Test(string name) => Utilities.Run(build, name);
    }
}
