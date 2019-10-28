#include "type.h"
#include <algorithm>
#include <exception>
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

}
