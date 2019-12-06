#include <il2cxx/thread.h>

namespace il2cxx
{

IL2CXX__PORTABLE__THREAD t_thread* t_thread::v_current;

IL2CXX__PORTABLE__THREAD t_System_2eThreading_2eThread* t_System_2eThreading_2eThread::v__current;

template<typename T>
void t_System_2eThreading_2eThread::f__start(T a_main)
{
	{
		t_epoch_region region;
		std::lock_guard<std::mutex> lock(f_engine()->v_thread__mutex);
		if (v__internal) throw std::runtime_error("already started.");
		v__internal = new t_thread();
		v__internal->v_epoch__blocking = true;
		v__internal->v_next = f_engine()->v_thread__internals;
		f_engine()->v_thread__internals = v__internal;
	}
	t_slot::v_increments->f_push(this);
	try {
		std::thread([this, main = std::move(a_main)]
		{
			t_slot::v_collector = v__internal->v_collector;
			v__internal->f_initialize();
			v__current = this;
			auto internal = v__internal;
			internal->f_epoch_leave();
			try {
				t_thread_static ts;
				main();
			} catch (...) {
			}
			f_engine()->f_pools__return();
			{
				t_epoch_region region;
				std::unique_lock<std::mutex> lock(f_engine()->v_thread__mutex);
				v__internal = nullptr;
			}
			t_slot::v_decrements->f_push(this);
			internal->f_epoch_enter();
			std::unique_lock<std::mutex> lock(f_engine()->v_thread__mutex);
			++internal->v_done;
			f_engine()->v_thread__condition.notify_all();
		}).detach();
	} catch (std::system_error&) {
		{
			t_epoch_region region;
			std::lock_guard<std::mutex> lock(f_engine()->v_thread__mutex);
			++v__internal->v_done;
			v__internal = nullptr;
		}
		t_slot::v_decrements->f_push(this);
		throw std::runtime_error("failed to create thread.");
	}
}

void t_System_2eThreading_2eThread::f__start()
{
	f__start([this]
	{
		if (v__5fdelegate->f_type()->f__is(&t__type_of<t_System_2eThreading_2eThreadStart>::v__instance))
			f_t_System_2eThreading_2eThreadStart__Invoke(v__5fdelegate);
		else
			f_t_System_2eThreading_2eParameterizedThreadStart__Invoke(v__5fdelegate, v__5fthreadStartArg);
	});
}

void t_System_2eThreading_2eThread::f__join()
{
	if (this == v__current) throw std::runtime_error("current thread can not be joined.");
	if (this == f_engine()->v_thread) throw std::runtime_error("engine thread can not be joined.");
	t_epoch_region region;
	std::unique_lock<std::mutex> lock(f_engine()->v_thread__mutex);
	while (v__internal) f_engine()->v_thread__condition.wait(lock);
}

}
