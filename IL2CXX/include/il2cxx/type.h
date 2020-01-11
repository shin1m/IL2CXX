#ifndef IL2CXX__TYPE_H
#define IL2CXX__TYPE_H

#include "object.h"
#include <map>

namespace il2cxx
{

struct t__member_info : t_object
{
};

struct t__type : t__member_info
{
	t__type* v__base;
	std::map<t__type*, void**> v__interface_to_methods;
	bool v__managed;
	size_t v__size;
	t__type* v__element;
	size_t v__rank;
	void* v__multicast_invoke;

	t__type(t__type* a_base, std::map<t__type*, void**>&& a_interface_to_methods, bool a_managed, size_t a_size, t__type* a_element = nullptr, size_t a_rank = 0, void* a_multicast_invoke = nullptr);
	t_scoped<t_slot> f__allocate(size_t a_size)
	{
		auto p = t_object::f_local_pool__allocate(a_size);
		p->v_next = nullptr;
		t_slot::f_increments()->f_push(this);
		p->v_type = this;
		return t_slot(p, t_slot::t_pass());
	}
	virtual t_scoped<t_slot> f_allocate(size_t a_size);
	virtual void f_scan(t_object* a_this, t_scan a_scan);
	virtual t_scoped<t_slot> f_clone(const t_object* a_this);
	virtual void f_register_finalize(t_object* a_this);
	virtual void f_suppress_finalize(t_object* a_this);
	virtual void f_copy(const char* a_from, size_t a_n, char* a_to);
	bool f__is(t__type* a_type) const
	{
		auto p = this;
		do {
			if (p == a_type) return true;
			p = p->v__base;
		} while (p);
		return false;
	}
	void** f__implementation(t__type* a_interface) const
	{
		auto i = v__interface_to_methods.find(a_interface);
		return i == v__interface_to_methods.end() ? nullptr : i->second;
	}
};

struct t__type_finalizee : t__type
{
	using t__type::t__type;
	t_scoped<t_slot> f__allocate(size_t a_size)
	{
		auto p = t__type::f__allocate(a_size);
		p->v_finalizee = true;
		return p;
	}
	virtual t_scoped<t_slot> f_allocate(size_t a_size);
	virtual void f_register_finalize(t_object* a_this);
	virtual void f_suppress_finalize(t_object* a_this);
};

template<typename T_interface, size_t A_i>
void* f__resolve(void*& a_site, t__type* a_type)
{
	auto p = a_type->v__interface_to_methods[&t__type_of<T_interface>::v__instance][A_i];
	a_site = p;
	return reinterpret_cast<void*(*)(void*&, t__type*)>(p)(a_site, a_type);
}

template<typename T_interface, size_t A_i, typename T_type, typename T_method, T_method A_method>
void* f__method(void*& a_site, t__type* a_type)
{
	return a_type == &t__type_of<T_type>::v__instance ? reinterpret_cast<void*>(A_method) : f__resolve<T_interface, A_i>(a_site, a_type);
}

template<typename T_interface, size_t A_i, size_t A_j>
void* f__generic_resolve(void*& a_site, t__type* a_type)
{
	auto p = reinterpret_cast<void**>(a_type->v__interface_to_methods[&t__type_of<T_interface>::v__instance][A_i])[A_j];
	a_site = p;
	return reinterpret_cast<void*(*)(void*&, t__type*)>(p)(a_site, a_type);
}

template<typename T_interface, size_t A_i, size_t A_j, typename T_type, typename T_method, T_method A_method>
void* f__generic_method(void*& a_site, t__type* a_type)
{
	return a_type == &t__type_of<T_type>::v__instance ? reinterpret_cast<void*>(A_method) : f__generic_resolve<T_interface, A_i, A_j>(a_site, a_type);
}

}

#endif
