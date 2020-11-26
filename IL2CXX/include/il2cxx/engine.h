#ifndef IL2CXX__ENGINE_H
#define IL2CXX__ENGINE_H

#include "thread.h"
#include "type.h"
#include <algorithm>
#include <deque>
#include <climits>
#include <csignal>
#include <cuchar>
#include <semaphore.h>

namespace il2cxx
{

class t_engine : public t_slot::t_collector
{
	friend class t_object;
	friend class t__weak_handle;
	friend class t_thread;
	friend struct t__thread;

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

private:
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
	t_thread* v_thread__internals = new t_thread();
	std::mutex v_thread__mutex;
	std::condition_variable v_thread__condition;
	t_slot_of<t__thread> v_thread{};
	t_conductor v_finalizer__conductor;
	std::deque<t_object*> v_finalizer__queue;
	bool v_finalizer__sleeping = false;
	uint8_t v_finalizer__awaken = 0;
	const t_options& v_options;
	size_t v_collector__threshold;
	size_t v_collector__full = 0;
	bool v_shuttingdown = false;

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
	void f_wait_foreground_threads();
	void f_shutdown();
	size_t f_statistics();

public:
	t_engine(const t_options& a_options);
	~t_engine();
	template<typename T_thread, typename T_static, typename T_thread_static, typename T_main>
	int f_run(void(*a_finalize)(t_object*), T_main a_main);
	IL2CXX__PORTABLE__ALWAYS_INLINE constexpr t_object* f_object__allocate(size_t a_size)
	{
		auto p = v_object__heap.f_allocate(a_size);
		p->v_next = nullptr;
		return p;
	}
	bool f_shuttingdown() const
	{
		return v_shuttingdown;
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
};

inline t_engine* f_engine()
{
	return static_cast<t_engine*>(t_slot::v_collector);
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

template<typename T>
void t__thread::f__start(T a_main)
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
				if (v__background) {
					internal->v_background = true;
					f_engine()->v_thread__condition.notify_all();
				}
				f__priority(internal->v_handle, v__priority);
			}
			v__current = this;
			try {
				main();
			} catch (t_object*) {
			}
			f_engine()->f_object__return();
			{
				std::lock_guard<std::mutex> lock(f_engine()->v_thread__mutex);
				internal->v_background = false;
				v__internal = nullptr;
			}
			t_slot::t_decrements::f_push(this);
			internal->f_epoch_get();
			std::lock_guard<std::mutex> lock(f_engine()->v_thread__mutex);
			++internal->v_done;
			f_engine()->v_thread__condition.notify_all();
		}).detach();
	} catch (...) {
		{
			std::lock_guard<std::mutex> lock(f_engine()->v_thread__mutex);
			v__internal->v_done = 1;
			v__internal = nullptr;
			f_engine()->v_thread__condition.notify_all();
		}
		t_slot::t_decrements::f_push(this);
		throw;
	}
}

template<typename T>
struct t__new
{
	T* v_p;

	IL2CXX__PORTABLE__ALWAYS_INLINE constexpr t__new(size_t a_extra) : v_p(static_cast<T*>(f_engine()->f_object__allocate(sizeof(T) + a_extra)))
	{
	}
	IL2CXX__PORTABLE__ALWAYS_INLINE ~t__new()
	{
		t__type_of<T>::v__instance.f__finish(v_p);
	}
	constexpr operator T*() const
	{
		return v_p;
	}
	constexpr T* operator->() const
	{
		return v_p;
	}
};

template<typename T>
T* f__new_zerod()
{
	t__new<T> p(0);
	std::memset(static_cast<t_object*>(p) + 1, 0, sizeof(T) - sizeof(t_object));
	return p;
}

template<typename T, typename... T_an>
T* f__new_constructed(T_an&&... a_n)
{
	t__new<T> p(0);
	p->f__construct(std::forward<T_an>(a_n)...);
	return p;
}

template<typename T_array, typename T_element>
T_array* f__new_array(size_t a_length)
{
	t__new<T_array> p(sizeof(T_element) * a_length);
	p->v__length = a_length;
	p->v__bounds[0] = {a_length, 0};
	std::memset(p->f__data(), 0, sizeof(T_element) * a_length);
	return p;
}

template<typename T0, typename T1>
inline T1 f__copy(T0 a_in, size_t a_n, T1 a_out)
{
	return a_in < a_out ? std::copy_backward(a_in, a_in + a_n, a_out + a_n) : std::copy_n(a_in, a_n, a_out);
}

template<typename T0, typename T1>
inline T1 f__move(T0 a_in, size_t a_n, T1 a_out)
{
	return a_in < a_out ? std::move_backward(a_in, a_in + a_n, a_out + a_n) : std::move(a_in, a_in + a_n, a_out);
}

template<typename T_thread, typename T_static, typename T_thread_static, typename T_main>
int t_engine::f_run(void(*a_finalize)(t_object*), T_main a_main)
{
	v_thread__internals->f_initialize(this);
	{
		std::unique_lock<std::mutex> lock(v_collector__conductor.v_mutex);
		std::thread(&t_engine::f_collector, this).detach();
		v_collector__conductor.f__wait(lock);
	}
	v_thread = f__new_zerod<T_thread>();
	v_thread->v__internal = v_thread__internals;
	t__thread::v__current = v_thread;
	{
		auto finalizer = f__new_zerod<T_thread>();
		std::unique_lock<std::mutex> lock(v_finalizer__conductor.v_mutex);
		finalizer->f__start([this, a_finalize]
		{
			T_thread_static ts;
			f_finalizer(a_finalize);
		});
		v_finalizer__conductor.f__wait(lock);
	}
	auto s = std::make_unique<T_static>();
	auto ts = std::make_unique<T_thread_static>();
	auto n = a_main();
	f_wait_foreground_threads();
	if (v_options.v_verify) {
		f_shutdown();
		return n;
	} else {
		if (v_options.v_verbose) f_statistics();
		std::exit(n);
	}
}

template<typename T_push>
void f__to_u16(const char* a_first, const char* a_last, T_push a_push)
{
	std::mbstate_t state{};
	char16_t c;
	while (a_first < a_last) {
		auto n = std::mbrtoc16(&c, a_first, a_last - a_first, &state);
		switch (n) {
		case size_t(-3):
			a_push(c);
			break;
		case size_t(-2):
			a_first = a_last;
			break;
		case size_t(-1):
			++a_first;
			break;
		case 0:
			a_push(u'\0');
			++a_first;
			break;
		default:
			a_push(c);
			a_first += n;
			break;
		}
	}
	if (std::mbrtoc16(&c, a_first, 0, &state) == size_t(-3)) a_push(c);
}

std::u16string f__u16string(std::string_view a_x);
std::string f__string(std::u16string_view a_x);

}

#endif
