using System.Reflection;

namespace IL2CXX;

partial class DefaultBuiltin
{
    private static Builtin SetupSystemThreading(this Builtin @this, Func<Type, Type> get, PlatformID target) => @this
    .For(get(typeof(Interlocked)), (type, code) =>
    {
        code.For(
            type.GetMethod(nameof(Interlocked.CompareExchange), [get(typeof(int)).MakeByRefType(), get(typeof(int)), get(typeof(int))]),
            transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}std::atomic_ref(*a_0).compare_exchange_strong(a_2, a_1);
{'\t'}return a_2;
", 1)
        );
        code.For(
            type.GetMethod(nameof(Interlocked.CompareExchange), [get(typeof(long)).MakeByRefType(), get(typeof(long)), get(typeof(long))]),
            transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}std::atomic_ref(*a_0).compare_exchange_strong(a_2, a_1);
{'\t'}return a_2;
", 1)
        );
        code.For(
            type.GetMethod(nameof(Interlocked.CompareExchange), [get(typeof(IntPtr)).MakeByRefType(), get(typeof(IntPtr)), get(typeof(IntPtr))]),
            transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}void* p = a_2;
{'\t'}std::atomic_ref(a_0->v__5fvalue).compare_exchange_strong(p, a_1);
{'\t'}return {transpiler.EscapeForStacked(get(typeof(IntPtr)))}{{p}};
", 1)
        );
        code.For(
            type.GetMethod(nameof(Interlocked.CompareExchange), [get(typeof(object)).MakeByRefType(), get(typeof(object)), get(typeof(object))]),
            transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}auto p = a_2;
{'\t'}f__compare_exchange(*a_0, p, a_1);
{'\t'}return p;
", 1)
        );
        code.For(
            type.GetMethod(nameof(Interlocked.Exchange), [get(typeof(int)).MakeByRefType(), get(typeof(int))]),
            transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\treturn std::atomic_ref(*a_0).exchange(a_1);\n", 1)
        );
        code.For(
            type.GetMethod(nameof(Interlocked.Exchange), [get(typeof(long)).MakeByRefType(), get(typeof(long))]),
            transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\treturn std::atomic_ref(*a_0).exchange(a_1);\n", 1)
        );
        code.For(
            type.GetMethod(nameof(Interlocked.Exchange), [get(typeof(IntPtr)).MakeByRefType(), get(typeof(IntPtr))]),
            transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $"\treturn {transpiler.EscapeForStacked(get(typeof(IntPtr)))}{{std::atomic_ref(a_0->v__5fvalue).exchange(a_1)}};\n", 1)
        );
        code.For(
            type.GetMethod(nameof(Interlocked.Exchange), [get(typeof(object)).MakeByRefType(), get(typeof(object))]),
            transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\treturn f__exchange(*a_0, a_1);\n", 1)
        );
        code.For(
            type.GetMethod("ExchangeAdd", BindingFlags.Static | BindingFlags.NonPublic, null, [get(typeof(int)).MakeByRefType(), get(typeof(int))], null),
            transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\treturn std::atomic_ref(*a_0).fetch_add(a_1);\n", 1)
        );
        code.For(
            type.GetMethod("ExchangeAdd", BindingFlags.Static | BindingFlags.NonPublic, null, [get(typeof(long)).MakeByRefType(), get(typeof(long))], null),
            transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\treturn std::atomic_ref(*a_0).fetch_add(a_1);\n", 1)
        );
        code.For(
            type.GetMethod(nameof(Interlocked.MemoryBarrier)),
            transpiler => ("\tstd::atomic_thread_fence(std::memory_order_seq_cst);\n", 1)
        );
    })
    .For(get(typeof(Monitor)), (type, code) =>
    {
        code.For(
            type.GetMethod("ReliableEnter", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}a_0->f_extension()->f_lock();
{'\t'}}});
{'\t'}*a_1 = true;
", 1)
        );
        code.For(
            type.GetMethod("ReliableEnterTimeout", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}*a_2 = a_0->f_extension()->f_try_lock_for(std::chrono::milliseconds(a_1));
{'\t'}}});
", 1)
        );
        code.For(
            type.GetMethod(nameof(Monitor.Enter), [get(typeof(object))]),
            transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}a_0->f_extension()->f_lock();
{'\t'}}});
", 1)
        );
        code.For(
            type.GetMethod(nameof(Monitor.Exit)),
            transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\ta_0->f_extension()->f_unlock();\n", 1)
        );
        code.For(
            type.GetMethod("IsEnteredNative", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($@"{'\t'}auto p = a_0->f_extension();
{'\t'}return p->f_locked();
", 1)
        );
        code.For(
            type.GetMethod("ObjPulse", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\ta_0->f_extension()->v_condition.notify_one();\n", 1)
        );
        code.For(
            type.GetMethod("ObjPulseAll", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\ta_0->f_extension()->v_condition.notify_all();\n", 1)
        );
        code.For(
            type.GetMethod("ObjWait", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($@"{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}auto p = a_1->f_extension();
{'\t'}{'\t'}std::unique_lock lock(p->v_mutex, std::adopt_lock);
{'\t'}{'\t'}auto finally = f__finally([&]
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}lock.release();
{'\t'}{'\t'}}});
{'\t'}{'\t'}if (a_0 != -1) return p->v_condition.wait_for(lock, std::chrono::milliseconds(a_0)) == std::cv_status::no_timeout;
{'\t'}{'\t'}p->v_condition.wait(lock);
{'\t'}{'\t'}return true;
{'\t'}}});
", 0)
        );
    })
    .For(get(typeof(Thread)), (type, code) =>
    {
        var helper = type.GetNestedType("StartHelper", BindingFlags.NonPublic) ?? throw new Exception();
        code.Base = "t__thread";
        code.Members = transpiler => ($@"{'\t'}{transpiler.EscapeForMember(get(typeof(ExecutionContext)))} v__5fexecutionContext;
{'\t'}{transpiler.EscapeForMember(get(typeof(SynchronizationContext)))} v__5fsynchronizationContext;
{'\t'}{transpiler.EscapeForMember(get(typeof(string)))} v__5fname;
{'\t'}{transpiler.EscapeForMember(helper)} v__5fstartHelper;
{'\t'}{transpiler.EscapeForMember(get(typeof(bool)))} v__5fmayNeedResetForThreadPool;
{'\t'}{transpiler.EscapeForMember(get(typeof(bool)))} v__pool;

{'\t'}void f__scan(t_scan<t__type> a_scan)
{'\t'}{{
{'\t'}{'\t'}t_System_2eObject::f__scan(a_scan);
{'\t'}{'\t'}a_scan(v__5fexecutionContext);
{'\t'}{'\t'}a_scan(v__5fsynchronizationContext);
{'\t'}{'\t'}a_scan(v__5fname);
{'\t'}{'\t'}a_scan(v__5fstartHelper);
{'\t'}}}
", true, null);
        code.For(
            type.GetMethod("Initialize", declaredAndInstance),
            transpiler => ("\ta_0->v__priority = 2;\n", 1)
        );
        code.For(
            type.GetMethod("InternalFinalize", declaredAndInstance),
            transpiler => (string.Empty, 1)
        );
        code.For(
            type.GetMethod("StartCore", declaredAndInstance),
            transpiler =>
            {
                var run = helper.GetMethod("Run", declaredAndInstance) ?? throw new Exception();
                transpiler.Enqueue(run);
                return (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}f_engine()->f_start(a_0, [a_0]
{'\t'}{{
{'\t'}{'\t'}{transpiler.EscapeForStacked(helper)} RECYCLONE__SPILL p = a_0->v__5fstartHelper;
{'\t'}{'\t'}a_0->v__5fstartHelper = {{}};
{'\t'}{'\t'}auto ts = std::make_unique<t_thread_static>();
{'\t'}{'\t'}try {{
{'\t'}{'\t'}{'\t'}{transpiler.Escape(run)}(p);
{'\t'}{'\t'}{'\t'}return;
{'\t'}{'\t'}}} catch (t__object* p) {{
{'\t'}{'\t'}{'\t'}std::cerr << ""caught: "" << f__string(f__to_string(p)) << std::endl;
{'\t'}{'\t'}}} catch (std::exception& e) {{
{'\t'}{'\t'}{'\t'}std::cerr << ""caught: "" << e.what() << std::endl;
{'\t'}{'\t'}}} catch (...) {{
{'\t'}{'\t'}{'\t'}std::cerr << ""caught unknown"" << std::endl;
{'\t'}{'\t'}}}
{'\t'}{'\t'}std::terminate();
{'\t'}}});
", 0);
            }
        );
        code.For(
            type.GetMethod(nameof(Thread.Join), Type.EmptyTypes),
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\tf_engine()->f_join(a_0);\n", 1)
        );
        code.For(
            type.GetMethod(nameof(Thread.Sleep), [get(typeof(int))]),
            transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}std::this_thread::sleep_for(a_0 == -1 ? std::chrono::milliseconds::max() : std::chrono::milliseconds(a_0));
{'\t'}}});
", 1)
        );
        code.For(
            type.GetMethod(nameof(Thread.SpinWait)),
            transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}for (; a_0 > 0; --a_0) {{
#ifdef _MSC_VER
#if defined(_M_IX86) || defined(_M_X64)
{'\t'}{'\t'}{'\t'}_mm_pause();
#elif defined(_M_ARM) || defined(_M_ARM64)
{'\t'}{'\t'}{'\t'}__yield();
#else
#error
#endif
#else
#if defined(__i386__) || defined(__x86_64__)
{'\t'}{'\t'}{'\t'}asm volatile (""pause"");
#elif defined(__arm__) || defined(__aarch64__)
{'\t'}{'\t'}{'\t'}asm volatile (""yield"");
#else
{'\t'}{'\t'}{'\t'}std::this_thread::yield();
#endif
#endif
{'\t'}{'\t'}}}
{'\t'}}});
", 0)
        );
        code.For(
            type.GetMethod(nameof(Thread.Yield)),
            transpiler => ("\tstd::this_thread::yield();\nreturn true;\n", 1)
        );
        // TODO
        code.For(
            type.GetMethod("GetThreadStateNative", BindingFlags.Instance | BindingFlags.NonPublic),
            transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
        );
        code.For(
            type.GetMethod("IsBackgroundNative", declaredAndInstance),
            transpiler => ("\treturn a_0->v__background;\n", 1)
        );
        code.For(
            type.GetMethod("SetBackgroundNative", declaredAndInstance),
            transpiler => ("\tf_engine()->f_background__(a_0, a_1);\n", 1)
        );
        // TODO
        code.For(
            type.GetProperty(nameof(Thread.IsAlive))!.GetMethod,
            transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
        );
        // TODO
        code.For(
            type.GetProperty(nameof(Thread.IsThreadPoolThread))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__pool;\n", 1)
        );
        // TODO
        code.For(
            type.GetProperty(nameof(Thread.IsThreadPoolThread))!.SetMethod,
            transpiler => ("\ta_0->v__pool = a_1;\n", 1)
        );
        code.For(
            type.GetProperty(nameof(Thread.ManagedThreadId))!.GetMethod,
            transpiler => ("\treturn reinterpret_cast<intptr_t>(static_cast<t__object*>(a_0));\n", 1)
        );
        code.For(
            type.GetMethod("GetPriorityNative", declaredAndInstance),
            transpiler => ("\treturn a_0->v__priority;\n", 1)
        );
        code.For(
            type.GetMethod("SetPriorityNative", declaredAndInstance),
            transpiler => ("\tf_engine()->f_priority__(a_0, a_1);\n", 1)
        );
        code.For(
            type.GetMethod("GetCurrentProcessorNumber", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($@"#ifdef __unix__
#ifdef __EMSCRIPTEN__
{'\t'}return -1;
#else
{'\t'}return sched_getcpu();
#endif
#endif
#ifdef _WIN32
{'\t'}return GetCurrentProcessorNumber();
#endif
", 1)
        );
        code.For(
            type.GetMethod("GetCurrentThreadNative", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ($"\treturn static_cast<{transpiler.Escape(type)}*>(t_engine::v_current_thread);\n", 1)
        );
        // TODO
        code.For(
            type.GetProperty("OptimalMaxSpinWaitsPerSpinIteration", BindingFlags.Static | BindingFlags.NonPublic)!.GetMethod,
            transpiler => ("\treturn 7;\n", 1)
        );
        code.For(
            type.GetMethod("ThreadNameChanged", declaredAndInstance),
            transpiler => (string.Empty, 1)
        );
        code.For(
            type.GetMethod("UninterruptibleSleep0", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\tstd::this_thread::yield();\n", 1)
        );
    })
    // TODO
    .For(get(typeof(ThreadPool)), (type, code) =>
    {
        code.For(
            type.GetMethod("InitializeConfig", BindingFlags.Static | BindingFlags.NonPublic),
            transpiler => ("\treturn true;\n", 1)
        );
    })
    .For(get(typeof(WaitHandle)), (type, code) =>
    {
        if (target == PlatformID.Win32NT)
        {
            // TODO
            code.For(
                type.GetMethod("SignalAndWaitNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return SignalObjectAndWait(a_0, a_1, a_2, TRUE);
{'\t'}}});
", 0)
            );
            code.For(
                type.GetMethod("WaitOneCore", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return WaitForSingleObjectEx(a_0, a_1, TRUE);
{'\t'}}});
", 0)
            );
            code.For(
                type.GetMethod("WaitMultipleIgnoringSyncContext", BindingFlags.Static | BindingFlags.NonPublic, null, [get(typeof(IntPtr*)), get(typeof(int)), get(typeof(bool)), get(typeof(int))], null),
                transpiler => ($@"{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return WaitForMultipleObjectsEx(a_1, reinterpret_cast<const HANDLE*>(a_0), a_2, a_3, TRUE);
{'\t'}}});
", 0)
            );
        }
        else
        {
            code.For(
                type.GetMethod("SignalAndWaitNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}static_cast<t__waitable*>(a_0.v__5fvalue)->f_signal();
{'\t'}{'\t'}return static_cast<t__waitable*>(a_1.v__5fvalue)->f_wait(a_2 == -1 ? std::chrono::milliseconds::max() : std::chrono::milliseconds(a_2)) ? 0 : 0x102;
{'\t'}}});
", 0)
            );
            code.For(
                type.GetMethod("WaitOneCore", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return static_cast<t__waitable*>(a_0.v__5fvalue)->f_wait(a_1 == -1 ? std::chrono::milliseconds::max() : std::chrono::milliseconds(a_1)) ? 0 : 0x102;
{'\t'}}});
", 0)
            );
            code.For(
                type.GetMethod("WaitMultipleIgnoringSyncContext", BindingFlags.Static | BindingFlags.NonPublic, null, [get(typeof(IntPtr*)), get(typeof(int)), get(typeof(bool)), get(typeof(int))], null),
                transpiler => ($@"{'\t'}return f_epoch_region([&]() -> int32_t
{'\t'}{{
{'\t'}{'\t'}if (a_2) {{
{'\t'}{'\t'}{'\t'}return t__waitable::f_wait_all(reinterpret_cast<t__waitable**>(a_0), a_1, a_3 == -1 ? std::chrono::milliseconds::max() : std::chrono::milliseconds(a_3)) ? 0 : 0x102;
{'\t'}{'\t'}}} else {{
{'\t'}{'\t'}{'\t'}auto i = t__waitable::f_wait_any(reinterpret_cast<t__waitable**>(a_0), a_1, a_3 == -1 ? std::chrono::milliseconds::max() : std::chrono::milliseconds(a_3));
{'\t'}{'\t'}{'\t'}return i < a_1 ? i : 0x102;
{'\t'}{'\t'}}}
{'\t'}}});
", 0)
            );
        }
    })
    .For(get(Type.GetType("System.Threading.LowLevelLifoSemaphore", true)!), (type, code) =>
    {
        if (target != PlatformID.Win32NT)
            code.For(
                type.GetMethod("WaitNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return static_cast<t__waitable*>(a_0->v_handle.v__5fvalue)->f_wait(a_1 == -1 ? std::chrono::milliseconds::max() : std::chrono::milliseconds(a_1)) ? 0 : 0x102;
{'\t'}}});
", 0)
            );
    });
}
