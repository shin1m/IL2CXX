#include <algorithm>
#include <exception>
#include <iostream>
#include <limits>
#include <map>
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

struct t__type;

std::u16string f__u16string(const std::string& a_x)
{
	std::vector<char16_t> cs;
	std::mbstate_t state{{}};
	auto p = a_x.c_str();
	auto q = p + a_x.size() + 1;
	while (p < q) {
		char16_t c;
		auto n = std::mbrtoc16(&c, p, q - p, &state);
		switch (n) {
		case size_t(-3):
			cs.push_back(c);
			break;
		case size_t(-2):
			break;
		case size_t(-1):
			++p;
			break;
		case 0:
			cs.push_back('\0');
			++p;
			break;
		default:
			cs.push_back(c);
			p += n;
			break;
		}
	}
	return {cs.data(), cs.size() - 1};
}

}
