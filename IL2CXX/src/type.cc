#include <il2cxx/engine.h>

namespace il2cxx
{

void t__type::f_scan(t_object* a_this, t_scan a_scan)
{
}

t_object* t__type::f_clone(const t_object* a_this)
{
	throw std::logic_error("not supported.");
}

void t__type::f_register_finalize(t_object* a_this)
{
}

void t__type::f_suppress_finalize(t_object* a_this)
{
}

void t__type::f_copy(const char* a_from, size_t a_n, char* a_to)
{
	f__copy(reinterpret_cast<const t_slot*>(a_from), a_n, reinterpret_cast<t_slot*>(a_to));
}

void t__type_finalizee::f_register_finalize(t_object* a_this)
{
	a_this->v_finalizee = true;
}

void t__type_finalizee::f_suppress_finalize(t_object* a_this)
{
	a_this->v_finalizee = false;
}

}
