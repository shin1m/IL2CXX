using System;
using System.Reflection;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupInterop(this Builtin @this) => @this
        .For(Type.GetType("Interop+Kernel32"), (type, code) =>
        {
            code.For(
                type.GetMethod("GetEnvironmentVariable", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(char*), typeof(int) }, null),
                transpiler => ($@"{'\t'}auto p = std::getenv(f__string({{&a_0->v__5ffirstChar, static_cast<size_t>(a_0->v__5fstringLength)}}).c_str());
{'\t'}if (!p) return 0;
{'\t'}auto q = f__u16string(p);
{'\t'}auto n = q.size();
{'\t'}if (a_2 < n) return n + 1;
{'\t'}std::copy_n(q.c_str(), n + 1, a_1);
{'\t'}return n;
", 0)
            );
            code.For(
                type.GetMethod("LocalAlloc", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}if (a_0 & ~0x40) return {{}};
{'\t'}auto p = std::malloc(a_1);
{'\t'}if (a_0 & 0x40) std::memset(p, 0, a_1);
{'\t'}return p;
", 1)
            );
            code.For(
                type.GetMethod("LocalFree", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}std::free(a_0);
{'\t'}return {{}};
", 1)
            );
        });
    }
}
