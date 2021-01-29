#ifndef RECYCLONE__THREAD_H
#define RECYCLONE__THREAD_H

#include "object.h"
#include <thread>
#include <csignal>
#include <unistd.h>
#include <sys/resource.h>

namespace recyclone
{

template<typename T_type>
struct t_thread
{
	static inline RECYCLONE__THREAD t_thread* v_current;

	t_thread* v_next;
	int v_done = -1;
	typename t_slot<T_type>::t_increments v_increments;
	typename t_slot<T_type>::t_decrements v_decrements;
#if WIN32
	HANDLE v_handle;
#else
	pthread_t v_handle;
#endif
	std::unique_ptr<char[]> v_stack_buffer;
	t_object<T_type>** v_stack_last_top;
	t_object<T_type>** v_stack_last_bottom;
	t_object<T_type>** v_stack_copy;
	t_object<T_type>** v_stack_bottom;
	void* v_stack_limit;
	t_object<T_type>** v_stack_top;
	t_object<T_type>* volatile* v_reviving = nullptr;
	bool v_background = false;

	t_thread();
	void f_initialize(void* a_bottom);
	void f_epoch_get()
	{
		t_object<T_type>* dummy = nullptr;
		v_stack_top = &dummy;
		v_increments.v_epoch.store(v_increments.v_head, std::memory_order_release);
		v_decrements.v_epoch.store(v_decrements.v_head, std::memory_order_release);
	}
	void f_epoch_suspend()
	{
#if WIN32
		SuspendThread(v_handle);
		f_epoch_get();
#else
		f_engine<T_type>()->f_epoch_send(v_handle, SIGUSR1);
#endif
	}
	void f_epoch_resume()
	{
#if WIN32
		ResumeThread(v_handle);
#else
		f_engine<T_type>()->f_epoch_send(v_handle, SIGUSR2);
#endif
	}
	void f_epoch();
	void f_revive()
	{
		v_reviving = v_increments.v_head;
	}
	template<typename T>
	static RECYCLONE__ALWAYS_INLINE void f_assign(T*& a_field, T* a_value)
	{
		reinterpret_cast<t_slot<T_type>&>(a_field) = a_value;
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
		if (p >= v_stack_limit && p < static_cast<void*>(v_stack_bottom))
			std::memcpy(p, &a_value, sizeof(T_field));
		else
			f_assign(a_field, std::forward<T_value>(a_value));
	}
	template<typename T>
	T* f_exchange(T*& a_target, T* a_desired)
	{
                auto p = reinterpret_cast<std::atomic<T*>*>(&a_target);
		if (p >= v_stack_limit && p < static_cast<void*>(v_stack_bottom)) {
			a_desired = p->exchange(a_desired, std::memory_order_relaxed);
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
			return p->compare_exchange_strong(a_expected, a_desired);
		} else {
			if (a_desired) v_increments.f_push(a_desired);
			if (p->compare_exchange_strong(a_expected, a_desired)) {
				if (a_expected) v_decrements.f_push(a_expected);
				return true;
			}
			if (a_desired) v_decrements.f_push(a_desired);
			return false;
		}
	}
};

template<typename T_type>
t_thread<T_type>::t_thread() : v_next(f_engine<T_type>()->v_thread__head)
{
	if (f_engine<T_type>()->v_exiting) throw std::runtime_error("engine is exiting.");
	rlimit limit;
	if (getrlimit(RLIMIT_STACK, &limit) == -1) throw std::system_error(errno, std::generic_category());
	v_stack_buffer = std::make_unique<char[]>(limit.rlim_cur * 2);
	auto p = v_stack_buffer.get() + limit.rlim_cur;
	v_stack_last_top = v_stack_last_bottom = reinterpret_cast<t_object<T_type>**>(p);
	v_stack_copy = reinterpret_cast<t_object<T_type>**>(p + limit.rlim_cur);
	f_engine<T_type>()->v_thread__head = this;
}

template<typename T_type>
void t_thread<T_type>::f_initialize(void* a_bottom)
{
#if WIN32
	v_handle = GetCurrentThread();
#else
	v_handle = pthread_self();
#endif
	v_stack_bottom = reinterpret_cast<t_object<T_type>**>(a_bottom);
	auto page = sysconf(_SC_PAGESIZE);
	rlimit limit;
	if (getrlimit(RLIMIT_STACK, &limit) == -1) throw std::system_error(errno, std::generic_category());
	v_stack_limit = reinterpret_cast<void*>(reinterpret_cast<uintptr_t>(a_bottom) / page * page + page - limit.rlim_cur);
	v_current = this;
	t_slot<T_type>::t_increments::v_instance = &v_increments;
	t_slot<T_type>::t_increments::v_head = v_increments.v_objects;
	t_slot<T_type>::t_increments::v_next = v_increments.v_objects + t_slot<T_type>::t_increments::V_SIZE / 8;
	t_slot<T_type>::t_decrements::v_instance = &v_decrements;
	t_slot<T_type>::t_decrements::v_head = v_decrements.v_objects;
	t_slot<T_type>::t_decrements::v_next = v_decrements.v_objects + t_slot<T_type>::t_decrements::V_SIZE / 8;
	v_done = 0;
}

template<typename T_type>
void t_thread<T_type>::f_epoch()
{
	t_object<T_type>** top0;
	t_object<T_type>** bottom0;
	auto top1 = v_stack_last_bottom;
	if (v_done > 0) {
		++v_done;
		top0 = bottom0 = nullptr;
	} else {
		f_epoch_suspend();
		auto n = v_stack_bottom - v_stack_top;
		top0 = v_stack_copy - n;
		std::memcpy(top0, v_stack_top, n * sizeof(t_object<T_type>**));
		f_epoch_resume();
		bottom0 = top0 + n;
		top1 -= n;
	}
	auto decrements = v_stack_last_bottom;
	{
		auto top2 = v_stack_last_top;
		v_stack_last_top = top1;
		std::lock_guard lock(f_engine<T_type>()->v_object__heap.f_mutex());
		if (top1 < top2) {
			do {
				auto p = f_engine<T_type>()->f_object__find(*top0++);
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
			p = f_engine<T_type>()->f_object__find(p);
			if (p == q) continue;
			if (p) p->f_increment();
			if (q) *decrements++ = q;
			*top1 = p;
		}
	}
	v_increments.f_flush();
	for (auto p = v_stack_last_bottom; p != decrements; ++p) (*p)->f_decrement();
	v_decrements.f_flush();
}

}

#endif
