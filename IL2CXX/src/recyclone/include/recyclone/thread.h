#ifndef RECYCLONE__THREAD_H
#define RECYCLONE__THREAD_H

//#define RECYCLONE__STACK_SCAN_PARTIAL

#include "object.h"
#include <thread>
#include <unistd.h>
#ifdef RECYCLONE__STACK_SCAN_PARTIAL
#define UNW_LOCAL_ONLY
#include <libunwind.h>
#endif

namespace recyclone
{

class t_engine;

struct t_thread
{
#ifdef RECYCLONE__STACK_SCAN_PARTIAL
	struct t_frame
	{
		t_object** v_base;
		uint8_t v_thunk[32];
		uint8_t* f_return_address()
		{
			return v_thunk + 22;
		}
	};
#endif

	static RECYCLONE__THREAD t_thread* v_current;

	t_thread* v_next;
	int v_done = -1;
	t_slot::t_increments v_increments;
	t_slot::t_decrements v_decrements;
#if WIN32
	HANDLE v_handle;
#else
	pthread_t v_handle;
#endif
	std::unique_ptr<char[]> v_stack_buffer;
	t_object** v_stack_last_top;
	t_object** v_stack_last_bottom;
	t_object** v_stack_copy;
	t_object** v_stack_bottom;
	void* v_stack_limit;
#ifdef RECYCLONE__STACK_SCAN_PARTIAL
	t_frame* v_stack_frames;
	t_frame* v_stack_preserved;
	unw_context_t v_unw_context;
	void* v_stack_dirty;
	std::atomic_bool v_unwinding = false;
#else
	t_object** v_stack_top;
#endif
	t_object* volatile* v_reviving = nullptr;
	bool v_background = false;

	t_thread();
#ifdef RECYCLONE__STACK_SCAN_PARTIAL
	~t_thread()
	{
		munmap(v_stack_frames, sysconf(_SC_PAGESIZE));
	}
#endif
	void f_initialize(void* a_bottom);
#ifdef RECYCLONE__STACK_SCAN_PARTIAL
	void f_thunk(unw_cursor_t& a_cursor);
	void f_unthunk(unw_cursor_t& a_cursor);
#endif
	void f_epoch_get()
	{
#ifdef RECYCLONE__STACK_SCAN_PARTIAL
		unw_getcontext(&v_unw_context);
#else
		t_object* dummy = nullptr;
		v_stack_top = &dummy;
#endif
		v_increments.v_epoch.store(v_increments.v_head, std::memory_order_release);
		v_decrements.v_epoch.store(v_decrements.v_head, std::memory_order_release);
	}
	void f_epoch_suspend();
	void f_epoch_resume();
	void f_epoch();
	void f_revive()
	{
		v_reviving = v_increments.v_head;
	}
	template<typename T>
	static RECYCLONE__ALWAYS_INLINE void f_assign(T*& a_field, T* a_value)
	{
		reinterpret_cast<t_slot&>(a_field) = a_value;
	}
	template<typename T_field, typename T_value>
	static RECYCLONE__ALWAYS_INLINE void f_assign(T_field& a_field, T_value&& a_value)
	{
		a_field = std::forward<T_value>(a_value);
	}
	template<typename T_field, typename T_value>
	RECYCLONE__ALWAYS_INLINE void f_store(T_field& a_field, T_value&& a_value)
	{
		auto p = &a_field;
		if (p >= v_stack_limit && p < static_cast<void*>(v_stack_bottom)) {
			std::memcpy(p, &a_value, sizeof(T_field));
#ifdef RECYCLONE__STACK_SCAN_PARTIAL
			std::atomic_signal_fence(std::memory_order_release);
			if (++p > v_stack_dirty) v_stack_dirty = p;
#endif
		} else {
			f_assign(a_field, std::forward<T_value>(a_value));
		}
	}
	template<typename T>
	T* f_exchange(T*& a_target, T* a_desired)
	{
                auto p = reinterpret_cast<std::atomic<T*>*>(&a_target);
		if (p >= v_stack_limit && p < static_cast<void*>(v_stack_bottom)) {
			a_desired = p->exchange(a_desired, std::memory_order_relaxed);
#ifdef RECYCLONE__STACK_SCAN_PARTIAL
			std::atomic_signal_fence(std::memory_order_release);
			if (++p > v_stack_dirty) v_stack_dirty = p;
#endif
		} else {
			if (a_desired) v_increments.f_push(a_desired);
			a_desired = p->exchange(a_desired, std::memory_order_relaxed);
			if (a_desired) v_decrements.f_push(a_desired);
		}
		return a_desired;
	}
	template<typename T>
	bool f_compare_exchange(T*& a_target, T*& a_expected, T* a_desired)
	{
                auto p = reinterpret_cast<std::atomic<T*>*>(&a_target);
		if (p >= v_stack_limit && p < static_cast<void*>(v_stack_bottom)) {
			if (p->compare_exchange_strong(a_expected, a_desired)) {
#ifdef RECYCLONE__STACK_SCAN_PARTIAL
				if (++p > v_stack_dirty) v_stack_dirty = p;
#endif
				return true;
			}
		} else {
			if (a_desired) v_increments.f_push(a_desired);
			if (p->compare_exchange_strong(a_expected, a_desired)) {
				if (a_expected) v_decrements.f_push(a_expected);
				return true;
			}
			if (a_desired) v_decrements.f_push(a_desired);
		}
		return false;
	}
};

template<typename T_field, typename T_value>
RECYCLONE__ALWAYS_INLINE inline void f__store(T_field& a_field, T_value&& a_value)
{
	t_thread::v_current->f_store(a_field, std::forward<T_value>(a_value));
}

template<typename T>
inline T* f__exchange(T*& a_target, T* a_desired)
{
	return t_thread::v_current->f_exchange(a_target, a_desired);
}

template<typename T>
inline bool f__compare_exchange(T*& a_target, T*& a_expected, T* a_desired)
{
	return t_thread::v_current->f_compare_exchange(a_target, a_expected, a_desired);
}

}

#endif
