#ifndef IL2CXX__ENGINE_H
#define IL2CXX__ENGINE_H

#include "types.h"

namespace il2cxx
{

struct t_engine : recyclone::t_engine<t__type>
{
	static thread_local t__thread* v_current_thread;

	using recyclone::t_engine<t__type>::t_engine;
	RECYCLONE__ALWAYS_INLINE constexpr t__object* f_allocate(size_t a_size)
	{
		return static_cast<t__object*>(recyclone::t_engine<t__type>::f_allocate(a_size));
	}
	template<typename T>
	void f_start(t__thread* a_thread, T a_main);
	void f_background__(t__thread* a_thread, bool a_value);
	void f_priority__(t__thread* a_thread, int32_t a_value);
	template<typename T_thread, typename T_static, typename T_thread_static, typename T_main>
	int f_run(void(*a_finalize)(t_object<t__type>*), T_main a_main);
	size_t f_load_count() const;
};

template<typename T>
void t_engine::f_start(t__thread* a_thread, T a_main)
{
	recyclone::t_engine<t__type>::f_start(a_thread, [a_thread, main = std::move(a_main)]
	{
		v_current_thread = a_thread;
		try {
			main();
		} catch (t_object<t__type>*) {
		}
	});
}

inline t_engine* f_engine()
{
	return static_cast<t_engine*>(recyclone::f_engine<t__type>());
}

template<typename T>
struct t__new
{
	T* v_p;

	RECYCLONE__ALWAYS_INLINE constexpr t__new(size_t a_extra) : v_p(static_cast<T*>(f_engine()->f_allocate(sizeof(T) + a_extra)))
	{
	}
	RECYCLONE__ALWAYS_INLINE ~t__new()
	{
		t__type_of<T>::v__instance.f_finish(v_p);
	}
	constexpr operator T*() const
	{
		return v_p;
	}
	constexpr T* operator->() const
	{
		return v_p;
	}
};

template<typename T>
T* f__new_zerod()
{
	t__new<T> p(0);
	std::memset(static_cast<t_object<t__type>*>(p) + 1, 0, sizeof(T) - sizeof(t_object<t__type>));
	return p;
}

template<typename T_thread, typename T_static, typename T_thread_static, typename T_main>
int t_engine::f_run(void(*a_finalize)(t_object<t__type>*), T_main a_main)
{
	auto thread = v_current_thread = f__new_zerod<T_thread>();
	thread->v_internal = v_thread__main;
	{
		auto finalizer = f__new_zerod<T_thread>();
		std::unique_lock lock(v_finalizer__conductor.v_mutex);
		f_start(finalizer, [this, a_finalize]
		{
			T_thread_static ts;
			f_finalizer(a_finalize);
		});
		v_thread__finalizer = finalizer->v_internal;
		v_finalizer__conductor.f_wait(lock);
	}
	auto s = std::make_unique<T_static>();
	auto ts = std::make_unique<T_thread_static>();
	return f_exit(a_main());
}

}

#endif
