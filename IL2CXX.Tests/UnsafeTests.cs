using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace IL2CXX.Tests
{
    [Parallelizable]
    unsafe class UnsafeTests
    {
        static int Add()
        {
            var x = (0, 1);
            return Unsafe.Add(ref x.Item1, 1) == 1 ? 0 : 1;
        }
        static int AddByteOffset()
        {
            var x = (0, 1);
            return Unsafe.AddByteOffset(ref x.Item1, sizeof(int)) == 1 ? 0 : 1;
        }
        static int CopyVoidT()
        {
            var x = 1;
            var y = 0;
            Unsafe.Copy(&y, ref x);
            return y == 1 ? 0 : 1;
        }
        static int CopyTVoid()
        {
            var x = 1;
            var y = 0;
            Unsafe.Copy(ref y, &x);
            return y == 1 ? 0 : 1;
        }
        static int CopyBlock()
        {
            var x = 1;
            var y = 0;
            Unsafe.CopyBlock(ref Unsafe.As<int, byte>(ref y), ref Unsafe.As<int, byte>(ref x), sizeof(int));
            return y == 1 ? 0 : 1;
        }
        static int CopyBlockUnaligned()
        {
            var x = 1;
            var y = 0;
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<int, byte>(ref y), ref Unsafe.As<int, byte>(ref x), sizeof(int));
            return y == 1 ? 0 : 1;
        }
        static int InitBlock()
        {
            var x = 0;
            Unsafe.InitBlock(ref Unsafe.As<int, byte>(ref x), 1, sizeof(int));
            return x == 0x01010101 ? 0 : 1;
        }
        static int InitBlockUnaligned()
        {
            var x = 0;
            Unsafe.InitBlockUnaligned(ref Unsafe.As<int, byte>(ref x), 1, sizeof(int));
            return x == 0x01010101 ? 0 : 1;
        }
        static int IsAddressGreaterThan()
        {
            var x = (0, 0);
            return Unsafe.IsAddressGreaterThan(ref x.Item2, ref x.Item1) ? 0 : 1;
        }
        static int IsAddressLessThan()
        {
            var x = (0, 0);
            return Unsafe.IsAddressLessThan(ref x.Item1, ref x.Item2) ? 0 : 1;
        }
        static int ReadUnaligned()
        {
            var x = 1;
            return Unsafe.ReadUnaligned<int>(ref Unsafe.As<int, byte>(ref x)) == 1 ? 0 : 1;
        }
        static int SubtractByteOffset()
        {
            var x = (1, 0);
            return Unsafe.SubtractByteOffset(ref x.Item2, sizeof(int)) == 1 ? 0 : 1;
        }
        static int Unbox()
        {
            object x = 1;
            return Unsafe.Unbox<int>(x) == 1 ? 0 : 1;
        }
        static int WriteUnaligned()
        {
            var x = 0;
            Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref x), 1);
            return x == 1 ? 0 : 1;
        }

        static int Run(string[] arguments) => arguments[1] switch
        {
            nameof(Add) => Add(),
            nameof(AddByteOffset) => AddByteOffset(),
            nameof(CopyVoidT) => CopyVoidT(),
            nameof(CopyTVoid) => CopyTVoid(),
            nameof(CopyBlock) => CopyBlock(),
            nameof(CopyBlockUnaligned) => CopyBlockUnaligned(),
            nameof(InitBlock) => InitBlock(),
            nameof(InitBlockUnaligned) => InitBlockUnaligned(),
            nameof(IsAddressGreaterThan) => IsAddressGreaterThan(),
            nameof(IsAddressLessThan) => IsAddressLessThan(),
            nameof(ReadUnaligned) => ReadUnaligned(),
            nameof(SubtractByteOffset) => SubtractByteOffset(),
            nameof(Unbox) => Unbox(),
            nameof(WriteUnaligned) => WriteUnaligned(),
            _ => -1
        };

        string build;

        [OneTimeSetUp]
        public void OneTimeSetUp() => build = Utilities.Build(Run);
        [Test]
        public void Test(
            [Values(
                nameof(Add),
                nameof(AddByteOffset),
                nameof(CopyVoidT),
                nameof(CopyTVoid),
                nameof(CopyBlock),
                nameof(CopyBlockUnaligned),
                nameof(InitBlock),
                nameof(InitBlockUnaligned),
                nameof(IsAddressGreaterThan),
                nameof(IsAddressLessThan),
                nameof(ReadUnaligned),
                nameof(SubtractByteOffset),
                nameof(Unbox),
                nameof(WriteUnaligned)
            )] string name,
            [Values] bool cooperative
        ) => Utilities.Run(build, cooperative, name);
    }
}
