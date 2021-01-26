#ifndef RECYCLONE__SLOT_H
#define RECYCLONE__SLOT_H

#include "define.h"
#include <atomic>
#include <cstring>

namespace recyclone
{

class t_object;

class t_slot
{
	friend class t_type;
	friend class t_object;
	friend struct t_weak_pointer;
	friend struct t_thread;
	friend class t_engine;

	template<size_t A_SIZE>
	struct t_queue
	{
		static const size_t V_SIZE = A_SIZE;

		static RECYCLONE__THREAD t_queue* v_instance;
		static RECYCLONE__THREAD t_object* volatile* v_head;
		static RECYCLONE__THREAD t_object* volatile* v_next;

		RECYCLONE__ALWAYS_INLINE static void f_push(t_object* a_object)
		{
			auto p = v_head;
			*p = a_object;
			if (p == v_next)
				v_instance->f_next();
			else
				[[likely]] v_head = p + 1;
		}

		t_object* volatile v_objects[V_SIZE];
		std::atomic<t_object* volatile*> v_epoch;
		t_object* volatile* v_tail{v_objects + V_SIZE - 1};

		void f_next() noexcept;
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

protected:
	std::atomic<t_object*> v_p;

public:
	t_slot() = default;
	t_slot(t_object* a_p) : v_p(a_p)
	{
		if (a_p) t_increments::f_push(a_p);
	}
	t_slot(const t_slot& a_value) : t_slot(static_cast<t_object*>(a_value))
	{
	}
	RECYCLONE__ALWAYS_INLINE t_slot& operator=(t_object* a_p)
	{
		if (a_p) t_increments::f_push(a_p);
		if (auto p = v_p.exchange(a_p, std::memory_order_relaxed)) t_decrements::f_push(p);
		return *this;
	}
	RECYCLONE__ALWAYS_INLINE t_slot& operator=(const t_slot& a_value)
	{
		return *this = static_cast<t_object*>(a_value);
	}
	void f_destruct()
	{
		if (auto p = v_p.load(std::memory_order_relaxed)) t_decrements::f_push(p);
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
	RECYCLONE__ALWAYS_INLINE t_slot_of& operator=(const t_slot_of& a_value)
	{
		static_cast<t_slot&>(*this) = a_value;
		return *this;
	}
	template<typename U>
	RECYCLONE__ALWAYS_INLINE t_slot_of& operator=(U&& a_value)
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
RECYCLONE__THREAD t_slot::t_queue<A_SIZE>* t_slot::t_queue<A_SIZE>::v_instance;
template<size_t A_SIZE>
RECYCLONE__THREAD t_object* volatile* t_slot::t_queue<A_SIZE>::v_head;
template<size_t A_SIZE>
RECYCLONE__THREAD t_object* volatile* t_slot::t_queue<A_SIZE>::v_next;

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
		this->f_destruct();
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

}

#endif
