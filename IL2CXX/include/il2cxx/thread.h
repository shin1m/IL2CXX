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
		bool v_epoch__requesting = false;
		bool v_epoch__blocking = false;
		std::mutex v_epoch__mutex;
		std::condition_variable v_epoch__done;
		t_object* volatile* v_reviving = nullptr;

		void f_initialize()
		{
			t_slot::v_increments = &v_increments;
			t_slot::v_decrements = &v_decrements;
		}
		void f_epoch_request()
		{
			std::lock_guard<std::mutex> lock(v_epoch__mutex);
			if (!v_epoch__blocking) v_epoch__requesting = true;
		}
		void f_epoch_wait()
		{
			std::unique_lock<std::mutex> lock(v_epoch__mutex);
			while (v_epoch__requesting) v_epoch__done.wait(lock);
		}
		void f_epoch_point()
		{
			std::lock_guard<std::mutex> lock(v_epoch__mutex);
			if (!v_epoch__requesting) return;
			v_increments.v_epoch = v_increments.v_head;
			v_decrements.v_epoch = v_decrements.v_head;
			v_epoch__requesting = false;
			v_epoch__done.notify_one();
		}
		void f_epoch_enter()
		{
			std::lock_guard<std::mutex> lock(v_epoch__mutex);
			v_increments.v_epoch = v_increments.v_head;
			v_decrements.v_epoch = v_decrements.v_head;
			v_epoch__blocking = true;
			if (!v_epoch__requesting) return;
			v_epoch__requesting = false;
			v_epoch__done.notify_one();
		}
		void f_epoch_leave()
		{
			std::lock_guard<std::mutex> lock(v_epoch__mutex);
			v_epoch__blocking = false;
		}
		void f_revive()
		{
			v_reviving = v_increments.v_head;
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
	static t__type_of v__instance;
};

inline void f_epoch_point()
{
	t_thread::f_current()->v_internal->f_epoch_point();
}

struct t_epoch_region
{
	t_epoch_region()
	{
		t_thread::f_current()->v_internal->f_epoch_enter();
	}
	~t_epoch_region()
	{
		t_thread::f_current()->v_internal->f_epoch_leave();
	}
};

inline void t_slot::t_collector::f_wait()
{
	t_epoch_region region;
	std::unique_lock<std::mutex> lock(v_collector__mutex);
	++v_collector__wait;
	if (!v_collector__running) {
		v_collector__running = true;
		v_collector__wake.notify_one();
	}
	v_collector__done.wait(lock);
}

}

#endif
