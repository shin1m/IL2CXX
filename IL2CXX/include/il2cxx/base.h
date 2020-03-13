#include "type.h"
#include <algorithm>
#include <iostream>
#include <limits>
#include <random>
#include <stdexcept>
#include <utility>
#include <climits>
#include <cstdint>
#include <cstring>
#include <cuchar>

namespace il2cxx
{

using namespace std::literals;

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

std::u16string f__u16string(std::string_view a_x);
std::string f__string(std::u16string_view a_x);

template<typename T0, typename T1>
inline T1 f__copy(T0 a_in, size_t a_n, T1 a_out)
{
	return a_in < a_out ? std::copy_backward(a_in, a_in + a_n, a_out + a_n) : std::copy_n(a_in, a_n, a_out);
}

template<typename T0, typename T1>
inline T1 f__move(T0 a_in, size_t a_n, T1 a_out)
{
	return a_in < a_out ? std::move_backward(a_in, a_in + a_n, a_out + a_n) : std::move(a_in, a_in + a_n, a_out);
}

}
