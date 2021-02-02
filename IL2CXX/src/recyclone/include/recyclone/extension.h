#ifndef RECYCLONE__EXTENSION_H
#define RECYCLONE__EXTENSION_H

#include "object.h"
#include <condition_variable>

namespace recyclone
{

template<typename T_type>
struct t_weak_pointers
{
	t_weak_pointer<T_type>* v_previous;
	t_weak_pointer<T_type>* v_next;

	t_weak_pointers() : v_previous(static_cast<t_weak_pointer<T_type>*>(this)), v_next(static_cast<t_weak_pointer<T_type>*>(this))
	{
	}
};

template<typename T_type>
class t_weak_pointer : t_weak_pointers<T_type>
{
	friend class t_weak_pointers<T_type>;
	friend class t_extension<T_type>;

	t_object<T_type>* v_target;
	bool v_final;

	void f_attach(t_root<t_slot<T_type>>& a_target);
	t_object<T_type>* f_detach();

public:
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
	if (extension->v_weak_pointers.v_next == &extension->v_weak_pointers) return extension->v_weak_pointers__cycle.v_p.exchange(nullptr, std::memory_order_relaxed);
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

template<typename T_type>
class t_extension
{
	friend class t_object<T_type>;
	friend class t_weak_pointer<T_type>;

	t_weak_pointers<T_type> v_weak_pointers;
	t_slot<T_type> v_weak_pointers__cycle{};
	std::mutex v_weak_pointers__mutex;

	~t_extension();
	void f_detach();
	void f_scan(t_scan<T_type> a_scan);

public:
	std::recursive_timed_mutex v_mutex;
	std::condition_variable_any v_condition;
};

template<typename T_type>
t_extension<T_type>::~t_extension()
{
	for (auto p = v_weak_pointers.v_next; p != &v_weak_pointers; p = p->v_next) p->v_target = nullptr;
}

template<typename T_type>
void t_extension<T_type>::f_detach()
{
	for (auto p = v_weak_pointers.v_next; p != &v_weak_pointers; p = p->v_next) {
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
	for (auto p = v_weak_pointers.v_next; p != &v_weak_pointers; p = p->v_next) p->f_scan(a_scan);
}

}

#endif
