#ifndef RECYCLONE__OBJECT_H
#define RECYCLONE__OBJECT_H

#include "heap.h"
#include "slot.h"
#include <condition_variable>
#include <cassert>

namespace recyclone
{

struct t_type;
struct t_extension;
struct t_weak_pointer;

class t_object
{
	template<typename T> friend class t_heap;
	friend class t_slot;
	friend struct t_type;
	friend class t_thread;
	friend class t_engine;
	friend struct t_weak_pointer;

	enum t_color : char
	{
		e_color__BLACK,
		e_color__PURPLE,
		e_color__GRAY,
		e_color__WHITING,
		e_color__WHITE,
		e_color__ORANGE,
		e_color__RED
	};

	static RECYCLONE__THREAD struct
	{
		t_object* v_next;
		t_object* v_previous;
	} v_roots;
	static RECYCLONE__THREAD t_object* v_scan_stack;
	static RECYCLONE__THREAD t_object* v_cycle;
	static RECYCLONE__THREAD t_object* v_cycles;

	RECYCLONE__FORCE_INLINE static void f_append(t_object* a_p)
	{
		a_p->v_next = reinterpret_cast<t_object*>(&v_roots);
		a_p->v_previous = v_roots.v_previous;
		a_p->v_previous->v_next = v_roots.v_previous = a_p;
	}
	static void f_push(t_object* a_p)
	{
		a_p->v_scan = v_scan_stack;
		v_scan_stack = a_p;
	}
	template<void (t_object::*A_push)()>
	static void f_push(t_slot& a_slot)
	{
		if (auto p = a_slot.v_p.load(std::memory_order_relaxed)) (p->*A_push)();
	}
	template<void (t_object::*A_push)()>
	static void f_push_and_clear(t_slot& a_slot)
	{
		auto p = a_slot.v_p.load(std::memory_order_relaxed);
		if (!p) return;
		(p->*A_push)();
		a_slot.v_p.store(nullptr, std::memory_order_relaxed);
	}
	static void f_collect();

	t_object* v_next;
	t_object* v_previous;
	t_object* v_scan;
	t_color v_color;
	bool v_finalizee = false;
	size_t v_count = 1;
	size_t v_cyclic;
	size_t v_rank;
	t_object* v_next_cycle;
	t_type* v_type = nullptr;
	std::atomic<t_extension*> v_extension = nullptr;

	template<void (t_object::*A_push)()>
	void f_step();
	template<void (t_object::*A_step)()>
	void f_loop()
	{
		auto p = this;
		while (true) {
			(p->*A_step)();
			p = v_scan_stack;
			if (!p) break;
			v_scan_stack = p->v_scan;
		}
	}
	RECYCLONE__FORCE_INLINE void f_increment()
	{
		++v_count;
		v_color = e_color__BLACK;
	}
	void f_decrement_push()
	{
		if (--v_count > 0) {
			v_color = e_color__PURPLE;
			if (!v_next) f_append(this);
		} else if (!v_finalizee || !f_queue_finalize()) {
			f_push(this);
		}
	}
	bool f_queue_finalize();
	void f_decrement_step();
	void f_decrement()
	{
		assert(v_count > 0);
		if (--v_count > 0) {
			v_color = e_color__PURPLE;
			if (!v_next) f_append(this);
		} else if (!v_finalizee || !f_queue_finalize()) {
			f_loop<&t_object::f_decrement_step>();
		}
	}
	void f_mark_gray_push()
	{
		if (v_color != e_color__GRAY) {
			v_color = e_color__GRAY;
			v_cyclic = v_count;
			f_push(this);
		}
		--v_cyclic;
	}
	void f_mark_gray()
	{
		v_color = e_color__GRAY;
		v_cyclic = v_count;
		f_loop<&t_object::f_step<&t_object::f_mark_gray_push>>();
	}
	void f_scan_black_push()
	{
		if (v_color == e_color__BLACK) return;
		v_color = e_color__BLACK;
		f_push(this);
	}
	void f_scan_gray_scan_black_push()
	{
		if (v_color == e_color__BLACK) return;
		if (v_color != e_color__WHITING) f_push(this);
		v_color = e_color__BLACK;
	}
	void f_scan_gray_push()
	{
		if (v_color != e_color__GRAY) return;
		v_color = v_cyclic > 0 ? e_color__BLACK : e_color__WHITING;
		f_push(this);
	}
	void f_scan_gray_step()
	{
		if (v_color == e_color__BLACK) {
			f_step<&t_object::f_scan_gray_scan_black_push>();
		} else {
			v_color = e_color__WHITE;
			f_step<&t_object::f_scan_gray_push>();
		}
	}
	void f_scan_gray()
	{
		if (v_color != e_color__GRAY) return;
		if (v_cyclic > 0) {
			v_color = e_color__BLACK;
			f_loop<&t_object::f_step<&t_object::f_scan_black_push>>();
		} else {
			f_loop<&t_object::f_scan_gray_step>();
		}
	}
	void f_collect_white_push()
	{
		if (v_color != e_color__WHITE) return;
		v_color = e_color__ORANGE;
		v_next = v_cycle->v_next;
		v_cycle->v_next = this;
		f_push(this);
	}
	void f_collect_white()
	{
		v_color = e_color__ORANGE;
		v_cycle = v_next = this;
		f_loop<&t_object::f_step<&t_object::f_collect_white_push>>();
	}
	void f_scan_red()
	{
		if (v_color == e_color__RED && v_cyclic > 0) --v_cyclic;
	}
	void f_cyclic_decrement_push()
	{
		if (v_color == e_color__RED) return;
		if (v_color == e_color__ORANGE) {
			--v_count;
			--v_cyclic;
		} else {
			f_decrement();
		}
	}
	void f_cyclic_decrement();

protected:
	void f_type__(t_type* a_type)
	{
		v_type = a_type;
	}

public:
	void f_finalizee__(bool a_value)
	{
		v_finalizee = a_value;
	}
	t_type* f_type() const
	{
		return v_type;
	}
	t_extension* f_extension();
};

typedef void (*t_scan)(t_slot&);

struct t_type
{
	RECYCLONE__ALWAYS_INLINE void f_finish(t_object* a_p)
	{
		//t_slot::t_increments::f_push(this);
		std::atomic_signal_fence(std::memory_order_release);
		a_p->v_type = this;
		t_slot::t_decrements::f_push(a_p);
	}
	static void f_do_scan(t_object* a_this, t_scan a_scan);
	void (*f_scan)(t_object*, t_scan) = f_do_scan;
};

struct t_extension
{
	std::recursive_timed_mutex v_mutex;
	std::condition_variable_any v_condition;
	struct
	{
		t_weak_pointer* v_previous;
		t_weak_pointer* v_next;
	} v_weak_pointers;
	t_slot v_weak_pointers__cycle{};
	std::mutex v_weak_pointers__mutex;

	t_extension();
	~t_extension();
	void f_detach();
	void f_scan(t_scan a_scan);
};

struct t_weak_pointer : decltype(t_extension::v_weak_pointers)
{
	t_object* v_target;
	bool v_final;

	void f_attach(t_root<t_slot>& a_target);
	t_object* f_detach();

	t_weak_pointer(t_object* a_target, bool a_final);
	~t_weak_pointer();
	t_object* f_target() const;
	void f_target__(t_object* a_p);
	virtual void f_scan(t_scan a_scan);
};

inline t_extension::t_extension() : v_weak_pointers{static_cast<t_weak_pointer*>(&v_weak_pointers), static_cast<t_weak_pointer*>(&v_weak_pointers)}
{
}

}

#endif
