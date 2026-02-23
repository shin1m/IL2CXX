using System.Reflection;
using Microsoft.Win32.SafeHandles;

namespace IL2CXX;

partial class DefaultBuiltin
{
    private static Builtin SetupInterop(this Builtin @this, Func<Type, Type> get, PlatformID target) => @this
    .For(get(Type.GetType("Interop+Kernel32", true)!), (type, code) =>
    {
        if (target == PlatformID.Win32NT) return;
        code.For(
            type.GetMethod("GetEnvironmentStringsW", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($@"{'\t'}std::string s;
{'\t'}for (auto p = environ; *p; ++p) {{
{'\t'}{'\t'}s += *p;
{'\t'}{'\t'}s += '\0';
{'\t'}}}
{'\t'}auto s16 = f__u16string(s);
{'\t'}auto n = s16.size() + 1;
{'\t'}auto p = new char16_t[n];
{'\t'}std::copy_n(s16.c_str(), n, p);
{'\t'}return p;
", 0)
        );
        code.For(
            type.GetMethod("FreeEnvironmentStringsW", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($@"{'\t'}delete[] a_0;
{'\t'}return 1;
", 0)
        );
        code.For(
            type.GetMethod("GetEnvironmentVariable", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($@"{'\t'}auto p = std::getenv(f__string(a_0).c_str());
{'\t'}if (!p) return 0;
{'\t'}auto q = f__u16string(p);
{'\t'}auto n = q.size();
{'\t'}if (a_2 < n) return n + 1;
{'\t'}std::copy_n(q.c_str(), n + 1, a_1);
{'\t'}return n;
", 0)
        );
        code.For(
            type.GetMethod("SetEnvironmentVariable", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($@"{'\t'}auto name = f__string(a_0);
{'\t'}return (a_1 ? setenv(name.c_str(), f__string(a_1).c_str(), 1) : unsetenv(name.c_str())) == 0;
", 0)
        );
        code.For(
            type.GetMethod("CloseHandle", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($@"{'\t'}delete static_cast<t__waitable*>(a_0.v__5fvalue);
{'\t'}return true;
", 0)
        );
        var swh = get(typeof(SafeWaitHandle));
        var swhc = swh.GetConstructor([get(typeof(nint)), get(typeof(bool))]) ?? throw new Exception();
        code.For(
            type.GetMethod("CreateEventEx", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($@"{'\t'}auto RECYCLONE__SPILL p = f__new_zeroed<{transpiler.Escape(swh)}>();
{'\t'}{transpiler.Escape(swhc)}(p, new t__event(a_2 & 1, a_2 & 2), true);
{'\t'}return p;
", 0)
        );
        code.For(
            type.GetMethod("CreateSemaphoreEx", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($@"{'\t'}auto RECYCLONE__SPILL p = f__new_zeroed<{transpiler.Escape(swh)}>();
{'\t'}{transpiler.Escape(swhc)}(p, new t__semaphore(a_2, a_1), true);
{'\t'}return p;
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
        code.For(
            type.GetMethod("FormatMessage", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
        );
    });
}
