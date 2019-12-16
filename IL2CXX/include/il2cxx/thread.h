#ifndef IL2CXX__THREAD_H
#define IL2CXX__THREAD_H

#include "object.h"
#include <thread>

namespace il2cxx
{

struct t_thread
{
	static IL2CXX__PORTABLE__THREAD t_thread* v_current;

	t_thread* v_next = nullptr;
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
		v_current = this;
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

inline void f_epoch_point()
{
	t_thread::v_current->f_epoch_point();
}

struct t_epoch_region
{
	t_epoch_region()
	{
		t_thread::v_current->f_epoch_enter();
	}
	~t_epoch_region()
	{
		t_thread::v_current->f_epoch_leave();
	}
};

inline void t_slot::t_collector::f_wait()
{
	t_epoch_region region;
	f__wait();
}

}

#endif
