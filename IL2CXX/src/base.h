#include "engine.h"
#include "handles.h"
#include "waitables.h"
#include <iostream>
#include <limits>
#include <random>
#include <regex>
#include <climits>
#include <cuchar>

#ifdef __unix__
#include <dlfcn.h>
#include <gnu/lib-names.h>
#endif
#ifdef _WIN32
#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0400
#endif
#define NOMINMAX
#include <windows.h>
#endif

namespace il2cxx
{

using namespace std::literals;

template<typename T_field, typename T_value>
RECYCLONE__ALWAYS_INLINE inline void f__store(T_field& a_field, T_value&& a_value)
{
	t_thread<t__type>::v_current->f_store(a_field, std::forward<T_value>(a_value));
}

template<typename T>
inline T* f__exchange(T*& a_target, T* a_desired)
{
	return t_thread<t__type>::v_current->f_exchange(a_target, a_desired);
}

template<typename T>
inline bool f__compare_exchange(T*& a_target, T*& a_expected, T* a_desired)
{
	return t_thread<t__type>::v_current->f_compare_exchange(a_target, a_expected, a_desired);
}

template<typename T>
struct t__finally
{
	T v_f;

	~t__finally()
	{
		v_f();
	}
};

template<typename T>
t__finally<T> f__finally(T&& a_f)
{
	return {{std::move(a_f)}};
}

template<typename T>
class t__lazy
{
	std::atomic<T*> v_initialized = nullptr;
	std::recursive_mutex v_mutex;
	T v_p;
	bool v_initializing = false;

	T* f_initialize();

public:
	T* f_get()
	{
		auto p = v_initialized.load(std::memory_order_consume);
		return p ? p : f_initialize();
	}
	T* operator->()
	{
		return f_get();
	}
};

template<typename T>
T* t__lazy<T>::f_initialize()
{
	std::lock_guard lock(v_mutex);
	if (v_initializing) return &v_p;
	v_initializing = true;
	v_p.f_initialize();
	v_initialized.store(&v_p, std::memory_order_release);
	return &v_p;
}

inline void* f_load_symbol(const std::string& a_path, const char* a_name)
{
#ifdef __unix__
	auto handle = dlopen(a_path.c_str(), RTLD_LAZY/* | RTLD_GLOBAL*/);
	if (handle == NULL) {
		handle = dlopen(a_path == "libc" ? LIBC_SO : (a_path + ".so").c_str(), RTLD_LAZY/* | RTLD_GLOBAL*/);
		if (handle == NULL) throw std::runtime_error("unable to dlopen " + a_path + ": " + dlerror());
	}
	return dlsym(handle, a_name);
#endif
#ifdef _WIN32
	auto handle = LoadLibraryA((a_path + ".dll").c_str());
	if (handle == NULL) throw std::runtime_error("unable to dlopen: " + a_path);
	return GetProcAddress(handle, a_name);
#endif
}

}
