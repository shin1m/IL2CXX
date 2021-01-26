#include "types.h"

namespace il2cxx
{

t__object* t__type::f_do_clone(const t__object* a_this)
{
	throw std::logic_error("not supported.");
}

void t__type::f_do_register_finalize(t__object* a_this)
{
}

void t__type::f_do_suppress_finalize(t__object* a_this)
{
}

void t__type::f_do_clear(void* a_p, size_t a_n)
{
	std::fill_n(static_cast<t_slot*>(a_p), a_n, nullptr);
}

void t__type::f_do_copy(const void* a_from, size_t a_n, void* a_to)
{
	f__copy(static_cast<const t_slot*>(a_from), a_n, static_cast<t_slot*>(a_to));
}

void t__type::f_do_to_unmanaged(const t__object* a_this, void* a_p)
{
	throw std::runtime_error("not marshalable.");
}

void t__type::f_do_to_unmanaged_blittable(const t__object* a_this, void* a_p)
{
	std::memcpy(a_p, a_this + 1, a_this->f_type()->v__unmanaged_size);
}

void t__type::f_do_from_unmanaged(t__object* a_this, const void* a_p)
{
	throw std::runtime_error("not marshalable.");
}

void t__type::f_do_from_unmanaged_blittable(t__object* a_this, const void* a_p)
{
	std::memcpy(a_this + 1, a_p, a_this->f_type()->v__unmanaged_size);
}

void t__type::f_do_destroy_unmanaged(void* a_p)
{
	throw std::runtime_error("not marshalable.");
}

void t__type::f_do_destroy_unmanaged_blittable(void* a_p)
{
}

void t__type_finalizee::f_do_register_finalize(t__object* a_this)
{
	a_this->f_finalizee__(true);
}

void t__type_finalizee::f_do_suppress_finalize(t__object* a_this)
{
	a_this->f_finalizee__(false);
}

}
