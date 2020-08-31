#ifndef IL2CXX__LIBRARY_H
#define IL2CXX__LIBRARY_H

#ifdef __unix__
#include <dlfcn.h>
#include <gnu/lib-names.h>
#endif
#ifdef _WIN32
#include <windows.h>
#endif
#include <cstdio>
#include <string>

namespace il2cxx
{

#ifdef __unix__
class t_library
{
	void* v_handle;
	void* v_symbol;

public:
	t_library(const std::string& a_path, const char* a_name) : v_handle(dlopen(a_path.c_str(), RTLD_LAZY/* | RTLD_GLOBAL*/))
	{
		if (v_handle == NULL) {
			v_handle = dlopen(a_path == "libc" ? LIBC_SO : (a_path + ".so").c_str(), RTLD_LAZY/* | RTLD_GLOBAL*/);
			if (v_handle == NULL) throw std::runtime_error("unable to dlopen " + a_path + ": " + dlerror());
		}
		v_symbol = dlsym(v_handle, a_name);
	}
	~t_library()
	{
		dlclose(v_handle);
	}
	void* f_symbol() const
	{
		return v_symbol;
	}
};
#endif

#ifdef _WIN32
class t_library
{
	HMODULE v_handle;
	FARPROC v_symbol;

public:
	t_library(const std::string& a_path, const char* a_name) : v_handle(LoadLibraryA((a_path + ".dll").c_str()))
	{
		if (v_handle == NULL) throw std::runtime_error("unable to dlopen: " + a_path);
		v_symbol = GetProcAddress(v_handle, a_name);
	}
	~t_library()
	{
		FreeLibrary(v_handle);
	}
	void* f_symbol() const
	{
		return v_symbol;
	}
};
#endif

}

#endif
