using System.Reflection;

namespace IL2CXX.Console
{
    using System;

    class Program
    {
        static int Test()
        {
            string f(int x) => $"Hello {x}!";
            //Console.WriteLine(f("World"));
            Console.WriteLine(f(0));
            return 0;
        }
        static void Main(string[] args)
        {
            new Transpiler(Console.Error.WriteLine).Do(typeof(Program).GetMethod(nameof(Test), BindingFlags.Static | BindingFlags.NonPublic), Console.Out);
        }
    }
}
