#include <il2cxx/thread.h>
#include <sys/resource.h>

extern "C" int _Unwind_RaiseException(void* exception_object)
{
	static auto original = reinterpret_cast<int(*)(void*)>(dlsym(RTLD_NEXT, "_Unwind_RaiseException"));
	if (il2cxx::t_thread::v_current) il2cxx::t_thread::v_current->f_unthunk();
	return original(exception_object);
}

extern "C" void _Unwind_Resume(void* exception_object)
{
	static auto original = reinterpret_cast<void(*)(void*)>(dlsym(RTLD_NEXT, "_Unwind_Resume"));
	if (il2cxx::t_thread::v_current) il2cxx::t_thread::v_current->f_unthunk();
	original(exception_object);
}

extern "C" void* __cxa_begin_catch(void* exceptionObject)
{
	if (il2cxx::t_thread::v_current) il2cxx::t_thread::v_current->f_rethunk();
	static auto original = reinterpret_cast<void*(*)(void*)>(dlsym(RTLD_NEXT, "__cxa_begin_catch"));
	return original(exceptionObject);
}

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
	v_stack_current = reinterpret_cast<t_object**>(p + limit.rlim_cur);
	union
	{
		t_frame** pp;
		uint8_t pb[8];
	};
	pp = &v_stack_preserved;
	uint8_t thunk[32] = {
		//movabs $0x0,%rcx
		// 0: 48 b9 00 00 00 00 00 00 00 00
		0x48, 0xb9, pb[0], pb[1], pb[2], pb[3], pb[4], pb[5], pb[6], pb[7],
		//lea 0xf(%rip),%rdx        # 0x20
		// a: 48 8d 15 0f 00 00 00
		0x48, 0x8d, 0x15, 0x0f, 0x00, 0x00, 0x00,
		//mov %rdx,(%rcx)
		//11: 48 89 11
		0x48, 0x89, 0x11,
		//movabs $0x0,%rcx
		//14: 48 b9 00 00 00 00 00 00 00 00
		0x48, 0xb9, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		//jmpq *%rcx
		//1e: ff e1
		0xff, 0xe1
	};
	auto page = sysconf(_SC_PAGESIZE);
	v_stack_frames = static_cast<t_frame*>(mmap(NULL, page, PROT_EXEC | PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0));
	v_stack_preserved = v_stack_frames + page / sizeof(t_frame) - 1;
	for (auto p = v_stack_frames; p <= v_stack_preserved; ++p) std::memcpy(p->v_thunk, thunk, sizeof(thunk));
}

void t_thread::f_initialize(void* a_bottom)
{
#if WIN32
	v_handle = GetCurrentThread();
#else
	v_handle = pthread_self();
#endif
	v_stack_preserved->v_base = v_stack_bottom = reinterpret_cast<t_object**>(a_bottom);
	auto page = sysconf(_SC_PAGESIZE);
	rlimit limit;
	if (getrlimit(RLIMIT_STACK, &limit) == -1) throw std::system_error(errno, std::generic_category());
	v_stack_dirty = v_stack_limit = reinterpret_cast<void*>(reinterpret_cast<uintptr_t>(a_bottom) / page * page + page - limit.rlim_cur);
	v_current = this;
	t_slot::v_increments = &v_increments;
	t_slot::v_decrements = &v_decrements;
	v_done = 0;
}

void f_dump(unw_cursor_t& a_cursor)
{
	return;
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
		if (unw_step(&a_cursor) <= 0) throw std::system_error(errno, std::generic_category());
		f_dump(a_cursor);
		unw_get_reg(&a_cursor, UNW_REG_SP, reinterpret_cast<unw_word_t*>(&frame->v_base));
		if (--frame->v_base >= v_stack_preserved->v_base) break;
		void* ip;
		unw_get_reg(&a_cursor, UNW_REG_IP, reinterpret_cast<unw_word_t*>(&ip));
		if (ip != *frame->v_base) throw std::domain_error("ip");
		++frame;
	}
	while (frame > v_stack_frames) {
		(--v_stack_preserved)->v_base = (--frame)->v_base;
		std::memcpy(v_stack_preserved->f_return_address(), v_stack_preserved->v_base, sizeof(void*));
		*reinterpret_cast<void**>(v_stack_preserved->v_base) = v_stack_preserved->v_thunk;
	}
}

void t_thread::f_unthunk()
{
	v_throwing.store(true);
	auto bottom = v_stack_frames + sysconf(_SC_PAGESIZE) / sizeof(t_frame) - 1;
	while (v_stack_preserved < bottom) {
		std::memcpy(v_stack_preserved->v_base, v_stack_preserved->f_return_address(), sizeof(void*));
		++v_stack_preserved;
	}
}

void t_thread::f_rethunk()
{
	unw_context_t context;
	unw_getcontext(&context);
	unw_cursor_t cursor;
	unw_init_local(&cursor, &context);
	f_thunk(cursor);
	v_throwing.store(false);
}

void t_thread::f_epoch()
{
	size_t m;
	size_t n;
	if (v_done > 0) {
		++v_done;
		m = n = 0;
	} else {
		f_epoch_suspend();
		//std::fprintf(stderr, "THREAD(%p), PRESERVED(%p)\n", this, v_stack_preserved->v_base);
		unw_cursor_t cursor;
		unw_init_local2(&cursor, &v_unw_context, UNW_INIT_SIGNAL_FRAME);
		if (unw_step(&cursor) <= 0) throw std::system_error(errno, std::generic_category());
		f_dump(cursor);
		t_object** top;
		do {
			unw_get_reg(&cursor, UNW_REG_SP, reinterpret_cast<unw_word_t*>(&top));
			if (unw_step(&cursor) <= 0) throw std::system_error(errno, std::generic_category());
			f_dump(cursor);
		} while (unw_is_signal_frame(&cursor) <= 0);
		m = v_stack_bottom - top;
		if (v_throwing.load()) {
			n = 0;
			std::copy(top, v_stack_bottom, v_stack_current - m);
		} else {
			auto p = std::max(v_stack_preserved->v_base, reinterpret_cast<t_object**>(reinterpret_cast<uintptr_t>(v_stack_dirty) / sizeof(t_object*) * sizeof(t_object*)));
			n = v_stack_bottom - p;
			std::copy(top, p, v_stack_current - m);
			void* ip;
			unw_get_reg(&cursor, UNW_REG_IP, reinterpret_cast<unw_word_t*>(&ip));
			if (ip < v_stack_frames->v_thunk || ip >= v_stack_preserved + 1) f_thunk(cursor);
		}
		v_stack_dirty = top;
		f_epoch_resume();
	}
	auto top0 = v_stack_current - m;
	auto bottom0 = v_stack_current - n;
	auto top1 = v_stack_last_top;
	auto top = v_stack_last_top = v_stack_last_bottom - m;
	std::vector<t_object*> decrements;
	if (top < top1) {
		do {
			auto p = f_engine()->f_find(*top0++);
			if (p) p->f_increment();
			*top++ = p;
		} while (top < top1);
	} else {
		for (; top1 < top; ++top1) if (*top1) decrements.push_back(*top1);
	}
	for (; top0 < bottom0; ++top) {
		auto p = f_engine()->f_find(*top0++);
		if (p == *top) continue;
		if (p) p->f_increment();
		if (*top) decrements.push_back(*top);
		*top = p;
	}
	v_increments.f_flush();
	for (auto p : decrements) p->f_decrement();
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
	t_slot::v_increments->f_push(this);
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
			t_slot::v_decrements->f_push(this);
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
		t_slot::v_decrements->f_push(this);
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
