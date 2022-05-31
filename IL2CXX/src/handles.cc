#include "handles.h"

namespace il2cxx
{

t__object* t__normal_handle::f_target() const
{
	return v_target;
}

void t__normal_handle::f_target__(t__object* a_value)
{
	v_target = a_value;
}

t__object* t__weak_handle::f_target() const
{
	return static_cast<t__object*>(t_weak_pointer<t__type>::f_get().first);
}

void t__weak_handle::f_target__(t__object* a_value)
{
	t_weak_pointer<t__type>::f_target__(a_value);
}

}
