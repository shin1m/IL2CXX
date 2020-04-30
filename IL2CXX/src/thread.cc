#include <il2cxx/thread.h>
#include <sys/resource.h>

#ifdef IL2CXX__STACK_SCAN_PARTIAL
extern "C"
{

enum _Unwind_Reason_Code
{
	_URC_NO_REASON = 0,
	_URC_FOREIGN_EXCEPTION_CAUGHT = 1,
	_URC_FATAL_PHASE2_ERROR = 2,
	_URC_FATAL_PHASE1_ERROR = 3,
	_URC_NORMAL_STOP = 4,
	_URC_END_OF_STACK = 5,
	_URC_HANDLER_FOUND = 6,
	_URC_INSTALL_CONTEXT = 7,
	_URC_CONTINUE_UNWIND = 8
};
typedef int _Unwind_Action;
#define _UA_SEARCH_PHASE        1
#define _UA_CLEANUP_PHASE       2
#define _UA_HANDLER_FRAME       4
#define _UA_FORCE_UNWIND        8
#define _UA_END_OF_STACK       16
struct _Unwind_Context;
struct _Unwind_Exception;
typedef void(*_Unwind_Exception_Cleanup_Fn)(_Unwind_Reason_Code, _Unwind_Exception*);
typedef _Unwind_Reason_Code(*_Unwind_Stop_Fn)(int, _Unwind_Action, uint64_t, _Unwind_Exception*, _Unwind_Context*, void*);
struct _Unwind_Exception
{
	uint64_t exception_class;
	_Unwind_Exception_Cleanup_Fn exception_cleanup;
	unsigned long private_1;
	unsigned long private_2;
} __attribute__((__aligned__));

#define _U_VERSION      1
typedef _Unwind_Reason_Code(*_Unwind_Personality_Fn)(int, _Unwind_Action, uint64_t, _Unwind_Exception*, _Unwind_Context*);
struct _Unwind_Context
{
	unw_cursor_t cursor;
	int end_of_stack = 0;
};

#define __IL2CXX_UNWIND_GETCONTEXT(error)\
	_Unwind_Context context;\
	unw_context_t uc;\
	if (unw_getcontext(&uc) < 0 || unw_init_local(&context.cursor, &uc) < 0) return error;

static _Unwind_Reason_Code _Unwind_Phase2(_Unwind_Exception* exception_object, _Unwind_Context* context)
{
	auto stop = reinterpret_cast<_Unwind_Stop_Fn>(exception_object->private_1);
	auto exception_class = exception_object->exception_class;
	auto stop_parameter = reinterpret_cast<void*>(exception_object->private_2);
	_Unwind_Action actions = _UA_CLEANUP_PHASE;
	if (stop) actions |= _UA_FORCE_UNWIND;
	while (true) {
		auto ret = unw_step(&context->cursor);
		if (ret <= 0) {
			if (ret < 0) return _URC_FATAL_PHASE2_ERROR;
			actions |= _UA_END_OF_STACK;
			context->end_of_stack = 1;
		}
		if (stop && stop(_U_VERSION, actions, exception_class, exception_object, context, stop_parameter) != _URC_NO_REASON) return _URC_FATAL_PHASE2_ERROR;
		unw_proc_info_t pi;
		if (context->end_of_stack || unw_get_proc_info(&context->cursor, &pi) < 0) return _URC_FATAL_PHASE2_ERROR;
		auto personality = reinterpret_cast<_Unwind_Personality_Fn>(static_cast<uintptr_t>(pi.handler));
		if (!personality) continue;
		if (!stop) {
			unw_word_t ip;
			if (unw_get_reg(&context->cursor, UNW_REG_IP, &ip) < 0) return _URC_FATAL_PHASE2_ERROR;
			if ((unsigned long)stop_parameter == ip) {
				actions |= _UA_HANDLER_FRAME;
				if (il2cxx::t_thread::v_current) il2cxx::t_thread::v_current->v_unwinding.store(false, std::memory_order_relaxed);
			}
		}
		auto reason = personality(_U_VERSION, actions, exception_class, exception_object, context);
		if (reason != _URC_CONTINUE_UNWIND) {
			if (reason != _URC_INSTALL_CONTEXT) return _URC_FATAL_PHASE2_ERROR;
			unw_resume(&context->cursor);
			abort();
		}
		if (actions & _UA_HANDLER_FRAME) abort();
	}
	return _URC_FATAL_PHASE2_ERROR;
}

_Unwind_Reason_Code _Unwind_RaiseException(_Unwind_Exception* exception_object)
{
	if (il2cxx::t_thread::v_current) il2cxx::t_thread::v_current->v_unwinding.store(true, std::memory_order_relaxed);
	__IL2CXX_UNWIND_GETCONTEXT(_URC_FATAL_PHASE1_ERROR)
	auto exception_class = exception_object->exception_class;
	while (true) {
		auto ret = unw_step(&context.cursor);
		if (ret <= 0) return ret == 0 ? _URC_END_OF_STACK : _URC_FATAL_PHASE1_ERROR;
		if (il2cxx::t_thread::v_current) il2cxx::t_thread::v_current->f_unthunk(context.cursor);
		unw_proc_info_t pi;
		if (unw_get_proc_info(&context.cursor, &pi) < 0) return _URC_FATAL_PHASE1_ERROR;
		auto personality = reinterpret_cast<_Unwind_Personality_Fn>(static_cast<uintptr_t>(pi.handler));
		if (!personality) continue;
		auto reason = personality(_U_VERSION, _UA_SEARCH_PHASE, exception_class, exception_object, &context);
		if (reason == _URC_CONTINUE_UNWIND) continue;
		if (reason == _URC_HANDLER_FOUND) break;
		return _URC_FATAL_PHASE1_ERROR;
	}
	unw_word_t ip;
	if (unw_get_reg(&context.cursor, UNW_REG_IP, &ip) < 0) return _URC_FATAL_PHASE1_ERROR;
	exception_object->private_1 = 0;
	exception_object->private_2 = ip;
	if (unw_init_local(&context.cursor, &uc) < 0) return _URC_FATAL_PHASE1_ERROR;
	return _Unwind_Phase2(exception_object, &context);
}

_Unwind_Reason_Code _Unwind_ForcedUnwind(_Unwind_Exception* exception_object, _Unwind_Stop_Fn stop, void* stop_parameter)
{
	if (!stop) return _URC_FATAL_PHASE2_ERROR;
	__IL2CXX_UNWIND_GETCONTEXT(_URC_FATAL_PHASE2_ERROR)
	exception_object->private_1 = reinterpret_cast<unsigned long>(stop);
	exception_object->private_2 = reinterpret_cast<unsigned long>(stop_parameter);
	return _Unwind_Phase2(exception_object, &context);
}

void _Unwind_Resume(_Unwind_Exception* exception_object)
{
	__IL2CXX_UNWIND_GETCONTEXT(abort())
	_Unwind_Phase2(exception_object, &context);
	abort();
}

void _Unwind_DeleteException(_Unwind_Exception* exception_object)
{
	auto cleanup = exception_object->exception_cleanup;
	if (cleanup) cleanup(_URC_FOREIGN_EXCEPTION_CAUGHT, exception_object);
}

unsigned long _Unwind_GetGR(_Unwind_Context* context, int index)
{
	if (index == UNW_REG_SP && context->end_of_stack) return 0;
	unw_word_t val;
	unw_get_reg(&context->cursor, index, &val);
	return val;
}

void _Unwind_SetGR(_Unwind_Context* context, int index, unsigned long new_value)
{
#ifdef UNW_TARGET_X86
	index = dwarf_to_unw_regnum(index);
#endif
	unw_set_reg(&context->cursor, index, new_value);
#ifdef UNW_TARGET_IA64
	if (index >= UNW_IA64_GR && index <= UNW_IA64_GR + 127) unw_set_reg(&context->cursor, UNW_IA64_NAT + (index - UNW_IA64_GR), 0);
#endif
}

unsigned long _Unwind_GetIP(_Unwind_Context* context)
{
	unw_word_t val;
	unw_get_reg(&context->cursor, UNW_REG_IP, &val);
	return val;
}

unsigned long _Unwind_GetIPInfo(_Unwind_Context* context, int* ip_before_insn)
{
	unw_word_t val;
	unw_get_reg(&context->cursor, UNW_REG_IP, &val);
	*ip_before_insn = unw_is_signal_frame(&context->cursor);
	return val;
}

void _Unwind_SetIP(_Unwind_Context* context, unsigned long new_value)
{
	unw_set_reg(&context->cursor, UNW_REG_IP, new_value);
}

unsigned long _Unwind_GetLanguageSpecificData(_Unwind_Context* context)
{
	unw_proc_info_t pi;
	pi.lsda = 0;
	unw_get_proc_info(&context->cursor, &pi);
	return pi.lsda;
}

unsigned long _Unwind_GetRegionStart(_Unwind_Context* context)
{
	unw_proc_info_t pi;
	pi.start_ip = 0;
	unw_get_proc_info(&context->cursor, &pi);
	return pi.start_ip;
}

_Unwind_Reason_Code _Unwind_Resume_or_Rethrow(_Unwind_Exception* exception_object)
{
	if (!exception_object->private_1) return _Unwind_RaiseException(exception_object);
	__IL2CXX_UNWIND_GETCONTEXT(_URC_FATAL_PHASE2_ERROR)
	return _Unwind_Phase2(exception_object, &context);
}

unsigned long _Unwind_GetBSP(_Unwind_Context* context)
{
#ifdef UNW_TARGET_IA64
	unw_word_t val;
	unw_get_reg(&context->cursor, UNW_IA64_BSP, &val);
	return val;
#else
	return 0;
#endif
}

unsigned long _Unwind_GetCFA(_Unwind_Context* context)
{
	unw_word_t val;
	unw_get_reg(&context->cursor, UNW_REG_SP, &val);
	return val;
}

unsigned long _Unwind_GetDataRelBase(_Unwind_Context* context)
{
	unw_proc_info_t pi;
	pi.gp = 0;
	unw_get_proc_info(&context->cursor, &pi);
	return pi.gp;
}

unsigned long _Unwind_GetTextRelBase(_Unwind_Context* context)
{
	return 0;
}

typedef _Unwind_Reason_Code (*_Unwind_Trace_Fn)(_Unwind_Context*, void*);

_Unwind_Reason_Code _Unwind_Backtrace(_Unwind_Trace_Fn trace, void* trace_parameter)
{
	__IL2CXX_UNWIND_GETCONTEXT(_URC_FATAL_PHASE1_ERROR)
	while (true) {
		auto ret = unw_step(&context.cursor);
		if (ret <= 0) return ret == 0 ? _URC_END_OF_STACK : _URC_FATAL_PHASE1_ERROR;
		if (il2cxx::t_thread::v_current) il2cxx::t_thread::v_current->f_unthunk(context.cursor);
		if (trace(&context, trace_parameter) != _URC_NO_REASON) return _URC_FATAL_PHASE1_ERROR;
	}
}

void* _Unwind_FindEnclosingFunction(void* ip)
{
	unw_proc_info_t pi;
	if (unw_get_proc_info_by_ip(unw_local_addr_space, static_cast<unw_word_t>(reinterpret_cast<uintptr_t>(ip)), &pi, 0) < 0) return NULL;
	return reinterpret_cast<void*>(static_cast<uintptr_t>(pi.start_ip));
}

}
#endif

