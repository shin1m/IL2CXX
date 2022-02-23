#include "declarations.h"
#include <set>
#include <emscripten.h>

#define MARSHAL_TYPE_NULL 0
#define MARSHAL_TYPE_INT 1
#define MARSHAL_TYPE_FP64 2
#define MARSHAL_TYPE_STRING 3
#define MARSHAL_TYPE_VT 4
#define MARSHAL_TYPE_DELEGATE 5
#define MARSHAL_TYPE_TASK 6
#define MARSHAL_TYPE_OBJECT 7
#define MARSHAL_TYPE_BOOL 8
#define MARSHAL_TYPE_ENUM 9
#define MARSHAL_TYPE_DATE 20
#define MARSHAL_TYPE_DATEOFFSET 21
#define MARSHAL_TYPE_URI 22
#define MARSHAL_TYPE_SAFEHANDLE 23

// typed array marshalling
#define MARSHAL_ARRAY_BYTE 10
#define MARSHAL_ARRAY_UBYTE 11
#define MARSHAL_ARRAY_UBYTE_C 12
#define MARSHAL_ARRAY_SHORT 13
#define MARSHAL_ARRAY_USHORT 14
#define MARSHAL_ARRAY_INT 15
#define MARSHAL_ARRAY_UINT 16
#define MARSHAL_ARRAY_FLOAT 17
#define MARSHAL_ARRAY_DOUBLE 18

#define MARSHAL_TYPE_FP32 24
#define MARSHAL_TYPE_UINT32 25
#define MARSHAL_TYPE_INT64 26
#define MARSHAL_TYPE_UINT64 27
#define MARSHAL_TYPE_CHAR 28
#define MARSHAL_TYPE_STRING_INTERNED 29
#define MARSHAL_TYPE_VOID 30
#define MARSHAL_TYPE_POINTER 32

// errors
#define MARSHAL_ERROR_BUFFER_TOO_SMALL 512

namespace il2cxx
{

void f__startup(void* a_bottom);

}

using namespace il2cxx;

namespace
{

template<typename T>
t__runtime_method_info* f_find_method(t__type* a_type, std::u16string_view a_name, T a_match_parameters)
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
		return a_x != a_y && std::u16string_view{&a_x->v__5ffirstChar, static_cast<size_t>(a_x->v__5fstringLength)} < std::u16string_view{&a_y->v__5ffirstChar, static_cast<size_t>(a_y->v__5fstringLength)};
	}
};

std::set<t_root<il2cxx::t_slot_of<t_System_2eString>>, t__string_less> v__interned_strings_by_value;
std::set<t_System_2eString*> v__interned_strings_by_pointer;

}

