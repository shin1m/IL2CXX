#ifndef IL2CXX__THREAD_H
#define IL2CXX__THREAD_H

#include "object.h"
#include <thread>
#include <csignal>

namespace il2cxx
{

struct t_thread
{
	static IL2CXX__PORTABLE__THREAD t_thread* v_current;

	t_thread* v_next = nullptr;
	int v_done = -1;
	t_slot::t_collector* v_collector = t_slot::v_collector;
	t_slot::t_increments v_increments;
	t_slot::t_decrements v_decrements;
#if WIN32
	HANDLE v_handle;
#else
	pthread_t v_handle;
#endif
	t_object* volatile* v_reviving = nullptr;

	void f_initialize()
	{
#if WIN32
		v_handle = GetCurrentThread();
#else
		v_handle = pthread_self();
#endif
		v_current = this;
		t_slot::v_increments = &v_increments;
		t_slot::v_decrements = &v_decrements;
		v_done = 0;
	}
	void f_epoch()
	{
		v_increments.v_epoch.store(v_increments.v_head, std::memory_order_release);
		v_decrements.v_epoch.store(v_decrements.v_head, std::memory_order_release);
	}
	void f_epoch_request()
	{
		if (v_done != 0) return;
#if WIN32
		SuspendThread(v_handle);
		f_epoch();
		ResumeThread(v_handle);
#else
		pthread_kill(v_handle, SIGUSR1);
#endif
	}
	void f_revive()
	{
		v_reviving = v_increments.v_head;
	}
};

}

#endif
