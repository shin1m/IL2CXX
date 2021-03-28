#ifndef IL2CXX__TYPES_H
#define IL2CXX__TYPES_H

#include <recyclone/engine.h>
#include <algorithm>

namespace il2cxx
{

using namespace recyclone;

struct t__type;

template<typename T>
using t_slot_of = recyclone::t_slot_of<T, t__type>;

struct t__object : t_object<t__type>
{
	void f__scan(t_scan<t__type>)
	{
	}
	void f_construct(t__object*) const
	{
	}
};

struct t__critical_finalizer_object : t__object
{
};

struct t__thread : t__critical_finalizer_object
{
#ifdef __unix__
	static bool f_priority(pthread_t a_handle, int32_t a_priority);
#endif
#ifdef _WIN32
	static bool f_priority(HANDLE a_handle, int32_t a_priority);
#endif

	t_thread<t__type>* v_internal;
	bool v__background;
	int32_t v__priority;

	void f_initialize()
	{
		v_internal->v_background = v__background;
		f_priority(v_internal->f_handle(), v__priority);
	}
};

struct t__member_info : t__object
{
	t__type* v__declaring_type;
	std::u16string_view v__name;

	t__member_info(t__type* a_type, t__type* a_declaring_type = nullptr, std::u16string_view a_name = {});
};

struct t__method_base : t__member_info
{
	using t__member_info::t__member_info;
};

struct t__constructor_info : t__method_base
{
	using t__method_base::t__method_base;
};

struct t__runtime_constructor_info : t__constructor_info
{
	t__object*(*v__invoke)();

	t__runtime_constructor_info(t__type* a_type, t__object*(*a_invoke)()) : t__constructor_info(a_type), v__invoke(a_invoke)
	{
	}
};

struct t__method_info : t__method_base
{
	using t__method_base::t__method_base;
};

struct t__runtime_method_info : t__method_info
{
	using t__method_info::t__method_info;
};

struct t__assembly : t__object
{
};

struct t__runtime_assembly : t__assembly
{
	std::u16string_view v__full_name;
	std::u16string_view v__name;
	t__runtime_method_info* v__entry_point;

	t__runtime_assembly(t__type* a_type, std::u16string_view a_full_name, std::u16string_view a_name, t__runtime_method_info* a_entry_point);
};

struct t__abstract_type : t__member_info
{
	using t__member_info::t__member_info;
};

struct t__type : t__abstract_type
{
	static void f_be(t__object* a_p, t__type* a_type)
	{
		a_p->v_type = a_type;
	}

	t__type* v__base;
	std::map<t__type*, std::pair<void**, void**>> v__interface_to_methods;
	t__runtime_assembly* v__assembly;
	std::u16string_view v__namespace;
	std::u16string_view v__full_name;
	std::u16string_view v__display_name;
	bool v__managed;
	bool v__value_type;
	bool v__enum;
	uint8_t v__cor_element_type;
	size_t v__size;
	size_t v__managed_size = 0;
	size_t v__unmanaged_size = 0;
	t__type* v__szarray;
	union
	{
		struct
		{
			t__type* v__element;
			size_t v__rank;
		};
		struct
		{
			void* v__multicast_invoke;
			void* v__invoke_unmanaged;
		};
		struct
		{
			const std::pair<uint64_t, std::u16string_view>* v__enum_pairs;
			size_t v__enum_count;
		};
	};
	t__runtime_constructor_info* v__default_constructor = nullptr;
	t__type* v__nullable_value = nullptr;