extern "C"
{

EMSCRIPTEN_KEEPALIVE void il2cxx_wasm_slot_set(il2cxx::t_slot_of<t__object>* a_slot, t__object* RECYCLONE__SPILL a_value)
{
	//std::printf("slot set: %p, %p -> %p\n", a_slot, static_cast<t__object*>(*a_slot), a_value);
	f_epoch_noiger([&]
	{
		//if (a_value && !f_engine()->f_object__find(a_value)) throw std::runtime_error("invalid object pointer");
		*a_slot = a_value;
	});
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
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_register_bundled_satellite_assemblies ()
{
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_load_runtime (const char *unused, int debug_level)
{
	f__startup(reinterpret_cast<void*>(emscripten_stack_get_base()));
	t_thread<t__type>::f_current()->f_epoch_enter();
}

EMSCRIPTEN_KEEPALIVE t__runtime_assembly*
mono_wasm_assembly_load (const char *name)
{
	return v__entry_assembly;
}

EMSCRIPTEN_KEEPALIVE t__runtime_assembly* 
mono_wasm_get_corlib ()
{
	return v__entry_assembly;
}

EMSCRIPTEN_KEEPALIVE t__type*
mono_wasm_assembly_find_class (t__runtime_assembly *assembly, const char *ns, const char *name)
{
	return f__find_type(v__name_to_type, f__u16string(std::string(ns) + '.' + name));
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

EMSCRIPTEN_KEEPALIVE t__runtime_method_info*
mono_wasm_get_delegate_invoke (t__object *delegate)
{
	return f_find_method(delegate->f_type(), u"Invoke"sv, [](auto)
	{
		return true;
	});
}

EMSCRIPTEN_KEEPALIVE t__object*
mono_wasm_box_primitive (t__type *klass, void *value, int value_size)
{
	assert (klass);
	if (klass->v__size > value_size) return nullptr;
	t__object* RECYCLONE__SPILL result;
	f_epoch_noiger([&]
	{
		result = klass->f_box(value);
	});
	return result;
}

EMSCRIPTEN_KEEPALIVE t__object*
mono_wasm_invoke_method (t__runtime_method_info *method, t__object *this_arg, void *params[], il2cxx::t_slot_of<t__object> *out_exc)
{
	t__object* RECYCLONE__SPILL result;
	f_epoch_noiger([&]
	{
		if (out_exc) *out_exc = nullptr;
		try {
			std::printf("invoke method: %s, %p, %p, %p\n", f__string(method->v__name).c_str(), method->v__wasm_invoke, this_arg, params);
			result = method->v__wasm_invoke(this_arg, params);
			std::printf("\tgot: %p (%s)\n", result, result ? f__string(result->f_type()->v__full_name).c_str() : "");
			auto s = result ? f__to_string(result) : nullptr;
			std::printf("\tstring: %s\n", s ? f__string({&s->v__5ffirstChar, static_cast<size_t>(s->v__5fstringLength)}).c_str() : nullptr);
		} catch (t__object* e) {
			std::fprintf(stderr, "\tcaught object: %p\n", e);
			if (out_exc) *out_exc = e;
			//result = f__to_string(e);
			auto s = f__to_string(e);
			std::fprintf(stderr, "\tstring: %s\n", f__string({&s->v__5ffirstChar, static_cast<size_t>(s->v__5fstringLength)}).c_str());
			result = s;
		} catch (std::exception& e) {
			std::fprintf(stderr, "\tcaught exception: %s\n", e.what());
			result = f__new_string(e.what());
			if (out_exc) *out_exc = result;
		} catch (...) {
			std::fprintf(stderr, "\tcaught unknown\n");
			result = f__new_string(u"unknown exception"sv);
			if (out_exc) *out_exc = result;
		}
	});
	return result;
}

EMSCRIPTEN_KEEPALIVE t__runtime_method_info*
mono_wasm_assembly_get_entry_point (t__runtime_assembly *assembly)
{
	auto method = assembly->v__entry_point;
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

EMSCRIPTEN_KEEPALIVE char *
mono_wasm_string_get_utf8 (t_System_2eString *str)
{
	return strdup(f__string({&str->v__5ffirstChar, static_cast<size_t>(str->v__5fstringLength)}).c_str());
}

EMSCRIPTEN_KEEPALIVE t_System_2eString *
mono_wasm_string_from_js (const char *str)
{
	if (!str) return nullptr;
	t_System_2eString* RECYCLONE__SPILL result;
	f_epoch_noiger([&]
	{
		result = f__new_string(str);
	});
	return result;
}

EMSCRIPTEN_KEEPALIVE t_System_2eString *
mono_wasm_string_from_utf16 (const char16_t * chars, int length)
{
	assert (length >= 0);
	if (!chars) return nullptr;
	t_System_2eString* RECYCLONE__SPILL result;
	f_epoch_noiger([&]
	{
		result = f__new_string({chars, static_cast<size_t>(length)});
	});
	return result;
}

EMSCRIPTEN_KEEPALIVE t__type *
mono_wasm_get_obj_class (t__object *obj)
{
	return obj ? obj->f_type() : nullptr;
}

typedef enum {
	MONO_TYPE_END        = 0x00,       /* End of List */
	MONO_TYPE_VOID       = 0x01,
	MONO_TYPE_BOOLEAN    = 0x02,
	MONO_TYPE_CHAR       = 0x03,
	MONO_TYPE_I1         = 0x04,
	MONO_TYPE_U1         = 0x05,
	MONO_TYPE_I2         = 0x06,
	MONO_TYPE_U2         = 0x07,
	MONO_TYPE_I4         = 0x08,
	MONO_TYPE_U4         = 0x09,
	MONO_TYPE_I8         = 0x0a,
	MONO_TYPE_U8         = 0x0b,
	MONO_TYPE_R4         = 0x0c,
	MONO_TYPE_R8         = 0x0d,
	MONO_TYPE_STRING     = 0x0e,
	MONO_TYPE_PTR        = 0x0f,       /* arg: <type> token */
	MONO_TYPE_BYREF      = 0x10,       /* arg: <type> token */
	MONO_TYPE_VALUETYPE  = 0x11,       /* arg: <type> token */
	MONO_TYPE_CLASS      = 0x12,       /* arg: <type> token */
	MONO_TYPE_VAR	     = 0x13,	   /* number */
	MONO_TYPE_ARRAY      = 0x14,       /* type, rank, boundsCount, bound1, loCount, lo1 */
	MONO_TYPE_GENERICINST= 0x15,	   /* <type> <type-arg-count> <type-1> \x{2026} <type-n> */
	MONO_TYPE_TYPEDBYREF = 0x16,
	MONO_TYPE_I          = 0x18,
	MONO_TYPE_U          = 0x19,
	MONO_TYPE_FNPTR      = 0x1b,	      /* arg: full method signature */
	MONO_TYPE_OBJECT     = 0x1c,
	MONO_TYPE_SZARRAY    = 0x1d,       /* 0-based one-dim-array */
	MONO_TYPE_MVAR	     = 0x1e,       /* number */
	MONO_TYPE_CMOD_REQD  = 0x1f,       /* arg: typedef or typeref token */
	MONO_TYPE_CMOD_OPT   = 0x20,       /* optional arg: typedef or typref token */
	MONO_TYPE_INTERNAL   = 0x21,       /* CLR internal type */

	MONO_TYPE_MODIFIER   = 0x40,       /* Or with the following types */
	MONO_TYPE_SENTINEL   = 0x41,       /* Sentinel for varargs method signature */
	MONO_TYPE_PINNED     = 0x45,       /* Local var that points to pinned object */

	MONO_TYPE_ENUM       = 0x55        /* an enumeration */
} MonoTypeEnum;

int
mono_wasm_marshal_type_from_mono_type (int mono_type, t__type *klass)
{
	switch (mono_type) {
	// case MONO_TYPE_CHAR: prob should be done not as a number?
	case MONO_TYPE_VOID:
		return MARSHAL_TYPE_VOID;
	case MONO_TYPE_BOOLEAN:
		return MARSHAL_TYPE_BOOL;
	case MONO_TYPE_I:	// IntPtr
	case MONO_TYPE_PTR:
		return MARSHAL_TYPE_POINTER;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
		return MARSHAL_TYPE_INT;
	case MONO_TYPE_CHAR:
		return MARSHAL_TYPE_CHAR;
	case MONO_TYPE_U4:  // The distinction between this and signed int is
						// important due to how numbers work in JavaScript
		return MARSHAL_TYPE_UINT32;
	case MONO_TYPE_I8:
		return MARSHAL_TYPE_INT64;
	case MONO_TYPE_U8:
		return MARSHAL_TYPE_UINT64;
	case MONO_TYPE_R4:
		return MARSHAL_TYPE_FP32;
	case MONO_TYPE_R8:
		return MARSHAL_TYPE_FP64;
	case MONO_TYPE_STRING:
		return MARSHAL_TYPE_STRING;
	case MONO_TYPE_SZARRAY: // simple zero based one-dim-array
		switch (klass->v__element->v__cor_element_type) {
		case MONO_TYPE_U1:
			return MARSHAL_ARRAY_UBYTE;
		case MONO_TYPE_I1:
			return MARSHAL_ARRAY_BYTE;
		case MONO_TYPE_U2:
			return MARSHAL_ARRAY_USHORT;
		case MONO_TYPE_I2:
			return MARSHAL_ARRAY_SHORT;
		case MONO_TYPE_U4:
			return MARSHAL_ARRAY_UINT;
		case MONO_TYPE_I4:
			return MARSHAL_ARRAY_INT;
		case MONO_TYPE_R4:
			return MARSHAL_ARRAY_FLOAT;
		case MONO_TYPE_R8:
			return MARSHAL_ARRAY_DOUBLE;
		default:
			return MARSHAL_TYPE_OBJECT;
		}
	default:
		if (klass == &t__type_of<t_System_2eDateTime>::v__instance) return MARSHAL_TYPE_DATE;
		if (klass == &t__type_of<t_System_2eDateTimeOffset>::v__instance) return MARSHAL_TYPE_DATEOFFSET;
		if (klass->f_is(&t__type_of<t_System_2eUri>::v__instance)) return MARSHAL_TYPE_URI;
		if (klass == &t__type_of<t_System_2eThreading_2eTasks_2eVoidTaskResult>::v__instance) return MARSHAL_TYPE_VOID;
		if (klass->v__enum) return MARSHAL_TYPE_ENUM;
		if (klass->v__value_type) return MARSHAL_TYPE_VT;
		if (klass->f_is(&t__type_of<t_System_2eDelegate>::v__instance)) return MARSHAL_TYPE_DELEGATE;
		if (klass->f_is(&t__type_of<t_System_2eThreading_2eTasks_2eTask>::v__instance)) return MARSHAL_TYPE_TASK;
		if (klass->f_is(&t__type_of<t_System_2eRuntime_2eInteropServices_2eSafeHandle>::v__instance)) return MARSHAL_TYPE_SAFEHANDLE;
		return MARSHAL_TYPE_OBJECT;
	}
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_try_unbox_primitive_and_get_type (t__object *obj, void *result, int result_capacity)
{
	if (!result || result_capacity < 16) return MARSHAL_ERROR_BUFFER_TOO_SMALL;
	auto resultP = static_cast<void**>(result);
	auto resultI = static_cast<int*>(result);
	auto resultL = static_cast<int64_t*>(result);
	if (result_capacity >= sizeof (int64_t))
		*resultL = 0;
	else if (result_capacity >= sizeof (int))
		*resultI = 0;
	if (!obj) return MARSHAL_TYPE_NULL;

	auto klass = obj->f_type();
	if (klass == &t__type_of<t_System_2eString>::v__instance) {
		*resultP = klass;
		return v__interned_strings_by_pointer.find(static_cast<t_System_2eString*>(obj)) == v__interned_strings_by_pointer.end() ? MARSHAL_TYPE_STRING : MARSHAL_TYPE_STRING_INTERNED;
	}

	auto original_klass = klass;
	if (klass->v__enum) klass = klass->v__underlying;

	int mono_type = klass->v__cor_element_type;
	if (mono_type == MONO_TYPE_GENERICINST) {
		// HACK: While the 'any other type' fallback is valid for classes, it will do the 
		//  wrong thing for structs, so we need to make sure the valuetype handler is used
		if (klass->v__value_type) mono_type = MONO_TYPE_VALUETYPE;
	}

	// FIXME: We would prefer to unbox once here but it will fail if the value isn't unboxable
	switch (mono_type) {
		case MONO_TYPE_I1:
		case MONO_TYPE_BOOLEAN:
			*resultI = *static_cast<int8_t*>(klass->f_unbox(obj));
			break;
		case MONO_TYPE_U1:
			*resultI = *static_cast<uint8_t*>(klass->f_unbox(obj));
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_CHAR:
			*resultI = *static_cast<int16_t*>(klass->f_unbox(obj));
			break;
		case MONO_TYPE_U2:
			*resultI = *static_cast<uint16_t*>(klass->f_unbox(obj));
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_I:
			*resultI = *static_cast<int32_t*>(klass->f_unbox(obj));
			break;
		case MONO_TYPE_U4:
			// FIXME: Will this behave the way we want for large unsigned values?
			*resultI = *static_cast<uint32_t*>(klass->f_unbox(obj));
			break;
		case MONO_TYPE_R4:
			*static_cast<float*>(result) = *static_cast<float*>(klass->f_unbox(obj));
			break;
		case MONO_TYPE_R8:
			*static_cast<double*>(result) = *static_cast<double*>(klass->f_unbox(obj));
			break;
		case MONO_TYPE_PTR:
			*resultL = reinterpret_cast<int64_t>(*static_cast<void**>(klass->f_unbox(obj)));
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			// FIXME: At present the javascript side of things can't handle this,
			//  but there's no reason not to future-proof this API
			*resultL = *static_cast<int64_t*>(klass->f_unbox(obj));
			break;
		case MONO_TYPE_VALUETYPE:
			{
				// Check whether this struct has special-case marshaling
				// FIXME: Do we need to null out obj before this?
				int marshal_type = mono_wasm_marshal_type_from_mono_type (mono_type, original_klass);
				if (marshal_type != MARSHAL_TYPE_VT) return marshal_type;
				// Check whether the result buffer is big enough for the struct and padding
				auto obj_size = klass->v__size;
				if (result_capacity < sizeof(t__type*) + sizeof(int) + obj_size) return MARSHAL_ERROR_BUFFER_TOO_SMALL;
				// Store a header before the struct data with the size of the data and its MonoType
				*resultP = klass;
				*reinterpret_cast<int*>(resultP + 1) = obj_size;
				std::memcpy(resultP + 2, klass->f_unbox(obj), obj_size);
				return MARSHAL_TYPE_VT;
			}
		default:
			// If we failed to do a fast unboxing, return the original type information so
			//  that the caller can do a proper, slow unboxing later
			// HACK: Store the class pointer into the result buffer so our caller doesn't
			//  have to call back into the native runtime later to get it
			*resultP = klass;
			int fallbackResultType = mono_wasm_marshal_type_from_mono_type (mono_type, original_klass);
			assert (fallbackResultType != MARSHAL_TYPE_VT);
			return fallbackResultType;
	}
	// We successfully performed a fast unboxing here so use the type information
	//  matching what we unboxed (i.e. an enum's underlying type instead of its type)
	int resultType = mono_wasm_marshal_type_from_mono_type (mono_type, original_klass);
	assert (resultType != MARSHAL_TYPE_VT);
	return resultType;
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_array_length (t_System_2eArray *array)
{
	return array->v__length;
}

EMSCRIPTEN_KEEPALIVE t__object*
mono_wasm_array_get (t_System_2eArray *array, int idx)
{
	t__object* RECYCLONE__SPILL result;
	f_epoch_noiger([&]
	{
		auto type = array->f_type();
		auto element = type->v__element;
		result = element->f_box(reinterpret_cast<char*>(array->f_bounds() + type->v__rank) + idx * element->v__size);
	});
	return result;
}

EMSCRIPTEN_KEEPALIVE t_System_2eArray*
mono_wasm_obj_array_new (int size)
{
	t_System_2eArray* RECYCLONE__SPILL result;
	f_epoch_noiger([&]
	{
		result = f__new_array<t_System_2eObject_5b_5d, t__object>(size);
	});
	return result;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_obj_array_set (t_System_2eArray *array, int idx, t__object *obj)
{
	f_epoch_noiger([&]
	{
		reinterpret_cast<il2cxx::t_slot_of<t__object>*>(array->f_bounds() + array->f_type()->v__rank)[idx] = obj;
	});
}

EMSCRIPTEN_KEEPALIVE t_System_2eArray*
mono_wasm_string_array_new (int size)
{
	t_System_2eArray* RECYCLONE__SPILL result;
	f_epoch_noiger([&]
	{
		result = f__new_array<t_System_2eString_5b_5d, t_System_2eString>(size);
	});
	return result;
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_exec_regression (int verbose_level, char *image)
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_exit (int exit_code)
{
	std::exit(exit_code);
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
mono_wasm_enable_on_demand_gc (int enable)
{
}

EMSCRIPTEN_KEEPALIVE t_System_2eString *
mono_wasm_intern_string (t_System_2eString *string) 
{
	t_System_2eString* RECYCLONE__SPILL result;
	f_epoch_noiger([&]
	{
		auto [i, b] = v__interned_strings_by_value.emplace(string);
		if (b) v__interned_strings_by_pointer.emplace(string);
		result = *i;
	});
	return result;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_string_get_data (
	t_System_2eString *string, char16_t **outChars, int *outLengthBytes, int *outIsInterned
) {
	if (!string) {
		if (outChars) *outChars = 0;
		if (outLengthBytes) *outLengthBytes = 0;
		if (outIsInterned) *outIsInterned = 1;
		return;
	}
	if (outChars) *outChars = &string->v__5ffirstChar;
	if (outLengthBytes) *outLengthBytes = string->v__5fstringLength * sizeof(char16_t);
	if (outIsInterned) *outIsInterned = v__interned_strings_by_pointer.find(string) != v__interned_strings_by_pointer.end();
}

EMSCRIPTEN_KEEPALIVE void *
mono_wasm_unbox_rooted (t__object *obj)
{
	return obj ? obj->f_type()->f_unbox(obj) : nullptr;
}

EMSCRIPTEN_KEEPALIVE int32_t mono_wasm_load_icu_data(void* pData)
{
	return 1;
}

// Int8Array 		| int8_t	| byte or SByte (signed byte)
// Uint8Array		| uint8_t	| byte or Byte (unsigned byte)
// Uint8ClampedArray| uint8_t	| byte or Byte (unsigned byte)
// Int16Array		| int16_t	| short (signed short)
// Uint16Array		| uint16_t	| ushort (unsigned short)
// Int32Array		| int32_t	| int (signed integer)
// Uint32Array		| uint32_t	| uint (unsigned integer)
// Float32Array		| float		| float
// Float64Array		| double	| double
// typed array marshalling

EMSCRIPTEN_KEEPALIVE t_System_2eArray*
mono_wasm_typed_array_new (char *arr, int length, int size, int type)
{
	t__type* typeClass;
	switch (type) {
	case MARSHAL_ARRAY_BYTE:
		typeClass = &t__type_of<t_System_2eSByte>::v__instance;
		break;
	case MARSHAL_ARRAY_SHORT:
		typeClass = &t__type_of<t_System_2eInt16>::v__instance;
		break;
	case MARSHAL_ARRAY_USHORT:
		typeClass = &t__type_of<t_System_2eUInt16>::v__instance;
		break;
	case MARSHAL_ARRAY_INT:
		typeClass = &t__type_of<t_System_2eInt32>::v__instance;
		break;
	case MARSHAL_ARRAY_UINT:
		typeClass = &t__type_of<t_System_2eUInt32>::v__instance;
		break;
	case MARSHAL_ARRAY_FLOAT:
		typeClass = &t__type_of<t_System_2eSingle>::v__instance;
		break;
	case MARSHAL_ARRAY_DOUBLE:
		typeClass = &t__type_of<t_System_2eDouble>::v__instance;
		break;
	default:
		typeClass = &t__type_of<t_System_2eByte>::v__instance;
	}
	t_System_2eArray* RECYCLONE__SPILL result;
	f_epoch_noiger([&]
	{
		if (!typeClass->v__szarray) throw std::runtime_error("no szarray: " + f__string(typeClass->v__full_name));
		result = f__new_array(typeClass, length, [&](auto a_p, auto a_n)
		{
			memcpy(a_p, arr, a_n);
		});
	});
	return result;
}

}
