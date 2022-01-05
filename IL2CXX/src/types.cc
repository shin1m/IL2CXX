#include "types.h"

namespace il2cxx
{

#ifdef __unix__
bool t__thread::f_priority(pthread_t a_handle, int32_t a_priority)
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
#endif
#ifdef _WIN32
bool t__thread::f_priority(HANDLE a_handle, int32_t a_priority)
{
	return SetThreadPriority(a_handle, a_priority + THREAD_PRIORITY_LOWEST);
}
#endif

void t__type::f_do_scan(t_object<t__type>* a_this, t_scan<t__type> a_scan)
{
}

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
	std::fill_n(static_cast<t_slot<t__type>*>(a_p), a_n, nullptr);
}

void t__type::f_do_copy(const void* a_from, size_t a_n, void* a_to)
{
	f__copy(static_cast<const t_slot<t__type>*>(a_from), a_n, static_cast<t_slot<t__type>*>(a_to));
}

t__object* t__type::f_do_box(void* a_p)
{
	return *static_cast<t__object**>(a_p);
}

void* t__type::f_do_unbox(t__object*& a_this)
{
	return &a_this;
}

void* t__type::f_do_unbox_value(t__object*& a_this)
{
	return a_this ? a_this + 1 : nullptr;
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

bool t__type::f_assignable_to(t__type* a_type) const
{
	if (a_type->v__value_type) return f_value_assignable_to(a_type);
	if (a_type->v__array) return f_array_assignable_to(a_type);
	constexpr int32_t ta_class_semantics_mask = 32;
	constexpr int32_t ta_interface = 32;
	return (v__attributes & ta_class_semantics_mask) == ta_interface ? static_cast<bool>(f_implementation(a_type)) : f_is(a_type);
}

t__object* t__type::f_new_zerod()
{
	auto p = f_engine()->f_allocate(v__managed_size);
	std::memset(p + 1, 0, v__managed_size - sizeof(t__object));
	f_register_finalize(p);
	p->f_be(this);
	return p;
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
