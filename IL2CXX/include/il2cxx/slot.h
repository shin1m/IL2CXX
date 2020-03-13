#ifndef IL2CXX__SLOT_H
#define IL2CXX__SLOT_H

#include "define.h"
#include <atomic>
#include <condition_variable>
#include <mutex>
#include <type_traits>
#include <cassert>
#include <cinttypes>
#include <cstddef>
#include <cstring>

namespace il2cxx
{

struct t__type;
struct t__type_finalizee;
class t_engine;
class t_object;
struct t_thread;
struct t_System_2eThreading_2eThread;
t_engine* f_engine();

struct t_conductor
{
	bool v_running = true;
	bool v_quitting = false;
	std::mutex v_mutex;
	std::condition_variable v_wake;
	std::condition_variable v_done;

	void f__next(std::unique_lock<std::mutex>& a_lock)
	{
		v_running = false;
		v_done.notify_all();
		do v_wake.wait(a_lock); while (!v_running);
	}
	void f_exit()
	{
		std::lock_guard<std::mutex> lock(v_mutex);
		v_running = false;
		v_done.notify_one();
	}
	void f__wake()
	{
		if (v_running) return;
		v_running = true;
		v_wake.notify_one();
	}
	void f__wait(std::unique_lock<std::mutex>& a_lock)
	{
		do v_done.wait(a_lock); while (v_running);
	}
	void f_quit()
	{
		std::unique_lock<std::mutex> lock(v_mutex);
		v_running = v_quitting = true;
		v_wake.notify_one();
		f__wait(lock);
	}
};

template<typename T>
struct t_root : T
{
	t_root() : T(T{})
	{
	}
	t_root(const t_root& a_value) : T(a_value)
	{
	}
	template<typename U>
	t_root(U&& a_value) : T(std::forward<U>(a_value))
	{
	}
	~t_root()
	{
		this->f__destruct();
	}
	t_root& operator=(const t_root& a_value)
	{
		static_cast<T&>(*this) = a_value;
		return *this;
	}
	template<typename U>
	t_root& operator=(U&& a_value)
	{
		static_cast<T&>(*this) = std::forward<U>(a_value);
		return *this;
	}
};

template<typename T>
struct t_stacked : T
{
	t_stacked() = default;
	template<typename U>
	t_stacked(U&& a_value)
	{
		std::memcpy(this, &a_value, sizeof(T));
	}
	t_stacked(const t_stacked& a_value) : t_stacked(static_cast<const T&>(a_value))
	{
	}
	template<typename U>
	t_stacked& operator=(U&& a_value)
	{
		std::memcpy(this, &a_value, sizeof(T));
		return *this;
	}
	t_stacked& operator=(const t_stacked& a_value)
	{
		return *this = static_cast<const T&>(a_value);
	}
};

class t_slot
{
	friend class t__type;
	friend class t__type_finalizee;
	friend class t_engine;
	friend class t_object;
	friend struct t__weak_handle;
	friend struct t_thread;
	friend struct t_System_2eThreading_2eThread;
	friend t_engine* f_engine();

public:
	class t_collector
	{
	protected:
		t_conductor v_collector__conductor;
		size_t v_collector__tick = 0;
		size_t v_collector__wait = 0;
		size_t v_collector__epoch = 0;
		size_t v_collector__collect = 0;

		t_collector()
		{
			v_collector = this;
		}
		~t_collector()
		{
			v_collector = nullptr;
		}

	public:
		void f_tick()
		{
			if (v_collector__conductor.v_running) return;
			std::lock_guard<std::mutex> lock(v_collector__conductor.v_mutex);
			++v_collector__tick;
			v_collector__conductor.f__wake();
		}
		void f_wait()
		{
			std::unique_lock<std::mutex> lock(v_collector__conductor.v_mutex);
			++v_collector__wait;
			v_collector__conductor.f__wake();
			v_collector__conductor.f__wait(lock);
		}
	};

protected:
	template<size_t A_SIZE>
	struct t_queue
	{
		static const size_t V_SIZE = A_SIZE;

		t_object* volatile* v_head{v_objects};
		t_object* volatile* v_next = v_objects + V_SIZE / 2;
		t_object* volatile v_objects[V_SIZE];
		std::atomic<t_object* volatile*> v_epoch;
		t_object* volatile* v_tail{v_objects + V_SIZE - 1};

