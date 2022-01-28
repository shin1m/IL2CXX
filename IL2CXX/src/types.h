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
	using t_object<t__type>::t_object;
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

struct t__custom_attribute;

struct t__member_info : t__object
{
	t__type* v__declaring_type;
	std::u16string_view v__name;
	int32_t v__attributes;
	t__custom_attribute* const* v__custom_attributes = nullptr;

	t__member_info(t__type* a_type, t__type* a_declaring_type, std::u16string_view a_name, int32_t a_attributes, t__custom_attribute* const* a_custom_attributes);
};

struct t__abstract_type : t__member_info
{
	using t__member_info::t__member_info;
};

struct t__field_info : t__member_info
{
	using t__member_info::t__member_info;
};

struct t__runtime_field_info : t__field_info
{
	t__type* v__field_type;

	t__runtime_field_info(t__type* a_type, t__type* a_declaring_type, std::u16string_view a_name, int32_t a_attributes, t__custom_attribute* const* a_custom_attributes, t__type* a_field_type, void*(*a_address)(void*)) : t__field_info(a_type, a_declaring_type, a_name, a_attributes, a_custom_attributes), v__field_type(a_field_type), f_address(a_address)
	{
	}
	void* (*f_address)(void*);
};

struct t__runtime_parameter_info
{
	static constexpr t__runtime_parameter_info* v__empty_parameters[] = {nullptr};

	int32_t v__attributes;
	t__abstract_type* v__parameter_type;
	void* v__default_value;
};

struct t__method_base : t__member_info
{
	t__runtime_parameter_info* const* v__parameters;
	t__object*(*v__invoke)(t__object*, int32_t, t__object*, t__object*, t__object*);

	t__method_base(t__type* a_type, t__type* a_declaring_type, std::u16string_view a_name, int32_t a_attributes, t__custom_attribute* const* a_custom_attributes, t__runtime_parameter_info* const* a_parameters, t__object*(*a_invoke)(t__object*, int32_t, t__object*, t__object*, t__object*)) : t__member_info(a_type, a_declaring_type, a_name, a_attributes, a_custom_attributes), v__parameters(a_parameters), v__invoke(a_invoke)
	{
	}
};

struct t__constructor_info : t__method_base
{
	using t__method_base::t__method_base;
};

struct t__runtime_constructor_info : t__constructor_info
{
	static t__object* f_create(t__runtime_constructor_info* RECYCLONE__SPILL a_this, int32_t a_binding_flags, t__object* RECYCLONE__SPILL a_binder, t__object* RECYCLONE__SPILL a_parameters, t__object* RECYCLONE__SPILL a_culture);

	t__object*(*v__create)(t__runtime_constructor_info*, int32_t, t__object*, t__object*, t__object*);

	t__runtime_constructor_info(t__type* a_type, t__type* a_declaring_type, std::u16string_view a_name, int32_t a_attributes, t__custom_attribute* const* a_custom_attributes, t__runtime_parameter_info* const* a_parameters, t__object*(*a_invoke)(t__object*, int32_t, t__object*, t__object*, t__object*), t__object*(*a_create)(t__runtime_constructor_info*, int32_t, t__object*, t__object*, t__object*)) : t__constructor_info(a_type, a_declaring_type, a_name, a_attributes, a_custom_attributes, a_parameters, a_invoke), v__create(a_create)
	{
	}
};

struct t__method_info : t__method_base
{
	using t__method_base::t__method_base;
};

struct t__runtime_method_info : t__method_info
{
	void* v__function;
#ifdef __EMSCRIPTEN__
	t__object*(*v__wasm_invoke)(t__object*, void**);
#endif
	t__runtime_method_info* v__generic_definition;
	t__abstract_type* const* v__generic_arguments;
	t__runtime_method_info* const* v__generic_methods;

