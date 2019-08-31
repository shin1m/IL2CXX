#include <il2cxx/engine.h>

namespace il2cxx
{

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
