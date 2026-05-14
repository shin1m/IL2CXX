#include "../../src/utilities.h"

//JS funcs
extern "C" void mono_wasm_release_cs_owned_object(int js_handle);
extern "C" void mono_wasm_resolve_or_reject_promise(void *args);
extern "C" void mono_wasm_cancel_promise(int task_holder_gc_handle);
extern "C" void mono_wasm_console_clear();
extern "C" void mono_wasm_set_entrypoint_breakpoint(int entry_point_metadata_token);
extern "C" void mono_wasm_trace_logger(const char *log_domain, const char *log_level, const char *message, /*mono_bool*/int fatal, void *user_data);
extern "C" void mono_wasm_invoke_js_function(int function_js_handle, void *args);
extern "C" void* mono_wasm_bind_js_import_ST(void *signature);
extern "C" void mono_wasm_invoke_jsimport_ST(int function_handle, void *args);
extern "C" void il2cxx_js_synchronization_context_notify();

void mono_wasm_bind_assembly_exports (char *assembly_name);
t__runtime_method_info* mono_wasm_assembly_get_entry_point (char *assembly_name, int auto_insert_breakpoint);
t__runtime_method_info* mono_wasm_get_assembly_export (char *assembly_name, char *ns, char *classname, char *methodname, int signature_hash);
