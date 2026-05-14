#include "declarations.h"
#include <set>
#include <emscripten.h>
#include <emscripten/stack.h>

namespace il2cxx
{

void f__startup(void* a_bottom);

}

using namespace il2cxx;

namespace
{

/*
inline void f__store(auto*& a_slot, auto* a_p) {
	if (!t_thread<t__type>::f_current()->f_on_stack(&a_slot)) {
		auto* p = a_slot;
		std::printf("store: %p, %p -> %p\n", &a_slot, p, a_p);
		if (p && !f_engine()->f_object__find(p)) throw std::runtime_error("invalid old pointer");
		if (a_p && !f_engine()->f_object__find(a_p)) throw std::runtime_error("invalid new pointer");
	}
	il2cxx::f__store(a_slot, a_p);
}
*/

t__runtime_method_info* f_find_method(t__type* a_type, std::u16string_view a_name, auto a_match_parameters)
{
	t__runtime_method_info* p = nullptr;
	a_type->f_each_method(t__type::bf_instance | t__type::bf_static | t__type::bf_public | t__type::bf_non_public, [&](auto a_x)
	{
		if (a_x->v__name != a_name || !a_match_parameters(a_x->v__parameters)) return true;
		p = a_x;
		return false;
	});
	return p;
}

struct t__string_less
{
	bool operator()(t_System_2eString* a_x, t_System_2eString* a_y) const
	{
		return f__string_view(a_x) < f__string_view(a_y);
	}
};

std::set<t_root<il2cxx::t_slot_of<t_System_2eString>>, t__string_less> v__interned_strings_by_value;
std::set<t_System_2eString*> v__interned_strings_by_pointer;

auto f_module_cctor(nullptr_t, auto... xs) -> decltype(f_t__3cModule_3e___2ecctor(xs...))
{
	f_epoch_noiger([&]
	{
		try {
			f_t__3cModule_3e___2ecctor(xs...);
		} catch (t__object* e) {
			throw std::runtime_error(f__string(f__to_string(e)).c_str());
		}
	});
}

void f_module_cctor(void*, auto...)
{
}

}

