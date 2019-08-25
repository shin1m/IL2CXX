#ifndef IL2CXX__ENGINE_H
#define IL2CXX__ENGINE_H

#include "thread.h"

namespace il2cxx
{

struct t_safe_region;

class t_engine : public t_slot::t_collector
{
	friend class t_object;
	friend class t_thread;
	friend struct t_safe_region;

	template<typename T, size_t A_size>
	struct t_pool : t_shared_pool<T, A_size>
	{
		size_t v_freed = 0;

		void f_return_all()
		{
			auto p = t_local_pool<T>::f_detach();
			while (p) {
				auto q = p;
				size_t n = 0;
				while (++n < A_size && q->v_next) q = q->v_next;
				auto p0 = p;
				p = q->v_next;
				q->v_next = nullptr;
				t_shared_pool<T, A_size>::f_free(p0, n);
			}
		}
		void f_return()
		{
			t_shared_pool<T, A_size>::f_free(t_local_pool<T>::f_detach(), v_freed);
			v_freed = 0;
		}
		void f_free(decltype(T::v_next) a_p)
		{
			assert(t_thread::v_current == nullptr);
			t_local_pool<T>::f_free(a_p);
			if (++v_freed >= A_size) f_return();
		}
		size_t f_live() const
		{
			return this->f_allocated() - this->f_freed() - v_freed;
		}
	};
public:
	struct t_options
	{
#ifdef NDEBUG
		size_t v_collector__threshold = 1024 * 16;
#else
		size_t v_collector__threshold = 64;
#endif
		size_t v_stack_size = 1 << 10;
		bool v_verbose = false;
	};

private:
	static IL2CXX__PORTABLE__THREAD size_t v_local_object__allocated;

	t_pool<t_object_and<0>, 4096> v_object__pool0;
	t_pool<t_object_and<1>, 4096> v_object__pool1;
	t_pool<t_object_and<2>, 4096> v_object__pool2;
	t_pool<t_object_and<3>, 4096> v_object__pool3;
	std::atomic<size_t> v_object__allocated = 0;
	size_t v_object__freed = 0;
	size_t v_object__lower = 0;
	bool v_object__reviving = false;
	std::mutex v_object__reviving__mutex;
	size_t v_object__release = 0;
	size_t v_object__collect = 0;
	t_thread::t_internal* v_thread__internals = new t_thread::t_internal();
	std::mutex v_thread__mutex;
	std::condition_variable v_thread__condition;
	t_scoped<t_slot_of<t_thread>> v_thread;
	t_options v_options;
	bool v_debug__stopping = false;
	size_t v_debug__safe = 0;