	t__runtime_method_info(
		t__type* a_type,
		t__type* a_declaring_type, std::u16string_view a_name,
		int32_t a_attributes,
		t__custom_attribute* const* a_custom_attributes,
		t__runtime_parameter_info* const* a_parameters,
		t__object*(*a_invoke)(t__object*, int32_t, t__object*, t__object*, t__object*), void* a_function,
#ifdef __EMSCRIPTEN__
		t__object*(*a_wasm_invoke)(t__object*, void**),
#endif
		t__runtime_method_info* a_generic_definition,
		t__abstract_type* const* a_generic_arguments,
		t__runtime_method_info* const* a_generic_methods
	) : t__method_info(a_type, a_declaring_type, a_name, a_attributes, a_custom_attributes, a_parameters, a_invoke), v__function(a_function),
#ifdef __EMSCRIPTEN__
		v__wasm_invoke(a_wasm_invoke),
#endif
		v__generic_definition(a_generic_definition),
		v__generic_arguments(a_generic_arguments),
		v__generic_methods(a_generic_methods)
	{
	}
};

struct t__property_info : t__member_info
{
	using t__member_info::t__member_info;
};

struct t__runtime_property_info : t__property_info
{
	t__type* v__property_type;
	t__runtime_parameter_info* const* v__parameters;
	t__runtime_method_info* v__get;
	t__runtime_method_info* v__set;

	t__runtime_property_info(t__type* a_type, t__type* a_declaring_type, std::u16string_view a_name, int32_t a_attributes, t__custom_attribute* const* a_custom_attributes, t__type* a_property_type, t__runtime_parameter_info* const* a_parameters, t__runtime_method_info* a_get, t__runtime_method_info* a_set) : t__property_info(a_type, a_declaring_type, a_name, a_attributes, a_custom_attributes), v__property_type(a_property_type), v__parameters(a_parameters), v__get(a_get), v__set(a_set)
	{
	}
};

struct t__assembly : t__object
{
};

struct t__runtime_assembly : t__assembly
{
	std::u16string_view v__full_name;
	std::u16string_view v__name;
	t__runtime_method_info* v__entry_point;
	t__type* const* v__exported_types;

	t__runtime_assembly(t__type* a_type, std::u16string_view a_full_name, std::u16string_view a_name, t__runtime_method_info* a_entry_point, t__type* const* a_exported_types);
};

struct t__type : t__abstract_type
{
	static constexpr t__type* v__empty_types[] = {nullptr};
	static constexpr t__runtime_field_info* v__empty_fields[] = {nullptr};
	static constexpr t__runtime_constructor_info* v__empty_constructors[] = {nullptr};
	static constexpr t__runtime_method_info* v__empty_methods[] = {nullptr};
	static constexpr t__runtime_property_info* v__empty_properties[] = {nullptr};
	static void f_be(t__object* a_p, t__type* a_type)
	{
		a_p->v_type = a_type;
	}

