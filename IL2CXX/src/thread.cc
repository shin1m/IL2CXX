#include <il2cxx/thread.h>
#include <thread>

namespace il2cxx
{

IL2CXX__PORTABLE__THREAD t_thread* t_thread::v_current;

void t_thread::f_main(t_thread* a_p)
{
	v_current = a_p;
	t_internal* internal = v_current->v_internal;
	t_slot::v_collector = internal->v_collector;
	internal->f_initialize();
	v_current->v_callable();
	v_current->v_internal = nullptr;
	t_slot::v_decrements->f_push(v_current);
	f_engine()->f_pools__return();
	std::unique_lock<std::mutex> lock(f_engine()->v_thread__mutex);
	++internal->v_done;
	f_engine()->v_thread__condition.notify_all();
}

t_scoped<t_slot_of<t_thread>> t_thread::f_instantiate(std::function<void()>&& a_callable)
{
	auto internal = new t_internal();
	auto object = f__new_constructed<t_thread>(internal, std::move(a_callable));
	{
		std::lock_guard<std::mutex> lock(f_engine()->v_thread__mutex);
		internal->v_next = f_engine()->v_thread__internals;
		f_engine()->v_thread__internals = internal;
	}
	t_slot::v_increments->f_push(object);
	try {
		std::thread(f_main, static_cast<t_thread*>(object)).detach();
	} catch (std::system_error&) {
		t_slot::v_decrements->f_push(object);
		{
			std::lock_guard<std::mutex> lock(f_engine()->v_thread__mutex);
			++internal->v_done;
		}
		throw std::runtime_error("failed to create thread.");
	}
	return object;
}

void t_thread::f_join()
{
	if (this == v_current) throw std::runtime_error("current thread can not be joined.");
	if (this == f_engine()->v_thread) throw std::runtime_error("engine thread can not be joined.");
	t_safe_region region;
	std::unique_lock<std::mutex> lock(f_engine()->v_thread__mutex);
	while (v_internal) f_engine()->v_thread__condition.wait(lock);
}

t__type_of<t_thread>::t__type_of() : t__type(&t__type_of<t_System_2eObject>::v__instance, {
}, sizeof(t_thread*))
{
}
void t__type_of<t_thread>::f_scan(t_object* a_this, t_scan a_scan)
{
}
void t__type_of<t_thread>::f_finalize(t_object* a_this)
{
}
t__type_of<t_thread> t__type_of<t_thread>::v__instance;

}
