#ifndef IL2CXX__SLOT_H
#define IL2CXX__SLOT_H

#include "define.h"
#include <cassert>
#include <cinttypes>
#include <cstddef>
#include <condition_variable>
#include <atomic>
#include <mutex>
#include <type_traits>

namespace il2cxx
{

struct t__type;
class t_engine;
class t_object;
class t_thread;
t_engine* f_engine();

class t_slot
{
	friend class t_engine;
	friend class t_object;
	friend class t_thread;
	friend t_engine* f_engine();

public:
	class t_collector
	{
	protected:
		bool v_collector__running = true;
		bool v_collector__quitting = false;
		std::mutex v_collector__mutex;
		std::condition_variable v_collector__wake;
		std::condition_variable v_collector__done;
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
			if (v_collector__running) return;
			std::lock_guard<std::mutex> lock(v_collector__mutex);
			++v_collector__tick;
			if (v_collector__running) return;
			v_collector__running = true;
			v_collector__wake.notify_one();
		}
		void f_wait()
		{
			std::unique_lock<std::mutex> lock(v_collector__mutex);
			++v_collector__wait;
			if (!v_collector__running) {
				v_collector__running = true;
				v_collector__wake.notify_one();
			}
			v_collector__done.wait(lock);
		}
	};

protected:
	template<size_t A_SIZE>
	struct t_queue
	{
		static const size_t V_SIZE = A_SIZE;

		std::atomic<t_object* volatile*> v_head{v_objects};
		t_object* volatile* v_next = v_objects + V_SIZE / 2;
		t_object* volatile v_objects[V_SIZE];
		std::atomic<t_object* volatile*> v_tail{v_objects + V_SIZE - 1};

		void f_next(t_object* a_object) noexcept;
		IL2CXX__PORTABLE__ALWAYS_INLINE IL2CXX__PORTABLE__FORCE_INLINE void f_push(t_object* a_object)
		{
			auto head = v_head.load(std::memory_order_relaxed);
			if (head == v_next) {
				f_next(a_object);
			} else {
				*head = a_object;
				v_head.store(++head, std::memory_order_release);
			}
		}
	};
#ifdef NDEBUG
	struct t_increments : t_queue<16384>
#else
	struct t_increments : t_queue<128>
#endif
	{
		void f_flush();
	};
#ifdef NDEBUG
	struct t_decrements : t_queue<32768>
#else
	struct t_decrements : t_queue<256>
#endif
	{
		t_object* volatile* v_last = v_objects;

		void f_flush();
	};
	class t_pass
	{
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

	t_object* v_p;

	IL2CXX__PORTABLE__ALWAYS_INLINE void f_copy_construct(const t_slot& a_value)
	{
		if (a_value.v_p) f_increments()->f_push(a_value.v_p);
		v_p = a_value.v_p;
	}
	void f_move_construct(t_slot& a_value)
	{
		v_p = a_value.v_p;
		a_value.v_p = nullptr;
	}
	t_slot(t_object* a_p, const t_pass&) : v_p(a_p)
	{
	}
	void f_assign(t_object* a_p)
	{
		if (a_p) f_increments()->f_push(a_p);
		auto p = v_p;
		v_p = a_p;
		if (p) f_decrements()->f_push(p);
	}
	void f_assign(t_object& a_p)
	{
		f_increments()->f_push(&a_p);
		auto p = v_p;
		v_p = &a_p;
		if (p) f_decrements()->f_push(p);
	}
	IL2CXX__PORTABLE__ALWAYS_INLINE void f_assign(const t_slot& a_value)
	{
		auto p = v_p;
		f_copy_construct(a_value);
		if (p) f_decrements()->f_push(p);
	}
	IL2CXX__PORTABLE__ALWAYS_INLINE void f_assign(t_slot&& a_value)
	{
		if (&a_value == this) return;
		auto p = v_p;
		f_move_construct(a_value);
		if (p) f_decrements()->f_push(p);
	}

public:
	t_slot(t_object* a_p = nullptr) : v_p(a_p)
	{
		if (v_p) f_increments()->f_push(v_p);
	}
	t_slot(const t_slot& a_value)
	{
		f_copy_construct(a_value);
	}
	t_slot(t_slot&& a_value)
	{
		f_move_construct(a_value);
	}
	t_slot& operator=(const t_slot& a_value)
	{
		f_assign(a_value);
		return *this;
	}
	t_slot& operator=(t_slot&& a_value)
	{
		f_assign(std::move(a_value));
		return *this;
	}
	bool operator==(const t_slot& a_value) const
	{
		return v_p == a_value.v_p;
	}
	bool operator!=(const t_slot& a_value) const
	{
		return !operator==(a_value);
	}
	operator bool() const
	{
		return v_p;
	}
	operator t_object*() const
	{
		return v_p;
	}
	template<typename T>
	explicit operator T*() const
	{
		return static_cast<T*>(v_p);
	}
	t_object* operator->() const
	{
		return v_p;
	}
	void f_construct(const t_slot& a_value)
	{
		assert(!v_p);
		f_copy_construct(a_value);
	}
	void f_construct(t_slot&& a_value)
	{
		f_construct(a_value);
		a_value.f__destruct();
	}
	IL2CXX__PORTABLE__ALWAYS_INLINE void f__destruct()
	{
		if (!v_p) return;
		f_decrements()->f_push(v_p);
		v_p = nullptr;
	}
};

template<typename T>
class t_slot_of : public t_slot
{
	friend struct t__type;
	friend class t_object;

