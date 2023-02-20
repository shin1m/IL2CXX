#include "../../src/utilities.h"

//JS funcs
//extern void mono_wasm_invoke_js_with_args_ref (int js_handle, MonoString **method, MonoArray **args, int *is_exception, MonoObject **result);
//extern void mono_wasm_get_object_property_ref (int js_handle, MonoString **propertyName, int *is_exception, MonoObject **result);
//extern void mono_wasm_get_by_index_ref (int js_handle, int property_index, int *is_exception, MonoObject **result);
//extern void mono_wasm_set_object_property_ref (int js_handle, MonoString **propertyName, MonoObject **value, int createIfNotExist, int hasOwnProperty, int *is_exception, MonoObject **result);
//extern void mono_wasm_set_by_index_ref (int js_handle, int property_index, MonoObject **value, int *is_exception, MonoObject **result);
//extern void mono_wasm_get_global_object_ref (MonoString **global_name, int *is_exception, MonoObject **result);
extern "C" void mono_wasm_release_cs_owned_object(int js_handle);
//extern void mono_wasm_create_cs_owned_object_ref (MonoString **core_name, MonoArray **args, int *is_exception, MonoObject** result);
//extern void mono_wasm_typed_array_to_array_ref (int js_handle, int *is_exception, MonoObject **result);
//extern void mono_wasm_typed_array_from_ref (int ptr, int begin, int end, int bytes_per_element, int type, int *is_exception, MonoObject** result);

extern "C" void mono_wasm_bind_js_function(t_System_2eString** function_name, t_System_2eString** module_name, void* signature, int* function_js_handle, int* is_exception, t__object** result);
extern "C" void mono_wasm_invoke_bound_function(int function_js_handle, void* data);
extern "C" void mono_wasm_bind_cs_function(t_System_2eString** fully_qualified_name, int signature_hash, void* signatures, int* is_exception, t__object** result);
extern "C" void mono_wasm_marshal_promise(void* data);

// Blazor specific custom routines - see dotnet_support.js for backing code
//extern "C" void* mono_wasm_invoke_js_blazor(t_System_2eString** exceptionMessage, void* callInfo, void* arg0, void* arg1, void* arg2);