	void f_pools__return();
	decltype(auto) f_object__pool(std::integral_constant<size_t, 0>)
	{
		return (v_object__pool0);
	}
	decltype(auto) f_object__pool(std::integral_constant<size_t, 1>)
	{
		return (v_object__pool1);
	}
	decltype(auto) f_object__pool(std::integral_constant<size_t, 2>)
	{
		return (v_object__pool2);
	}
	decltype(auto) f_object__pool(std::integral_constant<size_t, 3>)
	{
		return (v_object__pool3);
	}
	template<size_t A_rank>
	t_object* f_object__pool__allocate();
	t_object* f_object__allocate(size_t a_size)
	{
		if (++v_local_object__allocated >= 1024) {
			v_object__allocated += 1024;
			v_local_object__allocated = 0;
		}
		auto p = new(new char[a_size]) t_object;
		p->v_rank = 4;
		return p;
	}
	void f_free(t_object* a_p)
	{
		a_p->v_count = 1;
		switch (a_p->v_rank) {
		case 0:
			v_object__pool0.f_free(a_p);
			break;
		case 1:
			v_object__pool1.f_free(a_p);
			break;
		case 2:
			v_object__pool2.f_free(a_p);
			break;
		case 3:
			v_object__pool3.f_free(a_p);
			break;
		default:
			++v_object__freed;
			delete a_p;
		}
	}
	void f_free_as_release(t_object* a_p)
	{
		++v_object__release;
		f_free(a_p);
	}
	void f_free_as_collect(t_object* a_p)
	{
		++v_object__collect;
		f_free(a_p);
	}
	void f_collector();

public:
	t_engine(const t_options& a_options, size_t a_count, char** a_arguments);
	~t_engine();
	void f_debug_safe_region_enter();
	void f_debug_safe_region_leave();
};

template<size_t A_rank>
inline t_object* t_engine::f_object__pool__allocate()
{
	auto p = f_object__pool(std::integral_constant<size_t, A_rank>()).f_allocate(false);
	if (!p) {
		f_wait();
		p = f_object__pool(std::integral_constant<size_t, A_rank>()).f_allocate();
	}
	return p;
}

inline t_engine* f_engine()
{
	return static_cast<t_engine*>(t_slot::v_collector);
}

struct t_safe_region
{
	t_safe_region()
	{
		f_engine()->f_debug_safe_region_enter();
	}
	~t_safe_region()
	{
		f_engine()->f_debug_safe_region_leave();
	}
};

template<size_t A_rank>
inline t_object* t_object::f_pool__allocate()
{
	return f_engine()->f_object__pool__allocate<A_rank>();
}

inline t_object* t_object::f_local_pool__allocate(size_t a_size)
{
	switch ((a_size - sizeof(void*) * 8 - 1) / (sizeof(void*) * 8)) {
	case 0:
		return t_local_pool<t_object_and<0>>::f_allocate(f_pool__allocate<0>);
	case 1:
		return t_local_pool<t_object_and<1>>::f_allocate(f_pool__allocate<1>);
	case 2:
		return t_local_pool<t_object_and<2>>::f_allocate(f_pool__allocate<2>);
	case 3:
		return t_local_pool<t_object_and<3>>::f_allocate(f_pool__allocate<3>);
	default:
		return f_engine()->f_object__allocate(a_size);
	}
}

template<void (t_object::*A_push)()>
inline void t_object::f_step()
{
	v_type->f_scan(this, f_push<A_push>);
	(v_type->*A_push)();
}

inline void t_object::f_decrement_step()
{
	v_type->f_scan(this, f_push_and_clear<&t_object::f_decrement_push>);
	v_type->f_finalize(this);
	v_type->f_decrement_push();
	v_color = e_color__BLACK;
	if (v_next) {
		v_next->v_previous = v_previous;
		v_previous->v_next = v_next;
	}
	f_engine()->f_free_as_release(this);
}

inline t_scoped<t_slot> t_object::f_allocate(t__type* a_type, size_t a_size)
{
	auto p = f_local_pool__allocate(a_size);
	p->v_next = nullptr;
	t_slot::f_increments()->f_push(a_type);
	p->v_type = a_type;
	return {p, t_slot::t_pass()};
}

template<typename T>
inline t_scoped<t_slot_of<T>> t_object::f_allocate(size_t a_extra)
{
	return f_allocate(&t__type_of<T>::v__instance, sizeof(T) + a_extra);
}

template<typename T>
t_scoped<t_slot_of<T>> f__new_zerod()
{
	auto p = t_object::f_allocate<T>();
	std::fill_n(reinterpret_cast<char*>(static_cast<t_object*>(p)) + sizeof(t_object), sizeof(T) - sizeof(t_object), '\0');
	return p;
}

template<typename T, typename... T_an>
t_scoped<t_slot_of<T>> f__new_constructed(T_an&&... a_n)
{
	auto p = t_object::f_allocate<T>();
	p->f__construct(std::forward<T_an>(a_n)...);
	return p;
}

template<typename T_array, typename T_element>
t_scoped<t_slot_of<T_array>> f__new_array(size_t a_length)
{
	auto p = t_object::f_allocate<T_array>(sizeof(T_element) * a_length);
	p->v__length = a_length;
	p->v__bounds[0] = {a_length, 0};
	std::fill_n(reinterpret_cast<char*>(p->f__data()), sizeof(T_element) * a_length, '\0');
	return p;
}

inline t_scoped<t_slot_of<t_System_2eString>> f__new_string(size_t a_length)
{
	auto p = t_object::f_allocate<t_System_2eString>(sizeof(char16_t) * a_length);
	p->v__length = a_length;
	return p;
}

inline t_scoped<t_slot_of<t_System_2eString>> f__string(std::u16string_view a_value)
{
	auto p = f__new_string(a_value.size());
	std::copy(a_value.begin(), a_value.end(), p->f__data());
	return p;
}

inline t__type::t__type(t__type* a_base, std::map<t_System_2eType*, void**>&& a_interface_to_methods, size_t a_size, t__type* a_element, size_t a_rank) : v__base(a_base), v__interface_to_methods(std::move(a_interface_to_methods)), v__size(a_size), v__element(a_element), v__rank(a_rank)
{
	v_type = &t__type_of<t_System_2eType>::v__instance;
}

}

#endif