	t__type* v__base;
	t__type* const* v__interfaces;
	std::map<t__type*, std::pair<void**, void**>> v__interface_to_methods;
	t__runtime_assembly* v__assembly;
	std::u16string_view v__namespace;
	std::u16string_view v__full_name;
	std::u16string_view v__display_name;
	uint8_t v__managed : 1;
	uint8_t v__value_type : 1;
	uint8_t v__array : 1;
	uint8_t v__enum : 1;
	uint8_t v__by_ref : 1;
	uint8_t v__pointer : 1;
	uint8_t v__by_ref_like : 1;
	uint8_t v__cor_element_type;
	uint8_t v__type_code;
	size_t v__size;
	size_t v__managed_size = 0;
	size_t v__unmanaged_size = 0;
	size_t v__slots;
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
			void* v__invoke_static;
			void* v__invoke_unmanaged;
		};
		t__type* v__underlying = nullptr;
	};
	t__type* v__generic_definition = nullptr;
	t__abstract_type* const* v__generic_arguments;
	t__type* const* v__generic_types;
	t__runtime_field_info* const* v__fields = nullptr;
	t__runtime_constructor_info* const* v__constructors = nullptr;
	t__runtime_method_info* const* v__methods = nullptr;
	t__runtime_property_info* const* v__properties = nullptr;

	t__type(
		t__type* a_type, t__type* a_base,
		t__type* const* a_interfaces, std::map<t__type*, std::pair<void**, void**>>&& a_interface_to_methods,
		t__runtime_assembly* a_assembly,
		std::u16string_view a_namespace, std::u16string_view a_name, std::u16string_view a_full_name, std::u16string_view a_display_name,
		int32_t a_attribute_flags,
		t__custom_attribute* const* a_custom_attributes,
		bool a_managed, bool a_value_type, bool a_array, bool a_enum, bool a_by_ref, bool a_pointer, bool a_by_ref_like,
		size_t a_size,
		size_t a_slots,
		t__type* a_szarray
	) : t__abstract_type(a_type, nullptr, a_name, a_attribute_flags, a_custom_attributes), v__base(a_base),
	v__interfaces(a_interfaces), v__interface_to_methods(std::move(a_interface_to_methods)),
	v__assembly(a_assembly),
	v__namespace(a_namespace), v__full_name(a_full_name), v__display_name(a_display_name),
	v__managed(a_managed), v__value_type(a_value_type), v__array(a_array), v__enum(a_enum), v__by_ref(a_by_ref), v__pointer(a_pointer), v__by_ref_like(a_by_ref_like),
	v__size(a_size),
	v__slots(a_slots),
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
	RECYCLONE__ALWAYS_INLINE void f_finish(t_object<t__type>* RECYCLONE__SPILL a_p)
	{
		a_p->f_be(this);
	}
	static void f_do_scan(t_object<t__type>* a_this, t_scan<t__type> a_scan);
	void (*f_scan)(t_object<t__type>*, t_scan<t__type>) = f_do_scan;
	void f_finalize(t_object<t__type>* a_this, t_scan<t__type> a_scan)
	{
		f_scan(a_this, a_scan);
	}
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
	static t__object* f_do_box(void* a_p);
	t__object* (*f_box)(void*) = f_do_box;
	static void* f_do_unbox(t__object*& a_this);
	static void* f_do_unbox_value(t__object*& a_this);
	void* (*f_unbox)(t__object*&) = f_do_unbox;
	static void f_do_to_unmanaged(const t__object* a_this, void* a_p);
	static void f_do_to_unmanaged_blittable(const t__object* a_this, void* a_p);
	void (*f_to_unmanaged)(const t__object*, void*) = f_do_to_unmanaged;
	static void f_do_from_unmanaged(t__object* a_this, const void* a_p);
	static void f_do_from_unmanaged_blittable(t__object* a_this, const void* a_p);
	void (*f_from_unmanaged)(t__object*, const void*) = f_do_from_unmanaged;
	static void f_do_destroy_unmanaged(void* a_p);
	static void f_do_destroy_unmanaged_blittable(void* a_p);
	void (*f_destroy_unmanaged)(void*) = f_do_destroy_unmanaged;
	bool f_is(t__abstract_type* a_type) const;
	bool f_assignable_to_value(t__type* a_type) const
	{
		assert(a_type->v__value_type);
		return this == a_type || !a_type->v__enum && this == a_type->v__underlying;
	}
	bool f_assignable_to_array(t__type* a_type) const
	{
		assert(a_type->v__array);
		return v__array && v__rank == a_type->v__rank && v__element->f_assignable_to(a_type->v__element);
	}
	bool f_assignable_to_variant(t__type* a_type) const;
	const std::pair<void**, void**>* f_implementation(t__type* a_interface) const;
	bool f_assignable_to_interface(t__type* a_type) const
	{
		return f_is(a_type) || f_implementation(a_type);
	}
	bool f_assignable_to_variant_interface(t__type* a_type) const;
	bool f_assignable_to(t__type* a_type) const;
	t__object* f_new_zerod();
	static constexpr int32_t bf_declared_only = 2;
	static constexpr int32_t bf_instance = 4;
	static constexpr int32_t bf_static = 8;
	static constexpr int32_t bf_public = 16;
	static constexpr int32_t bf_non_public = 32;
	static constexpr int32_t bf_flatten_hierarchy = 64;
	template<typename T>
	void f_each_field(int32_t a_flags, T a_do)
	{
		constexpr int32_t fa_private = 1;
		constexpr int32_t fa_public = 6;
		constexpr int32_t fa_access_mask = 7;
		constexpr int32_t fa_static = 16;
		auto type = this;
		while (a_flags & (bf_instance | bf_static)) {
			if (auto p = type->v__fields) for (; *p; ++p) {
				auto x = *p;
				if (!(a_flags & bf_instance) && !(x->v__attributes & fa_static)) continue;
				if (!(a_flags & bf_static) && x->v__attributes & fa_static) continue;
				if (!(a_flags & bf_public) && (x->v__attributes & fa_access_mask) == fa_public) continue;
				if (a_flags & bf_non_public) {
					if (type != this && x->v__attributes & fa_static && (x->v__attributes & fa_access_mask) <= fa_private) continue;
				} else {
					if ((x->v__attributes & fa_access_mask) != fa_public) continue;
				}
				if (!a_do(x)) return;
			}
			type = type->v__base;
			if (!type) break;
			if (a_flags & bf_declared_only) a_flags &= ~bf_instance;
			if (!(a_flags & bf_flatten_hierarchy)) a_flags &= ~bf_static;
		}
	}
	static constexpr int32_t ma_public = 6;
	static constexpr int32_t ma_access_mask = 7;
	static constexpr int32_t ma_static = 16;
	template<typename T>
	void f_each_constructor(int32_t a_flags, T a_do)
	{
		if (auto p = v__constructors) for (; *p; ++p) {
			auto x = *p;
			auto staticc = x->v__attributes & ma_static;
			if (!(a_flags & bf_instance) && !staticc) continue;
			if (!(a_flags & bf_static) && staticc) continue;
			auto publicc = (x->v__attributes & ma_access_mask) >= ma_public;
			if (!(a_flags & bf_public) && publicc) continue;
			if (!(a_flags & bf_non_public) && !publicc) continue;
			if (!a_do(x)) return;
		}
	}
	template<typename T>
	void f_each_method(int32_t a_flags, T a_do)
	{
		constexpr int32_t ma_private_scope = 0;
		constexpr int32_t ma_private = 1;
		auto type = this;
		while (a_flags & (bf_instance | bf_static)) {
			if (auto p = type->v__methods) for (; *p; ++p) {
				auto x = *p;
				auto staticc = x->v__attributes & ma_static;
				if (!(a_flags & bf_instance) && !staticc) continue;
				if (!(a_flags & bf_static) && staticc) continue;
				auto publicc = (x->v__attributes & ma_access_mask) >= ma_public;
				if (!(a_flags & bf_public) && publicc) continue;
				if (a_flags & bf_non_public) {
					if (type != this && staticc && (x->v__attributes & ma_access_mask) <= ma_private) continue;
				} else {
					if (!publicc) continue;
				}
				if (!a_do(x)) return;
			}
			type = type->v__base;
			if (!type) break;
			if (a_flags & bf_declared_only) a_flags &= ~bf_instance;
			if (!(a_flags & bf_flatten_hierarchy)) a_flags &= ~bf_static;
		}
	}
	template<typename T>
	void f_each_property(int32_t a_flags, T a_do)
	{
		constexpr int32_t ma_private_scope = 0;
		constexpr int32_t ma_private = 1;
		constexpr int32_t ma_public = 6;
		constexpr int32_t ma_access_mask = 7;
		constexpr int32_t ma_static = 16;
		auto type = this;
		while (a_flags & (bf_instance | bf_static)) {
			if (auto p = type->v__properties) for (; *p; ++p) {
				auto x = *p;
				auto get = x->v__get;
				auto set = x->v__set;
				auto staticc = (get ? get : set)->v__attributes & ma_static;
				if (!(a_flags & bf_instance) && !staticc) continue;
				if (!(a_flags & bf_static) && staticc) continue;
				auto publicc = std::max(get ? get->v__attributes & ma_access_mask : ma_private_scope, set ? set->v__attributes & ma_access_mask : ma_private_scope) >= ma_public;
				if (!(a_flags & bf_public) && publicc) continue;
				if (a_flags & bf_non_public) {
					if (type != this && staticc && std::min(get ? get->v__attributes & ma_access_mask : ma_public, set ? set->v__attributes & ma_access_mask : ma_public) <= ma_private) continue;
				} else {
					if (!publicc) continue;
				}
				if (!a_do(x)) return;
			}
			type = type->v__base;
			if (!type) break;
			if (a_flags & bf_declared_only) a_flags &= ~bf_instance;
			if (!(a_flags & bf_flatten_hierarchy)) a_flags &= ~bf_static;
		}
	}
};