extern "C"
{

bool mono_bundled_resources_get_data_resource_values (const char *id, const uint8_t **data_out, uint32_t *size_out)
{
	return false;
}

EMSCRIPTEN_KEEPALIVE t__runtime_assembly*
mono_wasm_assembly_load (const char *name)
{
	return v__entry_assembly;
}

EMSCRIPTEN_KEEPALIVE t__type*
mono_wasm_assembly_find_class (t__runtime_assembly *assembly, const char *ns, const char *name)
{
	return f__find_type(v__name_to_type, f__u16string(*ns ? std::string(ns) + '.' + name : std::string_view(name)));
}

EMSCRIPTEN_KEEPALIVE t__runtime_method_info*
mono_wasm_assembly_find_method (t__type *klass, const char *name, int arguments)
{
	assert (klass);
	return arguments < 0 ? f_find_method(klass, f__u16string(name), [](auto)
	{
		return true;
	}) : f_find_method(klass, f__u16string(name), [&](auto a_xs)
	{
		for (int i = 0; i < arguments; ++i, ++a_xs) if (!*a_xs) return false;
		return !*a_xs;
	});
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_register_root (char *start, size_t size, const char *name)
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_deregister_root (char *addr)
{
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_add_assembly (const char *name, const unsigned char *data, unsigned int size)
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_add_satellite_assembly (const char *name, const char *culture, const unsigned char *data, unsigned int size)
{
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_setenv (const char *name, const char *value)
{
	setenv(name, value, 1);
}

EMSCRIPTEN_KEEPALIVE char *
mono_wasm_getenv (const char *name)
{
	return strdup(std::getenv(name)); // JS must free
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_load_runtime (int debug_level, int propertyCount, const char **propertyKeys, const char **propertyValues)
{
	f__startup(reinterpret_cast<void*>(emscripten_stack_get_base()));
	f_t_IL2CXX_2eJSSynchronizationContext__Install();
	t_thread<t__type>::f_current()->f_epoch_enter();
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_invoke_jsexport (t__runtime_method_info *method, void* args)
{
	f_epoch_noiger([&]
	{
		try {
			try {
				method->v__wasm_invoke(nullptr, &args);
				return;
			} catch (t__object* e) {
				std::fprintf(stderr, "\tcaught object: %s\n", f__string(f__to_string(e)).c_str());
			}
		} catch (t__object* e) {
			std::fprintf(stderr, "\tunexpected double fault: %p\n", e);
		} catch (std::exception& e) {
			std::fprintf(stderr, "\tcaught exception: %s\n", e.what());
		} catch (...) {
			std::fprintf(stderr, "\tcaught unknown\n");
		}
		abort();
	});
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_string_from_utf16_ref (const char16_t * chars, int length, t_System_2eString **result)
{
	assert (length >= 0);
	f_epoch_noiger([&]
	{
		f__store(*result, chars ? f__new_string({chars, static_cast<size_t>(length)}) : nullptr);
	});
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_exec_regression (int verbose_level, char *image)
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_exit (int exit_code)
{
	//if (exit_code == 0) f__cleanup();
	fflush (stdout);
	fflush (stderr);
	emscripten_force_exit (exit_code);
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_set_main_args (int argc, char* argv[])
{
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_strdup (const char *s)
{
	return reinterpret_cast<int>(strdup(s));
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_parse_runtime_options (int argc, char* argv[])
{
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_intern_string_ref (t_System_2eString **string)
{
	f_epoch_noiger([&]
	{
		auto [i, b] = v__interned_strings_by_value.emplace(*string);
		if (b) v__interned_strings_by_pointer.emplace(*string);
		f__store(*string, *i);
	});
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_string_get_data_ref (
	t_System_2eString **string, char16_t **outChars, int *outLengthBytes, int *outIsInterned
) {
	if (!string || !(*string)) {
		if (outChars) *outChars = 0;
		if (outLengthBytes) *outLengthBytes = 0;
		if (outIsInterned) *outIsInterned = 1;
	} else {
		if (outChars) *outChars = &(*string)->v__5ffirstChar;
		if (outLengthBytes) *outLengthBytes = (*string)->v__5fstringLength * sizeof(char16_t);
		if (outIsInterned) *outIsInterned = v__interned_strings_by_pointer.contains(*string);
	}
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_write_managed_pointer_unsafe (t__object** destination, t__object* RECYCLONE__SPILL source) {
	f_epoch_noiger([&]
	{
		f__store(*destination, source);
	});
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_copy_managed_pointer (t__object** destination, t__object** source) {
	f_epoch_noiger([&]
	{
		f__store(*destination, *source);
	});
}

#define I52_ERROR_NONE 0
#define I52_ERROR_NON_INTEGRAL 1
#define I52_ERROR_OUT_OF_RANGE 2

#define U52_MAX_VALUE ((1ULL << 53) - 1)
#define I52_MAX_VALUE ((1LL << 53) - 1)
#define I52_MIN_VALUE -I52_MAX_VALUE

EMSCRIPTEN_KEEPALIVE double mono_wasm_i52_to_f64 (int64_t *source, int *error) {
	int64_t value = *source;

	if ((value < I52_MIN_VALUE) || (value > I52_MAX_VALUE)) {
		*error = I52_ERROR_OUT_OF_RANGE;
		return NAN;
	}

	*error = I52_ERROR_NONE;
	return (double)value;
}

EMSCRIPTEN_KEEPALIVE double mono_wasm_u52_to_f64 (uint64_t *source, int *error) {
	uint64_t value = *source;

	if (value > U52_MAX_VALUE) {
		*error = I52_ERROR_OUT_OF_RANGE;
		return NAN;
	}

	*error = I52_ERROR_NONE;
	return (double)value;
}

EMSCRIPTEN_KEEPALIVE int mono_wasm_f64_to_u52 (uint64_t *destination, double value) {
	if ((value < 0) || (value > U52_MAX_VALUE))
		return I52_ERROR_OUT_OF_RANGE;
	if (floor(value) != value)
		return I52_ERROR_NON_INTEGRAL;

	*destination = (uint64_t)value;
	return I52_ERROR_NONE;
}

EMSCRIPTEN_KEEPALIVE int mono_wasm_f64_to_i52 (int64_t *destination, double value) {
	if ((value < I52_MIN_VALUE) || (value > I52_MAX_VALUE))
		return I52_ERROR_OUT_OF_RANGE;
	if (floor(value) != value)
		return I52_ERROR_NON_INTEGRAL;

	*destination = (int64_t)value;
	return I52_ERROR_NONE;
}

// JS is responsible for freeing this
EMSCRIPTEN_KEEPALIVE const char * mono_wasm_method_get_full_name (t__runtime_method_info *method) {
	return nullptr;
}

EMSCRIPTEN_KEEPALIVE const char * mono_wasm_method_get_name (t__runtime_method_info *method) {
	return nullptr;
}

EMSCRIPTEN_KEEPALIVE char * mono_wasm_method_get_name_ex (t__runtime_method_info *method) {
	return nullptr;
}

EMSCRIPTEN_KEEPALIVE float mono_wasm_get_f32_unaligned (const float *src) {
	return *src;
}

EMSCRIPTEN_KEEPALIVE double mono_wasm_get_f64_unaligned (const double *src) {
	return *src;
}

EMSCRIPTEN_KEEPALIVE int32_t mono_wasm_get_i32_unaligned (const int32_t *src) {
	return *src;
}

EMSCRIPTEN_KEEPALIVE int mono_wasm_is_zero_page_reserved () {
	// If the stack is above the first 512 bytes of memory this indicates that it is safe
	//  to optimize out null checks for operations that also do a bounds check, like string
	//  and array element loads. (We already know that Emscripten malloc will never allocate
	//  data at 0.) This is the default behavior for Emscripten release builds and is
	//  controlled by the emscripten GLOBAL_BASE option (default value 1024).
	// clang/llvm may perform this optimization if --low-memory-unused is set.
	// https://github.com/emscripten-core/emscripten/issues/19389
	return (emscripten_stack_get_base() > 512) && (emscripten_stack_get_end() > 512);
}

// this will return bool value if the object is a bool, otherwise it will return -1 or error
// we use it in Blazor's renderBatch as internal only
EMSCRIPTEN_KEEPALIVE int
mono_wasm_read_as_bool_or_null_unsafe (t__object* RECYCLONE__SPILL obj) {
	return f_epoch_noiger([&]
	{
		if (obj->f_type() != &t__type_of<t_System_2eBoolean>::v__instance) return -1;
		return static_cast<t_System_2eBoolean*>(obj)->v__value ? 1 : 0;
	});
}

EMSCRIPTEN_KEEPALIVE void il2cxx_js_synchronization_context_pump()
{
	f_epoch_noiger(f_t_IL2CXX_2eJSSynchronizationContext__Pump);
}

}

namespace il2cxx
{

t__runtime_method_info*
mono_wasm_assembly_get_entry_point (char *assembly_name, int auto_insert_breakpoint)
{
	free(assembly_name);
	auto method = v__entry_assembly->v__entry_point;
	if (!(method->v__attributes & 0x0800)) return method;
	auto name = method->v__name;
	if (name[0] != u'<' || name[name.size() - 1] != u'>') return method;
	auto type = method->v__declaring_type;
	auto match = [&](auto a_xs)
	{
		for (auto p = method->v__parameters; *p; ++p, ++a_xs) if ((*a_xs)->v__parameter_type != (*p)->v__parameter_type) return false;
		return !*a_xs;
	};
	if (auto p = f_find_method(type, std::u16string(name) + u'$', match)) return p;
	if (auto p = f_find_method(type, name.substr(1, name.size() - 2), match)) return p;;
	return method;
}

void mono_wasm_bind_assembly_exports (char *assembly_name)
{
	free(assembly_name);
	if (auto klass = mono_wasm_assembly_find_class(v__entry_assembly, "System.Runtime.InteropServices.JavaScript", "__GeneratedInitializer")) {
		if (auto method = mono_wasm_assembly_find_method(klass, "__Register_", -1)) mono_wasm_invoke_jsexport(method, nullptr);
	} else {
		f_module_cctor(nullptr);
	}
}

t__runtime_method_info* mono_wasm_get_assembly_export (char *assembly_name, char *ns, char *classname, char *methodname, int signature_hash)
{
	free(assembly_name);
	auto klass = mono_wasm_assembly_find_class(v__entry_assembly, ns, classname);
	free(ns);
	free(classname);
	assert (klass);

	char real_method_name_buffer[4096];
	snprintf(real_method_name_buffer, 4096, "__Wrapper_%s_%d", methodname, signature_hash);
	free(methodname);

	auto method = mono_wasm_assembly_find_method(klass, real_method_name_buffer, -1);
	assert (method);
	return method;
}

}
