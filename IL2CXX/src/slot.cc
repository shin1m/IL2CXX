#include <il2cxx/engine.h>

namespace il2cxx
{

void t_slot::t_increments::f_flush()
{
	auto end = v_objects + V_SIZE - 1;
	auto tail = v_tail.load(std::memory_order_relaxed);
	auto epoch = v_head.load(std::memory_order_acquire);
	if (epoch > v_objects)
		--epoch;
	else
		epoch = end;
	while (tail != epoch) {
		auto next = epoch;
		if (tail < end) {
			if (next < tail) next = end;
			++tail;
		} else {
			tail = v_objects;
		}
		while (true) {
			(*tail)->f_increment();
			if (tail == next) break;
			++tail;
		}
	}
	v_tail.store(tail, std::memory_order_release);
}

void t_slot::t_decrements::f_flush()
{
	auto end = v_objects + V_SIZE - 1;
	auto tail = v_tail.load(std::memory_order_relaxed);
	auto epoch = v_last;
	if (epoch > v_objects)
		--epoch;
	else
		epoch = end;
	while (tail != epoch) {
		auto next = epoch;
		if (tail < end) {
			if (next < tail) next = end;
			++tail;
		} else {
			tail = v_objects;
		}
		while (true) {
			(*tail)->f_decrement();
			if (tail == next) break;
			++tail;
		}
	}
	v_tail.store(tail, std::memory_order_release);
	v_last = v_head.load(std::memory_order_acquire);
}

IL2CXX__PORTABLE__THREAD t_slot::t_collector* t_slot::v_collector;
IL2CXX__PORTABLE__THREAD t_slot::t_increments* t_slot::v_increments;
IL2CXX__PORTABLE__THREAD t_slot::t_decrements* t_slot::v_decrements;

#ifndef IL2CXX__PORTABLE__SUPPORTS_THREAD_EXPORT
t_increments* t_slot::f_increments()
{
	return v_increments;
}

t_decrements* t_slot::f_decrements()
{
	return v_decrements;
}
#endif

}
