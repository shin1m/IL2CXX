#ifndef RECYCLONE__OBJECT_H
#define RECYCLONE__OBJECT_H

#include "heap.h"
#include "slot.h"
#include <condition_variable>
#include <cassert>

namespace recyclone
{

template<typename T_type>
struct t_extension;

template<typename T_type>
class t_object
{
	friend T_type;
	template<typename T> friend class t_heap;
	friend class t_slot<T_type>;
	friend class t_thread<T_type>;
	friend class t_engine<T_type>;
	friend struct t_weak_pointer<T_type>;

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

	static inline RECYCLONE__THREAD struct
	{
		t_object* v_next;
		t_object* v_previous;
	} v_roots;
	static inline RECYCLONE__THREAD t_object* v_scan_stack;
	static inline RECYCLONE__THREAD t_object* v_cycle;
	static inline RECYCLONE__THREAD t_object* v_cycles;

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
	static void f_push(t_slot<T_type>& a_slot)
	{
		if (auto p = a_slot.v_p.load(std::memory_order_relaxed)) p->template f_push<A_push>();
	}
	template<void (t_object::*A_push)()>
	static void f_push_and_clear(t_slot<T_type>& a_slot)
	{
		auto p = a_slot.v_p.load(std::memory_order_relaxed);
		if (!p) return;
		p->template f_push<A_push>();
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
	T_type* v_type = nullptr;
	std::atomic<t_extension<T_type>*> v_extension = nullptr;

	template<void (t_object::*A_push)()>
	void f_push()
	{
		(this->*A_push)();
	}
	template<void (t_object::*A_push)()>
	void f_step()
	{
		v_type->f_scan(this, f_push<A_push>);
		v_type->template f_push<A_push>();
		if (auto p = v_extension.load(std::memory_order_consume)) p->f_scan(f_push<A_push>);
	}
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
	void f_decrement_step()
	{
		if (auto p = v_extension.load(std::memory_order_consume)) {
			p->f_scan(f_push_and_clear<&t_object::f_decrement_push>);
			v_extension.store(nullptr, std::memory_order_relaxed);
			delete p;
		}
		v_type->f_scan(this, f_push_and_clear<&t_object::f_decrement_push>);
		v_type->f_decrement_push();
		v_type = nullptr;
		v_color = e_color__BLACK;
		if (v_next) {
			v_next->v_previous = v_previous;
			v_previous->v_next = v_next;
		}
		f_engine<T_type>()->f_free_as_release(this);
	}
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
	void f_own()
	{
		t_slot<T_type>::t_increments::f_push(this);
	}

public:
	using t_type = T_type;

	RECYCLONE__ALWAYS_INLINE void f_bless(T_type* a_type)
	{
		a_type->f_own();
		std::atomic_signal_fence(std::memory_order_release);
		v_type = a_type;
		t_slot<T_type>::t_decrements::f_push(this);
	}
	void f_finalizee__(bool a_value)
	{
		v_finalizee = a_value;
	}
	T_type* f_type() const
	{
		return v_type;
	}
	t_extension<T_type>* f_extension();
};

template<typename T_type>
void t_object<T_type>::f_collect()
{
	while (v_cycles) {
		std::lock_guard lock(f_engine<T_type>()->v_object__reviving__mutex);
		auto cycle = v_cycles;
		v_cycles = cycle->v_next_cycle;
		auto p = cycle;
		auto mutated = [&]
		{
			if (f_engine<T_type>()->v_object__reviving)
				do {
					if (p->v_color != e_color__ORANGE || p->v_cyclic > 0) return true;
					if (auto q = p->v_extension.load(std::memory_order_relaxed)) if (q->v_weak_pointers__cycle) return true;
				} while ((p = p->v_next) != cycle);
			else
				do if (p->v_color != e_color__ORANGE || p->v_cyclic > 0) return true; while ((p = p->v_next) != cycle);
			return false;
		};
		if (mutated()) {
			p = cycle;
			auto q = p->v_next;
			if (p->v_color == e_color__ORANGE) {
				p->v_color = e_color__PURPLE;
				f_append(p);
			} else if (p->v_color == e_color__PURPLE) {
				f_append(p);
			} else {
				p->v_color = e_color__BLACK;
				p->v_next = nullptr;
			}
			while (q != cycle) {
				p = q;
				q = p->v_next;
				if (p->v_color == e_color__PURPLE) {
					f_append(p);
				} else {
					p->v_color = e_color__BLACK;
					p->v_next = nullptr;
				}
			}
		} else {
			auto finalizee = false;
			do
				if (p->v_finalizee) {
					finalizee = true;
					p = cycle;
					break;
				}
			while ((p = p->v_next) != cycle);
			if (finalizee) {
				auto& conductor = f_engine<T_type>()->v_finalizer__conductor;
				std::lock_guard lock(conductor.v_mutex);
				if (conductor.v_quitting) {
					finalizee = false;
				} else {
					auto& queue = f_engine<T_type>()->v_finalizer__queue;
					do {
						if (auto q = p->v_extension.load(std::memory_order_relaxed)) q->f_detach();
						auto q = p->v_next;
						p->v_color = e_color__BLACK;
						p->v_next = nullptr;
						if (p->v_finalizee) {
							++p->v_count;
							queue.push_back(p);
						}
						p = q;
					} while (p != cycle);
					conductor.f_wake();
				}
			}
			if (!finalizee) {
				do p->v_color = e_color__RED; while ((p = p->v_next) != cycle);
				do p->f_cyclic_decrement(); while ((p = p->v_next) != cycle);
				do {
					auto q = p->v_next;
					f_engine<T_type>()->f_free_as_collect(p);
					p = q;
				} while (p != cycle);
			}
		}
	}
	auto roots = reinterpret_cast<t_object*>(&v_roots);
	if (roots->v_next == roots) return;
	{
		size_t live = f_engine<T_type>()->v_object__heap.f_live();
		auto& lower = f_engine<T_type>()->v_object__lower;
		if (live < lower) lower = live;
		if (live - lower < f_engine<T_type>()->v_collector__threshold) return;
		lower = live;
		++f_engine<T_type>()->v_collector__collect;
		auto p = roots;
		auto q = p->v_next;
		do {
			assert(q->v_count > 0);
			if (q->v_color == e_color__PURPLE) {
				q->f_mark_gray();
				p = q;
			} else {
				p->v_next = q->v_next;
				q->v_next = nullptr;
			}
			q = p->v_next;
		} while (q != roots);
	}
	if (roots->v_next == roots) {
		roots->v_previous = roots;
		return;
	}
	{
		auto p = roots->v_next;
		do {
			p->f_scan_gray();
			p = p->v_next;
		} while (p != roots);
	}
	do {
		auto p = roots->v_next;
		roots->v_next = p->v_next;
		if (p->v_color == e_color__WHITE) {
			p->f_collect_white();
			v_cycle->v_next_cycle = v_cycles;
			v_cycles = v_cycle;
		} else {
			p->v_next = nullptr;
		}
	} while (roots->v_next != roots);
	roots->v_previous = roots;
	for (auto cycle = v_cycles; cycle; cycle = cycle->v_next_cycle) {
		auto p = cycle;
		do {
			p->v_color = e_color__RED;
			p->v_cyclic = p->v_count;
		} while ((p = p->v_next) != cycle);
		do p->template f_step<&t_object::f_scan_red>(); while ((p = p->v_next) != cycle);
		do p->v_color = e_color__ORANGE; while ((p = p->v_next) != cycle);
	}
}

template<typename T_type>
bool t_object<T_type>::f_queue_finalize()
{
	auto& conductor = f_engine<T_type>()->v_finalizer__conductor;
	std::lock_guard lock(conductor.v_mutex);
	if (conductor.v_quitting) return false;
	f_increment();
	f_engine<T_type>()->v_finalizer__queue.push_back(this);
	conductor.f_wake();
	return true;
}

template<typename T_type>
void t_object<T_type>::f_cyclic_decrement()
{
	if (auto p = v_extension.load(std::memory_order_consume)) {
		p->f_scan(f_push_and_clear<&t_object::f_cyclic_decrement_push>);
		v_extension.store(nullptr, std::memory_order_relaxed);
		delete p;
	}
	v_type->f_scan(this, f_push_and_clear<&t_object::f_cyclic_decrement_push>);
	v_type->f_cyclic_decrement_push();
	v_type = nullptr;
}

template<typename T_type>
t_extension<T_type>* t_object<T_type>::f_extension()
{
	auto p = v_extension.load(std::memory_order_consume);
	if (p) return p;
	t_extension<T_type>* q = nullptr;
	p = new t_extension<T_type>();
	if (v_extension.compare_exchange_strong(q, p, std::memory_order_consume)) return p;
	delete p;
	return q;
}

template<typename T_type>
using t_scan = void (*)(t_slot<T_type>&);

template<typename T_type>
struct t_extension
{
	std::recursive_timed_mutex v_mutex;
	std::condition_variable_any v_condition;
	struct
	{
		t_weak_pointer<T_type>* v_previous;
		t_weak_pointer<T_type>* v_next;
	} v_weak_pointers;
	t_slot<T_type> v_weak_pointers__cycle{};
	std::mutex v_weak_pointers__mutex;

