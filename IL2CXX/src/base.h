#include "engine.h"
#include "handles.h"
#include <iostream>
#include <limits>
#include <random>
#include <regex>
#include <climits>
#include <clocale>
#ifdef __EMSCRIPTEN__
#include <future>
#include <uchar.h>
namespace std
{
	using ::mbstate_t;
	using ::mbrtoc16;
	using ::c16rtomb;
}
#include <emscripten/threading.h>
#else
#include <cuchar>
#ifdef __unix__
#include <dlfcn.h>
#include <gnu/lib-names.h>
#endif
#endif

namespace il2cxx
{

using namespace std::literals;

template<typename T>
struct t_stacked : T
{
	t_stacked() = default;
	template<typename U>
	t_stacked(U&& a_value)
	{
		std::memcpy(this, const_cast<std::remove_volatile_t<std::remove_reference_t<U>>*>(&a_value), sizeof(T));
	}
	t_stacked(const t_stacked& a_value) : t_stacked(static_cast<const T&>(a_value))
	{
	}
	template<typename U>
	t_stacked& operator=(U&& a_value)
	{
		std::memcpy(this, const_cast<std::remove_volatile_t<std::remove_reference_t<U>>*>(&a_value), sizeof(T));
		return *this;
	}
	t_stacked& operator=(const t_stacked& a_value)
	{
		return *this = static_cast<const T&>(a_value);
	}
	template<typename U>
	t_stacked volatile& operator=(U&& a_value) volatile
	{
		std::memcpy(const_cast<t_stacked*>(this), const_cast<std::remove_volatile_t<std::remove_reference_t<U>>*>(&a_value), sizeof(T));
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

template<typename T_field, typename T_value>
inline RECYCLONE__ALWAYS_INLINE void f__copy(T_field& a_field, T_value&& a_value)
{
	std::memcpy(&a_field, const_cast<std::remove_volatile_t<std::remove_reference_t<T_value>>*>(&a_value), sizeof(T_field));
}

template<typename T>
inline RECYCLONE__ALWAYS_INLINE void f__assign(T*& a_field, T* a_value)
{
	reinterpret_cast<t_slot<t__type>&>(a_field) = a_value;
}

template<typename T_field, typename T_value>
inline RECYCLONE__ALWAYS_INLINE void f__assign(T_field& a_field, T_value&& a_value)
{
	a_field = std::forward<T_value>(a_value);
}

template<typename T_field, typename T_value>
inline RECYCLONE__ALWAYS_INLINE void f__store(T_field& a_field, T_value&& a_value)
{
	auto p = &a_field;
	if (t_thread<t__type>::f_current()->f_on_stack(p))
		std::memcpy(p, const_cast<std::remove_volatile_t<std::remove_reference_t<T_value>>*>(&a_value), sizeof(T_field));
	else
		f__assign(a_field, std::forward<T_value>(a_value));
}

template<typename T>
inline T* f__exchange(T*& a_target, T* a_desired)
{
	return t_thread<t__type>::f_current()->f_on_stack(&a_target)
		? reinterpret_cast<std::atomic<T*>&>(a_target).exchange(a_desired, std::memory_order_relaxed)
		: reinterpret_cast<t_slot_of<T>&>(a_target).f_exchange(a_desired);
}

template<typename T>
inline bool f__compare_exchange(T*& a_target, T*& a_expected, T* a_desired)
{
	return t_thread<t__type>::f_current()->f_on_stack(&a_target)
		? reinterpret_cast<std::atomic<T*>&>(a_target).compare_exchange_strong(a_expected, a_desired)
		: reinterpret_cast<t_slot_of<T>&>(a_target).f_compare_exchange(a_expected, a_desired);
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
