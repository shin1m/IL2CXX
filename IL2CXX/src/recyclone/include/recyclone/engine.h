#ifndef RECYCLONE__ENGINE_H
#define RECYCLONE__ENGINE_H

#include "thread.h"
#include <deque>
#include <csignal>
#include <semaphore.h>

namespace recyclone
{

class t_engine
{
	friend class t_object;
	friend class t_weak_pointer;
	friend struct t_thread;
	friend t_engine* f_engine();

protected:
	struct t_conductor
	{
		bool v_running = true;
		bool v_quitting = false;
		std::mutex v_mutex;
		std::condition_variable v_wake;
		std::condition_variable v_done;

		void f_next(std::unique_lock<std::mutex>& a_lock)
		{
			v_running = false;
			v_done.notify_all();
			do v_wake.wait(a_lock); while (!v_running);
		}
		void f_exit()
		{
			std::lock_guard lock(v_mutex);
			v_running = false;
			v_done.notify_one();
		}
		void f_wake()
		{
			if (v_running) return;
			v_running = true;
			v_wake.notify_one();
		}
		void f_wait(std::unique_lock<std::mutex>& a_lock)
		{
			do v_done.wait(a_lock); while (v_running);
		}
		void f_quit()
		{
			std::unique_lock lock(v_mutex);
			v_running = v_quitting = true;
			v_wake.notify_one();
			f_wait(lock);
		}
	};

	static RECYCLONE__THREAD t_engine* v_instance;

	t_conductor v_collector__conductor;
	size_t v_collector__threshold;
	size_t v_collector__tick = 0;
	size_t v_collector__wait = 0;
	size_t v_collector__epoch = 0;
	size_t v_collector__collect = 0;
	size_t v_collector__full = 0;
	t_heap<t_object> v_object__heap;
	size_t v_object__lower = 0;
	bool v_object__reviving = false;
	std::mutex v_object__reviving__mutex;
	size_t v_object__release = 0;
	size_t v_object__collect = 0;
	sem_t v_epoch__received;
	sigset_t v_epoch__notsigusr2;
	struct sigaction v_epoch__old_sigusr1;
	struct sigaction v_epoch__old_sigusr2;
	t_thread* v_thread__head = nullptr;
	t_thread* v_thread__main;
	t_thread* v_thread__finalizer = nullptr;
	std::mutex v_thread__mutex;
	std::condition_variable v_thread__condition;
	t_conductor v_finalizer__conductor;
	std::deque<t_object*> v_finalizer__queue;
	bool v_finalizer__sleeping = false;
	uint8_t v_finalizer__awaken = 0;
	bool v_exiting = false;

	void f_object__return()
	{
		v_object__heap.f_return();
	}
	void f_free(t_object* a_p)
	{
		a_p->v_count = 1;
		v_object__heap.f_free(a_p);
	}
	void f_free_as_release(t_object* a_p)
	{
		++v_object__release;
		f_free(a_p);
	}
	void f_free_as_collect(t_object* a_p)
	{
		++v_object__collect;
		f_free(a_p);
	}
	t_object* f_object__find(void* a_p)
	{
		if (reinterpret_cast<uintptr_t>(a_p) & 127) return nullptr;
		auto p = v_object__heap.f_find(a_p);
		return p && p->v_type ? p : nullptr;
	}
	void f_epoch_suspend()
	{
		if (sem_post(&v_epoch__received) == -1) _exit(errno);
		sigsuspend(&v_epoch__notsigusr2);
		if (sem_post(&v_epoch__received) == -1) _exit(errno);
	}
	void f_epoch_send(pthread_t a_thread, int a_signal)
	{
		pthread_kill(a_thread, a_signal);
		while (sem_wait(&v_epoch__received) == -1) if (errno != EINTR) throw std::system_error(errno, std::generic_category());
	}
	void f_collector();
	void f_finalizer(void(*a_finalize)(t_object*));
	size_t f_statistics();

public:
	struct t_options
	{
#ifdef NDEBUG
		size_t v_collector__threshold = 1024 * 64;
#else
		size_t v_collector__threshold = 64;
#endif
		bool v_verbose = false;
		bool v_verify = false;
	};

	const t_options& v_options;

	t_engine(const t_options& a_options);
	~t_engine();
	int f_exit(int a_code);
	void f_tick()
	{
		if (v_collector__conductor.v_running) return;
		std::lock_guard lock(v_collector__conductor.v_mutex);
		++v_collector__tick;
		v_collector__conductor.f_wake();
	}
	void f_wait()
	{
		std::unique_lock lock(v_collector__conductor.v_mutex);
		++v_collector__wait;
		v_collector__conductor.f_wake();
		v_collector__conductor.f_wait(lock);
	}
	RECYCLONE__ALWAYS_INLINE constexpr t_object* f_object__allocate(size_t a_size)
	{
		auto p = v_object__heap.f_allocate(a_size);
		p->v_next = nullptr;
		return p;
	}
	void f_collect();
	void f_finalize();
	size_t f_load_count() const
	{
		size_t n = 0;
		v_object__heap.f_statistics([&](auto, auto, auto a_allocated, auto a_freed)
		{
			n += a_allocated - a_freed;
		});
		return n;
	}
	bool f_exiting() const
	{
		return v_exiting;
	}
};

inline t_engine* f_engine()
{
	return t_engine::v_instance;
}

template<size_t A_SIZE>
void t_slot::t_queue<A_SIZE>::f_next() noexcept
{
	f_engine()->f_tick();
	if (v_head < v_objects + V_SIZE - 1) {
		++v_head;
		while (v_tail == v_head) f_engine()->f_wait();
		auto tail = v_tail;
		v_next = std::min(tail < v_head ? v_objects + V_SIZE - 1 : tail - 1, v_head + V_SIZE / 8);
	} else {
		v_head = v_objects;
		while (v_tail == v_head) f_engine()->f_wait();
		v_next = std::min(v_tail - 1, v_head + V_SIZE / 8);
	}
}

template<void (t_object::*A_push)()>
inline void t_object::f_step()
{
	v_type->f_scan(this, f_push<A_push>);
	//(v_type->*A_push)();
	if (auto p = v_extension.load(std::memory_order_consume)) p->f_scan(f_push<A_push>);
}

inline void t_object::f_decrement_step()
{
	if (auto p = v_extension.load(std::memory_order_consume)) {
		p->f_scan(f_push_and_clear<&t_object::f_decrement_push>);
		v_extension.store(nullptr, std::memory_order_relaxed);
		delete p;
	}
	v_type->f_scan(this, f_push_and_clear<&t_object::f_decrement_push>);
	//v_type->f_decrement_push();
	v_type = nullptr;
	v_color = e_color__BLACK;
	if (v_next) {
		v_next->v_previous = v_previous;
		v_previous->v_next = v_next;
	}
	f_engine()->f_free_as_release(this);
}

inline void t_thread::f_epoch_suspend()
{
#if WIN32
	SuspendThread(v_handle);
	f_epoch_get();
#else
	f_engine()->f_epoch_send(v_handle, SIGUSR1);
#endif
}

inline void t_thread::f_epoch_resume()
{
#if WIN32
	ResumeThread(v_handle);
#else
	f_engine()->f_epoch_send(v_handle, SIGUSR2);
#endif
}

}

#endif
