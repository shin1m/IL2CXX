#include "engine.h"
#include "handles.h"
#include <iostream>
#include <limits>
#include <random>
#include <regex>
#include <climits>
#include <clocale>
#include <cuchar>
#ifdef __unix__
#include <unistd.h>
#endif
#ifdef __EMSCRIPTEN__
#include <emscripten/threading.h>
#elif defined(__unix__)
#include <dlfcn.h>
#include <gnu/lib-names.h>
#endif

namespace il2cxx
{

using namespace std::literals;

template<typename T>
struct t_stacked : T
{
	t_stacked() = default;
	t_stacked(auto&& a_value)
	{
		std::memcpy(this, const_cast<std::remove_volatile_t<std::remove_reference_t<decltype(a_value)>>*>(&a_value), sizeof(T));
	}
	t_stacked(const t_stacked& a_value) : t_stacked(static_cast<const T&>(a_value))
	{
	}
	t_stacked& operator=(auto&& a_value)
	{
		std::memcpy(this, const_cast<std::remove_volatile_t<std::remove_reference_t<decltype(a_value)>>*>(&a_value), sizeof(T));
		return *this;
	}
	t_stacked& operator=(const t_stacked& a_value)
	{
		return *this = static_cast<const T&>(a_value);
	}
	t_stacked volatile& operator=(auto&& a_value) volatile
	{
		std::memcpy(const_cast<t_stacked*>(this), const_cast<std::remove_volatile_t<std::remove_reference_t<decltype(a_value)>>*>(&a_value), sizeof(T));
		return *this;
	}
	t_stacked volatile& operator=(const t_stacked& a_value) volatile
	{
		return *this = static_cast<const T&>(a_value);
	}
	t_stacked volatile& operator=(const volatile t_stacked& a_value) volatile
	{
		return *this = const_cast<const t_stacked&>(a_value);
	}
};

inline RECYCLONE__ALWAYS_INLINE void f__copy(auto& a_field, auto&& a_value)
{
	std::memcpy(&a_field, const_cast<std::remove_volatile_t<std::remove_reference_t<decltype(a_value)>>*>(&a_value), sizeof(std::remove_reference_t<decltype(a_field)>));
}

inline RECYCLONE__ALWAYS_INLINE void f__assign(auto*& a_field, auto&& a_value)
{
	reinterpret_cast<t_slot<t__type>&>(a_field) = a_value;
}

inline RECYCLONE__ALWAYS_INLINE void f__assign(auto& a_field, auto&& a_value)
{
	a_field = std::forward<decltype(a_value)>(a_value);
}

inline RECYCLONE__ALWAYS_INLINE void f__store(auto& a_field, auto&& a_value)
{
	if (t_thread<t__type>::f_current()->f_on_stack(&a_field))
		f__copy(a_field, std::forward<decltype(a_value)>(a_value));
	else
		f__assign(a_field, std::forward<decltype(a_value)>(a_value));
}

template<typename T>
inline T* f__exchange(T*& a_target, T* a_desired)
{
	if (t_thread<t__type>::f_current()->f_on_stack(&a_target)) return std::atomic_ref(a_target).exchange(a_desired, std::memory_order_relaxed);
	if (a_desired) t_slot<t__type>::f_increment(a_desired);
	a_desired = std::atomic_ref(a_target).exchange(a_desired, std::memory_order_relaxed);
	if (a_desired) t_slot<t__type>::f_decrement(a_desired);
	return a_desired;
}

template<typename T>
inline bool f__compare_exchange(T*& a_target, T*& a_expected, T* a_desired)
{
	if (t_thread<t__type>::f_current()->f_on_stack(&a_target)) return std::atomic_ref(a_target).compare_exchange_strong(a_expected, a_desired);
	if (a_desired) t_slot<t__type>::f_increment(a_desired);
	if (std::atomic_ref(a_target).compare_exchange_strong(a_expected, a_desired)) {
		if (a_expected) t_slot<t__type>::f_decrement(a_expected);
		return true;
	} else {
		if (a_desired) t_slot<t__type>::f_decrement(a_desired);
		return false;
	}
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
	std::atomic<T*> v_initialized;
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
	f_epoch_region([this]
	{
		v_mutex.lock();
	});
	std::lock_guard lock(v_mutex, std::adopt_lock);
	if (v_initializing) return &v_p;
	v_initializing = true;
	v_p.f_initialize();
	v_initialized.store(&v_p, std::memory_order_release);
	return &v_p;
}

#ifdef __EMSCRIPTEN__
void* f_load_symbol(const std::string& a_path, const char* a_name);
#else
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
	auto handle = LoadLibraryA(a_path.c_str());
	if (handle == NULL) {
		handle = LoadLibraryA((a_path + ".dll").c_str());
		if (handle == NULL) throw std::runtime_error("unable to dlopen: " + a_path);
	}
	return GetProcAddress(handle, a_name);
#endif
}
#endif

#ifdef _MSC_VER
template<typename S, typename T, typename U>
inline std::enable_if_t<std::is_unsigned_v<U>, bool> __builtin_add_overflow(S a_x, T a_y, U* a_p)
{
	U x = a_x;
	*a_p = x + a_y;
	return *a_p < x;
}
template<typename S, typename T, typename U>
inline std::enable_if_t<std::is_unsigned_v<U>, bool> __builtin_sub_overflow(S a_x, T a_y, U* a_p)
{
	U x = a_x;
	U y = a_y;
	*a_p = x - y;
	return x < y;
}
template<typename S, typename T, typename U>
inline std::enable_if_t<std::is_unsigned_v<U>, bool> __builtin_mul_overflow(S a_x, T a_y, U* a_p)
{
	U x = a_x;
	U y = a_y;
	*a_p = x * y;
	return y > 0 && x > std::numeric_limits<U>::max() / y;
}
template<typename S, typename T, typename U>
inline std::enable_if_t<std::is_signed_v<U>, bool> __builtin_add_overflow(S a_x, T a_y, U* a_p)
{
	U x = a_x;
	U y = a_y;
	*a_p = static_cast<uintmax_t>(x) + static_cast<uintmax_t>(y);
	return ~(x ^ y) & (x ^ *a_p) & std::numeric_limits<U>::min();
}
template<typename S, typename T, typename U>
inline std::enable_if_t<std::is_signed_v<U>, bool> __builtin_sub_overflow(S a_x, T a_y, U* a_p)
{
	U x = a_x;
	U y = a_y;
	*a_p = static_cast<uintmax_t>(x) - static_cast<uintmax_t>(y);
	return (x ^ y) & (x ^ *a_p) & std::numeric_limits<U>::min();
}
template<typename S, typename T, typename U>
inline std::enable_if_t<std::is_signed_v<U>, bool> __builtin_mul_overflow(S a_x, T a_y, U* a_p)
{
	U x = a_x;
	U y = a_y;
	*a_p = static_cast<uintmax_t>(x) * static_cast<uintmax_t>(y);
	return x > 0 ? y > std::numeric_limits<U>::max() / x || y < std::numeric_limits<U>::min() / x :
		x < -1 ? y > std::numeric_limits<U>::min() / x || y < std::numeric_limits<U>::max() / x :
		x == -1 && y == std::numeric_limits<U>::min();
}
#endif

}

#define IL2CXX__AT() (__FILE__ ":" + std::to_string(__LINE__))