namespace il2cxx
{

IL2CXX__PORTABLE__THREAD t_thread* t_thread::v_current;

t_thread::t_thread()
{
	rlimit limit;
	if (getrlimit(RLIMIT_STACK, &limit) == -1) throw std::system_error(errno, std::generic_category());
	v_stack_buffer = std::make_unique<char[]>(limit.rlim_cur * 2);
	auto p = v_stack_buffer.get() + limit.rlim_cur;
	v_stack_last_top = v_stack_last_bottom = reinterpret_cast<t_object**>(p);
	v_stack_copy = reinterpret_cast<t_object**>(p + limit.rlim_cur);
#ifdef IL2CXX__STACK_SCAN_PARTIAL
	union
	{
		t_frame** pp;
		uint8_t pb[8];
	};
	pp = &v_stack_preserved;
	uint8_t thunk[32] = {
		//movabs $0x0,%rdi
		// 0: 48 bf 00 00 00 00 00 00 00 00
		0x48, 0xbf, pb[0], pb[1], pb[2], pb[3], pb[4], pb[5], pb[6], pb[7],
		//lea 0xf(%rip),%rsi        # 0x20
		// a: 48 8d 35 0f 00 00 00
		0x48, 0x8d, 0x35, 0x0f, 0x00, 0x00, 0x00,
		//mov %rsi,(%rdi)
		//11: 48 89 37
		0x48, 0x89, 0x37,
		//movabs $0x0,%rdi
		//14: 48 bf 00 00 00 00 00 00 00 00
		0x48, 0xbf, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		//jmpq *%rdi
		//1e: ff e7
		0xff, 0xe7
	};
	auto page = sysconf(_SC_PAGESIZE);
	v_stack_frames = static_cast<t_frame*>(mmap(NULL, page, PROT_EXEC | PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0));
	v_stack_preserved = v_stack_frames + page / sizeof(t_frame) - 1;
	for (auto p = v_stack_frames; p <= v_stack_preserved; ++p) std::memcpy(p->v_thunk, thunk, sizeof(thunk));
#endif
}

void t_thread::f_initialize(void* a_bottom)
{
#if WIN32
	v_handle = GetCurrentThread();
#else
	v_handle = pthread_self();
#endif
	v_stack_bottom = reinterpret_cast<t_object**>(a_bottom);
	auto page = sysconf(_SC_PAGESIZE);
	rlimit limit;
	if (getrlimit(RLIMIT_STACK, &limit) == -1) throw std::system_error(errno, std::generic_category());
	v_stack_limit = reinterpret_cast<void*>(reinterpret_cast<uintptr_t>(a_bottom) / page * page + page - limit.rlim_cur);
#ifdef IL2CXX__STACK_SCAN_PARTIAL
	v_stack_preserved->v_base = v_stack_bottom;
	v_stack_dirty = v_stack_limit;
#endif
	v_current = this;
	t_slot::t_increments::v_instance = &v_increments;
	t_slot::t_increments::v_head = v_increments.v_objects;
	t_slot::t_increments::v_next = v_increments.v_objects + t_slot::t_increments::V_SIZE / 8;
	t_slot::t_decrements::v_instance = &v_decrements;
	t_slot::t_decrements::v_head = v_decrements.v_objects;
	t_slot::t_decrements::v_next = v_decrements.v_objects + t_slot::t_decrements::V_SIZE / 8;
	v_done = 0;
}

#ifdef IL2CXX__STACK_SCAN_PARTIAL
void f_dump(unw_cursor_t& a_cursor)
{
	char cs[1024];
	unw_word_t offset;
	unw_get_proc_name(&a_cursor, cs, sizeof(cs), &offset);
	unw_word_t ip;
	unw_get_reg(&a_cursor, UNW_REG_IP, &ip);
	unw_word_t* sp;
	unw_get_reg(&a_cursor, UNW_REG_SP, reinterpret_cast<unw_word_t*>(&sp));
	std::fprintf(stderr, "%s%s%s\n\tip(%p), sp(%p), sp[-1](%p)\n", unw_is_signal_frame(&a_cursor) ? "SIGNAL " : "", cs, ip == sp[-1] ? "" : " !!!!", ip, sp, sp[-1]);
}

void t_thread::f_thunk(unw_cursor_t& a_cursor)
{
	auto frame = v_stack_frames;
	while (true) {
		if (frame >= v_stack_preserved) throw std::length_error("frame");
		if (unw_step(&a_cursor) <= 0) throw std::system_error(errno, std::generic_category());
#ifdef IL2CXX__STACK_SCAN_PARTIAL_DUMP
		f_dump(a_cursor);
#endif
		unw_get_reg(&a_cursor, UNW_REG_SP, reinterpret_cast<unw_word_t*>(&frame->v_base));
		if (--frame->v_base >= v_stack_preserved->v_base) break;
		void* ip;
		unw_get_reg(&a_cursor, UNW_REG_IP, reinterpret_cast<unw_word_t*>(&ip));
		if (ip != *frame->v_base) return;
		++frame;
	}
	while (frame > v_stack_frames) {
		(--v_stack_preserved)->v_base = (--frame)->v_base;
		std::memcpy(v_stack_preserved->f_return_address(), v_stack_preserved->v_base, sizeof(void*));
		*reinterpret_cast<void**>(v_stack_preserved->v_base) = v_stack_preserved->v_thunk;
	}
}

void t_thread::f_unthunk(unw_cursor_t& a_cursor)
{
	void* ip;
	if (unw_get_reg(&a_cursor, UNW_REG_IP, reinterpret_cast<unw_word_t*>(&ip)) < 0 || ip != v_stack_preserved->v_thunk) return;
	if (unw_set_reg(&a_cursor, UNW_REG_IP, *reinterpret_cast<unw_word_t*>(v_stack_preserved->f_return_address())) < 0) return;
	++v_stack_preserved;
}
#endif

void t_thread::f_epoch()
{
	t_object** top0;
	t_object** bottom0;
	auto top1 = v_stack_last_bottom;
	if (v_done > 0) {
		++v_done;
		top0 = bottom0 = nullptr;
	} else {
		f_epoch_suspend();
#ifdef IL2CXX__STACK_SCAN_PARTIAL
#ifdef IL2CXX__STACK_SCAN_PARTIAL_DUMP
		std::fprintf(stderr, "THREAD(%p), PRESERVED(%p)\n", this, v_stack_preserved->v_base);
#endif
		unw_cursor_t cursor;
		unw_init_local2(&cursor, &v_unw_context, UNW_INIT_SIGNAL_FRAME);
		if (unw_step(&cursor) <= 0) throw std::system_error(errno, std::generic_category());
#ifdef IL2CXX__STACK_SCAN_PARTIAL_DUMP
		f_dump(cursor);
#endif
		t_object** top;
		do {
			unw_get_reg(&cursor, UNW_REG_SP, reinterpret_cast<unw_word_t*>(&top));
			if (unw_step(&cursor) <= 0) throw std::system_error(errno, std::generic_category());
#ifdef IL2CXX__STACK_SCAN_PARTIAL_DUMP
			f_dump(cursor);
#endif
		} while (unw_is_signal_frame(&cursor) <= 0);
		auto bottom = std::max(v_stack_preserved->v_base, reinterpret_cast<t_object**>(reinterpret_cast<uintptr_t>(v_stack_dirty) / sizeof(t_object*) * sizeof(t_object*)));
		v_stack_dirty = top;
		if (!v_unwinding.load(std::memory_order_relaxed) && !f_engine()->v_shuttingdown) {
			void* ip;
			unw_get_reg(&cursor, UNW_REG_IP, reinterpret_cast<unw_word_t*>(&ip));
			if (ip < v_stack_frames->v_thunk || ip >= v_stack_preserved + 1) f_thunk(cursor);
		}
#else
		auto top = v_stack_top;
		auto bottom = v_stack_bottom;
#endif
		auto n = v_stack_bottom - top;
#ifdef IL2CXX__STACK_SCAN_DIRECT
		top0 = top;
		bottom0 = bottom;
#else
		top0 = v_stack_copy - n;
		auto m = bottom - top;
		std::memcpy(top0, top, m * sizeof(t_object**));
		bottom0 = top0 + m;
		f_epoch_resume();
#endif
		top1 -= n;
	}
	auto decrements = v_stack_last_bottom;
	{
		auto top2 = v_stack_last_top;
		v_stack_last_top = top1;
		std::lock_guard<std::mutex> lock(f_engine()->v_object__heap.f_mutex());
		if (top1 < top2) {
			do {
				auto p = f_engine()->f_object__find(*top0++);
				if (p) p->f_increment();
				*top1++ = p;
			} while (top1 < top2);
		} else {
			for (; top2 < top1; ++top2) if (*top2) *decrements++ = *top2;
		}
		for (; top0 < bottom0; ++top1) {
			auto p = *top0++;
			auto q = *top1;
			if (p == q) continue;
			p = f_engine()->f_object__find(p);
			if (p == q) continue;
			if (p) p->f_increment();
			if (q) *decrements++ = q;
			*top1 = p;
		}
	}
#ifdef IL2CXX__STACK_SCAN_DIRECT
	if (v_done <= 0) f_epoch_resume();
#endif
	v_increments.f_flush();
	for (auto p = v_stack_last_bottom; p != decrements; ++p) (*p)->f_decrement();
	v_decrements.f_flush();
}

IL2CXX__PORTABLE__THREAD t_System_2eThreading_2eThread* t_System_2eThreading_2eThread::v__current;

template<typename T>
void t_System_2eThreading_2eThread::f__start(T a_main)
{
	{
		std::lock_guard<std::mutex> lock(f_engine()->v_thread__mutex);
		if (v__internal) throw std::runtime_error("already started.");
		v__internal = new t_thread();
		v__internal->v_next = f_engine()->v_thread__internals;
		f_engine()->v_thread__internals = v__internal;
	}
	t_slot::t_increments::f_push(this);
	try {
		std::thread([this, main = std::move(a_main)]
		{
			auto internal = v__internal;
			t_slot::v_collector = internal->v_collector;
			{
				std::lock_guard<std::mutex> lock(f_engine()->v_thread__mutex);
				internal->f_initialize(&internal);
			}
			v__current = this;
			try {
				t_thread_static ts;
				main();
			} catch (...) {
			}
			f_engine()->f_object__return();
			{
				std::unique_lock<std::mutex> lock(f_engine()->v_thread__mutex);
				v__internal = nullptr;
			}
			t_slot::t_decrements::f_push(this);
			internal->f_epoch_get();
			std::unique_lock<std::mutex> lock(f_engine()->v_thread__mutex);
			++internal->v_done;
			f_engine()->v_thread__condition.notify_all();
		}).detach();
	} catch (...) {
		{
			std::lock_guard<std::mutex> lock(f_engine()->v_thread__mutex);
			v__internal->v_done = 1;
			v__internal = nullptr;
		}
		t_slot::t_decrements::f_push(this);
		throw;
	}
}

void t_System_2eThreading_2eThread::f__start()
{
	f__start([this]
	{
		if (v__5fdelegate->f_type()->f__is(&t__type_of<t_System_2eThreading_2eThreadStart>::v__instance))
			f_t_System_2eThreading_2eThreadStart__Invoke(static_cast<t_System_2eThreading_2eThreadStart*>(v__5fdelegate));
		else
			f_t_System_2eThreading_2eParameterizedThreadStart__Invoke(static_cast<t_System_2eThreading_2eParameterizedThreadStart*>(v__5fdelegate), v__5fthreadStartArg);
	});
}

void t_System_2eThreading_2eThread::f__join()
{
	if (this == v__current) throw std::runtime_error("current thread can not be joined.");
	if (this == f_engine()->v_thread) throw std::runtime_error("engine thread can not be joined.");
	std::unique_lock<std::mutex> lock(f_engine()->v_thread__mutex);
	while (v__internal) f_engine()->v_thread__condition.wait(lock);
}

}