	t_extension() : v_weak_pointers{static_cast<t_weak_pointer<T_type>*>(&v_weak_pointers), static_cast<t_weak_pointer<T_type>*>(&v_weak_pointers)}
	{
	}
	~t_extension();
	void f_detach();
	void f_scan(t_scan<T_type> a_scan);
};

template<typename T_type>
t_extension<T_type>::~t_extension()
{
	for (auto p = v_weak_pointers.v_next; p != static_cast<t_weak_pointer<T_type>*>(&v_weak_pointers); p = p->v_next) p->v_target = nullptr;
}

template<typename T_type>
void t_extension<T_type>::f_detach()
{
	for (auto p = v_weak_pointers.v_next; p != static_cast<t_weak_pointer<T_type>*>(&v_weak_pointers); p = p->v_next) {
		if (p->v_final) continue;
		p->v_target = nullptr;
		p->v_previous->v_next = p->v_next;
		p->v_next->v_previous = p->v_previous;
	}
}

template<typename T_type>
void t_extension<T_type>::f_scan(t_scan<T_type> a_scan)
{
	std::lock_guard lock(v_weak_pointers__mutex);
	a_scan(v_weak_pointers__cycle);
	for (auto p = v_weak_pointers.v_next; p != static_cast<t_weak_pointer<T_type>*>(&v_weak_pointers); p = p->v_next) p->f_scan(a_scan);
}

template<typename T_type>
struct t_weak_pointer : decltype(t_extension<T_type>::v_weak_pointers)
{
	t_object<T_type>* v_target;
	bool v_final;

