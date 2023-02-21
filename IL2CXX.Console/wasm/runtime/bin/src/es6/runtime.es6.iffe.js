//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

var __dotnet_runtime = (function (exports) {
    'use strict';

    var ProductVersion = "7.0.0-dev";

    var MonoWasmThreads = true;

    var BuildConfiguration = undefined;

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    // these are our public API (except internal)
    let Module;
    let INTERNAL;
    let IMPORTS;
    // these are imported and re-exported from emscripten internals
    let ENVIRONMENT_IS_NODE;
    let ENVIRONMENT_IS_SHELL;
    let ENVIRONMENT_IS_WEB;
    let ENVIRONMENT_IS_WORKER;
    let ENVIRONMENT_IS_PTHREAD;
    const exportedRuntimeAPI = {};
    const moduleExports = {};
    let emscriptenEntrypoint;
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function set_imports_exports(imports, exports) {
        INTERNAL = exports.internal;
        IMPORTS = exports.marshaled_imports;
        Module = exports.module;
        set_environment(imports);
        ENVIRONMENT_IS_NODE = imports.isNode;
        ENVIRONMENT_IS_SHELL = imports.isShell;
        ENVIRONMENT_IS_WEB = imports.isWeb;
        ENVIRONMENT_IS_WORKER = imports.isWorker;
        ENVIRONMENT_IS_PTHREAD = imports.isPThread;
        runtimeHelpers.quit = imports.quit_;
        runtimeHelpers.ExitStatus = imports.ExitStatus;
        runtimeHelpers.requirePromise = imports.requirePromise;
    }
    function set_environment(imports) {
        ENVIRONMENT_IS_NODE = imports.isNode;
        ENVIRONMENT_IS_SHELL = imports.isShell;
        ENVIRONMENT_IS_WEB = imports.isWeb;
        ENVIRONMENT_IS_WORKER = imports.isWorker;
        ENVIRONMENT_IS_PTHREAD = imports.isPThread;
    }
    function set_emscripten_entrypoint(entrypoint) {
        emscriptenEntrypoint = entrypoint;
    }
    const initialRuntimeHelpers = {
        javaScriptExports: {},
        mono_wasm_load_runtime_done: false,
        mono_wasm_bindings_is_ready: false,
        maxParallelDownloads: 16,
        config: {
            environmentVariables: {},
        },
        diagnosticTracing: false,
    };
    const runtimeHelpers = initialRuntimeHelpers;

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    const MonoMethodNull = 0;
    const MonoObjectNull = 0;
    const MonoArrayNull = 0;
    const MonoAssemblyNull = 0;
    const MonoClassNull = 0;
    const MonoTypeNull = 0;
    const MonoStringNull = 0;
    const MonoObjectRefNull = 0;
    const MonoStringRefNull = 0;
    const JSHandleDisposed = -1;
    const JSHandleNull = 0;
    const GCHandleNull = 0;
    const VoidPtrNull = 0;
    const CharPtrNull = 0;
    const NativePointerNull = 0;
    function coerceNull(ptr) {
        if ((ptr === null) || (ptr === undefined))
            return 0;
        else
            return ptr;
    }
    // see src\mono\wasm\runtime\rollup.config.js
    // inline this, because the lambda could allocate closure on hot path otherwise
    function mono_assert(condition, messageFactory) {
        if (!condition) {
            const message = typeof messageFactory === "string"
                ? messageFactory
                : messageFactory();
            throw new Error(`Assert failed: ${message}`);
        }
    }
    // Evaluates whether a value is nullish (same definition used as the ?? operator,
    //  https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Operators/Nullish_coalescing_operator)
    function is_nullish(value) {
        return (value === undefined) || (value === null);
    }
    /// Always throws. Used to handle unreachable switch branches when TypeScript refines the type of a variable
    /// to 'never' after you handle all the cases it knows about.
    function assertNever(x) {
        throw new Error("Unexpected value: " + x);
    }
    /// returns true if the given value is not Thenable
    ///
    /// Useful if some function returns a value or a promise of a value.
    function notThenable(x) {
        return typeof x !== "object" || typeof (x.then) !== "function";
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // when the method is assigned/cached at usage, instead of being invoked directly from cwraps, it can't be marked lazy, because it would be re-bound on each call
    const fn_signatures$1 = [
        // MONO
        [true, "mono_wasm_register_root", "number", ["number", "number", "string"]],
        [true, "mono_wasm_deregister_root", null, ["number"]],
        [true, "mono_wasm_string_get_data", null, ["number", "number", "number", "number"]],
        [true, "mono_wasm_string_get_data_ref", null, ["number", "number", "number", "number"]],
        [true, "mono_wasm_set_is_debugger_attached", "void", ["bool"]],
        [true, "mono_wasm_send_dbg_command", "bool", ["number", "number", "number", "number", "number"]],
        [true, "mono_wasm_send_dbg_command_with_parms", "bool", ["number", "number", "number", "number", "number", "number", "string"]],
        [true, "mono_wasm_setenv", null, ["string", "string"]],
        [true, "mono_wasm_parse_runtime_options", null, ["number", "number"]],
        [true, "mono_wasm_strdup", "number", ["string"]],
        [true, "mono_background_exec", null, []],
        [true, "mono_set_timeout_exec", null, []],
        [true, "mono_wasm_load_icu_data", "number", ["number"]],
        [true, "mono_wasm_get_icudt_name", "string", ["string"]],
        [false, "mono_wasm_add_assembly", "number", ["string", "number", "number"]],
        [true, "mono_wasm_add_satellite_assembly", "void", ["string", "string", "number", "number"]],
        [false, "mono_wasm_load_runtime", null, ["string", "number"]],
        [true, "mono_wasm_change_debugger_log_level", "void", ["number"]],
        // BINDING
        [true, "mono_wasm_get_corlib", "number", []],
        [true, "mono_wasm_assembly_load", "number", ["string"]],
        [true, "mono_wasm_find_corlib_class", "number", ["string", "string"]],
        [true, "mono_wasm_assembly_find_class", "number", ["number", "string", "string"]],
        [true, "mono_wasm_runtime_run_module_cctor", "void", ["number"]],
        [true, "mono_wasm_find_corlib_type", "number", ["string", "string"]],
        [true, "mono_wasm_assembly_find_type", "number", ["number", "string", "string"]],
        [true, "mono_wasm_assembly_find_method", "number", ["number", "string", "number"]],
        [true, "mono_wasm_invoke_method", "number", ["number", "number", "number", "number"]],
        [false, "mono_wasm_invoke_method_ref", "void", ["number", "number", "number", "number", "number"]],
        [true, "mono_wasm_string_get_utf8", "number", ["number"]],
        [true, "mono_wasm_string_from_utf16_ref", "void", ["number", "number", "number"]],
        [true, "mono_wasm_get_obj_type", "number", ["number"]],
        [true, "mono_wasm_array_length", "number", ["number"]],
        [true, "mono_wasm_array_get", "number", ["number", "number"]],
        [true, "mono_wasm_array_get_ref", "void", ["number", "number", "number"]],
        [false, "mono_wasm_obj_array_new", "number", ["number"]],
        [false, "mono_wasm_obj_array_new_ref", "void", ["number", "number"]],
        [false, "mono_wasm_obj_array_set", "void", ["number", "number", "number"]],
        [false, "mono_wasm_obj_array_set_ref", "void", ["number", "number", "number"]],
        [true, "mono_wasm_register_bundled_satellite_assemblies", "void", []],
        [false, "mono_wasm_try_unbox_primitive_and_get_type_ref", "number", ["number", "number", "number"]],
        [true, "mono_wasm_box_primitive_ref", "void", ["number", "number", "number", "number"]],
        [true, "mono_wasm_intern_string_ref", "void", ["number"]],
        [true, "mono_wasm_assembly_get_entry_point", "number", ["number"]],
        [true, "mono_wasm_get_delegate_invoke_ref", "number", ["number"]],
        [true, "mono_wasm_string_array_new_ref", "void", ["number", "number"]],
        [true, "mono_wasm_typed_array_new_ref", "void", ["number", "number", "number", "number", "number"]],
        [true, "mono_wasm_class_get_type", "number", ["number"]],
        [true, "mono_wasm_type_get_class", "number", ["number"]],
        [true, "mono_wasm_get_type_name", "string", ["number"]],
        [true, "mono_wasm_get_type_aqn", "string", ["number"]],
        // MONO.diagnostics
        [true, "mono_wasm_event_pipe_enable", "bool", ["string", "number", "number", "string", "bool", "number"]],
        [true, "mono_wasm_event_pipe_session_start_streaming", "bool", ["number"]],
        [true, "mono_wasm_event_pipe_session_disable", "bool", ["number"]],
        [true, "mono_wasm_diagnostic_server_create_thread", "bool", ["string", "number"]],
        [true, "mono_wasm_diagnostic_server_thread_attach_to_runtime", "void", []],
        [true, "mono_wasm_diagnostic_server_post_resume_runtime", "void", []],
        [true, "mono_wasm_diagnostic_server_create_stream", "number", []],
        //DOTNET
        [true, "mono_wasm_string_from_js", "number", ["string"]],
        //INTERNAL
        [false, "mono_wasm_exit", "void", ["number"]],
        [true, "mono_wasm_getenv", "number", ["string"]],
        [true, "mono_wasm_set_main_args", "void", ["number", "number"]],
        [false, "mono_wasm_enable_on_demand_gc", "void", ["number"]],
        [false, "mono_profiler_init_aot", "void", ["number"]],
        [false, "mono_wasm_exec_regression", "number", ["number", "string"]],
        [false, "mono_wasm_invoke_method_bound", "number", ["number", "number"]],
        [true, "mono_wasm_write_managed_pointer_unsafe", "void", ["number", "number"]],
        [true, "mono_wasm_copy_managed_pointer", "void", ["number", "number"]],
        [true, "mono_wasm_i52_to_f64", "number", ["number", "number"]],
        [true, "mono_wasm_u52_to_f64", "number", ["number", "number"]],
        [true, "mono_wasm_f64_to_i52", "number", ["number", "number"]],
        [true, "mono_wasm_f64_to_u52", "number", ["number", "number"]],
    ];
    const wrapped_c_functions = {};
    function init_c_exports() {
        // init_c_exports is called very early in a pthread before Module.cwrap is available
        const alwaysLazy = !!ENVIRONMENT_IS_PTHREAD;
        for (const sig of fn_signatures$1) {
            const wf = wrapped_c_functions;
            const [lazy, name, returnType, argTypes, opts] = sig;
            if (lazy || alwaysLazy) {
                // lazy init on first run
                wf[name] = function (...args) {
                    const fce = Module.cwrap(name, returnType, argTypes, opts);
                    wf[name] = fce;
                    return fce(...args);
                };
            }
            else {
                const fce = Module.cwrap(name, returnType, argTypes, opts);
                wf[name] = fce;
            }
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    // Code from JSIL:
    // https://github.com/sq/JSIL/blob/1d57d5427c87ab92ffa3ca4b82429cd7509796ba/JSIL.Libraries/Includes/Bootstrap/Core/Classes/System.Convert.js#L149
    // Thanks to Katelyn Gadd @kg
    function toBase64StringImpl(inArray, offset, length) {
        const reader = _makeByteReader(inArray, offset, length);
        let result = "";
        let ch1 = 0, ch2 = 0, ch3 = 0;
        let bits = 0, equalsCount = 0, sum = 0;
        const mask1 = (1 << 24) - 1, mask2 = (1 << 18) - 1, mask3 = (1 << 12) - 1, mask4 = (1 << 6) - 1;
        const shift1 = 18, shift2 = 12, shift3 = 6, shift4 = 0;
        for (;;) {
            ch1 = reader.read();
            ch2 = reader.read();
            ch3 = reader.read();
            if (ch1 === null)
                break;
            if (ch2 === null) {
                ch2 = 0;
                equalsCount += 1;
            }
            if (ch3 === null) {
                ch3 = 0;
                equalsCount += 1;
            }
            // Seems backwards, but is right!
            sum = (ch1 << 16) | (ch2 << 8) | (ch3 << 0);
            bits = (sum & mask1) >> shift1;
            result += _base64Table[bits];
            bits = (sum & mask2) >> shift2;
            result += _base64Table[bits];
            if (equalsCount < 2) {
                bits = (sum & mask3) >> shift3;
                result += _base64Table[bits];
            }
            if (equalsCount === 2) {
                result += "==";
            }
            else if (equalsCount === 1) {
                result += "=";
            }
            else {
                bits = (sum & mask4) >> shift4;
                result += _base64Table[bits];
            }
        }
        return result;
    }
    const _base64Table = [
        "A", "B", "C", "D",
        "E", "F", "G", "H",
        "I", "J", "K", "L",
        "M", "N", "O", "P",
        "Q", "R", "S", "T",
        "U", "V", "W", "X",
        "Y", "Z",
        "a", "b", "c", "d",
        "e", "f", "g", "h",
        "i", "j", "k", "l",
        "m", "n", "o", "p",
        "q", "r", "s", "t",
        "u", "v", "w", "x",
        "y", "z",
        "0", "1", "2", "3",
        "4", "5", "6", "7",
        "8", "9",
        "+", "/"
    ];
    function _makeByteReader(bytes, index, count) {
        let position = (typeof (index) === "number") ? index : 0;
        let endpoint;
        if (typeof (count) === "number")
            endpoint = (position + count);
        else
            endpoint = (bytes.length - position);
        const result = {
            read: function () {
                if (position >= endpoint)
                    return null;
                const nextByte = bytes[position];
                position += 1;
                return nextByte;
            }
        };
        Object.defineProperty(result, "eof", {
            get: function () {
                return (position >= endpoint);
            },
            configurable: true,
            enumerable: true
        });
        return result;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const commands_received = new Map();
    commands_received.remove = function (key) { const value = this.get(key); this.delete(key); return value; };
    let _call_function_res_cache = {};
    let _next_call_function_res_id = 0;
    let _debugger_buffer_len = -1;
    let _debugger_buffer;
    let _assembly_name_str; //keep this variable, it's used by BrowserDebugProxy
    let _entrypoint_method_token; //keep this variable, it's used by BrowserDebugProxy
    function mono_wasm_runtime_ready() {
        INTERNAL.mono_wasm_runtime_is_ready = runtimeHelpers.mono_wasm_runtime_is_ready = true;
        // FIXME: where should this go?
        _next_call_function_res_id = 0;
        _call_function_res_cache = {};
        _debugger_buffer_len = -1;
        // DO NOT REMOVE - magic debugger init function
        if (globalThis.dotnetDebugger)
            // eslint-disable-next-line no-debugger
            debugger;
        else
            console.debug("mono_wasm_runtime_ready", "fe00e07a-5519-4dfe-b35a-f867dbaf2e28");
    }
    function mono_wasm_fire_debugger_agent_message() {
        // eslint-disable-next-line no-debugger
        debugger;
    }
    function mono_wasm_add_dbg_command_received(res_ok, id, buffer, buffer_len) {
        const assembly_data = new Uint8Array(Module.HEAPU8.buffer, buffer, buffer_len);
        const base64String = toBase64StringImpl(assembly_data);
        const buffer_obj = {
            res_ok,
            res: {
                id,
                value: base64String
            }
        };
        if (commands_received.has(id))
            console.warn(`MONO_WASM: Adding an id (${id}) that already exists in commands_received`);
        commands_received.set(id, buffer_obj);
    }
    function mono_wasm_malloc_and_set_debug_buffer(command_parameters) {
        if (command_parameters.length > _debugger_buffer_len) {
            if (_debugger_buffer)
                Module._free(_debugger_buffer);
            _debugger_buffer_len = Math.max(command_parameters.length, _debugger_buffer_len, 256);
            _debugger_buffer = Module._malloc(_debugger_buffer_len);
        }
        const byteCharacters = atob(command_parameters);
        for (let i = 0; i < byteCharacters.length; i++) {
            Module.HEAPU8[_debugger_buffer + i] = byteCharacters.charCodeAt(i);
        }
    }
    function mono_wasm_send_dbg_command_with_parms(id, command_set, command, command_parameters, length, valtype, newvalue) {
        mono_wasm_malloc_and_set_debug_buffer(command_parameters);
        wrapped_c_functions.mono_wasm_send_dbg_command_with_parms(id, command_set, command, _debugger_buffer, length, valtype, newvalue.toString());
        const { res_ok, res } = commands_received.remove(id);
        if (!res_ok)
            throw new Error("Failed on mono_wasm_invoke_method_debugger_agent_with_parms");
        return res;
    }
    function mono_wasm_send_dbg_command(id, command_set, command, command_parameters) {
        mono_wasm_malloc_and_set_debug_buffer(command_parameters);
        wrapped_c_functions.mono_wasm_send_dbg_command(id, command_set, command, _debugger_buffer, command_parameters.length);
        const { res_ok, res } = commands_received.remove(id);
        if (!res_ok)
            throw new Error("Failed on mono_wasm_send_dbg_command");
        return res;
    }
    function mono_wasm_get_dbg_command_info() {
        const { res_ok, res } = commands_received.remove(0);
        if (!res_ok)
            throw new Error("Failed on mono_wasm_get_dbg_command_info");
        return res;
    }
    function mono_wasm_debugger_resume() {
        //nothing
    }
    function mono_wasm_detach_debugger() {
        wrapped_c_functions.mono_wasm_set_is_debugger_attached(false);
    }
    function mono_wasm_change_debugger_log_level(level) {
        wrapped_c_functions.mono_wasm_change_debugger_log_level(level);
    }
    /**
     * Raises an event for the debug proxy
     */
    function mono_wasm_raise_debug_event(event, args = {}) {
        if (typeof event !== "object")
            throw new Error(`event must be an object, but got ${JSON.stringify(event)}`);
        if (event.eventName === undefined)
            throw new Error(`event.eventName is a required parameter, in event: ${JSON.stringify(event)}`);
        if (typeof args !== "object")
            throw new Error(`args must be an object, but got ${JSON.stringify(args)}`);
        console.debug("mono_wasm_debug_event_raised:aef14bca-5519-4dfe-b35a-f867abc123ae", JSON.stringify(event), JSON.stringify(args));
    }
    function mono_wasm_wait_for_debugger() {
        return new Promise((resolve) => {
            const interval = setInterval(() => {
                if (runtimeHelpers.waitForDebugger != 1) {
                    return;
                }
                clearInterval(interval);
                resolve();
            }, 100);
        });
    }
    function mono_wasm_debugger_attached() {
        if (runtimeHelpers.waitForDebugger == -1)
            runtimeHelpers.waitForDebugger = 1;
        wrapped_c_functions.mono_wasm_set_is_debugger_attached(true);
    }
    function mono_wasm_set_entrypoint_breakpoint(assembly_name, entrypoint_method_token) {
        //keep these assignments, these values are used by BrowserDebugProxy
        _assembly_name_str = Module.UTF8ToString(assembly_name).concat(".dll");
        _entrypoint_method_token = entrypoint_method_token;
        //keep this console.assert, otherwise optimization will remove the assignments
        console.assert(true, `Adding an entrypoint breakpoint ${_assembly_name_str} at method token  ${_entrypoint_method_token}`);
        // eslint-disable-next-line no-debugger
        debugger;
    }
    function _create_proxy_from_object_id(objectId, details) {
        if (objectId.startsWith("dotnet:array:")) {
            let ret;
            if (details.items === undefined) {
                ret = details.map((p) => p.value);
                return ret;
            }
            if (details.dimensionsDetails === undefined || details.dimensionsDetails.length === 1) {
                ret = details.items.map((p) => p.value);
                return ret;
            }
        }
        const proxy = {};
        Object.keys(details).forEach(p => {
            const prop = details[p];
            if (prop.get !== undefined) {
                Object.defineProperty(proxy, prop.name, {
                    get() {
                        return mono_wasm_send_dbg_command(prop.get.id, prop.get.commandSet, prop.get.command, prop.get.buffer);
                    },
                    set: function (newValue) {
                        mono_wasm_send_dbg_command_with_parms(prop.set.id, prop.set.commandSet, prop.set.command, prop.set.buffer, prop.set.length, prop.set.valtype, newValue);
                        return true;
                    }
                });
            }
            else if (prop.set !== undefined) {
                Object.defineProperty(proxy, prop.name, {
                    get() {
                        return prop.value;
                    },
                    set: function (newValue) {
                        mono_wasm_send_dbg_command_with_parms(prop.set.id, prop.set.commandSet, prop.set.command, prop.set.buffer, prop.set.length, prop.set.valtype, newValue);
                        return true;
                    }
                });
            }
            else {
                proxy[prop.name] = prop.value;
            }
        });
        return proxy;
    }
    function mono_wasm_call_function_on(request) {
        if (request.arguments != undefined && !Array.isArray(request.arguments))
            throw new Error(`"arguments" should be an array, but was ${request.arguments}`);
        const objId = request.objectId;
        const details = request.details;
        let proxy = {};
        if (objId.startsWith("dotnet:cfo_res:")) {
            if (objId in _call_function_res_cache)
                proxy = _call_function_res_cache[objId];
            else
                throw new Error(`Unknown object id ${objId}`);
        }
        else {
            proxy = _create_proxy_from_object_id(objId, details);
        }
        const fn_args = request.arguments != undefined ? request.arguments.map(a => JSON.stringify(a.value)) : [];
        const fn_body_template = `const fn = ${request.functionDeclaration}; return fn.apply(proxy, [${fn_args}]);`;
        const fn_defn = new Function("proxy", fn_body_template);
        const fn_res = fn_defn(proxy);
        if (fn_res === undefined)
            return { type: "undefined" };
        if (Object(fn_res) !== fn_res) {
            if (typeof (fn_res) == "object" && fn_res == null)
                return { type: typeof (fn_res), subtype: `${fn_res}`, value: null };
            return { type: typeof (fn_res), description: `${fn_res}`, value: `${fn_res}` };
        }
        if (request.returnByValue && fn_res.subtype == undefined)
            return { type: "object", value: fn_res };
        if (Object.getPrototypeOf(fn_res) == Array.prototype) {
            const fn_res_id = _cache_call_function_res(fn_res);
            return {
                type: "object",
                subtype: "array",
                className: "Array",
                description: `Array(${fn_res.length})`,
                objectId: fn_res_id
            };
        }
        if (fn_res.value !== undefined || fn_res.subtype !== undefined) {
            return fn_res;
        }
        if (fn_res == proxy)
            return { type: "object", className: "Object", description: "Object", objectId: objId };
        const fn_res_id = _cache_call_function_res(fn_res);
        return { type: "object", className: "Object", description: "Object", objectId: fn_res_id };
    }
    function _get_cfo_res_details(objectId, args) {
        if (!(objectId in _call_function_res_cache))
            throw new Error(`Could not find any object with id ${objectId}`);
        const real_obj = _call_function_res_cache[objectId];
        const descriptors = Object.getOwnPropertyDescriptors(real_obj);
        if (args.accessorPropertiesOnly) {
            Object.keys(descriptors).forEach(k => {
                if (descriptors[k].get === undefined)
                    Reflect.deleteProperty(descriptors, k);
            });
        }
        const res_details = [];
        Object.keys(descriptors).forEach(k => {
            let new_obj;
            const prop_desc = descriptors[k];
            if (typeof prop_desc.value == "object") {
                // convert `{value: { type='object', ... }}`
                // to      `{ name: 'foo', value: { type='object', ... }}
                new_obj = Object.assign({ name: k }, prop_desc);
            }
            else if (prop_desc.value !== undefined) {
                // This is needed for values that were not added by us,
                // thus are like { value: 5 }
                // instead of    { value: { type = 'number', value: 5 }}
                //
                // This can happen, for eg., when `length` gets added for arrays
                // or `__proto__`.
                new_obj = {
                    name: k,
                    // merge/add `type` and `description` to `d.value`
                    value: Object.assign({ type: (typeof prop_desc.value), description: "" + prop_desc.value }, prop_desc)
                };
            }
            else if (prop_desc.get !== undefined) {
                // The real_obj has the actual getter. We are just returning a placeholder
                // If the caller tries to run function on the cfo_res object,
                // that accesses this property, then it would be run on `real_obj`,
                // which *has* the original getter
                new_obj = {
                    name: k,
                    get: {
                        className: "Function",
                        description: `get ${k} () {}`,
                        type: "function"
                    }
                };
            }
            else {
                new_obj = { name: k, value: { type: "symbol", value: "<Unknown>", description: "<Unknown>" } };
            }
            res_details.push(new_obj);
        });
        return { __value_as_json_string__: JSON.stringify(res_details) };
    }
    function mono_wasm_get_details(objectId, args = {}) {
        return _get_cfo_res_details(`dotnet:cfo_res:${objectId}`, args);
    }
    function _cache_call_function_res(obj) {
        const id = `dotnet:cfo_res:${_next_call_function_res_id++}`;
        _call_function_res_cache[id] = obj;
        return id;
    }
    function mono_wasm_release_object(objectId) {
        if (objectId in _call_function_res_cache)
            delete _call_function_res_cache[objectId];
    }
    function mono_wasm_debugger_log(level, message_ptr) {
        const message = Module.UTF8ToString(message_ptr);
        if (INTERNAL["logging"] && typeof INTERNAL.logging["debugger"] === "function") {
            INTERNAL.logging.debugger(level, message);
            return;
        }
        if (BuildConfiguration === "Debug") {
            console.debug(`MONO_WASM: Debugger.Debug: ${message}`);
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    let num_icu_assets_loaded_successfully = 0;
    // @offset must be the address of an ICU data archive in the native heap.
    // returns true on success.
    function mono_wasm_load_icu_data(offset) {
        const ok = (wrapped_c_functions.mono_wasm_load_icu_data(offset)) === 1;
        if (ok)
            num_icu_assets_loaded_successfully++;
        return ok;
    }
    // Get icudt.dat exact filename that matches given culture, examples:
    //   "ja" -> "icudt_CJK.dat"
    //   "en_US" (or "en-US" or just "en") -> "icudt_EFIGS.dat"
    // etc, see "mono_wasm_get_icudt_name" implementation in pal_icushim_static.c
    function mono_wasm_get_icudt_name(culture) {
        return wrapped_c_functions.mono_wasm_get_icudt_name(culture);
    }
    // Performs setup for globalization.
    // @globalizationMode is one of "icu", "invariant", or "auto".
    // "auto" will use "icu" if any ICU data archives have been loaded,
    //  otherwise "invariant".
    function mono_wasm_globalization_init() {
        const config = runtimeHelpers.config;
        let invariantMode = false;
        if (!config.globalizationMode)
            config.globalizationMode = "auto";
        if (config.globalizationMode === "invariant")
            invariantMode = true;
        if (!invariantMode) {
            if (num_icu_assets_loaded_successfully > 0) {
                if (runtimeHelpers.diagnosticTracing) {
                    console.debug("MONO_WASM: ICU data archive(s) loaded, disabling invariant mode");
                }
            }
            else if (config.globalizationMode !== "icu") {
                if (runtimeHelpers.diagnosticTracing) {
                    console.debug("MONO_WASM: ICU data archive(s) not loaded, using invariant globalization mode");
                }
                invariantMode = true;
            }
            else {
                const msg = "invariant globalization mode is inactive and no ICU data archives were loaded";
                Module.printErr(`MONO_WASM: ERROR: ${msg}`);
                throw new Error(msg);
            }
        }
        if (invariantMode)
            wrapped_c_functions.mono_wasm_setenv("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");
        // Set globalization mode to PredefinedCulturesOnly
        wrapped_c_functions.mono_wasm_setenv("DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY", "1");
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // Initialize the AOT profiler with OPTIONS.
    // Requires the AOT profiler to be linked into the app.
    // options = { writeAt: "<METHODNAME>", sendTo: "<METHODNAME>" }
    // <METHODNAME> should be in the format <CLASS>::<METHODNAME>.
    // writeAt defaults to 'WebAssembly.Runtime::StopProfile'.
    // sendTo defaults to 'WebAssembly.Runtime::DumpAotProfileData'.
    // DumpAotProfileData stores the data into INTERNAL.aotProfileData.
    //
    function mono_wasm_init_aot_profiler(options) {
        if (options == null)
            options = {};
        if (!("writeAt" in options))
            options.writeAt = "System.Runtime.InteropServices.JavaScript.JavaScriptExports::StopProfile";
        if (!("sendTo" in options))
            options.sendTo = "Interop/Runtime::DumpAotProfileData";
        const arg = "aot:write-at-method=" + options.writeAt + ",send-to-method=" + options.sendTo;
        Module.ccall("mono_wasm_load_profiler_aot", null, ["string"], [arg]);
    }
    // options = { writeAt: "<METHODNAME>", sendTo: "<METHODNAME>" }
    // <METHODNAME> should be in the format <CLASS>::<METHODNAME>.
    // writeAt defaults to 'WebAssembly.Runtime::StopProfile'.
    // sendTo defaults to 'WebAssembly.Runtime::DumpCoverageProfileData'.
    // DumpCoverageProfileData stores the data into INTERNAL.coverage_profile_data.
    function mono_wasm_init_coverage_profiler(options) {
        if (options == null)
            options = {};
        if (!("writeAt" in options))
            options.writeAt = "WebAssembly.Runtime::StopProfile";
        if (!("sendTo" in options))
            options.sendTo = "WebAssembly.Runtime::DumpCoverageProfileData";
        const arg = "coverage:write-at-method=" + options.writeAt + ",send-to-method=" + options.sendTo;
        Module.ccall("mono_wasm_load_profiler_coverage", null, ["string"], [arg]);
    }

    const _assembly_cache_by_name = new Map();
    const _class_cache_by_assembly = new Map();
    let _corlib = MonoAssemblyNull;
    function assembly_load(name) {
        if (_assembly_cache_by_name.has(name))
            return _assembly_cache_by_name.get(name);
        const result = wrapped_c_functions.mono_wasm_assembly_load(name);
        _assembly_cache_by_name.set(name, result);
        return result;
    }
    function _find_cached_class(assembly, namespace, name) {
        let namespaces = _class_cache_by_assembly.get(assembly);
        if (!namespaces)
            _class_cache_by_assembly.set(assembly, namespaces = new Map());
        let classes = namespaces.get(namespace);
        if (!classes) {
            classes = new Map();
            namespaces.set(namespace, classes);
        }
        return classes.get(name);
    }
    function _set_cached_class(assembly, namespace, name, ptr) {
        const namespaces = _class_cache_by_assembly.get(assembly);
        if (!namespaces)
            throw new Error("internal error");
        const classes = namespaces.get(namespace);
        if (!classes)
            throw new Error("internal error");
        classes.set(name, ptr);
    }
    function find_corlib_class(namespace, name, throw_on_failure) {
        if (!_corlib)
            _corlib = wrapped_c_functions.mono_wasm_get_corlib();
        let result = _find_cached_class(_corlib, namespace, name);
        if (result !== undefined)
            return result;
        result = wrapped_c_functions.mono_wasm_assembly_find_class(_corlib, namespace, name);
        if (throw_on_failure && !result)
            throw new Error(`Failed to find corlib class ${namespace}.${name}`);
        _set_cached_class(_corlib, namespace, name, result);
        return result;
    }
    function find_class_in_assembly(assembly_name, namespace, name, throw_on_failure) {
        const assembly = assembly_load(assembly_name);
        let result = _find_cached_class(assembly, namespace, name);
        if (result !== undefined)
            return result;
        result = wrapped_c_functions.mono_wasm_assembly_find_class(assembly, namespace, name);
        if (throw_on_failure && !result)
            throw new Error(`Failed to find class ${namespace}.${name} in ${assembly_name}`);
        _set_cached_class(assembly, namespace, name, result);
        return result;
    }
    function find_corlib_type(namespace, name, throw_on_failure) {
        const classPtr = find_corlib_class(namespace, name, throw_on_failure);
        if (!classPtr)
            return MonoTypeNull;
        return wrapped_c_functions.mono_wasm_class_get_type(classPtr);
    }
    function find_type_in_assembly(assembly_name, namespace, name, throw_on_failure) {
        const classPtr = find_class_in_assembly(assembly_name, namespace, name, throw_on_failure);
        if (!classPtr)
            return MonoTypeNull;
        return wrapped_c_functions.mono_wasm_class_get_type(classPtr);
    }

    //! Licensed to the .NET Foundation under one or more agreements.
    const wasm_func_map = new Map();
    const regexes = [];
    // V8
    //   at <anonymous>:wasm-function[1900]:0x83f63
    //   at dlfree (<anonymous>:wasm-function[18739]:0x2328ef)
    regexes.push(/at (?<replaceSection>[^:()]+:wasm-function\[(?<funcNum>\d+)\]:0x[a-fA-F\d]+)((?![^)a-fA-F\d])|$)/);
    //# 5: WASM [009712b2], function #111 (''), pc=0x7c16595c973 (+0x53), pos=38740 (+11)
    regexes.push(/(?:WASM \[[\da-zA-Z]+\], (?<replaceSection>function #(?<funcNum>[\d]+) \(''\)))/);
    //# chrome
    //# at http://127.0.0.1:63817/dotnet.wasm:wasm-function[8963]:0x1e23f4
    regexes.push(/(?<replaceSection>[a-z]+:\/\/[^ )]*:wasm-function\[(?<funcNum>\d+)\]:0x[a-fA-F\d]+)/);
    //# <?>.wasm-function[8962]
    regexes.push(/(?<replaceSection><[^ >]+>[.:]wasm-function\[(?<funcNum>[0-9]+)\])/);
    function mono_wasm_symbolicate_string(message) {
        try {
            if (wasm_func_map.size == 0)
                return message;
            const origMessage = message;
            for (let i = 0; i < regexes.length; i++) {
                const newRaw = message.replace(new RegExp(regexes[i], "g"), (substring, ...args) => {
                    const groups = args.find(arg => {
                        return typeof (arg) == "object" && arg.replaceSection !== undefined;
                    });
                    if (groups === undefined)
                        return substring;
                    const funcNum = groups.funcNum;
                    const replaceSection = groups.replaceSection;
                    const name = wasm_func_map.get(Number(funcNum));
                    if (name === undefined)
                        return substring;
                    return substring.replace(replaceSection, `${name} (${replaceSection})`);
                });
                if (newRaw !== origMessage)
                    return newRaw;
            }
            return origMessage;
        }
        catch (error) {
            console.debug(`MONO_WASM: failed to symbolicate: ${error}`);
            return message;
        }
    }
    function mono_wasm_stringify_as_error_with_stack(err) {
        let errObj = err;
        if (!(errObj instanceof Error)) {
            errObj = new Error(errObj);
        }
        // Error
        return mono_wasm_symbolicate_string(errObj.stack);
    }
    function mono_wasm_trace_logger(log_domain_ptr, log_level_ptr, message_ptr, fatal, user_data) {
        const origMessage = Module.UTF8ToString(message_ptr);
        const isFatal = !!fatal;
        const domain = Module.UTF8ToString(log_domain_ptr);
        const dataPtr = user_data;
        const log_level = Module.UTF8ToString(log_level_ptr);
        const message = `[MONO] ${origMessage}`;
        if (INTERNAL["logging"] && typeof INTERNAL.logging["trace"] === "function") {
            INTERNAL.logging.trace(domain, log_level, message, isFatal, dataPtr);
            return;
        }
        switch (log_level) {
            case "critical":
            case "error":
                console.error(mono_wasm_stringify_as_error_with_stack(message));
                break;
            case "warning":
                console.warn(message);
                break;
            case "message":
                console.log(message);
                break;
            case "info":
                console.info(message);
                break;
            case "debug":
                console.debug(message);
                break;
            default:
                console.log(message);
                break;
        }
    }
    let consoleWebSocket;
    function setup_proxy_console(id, console, origin) {
        // this need to be copy, in order to keep reference to original methods
        const originalConsole = {
            log: console.log,
            error: console.error
        };
        const anyConsole = console;
        function proxyConsoleMethod(prefix, func, asJson) {
            return function (...args) {
                try {
                    let payload = args[0];
                    if (payload === undefined)
                        payload = "undefined";
                    else if (payload === null)
                        payload = "null";
                    else if (typeof payload === "function")
                        payload = payload.toString();
                    else if (typeof payload !== "string") {
                        try {
                            payload = JSON.stringify(payload);
                        }
                        catch (e) {
                            payload = payload.toString();
                        }
                    }
                    if (typeof payload === "string" && id !== "main")
                        payload = `[${id}] ${payload}`;
                    if (asJson) {
                        func(JSON.stringify({
                            method: prefix,
                            payload: payload,
                            arguments: args
                        }));
                    }
                    else {
                        func([prefix + payload, ...args.slice(1)]);
                    }
                }
                catch (err) {
                    originalConsole.error(`proxyConsole failed: ${err}`);
                }
            };
        }
        const methods = ["debug", "trace", "warn", "info", "error"];
        for (const m of methods) {
            if (typeof (anyConsole[m]) !== "function") {
                anyConsole[m] = proxyConsoleMethod(`console.${m}: `, console.log, false);
            }
        }
        const consoleUrl = `${origin}/console`.replace("https://", "wss://").replace("http://", "ws://");
        consoleWebSocket = new WebSocket(consoleUrl);
        consoleWebSocket.addEventListener("open", () => {
            originalConsole.log(`browser: [${id}] Console websocket connected.`);
        });
        consoleWebSocket.addEventListener("error", (event) => {
            originalConsole.error(`[${id}] websocket error: ${event}`, event);
        });
        consoleWebSocket.addEventListener("close", (event) => {
            originalConsole.error(`[${id}] websocket closed: ${event}`, event);
        });
        const send = (msg) => {
            if (consoleWebSocket.readyState === WebSocket.OPEN) {
                consoleWebSocket.send(msg);
            }
            else {
                originalConsole.log(msg);
            }
        };
        for (const m of ["log", ...methods])
            anyConsole[m] = proxyConsoleMethod(`console.${m}`, send, true);
    }
    function readSymbolMapFile(filename) {
        if (runtimeHelpers.mono_wasm_symbols_are_ready)
            return;
        runtimeHelpers.mono_wasm_symbols_are_ready = true;
        try {
            const res = Module.FS_readFile(filename, { flags: "r", encoding: "utf8" });
            res.split(/[\r\n]/).forEach((line) => {
                const parts = line.split(/:/);
                if (parts.length < 2)
                    return;
                parts[1] = parts.splice(1).join(":");
                wasm_func_map.set(Number(parts[0]), parts[1]);
            });
            if (BuildConfiguration === "Debug") {
                console.debug(`MONO_WASM: Loaded ${wasm_func_map.size} symbols`);
            }
        }
        catch (error) {
            if (error.errno == 44) { // NOENT
                if (BuildConfiguration === "Debug") {
                    console.debug(`MONO_WASM: Could not find symbols file ${filename}. Ignoring.`);
                }
            }
            else {
                console.log(`MONO_WASM: Error loading symbol file ${filename}: ${JSON.stringify(error)}`);
            }
            return;
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    /**
     * Possible signatures are described here  https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/main-command-line
     */
    async function mono_run_main_and_exit(main_assembly_name, args) {
        try {
            const result = await mono_run_main(main_assembly_name, args);
            mono_exit(result);
            return result;
        }
        catch (error) {
            if (error instanceof runtimeHelpers.ExitStatus) {
                return error.status;
            }
            mono_exit(1, error);
            return 1;
        }
    }
    /**
     * Possible signatures are described here  https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/main-command-line
     */
    async function mono_run_main(main_assembly_name, args) {
        mono_wasm_set_main_args(main_assembly_name, args);
        if (runtimeHelpers.waitForDebugger == -1) {
            console.log("MONO_WASM: waiting for debugger...");
            await mono_wasm_wait_for_debugger();
        }
        const method = find_entry_point(main_assembly_name);
        return runtimeHelpers.javaScriptExports.call_entry_point(method, args);
    }
    function find_entry_point(assembly) {
        if (!(runtimeHelpers.mono_wasm_bindings_is_ready)) throw new Error("Assert failed: The runtime must be initialized."); // inlined mono_assert
        const asm = assembly_load(assembly);
        if (!asm)
            throw new Error("Could not find assembly: " + assembly);
        let auto_set_breakpoint = 0;
        if (runtimeHelpers.waitForDebugger == 1)
            auto_set_breakpoint = 1;
        const method = wrapped_c_functions.mono_wasm_assembly_get_entry_point(asm, auto_set_breakpoint);
        if (!method)
            throw new Error("Could not find entry point for assembly: " + assembly);
        return method;
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function mono_on_abort(error) {
        abort_startup(error, false);
        mono_exit(1, error);
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function mono_exit(exit_code, reason) {
        if (runtimeHelpers.config.asyncFlushOnExit && exit_code === 0) {
            // this would NOT call Node's exit() immediately, it's a hanging promise
            (async () => {
                try {
                    await flush_node_streams();
                }
                finally {
                    set_exit_code_and_quit_now(exit_code, reason);
                }
            })();
            // we need to throw, rather than let the caller continue the normal execution 
            // in the middle of some code, which expects this to stop the process
            throw runtimeHelpers.ExitStatus
                ? new runtimeHelpers.ExitStatus(exit_code)
                : reason
                    ? reason
                    : new Error("Stop with exit code " + exit_code);
        }
        else {
            set_exit_code_and_quit_now(exit_code, reason);
        }
    }
    async function flush_node_streams() {
        try {
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore:
            const process = await import(/* webpackIgnore: true */ 'process');
            const flushStream = (stream) => {
                return new Promise((resolve, reject) => {
                    stream.on("error", (error) => reject(error));
                    stream.write("", function () { resolve(); });
                });
            };
            const stderrFlushed = flushStream(process.stderr);
            const stdoutFlushed = flushStream(process.stdout);
            await Promise.all([stdoutFlushed, stderrFlushed]);
        }
        catch (err) {
            console.error(`flushing std* streams failed: ${err}`);
        }
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function set_exit_code_and_quit_now(exit_code, reason) {
        if (runtimeHelpers.ExitStatus) {
            if (reason && !(reason instanceof runtimeHelpers.ExitStatus)) {
                if (reason instanceof Error)
                    Module.printErr(INTERNAL.mono_wasm_stringify_as_error_with_stack(reason));
                else if (typeof reason == "string")
                    Module.printErr(reason);
                else
                    Module.printErr(JSON.stringify(reason));
            }
            else {
                reason = new runtimeHelpers.ExitStatus(exit_code);
            }
        }
        logErrorOnExit(exit_code, reason);
        appendElementOnExit(exit_code);
        if (exit_code !== 0 || !ENVIRONMENT_IS_WEB) {
            if (runtimeHelpers.quit) {
                runtimeHelpers.quit(exit_code, reason);
            }
            else {
                throw reason;
            }
        }
    }
    function appendElementOnExit(exit_code) {
        if (ENVIRONMENT_IS_WEB && runtimeHelpers.config.appendElementOnExit) {
            //Tell xharness WasmBrowserTestRunner what was the exit code
            const tests_done_elem = document.createElement("label");
            tests_done_elem.id = "tests_done";
            if (exit_code)
                tests_done_elem.style.background = "red";
            tests_done_elem.innerHTML = exit_code.toString();
            document.body.appendChild(tests_done_elem);
        }
    }
    function logErrorOnExit(exit_code, reason) {
        if (runtimeHelpers.config.logExitCode) {
            if (exit_code != 0 && reason) {
                if (reason instanceof Error)
                    console.error(mono_wasm_stringify_as_error_with_stack(reason));
                else if (typeof reason == "string")
                    console.error(reason);
                else
                    console.error(JSON.stringify(reason));
            }
            if (consoleWebSocket) {
                const stop_when_ws_buffer_empty = () => {
                    if (consoleWebSocket.bufferedAmount == 0) {
                        // tell xharness WasmTestMessagesProcessor we are done.
                        // note this sends last few bytes into the same WS
                        console.log("WASM EXIT " + exit_code);
                    }
                    else {
                        setTimeout(stop_when_ws_buffer_empty, 100);
                    }
                };
                stop_when_ws_buffer_empty();
            }
            else {
                console.log("WASM EXIT " + exit_code);
            }
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    const _use_weak_ref = typeof globalThis.WeakRef === "function";
    function create_weak_ref(js_obj) {
        if (_use_weak_ref) {
            return new WeakRef(js_obj);
        }
        else {
            // this is trivial WeakRef replacement, which holds strong refrence, instead of weak one, when the browser doesn't support it
            return {
                deref: () => {
                    return js_obj;
                }
            };
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const _use_finalization_registry = typeof globalThis.FinalizationRegistry === "function";
    let _js_owned_object_registry;
    // this is array, not map. We maintain list of gaps in _js_handle_free_list so that it could be as compact as possible
    const _cs_owned_objects_by_js_handle = [];
    const _js_handle_free_list = [];
    let _next_js_handle = 1;
    const _js_owned_object_table = new Map();
    // NOTE: FinalizationRegistry and WeakRef are missing on Safari below 14.1
    if (_use_finalization_registry) {
        _js_owned_object_registry = new globalThis.FinalizationRegistry(_js_owned_object_finalized);
    }
    const js_owned_gc_handle_symbol = Symbol.for("wasm js_owned_gc_handle");
    const cs_owned_js_handle_symbol = Symbol.for("wasm cs_owned_js_handle");
    function mono_wasm_get_jsobj_from_js_handle(js_handle) {
        if (js_handle !== JSHandleNull && js_handle !== JSHandleDisposed)
            return _cs_owned_objects_by_js_handle[js_handle];
        return null;
    }
    function get_js_obj(js_handle) {
        if (js_handle !== JSHandleNull && js_handle !== JSHandleDisposed)
            return mono_wasm_get_jsobj_from_js_handle(js_handle);
        return null;
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function mono_wasm_get_js_handle(js_obj) {
        if (js_obj[cs_owned_js_handle_symbol]) {
            return js_obj[cs_owned_js_handle_symbol];
        }
        const js_handle = _js_handle_free_list.length ? _js_handle_free_list.pop() : _next_js_handle++;
        // note _cs_owned_objects_by_js_handle is list, not Map. That's why we maintain _js_handle_free_list.
        _cs_owned_objects_by_js_handle[js_handle] = js_obj;
        if (Object.isExtensible(js_obj)) {
            js_obj[cs_owned_js_handle_symbol] = js_handle;
        }
        // else
        //   The consequence of not adding the cs_owned_js_handle_symbol is, that we could have multiple JSHandles and multiple proxy instances. 
        //   Throwing exception would prevent us from creating any proxy of non-extensible things. 
        //   If we have weakmap instead, we would pay the price of the lookup for all proxies, not just non-extensible objects.
        return js_handle;
    }
    function mono_wasm_release_cs_owned_object(js_handle) {
        const obj = _cs_owned_objects_by_js_handle[js_handle];
        if (typeof obj !== "undefined" && obj !== null) {
            // if this is the global object then do not
            // unregister it.
            if (globalThis === obj)
                return;
            if (typeof obj[cs_owned_js_handle_symbol] !== "undefined") {
                obj[cs_owned_js_handle_symbol] = undefined;
            }
            _cs_owned_objects_by_js_handle[js_handle] = undefined;
            _js_handle_free_list.push(js_handle);
        }
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function setup_managed_proxy(result, gc_handle) {
        // keep the gc_handle so that we could easily convert it back to original C# object for roundtrip
        result[js_owned_gc_handle_symbol] = gc_handle;
        // NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef
        if (_use_finalization_registry) {
            // register for GC of the C# object after the JS side is done with the object
            _js_owned_object_registry.register(result, gc_handle, result);
        }
        // register for instance reuse
        // NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef
        const wr = create_weak_ref(result);
        _js_owned_object_table.set(gc_handle, wr);
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function teardown_managed_proxy(result, gc_handle) {
        // The JS object associated with this gc_handle has been collected by the JS GC.
        // As such, it's not possible for this gc_handle to be invoked by JS anymore, so
        //  we can release the tracking weakref (it's null now, by definition),
        //  and tell the C# side to stop holding a reference to the managed object.
        // "The FinalizationRegistry callback is called potentially multiple times"
        if (result) {
            gc_handle = result[js_owned_gc_handle_symbol];
            result[js_owned_gc_handle_symbol] = GCHandleNull;
            if (_use_finalization_registry) {
                _js_owned_object_registry.unregister(result);
            }
        }
        if (gc_handle !== GCHandleNull && _js_owned_object_table.delete(gc_handle)) {
            runtimeHelpers.javaScriptExports.release_js_owned_object_by_gc_handle(gc_handle);
        }
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function assert_not_disposed(result) {
        const gc_handle = result[js_owned_gc_handle_symbol];
        if (!(gc_handle != GCHandleNull)) throw new Error("Assert failed: ObjectDisposedException"); // inlined mono_assert
        return gc_handle;
    }
    function _js_owned_object_finalized(gc_handle) {
        teardown_managed_proxy(null, gc_handle);
    }
    function _lookup_js_owned_object(gc_handle) {
        if (!gc_handle)
            return null;
        const wr = _js_owned_object_table.get(gc_handle);
        if (wr) {
            return wr.deref();
            // TODO: could this be null before _js_owned_object_finalized was called ?
            // TODO: are there race condition consequences ?
        }
        return null;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    /// a unique symbol used to mark a promise as controllable
    const promise_control_symbol = Symbol.for("wasm promise_control");
    /// Creates a new promise together with a controller that can be used to resolve or reject that promise.
    /// Optionally takes callbacks to be called immediately after a promise is resolved or rejected.
    function createPromiseController(afterResolve, afterReject) {
        let promise_control = null;
        const promise = new Promise(function (resolve, reject) {
            promise_control = {
                isDone: false,
                promise: null,
                resolve: (data) => {
                    if (!promise_control.isDone) {
                        promise_control.isDone = true;
                        resolve(data);
                        if (afterResolve) {
                            afterResolve();
                        }
                    }
                },
                reject: (reason) => {
                    if (!promise_control.isDone) {
                        promise_control.isDone = true;
                        reject(reason);
                        if (afterReject) {
                            afterReject();
                        }
                    }
                }
            };
        });
        promise_control.promise = promise;
        const controllablePromise = promise;
        controllablePromise[promise_control_symbol] = promise_control;
        return { promise: controllablePromise, promise_control: promise_control };
    }
    function getPromiseController(promise) {
        return promise[promise_control_symbol];
    }
    function isControllablePromise(promise) {
        return promise[promise_control_symbol] !== undefined;
    }
    function assertIsControllablePromise(promise) {
        if (!(isControllablePromise(promise))) throw new Error("Assert failed: Promise is not controllable"); // inlined mono_assert
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const _are_promises_supported = ((typeof Promise === "object") || (typeof Promise === "function")) && (typeof Promise.resolve === "function");
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function isThenable(js_obj) {
        // When using an external Promise library like Bluebird the Promise.resolve may not be sufficient
        // to identify the object as a Promise.
        return Promise.resolve(js_obj) === js_obj ||
            ((typeof js_obj === "object" || typeof js_obj === "function") && typeof js_obj.then === "function");
    }
    function wrap_as_cancelable_promise(fn) {
        const { promise, promise_control } = createPromiseController();
        const inner = fn();
        inner.then((data) => promise_control.resolve(data)).catch((reason) => promise_control.reject(reason));
        return promise;
    }
    function mono_wasm_cancel_promise(task_holder_gc_handle) {
        const holder = _lookup_js_owned_object(task_holder_gc_handle);
        if (!holder)
            return; // probably already GC collected
        const promise = holder.promise;
        if (!(!!promise)) throw new Error(`Assert failed: Expected Promise for GCHandle ${task_holder_gc_handle}`); // inlined mono_assert
        assertIsControllablePromise(promise);
        const promise_control = getPromiseController(promise);
        promise_control.reject("OperationCanceledException");
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    function toBigInt(x) {
        return BigInt(x[0]) | BigInt(x[1]) << BigInt(32);
    }
    function fromBigInt(x) {
        if (x < BigInt(0))
            throw new Error(`${x} is not a valid 64 bit integer`);
        if (x > BigInt(0xFFFFFFFFFFFFFFFF))
            throw new Error(`${x} is not a valid 64 bit integer`);
        const low = Number(x & BigInt(0xFFFFFFFF));
        const high = Number(x >> BigInt(32));
        return [low, high];
    }
    function dangerousToNumber(x) {
        return x[0] | x[1] << 32;
    }
    function fromNumber(x) {
        if (x < 0)
            throw new Error(`${x} is not a valid 64 bit integer`);
        if ((x >> 32) > 0xFFFFFFFF)
            throw new Error(`${x} is not a valid 64 bit integer`);
        if (Math.trunc(x) != x)
            throw new Error(`${x} is not a valid 64 bit integer`);
        return [x & 0xFFFFFFFF, x >> 32];
    }
    function pack32(lo, hi) {
        return [lo, hi];
    }
    function unpack32(x) {
        return [x[0], x[1]];
    }
    const zero = [0, 0];

    // Licensed to the .NET Foundation under one or more agreements.
    const alloca_stack = [];
    const alloca_buffer_size = 32 * 1024;
    let alloca_base, alloca_offset, alloca_limit;
    let HEAPI64 = null;
    function _ensure_allocated() {
        if (alloca_base)
            return;
        alloca_base = Module._malloc(alloca_buffer_size);
        alloca_offset = alloca_base;
        alloca_limit = (alloca_base + alloca_buffer_size);
    }
    const is_bigint_supported = typeof BigInt !== "undefined" && typeof BigInt64Array !== "undefined";
    function temp_malloc(size) {
        _ensure_allocated();
        if (!alloca_stack.length)
            throw new Error("No temp frames have been created at this point");
        const result = alloca_offset;
        alloca_offset += size;
        if (alloca_offset >= alloca_limit)
            throw new Error("Out of temp storage space");
        return result;
    }
    function _create_temp_frame() {
        _ensure_allocated();
        alloca_stack.push(alloca_offset);
    }
    function _release_temp_frame() {
        if (!alloca_stack.length)
            throw new Error("No temp frames have been created at this point");
        alloca_offset = alloca_stack.pop();
    }
    function assert_int_in_range(value, min, max) {
        if (!(Number.isSafeInteger(value))) throw new Error(`Assert failed: Value is not an integer: ${value} (${typeof (value)})`); // inlined mono_assert
        if (!(value >= min && value <= max)) throw new Error(`Assert failed: Overflow: value ${value} is out of ${min} ${max} range`); // inlined mono_assert
    }
    function _zero_region(byteOffset, sizeBytes) {
        Module.HEAP8.fill(0, byteOffset, byteOffset + sizeBytes);
    }
    function setB32(offset, value) {
        const boolValue = !!value;
        if (typeof (value) === "number")
            assert_int_in_range(value, 0, 1);
        Module.HEAP32[offset >>> 2] = boolValue ? 1 : 0;
    }
    function setU8(offset, value) {
        assert_int_in_range(value, 0, 0xFF);
        Module.HEAPU8[offset] = value;
    }
    function setU16(offset, value) {
        assert_int_in_range(value, 0, 0xFFFF);
        Module.HEAPU16[offset >>> 1] = value;
    }
    function setU32_unchecked(offset, value) {
        Module.HEAPU32[offset >>> 2] = value;
    }
    function setU32(offset, value) {
        assert_int_in_range(value, 0, 4294967295);
        Module.HEAPU32[offset >>> 2] = value;
    }
    function setI8(offset, value) {
        assert_int_in_range(value, -0x80, 0x7F);
        Module.HEAP8[offset] = value;
    }
    function setI16(offset, value) {
        assert_int_in_range(value, -0x8000, 0x7FFF);
        Module.HEAP16[offset >>> 1] = value;
    }
    function setI32_unchecked(offset, value) {
        Module.HEAP32[offset >>> 2] = value;
    }
    function setI32(offset, value) {
        assert_int_in_range(value, -2147483648, 2147483647);
        Module.HEAP32[offset >>> 2] = value;
    }
    function autoThrowI52(error) {
        if (error === 0 /* I52Error.NONE */)
            return;
        switch (error) {
            case 1 /* I52Error.NON_INTEGRAL */:
                throw new Error("value was not an integer");
            case 2 /* I52Error.OUT_OF_RANGE */:
                throw new Error("value out of range");
            default:
                throw new Error("unknown internal error");
        }
    }
    /**
     * Throws for values which are not 52 bit integer. See Number.isSafeInteger()
     */
    function setI52(offset, value) {
        if (!(Number.isSafeInteger(value))) throw new Error(`Assert failed: Value is not a safe integer: ${value} (${typeof (value)})`); // inlined mono_assert
        const error = wrapped_c_functions.mono_wasm_f64_to_i52(offset, value);
        autoThrowI52(error);
    }
    /**
     * Throws for values which are not 52 bit integer or are negative. See Number.isSafeInteger().
     */
    function setU52(offset, value) {
        if (!(Number.isSafeInteger(value))) throw new Error(`Assert failed: Value is not a safe integer: ${value} (${typeof (value)})`); // inlined mono_assert
        if (!(value >= 0)) throw new Error("Assert failed: Can't convert negative Number into UInt64"); // inlined mono_assert
        const error = wrapped_c_functions.mono_wasm_f64_to_u52(offset, value);
        autoThrowI52(error);
    }
    function setI64Big(offset, value) {
        if (!(is_bigint_supported)) throw new Error("Assert failed: BigInt is not supported."); // inlined mono_assert
        if (!(typeof value === "bigint")) throw new Error(`Assert failed: Value is not an bigint: ${value} (${typeof (value)})`); // inlined mono_assert
        if (!(value >= min_int64_big && value <= max_int64_big)) throw new Error(`Assert failed: Overflow: value ${value} is out of ${min_int64_big} ${max_int64_big} range`); // inlined mono_assert
        HEAPI64[offset >>> 3] = value;
    }
    function setF32(offset, value) {
        if (!(typeof value === "number")) throw new Error(`Assert failed: Value is not a Number: ${value} (${typeof (value)})`); // inlined mono_assert
        Module.HEAPF32[offset >>> 2] = value;
    }
    function setF64(offset, value) {
        if (!(typeof value === "number")) throw new Error(`Assert failed: Value is not a Number: ${value} (${typeof (value)})`); // inlined mono_assert
        Module.HEAPF64[offset >>> 3] = value;
    }
    function getB32(offset) {
        return !!(Module.HEAP32[offset >>> 2]);
    }
    function getU8(offset) {
        return Module.HEAPU8[offset];
    }
    function getU16(offset) {
        return Module.HEAPU16[offset >>> 1];
    }
    function getU32(offset) {
        return Module.HEAPU32[offset >>> 2];
    }
    function getI8(offset) {
        return Module.HEAP8[offset];
    }
    function getI16(offset) {
        return Module.HEAP16[offset >>> 1];
    }
    function getI32(offset) {
        return Module.HEAP32[offset >>> 2];
    }
    /**
     * Throws for Number.MIN_SAFE_INTEGER > value > Number.MAX_SAFE_INTEGER
     */
    function getI52(offset) {
        const result = wrapped_c_functions.mono_wasm_i52_to_f64(offset, runtimeHelpers._i52_error_scratch_buffer);
        const error = getI32(runtimeHelpers._i52_error_scratch_buffer);
        autoThrowI52(error);
        return result;
    }
    /**
     * Throws for 0 > value > Number.MAX_SAFE_INTEGER
     */
    function getU52(offset) {
        const result = wrapped_c_functions.mono_wasm_u52_to_f64(offset, runtimeHelpers._i52_error_scratch_buffer);
        const error = getI32(runtimeHelpers._i52_error_scratch_buffer);
        autoThrowI52(error);
        return result;
    }
    function getI64Big(offset) {
        if (!(is_bigint_supported)) throw new Error("Assert failed: BigInt is not supported."); // inlined mono_assert
        return HEAPI64[offset >>> 3];
    }
    function getF32(offset) {
        return Module.HEAPF32[offset >>> 2];
    }
    function getF64(offset) {
        return Module.HEAPF64[offset >>> 3];
    }
    let max_int64_big;
    let min_int64_big;
    function afterUpdateGlobalBufferAndViews(buffer) {
        if (is_bigint_supported) {
            max_int64_big = BigInt("9223372036854775807");
            min_int64_big = BigInt("-9223372036854775808");
            HEAPI64 = new BigInt64Array(buffer);
        }
    }
    function getCU64(offset) {
        const lo = getU32(offset);
        const hi = getU32(offset + 4);
        return pack32(lo, hi);
    }
    function setCU64(offset, value) {
        const [lo, hi] = unpack32(value);
        setU32_unchecked(offset, lo);
        setU32_unchecked(offset + 4, hi);
    }
    function withStackAlloc(bytesWanted, f, ud1, ud2, ud3) {
        const sp = Module.stackSave();
        const ptr = Module.stackAlloc(bytesWanted);
        try {
            return f(ptr, ud1, ud2, ud3);
        }
        finally {
            Module.stackRestore(sp);
        }
    }
    // @bytes must be a typed array. space is allocated for it in the native heap
    //  and it is copied to that location. returns the address of the allocation.
    function mono_wasm_load_bytes_into_heap(bytes) {
        const memoryOffset = Module._malloc(bytes.length);
        const heapBytes = new Uint8Array(Module.HEAPU8.buffer, memoryOffset, bytes.length);
        heapBytes.set(bytes);
        return memoryOffset;
    }
    function getEnv(name) {
        let charPtr = 0;
        try {
            charPtr = wrapped_c_functions.mono_wasm_getenv(name);
            if (charPtr === 0)
                return null;
            else
                return Module.UTF8ToString(charPtr);
        }
        finally {
            if (charPtr)
                Module._free(charPtr);
        }
    }
    const BuiltinAtomics = globalThis.Atomics;
    const Atomics = MonoWasmThreads ? {
        storeI32(offset, value) {
            BuiltinAtomics.store(Module.HEAP32, offset >>> 2, value);
        },
        notifyI32(offset, count) {
            BuiltinAtomics.notify(Module.HEAP32, offset >>> 2, count);
        }
    } : {
        storeI32: setI32,
        notifyI32: () => { }
    };

    // Licensed to the .NET Foundation under one or more agreements.
    const maxScratchRoots = 8192;
    let _scratch_root_buffer = null;
    let _scratch_root_free_indices = null;
    let _scratch_root_free_indices_count = 0;
    const _scratch_root_free_instances = [];
    const _external_root_free_instances = [];
    /**
     * Allocates a block of memory that can safely contain pointers into the managed heap.
     * The result object has get(index) and set(index, value) methods that can be used to retrieve and store managed pointers.
     * Once you are done using the root buffer, you must call its release() method.
     * For small numbers of roots, it is preferable to use the mono_wasm_new_root and mono_wasm_new_roots APIs instead.
     */
    function mono_wasm_new_root_buffer(capacity, name) {
        if (capacity <= 0)
            throw new Error("capacity >= 1");
        capacity = capacity | 0;
        const capacityBytes = capacity * 4;
        const offset = Module._malloc(capacityBytes);
        if ((offset % 4) !== 0)
            throw new Error("Malloc returned an unaligned offset");
        _zero_region(offset, capacityBytes);
        return new WasmRootBufferImpl(offset, capacity, true, name);
    }
    /**
     * Creates a root buffer object representing an existing allocation in the native heap and registers
     *  the allocation with the GC. The caller is responsible for managing the lifetime of the allocation.
     */
    function mono_wasm_new_root_buffer_from_pointer(offset, capacity, name) {
        if (capacity <= 0)
            throw new Error("capacity >= 1");
        capacity = capacity | 0;
        const capacityBytes = capacity * 4;
        if ((offset % 4) !== 0)
            throw new Error("Unaligned offset");
        _zero_region(offset, capacityBytes);
        return new WasmRootBufferImpl(offset, capacity, false, name);
    }
    /**
     * Allocates a WasmRoot pointing to a root provided and controlled by external code. Typicaly on managed stack.
     * Releasing this root will not de-allocate the root space. You still need to call .release().
     */
    function mono_wasm_new_external_root(address) {
        let result;
        if (!address)
            throw new Error("address must be a location in the native heap");
        if (_external_root_free_instances.length > 0) {
            result = _external_root_free_instances.pop();
            result._set_address(address);
        }
        else {
            result = new WasmExternalRoot(address);
        }
        return result;
    }
    /**
     * Allocates temporary storage for a pointer into the managed heap.
     * Pointers stored here will be visible to the GC, ensuring that the object they point to aren't moved or collected.
     * If you already have a managed pointer you can pass it as an argument to initialize the temporary storage.
     * The result object has get() and set(value) methods, along with a .value property.
     * When you are done using the root you must call its .release() method.
     */
    function mono_wasm_new_root(value = undefined) {
        let result;
        if (_scratch_root_free_instances.length > 0) {
            result = _scratch_root_free_instances.pop();
        }
        else {
            const index = _mono_wasm_claim_scratch_index();
            const buffer = _scratch_root_buffer;
            result = new WasmJsOwnedRoot(buffer, index);
        }
        if (value !== undefined) {
            if (typeof (value) !== "number")
                throw new Error("value must be an address in the managed heap");
            result.set(value);
        }
        else {
            result.set(0);
        }
        return result;
    }
    /**
     * Allocates 1 or more temporary roots, accepting either a number of roots or an array of pointers.
     * mono_wasm_new_roots(n): returns an array of N zero-initialized roots.
     * mono_wasm_new_roots([a, b, ...]) returns an array of new roots initialized with each element.
     * Each root must be released with its release method, or using the mono_wasm_release_roots API.
     */
    function mono_wasm_new_roots(count_or_values) {
        let result;
        if (Array.isArray(count_or_values)) {
            result = new Array(count_or_values.length);
            for (let i = 0; i < result.length; i++)
                result[i] = mono_wasm_new_root(count_or_values[i]);
        }
        else if ((count_or_values | 0) > 0) {
            result = new Array(count_or_values);
            for (let i = 0; i < result.length; i++)
                result[i] = mono_wasm_new_root();
        }
        else {
            throw new Error("count_or_values must be either an array or a number greater than 0");
        }
        return result;
    }
    /**
     * Releases 1 or more root or root buffer objects.
     * Multiple objects may be passed on the argument list.
     * 'undefined' may be passed as an argument so it is safe to call this method from finally blocks
     *  even if you are not sure all of your roots have been created yet.
     * @param {... WasmRoot} roots
     */
    function mono_wasm_release_roots(...args) {
        for (let i = 0; i < args.length; i++) {
            if (is_nullish(args[i]))
                continue;
            args[i].release();
        }
    }
    function _mono_wasm_release_scratch_index(index) {
        if (index === undefined)
            return;
        _scratch_root_buffer.set(index, 0);
        _scratch_root_free_indices[_scratch_root_free_indices_count] = index;
        _scratch_root_free_indices_count++;
    }
    function _mono_wasm_claim_scratch_index() {
        if (is_nullish(_scratch_root_buffer) || !_scratch_root_free_indices) {
            _scratch_root_buffer = mono_wasm_new_root_buffer(maxScratchRoots, "js roots");
            _scratch_root_free_indices = new Int32Array(maxScratchRoots);
            _scratch_root_free_indices_count = maxScratchRoots;
            for (let i = 0; i < maxScratchRoots; i++)
                _scratch_root_free_indices[i] = maxScratchRoots - i - 1;
        }
        if (_scratch_root_free_indices_count < 1)
            throw new Error("Out of scratch root space");
        const result = _scratch_root_free_indices[_scratch_root_free_indices_count - 1];
        _scratch_root_free_indices_count--;
        return result;
    }
    class WasmRootBufferImpl {
        constructor(offset, capacity, ownsAllocation, name) {
            const capacityBytes = capacity * 4;
            this.__offset = offset;
            this.__offset32 = offset >>> 2;
            this.__count = capacity;
            this.length = capacity;
            this.__handle = wrapped_c_functions.mono_wasm_register_root(offset, capacityBytes, name || "noname");
            this.__ownsAllocation = ownsAllocation;
        }
        _throw_index_out_of_range() {
            throw new Error("index out of range");
        }
        _check_in_range(index) {
            if ((index >= this.__count) || (index < 0))
                this._throw_index_out_of_range();
        }
        get_address(index) {
            this._check_in_range(index);
            return this.__offset + (index * 4);
        }
        get_address_32(index) {
            this._check_in_range(index);
            return this.__offset32 + index;
        }
        // NOTE: These functions do not use the helpers from memory.ts because WasmRoot.get and WasmRoot.set
        //  are hot-spots when you profile any application that uses the bindings extensively.
        get(index) {
            this._check_in_range(index);
            const offset = this.get_address_32(index);
            return Module.HEAPU32[offset];
        }
        set(index, value) {
            const address = this.get_address(index);
            wrapped_c_functions.mono_wasm_write_managed_pointer_unsafe(address, value);
            return value;
        }
        copy_value_from_address(index, sourceAddress) {
            const destinationAddress = this.get_address(index);
            wrapped_c_functions.mono_wasm_copy_managed_pointer(destinationAddress, sourceAddress);
        }
        _unsafe_get(index) {
            return Module.HEAPU32[this.__offset32 + index];
        }
        _unsafe_set(index, value) {
            const address = this.__offset + index;
            wrapped_c_functions.mono_wasm_write_managed_pointer_unsafe(address, value);
        }
        clear() {
            if (this.__offset)
                _zero_region(this.__offset, this.__count * 4);
        }
        release() {
            if (this.__offset && this.__ownsAllocation) {
                wrapped_c_functions.mono_wasm_deregister_root(this.__offset);
                _zero_region(this.__offset, this.__count * 4);
                Module._free(this.__offset);
            }
            this.__handle = this.__offset = this.__count = this.__offset32 = 0;
        }
        toString() {
            return `[root buffer @${this.get_address(0)}, size ${this.__count} ]`;
        }
    }
    class WasmJsOwnedRoot {
        constructor(buffer, index) {
            this.__buffer = buffer; //TODO
            this.__index = index;
        }
        get_address() {
            return this.__buffer.get_address(this.__index);
        }
        get_address_32() {
            return this.__buffer.get_address_32(this.__index);
        }
        get address() {
            return this.__buffer.get_address(this.__index);
        }
        get() {
            const result = this.__buffer._unsafe_get(this.__index);
            return result;
        }
        set(value) {
            const destinationAddress = this.__buffer.get_address(this.__index);
            wrapped_c_functions.mono_wasm_write_managed_pointer_unsafe(destinationAddress, value);
            return value;
        }
        copy_from(source) {
            const sourceAddress = source.address;
            const destinationAddress = this.address;
            wrapped_c_functions.mono_wasm_copy_managed_pointer(destinationAddress, sourceAddress);
        }
        copy_to(destination) {
            const sourceAddress = this.address;
            const destinationAddress = destination.address;
            wrapped_c_functions.mono_wasm_copy_managed_pointer(destinationAddress, sourceAddress);
        }
        copy_from_address(source) {
            const destinationAddress = this.address;
            wrapped_c_functions.mono_wasm_copy_managed_pointer(destinationAddress, source);
        }
        copy_to_address(destination) {
            const sourceAddress = this.address;
            wrapped_c_functions.mono_wasm_copy_managed_pointer(destination, sourceAddress);
        }
        get value() {
            return this.get();
        }
        set value(value) {
            this.set(value);
        }
        valueOf() {
            throw new Error("Implicit conversion of roots to pointers is no longer supported. Use .value or .address as appropriate");
        }
        clear() {
            this.set(0);
        }
        release() {
            if (!this.__buffer)
                throw new Error("No buffer");
            const maxPooledInstances = 128;
            if (_scratch_root_free_instances.length > maxPooledInstances) {
                _mono_wasm_release_scratch_index(this.__index);
                this.__buffer = null;
                this.__index = 0;
            }
            else {
                this.set(0);
                _scratch_root_free_instances.push(this);
            }
        }
        toString() {
            return `[root @${this.address}]`;
        }
    }
    class WasmExternalRoot {
        constructor(address) {
            this.__external_address = MonoObjectRefNull;
            this.__external_address_32 = 0;
            this._set_address(address);
        }
        _set_address(address) {
            this.__external_address = address;
            this.__external_address_32 = address >>> 2;
        }
        get address() {
            return this.__external_address;
        }
        get_address() {
            return this.__external_address;
        }
        get_address_32() {
            return this.__external_address_32;
        }
        get() {
            const result = Module.HEAPU32[this.__external_address_32];
            return result;
        }
        set(value) {
            wrapped_c_functions.mono_wasm_write_managed_pointer_unsafe(this.__external_address, value);
            return value;
        }
        copy_from(source) {
            const sourceAddress = source.address;
            const destinationAddress = this.__external_address;
            wrapped_c_functions.mono_wasm_copy_managed_pointer(destinationAddress, sourceAddress);
        }
        copy_to(destination) {
            const sourceAddress = this.__external_address;
            const destinationAddress = destination.address;
            wrapped_c_functions.mono_wasm_copy_managed_pointer(destinationAddress, sourceAddress);
        }
        copy_from_address(source) {
            const destinationAddress = this.__external_address;
            wrapped_c_functions.mono_wasm_copy_managed_pointer(destinationAddress, source);
        }
        copy_to_address(destination) {
            const sourceAddress = this.__external_address;
            wrapped_c_functions.mono_wasm_copy_managed_pointer(destination, sourceAddress);
        }
        get value() {
            return this.get();
        }
        set value(value) {
            this.set(value);
        }
        valueOf() {
            throw new Error("Implicit conversion of roots to pointers is no longer supported. Use .value or .address as appropriate");
        }
        clear() {
            this.set(0);
        }
        release() {
            const maxPooledInstances = 128;
            if (_external_root_free_instances.length < maxPooledInstances)
                _external_root_free_instances.push(this);
        }
        toString() {
            return `[external root @${this.address}]`;
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const cs_to_js_marshalers = new Map();
    const js_to_cs_marshalers = new Map();
    const bound_cs_function_symbol = Symbol.for("wasm bound_cs_function");
    const bound_js_function_symbol = Symbol.for("wasm bound_js_function");
    /**
     * JSFunctionSignature is pointer to [
     *      Version: number,
     *      ArgumentCount: number,
     *      exc:  { jsType: JsTypeFlags, type:MarshalerType, restype:MarshalerType, arg1type:MarshalerType, arg2type:MarshalerType, arg3type:MarshalerType}
     *      res:  { jsType: JsTypeFlags, type:MarshalerType, restype:MarshalerType, arg1type:MarshalerType, arg2type:MarshalerType, arg3type:MarshalerType}
     *      arg1: { jsType: JsTypeFlags, type:MarshalerType, restype:MarshalerType, arg1type:MarshalerType, arg2type:MarshalerType, arg3type:MarshalerType}
     *      arg2: { jsType: JsTypeFlags, type:MarshalerType, restype:MarshalerType, arg1type:MarshalerType, arg2type:MarshalerType, arg3type:MarshalerType}
     *      ...
     *      ]
     *
     * Layout of the call stack frame buffers is array of JSMarshalerArgument
     * JSMarshalerArguments is pointer to [
     *      exc:  {type:MarshalerType, handle: IntPtr, data: Int64|Ref*|Void* },
     *      res:  {type:MarshalerType, handle: IntPtr, data: Int64|Ref*|Void* },
     *      arg1: {type:MarshalerType, handle: IntPtr, data: Int64|Ref*|Void* },
     *      arg2: {type:MarshalerType, handle: IntPtr, data: Int64|Ref*|Void* },
     *      ...
     *      ]
     */
    const JavaScriptMarshalerArgSize = 16;
    const JSMarshalerTypeSize = 32;
    const JSMarshalerSignatureHeaderSize = 4 + 4; // without Exception and Result
    function alloc_stack_frame(size) {
        const anyModule = Module;
        const args = anyModule.stackAlloc(JavaScriptMarshalerArgSize * size);
        if (!(args && args % 8 == 0)) throw new Error("Assert failed: Arg alignment"); // inlined mono_assert
        const exc = get_arg(args, 0);
        set_arg_type(exc, MarshalerType.None);
        const res = get_arg(args, 1);
        set_arg_type(res, MarshalerType.None);
        return args;
    }
    function get_arg(args, index) {
        if (!(args)) throw new Error("Assert failed: Null args"); // inlined mono_assert
        return args + (index * JavaScriptMarshalerArgSize);
    }
    function is_args_exception(args) {
        if (!(args)) throw new Error("Assert failed: Null args"); // inlined mono_assert
        const exceptionType = get_arg_type(args);
        return exceptionType !== MarshalerType.None;
    }
    function get_sig(signature, index) {
        if (!(signature)) throw new Error("Assert failed: Null signatures"); // inlined mono_assert
        return signature + (index * JSMarshalerTypeSize) + JSMarshalerSignatureHeaderSize;
    }
    function get_signature_type(sig) {
        if (!(sig)) throw new Error("Assert failed: Null sig"); // inlined mono_assert
        return getU32(sig);
    }
    function get_signature_res_type(sig) {
        if (!(sig)) throw new Error("Assert failed: Null sig"); // inlined mono_assert
        return getU32(sig + 16);
    }
    function get_signature_custom_code(sig) {
        if (!(sig)) throw new Error("Assert failed: Null sig"); // inlined mono_assert
        return getU32(sig + 8);
    }
    function get_signature_custom_code_len(sig) {
        if (!(sig)) throw new Error("Assert failed: Null sig"); // inlined mono_assert
        return getU32(sig + 12);
    }
    function get_signature_arg1_type(sig) {
        if (!(sig)) throw new Error("Assert failed: Null sig"); // inlined mono_assert
        return getU32(sig + 20);
    }
    function get_signature_arg2_type(sig) {
        if (!(sig)) throw new Error("Assert failed: Null sig"); // inlined mono_assert
        return getU32(sig + 24);
    }
    function get_signature_arg3_type(sig) {
        if (!(sig)) throw new Error("Assert failed: Null sig"); // inlined mono_assert
        return getU32(sig + 28);
    }
    function get_signature_argument_count(signature) {
        if (!(signature)) throw new Error("Assert failed: Null signatures"); // inlined mono_assert
        return getI32(signature + 4);
    }
    function get_signature_version(signature) {
        if (!(signature)) throw new Error("Assert failed: Null signatures"); // inlined mono_assert
        return getI32(signature);
    }
    function get_sig_type(sig) {
        if (!(sig)) throw new Error("Assert failed: Null signatures"); // inlined mono_assert
        return getU32(sig);
    }
    function get_arg_type(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        const type = getU32(arg + 12);
        return type;
    }
    function get_arg_element_type(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        const type = getU32(arg + 4);
        return type;
    }
    function set_arg_type(arg, type) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        setU32(arg + 12, type);
    }
    function set_arg_element_type(arg, type) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        setU32(arg + 4, type);
    }
    function get_arg_b8(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        return !!getU8(arg);
    }
    function get_arg_u8(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        return getU8(arg);
    }
    function get_arg_u16(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        return getU16(arg);
    }
    function get_arg_i16(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        return getI16(arg);
    }
    function get_arg_i32(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        return getI32(arg);
    }
    function get_arg_intptr(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        return getU32(arg);
    }
    function get_arg_i52(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        // we know that the range check and conversion from Int64 was be done on C# side
        return getF64(arg);
    }
    function get_arg_i64_big(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        return getI64Big(arg);
    }
    function get_arg_date(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        const unixTime = getF64(arg);
        const date = new Date(unixTime);
        return date;
    }
    function get_arg_f32(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        return getF32(arg);
    }
    function get_arg_f64(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        return getF64(arg);
    }
    function set_arg_b8(arg, value) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        if (!(typeof value === "boolean")) throw new Error(`Assert failed: Value is not a Boolean: ${value} (${typeof (value)})`); // inlined mono_assert
        setU8(arg, value ? 1 : 0);
    }
    function set_arg_u8(arg, value) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        setU8(arg, value);
    }
    function set_arg_u16(arg, value) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        setU16(arg, value);
    }
    function set_arg_i16(arg, value) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        setI16(arg, value);
    }
    function set_arg_i32(arg, value) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        setI32(arg, value);
    }
    function set_arg_intptr(arg, value) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        setU32(arg, value);
    }
    function set_arg_i52(arg, value) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        if (!(Number.isSafeInteger(value))) throw new Error(`Assert failed: Value is not an integer: ${value} (${typeof (value)})`); // inlined mono_assert
        // we know that conversion to Int64 would be done on C# side
        setF64(arg, value);
    }
    function set_arg_i64_big(arg, value) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        setI64Big(arg, value);
    }
    function set_arg_date(arg, value) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        // getTime() is always UTC
        const unixTime = value.getTime();
        setF64(arg, unixTime);
    }
    function set_arg_f64(arg, value) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        setF64(arg, value);
    }
    function set_arg_f32(arg, value) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        setF32(arg, value);
    }
    function get_arg_js_handle(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        return getU32(arg + 4);
    }
    function set_js_handle(arg, jsHandle) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        setU32(arg + 4, jsHandle);
    }
    function get_arg_gc_handle(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        return getU32(arg + 4);
    }
    function set_gc_handle(arg, gcHandle) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        setU32(arg + 4, gcHandle);
    }
    function get_string_root(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        return mono_wasm_new_external_root(arg);
    }
    function get_arg_length(arg) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        return getI32(arg + 8);
    }
    function set_arg_length(arg, size) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        setI32(arg + 8, size);
    }
    function set_root(arg, root) {
        if (!(arg)) throw new Error("Assert failed: Null arg"); // inlined mono_assert
        setU32(arg + 0, root.get_address());
    }
    class ManagedObject {
        dispose() {
            teardown_managed_proxy(this, GCHandleNull);
        }
        get isDisposed() {
            return this[js_owned_gc_handle_symbol] === GCHandleNull;
        }
        toString() {
            return `CsObject(gc_handle: ${this[js_owned_gc_handle_symbol]})`;
        }
    }
    class ManagedError extends Error {
        constructor(message) {
            super(message);
            this.superStack = Object.getOwnPropertyDescriptor(this, "stack"); // this works on Chrome
            Object.defineProperty(this, "stack", {
                get: this.getManageStack,
            });
        }
        getSuperStack() {
            if (this.superStack) {
                return this.superStack.value;
            }
            return super.stack; // this works on FF
        }
        getManageStack() {
            const gc_handle = this[js_owned_gc_handle_symbol];
            if (gc_handle) {
                const managed_stack = runtimeHelpers.javaScriptExports.get_managed_stack_trace(gc_handle);
                if (managed_stack) {
                    return managed_stack + "\n" + this.getSuperStack();
                }
            }
            return this.getSuperStack();
        }
        dispose() {
            teardown_managed_proxy(this, GCHandleNull);
        }
        get isDisposed() {
            return this[js_owned_gc_handle_symbol] === GCHandleNull;
        }
    }
    function get_signature_marshaler(signature, index) {
        if (!(signature)) throw new Error("Assert failed: Null signatures"); // inlined mono_assert
        const sig = get_sig(signature, index);
        return getU32(sig + 8);
    }
    function array_element_size(element_type) {
        return element_type == MarshalerType.Byte ? 1
            : element_type == MarshalerType.Int32 ? 4
                : element_type == MarshalerType.Int52 ? 8
                    : element_type == MarshalerType.Double ? 8
                        : element_type == MarshalerType.String ? JavaScriptMarshalerArgSize
                            : element_type == MarshalerType.Object ? JavaScriptMarshalerArgSize
                                : element_type == MarshalerType.JSObject ? JavaScriptMarshalerArgSize
                                    : -1;
    }
    class MemoryView {
        constructor(_pointer, _length, _viewType) {
            this._pointer = _pointer;
            this._length = _length;
            this._viewType = _viewType;
        }
        _unsafe_create_view() {
            // this view must be short lived so that it doesn't fail after wasm memory growth
            // for that reason we also don't give the view out to end user and provide set/slice/copyTo API instead
            const view = this._viewType == 0 /* MemoryViewType.Byte */ ? new Uint8Array(Module.HEAPU8.buffer, this._pointer, this._length)
                : this._viewType == 1 /* MemoryViewType.Int32 */ ? new Int32Array(Module.HEAP32.buffer, this._pointer, this._length)
                    : this._viewType == 2 /* MemoryViewType.Double */ ? new Float64Array(Module.HEAPF64.buffer, this._pointer, this._length)
                        : null;
            if (!view)
                throw new Error("NotImplementedException");
            return view;
        }
        set(source, targetOffset) {
            if (!(!this.isDisposed)) throw new Error("Assert failed: ObjectDisposedException"); // inlined mono_assert
            const targetView = this._unsafe_create_view();
            if (!(source && targetView && source.constructor === targetView.constructor)) throw new Error(`Assert failed: Expected ${targetView.constructor}`); // inlined mono_assert
            targetView.set(source, targetOffset);
            // TODO consider memory write barrier
        }
        copyTo(target, sourceOffset) {
            if (!(!this.isDisposed)) throw new Error("Assert failed: ObjectDisposedException"); // inlined mono_assert
            const sourceView = this._unsafe_create_view();
            if (!(target && sourceView && target.constructor === sourceView.constructor)) throw new Error(`Assert failed: Expected ${sourceView.constructor}`); // inlined mono_assert
            const trimmedSource = sourceView.subarray(sourceOffset);
            // TODO consider memory read barrier
            target.set(trimmedSource);
        }
        slice(start, end) {
            if (!(!this.isDisposed)) throw new Error("Assert failed: ObjectDisposedException"); // inlined mono_assert
            const sourceView = this._unsafe_create_view();
            // TODO consider memory read barrier
            return sourceView.slice(start, end);
        }
        get length() {
            if (!(!this.isDisposed)) throw new Error("Assert failed: ObjectDisposedException"); // inlined mono_assert
            return this._length;
        }
        get byteLength() {
            if (!(!this.isDisposed)) throw new Error("Assert failed: ObjectDisposedException"); // inlined mono_assert
            return this._viewType == 0 /* MemoryViewType.Byte */ ? this._length
                : this._viewType == 1 /* MemoryViewType.Int32 */ ? this._length << 2
                    : this._viewType == 2 /* MemoryViewType.Double */ ? this._length << 3
                        : 0;
        }
    }
    class Span extends MemoryView {
        constructor(pointer, length, viewType) {
            super(pointer, length, viewType);
            this.is_disposed = false;
        }
        dispose() {
            this.is_disposed = true;
        }
        get isDisposed() {
            return this.is_disposed;
        }
    }
    class ArraySegment extends MemoryView {
        constructor(pointer, length, viewType) {
            super(pointer, length, viewType);
        }
        dispose() {
            teardown_managed_proxy(this, GCHandleNull);
        }
        get isDisposed() {
            return this[js_owned_gc_handle_symbol] === GCHandleNull;
        }
    }
    // please keep in sync with src\libraries\System.Runtime.InteropServices.JavaScript\src\System\Runtime\InteropServices\JavaScript\MarshalerType.cs
    var MarshalerType;
    (function (MarshalerType) {
        MarshalerType[MarshalerType["None"] = 0] = "None";
        MarshalerType[MarshalerType["Void"] = 1] = "Void";
        MarshalerType[MarshalerType["Discard"] = 2] = "Discard";
        MarshalerType[MarshalerType["Boolean"] = 3] = "Boolean";
        MarshalerType[MarshalerType["Byte"] = 4] = "Byte";
        MarshalerType[MarshalerType["Char"] = 5] = "Char";
        MarshalerType[MarshalerType["Int16"] = 6] = "Int16";
        MarshalerType[MarshalerType["Int32"] = 7] = "Int32";
        MarshalerType[MarshalerType["Int52"] = 8] = "Int52";
        MarshalerType[MarshalerType["BigInt64"] = 9] = "BigInt64";
        MarshalerType[MarshalerType["Double"] = 10] = "Double";
        MarshalerType[MarshalerType["Single"] = 11] = "Single";
        MarshalerType[MarshalerType["IntPtr"] = 12] = "IntPtr";
        MarshalerType[MarshalerType["JSObject"] = 13] = "JSObject";
        MarshalerType[MarshalerType["Object"] = 14] = "Object";
        MarshalerType[MarshalerType["String"] = 15] = "String";
        MarshalerType[MarshalerType["Exception"] = 16] = "Exception";
        MarshalerType[MarshalerType["DateTime"] = 17] = "DateTime";
        MarshalerType[MarshalerType["DateTimeOffset"] = 18] = "DateTimeOffset";
        MarshalerType[MarshalerType["Nullable"] = 19] = "Nullable";
        MarshalerType[MarshalerType["Task"] = 20] = "Task";
        MarshalerType[MarshalerType["Array"] = 21] = "Array";
        MarshalerType[MarshalerType["ArraySegment"] = 22] = "ArraySegment";
        MarshalerType[MarshalerType["Span"] = 23] = "Span";
        MarshalerType[MarshalerType["Action"] = 24] = "Action";
        MarshalerType[MarshalerType["Function"] = 25] = "Function";
        // only on runtime
        MarshalerType[MarshalerType["JSException"] = 26] = "JSException";
    })(MarshalerType || (MarshalerType = {}));

    // Licensed to the .NET Foundation under one or more agreements.
    class StringDecoder {
        init_fields() {
            if (!this.mono_wasm_string_decoder_buffer) {
                this.mono_text_decoder = typeof TextDecoder !== "undefined" ? new TextDecoder("utf-16le") : null;
                this.mono_wasm_string_root = mono_wasm_new_root();
                this.mono_wasm_string_decoder_buffer = Module._malloc(12);
            }
        }
        /**
         * @deprecated Not GC or thread safe
         */
        copy(mono_string) {
            this.init_fields();
            if (mono_string === MonoStringNull)
                return null;
            this.mono_wasm_string_root.value = mono_string;
            const result = this.copy_root(this.mono_wasm_string_root);
            this.mono_wasm_string_root.value = MonoStringNull;
            return result;
        }
        copy_root(root) {
            this.init_fields();
            if (root.value === MonoStringNull)
                return null;
            const ppChars = this.mono_wasm_string_decoder_buffer + 0, pLengthBytes = this.mono_wasm_string_decoder_buffer + 4, pIsInterned = this.mono_wasm_string_decoder_buffer + 8;
            wrapped_c_functions.mono_wasm_string_get_data_ref(root.address, ppChars, pLengthBytes, pIsInterned);
            let result = undefined;
            const lengthBytes = getI32(pLengthBytes), pChars = getU32(ppChars), isInterned = getI32(pIsInterned);
            if (isInterned)
                result = interned_string_table.get(root.value);
            if (result === undefined) {
                if (lengthBytes && pChars) {
                    result = this.decode(pChars, pChars + lengthBytes);
                    if (isInterned)
                        interned_string_table.set(root.value, result);
                }
                else
                    result = mono_wasm_empty_string;
            }
            if (result === undefined)
                throw new Error(`internal error when decoding string at location ${root.value}`);
            return result;
        }
        decode(start, end) {
            let str = "";
            if (this.mono_text_decoder) {
                // When threading is enabled, TextDecoder does not accept a view of a
                // SharedArrayBuffer, we must make a copy of the array first.
                // See https://github.com/whatwg/encoding/issues/172
                const subArray = typeof SharedArrayBuffer !== "undefined" && Module.HEAPU8.buffer instanceof SharedArrayBuffer
                    ? Module.HEAPU8.slice(start, end)
                    : Module.HEAPU8.subarray(start, end);
                str = this.mono_text_decoder.decode(subArray);
            }
            else {
                for (let i = 0; i < end - start; i += 2) {
                    const char = Module.getValue(start + i, "i16");
                    str += String.fromCharCode(char);
                }
            }
            return str;
        }
    }
    const interned_string_table = new Map();
    const interned_js_string_table = new Map();
    let _empty_string_ptr = 0;
    const _interned_string_full_root_buffers = [];
    let _interned_string_current_root_buffer = null;
    let _interned_string_current_root_buffer_count = 0;
    const string_decoder = new StringDecoder();
    const mono_wasm_empty_string = "";
    /**
     * @deprecated Not GC or thread safe
     */
    function conv_string(mono_obj) {
        return string_decoder.copy(mono_obj);
    }
    function conv_string_root(root) {
        return string_decoder.copy_root(root);
    }
    // Ensures the string is already interned on both the managed and JavaScript sides,
    //  then returns the interned string value (to provide fast reference comparisons like C#)
    function mono_intern_string(string) {
        if (string.length === 0)
            return mono_wasm_empty_string;
        // HACK: This would normally be unsafe, but the return value of js_string_to_mono_string_interned is always an
        //  interned string, so the address will never change and it is safe for us to use the raw pointer. Don't do this though
        const ptr = js_string_to_mono_string_interned(string);
        const result = interned_string_table.get(ptr);
        if (is_nullish(result))
            throw new Error("internal error: interned_string_table did not contain string after js_string_to_mono_string_interned");
        return result;
    }
    function _store_string_in_intern_table(string, root, internIt) {
        if (!root.value)
            throw new Error("null pointer passed to _store_string_in_intern_table");
        const internBufferSize = 8192;
        if (_interned_string_current_root_buffer_count >= internBufferSize) {
            _interned_string_full_root_buffers.push(_interned_string_current_root_buffer);
            _interned_string_current_root_buffer = null;
        }
        if (!_interned_string_current_root_buffer) {
            _interned_string_current_root_buffer = mono_wasm_new_root_buffer(internBufferSize, "interned strings");
            _interned_string_current_root_buffer_count = 0;
        }
        const rootBuffer = _interned_string_current_root_buffer;
        const index = _interned_string_current_root_buffer_count++;
        // Store the managed string into the managed intern table. This can theoretically
        //  provide a different managed object than the one we passed in, so update our
        //  pointer (stored in the root) with the result.
        if (internIt) {
            wrapped_c_functions.mono_wasm_intern_string_ref(root.address);
            if (!root.value)
                throw new Error("mono_wasm_intern_string_ref produced a null pointer");
        }
        interned_js_string_table.set(string, root.value);
        interned_string_table.set(root.value, string);
        if ((string.length === 0) && !_empty_string_ptr)
            _empty_string_ptr = root.value;
        // Copy the final pointer into our interned string root buffer to ensure the string
        //  remains rooted. TODO: Is this actually necessary?
        rootBuffer.copy_value_from_address(index, root.address);
    }
    function js_string_to_mono_string_interned_root(string, result) {
        let text;
        if (typeof (string) === "symbol") {
            text = string.description;
            if (typeof (text) !== "string")
                text = Symbol.keyFor(string);
            if (typeof (text) !== "string")
                text = "<unknown Symbol>";
        }
        else if (typeof (string) === "string") {
            text = string;
        }
        if (typeof (text) !== "string") {
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
            throw new Error(`Argument to js_string_to_mono_string_interned must be a string but was ${string}`);
        }
        if ((text.length === 0) && _empty_string_ptr) {
            result.set(_empty_string_ptr);
            return;
        }
        const ptr = interned_js_string_table.get(text);
        if (ptr) {
            result.set(ptr);
            return;
        }
        js_string_to_mono_string_new_root(text, result);
        _store_string_in_intern_table(text, result, true);
    }
    function js_string_to_mono_string_root(string, result) {
        result.clear();
        if (string === null)
            return;
        else if (typeof (string) === "symbol")
            js_string_to_mono_string_interned_root(string, result);
        else if (typeof (string) !== "string")
            throw new Error("Expected string argument, got " + typeof (string));
        else if (string.length === 0)
            // Always use an interned pointer for empty strings
            js_string_to_mono_string_interned_root(string, result);
        else {
            // Looking up large strings in the intern table will require the JS runtime to
            //  potentially hash them and then do full byte-by-byte comparisons, which is
            //  very expensive. Because we can not guarantee it won't happen, try to minimize
            //  the cost of this and prevent performance issues for large strings
            if (string.length <= 256) {
                const interned = interned_js_string_table.get(string);
                if (interned) {
                    result.set(interned);
                    return;
                }
            }
            js_string_to_mono_string_new_root(string, result);
        }
    }
    function js_string_to_mono_string_new_root(string, result) {
        const buffer = Module._malloc((string.length + 1) * 2);
        const buffer16 = (buffer >>> 1) | 0;
        for (let i = 0; i < string.length; i++)
            Module.HEAP16[buffer16 + i] = string.charCodeAt(i);
        Module.HEAP16[buffer16 + string.length] = 0;
        wrapped_c_functions.mono_wasm_string_from_utf16_ref(buffer, string.length, result.address);
        Module._free(buffer);
    }
    /**
     * @deprecated Not GC or thread safe
     */
    function js_string_to_mono_string_interned(string) {
        const temp = mono_wasm_new_root();
        try {
            js_string_to_mono_string_interned_root(string, temp);
            return temp.value;
        }
        finally {
            temp.release();
        }
    }
    /**
     * @deprecated Not GC or thread safe
     */
    function js_string_to_mono_string(string) {
        const temp = mono_wasm_new_root();
        try {
            js_string_to_mono_string_root(string, temp);
            return temp.value;
        }
        finally {
            temp.release();
        }
    }
    /**
     * @deprecated Not GC or thread safe
     */
    function js_string_to_mono_string_new(string) {
        const temp = mono_wasm_new_root();
        try {
            js_string_to_mono_string_new_root(string, temp);
            return temp.value;
        }
        finally {
            temp.release();
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    function initialize_marshalers_to_cs() {
        if (js_to_cs_marshalers.size == 0) {
            js_to_cs_marshalers.set(MarshalerType.Array, _marshal_array_to_cs);
            js_to_cs_marshalers.set(MarshalerType.Span, _marshal_span_to_cs);
            js_to_cs_marshalers.set(MarshalerType.ArraySegment, _marshal_array_segment_to_cs);
            js_to_cs_marshalers.set(MarshalerType.Boolean, _marshal_bool_to_cs);
            js_to_cs_marshalers.set(MarshalerType.Byte, _marshal_byte_to_cs);
            js_to_cs_marshalers.set(MarshalerType.Char, _marshal_char_to_cs);
            js_to_cs_marshalers.set(MarshalerType.Int16, _marshal_int16_to_cs);
            js_to_cs_marshalers.set(MarshalerType.Int32, _marshal_int32_to_cs);
            js_to_cs_marshalers.set(MarshalerType.Int52, _marshal_int52_to_cs);
            js_to_cs_marshalers.set(MarshalerType.BigInt64, _marshal_bigint64_to_cs);
            js_to_cs_marshalers.set(MarshalerType.Double, _marshal_double_to_cs);
            js_to_cs_marshalers.set(MarshalerType.Single, _marshal_float_to_cs);
            js_to_cs_marshalers.set(MarshalerType.IntPtr, marshal_intptr_to_cs);
            js_to_cs_marshalers.set(MarshalerType.DateTime, _marshal_date_time_to_cs);
            js_to_cs_marshalers.set(MarshalerType.DateTimeOffset, _marshal_date_time_offset_to_cs);
            js_to_cs_marshalers.set(MarshalerType.String, _marshal_string_to_cs);
            js_to_cs_marshalers.set(MarshalerType.Exception, marshal_exception_to_cs);
            js_to_cs_marshalers.set(MarshalerType.JSException, marshal_exception_to_cs);
            js_to_cs_marshalers.set(MarshalerType.JSObject, _marshal_js_object_to_cs);
            js_to_cs_marshalers.set(MarshalerType.Object, _marshal_cs_object_to_cs);
            js_to_cs_marshalers.set(MarshalerType.Task, _marshal_task_to_cs);
            js_to_cs_marshalers.set(MarshalerType.Action, _marshal_function_to_cs);
            js_to_cs_marshalers.set(MarshalerType.Function, _marshal_function_to_cs);
            js_to_cs_marshalers.set(MarshalerType.None, _marshal_null_to_cs); // also void
            js_to_cs_marshalers.set(MarshalerType.Discard, _marshal_null_to_cs); // also void
            js_to_cs_marshalers.set(MarshalerType.Void, _marshal_null_to_cs); // also void
        }
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function generate_arg_marshal_to_cs(sig, index, arg_offset, sig_offset, jsname, closure) {
        let converters = "";
        let converter_types = "";
        let call_body = "";
        const converter_name = "converter" + index;
        let converter_name_arg1 = "null";
        let converter_name_arg2 = "null";
        let converter_name_arg3 = "null";
        let converter_name_res = "null";
        let marshaler_type = get_signature_type(sig);
        if (marshaler_type === MarshalerType.None || marshaler_type === MarshalerType.Void) {
            return {
                converters,
                call_body,
                marshaler_type
            };
        }
        const marshaler_type_res = get_signature_res_type(sig);
        if (marshaler_type_res !== MarshalerType.None) {
            const converter = js_to_cs_marshalers.get(marshaler_type_res);
            if (!(converter && typeof converter === "function")) throw new Error(`Assert failed: Unknow converter for type ${marshaler_type_res} at ${index}`); // inlined mono_assert
            if (marshaler_type != MarshalerType.Nullable) {
                converter_name_res = "converter" + index + "_res";
                converters += ", " + converter_name_res;
                converter_types += " " + MarshalerType[marshaler_type_res];
                closure[converter_name_res] = converter;
            }
            else {
                marshaler_type = marshaler_type_res;
            }
        }
        const marshaler_type_arg1 = get_signature_arg1_type(sig);
        if (marshaler_type_arg1 !== MarshalerType.None) {
            const converter = cs_to_js_marshalers.get(marshaler_type_arg1);
            if (!(converter && typeof converter === "function")) throw new Error(`Assert failed: Unknow converter for type ${marshaler_type_arg1} at ${index}`); // inlined mono_assert
            converter_name_arg1 = "converter" + index + "_arg1";
            converters += ", " + converter_name_arg1;
            converter_types += " " + MarshalerType[marshaler_type_arg1];
            closure[converter_name_arg1] = converter;
        }
        const marshaler_type_arg2 = get_signature_arg2_type(sig);
        if (marshaler_type_arg2 !== MarshalerType.None) {
            const converter = cs_to_js_marshalers.get(marshaler_type_arg2);
            if (!(converter && typeof converter === "function")) throw new Error(`Assert failed: Unknow converter for type ${marshaler_type_arg2} at ${index}`); // inlined mono_assert
            converter_name_arg2 = "converter" + index + "_arg2";
            converters += ", " + converter_name_arg2;
            converter_types += " " + MarshalerType[marshaler_type_arg2];
            closure[converter_name_arg2] = converter;
        }
        const marshaler_type_arg3 = get_signature_arg3_type(sig);
        if (marshaler_type_arg3 !== MarshalerType.None) {
            const converter = cs_to_js_marshalers.get(marshaler_type_arg3);
            if (!(converter && typeof converter === "function")) throw new Error(`Assert failed: Unknow converter for type ${marshaler_type_arg3} at ${index}`); // inlined mono_assert
            converter_name_arg3 = "converter" + index + "_arg3";
            converters += ", " + converter_name_arg3;
            converter_types += " " + MarshalerType[marshaler_type_arg3];
            closure[converter_name_arg3] = converter;
        }
        const converter = js_to_cs_marshalers.get(marshaler_type);
        const arg_type_name = MarshalerType[marshaler_type];
        if (!(converter && typeof converter === "function")) throw new Error(`Assert failed: Unknow converter for type ${arg_type_name} (${marshaler_type}) at ${index} `); // inlined mono_assert
        converters += ", " + converter_name;
        converter_types += " " + arg_type_name;
        closure[converter_name] = converter;
        if (marshaler_type == MarshalerType.Task) {
            call_body = `  ${converter_name}(args + ${arg_offset}, ${jsname}, signature + ${sig_offset}, ${converter_name_res}); // ${converter_types} \n`;
        }
        else if (marshaler_type == MarshalerType.Action || marshaler_type == MarshalerType.Function) {
            call_body = `  ${converter_name}(args + ${arg_offset}, ${jsname}, signature + ${sig_offset}, ${converter_name_res}, ${converter_name_arg1}, ${converter_name_arg2}, ${converter_name_arg2}); // ${converter_types} \n`;
        }
        else {
            call_body = `  ${converter_name}(args + ${arg_offset}, ${jsname}, signature + ${sig_offset}); // ${converter_types} \n`;
        }
        return {
            converters,
            call_body,
            marshaler_type
        };
    }
    function _marshal_bool_to_cs(arg, value) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
        }
        else {
            set_arg_type(arg, MarshalerType.Boolean);
            set_arg_b8(arg, value);
        }
    }
    function _marshal_byte_to_cs(arg, value) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
        }
        else {
            set_arg_type(arg, MarshalerType.Byte);
            set_arg_u8(arg, value);
        }
    }
    function _marshal_char_to_cs(arg, value) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
        }
        else {
            set_arg_type(arg, MarshalerType.Char);
            set_arg_u16(arg, value);
        }
    }
    function _marshal_int16_to_cs(arg, value) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
        }
        else {
            set_arg_type(arg, MarshalerType.Int16);
            set_arg_i16(arg, value);
        }
    }
    function _marshal_int32_to_cs(arg, value) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
        }
        else {
            set_arg_type(arg, MarshalerType.Int32);
            set_arg_i32(arg, value);
        }
    }
    function _marshal_int52_to_cs(arg, value) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
        }
        else {
            set_arg_type(arg, MarshalerType.Int52);
            set_arg_i52(arg, value);
        }
    }
    function _marshal_bigint64_to_cs(arg, value) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
        }
        else {
            set_arg_type(arg, MarshalerType.BigInt64);
            set_arg_i64_big(arg, value);
        }
    }
    function _marshal_double_to_cs(arg, value) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
        }
        else {
            set_arg_type(arg, MarshalerType.Double);
            set_arg_f64(arg, value);
        }
    }
    function _marshal_float_to_cs(arg, value) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
        }
        else {
            set_arg_type(arg, MarshalerType.Single);
            set_arg_f32(arg, value);
        }
    }
    function marshal_intptr_to_cs(arg, value) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
        }
        else {
            set_arg_type(arg, MarshalerType.IntPtr);
            set_arg_intptr(arg, value);
        }
    }
    function _marshal_date_time_to_cs(arg, value) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
        }
        else {
            if (!(value instanceof Date)) throw new Error("Assert failed: Value is not a Date"); // inlined mono_assert
            set_arg_type(arg, MarshalerType.DateTime);
            set_arg_date(arg, value);
        }
    }
    function _marshal_date_time_offset_to_cs(arg, value) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
        }
        else {
            if (!(value instanceof Date)) throw new Error("Assert failed: Value is not a Date"); // inlined mono_assert
            set_arg_type(arg, MarshalerType.DateTimeOffset);
            set_arg_date(arg, value);
        }
    }
    function _marshal_string_to_cs(arg, value) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
        }
        else {
            set_arg_type(arg, MarshalerType.String);
            if (!(typeof value === "string")) throw new Error("Assert failed: Value is not a String"); // inlined mono_assert
            _marshal_string_to_cs_impl(arg, value);
        }
    }
    function _marshal_string_to_cs_impl(arg, value) {
        const root = get_string_root(arg);
        set_arg_intptr(arg, 0);
        try {
            js_string_to_mono_string_root(value, root);
        }
        finally {
            root.release();
        }
    }
    function _marshal_null_to_cs(arg) {
        set_arg_type(arg, MarshalerType.None);
    }
    function _marshal_function_to_cs(arg, value, _, res_converter, arg1_converter, arg2_converter, arg3_converter) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
            return;
        }
        if (!(value && value instanceof Function)) throw new Error("Assert failed: Value is not a Function"); // inlined mono_assert
        // TODO: we could try to cache value -> existing JSHandle
        const marshal_function_to_cs_wrapper = (args) => {
            const exc = get_arg(args, 0);
            const res = get_arg(args, 1);
            const arg1 = get_arg(args, 2);
            const arg2 = get_arg(args, 3);
            const arg3 = get_arg(args, 4);
            try {
                let arg1_js = undefined;
                let arg2_js = undefined;
                let arg3_js = undefined;
                if (arg1_converter) {
                    arg1_js = arg1_converter(arg1);
                }
                if (arg2_converter) {
                    arg2_js = arg2_converter(arg2);
                }
                if (arg3_converter) {
                    arg3_js = arg3_converter(arg3);
                }
                const res_js = value(arg1_js, arg2_js, arg3_js);
                if (res_converter) {
                    res_converter(res, res_js);
                }
            }
            catch (ex) {
                marshal_exception_to_cs(exc, ex);
            }
        };
        marshal_function_to_cs_wrapper[bound_js_function_symbol] = true;
        const bound_function_handle = mono_wasm_get_js_handle(marshal_function_to_cs_wrapper);
        set_js_handle(arg, bound_function_handle);
        set_arg_type(arg, MarshalerType.Function); //TODO or action ?
    }
    class TaskCallbackHolder {
        constructor(promise) {
            this.promise = promise;
        }
        dispose() {
            teardown_managed_proxy(this, GCHandleNull);
        }
        get isDisposed() {
            return this[js_owned_gc_handle_symbol] === GCHandleNull;
        }
    }
    function _marshal_task_to_cs(arg, value, _, res_converter) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
            return;
        }
        if (!(isThenable(value))) throw new Error("Assert failed: Value is not a Promise"); // inlined mono_assert
        const gc_handle = runtimeHelpers.javaScriptExports.create_task_callback();
        set_gc_handle(arg, gc_handle);
        set_arg_type(arg, MarshalerType.Task);
        const holder = new TaskCallbackHolder(value);
        setup_managed_proxy(holder, gc_handle);
        value.then(data => {
            runtimeHelpers.javaScriptExports.complete_task(gc_handle, null, data, res_converter || _marshal_cs_object_to_cs);
            teardown_managed_proxy(holder, gc_handle); // this holds holder alive for finalizer, until the promise is freed, (holding promise instead would not work)
        }).catch(reason => {
            runtimeHelpers.javaScriptExports.complete_task(gc_handle, reason, null, undefined);
            teardown_managed_proxy(holder, gc_handle); // this holds holder alive for finalizer, until the promise is freed
        });
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function marshal_exception_to_cs(arg, value) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
        }
        else if (value instanceof ManagedError) {
            set_arg_type(arg, MarshalerType.Exception);
            // this is managed exception round-trip
            const gc_handle = assert_not_disposed(value);
            set_gc_handle(arg, gc_handle);
        }
        else {
            if (!(typeof value === "object" || typeof value === "string")) throw new Error(`Assert failed: Value is not an Error ${typeof value}`); // inlined mono_assert
            set_arg_type(arg, MarshalerType.JSException);
            const message = value.toString();
            _marshal_string_to_cs_impl(arg, message);
            const known_js_handle = value[cs_owned_js_handle_symbol];
            if (known_js_handle) {
                set_js_handle(arg, known_js_handle);
            }
            else {
                const js_handle = mono_wasm_get_js_handle(value);
                set_js_handle(arg, js_handle);
            }
        }
    }
    function _marshal_js_object_to_cs(arg, value) {
        if (value === undefined || value === null) {
            set_arg_type(arg, MarshalerType.None);
        }
        else {
            // if value was ManagedObject, it would be double proxied, but the C# signature requires that
            if (!(value[js_owned_gc_handle_symbol] === undefined)) throw new Error("Assert failed: JSObject proxy of ManagedObject proxy is not supported"); // inlined mono_assert
            if (!(typeof value === "function" || typeof value === "object")) throw new Error(`Assert failed: JSObject proxy of ${typeof value} is not supported`); // inlined mono_assert
            set_arg_type(arg, MarshalerType.JSObject);
            const js_handle = mono_wasm_get_js_handle(value);
            set_js_handle(arg, js_handle);
        }
    }
    function _marshal_cs_object_to_cs(arg, value) {
        if (value === undefined || value === null) {
            set_arg_type(arg, MarshalerType.None);
        }
        else {
            const gc_handle = value[js_owned_gc_handle_symbol];
            const js_type = typeof (value);
            if (gc_handle === undefined) {
                if (js_type === "string" || js_type === "symbol") {
                    set_arg_type(arg, MarshalerType.String);
                    _marshal_string_to_cs_impl(arg, value);
                }
                else if (js_type === "number") {
                    set_arg_type(arg, MarshalerType.Double);
                    set_arg_f64(arg, value);
                }
                else if (js_type === "bigint") {
                    // we do it because not all bigint values could fit into Int64
                    throw new Error("NotImplementedException: bigint");
                }
                else if (js_type === "boolean") {
                    set_arg_type(arg, MarshalerType.Boolean);
                    set_arg_b8(arg, value);
                }
                else if (value instanceof Date) {
                    set_arg_type(arg, MarshalerType.DateTime);
                    set_arg_date(arg, value);
                }
                else if (value instanceof Error) {
                    marshal_exception_to_cs(arg, value);
                }
                else if (value instanceof Uint8Array) {
                    marshal_array_to_cs_impl(arg, value, MarshalerType.Byte);
                }
                else if (value instanceof Float64Array) {
                    marshal_array_to_cs_impl(arg, value, MarshalerType.Double);
                }
                else if (value instanceof Int32Array) {
                    marshal_array_to_cs_impl(arg, value, MarshalerType.Int32);
                }
                else if (Array.isArray(value)) {
                    marshal_array_to_cs_impl(arg, value, MarshalerType.Object);
                }
                else if (value instanceof Int16Array
                    || value instanceof Int8Array
                    || value instanceof Uint8ClampedArray
                    || value instanceof Uint16Array
                    || value instanceof Uint32Array
                    || value instanceof Float32Array) {
                    throw new Error("NotImplementedException: TypedArray");
                }
                else if (isThenable(value)) {
                    _marshal_task_to_cs(arg, value);
                }
                else if (value instanceof Span) {
                    throw new Error("NotImplementedException: Span");
                }
                else if (js_type == "object") {
                    const js_handle = mono_wasm_get_js_handle(value);
                    set_arg_type(arg, MarshalerType.JSObject);
                    set_js_handle(arg, js_handle);
                }
                else {
                    throw new Error(`JSObject proxy is not supported for ${js_type} ${value}`);
                }
            }
            else {
                assert_not_disposed(value);
                if (value instanceof ArraySegment) {
                    throw new Error("NotImplementedException: ArraySegment");
                }
                else if (value instanceof ManagedError) {
                    set_arg_type(arg, MarshalerType.Exception);
                    set_gc_handle(arg, gc_handle);
                }
                else if (value instanceof ManagedObject) {
                    set_arg_type(arg, MarshalerType.Object);
                    set_gc_handle(arg, gc_handle);
                }
                else {
                    throw new Error("NotImplementedException " + js_type);
                }
            }
        }
    }
    function _marshal_array_to_cs(arg, value, sig) {
        if (!(!!sig)) throw new Error("Assert failed: Expected valid sig parameter"); // inlined mono_assert
        const element_type = get_signature_arg1_type(sig);
        marshal_array_to_cs_impl(arg, value, element_type);
    }
    function marshal_array_to_cs_impl(arg, value, element_type) {
        if (value === null || value === undefined) {
            set_arg_type(arg, MarshalerType.None);
        }
        else {
            const element_size = array_element_size(element_type);
            if (!(element_size != -1)) throw new Error(`Assert failed: Element type ${MarshalerType[element_type]} not supported`); // inlined mono_assert
            const length = value.length;
            const buffer_length = element_size * length;
            const buffer_ptr = Module._malloc(buffer_length);
            if (element_type == MarshalerType.String) {
                if (!(Array.isArray(value))) throw new Error("Assert failed: Value is not an Array"); // inlined mono_assert
                _zero_region(buffer_ptr, buffer_length);
                wrapped_c_functions.mono_wasm_register_root(buffer_ptr, buffer_length, "marshal_array_to_cs");
                for (let index = 0; index < length; index++) {
                    const element_arg = get_arg(buffer_ptr, index);
                    _marshal_string_to_cs(element_arg, value[index]);
                }
            }
            else if (element_type == MarshalerType.Object) {
                if (!(Array.isArray(value))) throw new Error("Assert failed: Value is not an Array"); // inlined mono_assert
                _zero_region(buffer_ptr, buffer_length);
                wrapped_c_functions.mono_wasm_register_root(buffer_ptr, buffer_length, "marshal_array_to_cs");
                for (let index = 0; index < length; index++) {
                    const element_arg = get_arg(buffer_ptr, index);
                    _marshal_cs_object_to_cs(element_arg, value[index]);
                }
            }
            else if (element_type == MarshalerType.JSObject) {
                if (!(Array.isArray(value))) throw new Error("Assert failed: Value is not an Array"); // inlined mono_assert
                _zero_region(buffer_ptr, buffer_length);
                for (let index = 0; index < length; index++) {
                    const element_arg = get_arg(buffer_ptr, index);
                    _marshal_js_object_to_cs(element_arg, value[index]);
                }
            }
            else if (element_type == MarshalerType.Byte) {
                if (!(Array.isArray(value) || value instanceof Uint8Array)) throw new Error("Assert failed: Value is not an Array or Uint8Array"); // inlined mono_assert
                const targetView = Module.HEAPU8.subarray(buffer_ptr, buffer_ptr + length);
                targetView.set(value);
            }
            else if (element_type == MarshalerType.Int32) {
                if (!(Array.isArray(value) || value instanceof Int32Array)) throw new Error("Assert failed: Value is not an Array or Int32Array"); // inlined mono_assert
                const targetView = Module.HEAP32.subarray(buffer_ptr >> 2, (buffer_ptr >> 2) + length);
                targetView.set(value);
            }
            else if (element_type == MarshalerType.Double) {
                if (!(Array.isArray(value) || value instanceof Float64Array)) throw new Error("Assert failed: Value is not an Array or Float64Array"); // inlined mono_assert
                const targetView = Module.HEAPF64.subarray(buffer_ptr >> 3, (buffer_ptr >> 3) + length);
                targetView.set(value);
            }
            else {
                throw new Error("not implemented");
            }
            set_arg_intptr(arg, buffer_ptr);
            set_arg_type(arg, MarshalerType.Array);
            set_arg_element_type(arg, element_type);
            set_arg_length(arg, value.length);
        }
    }
    function _marshal_span_to_cs(arg, value, sig) {
        if (!(!!sig)) throw new Error("Assert failed: Expected valid sig parameter"); // inlined mono_assert
        if (!(!value.isDisposed)) throw new Error("Assert failed: ObjectDisposedException"); // inlined mono_assert
        checkViewType(sig, value._viewType);
        set_arg_type(arg, MarshalerType.Span);
        set_arg_intptr(arg, value._pointer);
        set_arg_length(arg, value.length);
    }
    // this only supports round-trip
    function _marshal_array_segment_to_cs(arg, value, sig) {
        if (!(!!sig)) throw new Error("Assert failed: Expected valid sig parameter"); // inlined mono_assert
        const gc_handle = assert_not_disposed(value);
        if (!(gc_handle)) throw new Error("Assert failed: Only roundtrip of ArraySegment instance created by C#"); // inlined mono_assert
        checkViewType(sig, value._viewType);
        set_arg_type(arg, MarshalerType.ArraySegment);
        set_arg_intptr(arg, value._pointer);
        set_arg_length(arg, value.length);
        set_gc_handle(arg, gc_handle);
    }
    function checkViewType(sig, viewType) {
        const element_type = get_signature_arg1_type(sig);
        if (element_type == MarshalerType.Byte) {
            if (!(0 /* MemoryViewType.Byte */ == viewType)) throw new Error("Assert failed: Expected MemoryViewType.Byte"); // inlined mono_assert
        }
        else if (element_type == MarshalerType.Int32) {
            if (!(1 /* MemoryViewType.Int32 */ == viewType)) throw new Error("Assert failed: Expected MemoryViewType.Int32"); // inlined mono_assert
        }
        else if (element_type == MarshalerType.Double) {
            if (!(2 /* MemoryViewType.Double */ == viewType)) throw new Error("Assert failed: Expected MemoryViewType.Double"); // inlined mono_assert
        }
        else {
            throw new Error(`NotImplementedException ${MarshalerType[element_type]} `);
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    function initialize_marshalers_to_js() {
        if (cs_to_js_marshalers.size == 0) {
            cs_to_js_marshalers.set(MarshalerType.Array, _marshal_array_to_js);
            cs_to_js_marshalers.set(MarshalerType.Span, _marshal_span_to_js);
            cs_to_js_marshalers.set(MarshalerType.ArraySegment, _marshal_array_segment_to_js);
            cs_to_js_marshalers.set(MarshalerType.Boolean, _marshal_bool_to_js);
            cs_to_js_marshalers.set(MarshalerType.Byte, _marshal_byte_to_js);
            cs_to_js_marshalers.set(MarshalerType.Char, _marshal_char_to_js);
            cs_to_js_marshalers.set(MarshalerType.Int16, _marshal_int16_to_js);
            cs_to_js_marshalers.set(MarshalerType.Int32, marshal_int32_to_js);
            cs_to_js_marshalers.set(MarshalerType.Int52, _marshal_int52_to_js);
            cs_to_js_marshalers.set(MarshalerType.BigInt64, _marshal_bigint64_to_js);
            cs_to_js_marshalers.set(MarshalerType.Single, _marshal_float_to_js);
            cs_to_js_marshalers.set(MarshalerType.IntPtr, _marshal_intptr_to_js);
            cs_to_js_marshalers.set(MarshalerType.Double, _marshal_double_to_js);
            cs_to_js_marshalers.set(MarshalerType.String, marshal_string_to_js);
            cs_to_js_marshalers.set(MarshalerType.Exception, marshal_exception_to_js);
            cs_to_js_marshalers.set(MarshalerType.JSException, marshal_exception_to_js);
            cs_to_js_marshalers.set(MarshalerType.JSObject, _marshal_js_object_to_js);
            cs_to_js_marshalers.set(MarshalerType.Object, _marshal_cs_object_to_js);
            cs_to_js_marshalers.set(MarshalerType.DateTime, _marshal_datetime_to_js);
            cs_to_js_marshalers.set(MarshalerType.DateTimeOffset, _marshal_datetime_to_js);
            cs_to_js_marshalers.set(MarshalerType.Task, marshal_task_to_js);
            cs_to_js_marshalers.set(MarshalerType.Action, _marshal_delegate_to_js);
            cs_to_js_marshalers.set(MarshalerType.Function, _marshal_delegate_to_js);
            cs_to_js_marshalers.set(MarshalerType.None, _marshal_null_to_js);
            cs_to_js_marshalers.set(MarshalerType.Void, _marshal_null_to_js);
            cs_to_js_marshalers.set(MarshalerType.Discard, _marshal_null_to_js);
        }
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function generate_arg_marshal_to_js(sig, index, arg_offset, sig_offset, jsname, closure) {
        let converters = "";
        let converter_types = "";
        let call_body = "";
        const converter_name = "converter" + index;
        let converter_name_arg1 = "null";
        let converter_name_arg2 = "null";
        let converter_name_arg3 = "null";
        let converter_name_res = "null";
        let marshaler_type = get_signature_type(sig);
        if (marshaler_type === MarshalerType.None || marshaler_type === MarshalerType.Void) {
            return {
                converters,
                call_body,
                marshaler_type
            };
        }
        const marshaler_type_res = get_signature_res_type(sig);
        if (marshaler_type_res !== MarshalerType.None) {
            const converter = cs_to_js_marshalers.get(marshaler_type_res);
            if (!(converter && typeof converter === "function")) throw new Error(`Assert failed: Unknow converter for type ${marshaler_type_res} at ${index}`); // inlined mono_assert
            if (marshaler_type != MarshalerType.Nullable) {
                converter_name_res = "converter" + index + "_res";
                converters += ", " + converter_name_res;
                converter_types += " " + MarshalerType[marshaler_type_res];
                closure[converter_name_res] = converter;
            }
            else {
                marshaler_type = marshaler_type_res;
            }
        }
        const marshaler_type_arg1 = get_signature_arg1_type(sig);
        if (marshaler_type_arg1 !== MarshalerType.None) {
            const converter = js_to_cs_marshalers.get(marshaler_type_arg1);
            if (!(converter && typeof converter === "function")) throw new Error(`Assert failed: Unknow converter for type ${marshaler_type_arg1} at ${index}`); // inlined mono_assert
            converter_name_arg1 = "converter" + index + "_arg1";
            converters += ", " + converter_name_arg1;
            converter_types += " " + MarshalerType[marshaler_type_arg1];
            closure[converter_name_arg1] = converter;
        }
        const marshaler_type_arg2 = get_signature_arg2_type(sig);
        if (marshaler_type_arg2 !== MarshalerType.None) {
            const converter = js_to_cs_marshalers.get(marshaler_type_arg2);
            if (!(converter && typeof converter === "function")) throw new Error(`Assert failed: Unknow converter for type ${marshaler_type_arg2} at ${index}`); // inlined mono_assert
            converter_name_arg2 = "converter" + index + "_arg2";
            converters += ", " + converter_name_arg2;
            converter_types += " " + MarshalerType[marshaler_type_arg2];
            closure[converter_name_arg2] = converter;
        }
        const marshaler_type_arg3 = get_signature_arg3_type(sig);
        if (marshaler_type_arg3 !== MarshalerType.None) {
            const converter = js_to_cs_marshalers.get(marshaler_type_arg3);
            if (!(converter && typeof converter === "function")) throw new Error(`Assert failed: Unknow converter for type ${marshaler_type_arg3} at ${index}`); // inlined mono_assert
            converter_name_arg3 = "converter" + index + "_arg3";
            converters += ", " + converter_name_arg3;
            converter_types += " " + MarshalerType[marshaler_type_arg3];
            closure[converter_name_arg3] = converter;
        }
        const converter = cs_to_js_marshalers.get(marshaler_type);
        if (!(converter && typeof converter === "function")) throw new Error(`Assert failed: Unknow converter for type ${marshaler_type} at ${index} `); // inlined mono_assert
        converters += ", " + converter_name;
        converter_types += " " + MarshalerType[marshaler_type];
        closure[converter_name] = converter;
        if (marshaler_type == MarshalerType.Task) {
            call_body = `  const ${jsname} = ${converter_name}(args + ${arg_offset}, signature + ${sig_offset}, ${converter_name_res}); // ${converter_types} \n`;
        }
        else if (marshaler_type == MarshalerType.Action || marshaler_type == MarshalerType.Function) {
            call_body = `  const ${jsname} = ${converter_name}(args + ${arg_offset}, signature + ${sig_offset}, ${converter_name_res}, ${converter_name_arg1}, ${converter_name_arg2}, ${converter_name_arg3}); // ${converter_types} \n`;
        }
        else {
            call_body = `  const ${jsname} = ${converter_name}(args + ${arg_offset}, signature + ${sig_offset}); // ${converter_types} \n`;
        }
        return {
            converters,
            call_body,
            marshaler_type
        };
    }
    function _marshal_bool_to_js(arg) {
        const type = get_arg_type(arg);
        if (type == MarshalerType.None) {
            return null;
        }
        return get_arg_b8(arg);
    }
    function _marshal_byte_to_js(arg) {
        const type = get_arg_type(arg);
        if (type == MarshalerType.None) {
            return null;
        }
        return get_arg_u8(arg);
    }
    function _marshal_char_to_js(arg) {
        const type = get_arg_type(arg);
        if (type == MarshalerType.None) {
            return null;
        }
        return get_arg_u16(arg);
    }
    function _marshal_int16_to_js(arg) {
        const type = get_arg_type(arg);
        if (type == MarshalerType.None) {
            return null;
        }
        return get_arg_i16(arg);
    }
    function marshal_int32_to_js(arg) {
        const type = get_arg_type(arg);
        if (type == MarshalerType.None) {
            return null;
        }
        return get_arg_i32(arg);
    }
    function _marshal_int52_to_js(arg) {
        const type = get_arg_type(arg);
        if (type == MarshalerType.None) {
            return null;
        }
        return get_arg_i52(arg);
    }
    function _marshal_bigint64_to_js(arg) {
        const type = get_arg_type(arg);
        if (type == MarshalerType.None) {
            return null;
        }
        return get_arg_i64_big(arg);
    }
    function _marshal_float_to_js(arg) {
        const type = get_arg_type(arg);
        if (type == MarshalerType.None) {
            return null;
        }
        return get_arg_f32(arg);
    }
    function _marshal_double_to_js(arg) {
        const type = get_arg_type(arg);
        if (type == MarshalerType.None) {
            return null;
        }
        return get_arg_f64(arg);
    }
    function _marshal_intptr_to_js(arg) {
        const type = get_arg_type(arg);
        if (type == MarshalerType.None) {
            return null;
        }
        return get_arg_intptr(arg);
    }
    function _marshal_null_to_js() {
        return null;
    }
    function _marshal_datetime_to_js(arg) {
        const type = get_arg_type(arg);
        if (type === MarshalerType.None) {
            return null;
        }
        return get_arg_date(arg);
    }
    function _marshal_delegate_to_js(arg, _, res_converter, arg1_converter, arg2_converter, arg3_converter) {
        const type = get_arg_type(arg);
        if (type === MarshalerType.None) {
            return null;
        }
        const gc_handle = get_arg_gc_handle(arg);
        let result = _lookup_js_owned_object(gc_handle);
        if (result === null || result === undefined) {
            // this will create new Function for the C# delegate
            result = (arg1_js, arg2_js, arg3_js) => {
                // arg numbers are shifted by one, the real first is a gc handle of the callback
                return runtimeHelpers.javaScriptExports.call_delegate(gc_handle, arg1_js, arg2_js, arg3_js, res_converter, arg1_converter, arg2_converter, arg3_converter);
            };
            setup_managed_proxy(result, gc_handle);
        }
        return result;
    }
    function marshal_task_to_js(arg, _, res_converter) {
        const type = get_arg_type(arg);
        if (type === MarshalerType.None) {
            return null;
        }
        if (type !== MarshalerType.Task) {
            if (!res_converter) {
                // when we arrived here from _marshal_cs_object_to_js
                res_converter = cs_to_js_marshalers.get(type);
            }
            if (!(res_converter)) throw new Error(`Assert failed: Unknow sub_converter for type ${MarshalerType[type]} `); // inlined mono_assert
            // this is already resolved
            const val = res_converter(arg);
            return new Promise((resolve) => resolve(val));
        }
        const js_handle = get_arg_js_handle(arg);
        // console.log("_marshal_task_to_js A" + js_handle);
        if (js_handle == JSHandleNull) {
            // this is already resolved void
            return new Promise((resolve) => resolve(undefined));
        }
        const promise = mono_wasm_get_jsobj_from_js_handle(js_handle);
        if (!(!!promise)) throw new Error(`Assert failed: ERR28: promise not found for js_handle: ${js_handle} `); // inlined mono_assert
        assertIsControllablePromise(promise);
        const promise_control = getPromiseController(promise);
        const orig_resolve = promise_control.resolve;
        promise_control.resolve = (argInner) => {
            // console.log("_marshal_task_to_js R" + js_handle);
            const type = get_arg_type(argInner);
            if (type === MarshalerType.None) {
                orig_resolve(null);
                return;
            }
            if (!res_converter) {
                // when we arrived here from _marshal_cs_object_to_js
                res_converter = cs_to_js_marshalers.get(type);
            }
            if (!(res_converter)) throw new Error(`Assert failed: Unknow sub_converter for type ${MarshalerType[type]}`); // inlined mono_assert
            const js_value = res_converter(argInner);
            orig_resolve(js_value);
        };
        return promise;
    }
    function mono_wasm_marshal_promise(args) {
        const exc = get_arg(args, 0);
        const res = get_arg(args, 1);
        const arg_handle = get_arg(args, 2);
        const arg_value = get_arg(args, 3);
        const exc_type = get_arg_type(exc);
        const value_type = get_arg_type(arg_value);
        const js_handle = get_arg_js_handle(arg_handle);
        if (js_handle === JSHandleNull) {
            const { promise, promise_control } = createPromiseController();
            const js_handle = mono_wasm_get_js_handle(promise);
            set_js_handle(res, js_handle);
            if (exc_type !== MarshalerType.None) {
                // this is already failed task
                const reason = marshal_exception_to_js(exc);
                promise_control.reject(reason);
            }
            else if (value_type !== MarshalerType.Task) {
                // this is already resolved task
                const sub_converter = cs_to_js_marshalers.get(value_type);
                if (!(sub_converter)) throw new Error(`Assert failed: Unknow sub_converter for type ${MarshalerType[value_type]} `); // inlined mono_assert
                const data = sub_converter(arg_value);
                promise_control.resolve(data);
            }
        }
        else {
            // resolve existing promise
            const promise = mono_wasm_get_jsobj_from_js_handle(js_handle);
            if (!(!!promise)) throw new Error(`Assert failed: ERR25: promise not found for js_handle: ${js_handle} `); // inlined mono_assert
            assertIsControllablePromise(promise);
            const promise_control = getPromiseController(promise);
            if (exc_type !== MarshalerType.None) {
                const reason = marshal_exception_to_js(exc);
                promise_control.reject(reason);
            }
            else if (value_type !== MarshalerType.Task) {
                // here we assume that resolve was wrapped with sub_converter inside _marshal_task_to_js
                promise_control.resolve(arg_value);
            }
        }
        set_arg_type(res, MarshalerType.Task);
        set_arg_type(exc, MarshalerType.None);
    }
    function marshal_string_to_js(arg) {
        const type = get_arg_type(arg);
        if (type == MarshalerType.None) {
            return null;
        }
        const root = get_string_root(arg);
        try {
            const value = conv_string_root(root);
            return value;
        }
        finally {
            root.clear();
            root.release();
        }
    }
    function marshal_exception_to_js(arg) {
        const type = get_arg_type(arg);
        if (type == MarshalerType.None) {
            return null;
        }
        if (type == MarshalerType.JSException) {
            // this is JSException roundtrip
            const js_handle = get_arg_js_handle(arg);
            const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
            return js_obj;
        }
        const gc_handle = get_arg_gc_handle(arg);
        let result = _lookup_js_owned_object(gc_handle);
        if (result === null || result === undefined) {
            // this will create new ManagedError
            const message = marshal_string_to_js(arg);
            result = new ManagedError(message);
            setup_managed_proxy(result, gc_handle);
        }
        return result;
    }
    function _marshal_js_object_to_js(arg) {
        const type = get_arg_type(arg);
        if (type == MarshalerType.None) {
            return null;
        }
        const js_handle = get_arg_js_handle(arg);
        const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        return js_obj;
    }
    function _marshal_cs_object_to_js(arg) {
        const marshaler_type = get_arg_type(arg);
        if (marshaler_type == MarshalerType.None) {
            return null;
        }
        if (marshaler_type == MarshalerType.JSObject) {
            const js_handle = get_arg_js_handle(arg);
            const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
            return js_obj;
        }
        if (marshaler_type == MarshalerType.Array) {
            const element_type = get_arg_element_type(arg);
            return _marshal_array_to_js_impl(arg, element_type);
        }
        if (marshaler_type == MarshalerType.Object) {
            const gc_handle = get_arg_gc_handle(arg);
            if (gc_handle === GCHandleNull) {
                return null;
            }
            // see if we have js owned instance for this gc_handle already
            let result = _lookup_js_owned_object(gc_handle);
            // If the JS object for this gc_handle was already collected (or was never created)
            if (!result) {
                result = new ManagedObject();
                setup_managed_proxy(result, gc_handle);
            }
            return result;
        }
        // other types
        const converter = cs_to_js_marshalers.get(marshaler_type);
        if (!(converter)) throw new Error(`Assert failed: Unknow converter for type ${MarshalerType[marshaler_type]}`); // inlined mono_assert
        return converter(arg);
    }
    function _marshal_array_to_js(arg, sig) {
        if (!(!!sig)) throw new Error("Assert failed: Expected valid sig parameter"); // inlined mono_assert
        const element_type = get_signature_arg1_type(sig);
        return _marshal_array_to_js_impl(arg, element_type);
    }
    function _marshal_array_to_js_impl(arg, element_type) {
        const type = get_arg_type(arg);
        if (type == MarshalerType.None) {
            return null;
        }
        const elementSize = array_element_size(element_type);
        if (!(elementSize != -1)) throw new Error(`Assert failed: Element type ${MarshalerType[element_type]} not supported`); // inlined mono_assert
        const buffer_ptr = get_arg_intptr(arg);
        const length = get_arg_length(arg);
        let result = null;
        if (element_type == MarshalerType.String) {
            result = new Array(length);
            for (let index = 0; index < length; index++) {
                const element_arg = get_arg(buffer_ptr, index);
                result[index] = marshal_string_to_js(element_arg);
            }
            wrapped_c_functions.mono_wasm_deregister_root(buffer_ptr);
        }
        else if (element_type == MarshalerType.Object) {
            result = new Array(length);
            for (let index = 0; index < length; index++) {
                const element_arg = get_arg(buffer_ptr, index);
                result[index] = _marshal_cs_object_to_js(element_arg);
            }
            wrapped_c_functions.mono_wasm_deregister_root(buffer_ptr);
        }
        else if (element_type == MarshalerType.JSObject) {
            result = new Array(length);
            for (let index = 0; index < length; index++) {
                const element_arg = get_arg(buffer_ptr, index);
                result[index] = _marshal_js_object_to_js(element_arg);
            }
        }
        else if (element_type == MarshalerType.Byte) {
            const sourceView = Module.HEAPU8.subarray(buffer_ptr, buffer_ptr + length);
            result = sourceView.slice(); //copy
        }
        else if (element_type == MarshalerType.Int32) {
            const sourceView = Module.HEAP32.subarray(buffer_ptr >> 2, (buffer_ptr >> 2) + length);
            result = sourceView.slice(); //copy
        }
        else if (element_type == MarshalerType.Double) {
            const sourceView = Module.HEAPF64.subarray(buffer_ptr >> 3, (buffer_ptr >> 3) + length);
            result = sourceView.slice(); //copy
        }
        else {
            throw new Error(`NotImplementedException ${MarshalerType[element_type]} `);
        }
        Module._free(buffer_ptr);
        return result;
    }
    function _marshal_span_to_js(arg, sig) {
        if (!(!!sig)) throw new Error("Assert failed: Expected valid sig parameter"); // inlined mono_assert
        const element_type = get_signature_arg1_type(sig);
        const buffer_ptr = get_arg_intptr(arg);
        const length = get_arg_length(arg);
        let result = null;
        if (element_type == MarshalerType.Byte) {
            result = new Span(buffer_ptr, length, 0 /* MemoryViewType.Byte */);
        }
        else if (element_type == MarshalerType.Int32) {
            result = new Span(buffer_ptr, length, 1 /* MemoryViewType.Int32 */);
        }
        else if (element_type == MarshalerType.Double) {
            result = new Span(buffer_ptr, length, 2 /* MemoryViewType.Double */);
        }
        else {
            throw new Error(`NotImplementedException ${MarshalerType[element_type]} `);
        }
        return result;
    }
    function _marshal_array_segment_to_js(arg, sig) {
        if (!(!!sig)) throw new Error("Assert failed: Expected valid sig parameter"); // inlined mono_assert
        const element_type = get_signature_arg1_type(sig);
        const buffer_ptr = get_arg_intptr(arg);
        const length = get_arg_length(arg);
        let result = null;
        if (element_type == MarshalerType.Byte) {
            result = new ArraySegment(buffer_ptr, length, 0 /* MemoryViewType.Byte */);
        }
        else if (element_type == MarshalerType.Int32) {
            result = new ArraySegment(buffer_ptr, length, 1 /* MemoryViewType.Int32 */);
        }
        else if (element_type == MarshalerType.Double) {
            result = new ArraySegment(buffer_ptr, length, 2 /* MemoryViewType.Double */);
        }
        else {
            throw new Error(`NotImplementedException ${MarshalerType[element_type]} `);
        }
        const gc_handle = get_arg_gc_handle(arg);
        setup_managed_proxy(result, gc_handle);
        return result;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    let node_fs = undefined;
    let node_url = undefined;
    function init_polyfills(replacements) {
        const anyModule = Module;
        // performance.now() is used by emscripten and doesn't work in JSC
        if (typeof globalThis.performance === "undefined") {
            globalThis.performance = dummyPerformance;
        }
        if (typeof globalThis.URL === "undefined") {
            globalThis.URL = class URL {
                constructor(url) {
                    this.url = url;
                }
                toString() {
                    return this.url;
                }
            };
        }
        // v8 shell doesn't have Event and EventTarget
        if (MonoWasmThreads && typeof globalThis.Event === "undefined") {
            globalThis.Event = class Event {
                constructor(type) {
                    this.type = type;
                }
            };
        }
        if (MonoWasmThreads && typeof globalThis.EventTarget === "undefined") {
            globalThis.EventTarget = class EventTarget {
                constructor() {
                    this.subscribers = new Map();
                }
                addEventListener(type, listener, options) {
                    if (listener === undefined || listener == null)
                        return;
                    let oneShot = false;
                    if (options !== undefined) {
                        for (const [k, v] of Object.entries(options)) {
                            if (k === "once") {
                                oneShot = v ? true : false;
                                continue;
                            }
                            throw new Error(`FIXME: addEventListener polyfill doesn't implement option '${k}'`);
                        }
                    }
                    if (!this.subscribers.has(type)) {
                        this.subscribers.set(type, []);
                    }
                    const listeners = this.subscribers.get(type);
                    if (listeners === undefined) {
                        throw new Error("can't happen");
                    }
                    listeners.push({ listener, oneShot });
                }
                removeEventListener(type, listener, options) {
                    if (listener === undefined || listener == null)
                        return;
                    if (options !== undefined) {
                        throw new Error("FIXME: removeEventListener polyfill doesn't implement options");
                    }
                    if (!this.subscribers.has(type)) {
                        return;
                    }
                    const subscribers = this.subscribers.get(type);
                    if (subscribers === undefined)
                        return;
                    let index = -1;
                    const n = subscribers.length;
                    for (let i = 0; i < n; ++i) {
                        if (subscribers[i].listener === listener) {
                            index = i;
                            break;
                        }
                    }
                    if (index > -1) {
                        subscribers.splice(index, 1);
                    }
                }
                dispatchEvent(event) {
                    if (!this.subscribers.has(event.type)) {
                        return true;
                    }
                    let subscribers = this.subscribers.get(event.type);
                    if (subscribers === undefined) {
                        return true;
                    }
                    let needsCopy = false;
                    for (const sub of subscribers) {
                        if (sub.oneShot) {
                            needsCopy = true;
                            break;
                        }
                    }
                    if (needsCopy) {
                        subscribers = subscribers.slice(0);
                    }
                    for (const sub of subscribers) {
                        const listener = sub.listener;
                        if (sub.oneShot) {
                            this.removeEventListener(event.type, listener);
                        }
                        if (typeof listener === "function") {
                            listener.call(this, event);
                        }
                        else {
                            listener.handleEvent(event);
                        }
                    }
                    return true;
                }
            };
        }
        // require replacement
        const imports = anyModule.imports = (Module.imports || {});
        const requireWrapper = (wrappedRequire) => (name) => {
            const resolved = Module.imports[name];
            if (resolved) {
                return resolved;
            }
            return wrappedRequire(name);
        };
        if (imports.require) {
            runtimeHelpers.requirePromise = replacements.requirePromise = Promise.resolve(requireWrapper(imports.require));
        }
        else if (replacements.require) {
            runtimeHelpers.requirePromise = replacements.requirePromise = Promise.resolve(requireWrapper(replacements.require));
        }
        else if (replacements.requirePromise) {
            runtimeHelpers.requirePromise = replacements.requirePromise.then(require => requireWrapper(require));
        }
        else {
            runtimeHelpers.requirePromise = replacements.requirePromise = Promise.resolve(requireWrapper((name) => {
                throw new Error(`Please provide Module.imports.${name} or Module.imports.require`);
            }));
        }
        // script location
        runtimeHelpers.scriptDirectory = replacements.scriptDirectory = detectScriptDirectory(replacements);
        anyModule.mainScriptUrlOrBlob = replacements.scriptUrl; // this is needed by worker threads
        if (BuildConfiguration === "Debug") {
            console.debug(`MONO_WASM: starting script ${replacements.scriptUrl}`);
            console.debug(`MONO_WASM: starting in ${runtimeHelpers.scriptDirectory}`);
        }
        if (anyModule.__locateFile === anyModule.locateFile) {
            // above it's our early version from dotnet.es6.pre.js, we could replace it with better
            anyModule.locateFile = runtimeHelpers.locateFile = (path) => {
                if (isPathAbsolute(path))
                    return path;
                return runtimeHelpers.scriptDirectory + path;
            };
        }
        else {
            // we use what was given to us
            runtimeHelpers.locateFile = anyModule.locateFile;
        }
        // fetch poly
        if (imports.fetch) {
            replacements.fetch = runtimeHelpers.fetch_like = imports.fetch;
        }
        else {
            replacements.fetch = runtimeHelpers.fetch_like = fetch_like;
        }
        // misc
        replacements.noExitRuntime = ENVIRONMENT_IS_WEB;
        // threads
        if (MonoWasmThreads) {
            if (replacements.pthreadReplacements) {
                //replaceEmscriptenPThreadLibrary(replacements.pthreadReplacements);
            }
        }
        // memory
        const originalUpdateMemoryViews = replacements.updateMemoryViews;
        replacements.updateMemoryViews = () => {
            originalUpdateMemoryViews();
            afterUpdateGlobalBufferAndViews(Module['asm']['memory'].buffer);
        };
    }
    async function init_polyfills_async() {
        if (ENVIRONMENT_IS_NODE) {
            // wait for locateFile setup on NodeJs
            INTERNAL.require = await runtimeHelpers.requirePromise;
            if (globalThis.performance === dummyPerformance) {
                const { performance } = INTERNAL.require("perf_hooks");
                globalThis.performance = performance;
            }
            if (!globalThis.crypto) {
                globalThis.crypto = {};
            }
            if (!globalThis.crypto.getRandomValues) {
                let nodeCrypto = undefined;
                try {
                    nodeCrypto = INTERNAL.require("node:crypto");
                }
                catch (err) {
                    // Noop, error throwing polyfill provided bellow
                }
                if (!nodeCrypto) {
                    globalThis.crypto.getRandomValues = () => {
                        throw new Error("Using node without crypto support. To enable current operation, either provide polyfill for 'globalThis.crypto.getRandomValues' or enable 'node:crypto' module.");
                    };
                }
                else if (nodeCrypto.webcrypto) {
                    globalThis.crypto = nodeCrypto.webcrypto;
                }
                else if (nodeCrypto.randomBytes) {
                    globalThis.crypto.getRandomValues = (buffer) => {
                        if (buffer) {
                            buffer.set(nodeCrypto.randomBytes(buffer.length));
                        }
                    };
                }
            }
        }
    }
    const dummyPerformance = {
        now: function () {
            return Date.now();
        }
    };
    async function fetch_like(url, init) {
        try {
            if (ENVIRONMENT_IS_NODE) {
                if (!node_fs) {
                    const node_require = await runtimeHelpers.requirePromise;
                    node_url = node_require("url");
                    node_fs = node_require("fs");
                }
                if (url.startsWith("file://")) {
                    url = node_url.fileURLToPath(url);
                }
                const arrayBuffer = await node_fs.promises.readFile(url);
                return {
                    ok: true,
                    url,
                    arrayBuffer: () => arrayBuffer,
                    json: () => JSON.parse(arrayBuffer)
                };
            }
            else if (typeof (globalThis.fetch) === "function") {
                return globalThis.fetch(url, init || { credentials: "same-origin" });
            }
            else if (typeof (read) === "function") {
                // note that it can't open files with unicode names, like Stra<unicode char - Latin Small Letter Sharp S>e.xml
                // https://bugs.chromium.org/p/v8/issues/detail?id=12541
                const arrayBuffer = new Uint8Array(read(url, "binary"));
                return {
                    ok: true,
                    url,
                    arrayBuffer: () => arrayBuffer,
                    json: () => JSON.parse(Module.UTF8ArrayToString(arrayBuffer, 0, arrayBuffer.length))
                };
            }
        }
        catch (e) {
            return {
                ok: false,
                url,
                status: 500,
                statusText: "ERR28: " + e,
                arrayBuffer: () => { throw e; },
                json: () => { throw e; }
            };
        }
        throw new Error("No fetch implementation available");
    }
    function normalizeFileUrl(filename) {
        // unix vs windows
        // remove query string
        return filename.replace(/\\/g, "/").replace(/[?#].*/, "");
    }
    function normalizeDirectoryUrl(dir) {
        return dir.slice(0, dir.lastIndexOf("/")) + "/";
    }
    function detectScriptDirectory(replacements) {
        if (ENVIRONMENT_IS_WORKER) {
            // Check worker, not web, since window could be polyfilled
            replacements.scriptUrl = self.location.href;
        }
        if (!replacements.scriptUrl) {
            // probably V8 shell in non ES6
            replacements.scriptUrl = "./dotnet.js";
        }
        replacements.scriptUrl = normalizeFileUrl(replacements.scriptUrl);
        let dir = replacements.scriptUrl;
        if (!ENVIRONMENT_IS_WORKER && ENVIRONMENT_IS_NODE) {
            dir = replacements.require('url').fileURLToPath(dir);
        }
        return normalizeDirectoryUrl(dir);
    }
    const protocolRx = /^[a-zA-Z][a-zA-Z\d+\-.]*?:\/\//;
    const windowsAbsoluteRx = /[a-zA-Z]:[\\/]/;
    function isPathAbsolute(path) {
        if (ENVIRONMENT_IS_NODE || ENVIRONMENT_IS_SHELL) {
            // unix /x.json
            // windows \x.json
            // windows C:\x.json
            // windows C:/x.json
            return path.startsWith("/") || path.startsWith("\\") || path.indexOf("///") !== -1 || windowsAbsoluteRx.test(path);
        }
        // anything with protocol is always absolute
        // windows file:///C:/x.json
        // windows http://C:/x.json
        return protocolRx.test(path);
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const MainThread = {
        get pthread_id() {
            return getBrowserThreadID();
        },
        isBrowserThread: true
    };
    let browser_thread_id_lazy;
    function getBrowserThreadID() {
        if (browser_thread_id_lazy === undefined) {
            browser_thread_id_lazy = Module["_emscripten_main_browser_thread_id"]();
        }
        return browser_thread_id_lazy;
    }
    function isMonoThreadMessage(x) {
        if (typeof (x) !== "object" || x === null) {
            return false;
        }
        const xmsg = x;
        return typeof (xmsg.type) === "string" && typeof (xmsg.cmd) === "string";
    }
    function isMonoThreadMessageApplyMonoConfig(x) {
        if (!isMonoThreadMessage(x)) {
            return false;
        }
        const xmsg = x;
        return xmsg.type === "pthread" && xmsg.cmd === "apply_mono_config" && typeof (xmsg.config) === "string";
    }
    function makeMonoThreadMessageApplyMonoConfig(config) {
        return {
            type: "pthread",
            cmd: "apply_mono_config",
            config: JSON.stringify(config)
        };
    }
    /// Messages sent using the worker object's postMessage() method ///
    /// a symbol that we use as a key on messages on the global worker-to-main channel to identify our own messages
    /// we can't use an actual JS Symbol because those don't transfer between workers.
    const monoSymbol = "__mono_message_please_dont_collide__"; //Symbol("mono");
    function makeChannelCreatedMonoMessage(thread_id, port) {
        return {
            [monoSymbol]: {
                mono_cmd: "channel_created",
                thread_id,
                port
            }
        };
    }
    function isMonoWorkerMessage(message) {
        return message !== undefined && typeof message === "object" && message !== null && monoSymbol in message;
    }
    function isMonoWorkerMessageChannelCreated(message) {
        if (isMonoWorkerMessage(message)) {
            const monoMessage = message[monoSymbol];
            if (monoMessage.mono_cmd === "channel_created") {
                return true;
            }
        }
        return false;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const dotnetPthreadCreated = "dotnet:pthread:created";
    const dotnetPthreadAttached = "dotnet:pthread:attached";
    let WorkerThreadEventClassConstructor;
    const makeWorkerThreadEvent = !MonoWasmThreads
        ? (() => { throw new Error("threads support disabled"); })
        : ((type, pthread_self) => {
            if (!WorkerThreadEventClassConstructor)
                WorkerThreadEventClassConstructor = class WorkerThreadEventImpl extends Event {
                    constructor(type, pthread_self) {
                        super(type);
                        this.pthread_self = pthread_self;
                    }
                };
            return new WorkerThreadEventClassConstructor(type, pthread_self);
        });

    // Licensed to the .NET Foundation under one or more agreements.
    class WorkerSelf {
        constructor(pthread_id, portToBrowser) {
            this.pthread_id = pthread_id;
            this.portToBrowser = portToBrowser;
            this.isBrowserThread = false;
        }
        postMessageToBrowser(message, transfer) {
            if (transfer) {
                this.portToBrowser.postMessage(message, transfer);
            }
            else {
                this.portToBrowser.postMessage(message);
            }
        }
        addEventListenerFromBrowser(listener) {
            this.portToBrowser.addEventListener("message", listener);
        }
    }
    // we are lying that this is never null, but afterThreadInit should be the first time we get to run any code
    // in the worker, so this becomes non-null very early.
    let pthread_self = null;
    /// This is the "public internal" API for runtime subsystems that wish to be notified about
    /// pthreads that are running on the current worker.
    /// Example:
    ///    currentWorkerThreadEvents.addEventListener(dotnetPthreadCreated, (ev: WorkerThreadEvent) => {
    ///       console.debug("MONO_WASM: thread created on worker with id", ev.pthread_ptr);
    ///    });
    const currentWorkerThreadEvents = MonoWasmThreads ? new EventTarget() : null; // treeshake if threads are disabled
    // this is the message handler for the worker that receives messages from the main thread
    // extend this with new cases as needed
    function monoDedicatedChannelMessageFromMainToWorker(event) {
        if (isMonoThreadMessageApplyMonoConfig(event.data)) {
            const config = JSON.parse(event.data.config);
            console.debug("MONO_WASM: applying mono config from main", event.data.config);
            onMonoConfigReceived(config);
            return;
        }
        console.debug("MONO_WASM: got message from main on the dedicated channel", event.data);
    }
    function setupChannelToMainThread(pthread_ptr) {
        console.debug("MONO_WASM: creating a channel", pthread_ptr);
        const channel = new MessageChannel();
        const workerPort = channel.port1;
        const mainPort = channel.port2;
        workerPort.addEventListener("message", monoDedicatedChannelMessageFromMainToWorker);
        workerPort.start();
        pthread_self = new WorkerSelf(pthread_ptr, workerPort);
        self.postMessage(makeChannelCreatedMonoMessage(pthread_ptr, mainPort), [mainPort]);
        return pthread_self;
    }
    // TODO: should we just assign to Module.config here?
    let workerMonoConfig = null;
    // called when the main thread sends us the mono config
    function onMonoConfigReceived(config) {
        if (workerMonoConfig !== null) {
            console.debug("MONO_WASM: mono config already received");
            return;
        }
        workerMonoConfig = config;
        console.debug("MONO_WASM: mono config received", config);
        if (workerMonoConfig.diagnosticTracing) {
            setup_proxy_console("pthread-worker", console, self.location.href);
        }
    }
    /// This is an implementation detail function.
    /// Called in the worker thread from mono when a pthread becomes attached to the mono runtime.
    function mono_wasm_pthread_on_pthread_attached(pthread_id) {
        const self = pthread_self;
        if (!(self !== null && self.pthread_id == pthread_id)) throw new Error("Assert failed: expected pthread_self to be set already when attaching"); // inlined mono_assert
        console.debug("MONO_WASM: attaching pthread to runtime", pthread_id);
        currentWorkerThreadEvents.dispatchEvent(makeWorkerThreadEvent(dotnetPthreadAttached, self));
    }
    /// This is an implementation detail function.
    /// Called by emscripten when a pthread is setup to run on a worker.  Can be called multiple times
    /// for the same worker, since emscripten can reuse workers.  This is an implementation detail, that shouldn't be used directly.
    function afterThreadInitTLS() {
        // don't do this callback for the main thread
        if (ENVIRONMENT_IS_PTHREAD) {
            const pthread_ptr = Module["_pthread_self"]();
            if (!(!is_nullish(pthread_ptr))) throw new Error("Assert failed: pthread_self() returned null"); // inlined mono_assert
            console.debug("MONO_WASM: after thread init, pthread ptr", pthread_ptr);
            const self = setupChannelToMainThread(pthread_ptr);
            currentWorkerThreadEvents.dispatchEvent(makeWorkerThreadEvent(dotnetPthreadCreated, self));
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    function mono_wasm_bind_js_function(function_name, module_name, signature, function_js_handle, is_exception, result_address) {
        const function_name_root = mono_wasm_new_external_root(function_name), module_name_root = mono_wasm_new_external_root(module_name), resultRoot = mono_wasm_new_external_root(result_address);
        try {
            const version = get_signature_version(signature);
            if (!(version === 1)) throw new Error(`Assert failed: Signature version ${version} mismatch.`); // inlined mono_assert
            const js_function_name = conv_string_root(function_name_root);
            const js_module_name = conv_string_root(module_name_root);
            if (runtimeHelpers.diagnosticTracing) {
                console.debug(`MONO_WASM: Binding [JSImport] ${js_function_name} from ${js_module_name}`);
            }
            const fn = mono_wasm_lookup_function(js_function_name, js_module_name);
            const args_count = get_signature_argument_count(signature);
            const closure = { fn, marshal_exception_to_cs, signature };
            const bound_js_function_name = "_bound_js_" + js_function_name.replace(/\./g, "_");
            let body = `//# sourceURL=https://dotnet.generated.invalid/${bound_js_function_name} \n`;
            let converter_names = "";
            let bodyToJs = "";
            let pass_args = "";
            for (let index = 0; index < args_count; index++) {
                const arg_offset = (index + 2) * JavaScriptMarshalerArgSize;
                const sig_offset = (index + 2) * JSMarshalerTypeSize + JSMarshalerSignatureHeaderSize;
                const arg_name = `arg${index}`;
                const sig = get_sig(signature, index + 2);
                const { converters, call_body } = generate_arg_marshal_to_js(sig, index + 2, arg_offset, sig_offset, arg_name, closure);
                converter_names += converters;
                bodyToJs += call_body;
                if (pass_args === "") {
                    pass_args += arg_name;
                }
                else {
                    pass_args += `, ${arg_name}`;
                }
            }
            const { converters: res_converters, call_body: res_call_body, marshaler_type: res_marshaler_type } = generate_arg_marshal_to_cs(get_sig(signature, 1), 1, JavaScriptMarshalerArgSize, JSMarshalerTypeSize + JSMarshalerSignatureHeaderSize, "js_result", closure);
            converter_names += res_converters;
            body += `const { signature, fn, marshal_exception_to_cs ${converter_names} } = closure;\n`;
            body += `return function ${bound_js_function_name} (args) { try {\n`;
            // body += `console.log("${bound_js_function_name}")\n`;
            body += bodyToJs;
            if (res_marshaler_type === MarshalerType.Void) {
                body += `  const js_result = fn(${pass_args});\n`;
                body += `  if (js_result !== undefined) throw new Error('Function ${js_function_name} returned unexpected value, C# signature is void');\n`;
            }
            else if (res_marshaler_type === MarshalerType.Discard) {
                body += `  fn(${pass_args});\n`;
            }
            else {
                body += `  const js_result = fn(${pass_args});\n`;
                body += res_call_body;
            }
            for (let index = 0; index < args_count; index++) {
                const sig = get_sig(signature, index + 2);
                const marshaler_type = get_signature_type(sig);
                if (marshaler_type == MarshalerType.Span) {
                    const arg_name = `arg${index}`;
                    body += `  ${arg_name}.dispose();\n`;
                }
            }
            body += "} catch (ex) {\n";
            body += "  marshal_exception_to_cs(args, ex);\n";
            body += "}}";
            const factory = new Function("closure", body);
            const bound_fn = factory(closure);
            bound_fn[bound_js_function_symbol] = true;
            const bound_function_handle = mono_wasm_get_js_handle(bound_fn);
            setI32(function_js_handle, bound_function_handle);
        }
        catch (ex) {
            wrap_error_root(is_exception, ex, resultRoot);
        }
        finally {
            resultRoot.release();
            function_name_root.release();
        }
    }
    function mono_wasm_invoke_bound_function(bound_function_js_handle, args) {
        const bound_fn = mono_wasm_get_jsobj_from_js_handle(bound_function_js_handle);
        if (!(bound_fn && typeof (bound_fn) === "function" && bound_fn[bound_js_function_symbol])) throw new Error(`Assert failed: Bound function handle expected ${bound_function_js_handle}`); // inlined mono_assert
        bound_fn(args);
    }
    function mono_wasm_set_module_imports(module_name, moduleImports) {
        importedModules.set(module_name, moduleImports);
        if (runtimeHelpers.diagnosticTracing)
            console.debug(`MONO_WASM: added module imports '${module_name}'`);
    }
    function mono_wasm_lookup_function(function_name, js_module_name) {
        if (!(function_name && typeof function_name === "string")) throw new Error("Assert failed: function_name must be string"); // inlined mono_assert
        let scope = IMPORTS;
        const parts = function_name.split(".");
        if (js_module_name) {
            scope = importedModules.get(js_module_name);
            if (!(scope)) throw new Error(`Assert failed: ES6 module ${js_module_name} was not imported yet, please call JSHost.Import() first.`); // inlined mono_assert
        }
        else if (parts[0] === "INTERNAL") {
            scope = INTERNAL;
            parts.shift();
        }
        else if (parts[0] === "globalThis") {
            scope = globalThis;
            parts.shift();
        }
        for (let i = 0; i < parts.length - 1; i++) {
            const part = parts[i];
            const newscope = scope[part];
            if (!(newscope)) throw new Error(`Assert failed: ${part} not found while looking up ${function_name}`); // inlined mono_assert
            scope = newscope;
        }
        const fname = parts[parts.length - 1];
        const fn = scope[fname];
        if (!(typeof (fn) === "function")) throw new Error(`Assert failed: ${function_name} must be a Function but was ${typeof fn}`); // inlined mono_assert
        // if the function was already bound to some object it would stay bound to original object. That's good.
        return fn.bind(scope);
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function set_property(self, name, value) {
        if (!(self)) throw new Error("Assert failed: Null reference"); // inlined mono_assert
        self[name] = value;
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function get_property(self, name) {
        if (!(self)) throw new Error("Assert failed: Null reference"); // inlined mono_assert
        return self[name];
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function has_property(self, name) {
        if (!(self)) throw new Error("Assert failed: Null reference"); // inlined mono_assert
        return name in self;
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function get_typeof_property(self, name) {
        if (!(self)) throw new Error("Assert failed: Null reference"); // inlined mono_assert
        return typeof self[name];
    }
    function get_global_this() {
        return globalThis;
    }
    const importedModulesPromises = new Map();
    const importedModules = new Map();
    function dynamic_import(module_name, module_url) {
        if (!(module_name)) throw new Error("Assert failed: Invalid module_name"); // inlined mono_assert
        if (!(module_url)) throw new Error("Assert failed: Invalid module_name"); // inlined mono_assert
        let promise = importedModulesPromises.get(module_name);
        const newPromise = !promise;
        if (newPromise) {
            if (runtimeHelpers.diagnosticTracing)
                console.debug(`MONO_WASM: importing ES6 module '${module_name}' from '${module_url}'`);
            promise = import(/* webpackIgnore: true */ module_url);
            importedModulesPromises.set(module_name, promise);
        }
        return wrap_as_cancelable_promise(async () => {
            const module = await promise;
            if (newPromise) {
                importedModules.set(module_name, module);
                if (runtimeHelpers.diagnosticTracing)
                    console.debug(`MONO_WASM: imported ES6 module '${module_name}' from '${module_url}'`);
            }
            return module;
        });
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function _wrap_error_flag(is_exception, ex) {
        let res = "unknown exception";
        if (ex) {
            res = ex.toString();
            const stack = ex.stack;
            if (stack) {
                // Some JS runtimes insert the error message at the top of the stack, some don't,
                //  so normalize it by using the stack as the result if it already contains the error
                if (stack.startsWith(res))
                    res = stack;
                else
                    res += "\n" + stack;
            }
            res = mono_wasm_symbolicate_string(res);
        }
        if (is_exception) {
            Module.setValue(is_exception, 1, "i32");
        }
        return res;
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function wrap_error_root(is_exception, ex, result) {
        const res = _wrap_error_flag(is_exception, ex);
        js_string_to_mono_string_root(res, result);
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const exportedMethods = new Map();
    function mono_wasm_bind_cs_function(fully_qualified_name, signature_hash, signature, is_exception, result_address) {
        const fqn_root = mono_wasm_new_external_root(fully_qualified_name), resultRoot = mono_wasm_new_external_root(result_address);
        const anyModule = Module;
        try {
            const version = get_signature_version(signature);
            if (!(version === 1)) throw new Error(`Assert failed: Signature version ${version} mismatch.`); // inlined mono_assert
            const args_count = get_signature_argument_count(signature);
            const js_fqn = conv_string_root(fqn_root);
            if (!(js_fqn)) throw new Error("Assert failed: fully_qualified_name must be string"); // inlined mono_assert
            if (runtimeHelpers.diagnosticTracing) {
                console.debug(`MONO_WASM: Binding [JSExport] ${js_fqn}`);
            }
            const { assembly, namespace, classname, methodname } = parseFQN(js_fqn);
            const asm = assembly_load(assembly);
            if (!asm)
                throw new Error("Could not find assembly: " + assembly);
            const klass = wrapped_c_functions.mono_wasm_assembly_find_class(asm, namespace, classname);
            if (!klass)
                throw new Error("Could not find class: " + namespace + ":" + classname + " in assembly " + assembly);
            const wrapper_name = `__Wrapper_${methodname}_${signature_hash}`;
            const method = wrapped_c_functions.mono_wasm_assembly_find_method(klass, wrapper_name, -1);
            if (!method)
                throw new Error(`Could not find method: ${wrapper_name} in ${klass} [${assembly}]`);
            const closure = {
                method, signature,
                stackSave: anyModule.stackSave, stackRestore: anyModule.stackRestore,
                alloc_stack_frame,
                invoke_method_and_handle_exception
            };
            const bound_js_function_name = "_bound_cs_" + `${namespace}_${classname}_${methodname}`.replace(/\./g, "_").replace(/\//g, "_");
            let body = `//# sourceURL=https://dotnet.generated.invalid/${bound_js_function_name} \n`;
            let bodyToCs = "";
            let converter_names = "";
            for (let index = 0; index < args_count; index++) {
                const arg_offset = (index + 2) * JavaScriptMarshalerArgSize;
                const sig_offset = (index + 2) * JSMarshalerTypeSize + JSMarshalerSignatureHeaderSize;
                const sig = get_sig(signature, index + 2);
                const { converters, call_body } = generate_arg_marshal_to_cs(sig, index + 2, arg_offset, sig_offset, `arguments[${index}]`, closure);
                converter_names += converters;
                bodyToCs += call_body;
            }
            const { converters: res_converters, call_body: res_call_body, marshaler_type: res_marshaler_type } = generate_arg_marshal_to_js(get_sig(signature, 1), 1, JavaScriptMarshalerArgSize, JSMarshalerTypeSize + JSMarshalerSignatureHeaderSize, "js_result", closure);
            converter_names += res_converters;
            body += `const { method, signature, stackSave, stackRestore,  alloc_stack_frame, invoke_method_and_handle_exception ${converter_names} } = closure;\n`;
            // TODO named arguments instead of arguments keyword
            body += `return function ${bound_js_function_name} () {\n`;
            body += "const sp = stackSave();\n";
            body += "try {\n";
            body += `  const args = alloc_stack_frame(${(args_count + 2)});\n`;
            body += bodyToCs;
            body += "  invoke_method_and_handle_exception(method, args);\n";
            if (res_marshaler_type !== MarshalerType.Void && res_marshaler_type !== MarshalerType.Discard) {
                body += res_call_body;
            }
            if (res_marshaler_type !== MarshalerType.Void && res_marshaler_type !== MarshalerType.Discard) {
                body += "  return js_result;\n";
            }
            body += "} finally {\n";
            body += "  stackRestore(sp);\n";
            body += "}}";
            const factory = new Function("closure", body);
            const bound_fn = factory(closure);
            bound_fn[bound_cs_function_symbol] = true;
            exportedMethods.set(js_fqn, bound_fn);
            _walk_exports_to_set_function(assembly, namespace, classname, methodname, signature_hash, bound_fn);
        }
        catch (ex) {
            Module.printErr(ex.toString());
            wrap_error_root(is_exception, ex, resultRoot);
        }
        finally {
            resultRoot.release();
            fqn_root.release();
        }
    }
    function invoke_method_and_handle_exception(method, args) {
        const fail = wrapped_c_functions.mono_wasm_invoke_method_bound(method, args);
        if (fail)
            throw new Error("ERR24: Unexpected error: " + conv_string(fail));
        if (is_args_exception(args)) {
            const exc = get_arg(args, 0);
            throw marshal_exception_to_js(exc);
        }
    }
    const exportsByAssembly = new Map();
    function _walk_exports_to_set_function(assembly, namespace, classname, methodname, signature_hash, fn) {
        const parts = `${namespace}.${classname}`.replace(/\//g, ".").split(".");
        let scope = undefined;
        let assemblyScope = exportsByAssembly.get(assembly);
        if (!assemblyScope) {
            assemblyScope = {};
            exportsByAssembly.set(assembly, assemblyScope);
            exportsByAssembly.set(assembly + ".dll", assemblyScope);
        }
        scope = assemblyScope;
        for (let i = 0; i < parts.length; i++) {
            const part = parts[i];
            if (part != "") {
                let newscope = scope[part];
                if (typeof newscope === "undefined") {
                    newscope = {};
                    scope[part] = newscope;
                }
                if (!(newscope)) throw new Error(`Assert failed: ${part} not found while looking up ${classname}`); // inlined mono_assert
                scope = newscope;
            }
        }
        if (!scope[methodname]) {
            scope[methodname] = fn;
        }
        scope[`${methodname}.${signature_hash}`] = fn;
    }
    async function mono_wasm_get_assembly_exports(assembly) {
        if (!(runtimeHelpers.mono_wasm_bindings_is_ready)) throw new Error("Assert failed: The runtime must be initialized."); // inlined mono_assert
        const result = exportsByAssembly.get(assembly);
        if (!result) {
            const asm = assembly_load(assembly);
            if (!asm)
                throw new Error("Could not find assembly: " + assembly);
            wrapped_c_functions.mono_wasm_runtime_run_module_cctor(asm);
        }
        return exportsByAssembly.get(assembly) || {};
    }
    function parseFQN(fqn) {
        const assembly = fqn.substring(fqn.indexOf("[") + 1, fqn.indexOf("]")).trim();
        fqn = fqn.substring(fqn.indexOf("]") + 1).trim();
        const methodname = fqn.substring(fqn.indexOf(":") + 1);
        fqn = fqn.substring(0, fqn.indexOf(":")).trim();
        let namespace = "";
        let classname = fqn;
        if (fqn.indexOf(".") != -1) {
            const idx = fqn.lastIndexOf(".");
            namespace = fqn.substring(0, idx);
            classname = fqn.substring(idx + 1);
        }
        if (!assembly.trim())
            throw new Error("No assembly name specified " + fqn);
        if (!classname.trim())
            throw new Error("No class name specified " + fqn);
        if (!methodname.trim())
            throw new Error("No method name specified " + fqn);
        return { assembly, namespace, classname, methodname };
    }

    // Licensed to the .NET Foundation under one or more agreements.
    function init_managed_exports() {
        const anyModule = Module;
        const exports_fqn_asm = "System.Runtime.InteropServices.JavaScript";
        runtimeHelpers.runtime_interop_module = wrapped_c_functions.mono_wasm_assembly_load(exports_fqn_asm);
        if (!runtimeHelpers.runtime_interop_module)
            throw "Can't find bindings module assembly: " + exports_fqn_asm;
        runtimeHelpers.runtime_interop_namespace = "System.Runtime.InteropServices.JavaScript";
        runtimeHelpers.runtime_interop_exports_classname = "JavaScriptExports";
        runtimeHelpers.runtime_interop_exports_class = wrapped_c_functions.mono_wasm_assembly_find_class(runtimeHelpers.runtime_interop_module, runtimeHelpers.runtime_interop_namespace, runtimeHelpers.runtime_interop_exports_classname);
        if (!runtimeHelpers.runtime_interop_exports_class)
            throw "Can't find " + runtimeHelpers.runtime_interop_namespace + "." + runtimeHelpers.runtime_interop_exports_classname + " class";
        const install_sync_context = wrapped_c_functions.mono_wasm_assembly_find_method(runtimeHelpers.runtime_interop_exports_class, "InstallSynchronizationContext", -1);
        // if (!(install_sync_context)) throw new Error("Assert failed: Can't find InstallSynchronizationContext method"); // inlined mono_assert
        const call_entry_point = get_method$1("CallEntrypoint");
        if (!(call_entry_point)) throw new Error("Assert failed: Can't find CallEntrypoint method"); // inlined mono_assert
        const release_js_owned_object_by_gc_handle_method = get_method$1("ReleaseJSOwnedObjectByGCHandle");
        if (!(release_js_owned_object_by_gc_handle_method)) throw new Error("Assert failed: Can't find ReleaseJSOwnedObjectByGCHandle method"); // inlined mono_assert
        const create_task_callback_method = get_method$1("CreateTaskCallback");
        if (!(create_task_callback_method)) throw new Error("Assert failed: Can't find CreateTaskCallback method"); // inlined mono_assert
        const complete_task_method = get_method$1("CompleteTask");
        if (!(complete_task_method)) throw new Error("Assert failed: Can't find CompleteTask method"); // inlined mono_assert
        const call_delegate_method = get_method$1("CallDelegate");
        if (!(call_delegate_method)) throw new Error("Assert failed: Can't find CallDelegate method"); // inlined mono_assert
        const get_managed_stack_trace_method = get_method$1("GetManagedStackTrace");
        if (!(get_managed_stack_trace_method)) throw new Error("Assert failed: Can't find GetManagedStackTrace method"); // inlined mono_assert
        runtimeHelpers.javaScriptExports.call_entry_point = (entry_point, program_args) => {
            const sp = anyModule.stackSave();
            try {
                const args = alloc_stack_frame(4);
                const res = get_arg(args, 1);
                const arg1 = get_arg(args, 2);
                const arg2 = get_arg(args, 3);
                marshal_intptr_to_cs(arg1, entry_point);
                if (program_args && program_args.length == 0) {
                    program_args = undefined;
                }
                marshal_array_to_cs_impl(arg2, program_args, MarshalerType.String);
                invoke_method_and_handle_exception(call_entry_point, args);
                const promise = marshal_task_to_js(res, undefined, marshal_int32_to_js);
                if (!promise) {
                    return Promise.resolve(0);
                }
                return promise;
            }
            finally {
                anyModule.stackRestore(sp);
            }
        };
        runtimeHelpers.javaScriptExports.release_js_owned_object_by_gc_handle = (gc_handle) => {
            if (!(gc_handle)) throw new Error("Assert failed: Must be valid gc_handle"); // inlined mono_assert
            const sp = anyModule.stackSave();
            try {
                const args = alloc_stack_frame(3);
                const arg1 = get_arg(args, 2);
                set_arg_type(arg1, MarshalerType.Object);
                set_gc_handle(arg1, gc_handle);
                invoke_method_and_handle_exception(release_js_owned_object_by_gc_handle_method, args);
            }
            finally {
                anyModule.stackRestore(sp);
            }
        };
        runtimeHelpers.javaScriptExports.create_task_callback = () => {
            const sp = anyModule.stackSave();
            try {
                const args = alloc_stack_frame(2);
                invoke_method_and_handle_exception(create_task_callback_method, args);
                const res = get_arg(args, 1);
                return get_arg_gc_handle(res);
            }
            finally {
                anyModule.stackRestore(sp);
            }
        };
        runtimeHelpers.javaScriptExports.complete_task = (holder_gc_handle, error, data, res_converter) => {
            const sp = anyModule.stackSave();
            try {
                const args = alloc_stack_frame(5);
                const arg1 = get_arg(args, 2);
                set_arg_type(arg1, MarshalerType.Object);
                set_gc_handle(arg1, holder_gc_handle);
                const arg2 = get_arg(args, 3);
                if (error) {
                    marshal_exception_to_cs(arg2, error);
                }
                else {
                    set_arg_type(arg2, MarshalerType.None);
                    const arg3 = get_arg(args, 4);
                    if (!(res_converter)) throw new Error("Assert failed: res_converter missing"); // inlined mono_assert
                    res_converter(arg3, data);
                }
                invoke_method_and_handle_exception(complete_task_method, args);
            }
            finally {
                anyModule.stackRestore(sp);
            }
        };
        runtimeHelpers.javaScriptExports.call_delegate = (callback_gc_handle, arg1_js, arg2_js, arg3_js, res_converter, arg1_converter, arg2_converter, arg3_converter) => {
            const sp = anyModule.stackSave();
            try {
                const args = alloc_stack_frame(6);
                const arg1 = get_arg(args, 2);
                set_arg_type(arg1, MarshalerType.Object);
                set_gc_handle(arg1, callback_gc_handle);
                // payload arg numbers are shifted by one, the real first is a gc handle of the callback
                if (arg1_converter) {
                    const arg2 = get_arg(args, 3);
                    arg1_converter(arg2, arg1_js);
                }
                if (arg2_converter) {
                    const arg3 = get_arg(args, 4);
                    arg2_converter(arg3, arg2_js);
                }
                if (arg3_converter) {
                    const arg4 = get_arg(args, 5);
                    arg3_converter(arg4, arg3_js);
                }
                invoke_method_and_handle_exception(call_delegate_method, args);
                if (res_converter) {
                    const res = get_arg(args, 1);
                    return res_converter(res);
                }
            }
            finally {
                anyModule.stackRestore(sp);
            }
        };
        runtimeHelpers.javaScriptExports.get_managed_stack_trace = (exception_gc_handle) => {
            const sp = anyModule.stackSave();
            try {
                const args = alloc_stack_frame(3);
                const arg1 = get_arg(args, 2);
                set_arg_type(arg1, MarshalerType.Exception);
                set_gc_handle(arg1, exception_gc_handle);
                invoke_method_and_handle_exception(get_managed_stack_trace_method, args);
                const res = get_arg(args, 1);
                return marshal_string_to_js(res);
            }
            finally {
                anyModule.stackRestore(sp);
            }
        };
        if (install_sync_context) {
            runtimeHelpers.javaScriptExports.install_synchronization_context = () => {
                const sp = anyModule.stackSave();
                try {
                    const args = alloc_stack_frame(2);
                    invoke_method_and_handle_exception(install_sync_context, args);
                }
                finally {
                    anyModule.stackRestore(sp);
                }
            };
            if (!ENVIRONMENT_IS_PTHREAD)
                // Install our sync context so that async continuations will migrate back to this thread (the main thread) automatically
                runtimeHelpers.javaScriptExports.install_synchronization_context();
        }
    }
    function get_method$1(method_name) {
        const res = wrapped_c_functions.mono_wasm_assembly_find_method(runtimeHelpers.runtime_interop_exports_class, method_name, -1);
        if (!res)
            throw "Can't find method " + runtimeHelpers.runtime_interop_namespace + "." + runtimeHelpers.runtime_interop_exports_classname + "." + method_name;
        return res;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    function http_wasm_supports_streaming_response() {
        return typeof Response !== "undefined" && "body" in Response.prototype && typeof ReadableStream === "function";
    }
    function http_wasm_create_abort_controler() {
        return new AbortController();
    }
    function http_wasm_abort_request(abort_controller) {
        abort_controller.abort();
    }
    function http_wasm_abort_response(res) {
        res.__abort_controller.abort();
        if (res.__reader) {
            res.__reader.cancel();
        }
    }
    function http_wasm_fetch_bytes(url, header_names, header_values, option_names, option_values, abort_controller, bodyPtr, bodyLength) {
        // the bufferPtr is pinned by the caller
        const view = new Span(bodyPtr, bodyLength, 0 /* MemoryViewType.Byte */);
        const copy = view.slice();
        return http_wasm_fetch(url, header_names, header_values, option_names, option_values, abort_controller, copy);
    }
    function http_wasm_fetch(url, header_names, header_values, option_names, option_values, abort_controller, body) {
        if (!(url && typeof url === "string")) throw new Error("Assert failed: expected url string"); // inlined mono_assert
        if (!(header_names && header_values && Array.isArray(header_names) && Array.isArray(header_values) && header_names.length === header_values.length)) throw new Error("Assert failed: expected headerNames and headerValues arrays"); // inlined mono_assert
        if (!(option_names && option_values && Array.isArray(option_names) && Array.isArray(option_values) && option_names.length === option_values.length)) throw new Error("Assert failed: expected headerNames and headerValues arrays"); // inlined mono_assert
        const headers = new Headers();
        for (let i = 0; i < header_names.length; i++) {
            headers.append(header_names[i], header_values[i]);
        }
        const options = {
            body,
            headers,
            signal: abort_controller.signal
        };
        for (let i = 0; i < option_names.length; i++) {
            options[option_names[i]] = option_values[i];
        }
        return wrap_as_cancelable_promise(async () => {
            const res = await fetch(url, options);
            res.__abort_controller = abort_controller;
            return res;
        });
    }
    function get_response_headers(res) {
        if (!res.__headerNames) {
            res.__headerNames = [];
            res.__headerValues = [];
            const entries = res.headers.entries();
            for (const pair of entries) {
                res.__headerNames.push(pair[0]);
                res.__headerValues.push(pair[1]);
            }
        }
    }
    function http_wasm_get_response_header_names(res) {
        get_response_headers(res);
        return res.__headerNames;
    }
    function http_wasm_get_response_header_values(res) {
        get_response_headers(res);
        return res.__headerValues;
    }
    function http_wasm_get_response_length(res) {
        return wrap_as_cancelable_promise(async () => {
            const buffer = await res.arrayBuffer();
            res.__buffer = buffer;
            res.__source_offset = 0;
            return buffer.byteLength;
        });
    }
    function http_wasm_get_response_bytes(res, view) {
        if (!(res.__buffer)) throw new Error("Assert failed: expected resoved arrayBuffer"); // inlined mono_assert
        if (res.__source_offset == res.__buffer.byteLength) {
            return 0;
        }
        const source_view = new Uint8Array(res.__buffer, res.__source_offset);
        view.set(source_view, 0);
        const bytes_read = Math.min(view.byteLength, source_view.byteLength);
        res.__source_offset += bytes_read;
        return bytes_read;
    }
    async function http_wasm_get_streamed_response_bytes(res, bufferPtr, bufferLength) {
        // the bufferPtr is pinned by the caller
        const view = new Span(bufferPtr, bufferLength, 0 /* MemoryViewType.Byte */);
        return wrap_as_cancelable_promise(async () => {
            if (!res.__chunk && res.body) {
                res.__reader = res.body.getReader();
                res.__chunk = await res.__reader.read();
                res.__source_offset = 0;
            }
            let target_offset = 0;
            let bytes_read = 0;
            // loop until end of browser stream or end of C# buffer
            while (res.__reader && res.__chunk && !res.__chunk.done) {
                const remaining_source = res.__chunk.value.byteLength - res.__source_offset;
                if (remaining_source === 0) {
                    res.__chunk = await res.__reader.read();
                    res.__source_offset = 0;
                    continue; // are we done yet
                }
                const remaining_target = view.byteLength - target_offset;
                const bytes_copied = Math.min(remaining_source, remaining_target);
                const source_view = res.__chunk.value.subarray(res.__source_offset, res.__source_offset + bytes_copied);
                // copy available bytes
                view.set(source_view, target_offset);
                target_offset += bytes_copied;
                bytes_read += bytes_copied;
                res.__source_offset += bytes_copied;
                if (target_offset == view.byteLength) {
                    return bytes_read;
                }
            }
            return bytes_read;
        });
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    let MONO;
    let BINDING;
    const legacyHelpers = {};
    function set_legacy_exports(exports) {
        MONO = exports.mono;
        BINDING = exports.binding;
    }
    const wasm_type_symbol = Symbol.for("wasm type");

    // Licensed to the .NET Foundation under one or more agreements.
    const fn_signatures = [
        [true, "_get_cs_owned_object_by_js_handle_ref", "GetCSOwnedObjectByJSHandleRef", "iim"],
        [true, "_get_cs_owned_object_js_handle_ref", "GetCSOwnedObjectJSHandleRef", "mi"],
        [true, "_try_get_cs_owned_object_js_handle_ref", "TryGetCSOwnedObjectJSHandleRef", "mi"],
        [false, "_create_cs_owned_proxy_ref", "CreateCSOwnedProxyRef", "iiim"],
        [false, "_get_js_owned_object_by_gc_handle_ref", "GetJSOwnedObjectByGCHandleRef", "im"],
        [true, "_get_js_owned_object_gc_handle_ref", "GetJSOwnedObjectGCHandleRef", "m"],
        [true, "_create_tcs", "CreateTaskSource", ""],
        [true, "_set_tcs_result_ref", "SetTaskSourceResultRef", "iR"],
        [true, "_set_tcs_failure", "SetTaskSourceFailure", "is"],
        [true, "_get_tcs_task_ref", "GetTaskSourceTaskRef", "im"],
        [true, "_setup_js_cont_ref", "SetupJSContinuationRef", "mo"],
        [true, "_object_to_string_ref", "ObjectToStringRef", "m"],
        [true, "_get_date_value_ref", "GetDateValueRef", "m"],
        [true, "_create_date_time_ref", "CreateDateTimeRef", "dm"],
        [true, "_create_uri_ref", "CreateUriRef", "sm"],
        [true, "_is_simple_array_ref", "IsSimpleArrayRef", "m"],
        [false, "_get_call_sig_ref", "GetCallSignatureRef", "im"],
    ];
    const legacyManagedExports = {};
    function bind_runtime_method(method_name, signature) {
        const method = get_method(method_name);
        return mono_bind_method(method, signature, false, "BINDINGS_" + method_name);
    }
    function init_legacy_exports() {
        // please keep System.Runtime.InteropServices.JavaScript.JSHostImplementation.MappedType in sync
        Object.prototype[wasm_type_symbol] = 0;
        Array.prototype[wasm_type_symbol] = 1;
        ArrayBuffer.prototype[wasm_type_symbol] = 2;
        DataView.prototype[wasm_type_symbol] = 3;
        Function.prototype[wasm_type_symbol] = 4;
        Uint8Array.prototype[wasm_type_symbol] = 11;
        const box_buffer_size = 65536;
        legacyHelpers._unbox_buffer_size = 65536;
        legacyHelpers._box_buffer = Module._malloc(box_buffer_size);
        legacyHelpers._unbox_buffer = Module._malloc(legacyHelpers._unbox_buffer_size);
        legacyHelpers._class_int32 = find_corlib_class("System", "Int32");
        legacyHelpers._class_uint32 = find_corlib_class("System", "UInt32");
        legacyHelpers._class_double = find_corlib_class("System", "Double");
        legacyHelpers._class_boolean = find_corlib_class("System", "Boolean");
        legacyHelpers._null_root = mono_wasm_new_root();
        _create_primitive_converters();
        legacyHelpers.runtime_legacy_exports_classname = "LegacyExports";
        legacyHelpers.runtime_legacy_exports_class = wrapped_c_functions.mono_wasm_assembly_find_class(runtimeHelpers.runtime_interop_module, runtimeHelpers.runtime_interop_namespace, legacyHelpers.runtime_legacy_exports_classname);
        if (!legacyHelpers.runtime_legacy_exports_class)
            throw "Can't find " + runtimeHelpers.runtime_interop_namespace + "." + runtimeHelpers.runtime_interop_exports_classname + " class";
        for (const sig of fn_signatures) {
            const wf = legacyManagedExports;
            const [lazy, jsname, csname, signature] = sig;
            if (lazy) {
                // lazy init on first run
                wf[jsname] = function (...args) {
                    const fce = bind_runtime_method(csname, signature);
                    wf[jsname] = fce;
                    return fce(...args);
                };
            }
            else {
                const fce = bind_runtime_method(csname, signature);
                wf[jsname] = fce;
            }
        }
    }
    function get_method(method_name) {
        const res = wrapped_c_functions.mono_wasm_assembly_find_method(legacyHelpers.runtime_legacy_exports_class, method_name, -1);
        if (!res)
            throw "Can't find method " + runtimeHelpers.runtime_interop_namespace + "." + legacyHelpers.runtime_legacy_exports_classname + "." + method_name;
        return res;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function mono_wasm_typed_array_from_ref(pinned_array, begin, end, bytes_per_element, type, is_exception, result_address) {
        const resultRoot = mono_wasm_new_external_root(result_address);
        try {
            const res = typed_array_from(pinned_array, begin, end, bytes_per_element, type);
            // returns JS typed array like Int8Array, to be wraped with JSObject proxy
            js_to_mono_obj_root(res, resultRoot, true);
        }
        catch (exc) {
            wrap_error_root(is_exception, String(exc), resultRoot);
        }
        finally {
            resultRoot.release();
        }
    }
    // Creates a new typed array from pinned array address from pinned_array allocated on the heap to the typed array.
    // 	 address of managed pinned array -> copy from heap -> typed array memory
    function typed_array_from(pinned_array, begin, end, bytes_per_element, type) {
        // typed array
        let newTypedArray = null;
        switch (type) {
            case 5:
                newTypedArray = new Int8Array(end - begin);
                break;
            case 6:
                newTypedArray = new Uint8Array(end - begin);
                break;
            case 7:
                newTypedArray = new Int16Array(end - begin);
                break;
            case 8:
                newTypedArray = new Uint16Array(end - begin);
                break;
            case 9:
                newTypedArray = new Int32Array(end - begin);
                break;
            case 10:
                newTypedArray = new Uint32Array(end - begin);
                break;
            case 13:
                newTypedArray = new Float32Array(end - begin);
                break;
            case 14:
                newTypedArray = new Float64Array(end - begin);
                break;
            case 15: // This is a special case because the typed array is also byte[]
                newTypedArray = new Uint8ClampedArray(end - begin);
                break;
            default:
                throw new Error("Unknown array type " + type);
        }
        typedarray_copy_from(newTypedArray, pinned_array, begin, end, bytes_per_element);
        return newTypedArray;
    }
    // Copy the pinned array address from pinned_array allocated on the heap to the typed array.
    // 	 address of managed pinned array -> copy from heap -> typed array memory
    function typedarray_copy_from(typed_array, pinned_array, begin, end, bytes_per_element) {
        // JavaScript typed arrays are array-like objects and provide a mechanism for accessing
        // raw binary data. (...) To achieve maximum flexibility and efficiency, JavaScript typed arrays
        // split the implementation into buffers and views. A buffer (implemented by the ArrayBuffer object)
        //  is an object representing a chunk of data; it has no format to speak of, and offers no
        // mechanism for accessing its contents. In order to access the memory contained in a buffer,
        // you need to use a view. A view provides a context - that is, a data type, starting offset,
        // and number of elements - that turns the data into an actual typed array.
        // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Typed_arrays
        if (has_backing_array_buffer(typed_array) && typed_array.BYTES_PER_ELEMENT) {
            // Some sanity checks of what is being asked of us
            // lets play it safe and throw an error here instead of assuming to much.
            // Better safe than sorry later
            if (bytes_per_element !== typed_array.BYTES_PER_ELEMENT)
                throw new Error("Inconsistent element sizes: TypedArray.BYTES_PER_ELEMENT '" + typed_array.BYTES_PER_ELEMENT + "' sizeof managed element: '" + bytes_per_element + "'");
            // how much space we have to work with
            let num_of_bytes = (end - begin) * bytes_per_element;
            // how much typed buffer space are we talking about
            const view_bytes = typed_array.length * typed_array.BYTES_PER_ELEMENT;
            // only use what is needed.
            if (num_of_bytes > view_bytes)
                num_of_bytes = view_bytes;
            // Create a new view for mapping
            const typedarrayBytes = new Uint8Array(typed_array.buffer, 0, num_of_bytes);
            // offset index into the view
            const offset = begin * bytes_per_element;
            // Set view bytes to value from HEAPU8
            typedarrayBytes.set(Module.HEAPU8.subarray(pinned_array + offset, pinned_array + offset + num_of_bytes));
            return num_of_bytes;
        }
        else {
            throw new Error("Object '" + typed_array + "' is not a typed array");
        }
    }
    function has_backing_array_buffer(js_obj) {
        return typeof SharedArrayBuffer !== "undefined"
            ? js_obj.buffer instanceof ArrayBuffer || js_obj.buffer instanceof SharedArrayBuffer
            : js_obj.buffer instanceof ArrayBuffer;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function _js_to_mono_uri_root(should_add_in_flight, js_obj, result) {
        switch (true) {
            case js_obj === null:
            case typeof js_obj === "undefined":
                result.clear();
                return;
            case typeof js_obj === "symbol":
            case typeof js_obj === "string":
                legacyManagedExports._create_uri_ref(js_obj, result.address);
                return;
            default:
                _extract_mono_obj_root(should_add_in_flight, js_obj, result);
                return;
        }
    }
    // this is only used from Blazor
    /**
     * @deprecated Not GC or thread safe. For blazor use only
     */
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function js_to_mono_obj(js_obj) {
        const temp = mono_wasm_new_root();
        try {
            js_to_mono_obj_root(js_obj, temp, false);
            return temp.value;
        }
        finally {
            temp.release();
        }
    }
    /**
     * @deprecated Not GC or thread safe
     */
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function _js_to_mono_obj_unsafe(should_add_in_flight, js_obj) {
        const temp = mono_wasm_new_root();
        try {
            js_to_mono_obj_root(js_obj, temp, should_add_in_flight);
            return temp.value;
        }
        finally {
            temp.release();
        }
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function js_to_mono_obj_root(js_obj, result, should_add_in_flight) {
        if (is_nullish(result))
            throw new Error("Expected (value, WasmRoot, boolean)");
        switch (true) {
            case js_obj === null:
            case typeof js_obj === "undefined":
                result.clear();
                return;
            case typeof js_obj === "number": {
                let box_class;
                if ((js_obj | 0) === js_obj) {
                    setI32_unchecked(legacyHelpers._box_buffer, js_obj);
                    box_class = legacyHelpers._class_int32;
                }
                else if ((js_obj >>> 0) === js_obj) {
                    setU32_unchecked(legacyHelpers._box_buffer, js_obj);
                    box_class = legacyHelpers._class_uint32;
                }
                else {
                    setF64(legacyHelpers._box_buffer, js_obj);
                    box_class = legacyHelpers._class_double;
                }
                wrapped_c_functions.mono_wasm_box_primitive_ref(box_class, legacyHelpers._box_buffer, 8, result.address);
                return;
            }
            case typeof js_obj === "string":
                js_string_to_mono_string_root(js_obj, result);
                return;
            case typeof js_obj === "symbol":
                js_string_to_mono_string_interned_root(js_obj, result);
                return;
            case typeof js_obj === "boolean":
                setB32(legacyHelpers._box_buffer, js_obj);
                wrapped_c_functions.mono_wasm_box_primitive_ref(legacyHelpers._class_boolean, legacyHelpers._box_buffer, 4, result.address);
                return;
            case isThenable(js_obj) === true: {
                _wrap_js_thenable_as_task_root(js_obj, result);
                return;
            }
            case js_obj.constructor.name === "Date":
                // getTime() is always UTC
                legacyManagedExports._create_date_time_ref(js_obj.getTime(), result.address);
                return;
            default:
                _extract_mono_obj_root(should_add_in_flight, js_obj, result);
                return;
        }
    }
    function _extract_mono_obj_root(should_add_in_flight, js_obj, result) {
        result.clear();
        if (js_obj === null || typeof js_obj === "undefined")
            return;
        if (js_obj[js_owned_gc_handle_symbol] !== undefined) {
            // for js_owned_gc_handle we don't want to create new proxy
            // since this is strong gc_handle we don't need to in-flight reference
            const gc_handle = assert_not_disposed(js_obj);
            get_js_owned_object_by_gc_handle_ref(gc_handle, result.address);
            return;
        }
        if (js_obj[cs_owned_js_handle_symbol]) {
            get_cs_owned_object_by_js_handle_ref(js_obj[cs_owned_js_handle_symbol], should_add_in_flight, result.address);
            // It's possible the managed object corresponding to this JS object was collected,
            //  in which case we need to make a new one.
            // FIXME: This check is not thread safe
            if (!result.value) {
                delete js_obj[cs_owned_js_handle_symbol];
            }
        }
        // FIXME: This check is not thread safe
        if (!result.value) {
            // Obtain the JS -> C# type mapping.
            const wasm_type = js_obj[wasm_type_symbol];
            const wasm_type_id = typeof wasm_type === "undefined" ? 0 : wasm_type;
            const js_handle = mono_wasm_get_js_handle(js_obj);
            legacyManagedExports._create_cs_owned_proxy_ref(js_handle, wasm_type_id, should_add_in_flight ? 1 : 0, result.address);
        }
    }
    // https://github.com/Planeshifter/emscripten-examples/blob/master/01_PassingArrays/sum_post.js
    function js_typedarray_to_heap(typedArray) {
        const numBytes = typedArray.length * typedArray.BYTES_PER_ELEMENT;
        const ptr = Module._malloc(numBytes);
        const heapBytes = new Uint8Array(Module.HEAPU8.buffer, ptr, numBytes);
        heapBytes.set(new Uint8Array(typedArray.buffer, typedArray.byteOffset, numBytes));
        return heapBytes;
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function js_typed_array_to_array_root(js_obj, result) {
        // JavaScript typed arrays are array-like objects and provide a mechanism for accessing
        // raw binary data. (...) To achieve maximum flexibility and efficiency, JavaScript typed arrays
        // split the implementation into buffers and views. A buffer (implemented by the ArrayBuffer object)
        //  is an object representing a chunk of data; it has no format to speak of, and offers no
        // mechanism for accessing its contents. In order to access the memory contained in a buffer,
        // you need to use a view. A view provides a context - that is, a data type, starting offset,
        // and number of elements - that turns the data into an actual typed array.
        // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Typed_arrays
        if (has_backing_array_buffer(js_obj) && js_obj.BYTES_PER_ELEMENT) {
            const arrayType = js_obj[wasm_type_symbol];
            const heapBytes = js_typedarray_to_heap(js_obj);
            wrapped_c_functions.mono_wasm_typed_array_new_ref(heapBytes.byteOffset, js_obj.length, js_obj.BYTES_PER_ELEMENT, arrayType, result.address);
            Module._free(heapBytes.byteOffset);
        }
        else {
            throw new Error("Object '" + js_obj + "' is not a typed array");
        }
    }
    /**
     * @deprecated Not GC or thread safe
     */
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function js_typed_array_to_array(js_obj) {
        const temp = mono_wasm_new_root();
        try {
            js_typed_array_to_array_root(js_obj, temp);
            return temp.value;
        }
        finally {
            temp.release();
        }
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars, @typescript-eslint/explicit-module-boundary-types
    function js_to_mono_enum(js_obj, method, parmIdx) {
        if (typeof (js_obj) !== "number")
            throw new Error(`Expected numeric value for enum argument, got '${js_obj}'`);
        return js_obj | 0;
    }
    function js_array_to_mono_array(js_array, asString, should_add_in_flight) {
        const arrayRoot = mono_wasm_new_root();
        if (asString)
            wrapped_c_functions.mono_wasm_string_array_new_ref(js_array.length, arrayRoot.address);
        else
            wrapped_c_functions.mono_wasm_obj_array_new_ref(js_array.length, arrayRoot.address);
        const elemRoot = mono_wasm_new_root(MonoObjectNull);
        const arrayAddress = arrayRoot.address;
        const elemAddress = elemRoot.address;
        try {
            for (let i = 0; i < js_array.length; ++i) {
                let obj = js_array[i];
                if (asString)
                    obj = obj.toString();
                js_to_mono_obj_root(obj, elemRoot, should_add_in_flight);
                wrapped_c_functions.mono_wasm_obj_array_set_ref(arrayAddress, i, elemAddress);
            }
            return arrayRoot.value;
        }
        finally {
            mono_wasm_release_roots(arrayRoot, elemRoot);
        }
    }
    function _wrap_js_thenable_as_task_root(thenable, resultRoot) {
        if (!thenable) {
            resultRoot.clear();
            return null;
        }
        // hold strong JS reference to thenable while in flight
        // ideally, this should be hold alive by lifespan of the resulting C# Task, but this is good cheap aproximation
        const thenable_js_handle = mono_wasm_get_js_handle(thenable);
        // Note that we do not implement promise/task roundtrip.
        // With more complexity we could recover original instance when this Task is marshaled back to JS.
        // TODO optimization: return the tcs.Task on this same call instead of _get_tcs_task
        const tcs_gc_handle = legacyManagedExports._create_tcs();
        const holder = { tcs_gc_handle };
        setup_managed_proxy(holder, tcs_gc_handle);
        thenable.then((result) => {
            legacyManagedExports._set_tcs_result_ref(tcs_gc_handle, result);
        }, (reason) => {
            legacyManagedExports._set_tcs_failure(tcs_gc_handle, reason ? reason.toString() : "");
        }).finally(() => {
            // let go of the thenable reference
            mono_wasm_release_cs_owned_object(thenable_js_handle);
            teardown_managed_proxy(holder, tcs_gc_handle); // this holds holder alive for finalizer, until the promise is freed
        });
        legacyManagedExports._get_tcs_task_ref(tcs_gc_handle, resultRoot.address);
        // returns raw pointer to tcs.Task
        return {
            then_js_handle: thenable_js_handle,
        };
    }
    function mono_wasm_typed_array_to_array_ref(js_handle, is_exception, result_address) {
        const resultRoot = mono_wasm_new_external_root(result_address);
        try {
            const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
            if (is_nullish(js_obj)) {
                wrap_error_root(is_exception, "ERR06: Invalid JS object handle '" + js_handle + "'", resultRoot);
                return;
            }
            // returns pointer to C# array
            js_typed_array_to_array_root(js_obj, resultRoot);
        }
        catch (exc) {
            wrap_error_root(is_exception, String(exc), resultRoot);
        }
        finally {
            resultRoot.release();
        }
    }
    // when should_add_in_flight === true, the JSObject would be temporarily hold by Normal gc_handle, so that it would not get collected during transition to the managed stack.
    // its InFlight gc_handle would be freed when the instance arrives to managed side via Interop.Runtime.ReleaseInFlight
    function get_cs_owned_object_by_js_handle_ref(js_handle, should_add_in_flight, result) {
        if (js_handle === JSHandleNull || js_handle === JSHandleDisposed) {
            setI32_unchecked(result, 0);
            return;
        }
        legacyManagedExports._get_cs_owned_object_by_js_handle_ref(js_handle, should_add_in_flight ? 1 : 0, result);
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const delegate_invoke_symbol = Symbol.for("wasm delegate_invoke");
    // this is only used from Blazor
    function unbox_mono_obj(mono_obj) {
        if (mono_obj === MonoObjectNull)
            return undefined;
        const root = mono_wasm_new_root(mono_obj);
        try {
            return unbox_mono_obj_root(root);
        }
        finally {
            root.release();
        }
    }
    function _unbox_cs_owned_root_as_js_object(root) {
        // we don't need in-flight reference as we already have it rooted here
        const js_handle = legacyManagedExports._get_cs_owned_object_js_handle_ref(root.address, 0);
        const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        return js_obj;
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function _unbox_mono_obj_root_with_known_nonprimitive_type_impl(root, type, typePtr, unbox_buffer) {
        //See MARSHAL_TYPE_ defines in driver.c
        switch (type) {
            case 0 /* MarshalType.NULL */:
                return null;
            case 26 /* MarshalType.INT64 */:
            case 27 /* MarshalType.UINT64 */:
                // TODO: Fix this once emscripten offers HEAPI64/HEAPU64 or can return them
                throw new Error("int64 not available");
            case 3 /* MarshalType.STRING */:
            case 29 /* MarshalType.STRING_INTERNED */:
                return conv_string_root(root);
            case 4 /* MarshalType.VT */:
                throw new Error("no idea on how to unbox value types");
            case 5 /* MarshalType.DELEGATE */:
                return _wrap_delegate_root_as_function(root);
            case 6 /* MarshalType.TASK */:
                return _unbox_task_root_as_promise(root);
            case 7 /* MarshalType.OBJECT */:
                return _unbox_ref_type_root_as_js_object(root);
            case 10 /* MarshalType.ARRAY_BYTE */:
            case 11 /* MarshalType.ARRAY_UBYTE */:
            case 12 /* MarshalType.ARRAY_UBYTE_C */:
            case 13 /* MarshalType.ARRAY_SHORT */:
            case 14 /* MarshalType.ARRAY_USHORT */:
            case 15 /* MarshalType.ARRAY_INT */:
            case 16 /* MarshalType.ARRAY_UINT */:
            case 17 /* MarshalType.ARRAY_FLOAT */:
            case 18 /* MarshalType.ARRAY_DOUBLE */:
                throw new Error("Marshaling of primitive arrays are not supported.");
            case 20: // clr .NET DateTime
                return new Date(legacyManagedExports._get_date_value_ref(root.address));
            case 21: // clr .NET DateTimeOffset
                return legacyManagedExports._object_to_string_ref(root.address);
            case 22 /* MarshalType.URI */:
                return legacyManagedExports._object_to_string_ref(root.address);
            case 23 /* MarshalType.SAFEHANDLE */:
                return _unbox_cs_owned_root_as_js_object(root);
            case 30 /* MarshalType.VOID */:
                return undefined;
            default:
                throw new Error(`no idea on how to unbox object of MarshalType ${type} at offset ${root.value} (root address is ${root.address})`);
        }
    }
    function _unbox_mono_obj_root_with_known_nonprimitive_type(root, type, unbox_buffer) {
        if (type >= 512 /* MarshalError.FIRST */)
            throw new Error(`Got marshaling error ${type} when attempting to unbox object at address ${root.value} (root located at ${root.address})`);
        let typePtr = MonoTypeNull;
        if ((type === 4 /* MarshalType.VT */) || (type == 7 /* MarshalType.OBJECT */)) {
            typePtr = getU32(unbox_buffer);
            if (typePtr < 1024)
                throw new Error(`Got invalid MonoType ${typePtr} for object at address ${root.value} (root located at ${root.address})`);
        }
        return _unbox_mono_obj_root_with_known_nonprimitive_type_impl(root, type, typePtr, unbox_buffer);
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function unbox_mono_obj_root(root) {
        if (root.value === 0)
            return undefined;
        const unbox_buffer = legacyHelpers._unbox_buffer;
        const type = wrapped_c_functions.mono_wasm_try_unbox_primitive_and_get_type_ref(root.address, unbox_buffer, legacyHelpers._unbox_buffer_size);
        switch (type) {
            case 1 /* MarshalType.INT */:
                return getI32(unbox_buffer);
            case 25 /* MarshalType.UINT32 */:
                return getU32(unbox_buffer);
            case 32 /* MarshalType.POINTER */:
                // FIXME: Is this right?
                return getU32(unbox_buffer);
            case 24 /* MarshalType.FP32 */:
                return getF32(unbox_buffer);
            case 2 /* MarshalType.FP64 */:
                return getF64(unbox_buffer);
            case 8 /* MarshalType.BOOL */:
                return (getI32(unbox_buffer)) !== 0;
            case 28 /* MarshalType.CHAR */:
                return String.fromCharCode(getI32(unbox_buffer));
            case 0 /* MarshalType.NULL */:
                return null;
            default:
                return _unbox_mono_obj_root_with_known_nonprimitive_type(root, type, unbox_buffer);
        }
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function mono_array_to_js_array(mono_array) {
        if (mono_array === MonoArrayNull)
            return null;
        const arrayRoot = mono_wasm_new_root(mono_array);
        try {
            return mono_array_root_to_js_array(arrayRoot);
        }
        finally {
            arrayRoot.release();
        }
    }
    function is_nested_array_ref(ele) {
        return legacyManagedExports._is_simple_array_ref(ele.address);
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function mono_array_root_to_js_array(arrayRoot) {
        if (arrayRoot.value === MonoArrayNull)
            return null;
        const arrayAddress = arrayRoot.address;
        const elemRoot = mono_wasm_new_root();
        const elemAddress = elemRoot.address;
        try {
            const len = wrapped_c_functions.mono_wasm_array_length(arrayRoot.value);
            const res = new Array(len);
            for (let i = 0; i < len; ++i) {
                // TODO: pass arrayRoot.address and elemRoot.address into new API that copies
                wrapped_c_functions.mono_wasm_array_get_ref(arrayAddress, i, elemAddress);
                if (is_nested_array_ref(elemRoot))
                    res[i] = mono_array_root_to_js_array(elemRoot);
                else
                    res[i] = unbox_mono_obj_root(elemRoot);
            }
            return res;
        }
        finally {
            elemRoot.release();
        }
    }
    function _wrap_delegate_root_as_function(root) {
        if (root.value === MonoObjectNull)
            return null;
        // get strong reference to the Delegate
        const gc_handle = legacyManagedExports._get_js_owned_object_gc_handle_ref(root.address);
        return _wrap_delegate_gc_handle_as_function(gc_handle);
    }
    function _wrap_delegate_gc_handle_as_function(gc_handle) {
        // see if we have js owned instance for this gc_handle already
        let result = _lookup_js_owned_object(gc_handle);
        // If the function for this gc_handle was already collected (or was never created)
        if (!result) {
            // note that we do not implement function/delegate roundtrip
            result = function (...args) {
                assert_not_disposed(result);
                const boundMethod = result[delegate_invoke_symbol];
                return boundMethod(...args);
            };
            // bind the method
            const delegateRoot = mono_wasm_new_root();
            get_js_owned_object_by_gc_handle_ref(gc_handle, delegateRoot.address);
            try {
                if (typeof result[delegate_invoke_symbol] === "undefined") {
                    const method = wrapped_c_functions.mono_wasm_get_delegate_invoke_ref(delegateRoot.address);
                    const signature = mono_method_get_call_signature_ref(method, delegateRoot);
                    const js_method = mono_bind_method(method, signature, true);
                    result[delegate_invoke_symbol] = js_method.bind({ this_arg_gc_handle: gc_handle });
                    if (!result[delegate_invoke_symbol]) {
                        throw new Error("System.Delegate Invoke method can not be resolved.");
                    }
                }
            }
            finally {
                delegateRoot.release();
            }
            setup_managed_proxy(result, gc_handle);
        }
        else {
            assert_not_disposed(result);
        }
        return result;
    }
    function mono_wasm_create_cs_owned_object_ref(core_name, args, is_exception, result_address) {
        const argsRoot = mono_wasm_new_external_root(args), nameRoot = mono_wasm_new_external_root(core_name), resultRoot = mono_wasm_new_external_root(result_address);
        try {
            const js_name = conv_string_root(nameRoot);
            if (!js_name) {
                wrap_error_root(is_exception, "Invalid name @" + nameRoot.value, resultRoot);
                return;
            }
            const coreObj = globalThis[js_name];
            if (coreObj === null || typeof coreObj === "undefined") {
                wrap_error_root(is_exception, "JavaScript host object '" + js_name + "' not found.", resultRoot);
                return;
            }
            try {
                const js_args = mono_array_root_to_js_array(argsRoot);
                // This is all experimental !!!!!!
                const allocator = function (constructor, js_args) {
                    // Not sure if we should be checking for anything here
                    let argsList = [];
                    argsList[0] = constructor;
                    if (js_args)
                        argsList = argsList.concat(js_args);
                    // eslint-disable-next-line prefer-spread
                    const tempCtor = constructor.bind.apply(constructor, argsList);
                    const js_obj = new tempCtor();
                    return js_obj;
                };
                const js_obj = allocator(coreObj, js_args);
                const js_handle = mono_wasm_get_js_handle(js_obj);
                // returns boxed js_handle int, because on exception we need to return String on same method signature
                // here we don't have anything to in-flight reference, as the JSObject doesn't exist yet
                js_to_mono_obj_root(js_handle, resultRoot, false);
            }
            catch (ex) {
                wrap_error_root(is_exception, ex, resultRoot);
                return;
            }
        }
        finally {
            resultRoot.release();
            argsRoot.release();
            nameRoot.release();
        }
    }
    function _unbox_task_root_as_promise(root) {
        if (root.value === MonoObjectNull)
            return null;
        if (!_are_promises_supported)
            throw new Error("Promises are not supported thus 'System.Threading.Tasks.Task' can not work in this context.");
        // get strong reference to Task
        const gc_handle = legacyManagedExports._get_js_owned_object_gc_handle_ref(root.address);
        // see if we have js owned instance for this gc_handle already
        let result = _lookup_js_owned_object(gc_handle);
        // If the promise for this gc_handle was already collected (or was never created)
        if (!result) {
            const explicitFinalization = () => teardown_managed_proxy(result, gc_handle);
            const { promise, promise_control } = createPromiseController(explicitFinalization, explicitFinalization);
            // note that we do not implement promise/task roundtrip
            // With more complexity we could recover original instance when this promise is marshaled back to C#.
            result = promise;
            // register C# side of the continuation
            legacyManagedExports._setup_js_cont_ref(root.address, promise_control);
            setup_managed_proxy(result, gc_handle);
        }
        return result;
    }
    function _unbox_ref_type_root_as_js_object(root) {
        if (root.value === MonoObjectNull)
            return null;
        // this could be JSObject proxy of a js native object
        // we don't need in-flight reference as we already have it rooted here
        const js_handle = legacyManagedExports._try_get_cs_owned_object_js_handle_ref(root.address, 0);
        if (js_handle) {
            if (js_handle === JSHandleDisposed) {
                throw new Error("Cannot access a disposed JSObject at " + root.value);
            }
            return mono_wasm_get_jsobj_from_js_handle(js_handle);
        }
        // otherwise this is C# only object
        // get strong reference to Object
        const gc_handle = legacyManagedExports._get_js_owned_object_gc_handle_ref(root.address);
        // see if we have js owned instance for this gc_handle already
        let result = _lookup_js_owned_object(gc_handle);
        // If the JS object for this gc_handle was already collected (or was never created)
        if (is_nullish(result)) {
            result = new ManagedObject();
            setup_managed_proxy(result, gc_handle);
        }
        return result;
    }
    function get_js_owned_object_by_gc_handle_ref(gc_handle, result) {
        if (!gc_handle) {
            setI32_unchecked(result, 0);
            return;
        }
        // this is always strong gc_handle
        legacyManagedExports._get_js_owned_object_by_gc_handle_ref(gc_handle, result);
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const boundMethodsByFqn = new Map();
    function _teardown_after_call(converter, token, buffer, resultRoot, exceptionRoot, thisArgRoot, sp) {
        _release_temp_frame();
        Module.stackRestore(sp);
        if (typeof (resultRoot) === "object") {
            resultRoot.clear();
            if ((token !== null) && (token.scratchResultRoot === null))
                token.scratchResultRoot = resultRoot;
            else
                resultRoot.release();
        }
        if (typeof (exceptionRoot) === "object") {
            exceptionRoot.clear();
            if ((token !== null) && (token.scratchExceptionRoot === null))
                token.scratchExceptionRoot = exceptionRoot;
            else
                exceptionRoot.release();
        }
        if (typeof (thisArgRoot) === "object") {
            thisArgRoot.clear();
            if ((token !== null) && (token.scratchThisArgRoot === null))
                token.scratchThisArgRoot = thisArgRoot;
            else
                thisArgRoot.release();
        }
    }
    function mono_bind_static_method(fqn, signature /*ArgsMarshalString*/) {
        if (!(runtimeHelpers.mono_wasm_bindings_is_ready)) throw new Error("Assert failed: The runtime must be initialized."); // inlined mono_assert
        const key = `${fqn}-${signature}`;
        let js_method = boundMethodsByFqn.get(key);
        if (js_method === undefined) {
            const method = mono_method_resolve(fqn);
            if (typeof signature === "undefined")
                signature = mono_method_get_call_signature_ref(method, undefined);
            js_method = mono_bind_method(method, signature, false, fqn);
            boundMethodsByFqn.set(key, js_method);
        }
        return js_method;
    }
    function mono_bind_assembly_entry_point(assembly, signature /*ArgsMarshalString*/) {
        const method = find_entry_point(assembly);
        if (typeof (signature) !== "string")
            signature = mono_method_get_call_signature_ref(method, undefined);
        const js_method = mono_bind_method(method, signature, false, "_" + assembly + "__entrypoint");
        return async function (...args) {
            if (args.length > 0 && Array.isArray(args[0]))
                args[0] = js_array_to_mono_array(args[0], true, false);
            return js_method(...args);
        };
    }
    function mono_call_assembly_entry_point(assembly, args, signature /*ArgsMarshalString*/) {
        if (!(runtimeHelpers.mono_wasm_bindings_is_ready)) throw new Error("Assert failed: The runtime must be initialized."); // inlined mono_assert
        if (!args) {
            args = [[]];
        }
        return mono_bind_assembly_entry_point(assembly, signature)(...args);
    }
    function mono_wasm_invoke_js_with_args_ref(js_handle, method_name, args, is_exception, result_address) {
        const argsRoot = mono_wasm_new_external_root(args), nameRoot = mono_wasm_new_external_root(method_name), resultRoot = mono_wasm_new_external_root(result_address);
        try {
            const js_name = conv_string_root(nameRoot);
            if (!js_name || (typeof (js_name) !== "string")) {
                wrap_error_root(is_exception, "ERR12: Invalid method name object @" + nameRoot.value, resultRoot);
                return;
            }
            const obj = get_js_obj(js_handle);
            if (is_nullish(obj)) {
                wrap_error_root(is_exception, "ERR13: Invalid JS object handle '" + js_handle + "' while invoking '" + js_name + "'", resultRoot);
                return;
            }
            const js_args = mono_array_root_to_js_array(argsRoot);
            try {
                const m = obj[js_name];
                if (typeof m === "undefined")
                    throw new Error("Method: '" + js_name + "' not found for: '" + Object.prototype.toString.call(obj) + "'");
                const res = m.apply(obj, js_args);
                js_to_mono_obj_root(res, resultRoot, true);
            }
            catch (ex) {
                wrap_error_root(is_exception, ex, resultRoot);
            }
        }
        finally {
            argsRoot.release();
            nameRoot.release();
            resultRoot.release();
        }
    }
    function mono_wasm_get_object_property_ref(js_handle, property_name, is_exception, result_address) {
        const nameRoot = mono_wasm_new_external_root(property_name), resultRoot = mono_wasm_new_external_root(result_address);
        try {
            const js_name = conv_string_root(nameRoot);
            if (!js_name) {
                wrap_error_root(is_exception, "Invalid property name object '" + nameRoot.value + "'", resultRoot);
                return;
            }
            const obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
            if (is_nullish(obj)) {
                wrap_error_root(is_exception, "ERR01: Invalid JS object handle '" + js_handle + "' while geting '" + js_name + "'", resultRoot);
                return;
            }
            const m = obj[js_name];
            js_to_mono_obj_root(m, resultRoot, true);
        }
        catch (ex) {
            wrap_error_root(is_exception, ex, resultRoot);
        }
        finally {
            resultRoot.release();
            nameRoot.release();
        }
    }
    function mono_wasm_set_object_property_ref(js_handle, property_name, value, createIfNotExist, hasOwnProperty, is_exception, result_address) {
        const valueRoot = mono_wasm_new_external_root(value), nameRoot = mono_wasm_new_external_root(property_name), resultRoot = mono_wasm_new_external_root(result_address);
        try {
            const property = conv_string_root(nameRoot);
            if (!property) {
                wrap_error_root(is_exception, "Invalid property name object '" + property_name + "'", resultRoot);
                return;
            }
            const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
            if (is_nullish(js_obj)) {
                wrap_error_root(is_exception, "ERR02: Invalid JS object handle '" + js_handle + "' while setting '" + property + "'", resultRoot);
                return;
            }
            let result = false;
            const js_value = unbox_mono_obj_root(valueRoot);
            if (createIfNotExist) {
                js_obj[property] = js_value;
                result = true;
            }
            else {
                result = false;
                if (!createIfNotExist) {
                    if (!Object.prototype.hasOwnProperty.call(js_obj, property)) {
                        js_to_mono_obj_root(false, resultRoot, false);
                        return;
                    }
                }
                if (hasOwnProperty === true) {
                    if (Object.prototype.hasOwnProperty.call(js_obj, property)) {
                        js_obj[property] = js_value;
                        result = true;
                    }
                }
                else {
                    js_obj[property] = js_value;
                    result = true;
                }
            }
            js_to_mono_obj_root(result, resultRoot, false);
        }
        catch (ex) {
            wrap_error_root(is_exception, ex, resultRoot);
        }
        finally {
            resultRoot.release();
            nameRoot.release();
            valueRoot.release();
        }
    }
    function mono_wasm_get_by_index_ref(js_handle, property_index, is_exception, result_address) {
        const resultRoot = mono_wasm_new_external_root(result_address);
        try {
            const obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
            if (is_nullish(obj)) {
                wrap_error_root(is_exception, "ERR03: Invalid JS object handle '" + js_handle + "' while getting [" + property_index + "]", resultRoot);
                return;
            }
            const m = obj[property_index];
            js_to_mono_obj_root(m, resultRoot, true);
        }
        catch (ex) {
            wrap_error_root(is_exception, ex, resultRoot);
        }
        finally {
            resultRoot.release();
        }
    }
    function mono_wasm_set_by_index_ref(js_handle, property_index, value, is_exception, result_address) {
        const valueRoot = mono_wasm_new_external_root(value), resultRoot = mono_wasm_new_external_root(result_address);
        try {
            const obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
            if (is_nullish(obj)) {
                wrap_error_root(is_exception, "ERR04: Invalid JS object handle '" + js_handle + "' while setting [" + property_index + "]", resultRoot);
                return;
            }
            const js_value = unbox_mono_obj_root(valueRoot);
            obj[property_index] = js_value;
            resultRoot.clear();
        }
        catch (ex) {
            wrap_error_root(is_exception, ex, resultRoot);
        }
        finally {
            resultRoot.release();
            valueRoot.release();
        }
    }
    function mono_wasm_get_global_object_ref(global_name, is_exception, result_address) {
        const nameRoot = mono_wasm_new_external_root(global_name), resultRoot = mono_wasm_new_external_root(result_address);
        try {
            const js_name = conv_string_root(nameRoot);
            let globalObj;
            if (!js_name) {
                globalObj = globalThis;
            }
            else if (js_name == "Module") {
                globalObj = Module;
            }
            else if (js_name == "INTERNAL") {
                globalObj = INTERNAL;
            }
            else {
                globalObj = globalThis[js_name];
            }
            // TODO returning null may be useful when probing for browser features
            if (globalObj === null || typeof globalObj === undefined) {
                wrap_error_root(is_exception, "Global object '" + js_name + "' not found.", resultRoot);
                return;
            }
            js_to_mono_obj_root(globalObj, resultRoot, true);
        }
        catch (ex) {
            wrap_error_root(is_exception, ex, resultRoot);
        }
        finally {
            resultRoot.release();
            nameRoot.release();
        }
    }
    // Blazor specific custom routine
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function mono_wasm_invoke_js_blazor(exceptionMessage, callInfo, arg0, arg1, arg2) {
        try {
            const blazorExports = globalThis.Blazor;
            if (!blazorExports) {
                throw new Error("The blazor.webassembly.js library is not loaded.");
            }
            return blazorExports._internal.invokeJSFromDotNet(callInfo, arg0, arg1, arg2);
        }
        catch (ex) {
            const exceptionJsString = ex.message + "\n" + ex.stack;
            const exceptionRoot = mono_wasm_new_root();
            js_string_to_mono_string_root(exceptionJsString, exceptionRoot);
            exceptionRoot.copy_to_address(exceptionMessage);
            exceptionRoot.release();
            return 0;
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const escapeRE = /[^A-Za-z0-9_$]/g;
    const primitiveConverters = new Map();
    const _signature_converters = new Map();
    const boundMethodsByMethod = new Map();
    function _get_type_name(typePtr) {
        if (!typePtr)
            return "<null>";
        return wrapped_c_functions.mono_wasm_get_type_name(typePtr);
    }
    function _get_type_aqn(typePtr) {
        if (!typePtr)
            return "<null>";
        return wrapped_c_functions.mono_wasm_get_type_aqn(typePtr);
    }
    function _get_class_name(classPtr) {
        if (!classPtr)
            return "<null>";
        return wrapped_c_functions.mono_wasm_get_type_name(wrapped_c_functions.mono_wasm_class_get_type(classPtr));
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function _create_named_function(name, argumentNames, body, closure) {
        let result = null;
        let closureArgumentList = null;
        let closureArgumentNames = null;
        if (closure) {
            closureArgumentNames = Object.keys(closure);
            closureArgumentList = new Array(closureArgumentNames.length);
            for (let i = 0, l = closureArgumentNames.length; i < l; i++)
                closureArgumentList[i] = closure[closureArgumentNames[i]];
        }
        const constructor = _create_rebindable_named_function(name, argumentNames, body, closureArgumentNames);
        // eslint-disable-next-line prefer-spread
        result = constructor.apply(null, closureArgumentList);
        return result;
    }
    function _create_rebindable_named_function(name, argumentNames, body, closureArgNames) {
        const strictPrefix = "\"use strict\";\r\n";
        let uriPrefix = "", escapedFunctionIdentifier = "";
        if (name) {
            uriPrefix = "//# sourceURL=https://dotnet.generated.invalid/" + name + "\r\n";
            escapedFunctionIdentifier = name;
        }
        else {
            escapedFunctionIdentifier = "unnamed";
        }
        let rawFunctionText = "function " + escapedFunctionIdentifier + "(" +
            argumentNames.join(", ") +
            ") {\r\n" +
            body +
            "\r\n};\r\n";
        const lineBreakRE = /\r(\n?)/g;
        rawFunctionText =
            uriPrefix + strictPrefix +
                rawFunctionText.replace(lineBreakRE, "\r\n    ") +
                `    return ${escapedFunctionIdentifier};\r\n`;
        let result = null, keys = null;
        if (closureArgNames) {
            keys = closureArgNames.concat([rawFunctionText]);
        }
        else {
            keys = [rawFunctionText];
        }
        result = Function.apply(Function, keys);
        return result;
    }
    function _create_primitive_converters() {
        const result = primitiveConverters;
        result.set("m", { steps: [{}], size: 0 });
        result.set("s", { steps: [{ convert_root: js_string_to_mono_string_root.bind(BINDING) }], size: 0, needs_root: true });
        result.set("S", { steps: [{ convert_root: js_string_to_mono_string_interned_root.bind(BINDING) }], size: 0, needs_root: true });
        // note we also bind first argument to false for both _js_to_mono_obj and _js_to_mono_uri,
        // because we will root the reference, so we don't need in-flight reference
        // also as those are callback arguments and we don't have platform code which would release the in-flight reference on C# end
        result.set("o", { steps: [{ convert_root: js_to_mono_obj_root.bind(BINDING) }], size: 0, needs_root: true });
        result.set("u", { steps: [{ convert_root: _js_to_mono_uri_root.bind(BINDING, false) }], size: 0, needs_root: true });
        // ref object aka T&&
        result.set("R", { steps: [{ convert_root: js_to_mono_obj_root.bind(BINDING), byref: true }], size: 0, needs_root: true });
        // result.set ('k', { steps: [{ convert: js_to_mono_enum.bind (this), indirect: 'i64'}], size: 8});
        result.set("j", { steps: [{ convert: js_to_mono_enum.bind(BINDING), indirect: "i32" }], size: 8 });
        result.set("b", { steps: [{ indirect: "bool" }], size: 8 });
        result.set("i", { steps: [{ indirect: "i32" }], size: 8 });
        result.set("I", { steps: [{ indirect: "u32" }], size: 8 });
        result.set("l", { steps: [{ indirect: "i52" }], size: 8 });
        result.set("L", { steps: [{ indirect: "u52" }], size: 8 });
        result.set("f", { steps: [{ indirect: "float" }], size: 8 });
        result.set("d", { steps: [{ indirect: "double" }], size: 8 });
    }
    function _create_converter_for_marshal_string(args_marshal /*ArgsMarshalString*/) {
        const steps = [];
        let size = 0;
        let is_result_definitely_unmarshaled = false, is_result_possibly_unmarshaled = false, result_unmarshaled_if_argc = -1, needs_root_buffer = false;
        for (let i = 0; i < args_marshal.length; ++i) {
            const key = args_marshal[i];
            if (i === args_marshal.length - 1) {
                if (key === "!") {
                    is_result_definitely_unmarshaled = true;
                    continue;
                }
                else if (key === "m") {
                    is_result_possibly_unmarshaled = true;
                    result_unmarshaled_if_argc = args_marshal.length - 1;
                }
            }
            else if (key === "!")
                throw new Error("! must be at the end of the signature");
            const conv = primitiveConverters.get(key);
            if (!conv)
                throw new Error("Unknown parameter type " + key);
            const localStep = Object.create(conv.steps[0]);
            localStep.size = conv.size;
            if (conv.needs_root)
                needs_root_buffer = true;
            localStep.needs_root = conv.needs_root;
            localStep.key = key;
            steps.push(localStep);
            size += conv.size;
        }
        return {
            steps, size, args_marshal,
            is_result_definitely_unmarshaled,
            is_result_possibly_unmarshaled,
            result_unmarshaled_if_argc,
            needs_root_buffer
        };
    }
    function _get_converter_for_marshal_string(args_marshal /*ArgsMarshalString*/) {
        let converter = _signature_converters.get(args_marshal);
        if (!converter) {
            converter = _create_converter_for_marshal_string(args_marshal);
            _signature_converters.set(args_marshal, converter);
        }
        return converter;
    }
    function _compile_converter_for_marshal_string(args_marshal /*ArgsMarshalString*/) {
        const converter = _get_converter_for_marshal_string(args_marshal);
        if (typeof (converter.args_marshal) !== "string")
            throw new Error("Corrupt converter for '" + args_marshal + "'");
        if (converter.compiled_function && converter.compiled_variadic_function)
            return converter;
        const converterName = args_marshal.replace("!", "_result_unmarshaled");
        converter.name = converterName;
        let body = [];
        let argumentNames = ["method"];
        const closure = {
            Module,
            setI32,
            setU32,
            setF32,
            setF64,
            setU52,
            setI52,
            setB32,
            setI32_unchecked,
            setU32_unchecked,
            scratchValueRoot: converter.scratchValueRoot,
            stackAlloc: Module.stackAlloc,
            _zero_region
        };
        let indirectLocalOffset = 0;
        // ensure the indirect values are 8-byte aligned so that aligned loads and stores will work
        const indirectBaseOffset = ((((args_marshal.length * 4) + 7) / 8) | 0) * 8;
        // worst-case allocation size instead of allocating dynamically, plus padding
        // the padding is necessary to ensure that we don't overrun the buffer due to
        //  the 8-byte alignment we did above
        const bufferSizeBytes = converter.size + (args_marshal.length * 4) + 16;
        body.push("if (!method) throw new Error('no method provided');", `const buffer = stackAlloc(${bufferSizeBytes});`, `_zero_region(buffer, ${bufferSizeBytes});`, `const indirectStart = buffer + ${indirectBaseOffset};`, "");
        for (let i = 0; i < converter.steps.length; i++) {
            const step = converter.steps[i];
            const closureKey = "step" + i;
            const valueKey = "value" + i;
            const argKey = "arg" + i;
            const offsetText = `(indirectStart + ${indirectLocalOffset})`;
            argumentNames.push(argKey);
            if (step.convert_root) {
                if (!(!step.indirect)) throw new Error("Assert failed: converter step cannot both be rooted and indirect"); // inlined mono_assert
                if (!converter.scratchValueRoot) {
                    // HACK: new_external_root rightly won't accept a null address
                    const dummyAddress = Module.stackSave();
                    converter.scratchValueRoot = mono_wasm_new_external_root(dummyAddress);
                    closure.scratchValueRoot = converter.scratchValueRoot;
                }
                closure[closureKey] = step.convert_root;
                // Update our scratch external root to point to the indirect slot where our
                //  managed pointer is destined to live
                body.push(`scratchValueRoot._set_address(${offsetText});`);
                // Convert the object and store the managed reference through our scratch external root
                body.push(`${closureKey}(${argKey}, scratchValueRoot);`);
                if (step.byref) {
                    // for T&& we pass the address of the pointer stored on the stack
                    body.push(`let ${valueKey} = ${offsetText};`);
                }
                else {
                    // It is safe to pass the pointer by value now since we know it is pinned
                    body.push(`let ${valueKey} = scratchValueRoot.value;`);
                }
            }
            else if (step.convert) {
                closure[closureKey] = step.convert;
                body.push(`let ${valueKey} = ${closureKey}(${argKey}, method, ${i});`);
            }
            else {
                body.push(`let ${valueKey} = ${argKey};`);
            }
            if (step.needs_root && !step.convert_root) {
                body.push("if (!rootBuffer) throw new Error('no root buffer provided');");
                body.push(`rootBuffer.set (${i}, ${valueKey});`);
            }
            if (step.indirect) {
                switch (step.indirect) {
                    case "bool":
                        body.push(`setB32(${offsetText}, ${valueKey});`);
                        break;
                    case "u32":
                        body.push(`setU32(${offsetText}, ${valueKey});`);
                        break;
                    case "i32":
                        body.push(`setI32(${offsetText}, ${valueKey});`);
                        break;
                    case "float":
                        body.push(`setF32(${offsetText}, ${valueKey});`);
                        break;
                    case "double":
                        body.push(`setF64(${offsetText}, ${valueKey});`);
                        break;
                    case "i52":
                        body.push(`setI52(${offsetText}, ${valueKey});`);
                        break;
                    case "u52":
                        body.push(`setU52(${offsetText}, ${valueKey});`);
                        break;
                    default:
                        throw new Error("Unimplemented indirect type: " + step.indirect);
                }
                body.push(`setU32_unchecked(buffer + (${i} * 4), ${offsetText});`);
                indirectLocalOffset += step.size;
            }
            else {
                body.push(`setU32_unchecked(buffer + (${i} * 4), ${valueKey});`);
                indirectLocalOffset += 4;
            }
            body.push("");
        }
        body.push("return buffer;");
        let bodyJs = body.join("\r\n"), compiledFunction = null, compiledVariadicFunction = null;
        try {
            compiledFunction = _create_named_function("converter_" + converterName, argumentNames, bodyJs, closure);
            converter.compiled_function = compiledFunction;
        }
        catch (exc) {
            converter.compiled_function = null;
            console.warn("MONO_WASM: compiling converter failed for", bodyJs, "with error", exc);
            throw exc;
        }
        argumentNames = ["method", "args"];
        const variadicClosure = {
            converter: compiledFunction
        };
        body = [
            "return converter(",
            "  method,"
        ];
        for (let i = 0; i < converter.steps.length; i++) {
            body.push("  args[" + i +
                ((i == converter.steps.length - 1)
                    ? "]"
                    : "], "));
        }
        body.push(");");
        bodyJs = body.join("\r\n");
        try {
            compiledVariadicFunction = _create_named_function("variadic_converter_" + converterName, argumentNames, bodyJs, variadicClosure);
            converter.compiled_variadic_function = compiledVariadicFunction;
        }
        catch (exc) {
            converter.compiled_variadic_function = null;
            console.warn("MONO_WASM: compiling converter failed for", bodyJs, "with error", exc);
            throw exc;
        }
        converter.scratchRootBuffer = null;
        converter.scratchBuffer = VoidPtrNull;
        return converter;
    }
    function _maybe_produce_signature_warning(converter) {
        if (converter.has_warned_about_signature)
            return;
        console.warn("MONO_WASM: Deprecated raw return value signature: '" + converter.args_marshal + "'. End the signature with '!' instead of 'm'.");
        converter.has_warned_about_signature = true;
    }
    function _decide_if_result_is_marshaled(converter, argc) {
        if (!converter)
            return true;
        if (converter.is_result_possibly_unmarshaled &&
            (argc === converter.result_unmarshaled_if_argc)) {
            if (argc < converter.result_unmarshaled_if_argc)
                throw new Error(`Expected >= ${converter.result_unmarshaled_if_argc} argument(s) but got ${argc} for signature '${converter.args_marshal}'`);
            _maybe_produce_signature_warning(converter);
            return false;
        }
        else {
            if (argc < converter.steps.length)
                throw new Error(`Expected ${converter.steps.length} argument(s) but got ${argc} for signature '${converter.args_marshal}'`);
            return !converter.is_result_definitely_unmarshaled;
        }
    }
    function mono_bind_method(method, args_marshal /*ArgsMarshalString*/, has_this_arg, friendly_name) {
        if (typeof (args_marshal) !== "string")
            throw new Error("args_marshal argument invalid, expected string");
        const key = `managed_${method}_${args_marshal}`;
        let result = boundMethodsByMethod.get(key);
        if (result) {
            return result;
        }
        if (!friendly_name) {
            friendly_name = key;
        }
        let converter = null;
        if (typeof (args_marshal) === "string") {
            converter = _compile_converter_for_marshal_string(args_marshal);
        }
        // FIXME
        const unbox_buffer_size = 128;
        const unbox_buffer = Module._malloc(unbox_buffer_size);
        const token = {
            method,
            converter,
            scratchRootBuffer: null,
            scratchBuffer: VoidPtrNull,
            scratchResultRoot: mono_wasm_new_root(),
            scratchExceptionRoot: mono_wasm_new_root(),
            scratchThisArgRoot: mono_wasm_new_root()
        };
        const closure = {
            Module,
            mono_wasm_new_root,
            get_js_owned_object_by_gc_handle_ref,
            _create_temp_frame,
            _handle_exception_for_call,
            _teardown_after_call,
            mono_wasm_try_unbox_primitive_and_get_type_ref: wrapped_c_functions.mono_wasm_try_unbox_primitive_and_get_type_ref,
            _unbox_mono_obj_root_with_known_nonprimitive_type,
            invoke_method_ref: wrapped_c_functions.mono_wasm_invoke_method_ref,
            method,
            token,
            unbox_buffer,
            unbox_buffer_size,
            getB32,
            getI32,
            getU32,
            getF32,
            getF64,
            stackSave: Module.stackSave
        };
        const converterKey = converter ? "converter_" + converter.name : "";
        if (converter)
            closure[converterKey] = converter;
        const argumentNames = [];
        const body = [
            "_create_temp_frame();",
            "let resultRoot = token.scratchResultRoot, exceptionRoot = token.scratchExceptionRoot, thisArgRoot = token.scratchThisArgRoot , sp = stackSave();",
            "token.scratchResultRoot = null;",
            "token.scratchExceptionRoot = null;",
            "token.scratchThisArgRoot = null;",
            "if (resultRoot === null)",
            "	resultRoot = mono_wasm_new_root ();",
            "if (exceptionRoot === null)",
            "	exceptionRoot = mono_wasm_new_root ();",
            "if (thisArgRoot === null)",
            "	thisArgRoot = mono_wasm_new_root ();",
            ""
        ];
        if (converter) {
            body.push(`let buffer = ${converterKey}.compiled_function(`, "    method,");
            for (let i = 0; i < converter.steps.length; i++) {
                const argName = "arg" + i;
                argumentNames.push(argName);
                body.push("    " + argName +
                    ((i == converter.steps.length - 1)
                        ? ""
                        : ", "));
            }
            body.push(");");
        }
        else {
            body.push("let buffer = 0;");
        }
        if (converter && converter.is_result_definitely_unmarshaled) {
            body.push("let is_result_marshaled = false;");
        }
        else if (converter && converter.is_result_possibly_unmarshaled) {
            body.push(`let is_result_marshaled = arguments.length !== ${converter.result_unmarshaled_if_argc};`);
        }
        else {
            body.push("let is_result_marshaled = true;");
        }
        // We inline a bunch of the invoke and marshaling logic here in order to eliminate the GC pressure normally
        //  created by the unboxing part of the call process. Because unbox_mono_obj(_root) can return non-numeric
        //  types, v8 and spidermonkey allocate and store its result on the heap (in the nursery, to be fair).
        // For a bound method however, we know the result will always be the same type because C# methods have known
        //  return types. Inlining the invoke and marshaling logic means that even though the bound method has logic
        //  for handling various types, only one path through the method (for its appropriate return type) will ever
        //  be taken, and the JIT will see that the 'result' local and thus the return value of this function are
        //  always of the exact same type. All of the branches related to this end up being predicted and low-cost.
        // The end result is that bound method invocations don't always allocate, so no more nursery GCs. Yay! -kg
        body.push("", "", "");
        if (has_this_arg) {
            body.push("get_js_owned_object_by_gc_handle_ref(this.this_arg_gc_handle, thisArgRoot.address);");
            body.push("invoke_method_ref (method, thisArgRoot.address, buffer, exceptionRoot.address, resultRoot.address);");
        }
        else {
            body.push("invoke_method_ref (method, 0, buffer, exceptionRoot.address, resultRoot.address);");
        }
        body.push(`_handle_exception_for_call (${converterKey}, token, buffer, resultRoot, exceptionRoot, thisArgRoot, sp);`, "", "let resultPtr = resultRoot.value, result = undefined;");
        if (converter) {
            if (converter.is_result_possibly_unmarshaled)
                body.push("if (!is_result_marshaled) ");
            if (converter.is_result_definitely_unmarshaled || converter.is_result_possibly_unmarshaled)
                body.push("    result = resultPtr;");
            if (!converter.is_result_definitely_unmarshaled)
                body.push("if (is_result_marshaled) {", 
                // For the common scenario where the return type is a primitive, we want to try and unbox it directly
                //  into our existing heap allocation and then read it out of the heap. Doing this all in one operation
                //  means that we only need to enter a gc safe region twice (instead of 3+ times with the normal,
                //  slower check-type-and-then-unbox flow which has extra checks since unbox verifies the type).
                "    let resultType = mono_wasm_try_unbox_primitive_and_get_type_ref (resultRoot.address, unbox_buffer, unbox_buffer_size);", "    switch (resultType) {", `    case ${1 /* MarshalType.INT */}:`, "        result = getI32(unbox_buffer); break;", `    case ${32 /* MarshalType.POINTER */}:`, // FIXME: Is this right?
                `    case ${25 /* MarshalType.UINT32 */}:`, "        result = getU32(unbox_buffer); break;", `    case ${24 /* MarshalType.FP32 */}:`, "        result = getF32(unbox_buffer); break;", `    case ${2 /* MarshalType.FP64 */}:`, "        result = getF64(unbox_buffer); break;", `    case ${8 /* MarshalType.BOOL */}:`, "        result = getB32(unbox_buffer); break;", `    case ${28 /* MarshalType.CHAR */}:`, "        result = String.fromCharCode(getI32(unbox_buffer)); break;", `    case ${0 /* MarshalType.NULL */}:`, "        result = null; break;", "    default:", "        result = _unbox_mono_obj_root_with_known_nonprimitive_type (resultRoot, resultType, unbox_buffer); break;", "    }", "}");
        }
        else {
            throw new Error("No converter");
        }
        let displayName = friendly_name.replace(escapeRE, "_");
        if (has_this_arg)
            displayName += "_this";
        body.push(`_teardown_after_call (${converterKey}, token, buffer, resultRoot, exceptionRoot, thisArgRoot, sp);`, "return result;");
        const bodyJs = body.join("\r\n");
        result = _create_named_function(displayName, argumentNames, bodyJs, closure);
        boundMethodsByMethod.set(key, result);
        return result;
    }
    function _handle_exception_for_call(converter, token, buffer, resultRoot, exceptionRoot, thisArgRoot, sp) {
        const exc = _convert_exception_for_method_call(resultRoot, exceptionRoot);
        if (!exc)
            return;
        _teardown_after_call(converter, token, buffer, resultRoot, exceptionRoot, thisArgRoot, sp);
        throw exc;
    }
    function _convert_exception_for_method_call(result, exception) {
        if (exception.value === MonoObjectNull)
            return null;
        const msg = conv_string_root(result);
        const err = new Error(msg); //the convention is that invoke_method ToString () any outgoing exception
        // console.warn (`error ${msg} at location ${err.stack});
        return err;
    }
    function mono_method_resolve(fqn) {
        const { assembly, namespace, classname, methodname } = parseFQN(fqn);
        const asm = wrapped_c_functions.mono_wasm_assembly_load(assembly);
        if (!asm)
            throw new Error("Could not find assembly: " + assembly);
        const klass = wrapped_c_functions.mono_wasm_assembly_find_class(asm, namespace, classname);
        if (!klass)
            throw new Error("Could not find class: " + namespace + ":" + classname + " in assembly " + assembly);
        const method = wrapped_c_functions.mono_wasm_assembly_find_method(klass, methodname, -1);
        if (!method)
            throw new Error("Could not find method: " + methodname);
        return method;
    }
    function mono_method_get_call_signature_ref(method, mono_obj) {
        return legacyManagedExports._get_call_sig_ref(method, mono_obj ? mono_obj.address : legacyHelpers._null_root.address);
    }

    // Licensed to the .NET Foundation under one or more agreements.
    let spread_timers_maximum = 0;
    let isChromium = false;
    let pump_count = 0;
    if (globalThis.navigator) {
        const nav = globalThis.navigator;
        if (nav.userAgentData && nav.userAgentData.brands) {
            isChromium = nav.userAgentData.brands.some((i) => i.brand == "Chromium");
        }
        else if (nav.userAgent) {
            isChromium = nav.userAgent.includes("Chrome");
        }
    }
    function pump_message() {
        while (pump_count > 0) {
            --pump_count;
            wrapped_c_functions.mono_background_exec();
        }
    }
    function prevent_timer_throttling() {
        if (!isChromium) {
            return;
        }
        // this will schedule timers every second for next 6 minutes, it should be called from WebSocket event, to make it work
        // on next call, it would only extend the timers to cover yet uncovered future
        const now = new Date().valueOf();
        const desired_reach_time = now + (1000 * 60 * 6);
        const next_reach_time = Math.max(now + 1000, spread_timers_maximum);
        const light_throttling_frequency = 1000;
        for (let schedule = next_reach_time; schedule < desired_reach_time; schedule += light_throttling_frequency) {
            const delay = schedule - now;
            setTimeout(() => {
                wrapped_c_functions.mono_set_timeout_exec();
                pump_count++;
                pump_message();
            }, delay);
        }
        spread_timers_maximum = desired_reach_time;
    }
    function schedule_background_exec() {
        ++pump_count;
        setTimeout(pump_message, 0);
    }
    let lastScheduledTimeoutId = undefined;
    function mono_set_timeout(timeout) {
        function mono_wasm_set_timeout_exec() {
            wrapped_c_functions.mono_set_timeout_exec();
        }
        if (lastScheduledTimeoutId) {
            clearTimeout(lastScheduledTimeoutId);
            lastScheduledTimeoutId = undefined;
        }
        lastScheduledTimeoutId = setTimeout(mono_wasm_set_timeout_exec, timeout);
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    class Queue {
        constructor() {
            this.queue = [];
            this.offset = 0;
        }
        // initialise the queue and offset
        // Returns the length of the queue.
        getLength() {
            return (this.queue.length - this.offset);
        }
        // Returns true if the queue is empty, and false otherwise.
        isEmpty() {
            return (this.queue.length == 0);
        }
        /* Enqueues the specified item. The parameter is:
        *
        * item - the item to enqueue
        */
        enqueue(item) {
            this.queue.push(item);
        }
        /* Dequeues an item and returns it. If the queue is empty, the value
        * 'undefined' is returned.
        */
        dequeue() {
            // if the queue is empty, return immediately
            if (this.queue.length === 0)
                return undefined;
            // store the item at the front of the queue
            const item = this.queue[this.offset];
            // for GC's sake
            this.queue[this.offset] = null;
            // increment the offset and remove the free space if necessary
            if (++this.offset * 2 >= this.queue.length) {
                this.queue = this.queue.slice(this.offset);
                this.offset = 0;
            }
            // return the dequeued item
            return item;
        }
        /* Returns the item at the front of the queue (without dequeuing it). If the
         * queue is empty then undefined is returned.
         */
        peek() {
            return (this.queue.length > 0 ? this.queue[this.offset] : undefined);
        }
        drain(onEach) {
            while (this.getLength()) {
                const item = this.dequeue();
                onEach(item);
            }
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const wasm_ws_pending_send_buffer = Symbol.for("wasm ws_pending_send_buffer");
    const wasm_ws_pending_send_buffer_offset = Symbol.for("wasm ws_pending_send_buffer_offset");
    const wasm_ws_pending_send_buffer_type = Symbol.for("wasm ws_pending_send_buffer_type");
    const wasm_ws_pending_receive_event_queue = Symbol.for("wasm ws_pending_receive_event_queue");
    const wasm_ws_pending_receive_promise_queue = Symbol.for("wasm ws_pending_receive_promise_queue");
    const wasm_ws_pending_open_promise = Symbol.for("wasm ws_pending_open_promise");
    const wasm_ws_pending_close_promises = Symbol.for("wasm ws_pending_close_promises");
    const wasm_ws_pending_send_promises = Symbol.for("wasm ws_pending_send_promises");
    const wasm_ws_is_aborted = Symbol.for("wasm ws_is_aborted");
    const wasm_ws_receive_status_ptr = Symbol.for("wasm ws_receive_status_ptr");
    let mono_wasm_web_socket_close_warning = false;
    let _text_decoder_utf8 = undefined;
    let _text_encoder_utf8 = undefined;
    const ws_send_buffer_blocking_threshold = 65536;
    const emptyBuffer = new Uint8Array();
    function ws_wasm_create(uri, sub_protocols, receive_status_ptr, onClosed) {
        if (!(uri && typeof uri === "string")) throw new Error(`Assert failed: ERR12: Invalid uri ${typeof uri}`); // inlined mono_assert
        const ws = new globalThis.WebSocket(uri, sub_protocols || undefined);
        const { promise_control: open_promise_control } = createPromiseController();
        ws[wasm_ws_pending_receive_event_queue] = new Queue();
        ws[wasm_ws_pending_receive_promise_queue] = new Queue();
        ws[wasm_ws_pending_open_promise] = open_promise_control;
        ws[wasm_ws_pending_send_promises] = [];
        ws[wasm_ws_pending_close_promises] = [];
        ws[wasm_ws_receive_status_ptr] = receive_status_ptr;
        ws.binaryType = "arraybuffer";
        const local_on_open = () => {
            if (ws[wasm_ws_is_aborted])
                return;
            open_promise_control.resolve(ws);
            prevent_timer_throttling();
        };
        const local_on_message = (ev) => {
            if (ws[wasm_ws_is_aborted])
                return;
            _mono_wasm_web_socket_on_message(ws, ev);
            prevent_timer_throttling();
        };
        const local_on_close = (ev) => {
            ws.removeEventListener("message", local_on_message);
            if (ws[wasm_ws_is_aborted])
                return;
            if (onClosed)
                onClosed(ev.code, ev.reason);
            // this reject would not do anything if there was already "open" before it.
            open_promise_control.reject(ev.reason);
            for (const close_promise_control of ws[wasm_ws_pending_close_promises]) {
                close_promise_control.resolve();
            }
            // send close to any pending receivers, to wake them
            const receive_promise_queue = ws[wasm_ws_pending_receive_promise_queue];
            receive_promise_queue.drain((receive_promise_control) => {
                setI32(receive_status_ptr, 0); // count
                setI32(receive_status_ptr + 4, 2); // type:close
                setI32(receive_status_ptr + 8, 1); // end_of_message: true
                receive_promise_control.resolve();
            });
        };
        const local_on_error = (ev) => {
            open_promise_control.reject(ev.message || "WebSocket error");
        };
        ws.addEventListener("message", local_on_message);
        ws.addEventListener("open", local_on_open, { once: true });
        ws.addEventListener("close", local_on_close, { once: true });
        ws.addEventListener("error", local_on_error, { once: true });
        return ws;
    }
    function ws_wasm_open(ws) {
        if (!(!!ws)) throw new Error("Assert failed: ERR17: expected ws instance"); // inlined mono_assert
        const open_promise_control = ws[wasm_ws_pending_open_promise];
        return open_promise_control.promise;
    }
    function ws_wasm_send(ws, buffer_ptr, buffer_length, message_type, end_of_message) {
        if (!(!!ws)) throw new Error("Assert failed: ERR17: expected ws instance"); // inlined mono_assert
        const buffer_view = new Uint8Array(Module.HEAPU8.buffer, buffer_ptr, buffer_length);
        const whole_buffer = _mono_wasm_web_socket_send_buffering(ws, buffer_view, message_type, end_of_message);
        if (!end_of_message || !whole_buffer) {
            return null;
        }
        return _mono_wasm_web_socket_send_and_wait(ws, whole_buffer);
    }
    function ws_wasm_receive(ws, buffer_ptr, buffer_length) {
        if (!(!!ws)) throw new Error("Assert failed: ERR18: expected ws instance"); // inlined mono_assert
        const receive_event_queue = ws[wasm_ws_pending_receive_event_queue];
        const receive_promise_queue = ws[wasm_ws_pending_receive_promise_queue];
        const readyState = ws.readyState;
        if (readyState != WebSocket.OPEN && readyState != WebSocket.CLOSING) {
            throw new Error("InvalidState: The WebSocket is not connected.");
        }
        if (receive_event_queue.getLength()) {
            if (!(receive_promise_queue.getLength() == 0)) throw new Error("Assert failed: ERR20: Invalid WS state"); // inlined mono_assert
            // finish synchronously
            _mono_wasm_web_socket_receive_buffering(ws, receive_event_queue, buffer_ptr, buffer_length);
            return null;
        }
        const { promise, promise_control } = createPromiseController();
        const receive_promise_control = promise_control;
        receive_promise_control.buffer_ptr = buffer_ptr;
        receive_promise_control.buffer_length = buffer_length;
        receive_promise_queue.enqueue(receive_promise_control);
        return promise;
    }
    function ws_wasm_close(ws, code, reason, wait_for_close_received) {
        if (!(!!ws)) throw new Error("Assert failed: ERR19: expected ws instance"); // inlined mono_assert
        if (ws.readyState == WebSocket.CLOSED) {
            return null;
        }
        if (wait_for_close_received) {
            const { promise, promise_control } = createPromiseController();
            ws[wasm_ws_pending_close_promises].push(promise_control);
            if (typeof reason === "string") {
                ws.close(code, reason);
            }
            else {
                ws.close(code);
            }
            return promise;
        }
        else {
            if (!mono_wasm_web_socket_close_warning) {
                mono_wasm_web_socket_close_warning = true;
                console.warn("WARNING: Web browsers do not support closing the output side of a WebSocket. CloseOutputAsync has closed the socket and discarded any incoming messages.");
            }
            if (typeof reason === "string") {
                ws.close(code, reason);
            }
            else {
                ws.close(code);
            }
            return null;
        }
    }
    function ws_wasm_abort(ws) {
        if (!(!!ws)) throw new Error("Assert failed: ERR18: expected ws instance"); // inlined mono_assert
        ws[wasm_ws_is_aborted] = true;
        const open_promise_control = ws[wasm_ws_pending_open_promise];
        if (open_promise_control) {
            open_promise_control.reject("OperationCanceledException");
        }
        for (const close_promise_control of ws[wasm_ws_pending_close_promises]) {
            close_promise_control.reject("OperationCanceledException");
        }
        for (const send_promise_control of ws[wasm_ws_pending_send_promises]) {
            send_promise_control.reject("OperationCanceledException");
        }
        ws[wasm_ws_pending_receive_promise_queue].drain(receive_promise_control => {
            receive_promise_control.reject("OperationCanceledException");
        });
        // this is different from Managed implementation
        ws.close(1000, "Connection was aborted.");
    }
    // send and return promise
    function _mono_wasm_web_socket_send_and_wait(ws, buffer_view) {
        ws.send(buffer_view);
        ws[wasm_ws_pending_send_buffer] = null;
        // if the remaining send buffer is small, we don't block so that the throughput doesn't suffer.
        // Otherwise we block so that we apply some backpresure to the application sending large data.
        // this is different from Managed implementation
        if (ws.bufferedAmount < ws_send_buffer_blocking_threshold) {
            return null;
        }
        // block the promise/task until the browser passed the buffer to OS
        const { promise, promise_control } = createPromiseController();
        const pending = ws[wasm_ws_pending_send_promises];
        pending.push(promise_control);
        let nextDelay = 1;
        const polling_check = () => {
            // was it all sent yet ?
            if (ws.bufferedAmount === 0) {
                promise_control.resolve();
            }
            else if (ws.readyState != WebSocket.OPEN) {
                // only reject if the data were not sent
                // bufferedAmount does not reset to zero once the connection closes
                promise_control.reject("InvalidState: The WebSocket is not connected.");
            }
            else if (!promise_control.isDone) {
                globalThis.setTimeout(polling_check, nextDelay);
                // exponentially longer delays, up to 1000ms
                nextDelay = Math.min(nextDelay * 1.5, 1000);
                return;
            }
            // remove from pending
            const index = pending.indexOf(promise_control);
            if (index > -1) {
                pending.splice(index, 1);
            }
        };
        globalThis.setTimeout(polling_check, 0);
        return promise;
    }
    function _mono_wasm_web_socket_on_message(ws, event) {
        const event_queue = ws[wasm_ws_pending_receive_event_queue];
        const promise_queue = ws[wasm_ws_pending_receive_promise_queue];
        if (typeof event.data === "string") {
            if (_text_encoder_utf8 === undefined) {
                _text_encoder_utf8 = new TextEncoder();
            }
            event_queue.enqueue({
                type: 0,
                // according to the spec https://encoding.spec.whatwg.org/
                // - Unpaired surrogates will get replaced with 0xFFFD
                // - utf8 encode specifically is defined to never throw
                data: _text_encoder_utf8.encode(event.data),
                offset: 0
            });
        }
        else {
            if (event.data.constructor.name !== "ArrayBuffer") {
                throw new Error("ERR19: WebSocket receive expected ArrayBuffer");
            }
            event_queue.enqueue({
                type: 1,
                data: new Uint8Array(event.data),
                offset: 0
            });
        }
        if (promise_queue.getLength() && event_queue.getLength() > 1) {
            throw new Error("ERR21: Invalid WS state"); // assert
        }
        while (promise_queue.getLength() && event_queue.getLength()) {
            const promise_control = promise_queue.dequeue();
            _mono_wasm_web_socket_receive_buffering(ws, event_queue, promise_control.buffer_ptr, promise_control.buffer_length);
            promise_control.resolve();
        }
        prevent_timer_throttling();
    }
    function _mono_wasm_web_socket_receive_buffering(ws, event_queue, buffer_ptr, buffer_length) {
        const event = event_queue.peek();
        const count = Math.min(buffer_length, event.data.length - event.offset);
        if (count > 0) {
            const sourceView = event.data.subarray(event.offset, event.offset + count);
            const bufferView = new Uint8Array(Module.HEAPU8.buffer, buffer_ptr, buffer_length);
            bufferView.set(sourceView, 0);
            event.offset += count;
        }
        const end_of_message = event.data.length === event.offset ? 1 : 0;
        if (end_of_message) {
            event_queue.dequeue();
        }
        const response_ptr = ws[wasm_ws_receive_status_ptr];
        setI32(response_ptr, count);
        setI32(response_ptr + 4, event.type);
        setI32(response_ptr + 8, end_of_message);
    }
    function _mono_wasm_web_socket_send_buffering(ws, buffer_view, message_type, end_of_message) {
        let buffer = ws[wasm_ws_pending_send_buffer];
        let offset = 0;
        const length = buffer_view.byteLength;
        if (buffer) {
            offset = ws[wasm_ws_pending_send_buffer_offset];
            // match desktop WebSocket behavior by copying message_type of the first part
            message_type = ws[wasm_ws_pending_send_buffer_type];
            // if not empty message, append to existing buffer
            if (length !== 0) {
                if (offset + length > buffer.length) {
                    const newbuffer = new Uint8Array((offset + length + 50) * 1.5); // exponential growth
                    newbuffer.set(buffer, 0); // copy previous buffer
                    newbuffer.subarray(offset).set(buffer_view); // append copy at the end
                    ws[wasm_ws_pending_send_buffer] = buffer = newbuffer;
                }
                else {
                    buffer.subarray(offset).set(buffer_view); // append copy at the end
                }
                offset += length;
                ws[wasm_ws_pending_send_buffer_offset] = offset;
            }
        }
        else if (!end_of_message) {
            // create new buffer
            if (length !== 0) {
                buffer = buffer_view.slice(); // copy
                offset = length;
                ws[wasm_ws_pending_send_buffer_offset] = offset;
                ws[wasm_ws_pending_send_buffer] = buffer;
            }
            ws[wasm_ws_pending_send_buffer_type] = message_type;
        }
        else {
            if (length !== 0) {
                // we could use the un-pinned view, because it will be immediately used in ws.send()
                buffer = buffer_view;
                offset = length;
            }
        }
        // buffer was updated, do we need to trim and convert it to final format ?
        if (end_of_message) {
            if (offset == 0 || buffer == null) {
                return emptyBuffer;
            }
            if (message_type === 0) {
                // text, convert from UTF-8 bytes to string, because of bad browser API
                if (_text_decoder_utf8 === undefined) {
                    // we do not validate outgoing data https://github.com/dotnet/runtime/issues/59214
                    _text_decoder_utf8 = new TextDecoder("utf-8", { fatal: false });
                }
                // See https://github.com/whatwg/encoding/issues/172
                const bytes = typeof SharedArrayBuffer !== "undefined" && buffer instanceof SharedArrayBuffer
                    ? buffer.slice(0, offset)
                    : buffer.subarray(0, offset);
                return _text_decoder_utf8.decode(bytes);
            }
            else {
                // binary, view to used part of the buffer
                return buffer.subarray(0, offset);
            }
        }
        return null;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    /// Make a promise that resolves after a given number of milliseconds.
    function delay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const allAssetsInMemory = createPromiseController();
    const allDownloadsQueued = createPromiseController();
    let actual_downloaded_assets_count = 0;
    let actual_instantiated_assets_count = 0;
    let expected_downloaded_assets_count = 0;
    let expected_instantiated_assets_count = 0;
    const loaded_files = [];
    const loaded_assets = Object.create(null);
    // in order to prevent net::ERR_INSUFFICIENT_RESOURCES if we start downloading too many files at same time
    let parallel_count = 0;
    let throttlingPromise;
    // don't `fetch` javaScript files
    const skipDownloadsByAssetTypes = {
        "js-module-threads": true,
    };
    // `response.arrayBuffer()` can't be called twice. Some usecases are calling it on response in the instantiation.
    const skipBufferByAssetTypes = {
        "dotnetwasm": true,
    };
    // these assets are instantiated differently than the main flow
    const skipInstantiateByAssetTypes = {
        "js-module-threads": true,
        "dotnetwasm": true,
    };
    function resolve_asset_path(behavior) {
        var _a;
        const asset = (_a = runtimeHelpers.config.assets) === null || _a === void 0 ? void 0 : _a.find(a => a.behavior == behavior);
        if (!(asset)) throw new Error(`Assert failed: Can't find asset for ${behavior}`); // inlined mono_assert
        if (!asset.resolvedUrl) {
            asset.resolvedUrl = resolve_path(asset, "");
        }
        return asset;
    }
    async function mono_download_assets() {
        if (runtimeHelpers.diagnosticTracing)
            console.debug("MONO_WASM: mono_download_assets");
        runtimeHelpers.maxParallelDownloads = runtimeHelpers.config.maxParallelDownloads || runtimeHelpers.maxParallelDownloads;
        try {
            const promises_of_assets_with_buffer = [];
            // start fetching and instantiating all assets in parallel
            for (const a of runtimeHelpers.config.assets) {
                const asset = a;
                if (!skipInstantiateByAssetTypes[asset.behavior]) {
                    expected_instantiated_assets_count++;
                }
                if (!skipDownloadsByAssetTypes[asset.behavior]) {
                    const headersOnly = skipBufferByAssetTypes[asset.behavior]; // `response.arrayBuffer()` can't be called twice. Some usecases are calling it on response in the instantiation.
                    expected_downloaded_assets_count++;
                    if (asset.pendingDownload) {
                        asset.pendingDownloadInternal = asset.pendingDownload;
                        const waitForExternalData = async () => {
                            const response = await asset.pendingDownloadInternal.response;
                            ++actual_downloaded_assets_count;
                            if (!headersOnly) {
                                asset.buffer = await response.arrayBuffer();
                            }
                            return { asset, buffer: asset.buffer };
                        };
                        promises_of_assets_with_buffer.push(waitForExternalData());
                    }
                    else {
                        const waitForExternalData = async () => {
                            asset.buffer = await start_asset_download_with_retries(asset, !headersOnly);
                            return { asset, buffer: asset.buffer };
                        };
                        promises_of_assets_with_buffer.push(waitForExternalData());
                    }
                }
            }
            allDownloadsQueued.promise_control.resolve();
            const promises_of_asset_instantiation = [];
            for (const downloadPromise of promises_of_assets_with_buffer) {
                promises_of_asset_instantiation.push((async () => {
                    const assetWithBuffer = await downloadPromise;
                    const asset = assetWithBuffer.asset;
                    if (assetWithBuffer.buffer) {
                        if (!skipInstantiateByAssetTypes[asset.behavior]) {
                            const url = asset.pendingDownloadInternal.url;
                            const data = new Uint8Array(asset.buffer);
                            asset.pendingDownloadInternal = null; // GC
                            asset.pendingDownload = null; // GC
                            asset.buffer = null; // GC
                            assetWithBuffer.buffer = null; // GC
                            await beforeOnRuntimeInitialized.promise;
                            // this is after onRuntimeInitialized
                            _instantiate_asset(asset, url, data);
                        }
                    }
                    else {
                        const headersOnly = skipBufferByAssetTypes[asset.behavior];
                        if (!headersOnly) {
                            if (!(asset.isOptional)) throw new Error("Assert failed: Expected asset to have the downloaded buffer"); // inlined mono_assert
                            if (!skipDownloadsByAssetTypes[asset.behavior]) {
                                expected_downloaded_assets_count--;
                            }
                            if (!skipInstantiateByAssetTypes[asset.behavior]) {
                                expected_instantiated_assets_count--;
                            }
                        }
                    }
                })());
            }
            // this await will get past the onRuntimeInitialized because we are not blocking via addRunDependency
            // and we are not awating it here
            Promise.all(promises_of_asset_instantiation).then(() => {
                allAssetsInMemory.promise_control.resolve();
            }).catch(err => {
                Module.printErr("MONO_WASM: Error in mono_download_assets: " + err);
                abort_startup(err, true);
            });
            // OPTIMIZATION explained:
            // we do it this way so that we could allocate memory immediately after asset is downloaded (and after onRuntimeInitialized which happened already)
            // spreading in time
            // rather than to block all downloads after onRuntimeInitialized or block onRuntimeInitialized after all downloads are done. That would create allocation burst.
        }
        catch (err) {
            Module.printErr("MONO_WASM: Error in mono_download_assets: " + err);
            throw err;
        }
    }
    // FIXME: Connection reset is probably the only good one for which we should retry
    async function start_asset_download_with_retries(asset, downloadData) {
        try {
            return await start_asset_download_with_throttle(asset, downloadData);
        }
        catch (err) {
            if (ENVIRONMENT_IS_SHELL || ENVIRONMENT_IS_NODE) {
                // we will not re-try on shell
                throw err;
            }
            if (asset.pendingDownload && asset.pendingDownloadInternal == asset.pendingDownload) {
                // we will not re-try with external source
                throw err;
            }
            if (asset.resolvedUrl && asset.resolvedUrl.indexOf("file://") != -1) {
                // we will not re-try with local file
                throw err;
            }
            if (err && err.status == 404) {
                // we will not re-try with 404
                throw err;
            }
            asset.pendingDownloadInternal = undefined;
            // second attempt only after all first attempts are queued
            await allDownloadsQueued.promise;
            try {
                return await start_asset_download_with_throttle(asset, downloadData);
            }
            catch (err) {
                asset.pendingDownloadInternal = undefined;
                // third attempt after small delay
                await delay(100);
                return await start_asset_download_with_throttle(asset, downloadData);
            }
        }
    }
    async function start_asset_download_with_throttle(asset, downloadData) {
        // we don't addRunDependency to allow download in parallel with onRuntimeInitialized event!
        while (throttlingPromise) {
            await throttlingPromise.promise;
        }
        try {
            ++parallel_count;
            if (parallel_count == runtimeHelpers.maxParallelDownloads) {
                if (runtimeHelpers.diagnosticTracing)
                    console.debug("MONO_WASM: Throttling further parallel downloads");
                throttlingPromise = createPromiseController();
            }
            const response = await start_asset_download_sources(asset);
            if (!downloadData || !response) {
                return undefined;
            }
            return await response.arrayBuffer();
        }
        finally {
            --parallel_count;
            if (throttlingPromise && parallel_count == runtimeHelpers.maxParallelDownloads - 1) {
                if (runtimeHelpers.diagnosticTracing)
                    console.debug("MONO_WASM: Resuming more parallel downloads");
                const old_throttling = throttlingPromise;
                throttlingPromise = undefined;
                old_throttling.promise_control.resolve();
            }
        }
    }
    async function start_asset_download_sources(asset) {
        // we don't addRunDependency to allow download in parallel with onRuntimeInitialized event!
        if (asset.buffer) {
            const buffer = asset.buffer;
            asset.buffer = null; // GC
            asset.pendingDownloadInternal = {
                url: "undefined://" + asset.name,
                name: asset.name,
                response: Promise.resolve({
                    arrayBuffer: () => buffer,
                    headers: {
                        get: () => undefined,
                    }
                })
            };
            ++actual_downloaded_assets_count;
            return asset.pendingDownloadInternal.response;
        }
        if (asset.pendingDownloadInternal && asset.pendingDownloadInternal.response) {
            const response = await asset.pendingDownloadInternal.response;
            return response;
        }
        const sourcesList = asset.loadRemote && runtimeHelpers.config.remoteSources ? runtimeHelpers.config.remoteSources : [""];
        let response = undefined;
        for (let sourcePrefix of sourcesList) {
            sourcePrefix = sourcePrefix.trim();
            // HACK: Special-case because MSBuild doesn't allow "" as an attribute
            if (sourcePrefix === "./")
                sourcePrefix = "";
            const attemptUrl = resolve_path(asset, sourcePrefix);
            if (asset.name === attemptUrl) {
                if (runtimeHelpers.diagnosticTracing)
                    console.debug(`MONO_WASM: Attempting to download '${attemptUrl}'`);
            }
            else {
                if (runtimeHelpers.diagnosticTracing)
                    console.debug(`MONO_WASM: Attempting to download '${attemptUrl}' for ${asset.name}`);
            }
            try {
                const loadingResource = download_resource({
                    name: asset.name,
                    resolvedUrl: attemptUrl,
                    hash: asset.hash,
                    behavior: asset.behavior
                });
                asset.pendingDownloadInternal = loadingResource;
                response = await loadingResource.response;
                if (!response.ok) {
                    continue; // next source
                }
                ++actual_downloaded_assets_count;
                return response;
            }
            catch (err) {
                continue; //next source
            }
        }
        const isOkToFail = asset.isOptional || (asset.name.match(/\.pdb$/) && runtimeHelpers.config.ignorePdbLoadErrors);
        if (!(response)) throw new Error(`Assert failed: Response undefined ${asset.name}`); // inlined mono_assert
        if (!isOkToFail) {
            const err = new Error(`MONO_WASM: download '${response.url}' for ${asset.name} failed ${response.status} ${response.statusText}`);
            err.status = response.status;
            throw err;
        }
        else {
            Module.print(`MONO_WASM: optional download '${response.url}' for ${asset.name} failed ${response.status} ${response.statusText}`);
            return undefined;
        }
    }
    function resolve_path(asset, sourcePrefix) {
        if (!(sourcePrefix !== null && sourcePrefix !== undefined)) throw new Error(`Assert failed: sourcePrefix must be provided for ${asset.name}`); // inlined mono_assert
        let attemptUrl;
        const assemblyRootFolder = runtimeHelpers.config.assemblyRootFolder;
        if (!asset.resolvedUrl) {
            if (sourcePrefix === "") {
                if (asset.behavior === "assembly" || asset.behavior === "pdb") {
                    attemptUrl = assemblyRootFolder
                        ? (assemblyRootFolder + "/" + asset.name)
                        : asset.name;
                }
                else if (asset.behavior === "resource") {
                    const path = asset.culture !== "" ? `${asset.culture}/${asset.name}` : asset.name;
                    attemptUrl = assemblyRootFolder
                        ? (assemblyRootFolder + "/" + path)
                        : path;
                }
                else {
                    attemptUrl = asset.name;
                }
            }
            else {
                attemptUrl = sourcePrefix + asset.name;
            }
            attemptUrl = runtimeHelpers.locateFile(attemptUrl);
        }
        else {
            attemptUrl = asset.resolvedUrl;
        }
        if (!(attemptUrl && typeof attemptUrl == "string")) throw new Error("Assert failed: attemptUrl need to be path or url string"); // inlined mono_assert
        return attemptUrl;
    }
    function download_resource(request) {
        try {
            if (typeof Module.downloadResource === "function") {
                const loading = Module.downloadResource(request);
                if (loading)
                    return loading;
            }
            const options = {};
            if (request.hash) {
                options.integrity = request.hash;
            }
            const response = runtimeHelpers.fetch_like(request.resolvedUrl, options);
            return {
                name: request.name, url: request.resolvedUrl, response
            };
        }
        catch (err) {
            const response = {
                ok: false,
                url: request.resolvedUrl,
                status: 500,
                statusText: "ERR29: " + err,
                arrayBuffer: () => { throw err; },
                json: () => { throw err; }
            };
            return {
                name: request.name, url: request.resolvedUrl, response: Promise.resolve(response)
            };
        }
    }
    // this need to be run only after onRuntimeInitialized event, when the memory is ready
    function _instantiate_asset(asset, url, bytes) {
        if (runtimeHelpers.diagnosticTracing)
            console.debug(`MONO_WASM: Loaded:${asset.name} as ${asset.behavior} size ${bytes.length} from ${url}`);
        const virtualName = typeof (asset.virtualPath) === "string"
            ? asset.virtualPath
            : asset.name;
        let offset = null;
        switch (asset.behavior) {
            case "dotnetwasm":
            case "js-module-threads":
                // do nothing
                break;
            case "resource":
            case "assembly":
            case "pdb":
                loaded_files.push({ url: url, file: virtualName });
            // falls through
            case "heap":
            case "icu":
                offset = mono_wasm_load_bytes_into_heap(bytes);
                loaded_assets[virtualName] = [offset, bytes.length];
                break;
            case "vfs": {
                // FIXME
                const lastSlash = virtualName.lastIndexOf("/");
                let parentDirectory = (lastSlash > 0)
                    ? virtualName.substr(0, lastSlash)
                    : null;
                let fileName = (lastSlash > 0)
                    ? virtualName.substr(lastSlash + 1)
                    : virtualName;
                if (fileName.startsWith("/"))
                    fileName = fileName.substr(1);
                if (parentDirectory) {
                    if (runtimeHelpers.diagnosticTracing)
                        console.debug(`MONO_WASM: Creating directory '${parentDirectory}'`);
                    Module.FS_createPath("/", parentDirectory, true, true // fixme: should canWrite be false?
                    );
                }
                else {
                    parentDirectory = "/";
                }
                if (runtimeHelpers.diagnosticTracing)
                    console.debug(`MONO_WASM: Creating file '${fileName}' in directory '${parentDirectory}'`);
                if (!mono_wasm_load_data_archive(bytes, parentDirectory)) {
                    Module.FS_createDataFile(parentDirectory, fileName, bytes, true /* canRead */, true /* canWrite */, true /* canOwn */);
                }
                break;
            }
            default:
                throw new Error(`Unrecognized asset behavior:${asset.behavior}, for asset ${asset.name}`);
        }
        if (asset.behavior === "assembly") {
            // this is reading flag inside the DLL about the existence of PDB
            // it doesn't relate to whether the .pdb file is downloaded at all
            const hasPpdb = wrapped_c_functions.mono_wasm_add_assembly(virtualName, offset, bytes.length);
            if (!hasPpdb) {
                const index = loaded_files.findIndex(element => element.file == virtualName);
                loaded_files.splice(index, 1);
            }
        }
        else if (asset.behavior === "icu") {
            if (!mono_wasm_load_icu_data(offset))
                Module.printErr(`MONO_WASM: Error loading ICU asset ${asset.name}`);
        }
        else if (asset.behavior === "resource") {
            wrapped_c_functions.mono_wasm_add_satellite_assembly(virtualName, asset.culture, offset, bytes.length);
        }
        ++actual_instantiated_assets_count;
    }
    async function instantiate_wasm_asset(pendingAsset, wasmModuleImports, successCallback) {
        if (!(pendingAsset && pendingAsset.pendingDownloadInternal)) throw new Error("Assert failed: Can't load dotnet.wasm"); // inlined mono_assert
        const response = await pendingAsset.pendingDownloadInternal.response;
        const contentType = response.headers ? response.headers.get("Content-Type") : undefined;
        let compiledInstance;
        let compiledModule;
        if (typeof WebAssembly.instantiateStreaming === "function" && contentType === "application/wasm") {
            if (runtimeHelpers.diagnosticTracing)
                console.debug("MONO_WASM: instantiate_wasm_module streaming");
            const streamingResult = await WebAssembly.instantiateStreaming(response, wasmModuleImports);
            compiledInstance = streamingResult.instance;
            compiledModule = streamingResult.module;
        }
        else {
            if (ENVIRONMENT_IS_WEB && contentType !== "application/wasm") {
                console.warn("MONO_WASM: WebAssembly resource does not have the expected content type \"application/wasm\", so falling back to slower ArrayBuffer instantiation.");
            }
            const arrayBuffer = await response.arrayBuffer();
            if (runtimeHelpers.diagnosticTracing)
                console.debug("MONO_WASM: instantiate_wasm_module buffered");
            const arrayBufferResult = await WebAssembly.instantiate(arrayBuffer, wasmModuleImports);
            compiledInstance = arrayBufferResult.instance;
            compiledModule = arrayBufferResult.module;
        }
        successCallback(compiledInstance, compiledModule);
    }
    // used from Blazor
    function mono_wasm_load_data_archive(data, prefix) {
        if (data.length < 8)
            return false;
        const dataview = new DataView(data.buffer);
        const magic = dataview.getUint32(0, true);
        //    get magic number
        if (magic != 0x626c6174) {
            return false;
        }
        const manifestSize = dataview.getUint32(4, true);
        if (manifestSize == 0 || data.length < manifestSize + 8)
            return false;
        let manifest;
        try {
            const manifestContent = Module.UTF8ArrayToString(data, 8, manifestSize);
            manifest = JSON.parse(manifestContent);
            if (!(manifest instanceof Array))
                return false;
        }
        catch (exc) {
            return false;
        }
        data = data.slice(manifestSize + 8);
        // Create the folder structure
        // /usr/share/zoneinfo
        // /usr/share/zoneinfo/Africa
        // /usr/share/zoneinfo/Asia
        // ..
        const folders = new Set();
        manifest.filter(m => {
            const file = m[0];
            const last = file.lastIndexOf("/");
            const directory = file.slice(0, last + 1);
            folders.add(directory);
        });
        folders.forEach(folder => {
            Module["FS_createPath"](prefix, folder, true, true);
        });
        for (const row of manifest) {
            const name = row[0];
            const length = row[1];
            const bytes = data.slice(0, length);
            Module["FS_createDataFile"](prefix, name, bytes, true, true);
            data = data.slice(length);
        }
        return true;
    }
    async function wait_for_all_assets() {
        // wait for all assets in memory
        await allAssetsInMemory.promise;
        if (runtimeHelpers.config.assets) {
            if (!(actual_downloaded_assets_count == expected_downloaded_assets_count)) throw new Error(`Assert failed: Expected ${expected_downloaded_assets_count} assets to be downloaded, but only finished ${actual_downloaded_assets_count}`); // inlined mono_assert
            if (!(actual_instantiated_assets_count == expected_instantiated_assets_count)) throw new Error(`Assert failed: Expected ${expected_instantiated_assets_count} assets to be in memory, but only instantiated ${actual_instantiated_assets_count}`); // inlined mono_assert
            loaded_files.forEach(value => MONO.loaded_files.push(value.url));
            if (runtimeHelpers.diagnosticTracing)
                console.debug("MONO_WASM: all assets are loaded in wasm memory");
        }
    }
    // Used by the debugger to enumerate loaded dlls and pdbs
    function mono_wasm_get_loaded_files() {
        return MONO.loaded_files;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    function export_internal() {
        return {
            // tests
            mono_wasm_exit: (exit_code) => { Module.printErr("MONO_WASM: early exit " + exit_code); },
            mono_wasm_enable_on_demand_gc: wrapped_c_functions.mono_wasm_enable_on_demand_gc,
            mono_profiler_init_aot: wrapped_c_functions.mono_profiler_init_aot,
            mono_wasm_exec_regression: wrapped_c_functions.mono_wasm_exec_regression,
            mono_method_resolve,
            mono_intern_string,
            // with mono_wasm_debugger_log and mono_wasm_trace_logger
            logging: undefined,
            mono_wasm_stringify_as_error_with_stack,
            // used in debugger DevToolsHelper.cs
            mono_wasm_get_loaded_files,
            mono_wasm_send_dbg_command_with_parms,
            mono_wasm_send_dbg_command,
            mono_wasm_get_dbg_command_info,
            mono_wasm_get_details,
            mono_wasm_release_object,
            mono_wasm_call_function_on,
            mono_wasm_debugger_resume,
            mono_wasm_detach_debugger,
            mono_wasm_raise_debug_event,
            mono_wasm_change_debugger_log_level,
            mono_wasm_debugger_attached,
            mono_wasm_runtime_is_ready: runtimeHelpers.mono_wasm_runtime_is_ready,
            // interop
            get_property,
            set_property,
            has_property,
            get_typeof_property,
            get_global_this,
            get_dotnet_instance: () => exportedRuntimeAPI,
            dynamic_import,
            // BrowserWebSocket
            mono_wasm_cancel_promise,
            ws_wasm_create,
            ws_wasm_open,
            ws_wasm_send,
            ws_wasm_receive,
            ws_wasm_close,
            ws_wasm_abort,
            // BrowserHttpHandler
            http_wasm_supports_streaming_response,
            http_wasm_create_abort_controler,
            http_wasm_abort_request,
            http_wasm_abort_response,
            http_wasm_fetch,
            http_wasm_fetch_bytes,
            http_wasm_get_response_header_names,
            http_wasm_get_response_header_values,
            http_wasm_get_response_bytes,
            http_wasm_get_response_length,
            http_wasm_get_streamed_response_bytes,
        };
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function cwraps_internal(internal) {
        Object.assign(internal, {
            mono_wasm_exit: wrapped_c_functions.mono_wasm_exit,
            mono_wasm_enable_on_demand_gc: wrapped_c_functions.mono_wasm_enable_on_demand_gc,
            mono_profiler_init_aot: wrapped_c_functions.mono_profiler_init_aot,
            mono_wasm_exec_regression: wrapped_c_functions.mono_wasm_exec_regression,
        });
    }

    // Licensed to the .NET Foundation under one or more agreements.
    function export_mono_api() {
        return {
            // legacy MONO API
            mono_wasm_setenv,
            mono_wasm_load_bytes_into_heap,
            mono_wasm_load_icu_data,
            mono_wasm_runtime_ready,
            mono_wasm_load_data_archive,
            mono_wasm_load_config,
            mono_load_runtime_and_bcl_args,
            mono_wasm_new_root_buffer,
            mono_wasm_new_root,
            mono_wasm_new_external_root,
            mono_wasm_release_roots,
            mono_run_main,
            mono_run_main_and_exit,
            // for Blazor's future!
            mono_wasm_add_assembly: null,
            mono_wasm_load_runtime,
            config: runtimeHelpers.config,
            loaded_files: [],
            // memory accessors
            setB32,
            setI8,
            setI16,
            setI32,
            setI52,
            setU52,
            setI64Big,
            setU8,
            setU16,
            setU32,
            setF32,
            setF64,
            getB32,
            getI8,
            getI16,
            getI32,
            getI52,
            getU52,
            getI64Big,
            getU8,
            getU16,
            getU32,
            getF32,
            getF64,
        };
    }
    function cwraps_mono_api(mono) {
        Object.assign(mono, {
            mono_wasm_add_assembly: wrapped_c_functions.mono_wasm_add_assembly,
        });
    }
    function export_binding_api() {
        return {
            // legacy BINDING API
            bind_static_method: mono_bind_static_method,
            call_assembly_entry_point: mono_call_assembly_entry_point,
            mono_obj_array_new: null,
            mono_obj_array_set: null,
            js_string_to_mono_string,
            js_typed_array_to_array,
            mono_array_to_js_array,
            js_to_mono_obj,
            conv_string,
            unbox_mono_obj,
            mono_obj_array_new_ref: null,
            mono_obj_array_set_ref: null,
            js_string_to_mono_string_root,
            js_typed_array_to_array_root,
            js_to_mono_obj_root,
            conv_string_root,
            unbox_mono_obj_root,
            mono_array_root_to_js_array,
        };
    }
    function cwraps_binding_api(binding) {
        Object.assign(binding, {
            mono_obj_array_new: wrapped_c_functions.mono_wasm_obj_array_new,
            mono_obj_array_set: wrapped_c_functions.mono_wasm_obj_array_set,
            mono_obj_array_new_ref: wrapped_c_functions.mono_wasm_obj_array_new_ref,
            mono_obj_array_set_ref: wrapped_c_functions.mono_wasm_obj_array_set_ref,
        });
    }

    // Licensed to the .NET Foundation under one or more agreements.
    function isRunningPThreadWorker(w) {
        return w.pthread !== undefined;
    }
    /// These utility functions dig into Emscripten internals
    const Internals = {
        get modulePThread() {
            return Module.PThread;
        },
        getWorker: (pthread_ptr) => {
            var _a;
            // see https://github.com/emscripten-core/emscripten/pull/16239
            return (_a = Internals.modulePThread.pthreads[pthread_ptr]) === null || _a === void 0 ? void 0 : _a.worker;
        },
        getThreadId: (worker) => {
            /// See library_pthread.js in Emscripten.
            /// They hang a "pthread" object from the worker if the worker is running a thread, and remove it when the thread stops by doing `pthread_exit` or when it's joined using `pthread_join`.
            if (!isRunningPThreadWorker(worker))
                return undefined;
            const emscriptenThreadInfo = worker.pthread;
            return emscriptenThreadInfo.threadInfoStruct;
        },
        allocateUnusedWorker: () => {
            /// See library_pthread.js in Emscripten.
            /// This function allocates a new worker and adds it to the pool of workers.
            /// It's called when the pool of workers is empty and a new thread is created.
            Internals.modulePThread.allocateUnusedWorker();
        },
        getUnusedWorkerPool: () => {
            return Internals.modulePThread.unusedWorkers;
        },
        loadWasmModuleToWorker: (worker, onFinishedLoading) => {
            Internals.modulePThread.loadWasmModuleToWorker(worker, onFinishedLoading);
        }
    };

    // Licensed to the .NET Foundation under one or more agreements.
    const threads = new Map();
    class ThreadImpl {
        constructor(pthread_ptr, worker, port) {
            this.pthread_ptr = pthread_ptr;
            this.worker = worker;
            this.port = port;
        }
        postMessageToWorker(message) {
            this.port.postMessage(message);
        }
    }
    const thread_promises = new Map();
    /// wait until the thread with the given id has set up a message port to the runtime
    function waitForThread(pthread_ptr) {
        if (threads.has(pthread_ptr)) {
            return Promise.resolve(threads.get(pthread_ptr));
        }
        const promiseAndController = createPromiseController();
        const arr = thread_promises.get(pthread_ptr);
        if (arr === undefined) {
            thread_promises.set(pthread_ptr, [promiseAndController.promise_control]);
        }
        else {
            arr.push(promiseAndController.promise_control);
        }
        return promiseAndController.promise;
    }
    function resolvePromises(pthread_ptr, thread) {
        const arr = thread_promises.get(pthread_ptr);
        if (arr !== undefined) {
            arr.forEach((controller) => controller.resolve(thread));
            thread_promises.delete(pthread_ptr);
        }
    }
    function addThread(pthread_ptr, worker, port) {
        const thread = new ThreadImpl(pthread_ptr, worker, port);
        threads.set(pthread_ptr, thread);
        return thread;
    }
    function removeThread(pthread_ptr) {
        threads.delete(pthread_ptr);
    }
    /// Given a thread id, return the thread object with the worker where the thread is running, and a message port.
    function getThread(pthread_ptr) {
        const thread = threads.get(pthread_ptr);
        if (thread === undefined) {
            return undefined;
        }
        // validate that the worker is still running pthread_ptr
        const worker = thread.worker;
        if (Internals.getThreadId(worker) !== pthread_ptr) {
            removeThread(pthread_ptr);
            thread.port.close();
            return undefined;
        }
        return thread;
    }
    /// Returns all the threads we know about
    const getThreadIds = () => threads.keys();
    function monoDedicatedChannelMessageFromWorkerToMain(event, thread) {
        // TODO: add callbacks that will be called from here
        console.debug("MONO_WASM: got message from worker on the dedicated channel", event.data, thread);
    }
    // handler that runs in the main thread when a message is received from a pthread worker
    function monoWorkerMessageHandler(worker, ev) {
        /// N.B. important to ignore messages we don't recognize - Emscripten uses the message event to send internal messages
        const data = ev.data;
        if (isMonoWorkerMessageChannelCreated(data)) {
            console.debug("MONO_WASM: received the channel created message", data, worker);
            const port = data[monoSymbol].port;
            const pthread_id = data[monoSymbol].thread_id;
            const thread = addThread(pthread_id, worker, port);
            port.addEventListener("message", (ev) => monoDedicatedChannelMessageFromWorkerToMain(ev, thread));
            port.start();
            port.postMessage(makeMonoThreadMessageApplyMonoConfig(runtimeHelpers.config));
            resolvePromises(pthread_id, thread);
        }
    }
    /// Called by Emscripten internals on the browser thread when a new pthread worker is created and added to the pthread worker pool.
    /// At this point the worker doesn't have any pthread assigned to it, yet.
    function afterLoadWasmModuleToWorker(worker) {
        worker.addEventListener("message", (ev) => monoWorkerMessageHandler(worker, ev));
        console.debug("MONO_WASM: afterLoadWasmModuleToWorker added message event handler", worker);
    }
    /// We call on the main thread this during startup to pre-allocate a pool of pthread workers.
    /// At this point asset resolution needs to be working (ie we loaded MonoConfig).
    /// This is used instead of the Emscripten PThread.initMainThread because we call it later.
    function preAllocatePThreadWorkerPool(defaultPthreadPoolSize, config) {
        const poolSizeSpec = config === null || config === void 0 ? void 0 : config.pthreadPoolSize;
        let n;
        if (poolSizeSpec === undefined) {
            n = defaultPthreadPoolSize;
        }
        else {
            if (!(typeof poolSizeSpec === "number")) throw new Error("Assert failed: pthreadPoolSize must be a number"); // inlined mono_assert
            if (poolSizeSpec < 0)
                n = defaultPthreadPoolSize;
            else
                n = poolSizeSpec;
        }
        for (let i = 0; i < n; i++) {
            Internals.allocateUnusedWorker();
        }
    }
    /// We call this on the main thread during startup once we fetched WasmModule.
    /// This sends a message to each pre-allocated worker to load the WasmModule and dotnet.js and to set up
    /// message handling.
    /// This is used instead of the Emscripten "receiveInstance" in "createWasm" because that code is
    /// conditioned on a non-zero PTHREAD_POOL_SIZE (but we set it to 0 to avoid early worker allocation).
    async function instantiateWasmPThreadWorkerPool() {
        // this is largely copied from emscripten's "receiveInstance" in "createWasm" in "src/preamble.js"
        const workers = Internals.getUnusedWorkerPool();
        if (workers.length > 0) {
            const allLoaded = createPromiseController();
            let leftToLoad = workers.length;
            workers.forEach((w) => {
                Internals.loadWasmModuleToWorker(w, function () {
                    if (!--leftToLoad)
                        allLoaded.promise_control.resolve();
                });
            });
            await allLoaded.promise;
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    function isDiagnosticMessage(x) {
        return isMonoThreadMessage(x) && x.type === "diagnostic_server";
    }
    function makeDiagnosticServerControlCommand(cmd) {
        return {
            type: "diagnostic_server",
            cmd: cmd,
        };
    }

    // Licensed to the .NET Foundation under one or more agreements.
    class ServerControllerImpl {
        constructor(server) {
            this.server = server;
            server.port.addEventListener("message", this.onServerReply.bind(this));
        }
        start() {
            console.debug("MONO_WASM: signaling the diagnostic server to start");
            this.server.postMessageToWorker(makeDiagnosticServerControlCommand("start"));
        }
        stop() {
            console.debug("MONO_WASM: signaling the diagnostic server to stop");
            this.server.postMessageToWorker(makeDiagnosticServerControlCommand("stop"));
        }
        postServerAttachToRuntime() {
            console.debug("MONO_WASM: signal the diagnostic server to attach to the runtime");
            this.server.postMessageToWorker(makeDiagnosticServerControlCommand("attach_to_runtime"));
        }
        onServerReply(event) {
            const d = event.data;
            if (isDiagnosticMessage(d)) {
                switch (d.cmd) {
                    default:
                        console.warn("MONO_WASM: Unknown control reply command: ", d);
                        break;
                }
            }
        }
    }
    let serverController = null;
    function getController() {
        if (serverController)
            return serverController;
        throw new Error("unexpected no server controller");
    }
    async function startDiagnosticServer(websocket_url) {
        const sizeOfPthreadT = 4;
        console.info(`MONO_WASM: starting the diagnostic server url: ${websocket_url}`);
        const result = withStackAlloc(sizeOfPthreadT, (pthreadIdPtr) => {
            if (!wrapped_c_functions.mono_wasm_diagnostic_server_create_thread(websocket_url, pthreadIdPtr))
                return undefined;
            const pthreadId = getI32(pthreadIdPtr);
            return pthreadId;
        });
        if (result === undefined) {
            console.warn("MONO_WASM: diagnostic server failed to start");
            return null;
        }
        // have to wait until the message port is created
        const thread = await waitForThread(result);
        if (thread === undefined) {
            throw new Error("unexpected diagnostic server thread not found");
        }
        const serverControllerImpl = new ServerControllerImpl(thread);
        serverController = serverControllerImpl;
        serverControllerImpl.start();
        return serverControllerImpl;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // called from C on the main thread
    function mono_wasm_event_pipe_early_startup_callback() {
        if (MonoWasmThreads) {
            return;
        }
    }
    // Initialization flow
    ///   * The runtime calls configure_diagnostics with options from MonoConfig
    ///   * We start the diagnostic server which connects to the host and waits for some configurations (an IPC CollectTracing command)
    ///   * The host sends us the configurations and we push them onto the startup_session_configs array and let the startup resume
    ///   * The runtime calls mono_wasm_initA_diagnostics with any options from MonoConfig
    ///   * The runtime C layer calls mono_wasm_event_pipe_early_startup_callback during startup once native EventPipe code is initialized
    ///   * We start all the sessiosn in startup_session_configs and allow them to start streaming
    ///   * The IPC sessions first send an IPC message with the session ID and then they start streaming
    ////  * If the diagnostic server gets more commands it will send us a message through the serverController and we will start additional sessions
    let suspendOnStartup = false;
    let diagnosticsServerEnabled = false;
    let diagnosticsInitialized = false;
    async function mono_wasm_init_diagnostics() {
        if (diagnosticsInitialized)
            return;
        if (!MonoWasmThreads) {
            console.warn("MONO_WASM: ignoring diagnostics options because this runtime does not support diagnostics");
            return;
        }
        else {
            const options = diagnostic_options_from_environment();
            if (!options)
                return;
            diagnosticsInitialized = true;
            if (!is_nullish(options === null || options === void 0 ? void 0 : options.server)) {
                if (options.server.connectUrl === undefined || typeof (options.server.connectUrl) !== "string") {
                    throw new Error("server.connectUrl must be a string");
                }
                const url = options.server.connectUrl;
                const suspend = boolsyOption(options.server.suspend);
                const controller = await startDiagnosticServer(url);
                if (controller) {
                    diagnosticsServerEnabled = true;
                    if (suspend) {
                        suspendOnStartup = true;
                    }
                }
            }
        }
    }
    function boolsyOption(x) {
        if (x === true || x === false)
            return x;
        if (typeof x === "string") {
            if (x === "true")
                return true;
            if (x === "false")
                return false;
        }
        throw new Error(`invalid option: "${x}", should be true, false, or "true" or "false"`);
    }
    /// Parse environment variables for diagnostics configuration
    ///
    /// The environment variables are:
    ///  * DOTNET_DiagnosticPorts
    ///
    function diagnostic_options_from_environment() {
        const val = getEnv("DOTNET_DiagnosticPorts");
        if (is_nullish(val))
            return null;
        // TODO: consider also parsing the DOTNET_EnableEventPipe and DOTNET_EventPipeOutputPath, DOTNET_EvnetPipeConfig variables
        // to configure the startup sessions that will dump output to the VFS.
        return diagnostic_options_from_ports_spec(val);
    }
    /// Parse a DOTNET_DiagnosticPorts string and return a DiagnosticOptions object.
    /// See https://docs.microsoft.com/en-us/dotnet/core/diagnostics/diagnostic-port#configure-additional-diagnostic-ports
    function diagnostic_options_from_ports_spec(val) {
        if (val === "")
            return null;
        const ports = val.split(";");
        if (ports.length === 0)
            return null;
        if (ports.length !== 1) {
            console.warn("MONO_WASM: multiple diagnostic ports specified, only the last one will be used");
        }
        const portSpec = ports[ports.length - 1];
        const components = portSpec.split(",");
        if (components.length < 1 || components.length > 3) {
            console.warn("MONO_WASM: invalid diagnostic port specification, should be of the form <port>[,<connect>],[<nosuspend|suspend>]");
            return null;
        }
        const uri = components[0];
        let connect = true;
        let suspend = true;
        // the C Diagnostic Server goes through these parts in reverse, do the same here.
        for (let i = components.length - 1; i >= 1; i--) {
            const component = components[i];
            switch (component.toLowerCase()) {
                case "nosuspend":
                    suspend = false;
                    break;
                case "suspend":
                    suspend = true;
                    break;
                case "listen":
                    connect = false;
                    break;
                case "connect":
                    connect = true;
                    break;
                default:
                    console.warn(`MONO_WASM: invalid diagnostic port specification component: ${component}`);
                    break;
            }
        }
        if (!connect) {
            console.warn("MONO_WASM: this runtime does not support listening on a diagnostic port; no diagnostic server started");
            return null;
        }
        return {
            server: {
                connectUrl: uri,
                suspend: suspend,
            }
        };
    }
    function mono_wasm_diagnostic_server_on_runtime_server_init(out_options) {
        if (diagnosticsServerEnabled) {
            /* called on the main thread when the runtime is sufficiently initialized */
            const controller = getController();
            controller.postServerAttachToRuntime();
            // FIXME: is this really the best place to do this?
            setI32(out_options, suspendOnStartup ? 1 : 0);
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    let config = undefined;
    let configLoaded = false;
    let isCustomStartup = false;
    const dotnetReady = createPromiseController();
    const afterConfigLoaded = createPromiseController();
    const afterInstantiateWasm = createPromiseController();
    const beforePreInit = createPromiseController();
    const afterPreInit = createPromiseController();
    const afterPreRun = createPromiseController();
    const beforeOnRuntimeInitialized = createPromiseController();
    const afterOnRuntimeInitialized = createPromiseController();
    const afterPostRun = createPromiseController();
    // default size if MonoConfig.pthreadPoolSize is undefined
    const MONO_PTHREAD_POOL_SIZE = 4;
    // we are making emscripten startup async friendly
    // emscripten is executing the events without awaiting it and so we need to block progress via PromiseControllers above
    function configure_emscripten_startup(module, exportedAPI) {
        // these all could be overridden on DotnetModuleConfig, we are chaing them to async below, as opposed to emscripten
        // when user set configSrc or config, we are running our default startup sequence.
        const userInstantiateWasm = module.instantiateWasm;
        const userPreInit = !module.preInit ? [] : typeof module.preInit === "function" ? [module.preInit] : module.preInit;
        const userPreRun = !module.preRun ? [] : typeof module.preRun === "function" ? [module.preRun] : module.preRun;
        const userpostRun = !module.postRun ? [] : typeof module.postRun === "function" ? [module.postRun] : module.postRun;
        // eslint-disable-next-line @typescript-eslint/no-empty-function
        const userOnRuntimeInitialized = module.onRuntimeInitialized ? module.onRuntimeInitialized : () => { };
        // when assets don't contain DLLs it means this is Blazor or another custom startup
        isCustomStartup = !module.configSrc && (!module.config || !module.config.assets || module.config.assets.findIndex(a => a.behavior === "assembly") == -1); // like blazor
        // execution order == [0] ==
        // - default or user Module.instantiateWasm (will start downloading dotnet.wasm)
        module.instantiateWasm = (imports, callback) => instantiateWasm(imports, callback, userInstantiateWasm);
        // execution order == [1] ==
        module.preInit = [() => preInit(userPreInit)];
        // execution order == [2] ==
        module.preRun = [() => preRunAsync(userPreRun)];
        // execution order == [4] ==
        module.onRuntimeInitialized = () => onRuntimeInitializedAsync(userOnRuntimeInitialized);
        // execution order == [5] ==
        module.postRun = [() => postRunAsync(userpostRun)];
        // execution order == [6] ==
        module.ready.then(async () => {
            // wait for previous stage
            await afterPostRun.promise;
            // - here we resolve the promise returned by createDotnetRuntime export
            // - any code after createDotnetRuntime is executed now
            dotnetReady.promise_control.resolve(exportedAPI);
        }).catch(err => {
            dotnetReady.promise_control.reject(err);
        });
        module.ready = dotnetReady.promise;
        // execution order == [*] ==
        if (!module.onAbort) {
            module.onAbort = () => mono_on_abort;
        }
    }
    function instantiateWasm(imports, successCallback, userInstantiateWasm) {
        // this is called so early that even Module exports like addRunDependency don't exist yet
        if (!Module.configSrc && !Module.config && !userInstantiateWasm) {
            Module.print("MONO_WASM: configSrc nor config was specified");
        }
        if (Module.config) {
            config = runtimeHelpers.config = Module.config;
        }
        else {
            config = runtimeHelpers.config = Module.config = {};
        }
        runtimeHelpers.diagnosticTracing = !!config.diagnosticTracing;
        if (userInstantiateWasm) {
            const exports = userInstantiateWasm(imports, (instance, module) => {
                afterInstantiateWasm.promise_control.resolve();
                successCallback(instance, module);
            });
            return exports;
        }
        instantiate_wasm_module(imports, successCallback);
        return []; // No exports
    }
    function preInit(userPreInit) {
        Module.addRunDependency("mono_pre_init");
        try {
            mono_wasm_pre_init_essential();
            if (runtimeHelpers.diagnosticTracing)
                console.debug("MONO_WASM: preInit");
            beforePreInit.promise_control.resolve();
            // all user Module.preInit callbacks
            userPreInit.forEach(fn => fn());
        }
        catch (err) {
            _print_error("MONO_WASM: user preInint() failed", err);
            abort_startup(err, true);
            throw err;
        }
        // this will start immediately but return on first await.
        // It will block our `preRun` by afterPreInit promise
        // It will block emscripten `userOnRuntimeInitialized` by pending addRunDependency("mono_pre_init")
        (async () => {
            try {
                await mono_wasm_pre_init_essential_async();
                if (!isCustomStartup) {
                    // - download Module.config from configSrc
                    // - start download assets like DLLs
                    await mono_wasm_pre_init_full();
                }
            }
            catch (err) {
                abort_startup(err, true);
                throw err;
            }
            // signal next stage
            afterPreInit.promise_control.resolve();
            Module.removeRunDependency("mono_pre_init");
        })();
    }
    async function preRunAsync(userPreRun) {
        Module.addRunDependency("mono_pre_run_async");
        // wait for previous stages
        await afterInstantiateWasm.promise;
        await afterPreInit.promise;
        if (runtimeHelpers.diagnosticTracing)
            console.debug("MONO_WASM: preRunAsync");
        if (MonoWasmThreads) {
            await instantiateWasmPThreadWorkerPool();
        }
        try {
            // all user Module.preRun callbacks
            userPreRun.map(fn => fn());
        }
        catch (err) {
            _print_error("MONO_WASM: user callback preRun() failed", err);
            abort_startup(err, true);
            throw err;
        }
        // signal next stage
        afterPreRun.promise_control.resolve();
        Module.removeRunDependency("mono_pre_run_async");
    }
    async function onRuntimeInitializedAsync(userOnRuntimeInitialized) {
        // wait for previous stage
        await afterPreRun.promise;
        if (runtimeHelpers.diagnosticTracing)
            console.debug("MONO_WASM: onRuntimeInitialized");
        // signal this stage, this will allow pending assets to allocate memory
        beforeOnRuntimeInitialized.promise_control.resolve();
        try {
            if (!isCustomStartup) {
                await wait_for_all_assets();
                // load runtime
                await mono_wasm_before_user_runtime_initialized();
            }
            if (config.runtimeOptions) {
                mono_wasm_set_runtime_options(config.runtimeOptions);
            }
            // call user code
            try {
                userOnRuntimeInitialized();
            }
            catch (err) {
                _print_error("MONO_WASM: user callback onRuntimeInitialized() failed", err);
                throw err;
            }
            // finish
            await mono_wasm_after_user_runtime_initialized();
        }
        catch (err) {
            _print_error("MONO_WASM: onRuntimeInitializedAsync() failed", err);
            abort_startup(err, true);
            throw err;
        }
        // signal next stage
        afterOnRuntimeInitialized.promise_control.resolve();
    }
    async function postRunAsync(userpostRun) {
        // wait for previous stage
        await afterOnRuntimeInitialized.promise;
        if (runtimeHelpers.diagnosticTracing)
            console.debug("MONO_WASM: postRunAsync");
        try {
            // all user Module.postRun callbacks
            userpostRun.map(fn => fn());
        }
        catch (err) {
            _print_error("MONO_WASM: user callback posRun() failed", err);
            abort_startup(err, true);
            throw err;
        }
        // signal next stage
        afterPostRun.promise_control.resolve();
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function abort_startup(reason, should_exit) {
        if (runtimeHelpers.diagnosticTracing)
            console.trace("MONO_WASM: abort_startup");
        dotnetReady.promise_control.reject(reason);
        afterInstantiateWasm.promise_control.reject(reason);
        beforePreInit.promise_control.reject(reason);
        afterPreInit.promise_control.reject(reason);
        afterPreRun.promise_control.reject(reason);
        beforeOnRuntimeInitialized.promise_control.reject(reason);
        afterOnRuntimeInitialized.promise_control.reject(reason);
        afterPostRun.promise_control.reject(reason);
        if (should_exit) {
            mono_exit(1, reason);
        }
    }
    // runs in both blazor and non-blazor
    function mono_wasm_pre_init_essential() {
        Module.addRunDependency("mono_wasm_pre_init_essential");
        if (runtimeHelpers.diagnosticTracing)
            console.debug("MONO_WASM: mono_wasm_pre_init_essential");
        // init_polyfills() is already called from export.ts
        init_c_exports();
        cwraps_internal(INTERNAL);
        cwraps_mono_api(MONO);
        cwraps_binding_api(BINDING);
        Module.removeRunDependency("mono_wasm_pre_init_essential");
    }
    // runs in both blazor and non-blazor
    async function mono_wasm_pre_init_essential_async() {
        if (runtimeHelpers.diagnosticTracing)
            console.debug("MONO_WASM: mono_wasm_pre_init_essential_async");
        Module.addRunDependency("mono_wasm_pre_init_essential_async");
        await init_polyfills_async();
        await mono_wasm_load_config(Module.configSrc);
        if (MonoWasmThreads) {
            preAllocatePThreadWorkerPool(MONO_PTHREAD_POOL_SIZE, config);
        }
        Module.removeRunDependency("mono_wasm_pre_init_essential_async");
    }
    // runs just in non-blazor
    async function mono_wasm_pre_init_full() {
        if (runtimeHelpers.diagnosticTracing)
            console.debug("MONO_WASM: mono_wasm_pre_init_full");
        Module.addRunDependency("mono_wasm_pre_init_full");
        await mono_download_assets();
        Module.removeRunDependency("mono_wasm_pre_init_full");
    }
    // runs just in non-blazor
    async function mono_wasm_before_user_runtime_initialized() {
        if (runtimeHelpers.diagnosticTracing)
            console.debug("MONO_WASM: mono_wasm_before_user_runtime_initialized");
        try {
            await _apply_configuration_from_args();
            mono_wasm_globalization_init();
            if (!runtimeHelpers.mono_wasm_load_runtime_done)
                mono_wasm_load_runtime("unused", config.debugLevel);
            if (!runtimeHelpers.mono_wasm_runtime_is_ready)
                mono_wasm_runtime_ready();
            if (!runtimeHelpers.mono_wasm_symbols_are_ready)
                readSymbolMapFile("dotnet.js.symbols");
            setTimeout(() => {
                // when there are free CPU cycles
                string_decoder.init_fields();
            });
        }
        catch (err) {
            _print_error("MONO_WASM: Error in mono_wasm_before_user_runtime_initialized", err);
            throw err;
        }
    }
    // runs in both blazor and non-blazor
    async function mono_wasm_after_user_runtime_initialized() {
        if (runtimeHelpers.diagnosticTracing)
            console.debug("MONO_WASM: mono_wasm_after_user_runtime_initialized");
        try {
            if (!Module.disableDotnet6Compatibility && Module.exports) {
                // Export emscripten defined in module through EXPORTED_RUNTIME_METHODS
                // Useful to export IDBFS or other similar types generally exposed as
                // global types when emscripten is not modularized.
                const globalThisAny = globalThis;
                for (let i = 0; i < Module.exports.length; ++i) {
                    const exportName = Module.exports[i];
                    const exportValue = Module[exportName];
                    if (exportValue != undefined) {
                        globalThisAny[exportName] = exportValue;
                    }
                    else {
                        console.warn(`MONO_WASM: The exported symbol ${exportName} could not be found in the emscripten module`);
                    }
                }
            }
            // for Blazor, init diagnostics after their "onRuntimeInitalized" sets env variables, but before their postRun callback (which calls mono_wasm_load_runtime)
            if (MonoWasmThreads) {
                await mono_wasm_init_diagnostics();
            }
            if (runtimeHelpers.diagnosticTracing)
                console.debug("MONO_WASM: Initializing mono runtime");
            if (Module.onDotnetReady) {
                try {
                    await Module.onDotnetReady();
                }
                catch (err) {
                    _print_error("MONO_WASM: onDotnetReady () failed", err);
                    throw err;
                }
            }
        }
        catch (err) {
            _print_error("MONO_WASM: Error in mono_wasm_after_user_runtime_initialized", err);
            throw err;
        }
    }
    function _print_error(message, err) {
        Module.printErr(`${message}: ${JSON.stringify(err)}`);
        if (err.stack) {
            Module.printErr("MONO_WASM: Stacktrace: \n");
            Module.printErr(err.stack);
        }
    }
    // Set environment variable NAME to VALUE
    // Should be called before mono_load_runtime_and_bcl () in most cases
    function mono_wasm_setenv(name, value) {
        wrapped_c_functions.mono_wasm_setenv(name, value);
    }
    function mono_wasm_set_runtime_options(options) {
        if (!Array.isArray(options))
            throw new Error("Expected runtimeOptions to be an array of strings");
        const argv = Module._malloc(options.length * 4);
        let aindex = 0;
        for (let i = 0; i < options.length; ++i) {
            const option = options[i];
            if (typeof (option) !== "string")
                throw new Error("Expected runtimeOptions to be an array of strings");
            Module.setValue(argv + (aindex * 4), wrapped_c_functions.mono_wasm_strdup(option), "i32");
            aindex += 1;
        }
        wrapped_c_functions.mono_wasm_parse_runtime_options(options.length, argv);
    }
    async function instantiate_wasm_module(imports, successCallback) {
        // this is called so early that even Module exports like addRunDependency don't exist yet
        try {
            await mono_wasm_load_config(Module.configSrc);
            if (runtimeHelpers.diagnosticTracing)
                console.debug("MONO_WASM: instantiate_wasm_module");
            const assetToLoad = resolve_asset_path("dotnetwasm");
            // FIXME: this would not apply re-try (on connection reset during download) for dotnet.wasm because we could not download the buffer before we pass it to instantiate_wasm_asset
            await start_asset_download_with_retries(assetToLoad, false);
            await beforePreInit.promise;
            Module.addRunDependency("instantiate_wasm_module");
            instantiate_wasm_asset(assetToLoad, imports, successCallback);
            if (runtimeHelpers.diagnosticTracing)
                console.debug("MONO_WASM: instantiate_wasm_module done");
            afterInstantiateWasm.promise_control.resolve();
        }
        catch (err) {
            _print_error("MONO_WASM: instantiate_wasm_module() failed", err);
            abort_startup(err, true);
            throw err;
        }
        Module.removeRunDependency("instantiate_wasm_module");
    }
    // runs just in non-blazor
    async function _apply_configuration_from_args() {
        try {
            const tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
            mono_wasm_setenv("TZ", tz || "UTC");
        }
        catch (_a) {
            mono_wasm_setenv("TZ", "UTC");
        }
        for (const k in config.environmentVariables) {
            const v = config.environmentVariables[k];
            if (typeof (v) === "string")
                mono_wasm_setenv(k, v);
            else
                throw new Error(`Expected environment variable '${k}' to be a string but it was ${typeof v}: '${v}'`);
        }
        if (config.runtimeOptions)
            mono_wasm_set_runtime_options(config.runtimeOptions);
        if (config.aotProfilerOptions)
            mono_wasm_init_aot_profiler(config.aotProfilerOptions);
        if (config.coverageProfilerOptions)
            mono_wasm_init_coverage_profiler(config.coverageProfilerOptions);
        // for non-Blazor, init diagnostics after environment variables are set
        if (MonoWasmThreads) {
            await mono_wasm_init_diagnostics();
        }
    }
    function mono_wasm_load_runtime(unused, debugLevel) {
        if (runtimeHelpers.diagnosticTracing)
            console.debug("MONO_WASM: mono_wasm_load_runtime");
        if (runtimeHelpers.mono_wasm_load_runtime_done) {
            return;
        }
        runtimeHelpers.mono_wasm_load_runtime_done = true;
        try {
            if (debugLevel == undefined) {
                debugLevel = 0;
                if (config && config.debugLevel) {
                    debugLevel = 0 + debugLevel;
                }
            }
            wrapped_c_functions.mono_wasm_load_runtime(unused || "unused", debugLevel);
            runtimeHelpers.waitForDebugger = config.waitForDebugger;
            if (!runtimeHelpers.mono_wasm_bindings_is_ready)
                bindings_init();
        }
        catch (err) {
            _print_error("MONO_WASM: mono_wasm_load_runtime () failed", err);
            abort_startup(err, false);
            if (ENVIRONMENT_IS_SHELL || ENVIRONMENT_IS_NODE) {
                const wasm_exit = wrapped_c_functions.mono_wasm_exit;
                wasm_exit(1);
            }
            throw err;
        }
    }
    function bindings_init() {
        if (runtimeHelpers.diagnosticTracing)
            console.debug("MONO_WASM: bindings_init");
        if (runtimeHelpers.mono_wasm_bindings_is_ready) {
            return;
        }
        runtimeHelpers.mono_wasm_bindings_is_ready = true;
        try {
            init_managed_exports();
            //init_legacy_exports();
            initialize_marshalers_to_js();
            initialize_marshalers_to_cs();
            runtimeHelpers._i52_error_scratch_buffer = Module._malloc(4);
        }
        catch (err) {
            _print_error("MONO_WASM: Error in bindings_init", err);
            throw err;
        }
    }
    /**
     * Loads the mono config file (typically called mono-config.json) asynchroniously
     * Note: the run dependencies are so emsdk actually awaits it in order.
     *
     * @param {string} configFilePath - relative path to the config file
     * @throws Will throw an error if the config file loading fails
     */
    async function mono_wasm_load_config(configFilePath) {
        if (configLoaded) {
            await afterConfigLoaded.promise;
            return;
        }
        configLoaded = true;
        if (!configFilePath) {
            normalize();
            afterConfigLoaded.promise_control.resolve();
            return;
        }
        if (runtimeHelpers.diagnosticTracing)
            console.debug("MONO_WASM: mono_wasm_load_config");
        try {
            const resolveSrc = runtimeHelpers.locateFile(configFilePath);
            const configResponse = await runtimeHelpers.fetch_like(resolveSrc);
            const loadedConfig = (await configResponse.json()) || {};
            if (loadedConfig.environmentVariables && typeof (loadedConfig.environmentVariables) !== "object")
                throw new Error("Expected config.environmentVariables to be unset or a dictionary-style object");
            // merge
            loadedConfig.assets = [...(loadedConfig.assets || []), ...(config.assets || [])];
            loadedConfig.environmentVariables = { ...(loadedConfig.environmentVariables || {}), ...(config.environmentVariables || {}) };
            config = runtimeHelpers.config = Module.config = Object.assign(Module.config, loadedConfig);
            normalize();
            if (Module.onConfigLoaded) {
                try {
                    await Module.onConfigLoaded(runtimeHelpers.config);
                    normalize();
                }
                catch (err) {
                    _print_error("MONO_WASM: onConfigLoaded() failed", err);
                    throw err;
                }
            }
            afterConfigLoaded.promise_control.resolve();
        }
        catch (err) {
            const errMessage = `Failed to load config file ${configFilePath} ${err}`;
            abort_startup(errMessage, true);
            config = runtimeHelpers.config = Module.config = { message: errMessage, error: err, isError: true };
            throw err;
        }
        function normalize() {
            // normalize
            config.environmentVariables = config.environmentVariables || {};
            config.assets = config.assets || [];
            config.runtimeOptions = config.runtimeOptions || [];
            config.globalizationMode = config.globalizationMode || "auto";
            if (config.debugLevel === undefined && BuildConfiguration === "Debug") {
                config.debugLevel = -1;
            }
            if (config.diagnosticTracing === undefined && BuildConfiguration === "Debug") {
                config.diagnosticTracing = true;
            }
            runtimeHelpers.diagnosticTracing = !!runtimeHelpers.config.diagnosticTracing;
        }
    }
    function mono_wasm_asm_loaded(assembly_name, assembly_ptr, assembly_len, pdb_ptr, pdb_len) {
        // Only trigger this codepath for assemblies loaded after app is ready
        if (runtimeHelpers.mono_wasm_runtime_is_ready !== true)
            return;
        const assembly_name_str = assembly_name !== CharPtrNull ? Module.UTF8ToString(assembly_name).concat(".dll") : "";
        const assembly_data = new Uint8Array(Module.HEAPU8.buffer, assembly_ptr, assembly_len);
        const assembly_b64 = toBase64StringImpl(assembly_data);
        let pdb_b64;
        if (pdb_ptr) {
            const pdb_data = new Uint8Array(Module.HEAPU8.buffer, pdb_ptr, pdb_len);
            pdb_b64 = toBase64StringImpl(pdb_data);
        }
        mono_wasm_raise_debug_event({
            eventName: "AssemblyLoaded",
            assembly_name: assembly_name_str,
            assembly_b64,
            pdb_b64
        });
    }
    function mono_wasm_set_main_args(name, allRuntimeArguments) {
        const main_argc = allRuntimeArguments.length + 1;
        const main_argv = Module._malloc(main_argc * 4);
        let aindex = 0;
        Module.setValue(main_argv + (aindex * 4), wrapped_c_functions.mono_wasm_strdup(name), "i32");
        aindex += 1;
        for (let i = 0; i < allRuntimeArguments.length; ++i) {
            Module.setValue(main_argv + (aindex * 4), wrapped_c_functions.mono_wasm_strdup(allRuntimeArguments[i]), "i32");
            aindex += 1;
        }
        wrapped_c_functions.mono_wasm_set_main_args(main_argc, main_argv);
    }
    /// Called when dotnet.worker.js receives an emscripten "load" event from the main thread.
    ///
    /// Notes:
    /// 1. Emscripten skips a lot of initialization on the pthread workers, Module may not have everything you expect.
    /// 2. Emscripten does not run the preInit or preRun functions in the workers.
    /// 3. At the point when this executes there is no pthread assigned to the worker yet.
    async function mono_wasm_pthread_worker_init() {
        console.debug("MONO_WASM: worker initializing essential C exports and APIs");
        // FIXME: copy/pasted from mono_wasm_pre_init_essential - can we share this code? Any other global state that needs initialization?
        init_c_exports();
        // not initializing INTERNAL, MONO, or BINDING C wrappers here - those legacy APIs are not likely to be needed on pthread workers.
        // This is a good place for subsystems to attach listeners for pthreads_worker.currentWorkerThreadEvents
        currentWorkerThreadEvents.addEventListener(dotnetPthreadCreated, (ev) => {
            console.debug("MONO_WASM: pthread created", ev.pthread_self.pthread_id);
        });
    }
    /**
    * @deprecated
    */
    async function mono_load_runtime_and_bcl_args(cfg) {
        config = Module.config = runtimeHelpers.config = Object.assign(runtimeHelpers.config || {}, cfg || {});
        await mono_download_assets();
        if (!isCustomStartup) {
            await wait_for_all_assets();
        }
    }

    var monoDiagnosticsMock = false;

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    let magic_buf = null;
    const Magic = {
        get DOTNET_IPC_V1() {
            if (magic_buf === null) {
                const magic = "DOTNET_IPC_V1";
                const magic_len = magic.length + 1; // nul terminated
                magic_buf = new Uint8Array(magic_len);
                for (let i = 0; i < magic_len; i++) {
                    magic_buf[i] = magic.charCodeAt(i);
                }
                magic_buf[magic_len - 1] = 0;
            }
            return magic_buf;
        },
        get MinimalHeaderSize() {
            // we just need to see the magic and the size
            const sizeOfSize = 2;
            return Magic.DOTNET_IPC_V1.byteLength + sizeOfSize;
        },
    };

    // Licensed to the .NET Foundation under one or more agreements.
    function advancePos$1(pos, count) {
        pos.pos += count;
    }
    function serializeHeader(buf, pos, commandSet, command, len) {
        Serializer.serializeMagic(buf, pos);
        Serializer.serializeUint16(buf, pos, len);
        Serializer.serializeUint8(buf, pos, commandSet);
        Serializer.serializeUint8(buf, pos, command);
        Serializer.serializeUint16(buf, pos, 0); // reserved
    }
    const Serializer = {
        computeMessageByteLength(payload) {
            const fullHeaderSize = Magic.MinimalHeaderSize // magic, len
                + 2 // commandSet, command
                + 2; // reserved ;
            const payloadLength = payload ? (payload instanceof Uint8Array ? payload.byteLength : payload) : 0;
            const len = fullHeaderSize + payloadLength; // magic, size, commandSet, command, reserved
            return len;
        },
        serializeMagic(buf, pos) {
            buf.set(Magic.DOTNET_IPC_V1, pos.pos);
            advancePos$1(pos, Magic.DOTNET_IPC_V1.byteLength);
        },
        serializeUint8(buf, pos, value) {
            buf[pos.pos++] = value;
        },
        serializeUint16(buf, pos, value) {
            buf[pos.pos++] = value & 0xFF;
            buf[pos.pos++] = (value >> 8) & 0xFF;
        },
        serializeUint32(buf, pos, value) {
            buf[pos.pos++] = value & 0xFF;
            buf[pos.pos++] = (value >> 8) & 0xFF;
            buf[pos.pos++] = (value >> 16) & 0xFF;
            buf[pos.pos++] = (value >> 24) & 0xFF;
        },
        serializeUint64(buf, pos, value) {
            Serializer.serializeUint32(buf, pos, value[0]);
            Serializer.serializeUint32(buf, pos, value[1]);
        },
        serializeHeader,
        serializePayload(buf, pos, payload) {
            buf.set(payload, pos.pos);
            advancePos$1(pos, payload.byteLength);
        },
        serializeString(buf, pos, s) {
            if (s === null) {
                Serializer.serializeUint32(buf, pos, 0);
            }
            else {
                const len = s.length;
                const hasNul = s[len - 1] === "\0";
                Serializer.serializeUint32(buf, pos, len + (hasNul ? 0 : 1));
                for (let i = 0; i < len; i++) {
                    Serializer.serializeUint16(buf, pos, s.charCodeAt(i));
                }
                if (!hasNul) {
                    Serializer.serializeUint16(buf, pos, 0);
                }
            }
        },
    };

    // Licensed to the .NET Foundation under one or more agreements.
    function expectAdvertise(data) {
        if (typeof (data) === "string") {
            assertNever(data);
        }
        else {
            const view = new Uint8Array(data);
            const ADVR_V1 = Array.from("ADVR_V1\0").map((c) => c.charCodeAt(0));
            /* TODO: check that the message is really long enough for the cookie, process ID and reserved bytes */
            return view.length >= ADVR_V1.length && ADVR_V1.every((v, i) => v === view[i]);
        }
    }
    function expectOk(payloadLength) {
        return (data) => {
            if (typeof (data) === "string") {
                assertNever(data);
            }
            else {
                const view = new Uint8Array(data);
                const extra = payloadLength !== undefined ? payloadLength : 0;
                return view.length >= (20 + extra) && view[16] === 0xFF && view[17] == 0x00;
            }
        };
    }
    function extractOkSessionID(data) {
        if (typeof (data) === "string") {
            assertNever(data);
        }
        else {
            const view = new Uint8Array(data, 20, 8);
            const sessionIDLo = view[0] | (view[1] << 8) | (view[2] << 16) | (view[3] << 24);
            const sessionIDHi = view[4] | (view[5] << 8) | (view[6] << 16) | (view[7] << 24);
            if (!(sessionIDHi === 0)) throw new Error("Assert failed: mock: sessionIDHi should be zero"); // inlined mono_assert
            return sessionIDLo;
        }
    }
    function computeStringByteLength(s) {
        if (s === undefined || s === null || s === "")
            return 4; // just length of zero
        return 4 + 2 * s.length + 2; // length + UTF16 + null
    }
    function computeCollectTracing2PayloadByteLength(payload) {
        let len = 0;
        len += 4; // circularBufferMB
        len += 4; // format
        len += 1; // requestRundown
        len += 4; // providers length
        for (const provider of payload.providers) {
            len += 8; // keywords
            len += 4; // level
            len += computeStringByteLength(provider.provider_name);
            len += computeStringByteLength(provider.filter_data);
        }
        return len;
    }
    function makeEventPipeCollectTracing2(payload) {
        const payloadLength = computeCollectTracing2PayloadByteLength(payload);
        const messageLength = Serializer.computeMessageByteLength(payloadLength);
        const buffer = new Uint8Array(messageLength);
        const pos = { pos: 0 };
        Serializer.serializeHeader(buffer, pos, 2 /* CommandSetId.EventPipe */, 3 /* EventPipeCommandId.CollectTracing2 */, messageLength);
        Serializer.serializeUint32(buffer, pos, payload.circularBufferMB);
        Serializer.serializeUint32(buffer, pos, payload.format);
        Serializer.serializeUint8(buffer, pos, payload.requestRundown ? 1 : 0);
        Serializer.serializeUint32(buffer, pos, payload.providers.length);
        for (const provider of payload.providers) {
            Serializer.serializeUint64(buffer, pos, provider.keywords);
            Serializer.serializeUint32(buffer, pos, provider.logLevel);
            Serializer.serializeString(buffer, pos, provider.provider_name);
            Serializer.serializeString(buffer, pos, provider.filter_data);
        }
        return buffer;
    }
    function makeEventPipeStopTracing(payload) {
        const payloadLength = 8;
        const messageLength = Serializer.computeMessageByteLength(payloadLength);
        const buffer = new Uint8Array(messageLength);
        const pos = { pos: 0 };
        Serializer.serializeHeader(buffer, pos, 2 /* CommandSetId.EventPipe */, 1 /* EventPipeCommandId.StopTracing */, messageLength);
        Serializer.serializeUint32(buffer, pos, payload.sessionID);
        Serializer.serializeUint32(buffer, pos, 0);
        return buffer;
    }
    function makeProcessResumeRuntime() {
        const payloadLength = 0;
        const messageLength = Serializer.computeMessageByteLength(payloadLength);
        const buffer = new Uint8Array(messageLength);
        const pos = { pos: 0 };
        Serializer.serializeHeader(buffer, pos, 4 /* CommandSetId.Process */, 1 /* ProcessCommandId.ResumeRuntime */, messageLength);
        return buffer;
    }
    function createMockEnvironment() {
        const command = {
            makeEventPipeCollectTracing2,
            makeEventPipeStopTracing,
            makeProcessResumeRuntime,
        };
        const reply = {
            expectOk,
            extractOkSessionID,
        };
        return {
            createPromiseController,
            delay,
            command,
            reply,
            expectAdvertise
        };
    }

    // Licensed to the .NET Foundation under one or more agreements.
    let MockImplConstructor;
    function mock(script, options) {
        if (monoDiagnosticsMock) {
            if (!MockImplConstructor) {
                class MockScriptEngineSocketImpl {
                    constructor(engine) {
                        this.engine = engine;
                    }
                    send(data) {
                        if (this.engine.trace) {
                            console.debug(`mock ${this.engine.ident} client sent: `, data);
                        }
                        let event = null;
                        if (typeof data === "string") {
                            event = new MessageEvent("message", { data });
                        }
                        else {
                            const message = new ArrayBuffer(data.byteLength);
                            const messageView = new Uint8Array(message);
                            const dataView = new Uint8Array(data);
                            messageView.set(dataView);
                            event = new MessageEvent("message", { data: message });
                        }
                        this.engine.mockReplyEventTarget.dispatchEvent(event);
                    }
                    addEventListener(event, listener, options) {
                        if (this.engine.trace) {
                            console.debug(`mock ${this.engine.ident} client added listener for ${event}`);
                        }
                        this.engine.eventTarget.addEventListener(event, listener, options);
                    }
                    removeEventListener(event, listener) {
                        if (this.engine.trace) {
                            console.debug(`mock ${this.engine.ident} client removed listener for ${event}`);
                        }
                        this.engine.eventTarget.removeEventListener(event, listener);
                    }
                    close() {
                        if (this.engine.trace) {
                            console.debug(`mock ${this.engine.ident} client closed`);
                        }
                        this.engine.mockReplyEventTarget.dispatchEvent(new CloseEvent("close"));
                    }
                    dispatchEvent(ev) {
                        return this.engine.eventTarget.dispatchEvent(ev);
                    }
                }
                class MockScriptEngineImpl {
                    constructor(trace, ident) {
                        this.trace = trace;
                        this.ident = ident;
                        // eventTarget that the MockReplySocket will dispatch to
                        this.eventTarget = new EventTarget();
                        // eventTarget that the MockReplySocket with send() to
                        this.mockReplyEventTarget = new EventTarget();
                        this.socket = new MockScriptEngineSocketImpl(this);
                    }
                    reply(data) {
                        if (this.trace) {
                            console.debug(`mock ${this.ident} reply:`, data);
                        }
                        let sendData;
                        if (typeof data === "object" && data instanceof ArrayBuffer) {
                            sendData = new ArrayBuffer(data.byteLength);
                            const sendDataView = new Uint8Array(sendData);
                            const dataView = new Uint8Array(data);
                            sendDataView.set(dataView);
                        }
                        else if (typeof data === "object" && data instanceof Uint8Array) {
                            sendData = new ArrayBuffer(data.byteLength);
                            const sendDataView = new Uint8Array(sendData);
                            sendDataView.set(data);
                        }
                        else {
                            console.warn(`mock ${this.ident} reply got wrong kind of reply data, expected ArrayBuffer`, data);
                            assertNever(data);
                        }
                        this.eventTarget.dispatchEvent(new MessageEvent("message", { data: sendData }));
                    }
                    async waitForSend(filter, extract) {
                        const trace = this.trace;
                        if (trace) {
                            console.debug(`mock ${this.ident} waitForSend`);
                        }
                        const event = await new Promise((resolve) => {
                            this.mockReplyEventTarget.addEventListener("message", (event) => {
                                if (trace) {
                                    console.debug(`mock ${this.ident} waitForSend got:`, event);
                                }
                                resolve(event);
                            }, { once: true });
                        });
                        const data = event.data;
                        if (typeof data === "string") {
                            console.warn(`mock ${this.ident} waitForSend got string:`, data);
                            throw new Error("mock script connection received string data");
                        }
                        if (!filter(data)) {
                            throw new Error("Unexpected data");
                        }
                        if (extract) {
                            return extract(data);
                        }
                        return undefined;
                    }
                }
                MockImplConstructor = class MockImpl {
                    constructor(mockScript, options) {
                        var _a;
                        this.mockScript = mockScript;
                        const env = createMockEnvironment();
                        this.connectionScripts = mockScript(env);
                        this.openCount = 0;
                        this.trace = (_a = options === null || options === void 0 ? void 0 : options.trace) !== null && _a !== void 0 ? _a : false;
                        const count = this.connectionScripts.length;
                        this.engines = new Array(count);
                        for (let i = 0; i < count; ++i) {
                            this.engines[i] = new MockScriptEngineImpl(this.trace, i);
                        }
                    }
                    open() {
                        const i = this.openCount++;
                        if (this.trace) {
                            console.debug(`mock ${i} open`);
                        }
                        return this.engines[i].socket;
                    }
                    async run() {
                        const scripts = this.connectionScripts;
                        await Promise.all(scripts.map((script, i) => script(this.engines[i])));
                    }
                };
            }
            return new MockImplConstructor(script, options);
        }
        else {
            return undefined;
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    function importAndInstantiateMock(mockURL) {
        if (monoDiagnosticsMock) {
            const mockPrefix = "mock:";
            const scriptURL = mockURL.substring(mockPrefix.length);
            return import(scriptURL).then((mockModule) => {
                const script = mockModule.default;
                return mock(script, { trace: true });
            });
        }
        else {
            return Promise.resolve(undefined);
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    function isDiagnosticCommandBase(x) {
        return typeof x === "object" && "command_set" in x && "command" in x;
    }
    function isProcessCommand(x) {
        return isDiagnosticCommandBase(x) && x.command_set === "Process";
    }
    function isEventPipeCommand(x) {
        return isDiagnosticCommandBase(x) && x.command_set === "EventPipe";
    }
    function isProcessCommandResumeRuntime(x) {
        return isProcessCommand(x) && x.command === "ResumeRuntime";
    }
    function isEventPipeCollectTracingCommandProvider(x) {
        return typeof x === "object" && "keywords" in x && "logLevel" in x && "provider_name" in x && "filter_data" in x;
    }
    function isEventPipeCommandCollectTracing2(x) {
        return isEventPipeCommand(x) && x.command === "CollectTracing2" && "circularBufferMB" in x &&
            "format" in x && "requestRundown" in x && "providers" in x &&
            Array.isArray(x.providers) && x.providers.every(isEventPipeCollectTracingCommandProvider);
    }
    function isEventPipeCommandStopTracing(x) {
        return isEventPipeCommand(x) && x.command === "StopTracing" && "sessionID" in x;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    var ListenerState;
    (function (ListenerState) {
        ListenerState[ListenerState["Sending"] = 0] = "Sending";
        ListenerState[ListenerState["Closed"] = 1] = "Closed";
        ListenerState[ListenerState["Error"] = 2] = "Error";
    })(ListenerState || (ListenerState = {}));
    class SocketGuts {
        constructor(socket) {
            this.socket = socket;
        }
        close() {
            this.socket.close();
        }
        write(data, size) {
            const buf = new ArrayBuffer(size);
            const view = new Uint8Array(buf);
            // Can we avoid this copy?
            view.set(new Uint8Array(Module.HEAPU8.buffer, data, size));
            this.socket.send(buf);
        }
    }
    /// A wrapper around a WebSocket that just sends data back to the host.
    /// It sets up message and clsoe handlers on the WebSocket tht put it into an idle state
    /// if the connection closes or we receive any replies.
    class EventPipeSocketConnection {
        constructor(socket) {
            this._state = ListenerState.Sending;
            this.stream = new SocketGuts(socket);
        }
        close() {
            console.debug("MONO_WASM: EventPipe session stream closing websocket");
            switch (this._state) {
                case ListenerState.Error:
                    return;
                case ListenerState.Closed:
                    return;
                default:
                    this._state = ListenerState.Closed;
                    this.stream.close();
                    return;
            }
        }
        write(ptr, len) {
            switch (this._state) {
                case ListenerState.Sending:
                    this.stream.write(ptr, len);
                    return true;
                case ListenerState.Closed:
                    // ignore
                    return false;
                case ListenerState.Error:
                    return false;
            }
        }
        _onMessage(event) {
            switch (this._state) {
                case ListenerState.Sending:
                    /* unexpected message */
                    console.warn("MONO_WASM: EventPipe session stream received unexpected message from websocket", event);
                    // TODO notify runtime that the connection had an error
                    this._state = ListenerState.Error;
                    break;
                case ListenerState.Closed:
                    /* ignore */
                    break;
                case ListenerState.Error:
                    /* ignore */
                    break;
                default:
                    assertNever(this._state);
            }
        }
        _onClose( /*event: CloseEvent*/) {
            switch (this._state) {
                case ListenerState.Closed:
                    return; /* do nothing */
                case ListenerState.Error:
                    return; /* do nothing */
                default:
                    this._state = ListenerState.Closed;
                    this.stream.close();
                    // TODO: notify runtime that connection is closed
                    return;
            }
        }
        _onError(event) {
            console.debug("MONO_WASM: EventPipe session stream websocket error", event);
            this._state = ListenerState.Error;
            this.stream.close();
            // TODO: notify runtime that connection had an error
        }
        addListeners() {
            const socket = this.stream.socket;
            socket.addEventListener("message", this._onMessage.bind(this));
            addEventListener("close", this._onClose.bind(this));
            addEventListener("error", this._onError.bind(this));
        }
    }
    /// Take over a WebSocket that was used by the diagnostic server to receive the StartCollecting command and
    /// use it for sending the event pipe data back to the host.
    function takeOverSocket(socket) {
        const connection = new EventPipeSocketConnection(socket);
        connection.addListeners();
        return connection;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    /// One-reader, one-writer, size 1 queue for messages from an EventPipe streaming thread to
    // the diagnostic server thread that owns the WebSocket.
    // EventPipeStreamQueue has 3 memory words that are used to communicate with the streaming thread:
    // struct MonoWasmEventPipeStreamQueue {
    //    union { void* buf; intptr_t close_msg; /* -1 */ };
    //    int32_t count;
    //    volatile int32_t buf_full;
    // }
    //
    // To write, the streaming thread:
    //  1. sets buf (or close_msg) and count, and then atomically sets buf_full.
    //  2. queues mono_wasm_diagnostic_server_stream_signal_work_available to run on the diagnostic server thread
    //  3. waits for buf_full to be 0.
    //
    // Note this is a little bit fragile if there are multiple writers.
    // There _are_ multiple writers - when the streaming session first starts, either the diagnostic server thread
    // or the main thread write to the queue before the streaming thread starts.  But those actions are
    // implicitly serialized because the streaming thread isn't started until the writes are done.
    const BUF_OFFSET = 0;
    const COUNT_OFFSET = 4;
    const WRITE_DONE_OFFSET = 8;
    const STREAM_CLOSE_SENTINEL = -1;
    class StreamQueue {
        constructor(queue_addr, syncSendBuffer, syncSendClose) {
            this.queue_addr = queue_addr;
            this.syncSendBuffer = syncSendBuffer;
            this.syncSendClose = syncSendClose;
            this.workAvailable = new EventTarget();
            this.signalWorkAvailable = this.signalWorkAvailableImpl.bind(this);
            this.workAvailable.addEventListener("workAvailable", this.onWorkAvailable.bind(this));
        }
        get buf_addr() {
            return this.queue_addr + BUF_OFFSET;
        }
        get count_addr() {
            return this.queue_addr + COUNT_OFFSET;
        }
        get buf_full_addr() {
            return this.queue_addr + WRITE_DONE_OFFSET;
        }
        /// called from native code on the diagnostic thread when the streaming thread queues a call to notify the
        /// diagnostic thread that it can send the buffer.
        wakeup() {
            queueMicrotask(this.signalWorkAvailable);
        }
        workAvailableNow() {
            // process the queue immediately, rather than waiting for the next event loop tick.
            this.onWorkAvailable();
        }
        signalWorkAvailableImpl() {
            this.workAvailable.dispatchEvent(new Event("workAvailable"));
        }
        onWorkAvailable() {
            const buf = getI32(this.buf_addr);
            const intptr_buf = buf;
            if (intptr_buf === STREAM_CLOSE_SENTINEL) {
                // special value signaling that the streaming thread closed the queue.
                this.syncSendClose();
            }
            else {
                const count = getI32(this.count_addr);
                setI32(this.buf_addr, 0);
                if (count > 0) {
                    this.syncSendBuffer(buf, count);
                }
            }
            /* buffer is now not full */
            Atomics.storeI32(this.buf_full_addr, 0);
            /* wake up the writer thread */
            Atomics.notifyI32(this.buf_full_addr, 1);
        }
    }
    // maps stream queue addresses to StreamQueue instances
    const streamQueueMap = new Map();
    function allocateQueue(nativeQueueAddr, syncSendBuffer, syncSendClose) {
        const queue = new StreamQueue(nativeQueueAddr, syncSendBuffer, syncSendClose);
        streamQueueMap.set(nativeQueueAddr, queue);
        return queue;
    }
    function closeQueue(nativeQueueAddr) {
        streamQueueMap.delete(nativeQueueAddr);
        // TODO: remove the event listener?
    }
    // called from native code on the diagnostic thread by queueing a call from the streaming thread.
    function mono_wasm_diagnostic_server_stream_signal_work_available(nativeQueueAddr, current_thread) {
        const queue = streamQueueMap.get(nativeQueueAddr);
        if (queue) {
            if (current_thread === 0) {
                queue.wakeup();
            }
            else {
                queue.workAvailableNow();
            }
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const sizeOfInt32 = 4;
    function createSessionWithPtrCB(sessionIdOutPtr, options, sessionType) {
        setI32(sessionIdOutPtr, 0);
        let tracePath;
        let ipcStreamAddr;
        if (sessionType.type === "file") {
            tracePath = sessionType.filePath;
            ipcStreamAddr = 0;
        }
        else {
            tracePath = null;
            ipcStreamAddr = sessionType.stream;
        }
        if (!wrapped_c_functions.mono_wasm_event_pipe_enable(tracePath, ipcStreamAddr, options.bufferSizeInMB, options.providers, options.rundownRequested, sessionIdOutPtr)) {
            return false;
        }
        else {
            return getU32(sessionIdOutPtr);
        }
    }
    function createEventPipeStreamingSession(ipcStreamAddr, options) {
        return withStackAlloc(sizeOfInt32, createSessionWithPtrCB, options, { type: "stream", stream: ipcStreamAddr });
    }
    function createEventPipeFileSession(tracePath, options) {
        return withStackAlloc(sizeOfInt32, createSessionWithPtrCB, options, { type: "file", filePath: tracePath });
    }

    /// The streaming session holds all the pieces of an event pipe streaming session that the
    ///  diagnostic server knows about: the session ID, a
    ///  queue used by the EventPipe streaming thread to forward events to the diagnostic server thread,
    ///  and a wrapper around the WebSocket object used to send event data back to the host.
    class EventPipeStreamingSession {
        constructor(sessionID, queue, connection) {
            this.sessionID = sessionID;
            this.queue = queue;
            this.connection = connection;
        }
    }
    async function makeEventPipeStreamingSession(ws, cmd) {
        // First, create the native IPC stream and get its queue.
        const ipcStreamAddr = wrapped_c_functions.mono_wasm_diagnostic_server_create_stream(); // FIXME: this should be a wrapped in a JS object so we can free it when we're done.
        const queueAddr = getQueueAddrFromStreamAddr(ipcStreamAddr);
        // then take over the websocket connection
        const conn = takeOverSocket(ws);
        // and set up queue notifications
        const queue = allocateQueue(queueAddr, conn.write.bind(conn), conn.close.bind(conn));
        const options = {
            rundownRequested: cmd.requestRundown,
            bufferSizeInMB: cmd.circularBufferMB,
            providers: providersStringFromObject(cmd.providers),
        };
        // create the event pipe session
        const sessionID = createEventPipeStreamingSession(ipcStreamAddr, options);
        if (sessionID === false)
            throw new Error("failed to create event pipe session");
        return new EventPipeStreamingSession(sessionID, queue, conn);
    }
    function providersStringFromObject(providers) {
        const providersString = providers.map(providerToString).join(",");
        return providersString;
        function providerToString(provider) {
            const keyword_str = provider.keywords[0] === 0 && provider.keywords[1] === 0 ? "" : keywordsToHexString(provider.keywords);
            const args_str = provider.filter_data === "" ? "" : ":" + provider.filter_data;
            return provider.provider_name + ":" + keyword_str + ":" + provider.logLevel + args_str;
        }
        function keywordsToHexString(k) {
            const lo = k[0];
            const hi = k[1];
            const lo_hex = leftPad(lo.toString(16), "0", 8);
            const hi_hex = leftPad(hi.toString(16), "0", 8);
            return hi_hex + lo_hex;
        }
        function leftPad(s, fill, width) {
            if (s.length >= width)
                return s;
            const prefix = fill.repeat(width - s.length);
            return prefix + s;
        }
    }
    const IPC_STREAM_QUEUE_OFFSET = 4; /* keep in sync with mono_wasm_diagnostic_server_create_stream() in C */
    function getQueueAddrFromStreamAddr(streamAddr) {
        return streamAddr + IPC_STREAM_QUEUE_OFFSET;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    function advancePos(pos, offset) {
        pos.pos += offset;
    }
    const Parser = {
        tryParseHeader(buf, pos) {
            let j = pos.pos;
            for (let i = 0; i < Magic.DOTNET_IPC_V1.length; i++) {
                if (buf[j++] !== Magic.DOTNET_IPC_V1[i]) {
                    return false;
                }
            }
            advancePos(pos, Magic.DOTNET_IPC_V1.length);
            return true;
        },
        tryParseSize(buf, pos) {
            return Parser.tryParseUint16(buf, pos);
        },
        tryParseCommand(buf, pos) {
            const commandSet = Parser.tryParseUint8(buf, pos);
            if (commandSet === undefined)
                return undefined;
            const command = Parser.tryParseUint8(buf, pos);
            if (command === undefined)
                return undefined;
            if (Parser.tryParseReserved(buf, pos) === undefined)
                return undefined;
            const payload = buf.slice(pos.pos);
            const result = {
                commandSet,
                command,
                payload
            };
            return result;
        },
        tryParseReserved(buf, pos) {
            const reservedLength = 2; // 2 bytes reserved, must be 0
            for (let i = 0; i < reservedLength; i++) {
                const reserved = Parser.tryParseUint8(buf, pos);
                if (reserved === undefined || reserved !== 0) {
                    return undefined;
                }
            }
            return true;
        },
        tryParseUint8(buf, pos) {
            const j = pos.pos;
            if (j >= buf.byteLength) {
                return undefined;
            }
            const size = buf[j];
            advancePos(pos, 1);
            return size;
        },
        tryParseUint16(buf, pos) {
            const j = pos.pos;
            if (j + 1 >= buf.byteLength) {
                return undefined;
            }
            const size = (buf[j + 1] << 8) | buf[j];
            advancePos(pos, 2);
            return size;
        },
        tryParseUint32(buf, pos) {
            const j = pos.pos;
            if (j + 3 >= buf.byteLength) {
                return undefined;
            }
            const size = (buf[j + 3] << 24) | (buf[j + 2] << 16) | (buf[j + 1] << 8) | buf[j];
            advancePos(pos, 4);
            return size;
        },
        tryParseUint64(buf, pos) {
            const lo = Parser.tryParseUint32(buf, pos);
            if (lo === undefined)
                return undefined;
            const hi = Parser.tryParseUint32(buf, pos);
            if (hi === undefined)
                return undefined;
            return [lo, hi];
        },
        tryParseBool(buf, pos) {
            const r = Parser.tryParseUint8(buf, pos);
            if (r === undefined)
                return undefined;
            return r !== 0;
        },
        tryParseArraySize(buf, pos) {
            const r = Parser.tryParseUint32(buf, pos);
            if (r === undefined)
                return undefined;
            return r;
        },
        tryParseStringLength(buf, pos) {
            return Parser.tryParseArraySize(buf, pos);
        },
        tryParseUtf16String(buf, pos) {
            const length = Parser.tryParseStringLength(buf, pos);
            if (length === undefined)
                return undefined;
            const j = pos.pos;
            if (j + length * 2 > buf.byteLength) {
                return undefined;
            }
            const result = new Array(length);
            for (let i = 0; i < length; i++) {
                result[i] = (buf[j + 2 * i + 1] << 8) | buf[j + 2 * i];
            }
            advancePos(pos, length * 2);
            /* Trim trailing nul character(s) that are added by the protocol */
            let trailingNulStart = -1;
            for (let i = result.length - 1; i >= 0; i--) {
                if (result[i] === 0) {
                    trailingNulStart = i;
                }
                else {
                    break;
                }
            }
            if (trailingNulStart >= 0)
                result.splice(trailingNulStart);
            return String.fromCharCode.apply(null, result);
        }
    };

    // Licensed to the .NET Foundation under one or more agreements.
    const dotnetDiagnosticsServerProtocolCommandEvent = "dotnet:diagnostics:protocolCommand";
    var InState;
    (function (InState) {
        InState[InState["Idle"] = 0] = "Idle";
        InState[InState["PartialCommand"] = 1] = "PartialCommand";
        InState[InState["Error"] = 2] = "Error";
    })(InState || (InState = {}));
    /// A helper object that accumulates command data that is received and provides parsed commands
    class StatefulParser {
        constructor(emitCommandCallback) {
            this.emitCommandCallback = emitCommandCallback;
            this.state = { state: InState.Idle };
        }
        /// process the data in the given buffer and update the state.
        receiveBuffer(buf) {
            if (this.state.state == InState.Error) {
                return;
            }
            let result;
            if (this.state.state === InState.Idle) {
                result = this.tryParseHeader(new Uint8Array(buf));
            }
            else {
                result = this.tryAppendBuffer(new Uint8Array(buf));
            }
            if (result.success) {
                console.debug("MONO_WASM: protocol-socket: got result", result);
                this.setState(result.newState);
                if (result.command) {
                    const command = result.command;
                    this.emitCommandCallback(command);
                }
            }
            else {
                console.warn("MONO_WASM: socket received invalid command header", buf, result.error);
                // FIXME: dispatch error event?
                this.setState({ state: InState.Error });
            }
        }
        tryParseHeader(buf) {
            const pos = { pos: 0 };
            if (buf.byteLength < Magic.MinimalHeaderSize) {
                // TODO: we need to see the magic and the size to make a partial commmand
                return { success: false, error: "not enough data" };
            }
            if (!Parser.tryParseHeader(buf, pos)) {
                return { success: false, error: "invalid header" };
            }
            const size = Parser.tryParseSize(buf, pos);
            if (size === undefined || size < Magic.MinimalHeaderSize) {
                return { success: false, error: "invalid size" };
            }
            // make a "partially completed" state with a buffer of the right size and just the header upto the size
            // field filled in.
            const parsedSize = pos.pos;
            const partialBuf = new ArrayBuffer(size);
            const partialBufView = new Uint8Array(partialBuf);
            partialBufView.set(buf.subarray(0, parsedSize));
            const partialState = { state: InState.PartialCommand, buf: partialBufView, size: parsedSize };
            return this.continueWithBuffer(partialState, buf.subarray(parsedSize));
        }
        tryAppendBuffer(moreBuf) {
            if (this.state.state !== InState.PartialCommand) {
                return { success: false, error: "not in partial command state" };
            }
            return this.continueWithBuffer(this.state, moreBuf);
        }
        continueWithBuffer(state, moreBuf) {
            const buf = state.buf;
            let partialSize = state.size;
            let overflow = null;
            if (partialSize + moreBuf.byteLength <= buf.byteLength) {
                buf.set(moreBuf, partialSize);
                partialSize += moreBuf.byteLength;
            }
            else {
                const overflowSize = partialSize + moreBuf.byteLength - buf.byteLength;
                const overflowOffset = moreBuf.byteLength - overflowSize;
                buf.set(moreBuf.subarray(0, buf.byteLength - partialSize), partialSize);
                partialSize = buf.byteLength;
                const overflowBuf = new ArrayBuffer(overflowSize);
                overflow = new Uint8Array(overflowBuf);
                overflow.set(moreBuf.subarray(overflowOffset));
            }
            if (partialSize < buf.byteLength) {
                const newState = { state: InState.PartialCommand, buf, size: partialSize };
                return { success: true, command: undefined, newState };
            }
            else {
                const pos = { pos: Magic.MinimalHeaderSize };
                let result = this.tryParseCompletedBuffer(buf, pos);
                if (overflow) {
                    console.warn("MONO_WASM: additional bytes past command payload", overflow);
                    if (result.success) {
                        const newResult = { success: true, command: result.command, newState: { state: InState.Error } };
                        result = newResult;
                    }
                }
                return result;
            }
        }
        tryParseCompletedBuffer(buf, pos) {
            const command = Parser.tryParseCommand(buf, pos);
            if (!command) {
                this.setState({ state: InState.Error });
                return { success: false, error: "invalid command" };
            }
            return { success: true, command, newState: { state: InState.Idle } };
        }
        setState(state) {
            this.state = state;
        }
        reset() {
            this.setState({ state: InState.Idle });
        }
    }
    class ProtocolSocketImpl {
        constructor(sock) {
            this.sock = sock;
            this.statefulParser = new StatefulParser(this.emitCommandCallback.bind(this));
            this.protocolListeners = 0;
            this.messageListener = this.onMessage.bind(this);
        }
        onMessage(ev) {
            const data = ev.data;
            console.debug("MONO_WASM: protocol socket received message", ev.data);
            if (typeof data === "object" && data instanceof ArrayBuffer) {
                this.onArrayBuffer(data);
            }
            else if (typeof data === "object" && data instanceof Blob) {
                data.arrayBuffer().then(this.onArrayBuffer.bind(this));
            }
            else if (typeof data === "string") {
                // otherwise it's string, ignore it.
                console.debug("MONO_WASM: protocol socket received string message; ignoring it", ev.data);
            }
            else {
                assertNever(data);
            }
        }
        dispatchEvent(evt) {
            return this.sock.dispatchEvent(evt);
        }
        onArrayBuffer(buf) {
            console.debug("MONO_WASM: protocol-socket: parsing array buffer", buf);
            this.statefulParser.receiveBuffer(buf);
        }
        // called by the stateful parser when it has a complete command
        emitCommandCallback(command) {
            console.debug("MONO_WASM: protocol-socket: queueing command", command);
            queueMicrotask(() => {
                console.debug("MONO_WASM: dispatching protocol event with command", command);
                this.dispatchProtocolCommandEvent(command);
            });
        }
        dispatchProtocolCommandEvent(cmd) {
            const ev = new Event(dotnetDiagnosticsServerProtocolCommandEvent);
            ev.data = cmd; // FIXME: use a proper event subclass
            this.sock.dispatchEvent(ev);
        }
        addEventListener(type, listener, options) {
            this.sock.addEventListener(type, listener, options);
            if (type === dotnetDiagnosticsServerProtocolCommandEvent) {
                if (this.protocolListeners === 0) {
                    console.debug("MONO_WASM: adding protocol listener, with a message chaser");
                    this.sock.addEventListener("message", this.messageListener);
                }
                this.protocolListeners++;
            }
        }
        removeEventListener(type, listener) {
            if (type === dotnetDiagnosticsServerProtocolCommandEvent) {
                console.debug("MONO_WASM: removing protocol listener and message chaser");
                this.protocolListeners--;
                if (this.protocolListeners === 0) {
                    this.sock.removeEventListener("message", this.messageListener);
                    this.statefulParser.reset();
                }
            }
            this.sock.removeEventListener(type, listener);
        }
        send(buf) {
            this.sock.send(buf);
        }
        close() {
            this.sock.close();
            this.statefulParser.reset();
        }
    }
    function createProtocolSocket(socket) {
        return new ProtocolSocketImpl(socket);
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    function isBinaryProtocolCommand(x) {
        return "commandSet" in x && "command" in x && "payload" in x;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    function parseBinaryProtocolCommand(cmd) {
        switch (cmd.commandSet) {
            case 0 /* CommandSetId.Reserved */:
                throw new Error("unexpected reserved command_set command");
            case 1 /* CommandSetId.Dump */:
                throw new Error("TODO");
            case 2 /* CommandSetId.EventPipe */:
                return parseEventPipeCommand(cmd);
            case 3 /* CommandSetId.Profiler */:
                throw new Error("TODO");
            case 4 /* CommandSetId.Process */:
                return parseProcessCommand(cmd);
            default:
                return { success: false, error: `unexpected command_set ${cmd.commandSet} command` };
        }
    }
    function parseEventPipeCommand(cmd) {
        switch (cmd.command) {
            case 1 /* EventPipeCommandId.StopTracing */:
                return parseEventPipeStopTracing(cmd);
            case 2 /* EventPipeCommandId.CollectTracing */:
                throw new Error("TODO");
            case 3 /* EventPipeCommandId.CollectTracing2 */:
                return parseEventPipeCollectTracing2(cmd);
            default:
                console.warn("MONO_WASM: unexpected EventPipe command: " + cmd.command);
                return { success: false, error: `unexpected EventPipe command ${cmd.command}` };
        }
    }
    function parseEventPipeCollectTracing2(cmd) {
        const pos = { pos: 0 };
        const buf = cmd.payload;
        const circularBufferMB = Parser.tryParseUint32(buf, pos);
        if (circularBufferMB === undefined) {
            return { success: false, error: "failed to parse circularBufferMB in EventPipe CollectTracing2 command" };
        }
        const format = Parser.tryParseUint32(buf, pos);
        if (format === undefined) {
            return { success: false, error: "failed to parse format in EventPipe CollectTracing2 command" };
        }
        const requestRundown = Parser.tryParseBool(buf, pos);
        if (requestRundown === undefined) {
            return { success: false, error: "failed to parse requestRundown in EventPipe CollectTracing2 command" };
        }
        const numProviders = Parser.tryParseArraySize(buf, pos);
        if (numProviders === undefined) {
            return { success: false, error: "failed to parse numProviders in EventPipe CollectTracing2 command" };
        }
        const providers = new Array(numProviders);
        for (let i = 0; i < numProviders; i++) {
            const result = parseEventPipeCollectTracingCommandProvider(buf, pos);
            if (!result.success) {
                return result;
            }
            providers[i] = result.result;
        }
        const command = { command_set: "EventPipe", command: "CollectTracing2", circularBufferMB, format, requestRundown, providers };
        return { success: true, result: command };
    }
    function parseEventPipeCollectTracingCommandProvider(buf, pos) {
        const keywords = Parser.tryParseUint64(buf, pos);
        if (keywords === undefined) {
            return { success: false, error: "failed to parse keywords in EventPipe CollectTracing provider" };
        }
        const logLevel = Parser.tryParseUint32(buf, pos);
        if (logLevel === undefined)
            return { success: false, error: "failed to parse logLevel in EventPipe CollectTracing provider" };
        const providerName = Parser.tryParseUtf16String(buf, pos);
        if (providerName === undefined)
            return { success: false, error: "failed to parse providerName in EventPipe CollectTracing provider" };
        const filterData = Parser.tryParseUtf16String(buf, pos);
        if (filterData === undefined)
            return { success: false, error: "failed to parse filterData in EventPipe CollectTracing provider" };
        const provider = { keywords, logLevel, provider_name: providerName, filter_data: filterData };
        return { success: true, result: provider };
    }
    function parseEventPipeStopTracing(cmd) {
        const pos = { pos: 0 };
        const buf = cmd.payload;
        const sessionID = Parser.tryParseUint64(buf, pos);
        if (sessionID === undefined) {
            return { success: false, error: "failed to parse sessionID in EventPipe StopTracing command" };
        }
        const [lo, hi] = sessionID;
        if (hi !== 0) {
            return { success: false, error: "sessionID is too large in EventPipe StopTracing command" };
        }
        const command = { command_set: "EventPipe", command: "StopTracing", sessionID: lo };
        return { success: true, result: command };
    }
    function parseProcessCommand(cmd) {
        switch (cmd.command) {
            case 0 /* ProcessCommandId.ProcessInfo */:
                throw new Error("TODO");
            case 1 /* ProcessCommandId.ResumeRuntime */:
                return parseProcessResumeRuntime(cmd);
            case 2 /* ProcessCommandId.ProcessEnvironment */:
                throw new Error("TODO");
            case 4 /* ProcessCommandId.ProcessInfo2 */:
                throw new Error("TODO");
            default:
                console.warn("MMONO_WASM: unexpected Process command: " + cmd.command);
                return { success: false, error: `unexpected Process command ${cmd.command}` };
        }
    }
    function parseProcessResumeRuntime(cmd) {
        const buf = cmd.payload;
        if (buf.byteLength !== 0) {
            return { success: false, error: "unexpected payload in Process ResumeRuntime command" };
        }
        const command = { command_set: "Process", command: "ResumeRuntime" };
        return { success: true, result: command };
    }

    // Licensed to the .NET Foundation under one or more agreements.
    function createBinaryCommandOKReply(payload) {
        const len = Serializer.computeMessageByteLength(payload);
        const buf = new Uint8Array(len);
        const pos = { pos: 0 };
        Serializer.serializeHeader(buf, pos, 255 /* CommandSetId.Server */, 0 /* ServerCommandId.OK */, len);
        if (payload !== undefined) {
            Serializer.serializePayload(buf, pos, payload);
        }
        return buf;
    }
    function serializeGuid(buf, pos, guid) {
        guid.split("-").forEach((part) => {
            // FIXME: I'm sure the endianness is wrong here
            for (let i = 0; i < part.length; i += 2) {
                const idx = part.length - i - 2; // go through the pieces backwards
                buf[pos.pos++] = Number.parseInt(part.substring(idx, idx + 2), 16);
            }
        });
    }
    function serializeAsciiLiteralString(buf, pos, s) {
        const len = s.length;
        const hasNul = s[len - 1] === "\0";
        for (let i = 0; i < len; i++) {
            Serializer.serializeUint8(buf, pos, s.charCodeAt(i));
        }
        if (!hasNul) {
            Serializer.serializeUint8(buf, pos, 0);
        }
    }
    function createAdvertise(guid, processId) {
        const BUF_LENGTH = 34;
        const buf = new Uint8Array(BUF_LENGTH);
        const pos = { pos: 0 };
        const advrText = "ADVR_V1\0";
        serializeAsciiLiteralString(buf, pos, advrText);
        serializeGuid(buf, pos, guid);
        Serializer.serializeUint64(buf, pos, processId);
        Serializer.serializeUint16(buf, pos, 0); // reserved
        if (!(pos.pos == BUF_LENGTH)) throw new Error("Assert failed: did not format ADVR_V1 correctly"); // inlined mono_assert
        return buf;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    function addOneShotProtocolCommandEventListener(src) {
        return new Promise((resolve) => {
            const listener = (event) => { resolve(event); };
            src.addEventListener(dotnetDiagnosticsServerProtocolCommandEvent, listener, { once: true });
        });
    }
    function addOneShotOpenEventListenr(src) {
        return new Promise((resolve) => {
            const listener = (event) => { resolve(event); };
            src.addEventListener("open", listener, { once: true });
        });
    }
    class DiagnosticServerImpl {
        constructor(websocketUrl, mockPromise) {
            this.runtimeResumed = false;
            this.startRequestedController = createPromiseController().promise_control;
            this.stopRequested = false;
            this.stopRequestedController = createPromiseController().promise_control;
            this.attachToRuntimeController = createPromiseController().promise_control;
            this.openCount = 0;
            this.websocketUrl = websocketUrl;
            pthread_self.addEventListenerFromBrowser(this.onMessageFromMainThread.bind(this));
            this.mocked = monoDiagnosticsMock ? mockPromise : undefined;
        }
        start() {
            console.log(`MONO_WASM: starting diagnostic server with url: ${this.websocketUrl}`);
            this.startRequestedController.resolve();
        }
        stop() {
            this.stopRequested = true;
            this.stopRequestedController.resolve();
        }
        attachToRuntime() {
            wrapped_c_functions.mono_wasm_diagnostic_server_thread_attach_to_runtime();
            this.attachToRuntimeController.resolve();
        }
        async serverLoop() {
            await this.startRequestedController.promise;
            await this.attachToRuntimeController.promise; // can't start tracing until we've attached to the runtime
            while (!this.stopRequested) {
                console.debug("MONO_WASM: diagnostic server: advertising and waiting for client");
                const p1 = this.advertiseAndWaitForClient().then(() => "first");
                const p2 = this.stopRequestedController.promise.then(() => "second");
                const result = await Promise.race([p1, p2]);
                switch (result) {
                    case "first":
                        break;
                    case "second":
                        console.debug("MONO_WASM: stop requested");
                        break;
                    default:
                        assertNever(result);
                }
            }
        }
        async openSocket() {
            if (monoDiagnosticsMock && this.mocked) {
                return (await this.mocked).open();
            }
            else {
                const sock = new WebSocket(this.websocketUrl);
                // TODO: add an "error" handler here - if we get readyState === 3, the connection failed.
                await addOneShotOpenEventListenr(sock);
                return sock;
            }
        }
        async advertiseAndWaitForClient() {
            try {
                const connNum = this.openCount++;
                console.debug("MONO_WASM: opening websocket and sending ADVR_V1", connNum);
                const ws = await this.openSocket();
                const p = addOneShotProtocolCommandEventListener(createProtocolSocket(ws));
                this.sendAdvertise(ws);
                const message = await p;
                console.debug("MONO_WASM: received advertising response: ", message, connNum);
                queueMicrotask(() => this.parseAndDispatchMessage(ws, connNum, message));
            }
            finally {
                // if there were errors, resume the runtime anyway
                this.resumeRuntime();
            }
        }
        async parseAndDispatchMessage(ws, connNum, message) {
            try {
                const cmd = this.parseCommand(message, connNum);
                if (cmd === null) {
                    console.error("MONO_WASM: unexpected message from client", message, connNum);
                    return;
                }
                else if (isEventPipeCommand(cmd)) {
                    await this.dispatchEventPipeCommand(ws, cmd);
                }
                else if (isProcessCommand(cmd)) {
                    await this.dispatchProcessCommand(ws, cmd); // resume
                }
                else {
                    console.warn("MONO_WASM Client sent unknown command", cmd);
                }
            }
            finally {
                // if there were errors, resume the runtime anyway
                this.resumeRuntime();
            }
        }
        sendAdvertise(ws) {
            /* FIXME: don't use const fake guid and fake process id. In dotnet-dsrouter the pid is used
             * as a dictionary key,so if we ever supprt multiple runtimes, this might need to change.
            */
            const guid = "C979E170-B538-475C-BCF1-B04A30DA1430";
            const processIdLo = 0;
            const processIdHi = 1234;
            const buf = createAdvertise(guid, [processIdLo, processIdHi]);
            ws.send(buf);
        }
        parseCommand(message, connNum) {
            console.debug("MONO_WASM: parsing byte command: ", message.data, connNum);
            const result = parseProtocolCommand(message.data);
            console.debug("MONO_WASM: parsied byte command: ", result, connNum);
            if (result.success) {
                return result.result;
            }
            else {
                console.warn("MONO_WASM: failed to parse command: ", result.error, connNum);
                return null;
            }
        }
        onMessageFromMainThread(event) {
            const d = event.data;
            if (d && isDiagnosticMessage(d)) {
                this.controlCommandReceived(d);
            }
        }
        /// dispatch commands received from the main thread
        controlCommandReceived(cmd) {
            switch (cmd.cmd) {
                case "start":
                    this.start();
                    break;
                case "stop":
                    this.stop();
                    break;
                case "attach_to_runtime":
                    this.attachToRuntime();
                    break;
                default:
                    console.warn("MONO_WASM: Unknown control command: ", cmd);
                    break;
            }
        }
        // dispatch EventPipe commands received from the diagnostic client
        async dispatchEventPipeCommand(ws, cmd) {
            if (isEventPipeCommandCollectTracing2(cmd)) {
                await this.collectTracingEventPipe(ws, cmd);
            }
            else if (isEventPipeCommandStopTracing(cmd)) {
                await this.stopEventPipe(ws, cmd.sessionID);
            }
            else {
                console.warn("MONO_WASM: unknown EventPipe command: ", cmd);
            }
        }
        postClientReplyOK(ws, payload) {
            // FIXME: send a binary response for non-mock sessions!
            ws.send(createBinaryCommandOKReply(payload));
        }
        async stopEventPipe(ws, sessionID) {
            console.debug("MONO_WASM: stopEventPipe", sessionID);
            wrapped_c_functions.mono_wasm_event_pipe_session_disable(sessionID);
            // we might send OK before the session is actually stopped since the websocket is async
            // but the client end should be robust to that.
            this.postClientReplyOK(ws);
        }
        async collectTracingEventPipe(ws, cmd) {
            const session = await makeEventPipeStreamingSession(ws, cmd);
            const sessionIDbuf = new Uint8Array(8); // 64 bit
            sessionIDbuf[0] = session.sessionID & 0xFF;
            sessionIDbuf[1] = (session.sessionID >> 8) & 0xFF;
            sessionIDbuf[2] = (session.sessionID >> 16) & 0xFF;
            sessionIDbuf[3] = (session.sessionID >> 24) & 0xFF;
            // sessionIDbuf[4..7] is 0 because all our session IDs are 32-bit
            this.postClientReplyOK(ws, sessionIDbuf);
            console.debug("MONO_WASM: created session, now streaming: ", session);
            wrapped_c_functions.mono_wasm_event_pipe_session_start_streaming(session.sessionID);
        }
        // dispatch Process commands received from the diagnostic client
        async dispatchProcessCommand(ws, cmd) {
            if (isProcessCommandResumeRuntime(cmd)) {
                this.processResumeRuntime(ws);
            }
            else {
                console.warn("MONO_WASM: unknown Process command", cmd);
            }
        }
        processResumeRuntime(ws) {
            this.postClientReplyOK(ws);
            this.resumeRuntime();
        }
        resumeRuntime() {
            if (!this.runtimeResumed) {
                console.info("MONO_WASM: resuming runtime startup");
                wrapped_c_functions.mono_wasm_diagnostic_server_post_resume_runtime();
                this.runtimeResumed = true;
            }
        }
    }
    function parseProtocolCommand(data) {
        if (isBinaryProtocolCommand(data)) {
            return parseBinaryProtocolCommand(data);
        }
        else {
            throw new Error("binary blob from mock is not implemented");
        }
    }
    /// Called by the runtime  to initialize the diagnostic server workers
    function mono_wasm_diagnostic_server_on_server_thread_created(websocketUrlPtr) {
        const websocketUrl = Module.UTF8ToString(websocketUrlPtr);
        console.debug(`mono_wasm_diagnostic_server_on_server_thread_created, url ${websocketUrl}`);
        let mock = undefined;
        if (monoDiagnosticsMock && websocketUrl.startsWith("mock:")) {
            mock = createPromiseController();
            queueMicrotask(async () => {
                const m = await importAndInstantiateMock(websocketUrl);
                mock.promise_control.resolve(m);
                m.run();
            });
        }
        const server = new DiagnosticServerImpl(websocketUrl, mock === null || mock === void 0 ? void 0 : mock.promise);
        queueMicrotask(() => {
            server.serverLoop();
        });
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // the methods would be visible to EMCC linker
    // --- keep in sync with dotnet.cjs.lib.js ---
    const mono_wasm_threads_exports = !MonoWasmThreads ? undefined : {
        // mono-threads-wasm.c
        mono_wasm_pthread_on_pthread_attached,
        // diagnostics_server.c
        mono_wasm_diagnostic_server_on_server_thread_created,
        mono_wasm_diagnostic_server_on_runtime_server_init,
        mono_wasm_diagnostic_server_stream_signal_work_available,
    };
    // the methods would be visible to EMCC linker
    // --- keep in sync with dotnet.cjs.lib.js ---
    // --- keep in sync with dotnet.es6.lib.js ---
    function export_linker() {
        return {
            // mini-wasm.c
            mono_set_timeout,
            // mini-wasm-debugger.c
            mono_wasm_asm_loaded,
            mono_wasm_fire_debugger_agent_message,
            mono_wasm_debugger_log,
            mono_wasm_add_dbg_command_received,
            // mono-threads-wasm.c
            schedule_background_exec,
            // also keep in sync with driver.c
            mono_wasm_invoke_js_blazor,
            mono_wasm_trace_logger,
            mono_wasm_set_entrypoint_breakpoint,
            mono_wasm_event_pipe_early_startup_callback,
            // also keep in sync with corebindings.c
            mono_wasm_invoke_js_with_args_ref,
            mono_wasm_get_object_property_ref,
            mono_wasm_set_object_property_ref,
            mono_wasm_get_by_index_ref,
            mono_wasm_set_by_index_ref,
            mono_wasm_get_global_object_ref,
            mono_wasm_create_cs_owned_object_ref,
            mono_wasm_release_cs_owned_object,
            mono_wasm_typed_array_to_array_ref,
            mono_wasm_typed_array_from_ref,
            mono_wasm_bind_js_function,
            mono_wasm_invoke_bound_function,
            mono_wasm_bind_cs_function,
            mono_wasm_marshal_promise,
            //  also keep in sync with pal_icushim_static.c
            mono_wasm_load_icu_data,
            mono_wasm_get_icudt_name,
            // threading exports, if threading is enabled
            ...mono_wasm_threads_exports,
        };
    }

    // Licensed to the .NET Foundation under one or more agreements.
    class HostBuilder {
        constructor() {
            this.moduleConfig = {
                disableDotnet6Compatibility: true,
                configSrc: "./mono-config.json",
                config: runtimeHelpers.config,
            };
        }
        withModuleConfig(moduleConfig) {
            try {
                Object.assign(this.moduleConfig, moduleConfig);
                return this;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        withConsoleForwarding() {
            try {
                const configInternal = {
                    forwardConsoleLogsToWS: true
                };
                Object.assign(this.moduleConfig.config, configInternal);
                return this;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        withAsyncFlushOnExit() {
            try {
                const configInternal = {
                    asyncFlushOnExit: true
                };
                Object.assign(this.moduleConfig.config, configInternal);
                return this;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        withExitCodeLogging() {
            try {
                const configInternal = {
                    logExitCode: true
                };
                Object.assign(this.moduleConfig.config, configInternal);
                return this;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        withElementOnExit() {
            try {
                const configInternal = {
                    appendElementOnExit: true
                };
                Object.assign(this.moduleConfig.config, configInternal);
                return this;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        //  todo fallback later by debugLevel
        withWaitingForDebugger(level) {
            try {
                const configInternal = {
                    waitForDebugger: level
                };
                Object.assign(this.moduleConfig.config, configInternal);
                return this;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        withConfig(config) {
            try {
                const providedConfig = { ...config };
                providedConfig.assets = [...(this.moduleConfig.config.assets || []), ...(providedConfig.assets || [])];
                providedConfig.environmentVariables = { ...(this.moduleConfig.config.environmentVariables || {}), ...(providedConfig.environmentVariables || {}) };
                Object.assign(this.moduleConfig.config, providedConfig);
                return this;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        withConfigSrc(configSrc) {
            try {
                if (!(configSrc && typeof configSrc === "string")) throw new Error("Assert failed: must be file path or URL"); // inlined mono_assert
                Object.assign(this.moduleConfig, { configSrc });
                return this;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        withVirtualWorkingDirectory(vfsPath) {
            try {
                if (!(vfsPath && typeof vfsPath === "string")) throw new Error("Assert failed: must be directory path"); // inlined mono_assert
                this.virtualWorkingDirectory = vfsPath;
                return this;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        withEnvironmentVariable(name, value) {
            try {
                this.moduleConfig.config.environmentVariables[name] = value;
                return this;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        withEnvironmentVariables(variables) {
            try {
                if (!(variables && typeof variables === "object")) throw new Error("Assert failed: must be dictionary object"); // inlined mono_assert
                Object.assign(this.moduleConfig.config.environmentVariables, variables);
                return this;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        withDiagnosticTracing(enabled) {
            try {
                if (!(typeof enabled === "boolean")) throw new Error("Assert failed: must be boolean"); // inlined mono_assert
                this.moduleConfig.config.diagnosticTracing = enabled;
                return this;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        withDebugging(level) {
            try {
                if (!(level && typeof level === "number")) throw new Error("Assert failed: must be number"); // inlined mono_assert
                this.moduleConfig.config.debugLevel = level;
                return this;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        withApplicationArguments(...args) {
            try {
                if (!(args && Array.isArray(args))) throw new Error("Assert failed: must be array of strings"); // inlined mono_assert
                this.applicationArguments = args;
                return this;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        withRuntimeOptions(runtimeOptions) {
            try {
                if (!(runtimeOptions && Array.isArray(runtimeOptions))) throw new Error("Assert failed: must be array of strings"); // inlined mono_assert
                Object.assign(this.moduleConfig, { runtimeOptions });
                return this;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        withMainAssembly(mainAssemblyName) {
            try {
                this.moduleConfig.config.mainAssemblyName = mainAssemblyName;
                return this;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        withApplicationArgumentsFromQuery() {
            try {
                if (typeof globalThis.URLSearchParams != "undefined") {
                    const params = new URLSearchParams(window.location.search);
                    const values = params.getAll("arg");
                    return this.withApplicationArguments(...values);
                }
                throw new Error("URLSearchParams is supported");
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        async create() {
            try {
                if (!this.instance) {
                    if (ENVIRONMENT_IS_WEB && !ENVIRONMENT_IS_PTHREAD && this.moduleConfig.config.forwardConsoleLogsToWS && typeof globalThis.WebSocket != "undefined") {
                        setup_proxy_console("main", globalThis.console, globalThis.location.origin);
                    }
                    if (ENVIRONMENT_IS_NODE) {
                        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
                        // @ts-ignore:
                        const process = await import(/* webpackIgnore: true */ 'process');
                        if (process.versions.node.split(".")[0] < 14) {
                            throw new Error(`NodeJS at '${process.execPath}' has too low version '${process.versions.node}'`);
                        }
                    }
                    if (!(this.moduleConfig)) throw new Error("Assert failed: Null moduleConfig"); // inlined mono_assert
                    if (!(this.moduleConfig.config)) throw new Error("Assert failed: Null moduleConfig.config"); // inlined mono_assert
                    this.instance = await emscriptenEntrypoint(this.moduleConfig);
                }
                if (this.virtualWorkingDirectory) {
                    const FS = this.instance.Module.FS;
                    const wds = FS.stat(this.virtualWorkingDirectory);
                    if (!(wds && FS.isDir(wds.mode))) throw new Error(`Assert failed: Could not find working directory ${this.virtualWorkingDirectory}`); // inlined mono_assert
                    FS.chdir(this.virtualWorkingDirectory);
                }
                return this.instance;
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
        async run() {
            try {
                if (!(this.moduleConfig.config)) throw new Error("Assert failed: Null moduleConfig.config"); // inlined mono_assert
                if (!this.instance) {
                    await this.create();
                }
                if (!(this.moduleConfig.config.mainAssemblyName)) throw new Error("Assert failed: Null moduleConfig.config.mainAssemblyName"); // inlined mono_assert
                if (!this.applicationArguments) {
                    if (ENVIRONMENT_IS_NODE) {
                        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
                        // @ts-ignore:
                        const process = await import(/* webpackIgnore: true */ 'process');
                        this.applicationArguments = process.argv.slice(2);
                    }
                    else {
                        this.applicationArguments = [];
                    }
                }
                return this.instance.runMainAndExit(this.moduleConfig.config.mainAssemblyName, this.applicationArguments);
            }
            catch (err) {
                mono_exit(1, err);
                throw err;
            }
        }
    }
    const dotnet = new HostBuilder();

    // Licensed to the .NET Foundation under one or more agreements.
    function export_api() {
        const api = {
            runMain: mono_run_main,
            runMainAndExit: mono_run_main_and_exit,
            setEnvironmentVariable: mono_wasm_setenv,
            getAssemblyExports: mono_wasm_get_assembly_exports,
            setModuleImports: mono_wasm_set_module_imports,
            getConfig: () => {
                return runtimeHelpers.config;
            },
            setHeapB32: setB32,
            setHeapU8: setU8,
            setHeapU16: setU16,
            setHeapU32: setU32,
            setHeapI8: setI8,
            setHeapI16: setI16,
            setHeapI32: setI32,
            setHeapI52: setI52,
            setHeapU52: setU52,
            setHeapI64Big: setI64Big,
            setHeapF32: setF32,
            setHeapF64: setF64,
            getHeapB32: getB32,
            getHeapU8: getU8,
            getHeapU16: getU16,
            getHeapU32: getU32,
            getHeapI8: getI8,
            getHeapI16: getI16,
            getHeapI32: getI32,
            getHeapI52: getI52,
            getHeapU52: getU52,
            getHeapI64Big: getI64Big,
            getHeapF32: getF32,
            getHeapF64: getF64,
        };
        return api;
    }
    function export_module() {
        const exports = {
            dotnet,
            exit: mono_exit
        };
        return exports;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const __initializeImportsAndExports = initializeImportsAndExports; // don't want to export the type
    const __setEmscriptenEntrypoint = setEmscriptenEntrypoint; // don't want to export the type
    exports.__linker_exports = null;
    // this is executed early during load of emscripten runtime
    // it exports methods to global objects MONO, BINDING and Module in backward compatible way
    // At runtime this will be referred to as 'createDotnetRuntime'
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function initializeImportsAndExports(imports, exports$1, replacements, callbackAPI) {
        const module = exports$1.module;
        const globalThisAny = globalThis;
        // we want to have same instance of MONO, BINDING and Module in dotnet iffe
        set_imports_exports(imports, exports$1);
        set_legacy_exports(exports$1);
        init_polyfills(replacements);
        // here we merge methods from the local objects into exported objects
        Object.assign(exports$1.mono, export_mono_api());
        Object.assign(exports$1.binding, export_binding_api());
        Object.assign(exports$1.internal, export_internal());
        Object.assign(exports$1.internal, export_internal());
        const API = export_api();
        exports.__linker_exports = export_linker();
        Object.assign(exportedRuntimeAPI, {
            MONO: exports$1.mono,
            BINDING: exports$1.binding,
            INTERNAL: exports$1.internal,
            IMPORTS: exports$1.marshaled_imports,
            Module: module,
            runtimeBuildInfo: {
                productVersion: ProductVersion,
                buildConfiguration: BuildConfiguration
            },
            ...API,
        });
        Object.assign(callbackAPI, API);
        if (exports$1.module.__undefinedConfig) {
            module.disableDotnet6Compatibility = true;
            module.configSrc = "./mono-config.json";
        }
        if (!module.print) {
            module.print = console.log.bind(console);
        }
        if (!module.printErr) {
            module.printErr = console.error.bind(console);
        }
        if (typeof module.disableDotnet6Compatibility === "undefined") {
            module.disableDotnet6Compatibility = true;
        }
        // here we expose objects global namespace for tests and backward compatibility
        if (imports.isGlobal || !module.disableDotnet6Compatibility) {
            Object.assign(module, exportedRuntimeAPI);
            // backward compatibility
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
            module.mono_bind_static_method = (fqn, signature /*ArgsMarshalString*/) => {
                console.warn("MONO_WASM: Module.mono_bind_static_method is obsolete, please use [JSExportAttribute] interop instead");
                return mono_bind_static_method(fqn, signature);
            };
            const warnWrap = (name, provider) => {
                if (typeof globalThisAny[name] !== "undefined") {
                    // it already exists in the global namespace
                    return;
                }
                let value = undefined;
                Object.defineProperty(globalThis, name, {
                    get: () => {
                        if (is_nullish(value)) {
                            const stack = (new Error()).stack;
                            const nextLine = stack ? stack.substr(stack.indexOf("\n", 8) + 1) : "";
                            console.warn(`MONO_WASM: global ${name} is obsolete, please use Module.${name} instead ${nextLine}`);
                            value = provider();
                        }
                        return value;
                    }
                });
            };
            globalThisAny.MONO = exports$1.mono;
            globalThisAny.BINDING = exports$1.binding;
            globalThisAny.INTERNAL = exports$1.internal;
            if (!imports.isGlobal) {
                globalThisAny.Module = module;
            }
            // Blazor back compat
            warnWrap("cwrap", () => module.cwrap);
            warnWrap("addRunDependency", () => module.addRunDependency);
            warnWrap("removeRunDependency", () => module.removeRunDependency);
        }
        // this code makes it possible to find dotnet runtime on a page via global namespace, even when there are multiple runtimes at the same time
        let list;
        if (!globalThisAny.getDotnetRuntime) {
            globalThisAny.getDotnetRuntime = (runtimeId) => globalThisAny.getDotnetRuntime.__list.getRuntime(runtimeId);
            globalThisAny.getDotnetRuntime.__list = list = new RuntimeList();
        }
        else {
            list = globalThisAny.getDotnetRuntime.__list;
        }
        list.registerRuntime(exportedRuntimeAPI);
        if (MonoWasmThreads && ENVIRONMENT_IS_PTHREAD) {
            // eslint-disable-next-line no-inner-declarations
            async function workerInit() {
                await mono_wasm_pthread_worker_init();
                // HACK: Emscripten's dotnet.worker.js expects the exports of dotnet.js module to be Module object
                // until we have our own fix for dotnet.worker.js file
                // we also skip all emscripten startup event and configuration of worker's JS state
                // note that emscripten events are not firing either
                return exportedRuntimeAPI.Module;
            }
            // Emscripten pthread worker.js is ok with a Promise here.
            return workerInit();
        }
        configure_emscripten_startup(module, exportedRuntimeAPI);
        return exportedRuntimeAPI;
    }
    class RuntimeList {
        constructor() {
            this.list = {};
        }
        registerRuntime(api) {
            api.runtimeId = Object.keys(this.list).length;
            this.list[api.runtimeId] = create_weak_ref(api);
            return api.runtimeId;
        }
        getRuntime(runtimeId) {
            const wr = this.list[runtimeId];
            return wr ? wr.deref() : undefined;
        }
    }
    function setEmscriptenEntrypoint(emscriptenEntrypoint, env) {
        set_environment(env);
        Object.assign(moduleExports, export_module());
        set_emscripten_entrypoint(emscriptenEntrypoint);
    }

    exports.__initializeImportsAndExports = __initializeImportsAndExports;
    exports.__setEmscriptenEntrypoint = __setEmscriptenEntrypoint;
    exports.moduleExports = moduleExports;

    Object.defineProperty(exports, '__esModule', { value: true });

    return exports;

})({});