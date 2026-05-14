#include <string>

namespace
{

struct PinvokeImport
{
	const char* v_name;
	void* v_function;

	PinvokeImport(const char* a_name, auto a_function) : v_name(a_name), v_function(reinterpret_cast<void*>(a_function))
	{
	}
};

struct PinvokeTable
{
	const char* v_name;
	const PinvokeImport* v_imports;
	size_t v_count;
};

}

extern "C"
{
#include "pinvoke-table.h"
}

namespace il2cxx
{

void* f_load_symbol(const std::string& a_path, const char* a_name)
{
	auto last0 = pinvoke_tables + sizeof(pinvoke_tables) / sizeof(PinvokeTable);
	auto i = std::lower_bound(pinvoke_tables, last0, a_path, [](auto x, auto y)
	{
		return x.v_name < y;
	});
	if (i == last0 || i->v_name != a_path) throw std::runtime_error("unable to dlopen " + a_path + ": " + a_name);
	auto last1 = i->v_imports + i->v_count;
	auto j = std::lower_bound(i->v_imports, last1, a_name, [](auto x, auto y)
	{
		return strcmp(x.v_name, y) < 0;
	});
	if (j != last1 && !strcmp(j->v_name, a_name)) return j->v_function;
	std::fprintf(stderr, "f_load_symbol failed: %s, %s\n", a_path.c_str(), a_name);
	return NULL;
}

}
