#ifndef IL2CXX__ENGINE_H
#define IL2CXX__ENGINE_H

#include "thread.h"
#include <deque>
#include <semaphore.h>

namespace il2cxx
{

struct t_safe_region;

class t_engine : public t_slot::t_collector
{
	friend class t_object;
	friend class t__weak_handle;
	friend class t_thread;
	friend struct t_System_2eThreading_2eThread;

public:
	struct t_options
	{
#ifdef NDEBUG
		size_t v_collector__threshold = 1024 * 64;
#else
		size_t v_collector__threshold = 64;
#endif
		bool v_verbose = false;
	};

private:
	t_heap<t_object, void(*)()> v_object__heap;
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
	t_slot_of<t_System_2eThreading_2eThread> v_thread{};
	t_conductor v_finalizer__conductor;
	std::deque<t_object*> v_finalizer__queue;
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
		return p && p->v_type.load(std::memory_order_acquire) ? p : nullptr;
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
	static void f_finalize(t_object* a_p);
	void f_finalizer();

public:
	t_engine(const t_options& a_options, size_t a_count, char** a_arguments);
	~t_engine();
	t_object* f_object__allocate(size_t a_size)
	{
		auto p = v_object__heap.f_allocate(a_size);
		p->v_next = nullptr;
		return p;
	}
	bool f_shuttingdown() const
	{
		return v_shuttingdown;
	}
	void f_shutdown();
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

template<typename T>
inline t__new<T>::t__new(size_t a_extra) : v_p(static_cast<T*>(f_engine()->f_object__allocate(sizeof(T) + a_extra)))
{
}

template<typename T>
inline t__new<T>::~t__new()
{
	t__type_of<T>::v__instance.f__finish(v_p);
}

template<void (t_object::*A_push)()>
inline void t_object::f_step()
{
	f_type()->f_scan(this, f_push<A_push>);
	//(f_type()->*A_push)();
	if (auto p = v_extension.load(std::memory_order_consume)) p->f_scan(f_push<A_push>);
}

inline void t_object::f_decrement_step()
{
	if (auto p = v_extension.load(std::memory_order_consume)) {
		p->f_scan(f_push_and_clear<&t_object::f_decrement_push>);
		v_extension.store(nullptr, std::memory_order_relaxed);
		delete p;
	}
	f_type()->f_scan(this, f_push_and_clear<&t_object::f_decrement_push>);
	//f_type()->f_decrement_push();
	v_type.store(nullptr, std::memory_order_relaxed);
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

inline t__type::t__type(t__type* a_base, std::map<t__type*, void**>&& a_interface_to_methods, bool a_managed, size_t a_size, t__type* a_element, size_t a_rank, void* a_multicast_invoke) : v__base(a_base), v__interface_to_methods(std::move(a_interface_to_methods)), v__managed(a_managed), v__size(a_size), v__element(a_element), v__rank(a_rank), v__multicast_invoke(a_multicast_invoke)
{
	v_type.store(&t__type_of<t__type>::v__instance, std::memory_order_relaxed);
}

template<typename T>
T* f__new_zerod()
{
	t__new<T> p(0);
	std::fill_n(reinterpret_cast<char*>(static_cast<t_object*>(p) + 1), sizeof(T) - sizeof(t_object), '\0');
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
	std::fill_n(reinterpret_cast<char*>(p->f__data()), sizeof(T_element) * a_length, '\0');
	return p;
}

inline t_System_2eString* IL2CXX__PORTABLE__ALWAYS_INLINE f__new_string(size_t a_length)
{
	t__new<t_System_2eString> p(sizeof(char16_t) * a_length);
	p->v__5fstringLength = a_length;
	(&p->v__5ffirstChar)[a_length] = u'\0';
	return p;
}

inline t_System_2eString* f__new_string(std::u16string_view a_value)
{
	auto p = f__new_string(a_value.size());
	std::memcpy(&p->v__5ffirstChar, a_value.data(), a_value.size() * sizeof(char16_t));
	return p;
}

template<typename T>
class t__lazy
{
	std::atomic<T*> v_initialized = nullptr;
	std::recursive_mutex v_mutex;
	T v_p;
	bool v_initializing = false;

	T* f_initialize();

public:
	T* f_get()
	{
		auto p = v_initialized.load(std::memory_order_consume);
		return p ? p : f_initialize();
	}
	T* operator->()
	{
		return f_get();
	}
};

template<typename T>
T* t__lazy<T>::f_initialize()
{
	std::lock_guard<std::recursive_mutex> lock(v_mutex);
	if (v_initializing) return &v_p;
	v_initializing = true;
	v_p.f_initialize();
	v_initialized.store(&v_p, std::memory_order_release);
	return &v_p;
}

}

#endif