struct t__generic_parameter : t__abstract_type
{
	int32_t v__parameter_attributes;
	int32_t v__position;

	t__generic_parameter(t__type* a_type, std::u16string_view a_name, int32_t a_attribute_flags, t__custom_attribute* const* a_custom_attributes, int32_t a_parameter_attributes, int32_t a_position) : t__abstract_type(a_type, nullptr, a_name, a_attribute_flags, a_custom_attributes), v__parameter_attributes(a_parameter_attributes), v__position(a_position)
	{
	}
};

struct t__generic_type_parameter : t__generic_parameter
{
	using t__generic_parameter::t__generic_parameter;
};

struct t__generic_method_parameter : t__generic_parameter
{
	using t__generic_parameter::t__generic_parameter;
};

struct t__custom_attribute
{
	struct t_typed
	{
		t__type* v_type;
		void* v_value;
	};
	struct t_named : t_typed
	{
		t__member_info* v_member;
	};

	static constexpr t__custom_attribute* v_empty_attributes[] = {nullptr};
	static constexpr t_typed* v_empty_cas[] = {nullptr};
	static constexpr t_named* v_empty_nas[] = {nullptr};

	t__runtime_constructor_info* v_constructor;
	t_typed* const* v_constructor_arguments;
	t_named* const* v_named_arguments;
};

