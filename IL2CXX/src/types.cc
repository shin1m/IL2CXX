#include "engine.h"

namespace il2cxx
{

void t__weak_pointer::f_attach(t_root<t_slot_of<t__object>>& a_target)
{
	v_target = a_target;
	if (!v_target) return;
	auto extension = v_target->f_extension();
	std::lock_guard lock(extension->v_weak_pointers__mutex);
	if (!extension->v_weak_pointers__cycle) extension->v_weak_pointers__cycle.f_raw().store(a_target.f_raw().exchange(nullptr, std::memory_order_relaxed), std::memory_order_relaxed);
	this->v_previous = extension->v_weak_pointers.v_previous;
	this->v_next = static_cast<t__weak_pointer*>(&extension->v_weak_pointers);
	this->v_previous->v_next = this->v_next->v_previous = this;
}

t_object<t__type>* t__weak_pointer::f_detach()
{
	if (!v_target) return nullptr;
	auto extension = v_target->v_extension;
	std::lock_guard lock(extension->v_weak_pointers__mutex);
	this->v_previous->v_next = this->v_next;
	this->v_next->v_previous = this->v_previous;
	return extension->v_weak_pointers.v_next == &extension->v_weak_pointers ? extension->v_weak_pointers__cycle.f_raw().exchange(nullptr, std::memory_order_relaxed) : nullptr;
}

t__weak_pointer::t__weak_pointer(t__object* a_target, bool a_final) : v_final(a_final)
{
	t_root<t_slot_of<t__object>> p = a_target;
	f_engine()->f_revive([&](auto)
	{
		f_attach(p);
	});
}

t__weak_pointer::t__weak_pointer(t__object* a_target, t_object<t__type>* a_dependent)
{
	if (!a_target) a_dependent = nullptr;
	if (a_dependent) t_slot<t__type>::f_increment(a_dependent);
	t_root<t_slot_of<t__object>> p = a_target;
	f_engine()->f_revive([&](auto)
	{
		f_attach(p);
		v_dependent = a_dependent;
	});
}

t__weak_pointer::~t__weak_pointer()
{
	if (auto p = f_engine()->f_revive([&](auto a_revive)
	{
		auto p = f_detach();
		if (p) a_revive(p);
		return p;
	})) t_slot<t__type>::f_decrement(p);
	if (v_dependent) t_slot<t__type>::f_decrement(v_dependent);
}

std::pair<t_object<t__type>*, t_object<t__type>*> t__weak_pointer::f_get() const
{
	return f_engine()->f_revive([&](auto a_revive)
	{
		if (v_target) a_revive(v_target);
		return std::make_pair(v_target, v_dependent);
	});
}

void t__weak_pointer::f_target__(t__object* a_p)
{
	t_root<t_slot<t__type>> dependent;
	t_root<t_slot_of<t__object>> p = a_p;
	if (auto q = f_engine()->f_revive([&](auto a_revive)
	{
		if (!a_p) v_dependent = dependent.f_raw().exchange(v_dependent, std::memory_order_relaxed);
		auto q = f_detach();
		if (q) a_revive(q);
		f_attach(p);
		return q;
	})) t_slot<t__type>::f_decrement(q);
}

void t__weak_pointer::f_dependent__(t_object<t__type>* a_p)
{
	if (v_final) throw std::runtime_error("cannot have a dependent.");
	t_root<t_slot<t__type>> dependent = a_p;
	f_engine()->f_revive([&](auto)
	{
		if (v_target) v_dependent = dependent.f_raw().exchange(v_dependent, std::memory_order_relaxed);
	});
}

t__extension::~t__extension()
{
	for (auto p = v_weak_pointers.v_next; p != &v_weak_pointers; p = p->v_next) {
		p->v_target = nullptr;
		p->v_dependent = nullptr;
	}
}

void t__extension::f_detach()
{
	for (auto p = v_weak_pointers.v_next; p != &v_weak_pointers; p = p->v_next) {
		if (p->v_final) continue;
		p->v_target = nullptr;
		p->v_dependent = nullptr;
		p->v_previous->v_next = p->v_next;
		p->v_next->v_previous = p->v_previous;
	}
}

void t__extension::f_scan(t_scan<t__type> a_scan)
{
	std::lock_guard lock(v_weak_pointers__mutex);
	a_scan(v_weak_pointers__cycle);
	for (auto p = v_weak_pointers.v_next; p != &v_weak_pointers; p = p->v_next) a_scan(p->v_dependent);
}

t__extension* t__object::f_extension()
{
	auto p = std::atomic_ref(v_extension).load(std::memory_order_consume);
	if (p) return p;
	t__extension* q = nullptr;
	p = new t__extension;
	if (std::atomic_ref(v_extension).compare_exchange_strong(q, p, std::memory_order_consume)) return p;
	delete p;
	return q;
}

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

t__object* t__runtime_constructor_info::f_create(t__runtime_constructor_info* RECYCLONE__SPILL a_this, int32_t a_binding_flags, t__object* RECYCLONE__SPILL a_binder, t__object* RECYCLONE__SPILL a_parameters, t__object* RECYCLONE__SPILL a_culture)
{
	auto p = a_this->v__declaring_type->f_new_zerod();
	a_this->v__invoke(p, a_binding_flags, a_binder, a_parameters, a_culture);
	return p;
}

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

void t__type::f_do_clear_pointer(void* a_p, size_t a_n)
{
	std::fill_n(static_cast<void**>(a_p), a_n, nullptr);
}

void t__type::f_do_copy(const void* a_from, size_t a_n, void* a_to)
{
	f__copy(static_cast<const t_slot<t__type>*>(a_from), a_n, static_cast<t_slot<t__type>*>(a_to));
}

void t__type::f_do_copy_pointer(const void* a_from, size_t a_n, void* a_to)
{
	f__copy(static_cast<void* const*>(a_from), a_n, static_cast<void**>(a_to));
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

bool t__type::f_is(t__abstract_type* a_type) const
{
	auto p = this;
	do {
		if (p == a_type) return true;
		p = p->v__base;
	} while (p);
	return false;
}

bool t__type::f_assignable_to_variant(t__type* a_type) const
{
	assert(a_type->v__generic_definition);
	if (v__generic_definition != a_type->v__generic_definition) return false;
	constexpr int32_t gpa_covariant = 1;
	constexpr int32_t gpa_contravariant = 2;
	constexpr int32_t gpa_variance_mask = 3;
	for (auto a = a_type->v__generic_definition->v__generic_arguments, a0 = v__generic_arguments, a1 = a_type->v__generic_arguments; *a; ++a, ++a0, ++a1) {
		if (*a0 == *a1) continue;
		if ((*a0)->f_type() != &t__type_of<t__type>::v__instance) return false;
		auto t0 = static_cast<t__type*>(*a0);
		if (t0->v__value_type) return false;
		if ((*a1)->f_type() != &t__type_of<t__type>::v__instance) return false;
		auto t1 = static_cast<t__type*>(*a1);
		if (t1->v__value_type) return false;
		switch (static_cast<t__generic_parameter*>(*a)->v__parameter_attributes & gpa_variance_mask) {
		case gpa_covariant:
			if (t0->f_assignable_to(t1)) continue;
			break;
		case gpa_contravariant:
			if (t1->f_assignable_to(t0)) continue;
			break;
		}
		return false;
	}
	return true;
}

const std::pair<void**, void**>* t__type::f_implementation(t__type* a_interface) const
{
	auto i = v__interface_to_methods.find(a_interface);
	if (i != v__interface_to_methods.end()) return &i->second;
	if (a_interface->v__generic_definition)
		for (auto p = v__interfaces; *p; ++p)
			if ((*p)->f_assignable_to_variant(a_interface)) return &v__interface_to_methods.at(*p);
	return nullptr;
}

bool t__type::f_assignable_to_variant_interface(t__type* a_type) const
{
	assert(a_type->v__generic_definition);
	auto p = this;
	do {
		if (p == a_type || f_assignable_to_variant(a_type)) return true;
		p = p->v__base;
	} while (p);
	return f_implementation(a_type);
}

bool t__type::f_assignable_to(t__type* a_type) const
{
	if (a_type->v__value_type) return f_assignable_to_value(a_type);
	if (a_type->v__array) return f_assignable_to_array(a_type);
	constexpr int32_t ta_class_semantics_mask = 32;
	constexpr int32_t ta_interface = 32;
	if ((a_type->v__attributes & ta_class_semantics_mask) == ta_interface) return a_type->v__generic_definition ? f_assignable_to_variant_interface(a_type) : f_assignable_to_interface(a_type);
	return a_type->v__multicast_invoke && a_type->v__generic_definition && f_assignable_to_variant(a_type) || f_is(a_type);
}

t__object* t__type::f_new_zerod()
{
	auto RECYCLONE__SPILL p = f_engine()->f_allocate(v__managed_size);
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
