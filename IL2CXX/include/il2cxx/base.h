#include "engine.h"
#include "library.h"
#include <iostream>
#include <limits>
#include <random>
#include <regex>
#include <stdexcept>
#include <utility>
#include <climits>
#include <cstdint>
#include <cstring>
#include <cuchar>

namespace il2cxx
{

using namespace std::literals;

std::u16string f__u16string(std::string_view a_x);
std::string f__string(std::u16string_view a_x);

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
	std::lock_guard<std::recursive_mutex> lock(v_mutex);
	if (v_initializing) return &v_p;
	v_initializing = true;
	v_p.f_initialize();
	v_initialized.store(&v_p, std::memory_order_release);
	return &v_p;
}

}
