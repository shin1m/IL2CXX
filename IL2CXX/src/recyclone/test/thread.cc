#include <recyclone/engine.h>

using namespace recyclone;

struct t_engine : recyclone::t_engine
{
	using recyclone::t_engine::t_engine;
	template<typename T>
	t_thread* f_start_thread(T a_main, bool a_background = false)
	{
		t_thread* thread;
		{
			std::lock_guard lock(v_thread__mutex);
			thread = new t_thread();
			thread->v_background = a_background;
		}
		try {
			std::thread([this, thread, main = std::move(a_main)]()
			{
				auto engine = v_instance = this;
				{
					std::lock_guard lock(v_thread__mutex);
					thread->f_initialize(&engine);
				}
				main();
				f_object__return();
				thread->f_epoch_get();
				std::lock_guard lock(v_thread__mutex);
				++thread->v_done;
				v_thread__condition.notify_all();
			}).detach();
			return thread;
		} catch (...) {
			{
				std::lock_guard lock(v_thread__mutex);
				thread->v_done = 1;
				v_thread__condition.notify_all();
			}
			throw;
		}
	}
	void f_join(t_thread* a_thread)
	{
		std::unique_lock lock(v_thread__mutex);
		while (a_thread->v_done <= 0) v_thread__condition.wait(lock);
	}
	void f_start_finalizer(void(*a_finalize)(t_object*))
	{
		std::unique_lock lock(v_finalizer__conductor.v_mutex);
		v_thread__finalizer = f_start_thread([this, a_finalize]
		{
			f_finalizer(a_finalize);
		});
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
