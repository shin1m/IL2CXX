#ifndef RECYCLONE__SLOT_H
#define RECYCLONE__SLOT_H

#include "define.h"
#include <atomic>

namespace recyclone
{

template<typename T_type>
class t_object;
template<typename T_type>
class t_weak_pointer;
template<typename T_type>
class t_thread;
template<typename T_type>
class t_engine;
template<typename T_type>
t_engine<T_type>* f_engine();

template<typename T_type>
class t_slot
{
	friend T_type;
	friend class t_object<T_type>;
	friend class t_weak_pointer<T_type>;
	friend class t_thread<T_type>;
	friend class t_engine<T_type>;

	template<size_t A_SIZE>
	struct t_queue
	{
		static constexpr size_t V_SIZE = A_SIZE;

		static inline thread_local t_queue* v_instance;
		static inline thread_local t_object<T_type>* volatile* v_head;
		static inline thread_local t_object<T_type>* volatile* v_next;

		RECYCLONE__ALWAYS_INLINE static void f_push(t_object<T_type>* a_object)
		{
			auto p = v_head;
			*p = a_object;
			if (p == v_next)
				v_instance->f_next();
			else
				[[likely]] v_head = p + 1;
		}

		t_object<T_type>* volatile v_objects[V_SIZE];
		std::atomic<t_object<T_type>* volatile*> v_epoch;
		t_object<T_type>* volatile* v_tail{v_objects + V_SIZE - 1};

		void f_next() noexcept;
		template<typename T>
		void f__flush(t_object<T_type>* volatile* a_epoch, T a_do)
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
			this->f__flush(this->v_epoch.load(std::memory_order_acquire), [](auto x)
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
		t_object<T_type>* volatile* v_last = this->v_objects;

		void f_flush()
		{
			this->f__flush(v_last, [](auto x)
			{
				x->f_decrement();
			});
			v_last = this->v_epoch.load(std::memory_order_acquire);
		}
	};

protected:
	std::atomic<t_object<T_type>*> v_p;

public:
	t_slot() = default;
	t_slot(t_object<T_type>* a_p) : v_p(a_p)
	{
		if (a_p) t_increments::f_push(a_p);
	}
	t_slot(const t_slot& a_value) : t_slot(static_cast<t_object<T_type>*>(a_value))
	{
	}
	RECYCLONE__ALWAYS_INLINE t_slot& operator=(t_object<T_type>* a_p)
	{
		if (a_p) t_increments::f_push(a_p);
		if (auto p = v_p.exchange(a_p, std::memory_order_relaxed)) t_decrements::f_push(p);
		return *this;
	}
	RECYCLONE__ALWAYS_INLINE t_slot& operator=(const t_slot& a_value)
	{
		return *this = static_cast<t_object<T_type>*>(a_value);
	}
	void f_destruct()
	{
		if (auto p = v_p.load(std::memory_order_relaxed)) t_decrements::f_push(p);
	}
	operator bool() const
	{
		return v_p.load(std::memory_order_relaxed);
	}
	operator t_object<T_type>*() const
	{
		return v_p.load(std::memory_order_relaxed);
	}
	template<typename T>
	explicit operator T*() const
	{
		return static_cast<T*>(v_p.load(std::memory_order_relaxed));
	}
	t_object<T_type>* operator->() const
	{
		return v_p.load(std::memory_order_relaxed);
	}
	t_object<T_type>* f_exchange(t_object<T_type>* a_desired)
	{
		if (a_desired) t_increments::f_push(a_desired);
		a_desired = v_p.exchange(a_desired, std::memory_order_relaxed);
		if (a_desired) t_decrements::f_push(a_desired);
		return a_desired;
	}
	bool f_compare_exchange(t_object<T_type>*& a_expected, t_object<T_type>* a_desired)
	{
		if (a_desired) t_increments::f_push(a_desired);
		if (v_p.compare_exchange_strong(a_expected, a_desired)) {
			if (a_expected) t_decrements::f_push(a_expected);
			return true;
		} else {
			if (a_desired) t_decrements::f_push(a_desired);
			return false;
		}
	}
};

template<typename T_type>
template<size_t A_SIZE>
void t_slot<T_type>::t_queue<A_SIZE>::f_next() noexcept
{
	f_engine<T_type>()->f_tick();
	if (v_head < v_objects + V_SIZE - 1) {
		++v_head;
		while (v_tail == v_head) f_engine<T_type>()->f_wait();
		auto tail = v_tail;
		v_next = std::min(tail < v_head ? v_objects + V_SIZE - 1 : tail - 1, v_head + V_SIZE / 8);
	} else {
		v_head = v_objects;
		while (v_tail == v_head) f_engine<T_type>()->f_wait();
		v_next = std::min(v_tail - 1, v_head + V_SIZE / 8);
	}
}

template<typename T, typename T_type = typename T::t_type>
struct t_slot_of : t_slot<T_type>
{
	using t_slot<T_type>::t_slot;
	RECYCLONE__ALWAYS_INLINE t_slot_of& operator=(const t_slot_of& a_value)
	{
		static_cast<t_slot<T_type>&>(*this) = a_value;
		return *this;
	}
	template<typename U>
	RECYCLONE__ALWAYS_INLINE t_slot_of& operator=(U&& a_value)
	{
		static_cast<t_slot<T_type>&>(*this) = std::forward<U>(a_value);
		return *this;
	}
	operator T*() const
	{
		return static_cast<T*>(this->v_p.load(std::memory_order_relaxed));
	}
	T* operator->() const
	{
		return static_cast<T*>(this->v_p.load(std::memory_order_relaxed));
	}
	T* f_exchange(T* a_desired)
	{
		return static_cast<T*>(t_slot<T_type>::f_exchange(a_desired));
	}
	bool f_compare_exchange(T*& a_expected, T* a_desired)
	{
		return t_slot<T_type>::f_compare_exchange(reinterpret_cast<t_object<T_type>*&>(a_expected), a_desired);
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

}

#endif
