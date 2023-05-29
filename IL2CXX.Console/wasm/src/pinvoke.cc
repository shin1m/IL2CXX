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

}

extern "C"
{
#include "pinvoke-table.h"
}

namespace il2cxx
{

void* f_load_symbol(const std::string& a_path, const char* a_name)
{
	size_t i = 0;
	while (true) {
		if (i >= sizeof(pinvoke_tables) / sizeof(void*)) throw std::runtime_error("unable to dlopen " + a_path + ": " + a_name);
		if (pinvoke_names[i] == a_path) break;
		++i;
	}
	for (auto p = static_cast<PinvokeImport*>(pinvoke_tables[i]); p->v_name; ++p) if (!strcmp(p->v_name, a_name)) return p->v_function;
	std::fprintf(stderr, "f_load_symbol failed: %s, %s\n", a_path.c_str(), a_name);
	return NULL;
}

}
