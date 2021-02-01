#include "engine.h"

namespace il2cxx
{

thread_local t__thread* t_engine::v_current_thread;

bool t_engine::f_priority(pthread_t a_handle, int32_t a_priority)
{
	int policy;
	sched_param sp;
	if (pthread_getschedparam(a_handle, &policy, &sp)) return false;
	int max = sched_get_priority_max(policy);
	if (max == -1) return false;
	int min = sched_get_priority_min(policy);
	if (min == -1) return false;
	sp.sched_priority = a_priority * (max - min) / 4 + min;
	return !pthread_setschedparam(a_handle, policy, &sp);
}

void t_engine::f_join(t__thread* a_thread)
{
	if (a_thread == v_current_thread) throw std::runtime_error("current thread can not be joined.");
	if (a_thread->v__internal == v_thread__main) throw std::runtime_error("engine thread can not be joined.");
	std::unique_lock lock(v_thread__mutex);
	while (a_thread->v__internal) v_thread__condition.wait(lock);
}

void t_engine::f_background__(t__thread* a_thread, bool a_value)
{
	std::lock_guard lock(v_thread__mutex);
	if (a_value == a_thread->v__background) return;
	a_thread->v__background = a_value;
	auto internal = a_thread->v__internal;
	if (!internal || internal->v_done < 0) return;
	if (internal->v_done > 0) throw std::runtime_error("already done.");
	internal->v_background = a_thread->v__background;
}

void t_engine::f_priority__(t__thread* a_thread, int32_t a_value)
{
	if (a_value < 0 || a_value > 4) throw std::runtime_error("invalid priority.");
	std::lock_guard lock(v_thread__mutex);
	a_thread->v__priority = a_value;
	auto internal = a_thread->v__internal;
	if (!internal || internal->v_done < 0) return;
	if (internal->v_done > 0) throw std::runtime_error("already done.");
	if (!f_priority(internal->v_handle, a_thread->v__priority)) throw std::system_error(errno, std::generic_category());
}

}