	void f_attach(t_root<t_slot<T_type>>& a_target);
	t_object<T_type>* f_detach();

	t_weak_pointer(t_object<T_type>* a_target, bool a_final);
	~t_weak_pointer();
	t_object<T_type>* f_target() const;
	void f_target__(t_object<T_type>* a_p);
	virtual void f_scan(t_scan<T_type> a_scan)
	{
	}
};

template<typename T_type>
void t_weak_pointer<T_type>::f_attach(t_root<t_slot<T_type>>& a_target)
{
	v_target = a_target;
	if (!v_target) return;
	auto extension = v_target->f_extension();
	std::lock_guard lock(extension->v_weak_pointers__mutex);
	if (!extension->v_weak_pointers__cycle) extension->v_weak_pointers__cycle.v_p.store(a_target.v_p.exchange(nullptr, std::memory_order_relaxed), std::memory_order_relaxed);
	this->v_previous = extension->v_weak_pointers.v_previous;
	this->v_next = static_cast<t_weak_pointer<T_type>*>(&extension->v_weak_pointers);
	this->v_previous->v_next = this->v_next->v_previous = this;
}

template<typename T_type>
t_object<T_type>* t_weak_pointer<T_type>::f_detach()
{
	if (!v_target) return nullptr;
	auto extension = v_target->v_extension.load(std::memory_order_relaxed);
	std::lock_guard lock(extension->v_weak_pointers__mutex);
	this->v_previous->v_next = this->v_next;
	this->v_next->v_previous = this->v_previous;
	if (extension->v_weak_pointers.v_next == static_cast<t_weak_pointer*>(&extension->v_weak_pointers)) return extension->v_weak_pointers__cycle.v_p.exchange(nullptr, std::memory_order_relaxed);
	return nullptr;
}

template<typename T_type>
t_weak_pointer<T_type>::t_weak_pointer(t_object<T_type>* a_target, bool a_final) : v_final(a_final)
{
	t_root<t_slot<T_type>> p = a_target;
	std::lock_guard lock(f_engine<T_type>()->v_object__reviving__mutex);
	f_attach(p);
}

template<typename T_type>
t_weak_pointer<T_type>::~t_weak_pointer()
{
	f_engine<T_type>()->v_object__reviving__mutex.lock();
	auto p = f_detach();
	f_engine<T_type>()->v_object__reviving__mutex.unlock();
	if (p) t_slot<T_type>::t_decrements::f_push(p);
}

template<typename T_type>
t_object<T_type>* t_weak_pointer<T_type>::f_target() const
{
	f_engine<T_type>()->v_object__reviving__mutex.lock();
	f_engine<T_type>()->v_object__reviving = true;
	t_thread<T_type>::v_current->f_revive();
	auto p = v_target;
	f_engine<T_type>()->v_object__reviving__mutex.unlock();
	return t_root<t_slot<T_type>>(p);
}

template<typename T_type>
void t_weak_pointer<T_type>::f_target__(t_object<T_type>* a_p)
{
	t_root<t_slot<T_type>> p = a_p;
	f_engine<T_type>()->v_object__reviving__mutex.lock();
	auto q = f_detach();
	v_target = a_p;
	f_attach(p);
	f_engine<T_type>()->v_object__reviving__mutex.unlock();
	if (q) t_slot<T_type>::t_decrements::f_push(q);
}

}

#endif
