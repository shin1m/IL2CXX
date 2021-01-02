using System;
using System.Reflection;
using Microsoft.Win32.SafeHandles;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupInterop(this Builtin @this) => @this
        .For(Type.GetType("Interop+Kernel32"), (type, code) =>
        {
            code.For(
                type.GetMethod("CloseHandle", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}delete static_cast<t__waitable*>(a_0.v__5fvalue);
{'\t'}return true;
", 0)
            );
            code.For(
                type.GetMethod("CreateEventEx", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(typeof(SafeWaitHandle))}>();
{'\t'}{transpiler.Escape(typeof(SafeWaitHandle).GetConstructor(new[] { typeof(IntPtr), typeof(bool) }))}(p, new t__event(a_2 & 1, a_2 & 2), true);
{'\t'}return p;
", 0)
            );
            code.For(
                type.GetMethod("CreateMutexEx", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(typeof(SafeWaitHandle))}>();
{'\t'}{transpiler.Escape(typeof(SafeWaitHandle).GetConstructor(new[] { typeof(IntPtr), typeof(bool) }))}(p, new t__mutex(a_2 & 1), true);
{'\t'}return p;
", 0)
            );
            code.For(
                type.GetMethod("CreateSemaphoreEx", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(typeof(SafeWaitHandle))}>();
{'\t'}{transpiler.Escape(typeof(SafeWaitHandle).GetConstructor(new[] { typeof(IntPtr), typeof(bool) }))}(p, new t__semaphore(a_2, a_1), true);
{'\t'}return p;
", 0)
            );
            code.For(
                type.GetMethod("GetEnvironmentVariable", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(char).MakeByRefType(), typeof(uint) }, null),
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
                type.GetMethod("ReleaseMutex", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}static_cast<t__mutex*>(a_0->v_handle.v__5fvalue)->f_release();
{'\t'}return true;
", 0)
            );
            code.For(
                type.GetMethod("ReleaseSemaphore", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}*a_2 = static_cast<t__semaphore*>(a_0->v_handle.v__5fvalue)->f_release(a_1);
{'\t'}return true;
", 0)
            );
            code.For(
                type.GetMethod("ResetEvent", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}static_cast<t__event*>(a_0->v_handle.v__5fvalue)->f_reset();
{'\t'}return true;
", 0)
            );
            code.For(
                type.GetMethod("SetEvent", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}static_cast<t__event*>(a_0->v_handle.v__5fvalue)->f_set();
{'\t'}return true;
", 0)
            );
        });
    }
}
