#ifndef TEST__THREAD_H
#define TEST__THREAD_H

#include "type.h"

//! An example thread implementation.
struct t_thread : t_object<t_type>
{
	bool v_background = false;
	//! Required by recyclone.
	recyclone::t_thread<t_type>* v_internal = nullptr;

	//! Called by f_new<t_thread>(...).
	void f_construct(bool a_background)
	{
		v_background = a_background;
	}
	//! Called by t_typeof<t_thread>::f_scan(...).
	void f_scan(t_scan<t_type>)
	{
	}
	//! Called by recyclone::t_engine::f_start(...) on a new thread
	//! after the thread has been initialized and before entering a_main.
	void f_initialize()
	{
		v_internal->v_background = v_background;
	}
};

struct t_engine_with_threads : t_engine<t_type>
{
	using t_engine<t_type>::t_engine;
	template<typename T>
	::t_thread* f_start_thread(T a_main, bool a_background = false)
	{
		auto thread = f_new<::t_thread>(a_background);
		f_start(thread, std::move(a_main));
		return thread;
	}
};

struct t_engine_with_finalizer : t_engine_with_threads
{
	t_engine_with_finalizer(t_options& a_options, void(*a_finalize)(t_object<t_type>*)) : t_engine_with_threads(a_options)
	{
		// Finalizer is an instance of recyclone::t_thread.
		std::unique_lock lock(v_finalizer__conductor.v_mutex);
		v_thread__finalizer = f_start_thread([this, a_finalize]
		{
			f_finalizer(a_finalize);
		})->v_internal;
		v_finalizer__conductor.f_wait(lock);
	}
};

template<typename T_do>
void f_padding(T_do a_do)
{
	char padding[4096];
	std::memset(padding, 0, sizeof(padding));
	a_do();
}

#endif
