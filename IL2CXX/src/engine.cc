#include "engine.h"

namespace il2cxx
{

RECYCLONE__THREAD t__thread* t_engine::v_current_thread;

void t_engine::f_background__(t__thread* RECYCLONE__SPILL a_thread, bool a_value)
{
	f_epoch_region([this]
	{
		v_thread__mutex.lock();
	});
	std::lock_guard lock(v_thread__mutex, std::adopt_lock);
	if (a_value == a_thread->v__background) return;
	a_thread->v__background = a_value;
	auto internal = a_thread->v_internal;
	if (!internal || internal->f_done() < 0) return;
	if (internal->f_done() > 0) throw std::runtime_error("already done.");
	internal->v_background = a_thread->v__background;
}

void t_engine::f_priority__(t__thread* RECYCLONE__SPILL a_thread, int32_t a_value)
{
	if (a_value < 0 || a_value > 4) throw std::runtime_error("invalid priority.");
	f_epoch_region([this]
	{
		v_thread__mutex.lock();
	});
	std::lock_guard lock(v_thread__mutex, std::adopt_lock);
	a_thread->v__priority = a_value;
	auto internal = a_thread->v_internal;
	if (!internal || internal->f_done() < 0) return;
	if (internal->f_done() > 0) throw std::runtime_error("already done.");
	if (!t__thread::f_priority(internal->f_handle(), a_thread->v__priority)) throw std::system_error(errno, std::generic_category());
}

size_t t_engine::f_load_count() const
{
	size_t n = 0;
	v_object__heap.f_statistics([&](auto, auto, auto a_allocated, auto a_freed)
	{
		n += a_allocated - a_freed;
	});
	return n;
}

}
