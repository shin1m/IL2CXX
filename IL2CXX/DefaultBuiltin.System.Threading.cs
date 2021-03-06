using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace IL2CXX
{
    partial class DefaultBuiltin
    {
        private static Builtin SetupSystemThreading(this Builtin @this) => @this
        .For(typeof(Interlocked), (type, code) =>
        {
            code.For(
                type.GetMethod(nameof(Interlocked.CompareExchange), new[] { typeof(int).MakeByRefType(), typeof(int), typeof(int) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}reinterpret_cast<std::atomic_int32_t*>(a_0)->compare_exchange_strong(a_2, a_1);
{'\t'}return a_2;
", 1)
            );
            code.For(
                type.GetMethod(nameof(Interlocked.CompareExchange), new[] { typeof(long).MakeByRefType(), typeof(long), typeof(long) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}reinterpret_cast<std::atomic_int64_t*>(a_0)->compare_exchange_strong(a_2, a_1);
{'\t'}return a_2;
", 1)
            );
            code.For(
                type.GetMethod(nameof(Interlocked.CompareExchange), new[] { typeof(IntPtr).MakeByRefType(), typeof(IntPtr), typeof(IntPtr) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}void* p = a_2;
{'\t'}reinterpret_cast<std::atomic<void*>&>(a_0->v__5fvalue).compare_exchange_strong(p, a_1);
{'\t'}return {transpiler.EscapeForValue(typeof(IntPtr))}{{p}};
", 1)
            );
            code.For(
                type.GetMethod(nameof(Interlocked.CompareExchange), new[] { typeof(object).MakeByRefType(), typeof(object), typeof(object) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}f__compare_exchange(*a_0, a_2, a_1);
{'\t'}return a_2;
", 1)
            );
            code.For(
                type.GetMethod(nameof(Interlocked.Exchange), new[] { typeof(int).MakeByRefType(), typeof(int) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\treturn reinterpret_cast<std::atomic_int32_t*>(a_0)->exchange(a_1);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Interlocked.Exchange), new[] { typeof(long).MakeByRefType(), typeof(long) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\treturn reinterpret_cast<std::atomic_int64_t*>(a_0)->exchange(a_1);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Interlocked.Exchange), new[] { typeof(IntPtr).MakeByRefType(), typeof(IntPtr) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $"\treturn {transpiler.EscapeForValue(typeof(IntPtr))}{{reinterpret_cast<std::atomic<void*>&>(a_0->v__5fvalue).exchange(a_1)}};\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Interlocked.Exchange), new[] { typeof(object).MakeByRefType(), typeof(object) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\treturn f__exchange(*a_0, a_1);\n", 1)
            );
            code.For(
                type.GetMethod("ExchangeAdd", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(int).MakeByRefType(), typeof(int) }, null),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\treturn reinterpret_cast<std::atomic_int32_t*>(a_0)->fetch_add(a_1);\n", 1)
            );
            code.For(
                type.GetMethod("ExchangeAdd", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(long).MakeByRefType(), typeof(long) }, null),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\treturn reinterpret_cast<std::atomic_int64_t*>(a_0)->fetch_add(a_1);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Interlocked.MemoryBarrier)),
                transpiler => ("\tstd::atomic_thread_fence(std::memory_order_seq_cst);\n", 1)
            );
        })
        .For(typeof(Monitor), (type, code) =>
        {
            code.For(
                type.GetMethod("ReliableEnter", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}a_0->f_extension()->v_mutex.lock();
{'\t'}*a_1 = true;
", 1)
            );
            code.For(
                type.GetMethod("ReliableEnterTimeout", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\t*a_2 = a_0->f_extension()->v_mutex.try_lock_for(std::chrono::milliseconds(a_1));\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Monitor.Enter), new[] { typeof(object) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\ta_0->f_extension()->v_mutex.lock();\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Monitor.Exit)),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + "\ta_0->f_extension()->v_mutex.unlock();\n", 1)
            );
            code.For(
                type.GetMethod("IsEnteredNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}auto p = a_0->f_extension();
{'\t'}return p->v_mutex.locked();
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
                transpiler => ($@"{'\t'}auto p = a_1->f_extension();
{'\t'}std::unique_lock lock(p->v_mutex, std::adopt_lock);
{'\t'}auto finally = f__finally([&]
{'\t'}{{
{'\t'}{'\t'}lock.release();
{'\t'}}});
{'\t'}if (a_0 != -1) return p->v_condition.wait_for(lock, std::chrono::milliseconds(a_0)) == std::cv_status::no_timeout;
{'\t'}p->v_condition.wait(lock);
{'\t'}return true;
", 0)
            );
        })
        .For(typeof(RegisteredWaitHandle), (type, code) =>
        {
            code.For(
                type.GetMethod("WaitHandleCleanupNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
            code.For(
                type.GetMethod("UnregisterWaitNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(typeof(Thread), (type, code) =>
        {
            var helper = type.GetNestedType("StartHelper", BindingFlags.NonPublic);
            code.Base = "t__thread";
            code.Members = transpiler => ($@"{'\t'}{transpiler.EscapeForMember(typeof(ExecutionContext))} v__5fexecutionContext;
{'\t'}{transpiler.EscapeForMember(typeof(SynchronizationContext))} v__5fsynchronizationContext;
{'\t'}{transpiler.EscapeForMember(typeof(string))} v__5fname;
{'\t'}{transpiler.EscapeForMember(helper)} v__5fstartHelper;
{'\t'}{transpiler.EscapeForMember(typeof(bool))} v__5fmayNeedResetForThreadPool;
{'\t'}{transpiler.EscapeForMember(typeof(bool))} v__pool;

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
                transpiler => ("\ta_0->v__priority == 2;\n", 1)
            );
            code.For(
                type.GetMethod("InternalFinalize", declaredAndInstance),
                transpiler => (string.Empty, 1)
            );
            code.For(
                type.GetMethod("StartCore", declaredAndInstance),
                transpiler =>
                {
                    var run = helper.GetMethod("Run", declaredAndInstance);
                    transpiler.Enqueue(run);
                    return (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}f_engine()->f_start(a_0, [a_0]
{'\t'}{{
{'\t'}{'\t'}{transpiler.EscapeForRoot(helper)} p = std::move(a_0->v__5fstartHelper);
{'\t'}{'\t'}t_thread_static ts;
{'\t'}{'\t'}{transpiler.Escape(run)}(p);
{'\t'}}});
", 0);
                }
            );
            code.For(
                type.GetMethod(nameof(Thread.Join), Type.EmptyTypes),
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\tf_engine()->f_join(a_0);\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Thread.Sleep), new[] { typeof(int) }),
                transpiler => ("\tstd::this_thread::sleep_for(a_0 == -1 ? std::chrono::milliseconds::max() : std::chrono::milliseconds(a_0));\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Thread.SpinWait)),
                transpiler => ("\tfor (; a_0 > 0; --a_0) std::this_thread::yield();\n", 0)
            );
            code.For(
                type.GetMethod(nameof(Thread.Yield)),
                transpiler => ("\tstd::this_thread::yield();\nreturn true;\n", 1)
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
                type.GetProperty(nameof(Thread.IsThreadPoolThread)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__pool;\n", 1)
            );
            // TODO
            code.For(
                type.GetProperty(nameof(Thread.IsThreadPoolThread)).SetMethod,
                transpiler => ("\ta_0->v__pool = a_1;\n", 1)
            );
            code.For(
                type.GetProperty(nameof(Thread.ManagedThreadId)).GetMethod,
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
{'\t'}return sched_getcpu();
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
                type.GetMethod("GetOptimalMaxSpinWaitsPerSpinIterationInternal", BindingFlags.Static | BindingFlags.NonPublic),
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
        .For(typeof(ThreadPool), (type, code) =>
        {
            code.For(
                type.GetMethod("GetEnableWorkerTracking", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn false;\n", 0)
            );
            code.For(
                type.GetMethod("InitializeConfigAndDetermineUsePortableThreadPool", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn true;\n", 1)
            );
            code.For(
                type.GetMethod("NotifyWorkItemCompleteNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
            code.For(
                type.GetMethod("NotifyWorkItemProgressNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
            code.For(
                type.GetMethod("PerformRuntimeSpecificGateActivities", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn false;\n", 1)
            );
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                code.For(
                    type.GetMethod("QueueWaitCompletionNative", BindingFlags.Static | BindingFlags.NonPublic),
                    transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
                );
            code.For(
                type.GetMethod("ReportThreadStatusNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(typeof(WaitHandle), (type, code) =>
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO
                code.For(
                    type.GetMethod("WaitOneCore", BindingFlags.Static | BindingFlags.NonPublic),
                    transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
                );
                code.For(
                    type.GetMethod("WaitMultipleIgnoringSyncContext", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(IntPtr*), typeof(int), typeof(bool), typeof(int) }, null),
                    transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
                );
            }
            else
            {
                code.For(
                    type.GetMethod("SignalAndWaitNative", BindingFlags.Static | BindingFlags.NonPublic),
                    transpiler => ($@"{'\t'}static_cast<t__waitable*>(a_0.v__5fvalue)->f_signal();
{'\t'}return static_cast<t__waitable*>(a_1.v__5fvalue)->f_wait(a_2 == -1 ? std::chrono::milliseconds::max() : std::chrono::milliseconds(a_2)) ? 0 : 0x102;
", 0)
                );
                code.For(
                    type.GetMethod("WaitOneCore", BindingFlags.Static | BindingFlags.NonPublic),
                    transpiler => ("\treturn static_cast<t__waitable*>(a_0.v__5fvalue)->f_wait(a_1 == -1 ? std::chrono::milliseconds::max() : std::chrono::milliseconds(a_1)) ? 0 : 0x102;\n", 0)
                );
                code.For(
                    type.GetMethod("WaitMultipleIgnoringSyncContext", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(IntPtr*), typeof(int), typeof(bool), typeof(int) }, null),
                    transpiler => ($@"{'\t'}if (a_2) {{
{'\t'}{'\t'}return t__waitable::f_wait_all(reinterpret_cast<t__waitable**>(a_0), a_1, a_3 == -1 ? std::chrono::milliseconds::max() : std::chrono::milliseconds(a_3)) ? 0 : 0x102;
{'\t'}}} else {{
{'\t'}{'\t'}auto i = t__waitable::f_wait_any(reinterpret_cast<t__waitable**>(a_0), a_1, a_3 == -1 ? std::chrono::milliseconds::max() : std::chrono::milliseconds(a_3));
{'\t'}{'\t'}return i < a_1 ? i : 0x102;
{'\t'}}}
", 0)
                );
            }
        })
        .For(Type.GetType("System.Threading.LowLevelLifoSemaphore"), (type, code) =>
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                code.For(
                    type.GetMethod("WaitNative", BindingFlags.Static | BindingFlags.NonPublic),
                    transpiler => ("\treturn static_cast<t__waitable*>(a_0->v_handle.v__5fvalue)->f_wait(a_1 == -1 ? std::chrono::milliseconds::max() : std::chrono::milliseconds(a_1)) ? 0 : 0x102;\n", 0)
                );
        });
    }
}
