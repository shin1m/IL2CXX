using System;
using System.Reflection;
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
                transpiler => $@"{'\t'}reinterpret_cast<std::atomic_int32_t*>(a_0)->compare_exchange_strong(a_2, a_1);
{'\t'}return a_2;
"
            );
            code.For(
                type.GetMethod(nameof(Interlocked.CompareExchange), new[] { typeof(long).MakeByRefType(), typeof(long), typeof(long) }),
                transpiler => $@"{'\t'}reinterpret_cast<std::atomic_int64_t*>(a_0)->compare_exchange_strong(a_2, a_1);
{'\t'}return a_2;
"
            );
            code.For(
                type.GetMethod(nameof(Interlocked.CompareExchange), new[] { typeof(IntPtr).MakeByRefType(), typeof(IntPtr), typeof(IntPtr) }),
                transpiler => $@"{'\t'}void* p = a_2;
{'\t'}reinterpret_cast<std::atomic<void*>&>(a_0->v__5fvalue).compare_exchange_strong(p, a_1);
{'\t'}return {transpiler.EscapeForValue(typeof(IntPtr))}{{p}};
"
            );
            code.For(
                type.GetMethod(nameof(Interlocked.CompareExchange), new[] { typeof(object).MakeByRefType(), typeof(object), typeof(object) }),
                transpiler => $@"{'\t'}a_0->f_compare_exchange(a_2, std::move(a_1));
{'\t'}return a_2;
"
            );
            code.For(
                type.GetMethod(nameof(Interlocked.Exchange), new[] { typeof(int).MakeByRefType(), typeof(int) }),
                transpiler => "\treturn reinterpret_cast<std::atomic_int32_t*>(a_0)->exchange(a_1);\n"
            );
            code.For(
                type.GetMethod(nameof(Interlocked.Exchange), new[] { typeof(long).MakeByRefType(), typeof(long) }),
                transpiler => "\treturn reinterpret_cast<std::atomic_int64_t*>(a_0)->exchange(a_1);\n"
            );
            code.For(
                type.GetMethod(nameof(Interlocked.Exchange), new[] { typeof(IntPtr).MakeByRefType(), typeof(IntPtr) }),
                transpiler => $"\treturn {transpiler.EscapeForValue(typeof(IntPtr))}{{reinterpret_cast<std::atomic<void*>&>(a_0->v__5fvalue).exchange(a_1)}};\n"
            );
            code.For(
                type.GetMethod(nameof(Interlocked.Exchange), new[] { typeof(object).MakeByRefType(), typeof(object) }),
                transpiler => "\treturn a_0->f_exchange(std::move(a_1));\n"
            );
            code.For(
                type.GetMethod("ExchangeAdd", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(int).MakeByRefType(), typeof(int) }, null),
                transpiler => "\treturn reinterpret_cast<std::atomic_int32_t*>(a_0)->fetch_add(a_1);\n"
            );
            code.For(
                type.GetMethod("ExchangeAdd", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(long).MakeByRefType(), typeof(long) }, null),
                transpiler => "\treturn reinterpret_cast<std::atomic_int64_t*>(a_0)->fetch_add(a_1);\n"
            );
            code.For(
                type.GetMethod(nameof(Interlocked.MemoryBarrier)),
                transpiler => "\tstd::atomic_thread_fence(std::memory_order_seq_cst);\n"
            );
        })
        .For(typeof(Thread), (type, code) =>
        {
            code.Members = transpiler => ($@"
{'\t'}static IL2CXX__PORTABLE__THREAD {transpiler.Escape(type)}* v__current;

{'\t'}static {transpiler.Escape(type)}* f__current()
{'\t'}{{
{'\t'}{'\t'}return v__current;
{'\t'}}}

{'\t'}{transpiler.EscapeForMember(typeof(ExecutionContext))} v__5fexecutionContext;
{'\t'}{transpiler.EscapeForMember(typeof(SynchronizationContext))} v__5fsynchronizationContext;
{'\t'}{transpiler.EscapeForMember(typeof(Delegate))} v__5fdelegate;
{'\t'}{transpiler.EscapeForMember(typeof(object))} v__5fthreadStartArg;
{'\t'}t_thread* v__internal;
{'\t'}{transpiler.EscapeForMember(Type.GetType("System.Runtime.Serialization.DeserializationTracker"))} v__deserialization_tracker;

{'\t'}void f__scan(t_scan a_scan)
{'\t'}{{
{'\t'}{'\t'}t_System_2eObject::f__scan(a_scan);
{'\t'}{'\t'}a_scan(v__5fexecutionContext);
{'\t'}{'\t'}a_scan(v__5fsynchronizationContext);
{'\t'}{'\t'}a_scan(v__5fdelegate);
{'\t'}{'\t'}a_scan(v__5fthreadStartArg);
{'\t'}{'\t'}a_scan(v__deserialization_tracker);
{'\t'}}}
{'\t'}template<typename T>
{'\t'}void f__start(T a_do);
{'\t'}void f__start();
{'\t'}void f__join();
", true);
            code.For(
                type.GetConstructor(new[] { typeof(ThreadStart) }),
                transpiler => $@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}p->v__5fdelegate = std::move(a_0);
{'\t'}return p;
"
            );
            code.For(
                type.GetConstructor(new[] { typeof(ParameterizedThreadStart) }),
                transpiler => $@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}p->v__5fdelegate = std::move(a_0);
{'\t'}return p;
"
            );
            code.For(
                type.GetMethod(nameof(object.GetHashCode)),
                transpiler => "\treturn reinterpret_cast<intptr_t>(static_cast<t_object*>(a_0));\n"
            );
            code.For(
                type.GetMethod(nameof(Thread.Start), Type.EmptyTypes),
                transpiler => "\treturn a_0->f__start();\n"
            );
            code.For(
                type.GetMethod(nameof(Thread.Join), Type.EmptyTypes),
                transpiler => "\ta_0->f__join();\n"
            );
            code.For(
                type.GetMethod(nameof(Thread.Sleep), new[] { typeof(int) }),
                transpiler => "\tstd::this_thread::sleep_for(std::chrono::milliseconds(a_0));\n"
            );
            code.For(
                type.GetMethod(nameof(Thread.SpinWait)),
                transpiler => "\tfor (; a_0 > 0; --a_0) std::this_thread::yield();\n"
            );
            code.For(
                type.GetProperty(nameof(Thread.IsBackground)).SetMethod,
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
            code.For(
                type.GetProperty(nameof(Thread.IsThreadPoolThread)).GetMethod,
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
            code.For(
                type.GetProperty(nameof(Thread.ManagedThreadId)).GetMethod,
                transpiler => "\treturn reinerpret_cast<intptr_t>(static_cast<t_object*>(a_0));\n"
            );
            code.For(
                type.GetMethod("InternalFinalize", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => string.Empty
            );
            code.For(
                type.GetMethod("GetCurrentProcessorNumber", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\treturn sched_getcpu();\n"
            );
            code.For(
                type.GetMethod("GetCurrentThreadNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $"\treturn {transpiler.Escape(typeof(Thread))}::f__current();\n"
            );
            code.For(
                type.GetMethod("GetThreadDeserializationTracker", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}auto p = {transpiler.Escape(typeof(Thread))}::f__current();
{'\t'}if (!p->v__deserialization_tracker) p->v__deserialization_tracker = f__new_zerod<{transpiler.Escape(Type.GetType("System.Runtime.Serialization.DeserializationTracker"))}>();
{'\t'}return p->v__deserialization_tracker;
"
            );
        })
        .For(typeof(ThreadPool), (type, code) =>
        {
            code.For(
                type.GetMethod("NotifyWorkItemProgressNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\tthrow std::runtime_error(\"NotImplementedException\");\n"
            );
        })
        .For(typeof(Monitor), (type, code) =>
        {
            code.For(
                type.GetMethod("ReliableEnter", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}t_epoch_region region;
{'\t'}a_0->f_extension()->v_mutex.lock();
{'\t'}*a_1 = true;
"
            );
            code.For(
                type.GetMethod("ReliableEnterTimeout", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}t_epoch_region region;
{'\t'}*a_2 = a_0->f_extension()->v_mutex.try_lock_for(std::chrono::milliseconds(a_1));
"
            );
            code.For(
                type.GetMethod(nameof(Monitor.Enter), new[] { typeof(object) }),
                transpiler => $@"{'\t'}t_epoch_region region;
{'\t'}a_0->f_extension()->v_mutex.lock();
"
            );
            code.For(
                type.GetMethod(nameof(Monitor.Exit)),
                transpiler => "\ta_0->f_extension()->v_mutex.unlock();\n"
            );
            code.For(
                type.GetMethod("IsEnteredNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}auto p = a_0->f_extension();
{'\t'}return p->v_mutex.locked();
"
            );
            code.For(
                type.GetMethod("ObjPulse", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\ta_0->f_extension()->v_condition.notify_one();\n"
            );
            code.For(
                type.GetMethod("ObjPulseAll", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => "\ta_0->f_extension()->v_condition.notify_all();\n"
            );
            code.For(
                type.GetMethod("ObjWait", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => $@"{'\t'}if (a_0) throw std::runtime_error(""NotSupportedException"");
{'\t'}t_epoch_region region;
{'\t'}auto p = a_2->f_extension();
{'\t'}std::unique_lock<std::recursive_timed_mutex> lock(p->v_mutex, std::adopt_lock);
{'\t'}auto finally = f__finally([&]
{'\t'}{{
{'\t'}{'\t'}lock.release();
{'\t'}}});
{'\t'}if (a_1 != -1) return p->v_condition.wait_for(lock, std::chrono::milliseconds(a_1)) == std::cv_status::no_timeout;
{'\t'}p->v_condition.wait(lock);
{'\t'}return true;
"
            );
        });
    }
}
