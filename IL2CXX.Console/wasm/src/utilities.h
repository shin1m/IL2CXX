#include "../../src/utilities.h"

//JS funcs
extern "C" void mono_wasm_release_cs_owned_object(int js_handle);
extern "C" void mono_wasm_bind_js_function(t_System_2eString** function_name, t_System_2eString** module_name, void* signature, int* function_js_handle, int* is_exception, t__object** result);
extern "C" void mono_wasm_invoke_bound_function(int function_js_handle, void* data);
extern "C" void mono_wasm_invoke_import(int fn_handle, void* data);
extern "C" void mono_wasm_bind_cs_function(t_System_2eString** fully_qualified_name, int signature_hash, void* signatures, int* is_exception, t__object** result);
extern "C" void mono_wasm_marshal_promise(void* data);
