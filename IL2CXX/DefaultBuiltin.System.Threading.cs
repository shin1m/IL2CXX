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
                transpiler => ($@"{'\t'}if (a_0) throw std::runtime_error(""NotSupportedException"");
{'\t'}auto p = a_2->f_extension();
{'\t'}std::unique_lock<std::recursive_timed_mutex> lock(p->v_mutex, std::adopt_lock);
{'\t'}auto finally = f__finally([&]
{'\t'}{{
{'\t'}{'\t'}lock.release();
{'\t'}}});
{'\t'}if (a_1 != -1) return p->v_condition.wait_for(lock, std::chrono::milliseconds(a_1)) == std::cv_status::no_timeout;
{'\t'}p->v_condition.wait(lock);
{'\t'}return true;
", 0)
            );
        })
        .For(typeof(Thread), (type, code) =>
        {
            code.Base = "t__thread";
            code.Members = transpiler => ($@"{'\t'}static {transpiler.Escape(type)}* f__current()
{'\t'}{{
{'\t'}{'\t'}return static_cast<{transpiler.Escape(type)}*>(v__current);
{'\t'}}}

{'\t'}{transpiler.EscapeForMember(typeof(ExecutionContext))} v__5fexecutionContext;
{'\t'}{transpiler.EscapeForMember(typeof(SynchronizationContext))} v__5fsynchronizationContext;
{'\t'}{transpiler.EscapeForMember(typeof(Delegate))} v__5fdelegate;
{'\t'}{transpiler.EscapeForMember(typeof(object))} v__5fthreadStartArg;
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
", true, null);
            code.For(
                type.GetConstructor(new[] { typeof(ThreadStart) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}p->v__5fdelegate = a_0;
{'\t'}p->v__priority = 2;
{'\t'}return p;
", 0)
            );
            code.For(
                type.GetConstructor(new[] { typeof(ParameterizedThreadStart) }),
                transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}auto p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}p->v__5fdelegate = a_0;
{'\t'}p->v__priority = 2;
{'\t'}return p;
", 0)
            );
            code.For(
                type.GetMethod(nameof(object.GetHashCode)),
                transpiler => ("\treturn reinterpret_cast<intptr_t>(static_cast<t_object*>(a_0));\n", 1)
            );
            code.For(
                type.GetMethod(nameof(Thread.Start), Type.EmptyTypes),
                transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}return a_0->f__start([a_0]
{'\t'}{{
{'\t'}{'\t'}t_thread_static ts;
{'\t'}{'\t'}if (a_0->v__5fdelegate->f_type()->f__is(&t__type_of<{transpiler.Escape(typeof(ThreadStart))}>::v__instance))
{'\t'}{'\t'}{'\t'}{transpiler.Escape(typeof(ThreadStart).GetMethod("Invoke"))}({transpiler.CastValue(typeof(ThreadStart), "a_0->v__5fdelegate")});
{'\t'}{'\t'}else
{'\t'}{'\t'}{'\t'}{transpiler.Escape(typeof(ParameterizedThreadStart).GetMethod("Invoke"))}({transpiler.CastValue(typeof(ParameterizedThreadStart), "a_0->v__5fdelegate")}, a_0->v__5fthreadStartArg);
{'\t'}{'\t'}}});
", 1)
            );
            code.For(
                type.GetMethod(nameof(Thread.Join), Type.EmptyTypes),
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\ta_0->f__join();\n", 1)
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
                type.GetProperty(nameof(Thread.IsBackground)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__background;\n", 1)
            );
            code.For(
                type.GetProperty(nameof(Thread.IsBackground)).SetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\ta_0->f__background__(a_1);\n", 1)
            );
            // TODO
            code.For(
                type.GetProperty(nameof(Thread.IsThreadPoolThread)).GetMethod,
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
            code.For(
                type.GetProperty(nameof(Thread.ManagedThreadId)).GetMethod,
                transpiler => ("\treturn reinterpret_cast<intptr_t>(static_cast<t_object*>(a_0));\n", 1)
            );
            code.For(
                type.GetProperty(nameof(Thread.Priority)).GetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\ta_0->v__priority;\n", 1)
            );
            code.For(
                type.GetProperty(nameof(Thread.Priority)).SetMethod,
                transpiler => (transpiler.GenerateCheckNull("a_0") + "\ta_0->f__priority__(a_1);\n", 1)
            );
            code.For(
                type.GetMethod("InternalFinalize", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler => (string.Empty, 0)
            );
            code.For(
                type.GetMethod("GetCurrentProcessorNumber", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\treturn sched_getcpu();\n", 1)
            );
            code.For(
                type.GetMethod("GetCurrentThreadNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($"\treturn {transpiler.Escape(typeof(Thread))}::f__current();\n", 1)
            );
            code.For(
                type.GetMethod("GetThreadDeserializationTracker", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ($@"{'\t'}auto p = {transpiler.Escape(typeof(Thread))}::f__current();
{'\t'}if (!p->v__deserialization_tracker) p->v__deserialization_tracker = f__new_zerod<{transpiler.Escape(Type.GetType("System.Runtime.Serialization.DeserializationTracker"))}>();
{'\t'}return p->v__deserialization_tracker;
", 0)
            );
        })
        .For(typeof(ThreadPool), (type, code) =>
        {
            // TODO
            code.For(
                type.GetMethod("NotifyWorkItemProgressNative", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        })
        .For(typeof(WaitHandle), (type, code) =>
        {
            // TODO
            code.For(
                type.GetMethod("WaitOneCore", BindingFlags.Static | BindingFlags.NonPublic),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
            // TODO
            code.For(
                type.GetMethod("WaitMultipleIgnoringSyncContext", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(IntPtr*), typeof(int), typeof(bool), typeof(int) }, null),
                transpiler => ("\tthrow std::runtime_error(\"NotImplementedException\");\n", 0)
            );
        });
    }
}
