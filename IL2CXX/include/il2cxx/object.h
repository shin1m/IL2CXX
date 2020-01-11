#ifndef IL2CXX__OBJECT_H
#define IL2CXX__OBJECT_H

#include "pool.h"
#include "slot.h"

namespace il2cxx
{

struct t__extension;
struct t__weak_handle;
template<typename T>
struct t__type_of;

class t_object
{
	friend class t_slot;
	template<typename T, size_t A_size> friend class t_shared_pool;
	template<typename T> friend class t_local_pool;
	template<size_t A_rank> friend class t_object_and;
	friend struct t__type;
	friend struct t__type_finalizee;
	friend class t_engine;
	friend struct t__weak_handle;

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

	static IL2CXX__PORTABLE__THREAD struct
	{
		t_object* v_next;
		t_object* v_previous;
	} v_roots;
	static IL2CXX__PORTABLE__THREAD t_object* v_scan_stack;
	static IL2CXX__PORTABLE__THREAD t_object* v_cycle;
	static IL2CXX__PORTABLE__THREAD t_object* v_cycles;

	IL2CXX__PORTABLE__FORCE_INLINE static void f_append(t_object* a_p)
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
		if (auto p = a_slot.v_p.load()) (p->*A_push)();
	}
	template<void (t_object::*A_push)()>
	static void f_push_and_clear(t_slot& a_slot)
	{
		auto p = a_slot.v_p.load();
		if (!p) return;
		(p->*A_push)();
		a_slot.v_p = nullptr;
	}
	static void f_collect();
	template<size_t A_rank>
	static t_object* f_pool__allocate();
	static t_object* f_local_pool__allocate(size_t a_size);

	t_object* v_next;
	t_object* v_previous;
	t_object* v_scan;
	t_color v_color;
	bool v_finalizee = false;
	size_t v_count = 1;
	size_t v_cyclic;
	size_t v_rank;
	t_object* v_next_cycle;
	t__type* v_type;
	std::atomic<t__extension*> v_extension{nullptr};

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
	IL2CXX__PORTABLE__FORCE_INLINE void f_increment()
	{
		++v_count;
		v_color = e_color__BLACK;
	}
	void f_decrement_push()
	{
		if (--v_count > 0) {
			v_color = e_color__PURPLE;
			if (!v_next) f_append(this);
		} else {
			f_push(this);
		}
	}
	void f_decrement_step();
	void f_decrement();
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

public:
	template<typename T>
	static t_scoped<t_slot_of<T>> f_allocate(size_t a_extra = 0);

	t__type* f_type() const
	{
		return v_type;
	}
	t__extension* f_extension();
	void f__scan(t_scan a_scan)
	{
	}
	void f__construct(t_object* a_p) const
	{
	}
	t_scoped<t_slot> f__clone() const
	{
		return f_allocate<t_object>();
	}
};

template<size_t A_rank>
struct t_object_and : t_object
{
	char v_data[sizeof(void*) * (6 + 8 * A_rank)];

	t_object_and()
	{
		v_rank = A_rank;
	}
};

struct t__extension
{
	std::recursive_timed_mutex v_mutex;
	std::condition_variable_any v_condition;
	struct
	{
		t__weak_handle* v_previous;
		t__weak_handle* v_next;
	} v_weak_handles;
	t_member<t_slot> v_weak_handles__cycle;
	std::mutex v_weak_handles__mutex;

	t__extension();
	~t__extension();
	void f_detach();
	void f_scan(t_scan a_scan);
};

struct t__handle
{
	virtual ~t__handle() = default;
	virtual t_scoped<t_slot> f_target() const = 0;
};

struct t__normal_handle : t__handle
{
	t_scoped<t_slot> v_target;

	t__normal_handle(t_scoped<t_slot>&& a_target) : v_target(std::move(a_target))
	{
	}
	virtual t_scoped<t_slot> f_target() const;
};

struct t__weak_handle : t__handle, decltype(t__extension::v_weak_handles)
{
	t_object* v_target;
	bool v_final;

	void f_attach(t_scoped<t_slot>&& a_target);
	t_scoped<t_slot> f_detach();

	t__weak_handle(t_scoped<t_slot>&& a_target, bool a_final);
	virtual ~t__weak_handle();
	virtual t_scoped<t_slot> f_target() const;
	virtual void f_target__(t_scoped<t_slot>&& a_p);
	virtual void f_scan(t_scan a_scan);
};

inline t__extension::t__extension() : v_weak_handles{static_cast<t__weak_handle*>(&v_weak_handles), static_cast<t__weak_handle*>(&v_weak_handles)}
{
}

struct t__dependent_handle : t__weak_handle
{
	t_member<t_slot> v_secondary;

	t__dependent_handle(t_scoped<t_slot>&& a_target, t_scoped<t_slot>&& a_secondary) : t__weak_handle(std::move(a_target), false), v_secondary(std::move(a_secondary))
	{
	}
	virtual ~t__dependent_handle();
	virtual void f_target__(t_scoped<t_slot>&& a_p);
	virtual void f_scan(t_scan a_scan);
};

}

#endif
