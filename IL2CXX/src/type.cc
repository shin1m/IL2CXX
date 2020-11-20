#include <il2cxx/engine.h>

namespace il2cxx
{

void t__type::f_do_scan(t_object* a_this, t_scan a_scan)
{
}

t_object* t__type::f_do_clone(const t_object* a_this)
{
	throw std::logic_error("not supported.");
}

void t__type::f_do_register_finalize(t_object* a_this)
{
}

void t__type::f_do_suppress_finalize(t_object* a_this)
{
}

void t__type::f_do_copy(const char* a_from, size_t a_n, char* a_to)
{
	f__copy(reinterpret_cast<const t_slot*>(a_from), a_n, reinterpret_cast<t_slot*>(a_to));
}

void t__type::f_do_to_unmanaged(const t_object* a_this, void* a_p)
{
	throw std::runtime_error("not marshalable.");
}

void t__type::f_do_to_unmanaged_blittable(const t_object* a_this, void* a_p)
{
	std::memcpy(a_p, a_this + 1, a_this->f_type()->v__unmanaged_size);
}

void t__type::f_do_from_unmanaged(t_object* a_this, const void* a_p)
{
	throw std::runtime_error("not marshalable.");
}

void t__type::f_do_from_unmanaged_blittable(t_object* a_this, const void* a_p)
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

void t__type_finalizee::f_do_register_finalize(t_object* a_this)
{
	a_this->v_finalizee = true;
}

void t__type_finalizee::f_do_suppress_finalize(t_object* a_this)
{
	a_this->v_finalizee = false;
}

}
