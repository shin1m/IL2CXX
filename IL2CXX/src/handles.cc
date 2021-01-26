#include "handles.h"

namespace il2cxx
{

t__object* t__normal_handle::f_target() const
{
	return v_target;
}

t__object* t__weak_handle::f_target() const
{
	return static_cast<t__object*>(t_weak_pointer::f_target());
}

void t__weak_handle::f_target__(t__object* a_p)
{
	t_weak_pointer::f_target__(a_p);
}

t__dependent_handle::~t__dependent_handle()
{
	v_secondary = nullptr;
}

void t__dependent_handle::f_target__(t__object* a_p)
{
	if (!a_p) v_secondary = nullptr;
	t__weak_handle::f_target__(a_p);
}

void t__dependent_handle::f_scan(t_scan a_scan)
{
	a_scan(v_secondary);
}

}