	t_slot_of(T* a_p, const t_pass&) : t_slot(a_p, t_pass())
	{
	}

public:
	t_slot_of(T* a_p = nullptr) : t_slot(a_p)
	{
	}
	t_slot_of(const t_slot& a_value) : t_slot(a_value)
	{
	}
	t_slot_of(t_slot&& a_value) : t_slot(std::move(a_value))
	{
	}
	t_slot_of(const t_slot_of& a_value) : t_slot(a_value)
	{
	}
	t_slot_of(t_slot_of&& a_value) : t_slot(std::move(a_value))
	{
	}
	t_slot_of& operator=(T* a_p)
	{
		f_assign(a_p);
		return *this;
	}
	IL2CXX__PORTABLE__ALWAYS_INLINE t_slot_of& operator=(const t_slot& a_value)
	{
		f_assign(a_value);
		return *this;
	}
	IL2CXX__PORTABLE__ALWAYS_INLINE t_slot_of& operator=(t_slot&& a_value)
	{
		f_assign(std::move(a_value));
		return *this;
	}
	t_slot_of& operator=(const t_slot_of& a_value)
	{
		f_assign(a_value);
		return *this;
	}
	t_slot_of& operator=(t_slot_of&& a_value)
	{
		f_assign(std::move(a_value));
		return *this;
	}
	/*void f_construct(T* a_p = nullptr)
	{
		assert(!v_p);
		if (a_p) f_increments()->f_push(a_p);
		v_p = a_p;
	}*/
	operator T*() const
	{
		return static_cast<T*>(v_p);
	}
	T* operator->() const
	{
		return static_cast<T*>(v_p);
	}
};

template<typename T>
struct t_scoped : T
{
	using T::T;
	template<typename U>
	t_scoped(U&& a_value) : T(std::forward<U>(a_value))
	{
	}
	template<typename U>
	t_scoped(const t_scoped<U>& a_value) : T(a_value)
	{
	}
	template<typename U>
	t_scoped(t_scoped<U>&& a_value) : T(std::move(a_value))
	{
	}
	~t_scoped()
	{
		this->f__destruct();
	}
	template<typename U>
	t_scoped& operator=(U&& a_value)
	{
		static_cast<T&>(*this) = std::forward<U>(a_value);
		return *this;
	}
};

template<size_t A_SIZE>
void t_slot::t_queue<A_SIZE>::f_next(t_object* a_object) noexcept
{
	v_collector->f_tick();
	auto head = v_head.load(std::memory_order_relaxed);
	while (head == v_tail.load(std::memory_order_acquire)) v_collector->f_wait();
	*head = a_object;
	if (head < v_objects + V_SIZE - 1) {
		v_head.store(++head, std::memory_order_release);
		auto tail = v_tail.load(std::memory_order_acquire);
		v_next = std::min(tail < head ? v_objects + V_SIZE - 1 : tail, head + V_SIZE / 2);
	} else {
		v_head.store(v_objects, std::memory_order_release);
		auto tail = v_tail.load(std::memory_order_acquire);
		v_next = std::min(tail, v_objects + V_SIZE / 2);
	}
}

typedef void (*t_scan)(t_slot&);

}

#endif
