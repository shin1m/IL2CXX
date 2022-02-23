#include "../src/utilities.h"

extern "C" t_System_2eString* mono_wasm_invoke_js(t_System_2eString* str, int* is_exception);
//JS funcs
extern "C" t__object* mono_wasm_invoke_js_with_args(int js_handle, t_System_2eString* method, t_System_2eArray* args, int* is_exception);
extern "C" t__object* mono_wasm_get_object_property(int js_handle, t_System_2eString* propertyName, int* is_exception);
extern "C" t__object* mono_wasm_get_by_index(int js_handle, int property_index, int* is_exception);
extern "C" t__object* mono_wasm_set_object_property(int js_handle, t_System_2eString* propertyName, t__object* value, int createIfNotExist, int hasOwnProperty, int* is_exception);
extern "C" t__object* mono_wasm_set_by_index(int js_handle, int property_index, t__object* value, int* is_exception);
extern "C" t__object* mono_wasm_get_global_object(t_System_2eString* global_name, int* is_exception);
extern "C" void* mono_wasm_release_cs_owned_object(int js_handle);
extern "C" t__object* mono_wasm_create_cs_owned_object(t_System_2eString* core_name, t_System_2eArray* args, int* is_exception);
extern "C" t__object* mono_wasm_typed_array_to_array (int js_handle, int* is_exception);
//extern "C" t__object* mono_wasm_typed_array_copy_to (int js_handle, int ptr, int begin, int end, int bytes_per_element, int *is_exception);
extern "C" t__object* mono_wasm_typed_array_from(int ptr, int begin, int end, int bytes_per_element, int type, int* is_exception);
//extern "C" t__object* mono_wasm_typed_array_copy_from (int js_handle, int ptr, int begin, int end, int bytes_per_element, int *is_exception);
//extern "C" t_System_2eString* mono_wasm_add_event_listener (int jsObjHandle, t_System_2eString *name, int weakDelegateHandle, int optionsObjHandle);
//extern "C" t_System_2eString* mono_wasm_remove_event_listener (int jsObjHandle, t_System_2eString *name, int weakDelegateHandle, int capture);
//extern "C" t_System_2eString* mono_wasm_cancel_promise (int thenable_js_handle, int *is_exception);
//extern "C" t__object* mono_wasm_web_socket_open (t_System_2eString *uri, t_System_2eArray *subProtocols, t_System_2eDelegate *on_close, int *web_socket_js_handle, int *thenable_js_handle, int *is_exception);
//extern "C" t__object* mono_wasm_web_socket_send (int webSocket_js_handle, void* buffer_ptr, int offset, int length, int message_type, int end_of_message, int *thenable_js_handle, int *is_exception);
//extern "C" t__object* mono_wasm_web_socket_receive (int webSocket_js_handle, void* buffer_ptr, int offset, int length, void* response_ptr, int *thenable_js_handle, int *is_exception);
//extern "C" t__object* mono_wasm_web_socket_close (int webSocket_js_handle, int code, t_System_2eString * reason, int wait_for_close_received, int *thenable_js_handle, int *is_exception);
//extern "C" t_System_2eString* mono_wasm_web_socket_abort (int webSocket_js_handle, int *is_exception);
extern "C" t__object* mono_wasm_compile_function(t_System_2eString* str, int* is_exception);
// Blazor specific custom routines - see dotnet_support.js for backing code
extern "C" void* mono_wasm_invoke_js_blazor(t_System_2eString** exceptionMessage, void* callInfo, void* arg0, void* arg1, void* arg2);