inline t__member_info::t__member_info(t__type* a_type, t__type* a_declaring_type, std::u16string_view a_name, int32_t a_attributes, t__custom_attribute* const* a_custom_attributes) : t__object(-1), v__declaring_type(a_declaring_type), v__name(a_name), v__attributes(a_attributes), v__custom_attributes(a_custom_attributes)
{
	t__type::f_be(this, a_type);
}

inline t__runtime_assembly::t__runtime_assembly(t__type* a_type, std::u16string_view a_full_name, std::u16string_view a_name, t__runtime_method_info* a_entry_point, t__type* const* a_exported_types) : v__full_name(a_full_name), v__name(a_name), v__entry_point(a_entry_point), v__exported_types(a_exported_types)
{
	t__type::f_be(this, a_type);
}

struct t__type_finalizee : t__type
{
	template<typename... T_n>
	t__type_finalizee(t__type* a_type, t__type* a_base, t__type* const* a_interfaces, std::map<t__type*, std::pair<void**, void**>>&& a_interface_to_methods, T_n&&... a_n) : t__type(a_type, a_base, a_interfaces, std::move(a_interface_to_methods), std::forward<T_n>(a_n)...)
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
	return a_this->f_type()->f_implementation(&t__type_of<T_interface>::v__instance)->second[A_i];
}

template<typename T_interface, size_t A_i, typename T_r, typename... T_an>
T_r f__invoke(t__object* a_this, T_an... a_n, void** a_site)
{
	auto p = a_this->f_type()->f_implementation(&t__type_of<T_interface>::v__instance)->first[A_i];
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
	return reinterpret_cast<void**>(a_this->f_type()->f_implementation(&t__type_of<T_interface>::v__instance)->second[A_i])[A_j];
}

template<typename T_interface, size_t A_i, size_t A_j, typename T_r, typename... T_an>
T_r f__generic_invoke(t__object* a_this, T_an... a_n, void** a_site)
{
	auto p = reinterpret_cast<void**>(a_this->f_type()->f_implementation(&t__type_of<T_interface>::v__instance)->first[A_i])[A_j];
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