	t__type(
		t__type* a_type, t__type* a_base,
		std::map<t__type*, std::pair<void**, void**>>&& a_interface_to_methods,
		t__runtime_assembly* a_assembly,
		std::u16string_view a_namespace, std::u16string_view a_name, std::u16string_view a_full_name, std::u16string_view a_display_name,
		bool a_managed, bool a_value_type, bool a_enum, size_t a_size,
		t__type* a_szarray
	) : t__abstract_type(a_type, nullptr, a_name), v__base(a_base),
	v__interface_to_methods(std::move(a_interface_to_methods)),
	v__assembly(a_assembly),
	v__namespace(a_namespace), v__full_name(a_full_name), v__display_name(a_display_name),
	v__managed(a_managed), v__value_type(a_value_type), v__enum(a_enum), v__size(a_size),
	v__szarray(a_szarray)
	{
	}
	template<void (t_object<t__type>::*A_push)()>
	void f_push()
	{
	}
	void f_decrement_push()
	{
	}
	void f_cyclic_decrement_push()
	{
	}
	void f_own()
	{
	}
	RECYCLONE__ALWAYS_INLINE void f_finish(t_object<t__type>* a_p)
	{
		a_p->f_be(this);
	}
	static void f_do_scan(t_object<t__type>* a_this, t_scan<t__type> a_scan);
	void (*f_scan)(t_object<t__type>*, t_scan<t__type>) = f_do_scan;
	static t__object* f_do_clone(const t__object* a_this);
	t__object* (*f_clone)(const t__object*) = f_do_clone;
	static void f_do_register_finalize(t__object* a_this);
	void (*f_register_finalize)(t__object*) = f_do_register_finalize;
	static void f_do_suppress_finalize(t__object* a_this);
	void (*f_suppress_finalize)(t__object*) = f_do_suppress_finalize;
	static void f_do_clear(void* a_p, size_t a_n);
	void (*f_clear)(void*, size_t) = f_do_clear;
	static void f_do_copy(const void* a_from, size_t a_n, void* a_to);
	void (*f_copy)(const void*, size_t, void*) = f_do_copy;
	static void f_do_to_unmanaged(const t__object* a_this, void* a_p);
	static void f_do_to_unmanaged_blittable(const t__object* a_this, void* a_p);
	void (*f_to_unmanaged)(const t__object*, void*) = f_do_to_unmanaged;
	static void f_do_from_unmanaged(t__object* a_this, const void* a_p);
	static void f_do_from_unmanaged_blittable(t__object* a_this, const void* a_p);
	void (*f_from_unmanaged)(t__object*, const void*) = f_do_from_unmanaged;
	static void f_do_destroy_unmanaged(void* a_p);
	static void f_do_destroy_unmanaged_blittable(void* a_p);
	void (*f_destroy_unmanaged)(void*) = f_do_destroy_unmanaged;
	bool f_is(t__type* a_type) const
	{
		auto p = this;
		do {
			if (p == a_type) return true;
			p = p->v__base;
		} while (p);
		return false;
	}
	void** f_implementation(t__type* a_interface) const
	{
		auto i = v__interface_to_methods.find(a_interface);
		return i == v__interface_to_methods.end() ? nullptr : i->second.first;
	}
};

inline t__member_info::t__member_info(t__type* a_type, t__type* a_declaring_type, std::u16string_view a_name) : v__declaring_type(a_declaring_type), v__name(a_name)
{
	t__type::f_be(this, a_type);
}

inline t__runtime_assembly::t__runtime_assembly(t__type* a_type, std::u16string_view a_full_name, std::u16string_view a_name, t__runtime_method_info* a_entry_point) : v__full_name(a_full_name), v__name(a_name), v__entry_point(a_entry_point)
{
	t__type::f_be(this, a_type);
}

struct t__type_finalizee : t__type
{
	template<typename... T_n>
	t__type_finalizee(t__type* a_type, t__type* a_base, std::map<t__type*, std::pair<void**, void**>>&& a_interface_to_methods, T_n&&... a_n) : t__type(a_type, a_base, std::move(a_interface_to_methods), std::forward<T_n>(a_n)...)
	{
		f_register_finalize = f_do_register_finalize;
		f_suppress_finalize = f_do_suppress_finalize;
	}
	RECYCLONE__ALWAYS_INLINE void f_finish(t_object<t__type>* a_p)
	{
		a_p->f_finalizee__(true);
		t__type::f_finish(a_p);
	}
	static void f_do_register_finalize(t__object* a_this);
	static void f_do_suppress_finalize(t__object* a_this);
};

template<typename T>
struct t__type_of;

template<typename T_interface, size_t A_i>
void* f__resolve(t__object* a_this)
{
	return a_this->f_type()->v__interface_to_methods.at(&t__type_of<T_interface>::v__instance).second[A_i];
}

template<typename T_interface, size_t A_i, typename T_r, typename... T_an>
T_r f__invoke(t__object* a_this, T_an... a_n, void** a_site)
{
	auto p = a_this->f_type()->v__interface_to_methods.at(&t__type_of<T_interface>::v__instance).first[A_i];
	*a_site = p;
	return reinterpret_cast<T_r(*)(t__object*, T_an..., void**)>(p)(a_this, a_n..., a_site);
}

template<typename T_interface, size_t A_i, typename T_type, typename T_method, T_method A_method, typename T_r, typename... T_an>
T_r f__method(t__object* a_this, T_an... a_n, void** a_site)
{
	return a_this->f_type() == &t__type_of<T_type>::v__instance ? A_method(static_cast<T_type*>(a_this), a_n...) : f__invoke<T_interface, A_i, T_r, T_an...>(a_this, a_n..., a_site);
}

template<typename T_interface, size_t A_i, size_t A_j>
void* f__generic_resolve(t__object* a_this)
{
	return reinterpret_cast<void**>(a_this->f_type()->v__interface_to_methods.at(&t__type_of<T_interface>::v__instance).second[A_i])[A_j];
}

template<typename T_interface, size_t A_i, size_t A_j, typename T_r, typename... T_an>
T_r f__generic_invoke(t__object* a_this, T_an... a_n, void** a_site)
{
	auto p = reinterpret_cast<void**>(a_this->f_type()->v__interface_to_methods.at(&t__type_of<T_interface>::v__instance).first[A_i])[A_j];
	*a_site = p;
	return reinterpret_cast<T_r(*)(t__object*, T_an..., void**)>(p)(a_this, a_n..., a_site);
}

template<typename T_interface, size_t A_i, size_t A_j, typename T_type, typename T_method, T_method A_method, typename T_r, typename... T_an>
T_r f__generic_method(t__object* a_this, T_an... a_n, void** a_site)
{
	return a_this->f_type() == &t__type_of<T_type>::v__instance ? A_method(static_cast<T_type*>(a_this), a_n...) : f__generic_invoke<T_interface, A_i, A_j, T_r, T_an...>(a_this, a_n..., a_site);
}

template<typename T0, typename T1>
inline T1 f__copy(T0 a_in, size_t a_n, T1 a_out)
{
	return a_in < a_out ? std::copy_backward(a_in, a_in + a_n, a_out + a_n) : std::copy_n(a_in, a_n, a_out);
}

}

#endif
