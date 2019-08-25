#ifndef IL2CXX__THREAD_H
#define IL2CXX__THREAD_H

#include "object.h"

namespace il2cxx
{

struct t_thread : t_object
{
	struct t_internal
	{
		t_internal* v_next = nullptr;
		size_t v_done = 0;
		t_slot::t_collector* v_collector = t_slot::v_collector;
		t_slot::t_increments v_increments;
		t_slot::t_decrements v_decrements;
		t_object* volatile* v_reviving = nullptr;

		void f_initialize()
		{
			t_slot::v_increments = &v_increments;
			t_slot::v_decrements = &v_decrements;
		}
		void f_revive()
		{
			v_reviving = v_increments.v_head.load(std::memory_order_relaxed);
		}
	};

	static IL2CXX__PORTABLE__THREAD t_thread* v_current;

	static void f_main(t_thread* a_p);
	static t_thread* f_current()
	{
		return v_current;
	}
	static t_scoped<t_slot_of<t_thread>> f_instantiate(std::function<void()>&& a_callable);

	t_internal* v_internal;
	std::function<void()> v_callable;

	void f__construct(t_internal* a_internal, std::function<void()>&& a_callable = {})
	{
		v_internal = a_internal;
		new(&v_callable) std::function<void()>(std::move(a_callable));
	}
	void f_join();
};

template<>
struct t__type_of<t_thread> : t__type
{
	t__type_of();
	virtual void f_scan(t_object* a_this, t_scan a_scan);
	virtual void f_finalize(t_object* a_this);
	static t__type_of v__instance;
};

}

#endif