		void f_next() noexcept;
		IL2CXX__PORTABLE__ALWAYS_INLINE IL2CXX__PORTABLE__FORCE_INLINE void f_push(t_object* a_object)
		{
			*v_head = a_object;
			if (v_head == v_next)
				f_next();
			else
				++v_head;
		}
		template<typename T>
		void f__flush(t_object* volatile* a_epoch, T a_do)
		{
			auto end = v_objects + V_SIZE - 1;
			if (a_epoch > v_objects)
				--a_epoch;
			else
				a_epoch = end;
			while (v_tail != a_epoch) {
				auto next = a_epoch;
				if (v_tail < end) {
					if (next < v_tail) next = end;
					++v_tail;
				} else {
					v_tail = v_objects;
				}
				while (true) {
					a_do(*v_tail);
					if (v_tail == next) break;
					++v_tail;
				}
			}
		}
	};
#ifdef NDEBUG
	struct t_increments : t_queue<16384>
#else
	struct t_increments : t_queue<128>
#endif
	{
		void f_flush()
		{
			f__flush(v_epoch.load(std::memory_order_acquire), [](auto x)
			{
				x->f_increment();
			});
		}
	};
#ifdef NDEBUG
	struct t_decrements : t_queue<32768>
#else
	struct t_decrements : t_queue<256>
#endif
	{
		t_object* volatile* v_last = v_objects;

		void f_flush()
		{
			f__flush(v_last, [](auto x)
			{
				x->f_decrement();
			});
			v_last = v_epoch.load(std::memory_order_acquire);
		}
	};

	static IL2CXX__PORTABLE__THREAD t_collector* v_collector;
	static IL2CXX__PORTABLE__THREAD t_increments* v_increments;
	static IL2CXX__PORTABLE__THREAD t_decrements* v_decrements;

#ifdef IL2CXX__PORTABLE__SUPPORTS_THREAD_EXPORT
	static t_increments* f_increments()
	{
		return v_increments;
	}
	static t_decrements* f_decrements()
	{
		return v_decrements;
	}
#else
	static IL2CXX__PORTABLE__EXPORT t_increments* f_increments();
	static IL2CXX__PORTABLE__EXPORT t_decrements* f_decrements();
#endif

	std::atomic<t_object*> v_p;

public:
	t_slot() = default;
	t_slot(t_object* a_p) : v_p(a_p)
	{
		if (a_p) f_increments()->f_push(a_p);
	}
	t_slot(const t_slot& a_value) : t_slot(static_cast<t_object*>(a_value))
	{
	}
	t_slot& operator=(t_object* a_p)
	{
		if (a_p) f_increments()->f_push(a_p);
		if (auto p = v_p.exchange(a_p, std::memory_order_relaxed)) f_decrements()->f_push(p);
		return *this;
	}
	t_slot& operator=(const t_slot& a_value)
	{
		return *this = static_cast<t_object*>(a_value);
	}
	void f__destruct()
	{
		if (auto p = v_p.load(std::memory_order_relaxed)) f_decrements()->f_push(p);
	}
	operator bool() const
	{
		return v_p.load(std::memory_order_relaxed);
	}
	operator t_object*() const
	{
		return v_p.load(std::memory_order_relaxed);
	}
	template<typename T>
	explicit operator T*() const
	{
		return static_cast<T*>(v_p.load(std::memory_order_relaxed));
	}
	t_object* operator->() const
	{
		return v_p.load(std::memory_order_relaxed);
	}
};

template<typename T>
struct t_slot_of : t_slot
{
	using t_slot::t_slot;
	t_slot_of& operator=(const t_slot_of& a_value)
	{
		static_cast<t_slot&>(*this) = a_value;
		return *this;
	}
	template<typename U>
	t_slot_of& operator=(U&& a_value)
	{
		static_cast<t_slot&>(*this) = std::forward<U>(a_value);
		return *this;
	}
	operator T*() const
	{
		return static_cast<T*>(v_p.load(std::memory_order_relaxed));
	}
	T* operator->() const
	{
		return static_cast<T*>(v_p.load(std::memory_order_relaxed));
	}
};

template<size_t A_SIZE>
void t_slot::t_queue<A_SIZE>::f_next() noexcept
{
	v_collector->f_tick();
	if (v_head < v_objects + V_SIZE - 1) {
		++v_head;
		while (v_tail == v_head) v_collector->f_wait();
		auto tail = v_tail;
		v_next = std::min(tail < v_head ? v_objects + V_SIZE - 1 : tail - 1, v_head + V_SIZE / 2);
	} else {
		v_head = v_objects;
		while (v_tail == v_head) v_collector->f_wait();
		v_next = std::min(v_tail - 1, v_head + V_SIZE / 2);
	}
}

typedef void (*t_scan)(t_slot&);

}

#endif
