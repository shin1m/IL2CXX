#include "pair.cc"

struct t_thread : t_object<t_type>
{
	recyclone::t_thread<t_type>* v_internal;

	void f_construct()
	{
	}
	void f_scan(t_scan<t_type>)
	{
	}
};

struct t_engine : recyclone::t_engine<t_type>
{
	using recyclone::t_engine<t_type>::t_engine;
	template<typename T>
	::t_thread* f_start_thread(T a_main, bool a_background = false)
	{
		auto thread = f_new<::t_thread>();
		{
			std::lock_guard lock(v_thread__mutex);
			thread->v_internal = new recyclone::t_thread<t_type>();
			thread->v_internal->v_background = a_background;
		}
		t_slot<t_type>::t_increments::f_push(thread);
		try {
			std::thread([this, thread, main = std::move(a_main)]()
			{
				v_instance = this;
				auto internal = thread->v_internal;
				{
					std::lock_guard lock(v_thread__mutex);
					internal->f_initialize(&internal);
				}
				try {
					main();
				} catch (...) {
				}
				f_object__return();
				{
					std::lock_guard lock(v_thread__mutex);
					internal->v_background = false;
					thread->v_internal = nullptr;
				}
				t_slot<t_type>::t_decrements::f_push(thread);
				internal->f_epoch_get();
				std::lock_guard lock(v_thread__mutex);
				++internal->v_done;
				v_thread__condition.notify_all();
			}).detach();
			return thread;
		} catch (...) {
			{
				std::lock_guard lock(v_thread__mutex);
				thread->v_internal->v_background = false;
				thread->v_internal->v_done = 1;
				v_thread__condition.notify_all();
			}
			t_slot<t_type>::t_decrements::f_push(thread);
			throw;
		}
	}
	void f_join(::t_thread* a_thread)
	{
		std::unique_lock lock(v_thread__mutex);
		while (a_thread->v_internal) v_thread__condition.wait(lock);
	}
	void f_start_finalizer(void(*a_finalize)(t_object<t_type>*))
	{
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
