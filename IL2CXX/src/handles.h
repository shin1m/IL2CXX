#ifndef IL2CXX__OBJECT_H
#define IL2CXX__OBJECT_H

#include "types.h"

namespace il2cxx
{

using namespace recyclone;

struct t__handle
{
	virtual ~t__handle() = default;
	virtual t__object* f_target() const = 0;
};

struct t__normal_handle : t__handle
{
	t_root<t_slot_of<t__object>> v_target;

	t__normal_handle(t__object* a_target) : v_target(a_target)
	{
	}
	virtual t__object* f_target() const;
};

struct t__weak_handle : t__handle, t_weak_pointer
{
	t__weak_handle(t__object* a_target, bool a_final) : t_weak_pointer(a_target, a_final)
	{
	}
	virtual t__object* f_target() const;
	virtual void f_target__(t__object* a_p);
};

struct t__dependent_handle : t__weak_handle
{
	t_slot_of<t__object> v_secondary;

	t__dependent_handle(t__object* a_target, t__object* a_secondary) : t__weak_handle(a_target, false), v_secondary(a_secondary)
	{
	}
	virtual ~t__dependent_handle();
	virtual void f_target__(t__object* a_p);
	virtual void f_scan(t_scan a_scan);
};

}

#endif
