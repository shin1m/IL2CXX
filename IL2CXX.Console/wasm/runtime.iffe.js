//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

var __dotnet_runtime = (function (exports) {
    'use strict';

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    // these are our public API (except internal)
    let Module;
    let MONO$1;
    let BINDING$1;
    let INTERNAL$1;
    // these are imported and re-exported from emscripten internals
    let ENVIRONMENT_IS_GLOBAL;
    let ENVIRONMENT_IS_NODE;
    let ENVIRONMENT_IS_SHELL;
    let ENVIRONMENT_IS_WEB;
    let locateFile;
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function setImportsAndExports(imports, exports) {
        MONO$1 = exports.mono;
        BINDING$1 = exports.binding;
        INTERNAL$1 = exports.internal;
        Module = exports.module;
        ENVIRONMENT_IS_GLOBAL = imports.isGlobal;
        ENVIRONMENT_IS_NODE = imports.isNode;
        ENVIRONMENT_IS_SHELL = imports.isShell;
        ENVIRONMENT_IS_WEB = imports.isWeb;
        locateFile = imports.locateFile;
    }
    let monoConfig;
    let runtime_is_ready = false;
    const runtimeHelpers = {
        namespace: "System.Runtime.InteropServices.JavaScript",
        classname: "Runtime",
        get mono_wasm_runtime_is_ready() {
            return runtime_is_ready;
        },
        set mono_wasm_runtime_is_ready(value) {
            runtime_is_ready = value;
            INTERNAL$1.mono_wasm_runtime_is_ready = value;
        },
        get config() {
            return monoConfig;
        },
        set config(value) {
            monoConfig = value;
            MONO$1.config = value;
            Module.config = value;
        },
    };

    // Licensed to the .NET Foundation under one or more agreements.
    const fn_signatures$1 = [
        // MONO
        ["il2cxx_wasm_slot_set", null, ["number", "number"]],
        ["mono_wasm_string_get_data", null, ["number", "number", "number", "number"]],
        ["mono_wasm_set_is_debugger_attached", "void", ["bool"]],
        ["mono_wasm_send_dbg_command", "bool", ["number", "number", "number", "number", "number"]],
        ["mono_wasm_send_dbg_command_with_parms", "bool", ["number", "number", "number", "number", "number", "number", "string"]],
        ["mono_wasm_setenv", null, ["string", "string"]],
        ["mono_wasm_parse_runtime_options", null, ["number", "number"]],
        ["mono_wasm_strdup", "number", ["string"]],
        ["mono_background_exec", null, []],
        ["mono_set_timeout_exec", null, ["number"]],
        ["mono_wasm_load_icu_data", "number", ["number"]],
        ["mono_wasm_get_icudt_name", "string", ["string"]],
        ["mono_wasm_add_assembly", "number", ["string", "number", "number"]],
        ["mono_wasm_add_satellite_assembly", "void", ["string", "string", "number", "number"]],
        ["mono_wasm_load_runtime", null, ["string", "number"]],
        ["mono_wasm_exit", null, ["number"]],
        // BINDING
        ["mono_wasm_get_corlib", "number", []],
        ["mono_wasm_assembly_load", "number", ["string"]],
        ["mono_wasm_find_corlib_class", "number", ["string", "string"]],
        ["mono_wasm_assembly_find_class", "number", ["number", "string", "string"]],
        ["mono_wasm_find_corlib_type", "number", ["string", "string"]],
        ["mono_wasm_assembly_find_type", "number", ["number", "string", "string"]],
        ["mono_wasm_assembly_find_method", "number", ["number", "string", "number"]],
        ["mono_wasm_invoke_method", "number", ["number", "number", "number", "number"]],
        ["mono_wasm_string_get_utf8", "number", ["number"]],
        ["mono_wasm_string_from_utf16", "number", ["number", "number"]],
        ["mono_wasm_get_obj_type", "number", ["number"]],
        ["mono_wasm_array_length", "number", ["number"]],
        ["mono_wasm_array_get", "number", ["number", "number"]],
        ["mono_wasm_obj_array_new", "number", ["number"]],
        ["mono_wasm_obj_array_set", "void", ["number", "number", "number"]],
        ["mono_wasm_register_bundled_satellite_assemblies", "void", []],
        ["mono_wasm_try_unbox_primitive_and_get_type", "number", ["number", "number", "number"]],
        ["mono_wasm_box_primitive", "number", ["number", "number", "number"]],
        ["mono_wasm_intern_string", "number", ["number"]],
        ["mono_wasm_assembly_get_entry_point", "number", ["number"]],
        ["mono_wasm_get_delegate_invoke", "number", ["number"]],
        ["mono_wasm_string_array_new", "number", ["number"]],
        ["mono_wasm_typed_array_new", "number", ["number", "number", "number", "number"]],
        ["mono_wasm_class_get_type", "number", ["number"]],
        ["mono_wasm_type_get_class", "number", ["number"]],
        ["mono_wasm_get_type_name", "string", ["number"]],
        ["mono_wasm_get_type_aqn", "string", ["number"]],
        ["mono_wasm_unbox_rooted", "number", ["number"]],
        //DOTNET
        ["mono_wasm_string_from_js", "number", ["string"]],
        //INTERNAL
        ["mono_wasm_exit", "void", ["number"]],
        ["mono_wasm_set_main_args", "void", ["number", "number"]],
        ["mono_wasm_enable_on_demand_gc", "void", ["number"]],
        ["mono_profiler_init_aot", "void", ["number"]],
        ["mono_wasm_exec_regression", "number", ["number", "string"]],
    ];
    const wrapped_c_functions = {};
    for (const sig of fn_signatures$1) {
        const wf = wrapped_c_functions;
        // lazy init on first run
        wf[sig[0]] = function (...args) {
            const fce = Module.cwrap(sig[0], sig[1], sig[2], sig[3]);
            wf[sig[0]] = fce;
            return fce(...args);
        };
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const maxScratchRoots = 8192;
    let _scratch_root_buffer = null;
    let _scratch_root_free_indices = null;
    let _scratch_root_free_indices_count = 0;
    const _scratch_root_free_instances = [];
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
        return new WasmRootBuffer(offset, capacity, true, name);
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
        return new WasmRootBuffer(offset, capacity, false, name);
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
            result = new WasmRoot(buffer, index);
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
            if (!args[i])
                continue;
            args[i].release();
        }
    }
    function _zero_region(byteOffset, sizeBytes) {
        sizeBytes += byteOffset;
        if (((byteOffset % 4) === 0) && ((sizeBytes % 4) === 0))
            Module.HEAP32.fill(0, byteOffset >>> 2, sizeBytes >>> 2);
        else
            Module.HEAP8.fill(0, byteOffset, sizeBytes);
    }
    function _mono_wasm_release_scratch_index(index) {
        if (index === undefined)
            return;
        _scratch_root_buffer.set(index, 0);
        _scratch_root_free_indices[_scratch_root_free_indices_count] = index;
        _scratch_root_free_indices_count++;
    }
    function _mono_wasm_claim_scratch_index() {
        if (!_scratch_root_buffer || !_scratch_root_free_indices) {
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
    class WasmRootBuffer {
        constructor(offset, capacity, ownsAllocation, name) {
            const capacityBytes = capacity * 4;
            this.__offset = offset;
            this.__offset32 = offset >>> 2;
            this.__count = capacity;
            this.length = capacity;
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
            return this._unsafe_get(index);
        }
        set(index, value) {
            this._check_in_range(index);
            this._unsafe_set(index, value);
            return value;
        }
        _unsafe_get(index) {
            return Module.HEAP32[this.__offset32 + index];
        }
        _unsafe_set(index, value) {
            wrapped_c_functions.il2cxx_wasm_slot_set(this.__offset + (index * 4), value);
        }
        clear() {
            if (!this.__offset)
                return;
            const q = this.__offset + this.__count * 4;
            for (let p = this.__offset; p < q; p += 4)
                wrapped_c_functions.il2cxx_wasm_slot_set(p, 0);
        }
        release() {
            this.clear();
            if (this.__offset && this.__ownsAllocation)
                Module._free(this.__offset);
        }
        toString() {
            return `[root buffer @${this.get_address(0)}, size ${this.__count} ]`;
        }
    }
    class WasmRoot {
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
        get() {
            const result = this.__buffer._unsafe_get(this.__index);
            return result;
        }
        set(value) {
            this.__buffer._unsafe_set(this.__index, value);
            return value;
        }
        get value() {
            return this.get();
        }
        set value(value) {
            this.set(value);
        }
        valueOf() {
            return this.get();
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
            return `[root @${this.get_address()}]`;
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
    let commands_received;
    let _call_function_res_cache = {};
    let _next_call_function_res_id = 0;
    let _debugger_buffer_len = -1;
    let _debugger_buffer;
    function mono_wasm_runtime_ready() {
        runtimeHelpers.mono_wasm_runtime_is_ready = true;
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
        commands_received = buffer_obj;
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
        const { res_ok, res } = commands_received;
        if (!res_ok)
            throw new Error("Failed on mono_wasm_invoke_method_debugger_agent_with_parms");
        return res;
    }
    function mono_wasm_send_dbg_command(id, command_set, command, command_parameters) {
        mono_wasm_malloc_and_set_debug_buffer(command_parameters);
        wrapped_c_functions.mono_wasm_send_dbg_command(id, command_set, command, _debugger_buffer, command_parameters.length);
        const { res_ok, res } = commands_received;
        if (!res_ok)
            throw new Error("Failed on mono_wasm_send_dbg_command");
        return res;
    }
    function mono_wasm_get_dbg_command_info() {
        const { res_ok, res } = commands_received;
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
    // Used by the debugger to enumerate loaded dlls and pdbs
    function mono_wasm_get_loaded_files() {
        wrapped_c_functions.mono_wasm_set_is_debugger_attached(true);
        return MONO$1.loaded_files;
    }
    function _create_proxy_from_object_id(objectId, details) {
        if (objectId.startsWith("dotnet:array:")) {
            let ret;
            if (details.dimensionsDetails == undefined || details.dimensionsDetails.length == 1) {
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
                        return mono_wasm_send_dbg_command(-1, prop.get.commandSet, prop.get.command, prop.get.buffer);
                    },
                    set: function (newValue) {
                        mono_wasm_send_dbg_command_with_parms(-1, prop.set.commandSet, prop.set.command, prop.set.buffer, prop.set.length, prop.set.valtype, newValue);
                        return commands_received.res_ok;
                    }
                });
            }
            else if (prop.set !== undefined) {
                Object.defineProperty(proxy, prop.name, {
                    get() {
                        return prop.value;
                    },
                    set: function (newValue) {
                        mono_wasm_send_dbg_command_with_parms(-1, prop.set.commandSet, prop.set.command, prop.set.buffer, prop.set.length, prop.set.valtype, newValue);
                        return commands_received.res_ok;
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
        const fn_body_template = `var fn = ${request.functionDeclaration}; return fn.apply(proxy, [${fn_args}]);`;
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

    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    const MonoMethodNull = 0;
    const MonoObjectNull = 0;
    const MonoArrayNull = 0;
    const MonoAssemblyNull = 0;
    const MonoClassNull = 0;
    const MonoTypeNull = 0;
    const MonoStringNull = 0;
    const JSHandleDisposed = -1;
    const JSHandleNull = 0;
    const VoidPtrNull = 0;
    const CharPtrNull = 0;
    function coerceNull(ptr) {
        return (ptr | 0);
    }
    const wasm_type_symbol = Symbol.for("wasm type");

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
    // @globalization_mode is one of "icu", "invariant", or "auto".
    // "auto" will use "icu" if any ICU data archives have been loaded,
    //  otherwise "invariant".
    function mono_wasm_globalization_init(globalization_mode) {
        let invariantMode = false;
        if (globalization_mode === "invariant")
            invariantMode = true;
        if (!invariantMode) {
            if (num_icu_assets_loaded_successfully > 0) {
                console.debug("MONO_WASM: ICU data archive(s) loaded, disabling invariant mode");
            }
            else if (globalization_mode !== "icu") {
                console.debug("MONO_WASM: ICU data archive(s) not loaded, using invariant globalization mode");
                invariantMode = true;
            }
            else {
                const msg = "invariant globalization mode is inactive and no ICU data archives were loaded";
                console.error(`MONO_WASM: ERROR: ${msg}`);
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
    // options = { write_at: "<METHODNAME>", send_to: "<METHODNAME>" }
    // <METHODNAME> should be in the format <CLASS>::<METHODNAME>.
    // write_at defaults to 'WebAssembly.Runtime::StopProfile'.
    // send_to defaults to 'WebAssembly.Runtime::DumpAotProfileData'.
    // DumpAotProfileData stores the data into INTERNAL.aot_profile_data.
    //
    function mono_wasm_init_aot_profiler(options) {
        if (options == null)
            options = {};
        if (!("write_at" in options))
            options.write_at = "Interop/Runtime::StopProfile";
        if (!("send_to" in options))
            options.send_to = "Interop/Runtime::DumpAotProfileData";
        const arg = "aot:write-at-method=" + options.write_at + ",send-to-method=" + options.send_to;
        Module.ccall("mono_wasm_load_profiler_aot", null, ["string"], [arg]);
    }
    // options = { write_at: "<METHODNAME>", send_to: "<METHODNAME>" }
    // <METHODNAME> should be in the format <CLASS>::<METHODNAME>.
    // write_at defaults to 'WebAssembly.Runtime::StopProfile'.
    // send_to defaults to 'WebAssembly.Runtime::DumpCoverageProfileData'.
    // DumpCoverageProfileData stores the data into INTERNAL.coverage_profile_data.
    function mono_wasm_init_coverage_profiler(options) {
        if (options == null)
            options = {};
        if (!("write_at" in options))
            options.write_at = "WebAssembly.Runtime::StopProfile";
        if (!("send_to" in options))
            options.send_to = "WebAssembly.Runtime::DumpCoverageProfileData";
        const arg = "coverage:write-at-method=" + options.write_at + ",send-to-method=" + options.send_to;
        Module.ccall("mono_wasm_load_profiler_coverage", null, ["string"], [arg]);
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const fn_signatures = [
        ["_get_cs_owned_object_by_js_handle", "GetCSOwnedObjectByJSHandle", "ii!"],
        ["_get_cs_owned_object_js_handle", "GetCSOwnedObjectJSHandle", "mi"],
        ["_try_get_cs_owned_object_js_handle", "TryGetCSOwnedObjectJSHandle", "mi"],
        ["_create_cs_owned_proxy", "CreateCSOwnedProxy", "iii!"],
        ["_get_js_owned_object_by_gc_handle", "GetJSOwnedObjectByGCHandle", "i!"],
        ["_get_js_owned_object_gc_handle", "GetJSOwnedObjectGCHandle", "m"],
        ["_release_js_owned_object_by_gc_handle", "ReleaseJSOwnedObjectByGCHandle", "i"],
        ["_create_tcs", "CreateTaskSource", ""],
        ["_set_tcs_result", "SetTaskSourceResult", "io"],
        ["_set_tcs_failure", "SetTaskSourceFailure", "is"],
        ["_get_tcs_task", "GetTaskSourceTask", "i!"],
        ["_task_from_result", "TaskFromResult", "o!"],
        ["_setup_js_cont", "SetupJSContinuation", "mo"],
        ["_object_to_string", "ObjectToString", "m"],
        ["_get_date_value", "GetDateValue", "m"],
        ["_create_date_time", "CreateDateTime", "d!"],
        ["_create_uri", "CreateUri", "s!"],
        ["_is_simple_array", "IsSimpleArray", "m"],
    ];
    const wrapped_cs_functions = {};
    for (const sig of fn_signatures) {
        const wf = wrapped_cs_functions;
        // lazy init on first run
        wf[sig[0]] = function (...args) {
            const fce = runtimeHelpers.bind_runtime_method(sig[1], sig[2]);
            wf[sig[0]] = fce;
            return fce(...args);
        };
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const _use_finalization_registry = typeof globalThis.FinalizationRegistry === "function";
    const _use_weak_ref = typeof globalThis.WeakRef === "function";
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
    function get_js_owned_object_by_gc_handle(gc_handle) {
        if (!gc_handle) {
            return MonoObjectNull;
        }
        // this is always strong gc_handle
        return wrapped_cs_functions._get_js_owned_object_by_gc_handle(gc_handle);
    }
    function mono_wasm_get_jsobj_from_js_handle(js_handle) {
        if (js_handle !== JSHandleNull && js_handle !== JSHandleDisposed)
            return _cs_owned_objects_by_js_handle[js_handle];
        return null;
    }
    // when should_add_in_flight === true, the JSObject would be temporarily hold by Normal gc_handle, so that it would not get collected during transition to the managed stack.
    // its InFlight gc_handle would be freed when the instance arrives to managed side via Interop.Runtime.ReleaseInFlight
    function get_cs_owned_object_by_js_handle(js_handle, should_add_in_flight) {
        if (js_handle === JSHandleNull || js_handle === JSHandleDisposed) {
            return MonoObjectNull;
        }
        return wrapped_cs_functions._get_cs_owned_object_by_js_handle(js_handle, should_add_in_flight ? 1 : 0);
    }
    function get_js_obj(js_handle) {
        if (js_handle !== JSHandleNull && js_handle !== JSHandleDisposed)
            return mono_wasm_get_jsobj_from_js_handle(js_handle);
        return null;
    }
    function _js_owned_object_finalized(gc_handle) {
        // The JS object associated with this gc_handle has been collected by the JS GC.
        // As such, it's not possible for this gc_handle to be invoked by JS anymore, so
        //  we can release the tracking weakref (it's null now, by definition),
        //  and tell the C# side to stop holding a reference to the managed object.
        // "The FinalizationRegistry callback is called potentially multiple times"
        if (_js_owned_object_table.delete(gc_handle)) {
            wrapped_cs_functions._release_js_owned_object_by_gc_handle(gc_handle);
        }
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
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function _register_js_owned_object(gc_handle, js_obj) {
        let wr;
        if (_use_weak_ref) {
            wr = new WeakRef(js_obj);
        }
        else {
            // this is trivial WeakRef replacement, which holds strong refrence, instead of weak one, when the browser doesn't support it
            wr = {
                deref: () => {
                    return js_obj;
                }
            };
        }
        _js_owned_object_table.set(gc_handle, wr);
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function mono_wasm_get_js_handle(js_obj) {
        if (js_obj[cs_owned_js_handle_symbol]) {
            return js_obj[cs_owned_js_handle_symbol];
        }
        const js_handle = _js_handle_free_list.length ? _js_handle_free_list.pop() : _next_js_handle++;
        // note _cs_owned_objects_by_js_handle is list, not Map. That's why we maintain _js_handle_free_list.
        _cs_owned_objects_by_js_handle[js_handle] = js_obj;
        js_obj[cs_owned_js_handle_symbol] = js_handle;
        return js_handle;
    }
    function mono_wasm_release_cs_owned_object(js_handle) {
        const obj = _cs_owned_objects_by_js_handle[js_handle];
        if (typeof obj !== "undefined" && obj !== null) {
            // if this is the global object then do not
            // unregister it.
            if (globalThis === obj)
                return obj;
            if (typeof obj[cs_owned_js_handle_symbol] !== "undefined") {
                obj[cs_owned_js_handle_symbol] = undefined;
            }
            _cs_owned_objects_by_js_handle[js_handle] = undefined;
            _js_handle_free_list.push(js_handle);
        }
        return obj;
    }

    const _temp_mallocs = [];
    function temp_malloc(size) {
        if (!_temp_mallocs || !_temp_mallocs.length)
            throw new Error("No temp frames have been created at this point");
        const frame = _temp_mallocs[_temp_mallocs.length - 1] || [];
        const result = Module._malloc(size);
        frame.push(result);
        _temp_mallocs[_temp_mallocs.length - 1] = frame;
        return result;
    }
    function _create_temp_frame() {
        _temp_mallocs.push(null);
    }
    function _release_temp_frame() {
        if (!_temp_mallocs.length)
            throw new Error("No temp frames have been created at this point");
        const frame = _temp_mallocs.pop();
        if (!frame)
            return;
        for (let i = 0, l = frame.length; i < l; i++)
            Module._free(frame[i]);
    }
    function setU8(offset, value) {
        Module.HEAPU8[offset] = value;
    }
    function setU16(offset, value) {
        Module.HEAPU16[offset >>> 1] = value;
    }
    function setU32(offset, value) {
        Module.HEAPU32[offset >>> 2] = value;
    }
    function setI8(offset, value) {
        Module.HEAP8[offset] = value;
    }
    function setI16(offset, value) {
        Module.HEAP16[offset >>> 1] = value;
    }
    function setI32(offset, value) {
        Module.HEAP32[offset >>> 2] = value;
    }
    // NOTE: Accepts a number, not a BigInt, so values over Number.MAX_SAFE_INTEGER will be corrupted
    function setI64(offset, value) {
        Module.setValue(offset, value, "i64");
    }
    function setF32(offset, value) {
        Module.HEAPF32[offset >>> 2] = value;
    }
    function setF64(offset, value) {
        Module.HEAPF64[offset >>> 3] = value;
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
    // NOTE: Returns a number, not a BigInt. This means values over Number.MAX_SAFE_INTEGER will be corrupted
    function getI64(offset) {
        return Module.getValue(offset, "i64");
    }
    function getF32(offset) {
        return Module.HEAPF32[offset >>> 2];
    }
    function getF64(offset) {
        return Module.HEAPF64[offset >>> 3];
    }

    // Licensed to the .NET Foundation under one or more agreements.
    class StringDecoder {
        copy(mono_string) {
            if (!this.mono_wasm_string_decoder_buffer) {
                this.mono_text_decoder = typeof TextDecoder !== "undefined" ? new TextDecoder("utf-16le") : null;
                this.mono_wasm_string_root = mono_wasm_new_root();
                this.mono_wasm_string_decoder_buffer = Module._malloc(12);
            }
            if (mono_string === MonoStringNull)
                return null;
            this.mono_wasm_string_root.value = mono_string;
            const ppChars = this.mono_wasm_string_decoder_buffer + 0, pLengthBytes = this.mono_wasm_string_decoder_buffer + 4, pIsInterned = this.mono_wasm_string_decoder_buffer + 8;
            wrapped_c_functions.mono_wasm_string_get_data(mono_string, ppChars, pLengthBytes, pIsInterned);
            let result = mono_wasm_empty_string;
            const lengthBytes = getI32(pLengthBytes), pChars = getI32(ppChars), isInterned = getI32(pIsInterned);
            if (pLengthBytes && pChars) {
                if (isInterned &&
                    interned_string_table.has(mono_string) //TODO remove 2x lookup
                ) {
                    result = interned_string_table.get(mono_string);
                    // console.log(`intern table cache hit ${mono_string} ${result.length}`);
                }
                else {
                    result = this.decode(pChars, pChars + lengthBytes);
                    if (isInterned) {
                        // console.log("interned", mono_string, result.length);
                        interned_string_table.set(mono_string, result);
                    }
                }
            }
            this.mono_wasm_string_root.value = 0;
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
    function conv_string(mono_obj) {
        return string_decoder.copy(mono_obj);
    }
    // Ensures the string is already interned on both the managed and JavaScript sides,
    //  then returns the interned string value (to provide fast reference comparisons like C#)
    function mono_intern_string(string) {
        if (string.length === 0)
            return mono_wasm_empty_string;
        const ptr = js_string_to_mono_string_interned(string);
        const result = interned_string_table.get(ptr);
        return result;
    }
    function _store_string_in_intern_table(string, ptr, internIt) {
        if (!ptr)
            throw new Error("null pointer passed to _store_string_in_intern_table");
        else if (typeof (ptr) !== "number")
            throw new Error(`non-pointer passed to _store_string_in_intern_table: ${typeof (ptr)}`);
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
        rootBuffer.set(index, ptr);
        // Store the managed string into the managed intern table. This can theoretically
        //  provide a different managed object than the one we passed in, so update our
        //  pointer (stored in the root) with the result.
        if (internIt) {
            ptr = wrapped_c_functions.mono_wasm_intern_string(ptr);
            rootBuffer.set(index, ptr);
        }
        if (!ptr)
            throw new Error("mono_wasm_intern_string produced a null pointer");
        interned_js_string_table.set(string, ptr);
        interned_string_table.set(ptr, string);
        if ((string.length === 0) && !_empty_string_ptr)
            _empty_string_ptr = ptr;
        return ptr;
    }
    function js_string_to_mono_string_interned(string) {
        const text = (typeof (string) === "symbol")
            ? (string.description || Symbol.keyFor(string) || "<unknown Symbol>")
            : string;
        if ((text.length === 0) && _empty_string_ptr)
            return _empty_string_ptr;
        let ptr = interned_js_string_table.get(text);
        if (ptr)
            return ptr;
        ptr = js_string_to_mono_string_new(text);
        ptr = _store_string_in_intern_table(text, ptr, true);
        return ptr;
    }
    function js_string_to_mono_string(string) {
        if (string === null)
            return null;
        else if (typeof (string) === "symbol")
            return js_string_to_mono_string_interned(string);
        else if (typeof (string) !== "string")
            throw new Error("Expected string argument, got " + typeof (string));
        // Always use an interned pointer for empty strings
        if (string.length === 0)
            return js_string_to_mono_string_interned(string);
        // Looking up large strings in the intern table will require the JS runtime to
        //  potentially hash them and then do full byte-by-byte comparisons, which is
        //  very expensive. Because we can not guarantee it won't happen, try to minimize
        //  the cost of this and prevent performance issues for large strings
        if (string.length <= 256) {
            const interned = interned_js_string_table.get(string);
            if (interned)
                return interned;
        }
        return js_string_to_mono_string_new(string);
    }
    function js_string_to_mono_string_new(string) {
        const buffer = Module._malloc((string.length + 1) * 2);
        const buffer16 = (buffer >>> 1) | 0;
        for (let i = 0; i < string.length; i++)
            Module.HEAP16[buffer16 + i] = string.charCodeAt(i);
        Module.HEAP16[buffer16 + string.length] = 0;
        const result = wrapped_c_functions.mono_wasm_string_from_utf16(buffer, string.length);
        Module._free(buffer);
        return result;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const _are_promises_supported = ((typeof Promise === "object") || (typeof Promise === "function")) && (typeof Promise.resolve === "function");
    const promise_control_symbol = Symbol.for("wasm promise_control");
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function isThenable(js_obj) {
        // When using an external Promise library like Bluebird the Promise.resolve may not be sufficient
        // to identify the object as a Promise.
        return Promise.resolve(js_obj) === js_obj ||
            ((typeof js_obj === "object" || typeof js_obj === "function") && typeof js_obj.then === "function");
    }
    function mono_wasm_cancel_promise(thenable_js_handle, is_exception) {
        try {
            const promise = mono_wasm_get_jsobj_from_js_handle(thenable_js_handle);
            const promise_control = promise[promise_control_symbol];
            promise_control.reject("OperationCanceledException");
        }
        catch (ex) {
            return wrap_error(is_exception, ex);
        }
    }
    function _create_cancelable_promise(afterResolve, afterReject) {
        let promise_control = null;
        const promise = new Promise(function (resolve, reject) {
            promise_control = {
                isDone: false,
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
        promise[promise_control_symbol] = promise_control;
        return { promise, promise_control: promise_control };
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function _js_to_mono_uri(should_add_in_flight, js_obj) {
        switch (true) {
            case js_obj === null:
            case typeof js_obj === "undefined":
                return MonoObjectNull;
            case typeof js_obj === "symbol":
            case typeof js_obj === "string":
                return wrapped_cs_functions._create_uri(js_obj);
            default:
                return _extract_mono_obj(should_add_in_flight, js_obj);
        }
    }
    // this is only used from Blazor
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function js_to_mono_obj(js_obj) {
        return _js_to_mono_obj(false, js_obj);
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function _js_to_mono_obj(should_add_in_flight, js_obj) {
        switch (true) {
            case js_obj === null:
            case typeof js_obj === "undefined":
                return MonoObjectNull;
            case typeof js_obj === "number": {
                let result = null;
                if ((js_obj | 0) === js_obj)
                    result = _box_js_int(js_obj);
                else if ((js_obj >>> 0) === js_obj)
                    result = _box_js_uint(js_obj);
                else
                    result = _box_js_double(js_obj);
                if (!result)
                    throw new Error(`Boxing failed for ${js_obj}`);
                return result;
            }
            case typeof js_obj === "string":
                return js_string_to_mono_string(js_obj);
            case typeof js_obj === "symbol":
                return js_string_to_mono_string_interned(js_obj);
            case typeof js_obj === "boolean":
                return _box_js_bool(js_obj);
            case isThenable(js_obj) === true: {
                const { task_ptr } = _wrap_js_thenable_as_task(js_obj);
                // task_ptr above is not rooted, we need to return it to mono without any intermediate mono call which could cause GC
                return task_ptr;
            }
            case js_obj.constructor.name === "Date":
                // getTime() is always UTC
                return wrapped_cs_functions._create_date_time(js_obj.getTime());
            default:
                return _extract_mono_obj(should_add_in_flight, js_obj);
        }
    }
    function _extract_mono_obj(should_add_in_flight, js_obj) {
        if (js_obj === null || typeof js_obj === "undefined")
            return MonoObjectNull;
        let result = null;
        if (js_obj[js_owned_gc_handle_symbol]) {
            // for js_owned_gc_handle we don't want to create new proxy
            // since this is strong gc_handle we don't need to in-flight reference
            result = get_js_owned_object_by_gc_handle(js_obj[js_owned_gc_handle_symbol]);
            return result;
        }
        if (js_obj[cs_owned_js_handle_symbol]) {
            result = get_cs_owned_object_by_js_handle(js_obj[cs_owned_js_handle_symbol], should_add_in_flight);
            // It's possible the managed object corresponding to this JS object was collected,
            //  in which case we need to make a new one.
            if (!result) {
                delete js_obj[cs_owned_js_handle_symbol];
            }
        }
        if (!result) {
            // Obtain the JS -> C# type mapping.
            const wasm_type = js_obj[wasm_type_symbol];
            const wasm_type_id = typeof wasm_type === "undefined" ? 0 : wasm_type;
            const js_handle = mono_wasm_get_js_handle(js_obj);
            result = wrapped_cs_functions._create_cs_owned_proxy(js_handle, wasm_type_id, should_add_in_flight ? 1 : 0);
        }
        return result;
    }
    function _box_js_int(js_obj) {
        setI32(runtimeHelpers._box_buffer, js_obj);
        return wrapped_c_functions.mono_wasm_box_primitive(runtimeHelpers._class_int32, runtimeHelpers._box_buffer, 4);
    }
    function _box_js_uint(js_obj) {
        setU32(runtimeHelpers._box_buffer, js_obj);
        return wrapped_c_functions.mono_wasm_box_primitive(runtimeHelpers._class_uint32, runtimeHelpers._box_buffer, 4);
    }
    function _box_js_double(js_obj) {
        setF64(runtimeHelpers._box_buffer, js_obj);
        return wrapped_c_functions.mono_wasm_box_primitive(runtimeHelpers._class_double, runtimeHelpers._box_buffer, 8);
    }
    function _box_js_bool(js_obj) {
        setI32(runtimeHelpers._box_buffer, js_obj ? 1 : 0);
        return wrapped_c_functions.mono_wasm_box_primitive(runtimeHelpers._class_boolean, runtimeHelpers._box_buffer, 4);
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
    function js_typed_array_to_array(js_obj) {
        // JavaScript typed arrays are array-like objects and provide a mechanism for accessing
        // raw binary data. (...) To achieve maximum flexibility and efficiency, JavaScript typed arrays
        // split the implementation into buffers and views. A buffer (implemented by the ArrayBuffer object)
        //  is an object representing a chunk of data; it has no format to speak of, and offers no
        // mechanism for accessing its contents. In order to access the memory contained in a buffer,
        // you need to use a view. A view provides a context — that is, a data type, starting offset,
        // and number of elements — that turns the data into an actual typed array.
        // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Typed_arrays
        if (has_backing_array_buffer(js_obj) && js_obj.BYTES_PER_ELEMENT) {
            const arrayType = js_obj[wasm_type_symbol];
            const heapBytes = js_typedarray_to_heap(js_obj);
            const bufferArray = wrapped_c_functions.mono_wasm_typed_array_new(heapBytes.byteOffset, js_obj.length, js_obj.BYTES_PER_ELEMENT, arrayType);
            Module._free(heapBytes.byteOffset);
            return bufferArray;
        }
        else {
            throw new Error("Object '" + js_obj + "' is not a typed array");
        }
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars, @typescript-eslint/explicit-module-boundary-types
    function js_to_mono_enum(js_obj, method, parmIdx) {
        if (typeof (js_obj) !== "number")
            throw new Error(`Expected numeric value for enum argument, got '${js_obj}'`);
        return js_obj | 0;
    }
    function js_array_to_mono_array(js_array, asString, should_add_in_flight) {
        const mono_array = asString ? wrapped_c_functions.mono_wasm_string_array_new(js_array.length) : wrapped_c_functions.mono_wasm_obj_array_new(js_array.length);
        const arrayRoot = mono_wasm_new_root(mono_array);
        const elemRoot = mono_wasm_new_root(MonoObjectNull);
        try {
            for (let i = 0; i < js_array.length; ++i) {
                let obj = js_array[i];
                if (asString)
                    obj = obj.toString();
                elemRoot.value = _js_to_mono_obj(should_add_in_flight, obj);
                wrapped_c_functions.mono_wasm_obj_array_set(arrayRoot.value, i, elemRoot.value);
            }
            return mono_array;
        }
        finally {
            mono_wasm_release_roots(arrayRoot, elemRoot);
        }
    }
    function _wrap_js_thenable_as_task(thenable) {
        if (!thenable)
            return null;
        // hold strong JS reference to thenable while in flight
        // ideally, this should be hold alive by lifespan of the resulting C# Task, but this is good cheap aproximation
        const thenable_js_handle = mono_wasm_get_js_handle(thenable);
        // Note that we do not implement promise/task roundtrip. 
        // With more complexity we could recover original instance when this Task is marshaled back to JS.
        // TODO optimization: return the tcs.Task on this same call instead of _get_tcs_task
        const tcs_gc_handle = wrapped_cs_functions._create_tcs();
        thenable.then((result) => {
            wrapped_cs_functions._set_tcs_result(tcs_gc_handle, result);
            // let go of the thenable reference
            mono_wasm_release_cs_owned_object(thenable_js_handle);
            // when FinalizationRegistry is not supported by this browser, we will do immediate cleanup after promise resolve/reject
            if (!_use_finalization_registry) {
                wrapped_cs_functions._release_js_owned_object_by_gc_handle(tcs_gc_handle);
            }
        }, (reason) => {
            wrapped_cs_functions._set_tcs_failure(tcs_gc_handle, reason ? reason.toString() : "");
            // let go of the thenable reference
            mono_wasm_release_cs_owned_object(thenable_js_handle);
            // when FinalizationRegistry is not supported by this browser, we will do immediate cleanup after promise resolve/reject
            if (!_use_finalization_registry) {
                wrapped_cs_functions._release_js_owned_object_by_gc_handle(tcs_gc_handle);
            }
        });
        // collect the TaskCompletionSource with its Task after js doesn't hold the thenable anymore
        if (_use_finalization_registry) {
            _js_owned_object_registry.register(thenable, tcs_gc_handle);
        }
        // returns raw pointer to tcs.Task
        return {
            task_ptr: wrapped_cs_functions._get_tcs_task(tcs_gc_handle),
            then_js_handle: thenable_js_handle,
        };
    }
    function mono_wasm_typed_array_to_array(js_handle, is_exception) {
        const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        if (!js_obj) {
            return wrap_error(is_exception, "ERR06: Invalid JS object handle '" + js_handle + "'");
        }
        // returns pointer to C# array
        return js_typed_array_to_array(js_obj);
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // see src/mono/wasm/driver.c MARSHAL_TYPE_xxx and Runtime.cs MarshalType
    var MarshalType;
    (function (MarshalType) {
        MarshalType[MarshalType["NULL"] = 0] = "NULL";
        MarshalType[MarshalType["INT"] = 1] = "INT";
        MarshalType[MarshalType["FP64"] = 2] = "FP64";
        MarshalType[MarshalType["STRING"] = 3] = "STRING";
        MarshalType[MarshalType["VT"] = 4] = "VT";
        MarshalType[MarshalType["DELEGATE"] = 5] = "DELEGATE";
        MarshalType[MarshalType["TASK"] = 6] = "TASK";
        MarshalType[MarshalType["OBJECT"] = 7] = "OBJECT";
        MarshalType[MarshalType["BOOL"] = 8] = "BOOL";
        MarshalType[MarshalType["ENUM"] = 9] = "ENUM";
        MarshalType[MarshalType["URI"] = 22] = "URI";
        MarshalType[MarshalType["SAFEHANDLE"] = 23] = "SAFEHANDLE";
        MarshalType[MarshalType["ARRAY_BYTE"] = 10] = "ARRAY_BYTE";
        MarshalType[MarshalType["ARRAY_UBYTE"] = 11] = "ARRAY_UBYTE";
        MarshalType[MarshalType["ARRAY_UBYTE_C"] = 12] = "ARRAY_UBYTE_C";
        MarshalType[MarshalType["ARRAY_SHORT"] = 13] = "ARRAY_SHORT";
        MarshalType[MarshalType["ARRAY_USHORT"] = 14] = "ARRAY_USHORT";
        MarshalType[MarshalType["ARRAY_INT"] = 15] = "ARRAY_INT";
        MarshalType[MarshalType["ARRAY_UINT"] = 16] = "ARRAY_UINT";
        MarshalType[MarshalType["ARRAY_FLOAT"] = 17] = "ARRAY_FLOAT";
        MarshalType[MarshalType["ARRAY_DOUBLE"] = 18] = "ARRAY_DOUBLE";
        MarshalType[MarshalType["FP32"] = 24] = "FP32";
        MarshalType[MarshalType["UINT32"] = 25] = "UINT32";
        MarshalType[MarshalType["INT64"] = 26] = "INT64";
        MarshalType[MarshalType["UINT64"] = 27] = "UINT64";
        MarshalType[MarshalType["CHAR"] = 28] = "CHAR";
        MarshalType[MarshalType["STRING_INTERNED"] = 29] = "STRING_INTERNED";
        MarshalType[MarshalType["VOID"] = 30] = "VOID";
        MarshalType[MarshalType["ENUM64"] = 31] = "ENUM64";
        MarshalType[MarshalType["POINTER"] = 32] = "POINTER";
    })(MarshalType || (MarshalType = {}));
    // see src/mono/wasm/driver.c MARSHAL_ERROR_xxx and Runtime.cs
    var MarshalError;
    (function (MarshalError) {
        MarshalError[MarshalError["BUFFER_TOO_SMALL"] = 512] = "BUFFER_TOO_SMALL";
        MarshalError[MarshalError["NULL_CLASS_POINTER"] = 513] = "NULL_CLASS_POINTER";
        MarshalError[MarshalError["NULL_TYPE_POINTER"] = 514] = "NULL_TYPE_POINTER";
        MarshalError[MarshalError["UNSUPPORTED_TYPE"] = 515] = "UNSUPPORTED_TYPE";
        MarshalError[MarshalError["FIRST"] = 512] = "FIRST";
    })(MarshalError || (MarshalError = {}));
    const delegate_invoke_symbol = Symbol.for("wasm delegate_invoke");
    const delegate_invoke_signature_symbol = Symbol.for("wasm delegate_invoke_signature");
    // this is only used from Blazor
    function unbox_mono_obj(mono_obj) {
        if (mono_obj === MonoObjectNull)
            return undefined;
        const root = mono_wasm_new_root(mono_obj);
        try {
            return _unbox_mono_obj_root(root);
        }
        finally {
            root.release();
        }
    }
    function _unbox_cs_owned_root_as_js_object(root) {
        // we don't need in-flight reference as we already have it rooted here
        const js_handle = wrapped_cs_functions._get_cs_owned_object_js_handle(root.value, 0);
        const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        return js_obj;
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function _unbox_mono_obj_root_with_known_nonprimitive_type_impl(root, type, typePtr, unbox_buffer) {
        //See MARSHAL_TYPE_ defines in driver.c
        switch (type) {
            case MarshalType.INT64:
            case MarshalType.UINT64:
                // TODO: Fix this once emscripten offers HEAPI64/HEAPU64 or can return them
                throw new Error("int64 not available");
            case MarshalType.STRING:
            case MarshalType.STRING_INTERNED:
                return conv_string(root.value);
            case MarshalType.VT:
                throw new Error("no idea on how to unbox value types");
            case MarshalType.DELEGATE:
                return _wrap_delegate_root_as_function(root);
            case MarshalType.TASK:
                return _unbox_task_root_as_promise(root);
            case MarshalType.OBJECT:
                return _unbox_ref_type_root_as_js_object(root);
            case MarshalType.ARRAY_BYTE:
            case MarshalType.ARRAY_UBYTE:
            case MarshalType.ARRAY_UBYTE_C:
            case MarshalType.ARRAY_SHORT:
            case MarshalType.ARRAY_USHORT:
            case MarshalType.ARRAY_INT:
            case MarshalType.ARRAY_UINT:
            case MarshalType.ARRAY_FLOAT:
            case MarshalType.ARRAY_DOUBLE:
                throw new Error("Marshalling of primitive arrays are not supported.  Use the corresponding TypedArray instead.");
            case 20: // clr .NET DateTime
                return new Date(wrapped_cs_functions._get_date_value(root.value));
            case 21: // clr .NET DateTimeOffset
                return wrapped_cs_functions._object_to_string(root.value);
            case MarshalType.URI:
                return wrapped_cs_functions._object_to_string(root.value);
            case MarshalType.SAFEHANDLE:
                return _unbox_cs_owned_root_as_js_object(root);
            case MarshalType.VOID:
                return undefined;
            default:
                throw new Error(`no idea on how to unbox object of MarshalType ${type} at offset ${root.value} (root address is ${root.get_address()})`);
        }
    }
    function _unbox_mono_obj_root_with_known_nonprimitive_type(root, type, unbox_buffer) {
        if (type >= MarshalError.FIRST)
            throw new Error(`Got marshaling error ${type} when attempting to unbox object at address ${root.value} (root located at ${root.get_address()})`);
        let typePtr = MonoTypeNull;
        if ((type === MarshalType.VT) || (type == MarshalType.OBJECT)) {
            typePtr = getU32(unbox_buffer);
            if (typePtr < 1024)
                throw new Error(`Got invalid MonoType ${typePtr} for object at address ${root.value} (root located at ${root.get_address()})`);
        }
        return _unbox_mono_obj_root_with_known_nonprimitive_type_impl(root, type, typePtr, unbox_buffer);
    }
    function _unbox_mono_obj_root(root) {
        if (root.value === 0)
            return undefined;
        const unbox_buffer = runtimeHelpers._unbox_buffer;
        const type = wrapped_c_functions.mono_wasm_try_unbox_primitive_and_get_type(root.value, unbox_buffer, runtimeHelpers._unbox_buffer_size);
        switch (type) {
            case MarshalType.INT:
                return getI32(unbox_buffer);
            case MarshalType.UINT32:
                return getU32(unbox_buffer);
            case MarshalType.POINTER:
                // FIXME: Is this right?
                return getU32(unbox_buffer);
            case MarshalType.FP32:
                return getF32(unbox_buffer);
            case MarshalType.FP64:
                return getF64(unbox_buffer);
            case MarshalType.BOOL:
                return (getI32(unbox_buffer)) !== 0;
            case MarshalType.CHAR:
                return String.fromCharCode(getI32(unbox_buffer));
            case MarshalType.NULL:
                return null;
            default:
                return _unbox_mono_obj_root_with_known_nonprimitive_type(root, type, unbox_buffer);
        }
    }
    function mono_array_to_js_array(mono_array) {
        if (mono_array === MonoArrayNull)
            return null;
        const arrayRoot = mono_wasm_new_root(mono_array);
        try {
            return _mono_array_root_to_js_array(arrayRoot);
        }
        finally {
            arrayRoot.release();
        }
    }
    function is_nested_array(ele) {
        return wrapped_cs_functions._is_simple_array(ele);
    }
    function _mono_array_root_to_js_array(arrayRoot) {
        if (arrayRoot.value === MonoArrayNull)
            return null;
        const elemRoot = mono_wasm_new_root();
        try {
            const len = wrapped_c_functions.mono_wasm_array_length(arrayRoot.value);
            const res = new Array(len);
            for (let i = 0; i < len; ++i) {
                elemRoot.value = wrapped_c_functions.mono_wasm_array_get(arrayRoot.value, i);
                if (is_nested_array(elemRoot.value))
                    res[i] = _mono_array_root_to_js_array(elemRoot);
                else
                    res[i] = _unbox_mono_obj_root(elemRoot);
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
        const gc_handle = wrapped_cs_functions._get_js_owned_object_gc_handle(root.value);
        return _wrap_delegate_gc_handle_as_function(gc_handle);
    }
    function _wrap_delegate_gc_handle_as_function(gc_handle, after_listener_callback) {
        // see if we have js owned instance for this gc_handle already
        let result = _lookup_js_owned_object(gc_handle);
        // If the function for this gc_handle was already collected (or was never created)
        if (!result) {
            // note that we do not implement function/delegate roundtrip
            result = function (...args) {
                const delegateRoot = mono_wasm_new_root(get_js_owned_object_by_gc_handle(gc_handle));
                try {
                    const res = call_method(result[delegate_invoke_symbol], delegateRoot.value, result[delegate_invoke_signature_symbol], args);
                    if (after_listener_callback) {
                        after_listener_callback();
                    }
                    return res;
                }
                finally {
                    delegateRoot.release();
                }
            };
            // bind the method
            const delegateRoot = mono_wasm_new_root(get_js_owned_object_by_gc_handle(gc_handle));
            try {
                if (typeof result[delegate_invoke_symbol] === "undefined") {
                    result[delegate_invoke_symbol] = wrapped_c_functions.mono_wasm_get_delegate_invoke(delegateRoot.value);
                    if (!result[delegate_invoke_symbol]) {
                        throw new Error("System.Delegate Invoke method can not be resolved.");
                    }
                }
                if (typeof result[delegate_invoke_signature_symbol] === "undefined") {
                    result[delegate_invoke_signature_symbol] = mono_method_get_call_signature(result[delegate_invoke_symbol], delegateRoot.value);
                }
            }
            finally {
                delegateRoot.release();
            }
            // NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry. Except in case of EventListener where we cleanup after unregistration.
            if (_use_finalization_registry) {
                // register for GC of the deleate after the JS side is done with the function
                _js_owned_object_registry.register(result, gc_handle);
            }
            // register for instance reuse
            // NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef. Except in case of EventListener where we cleanup after unregistration.
            _register_js_owned_object(gc_handle, result);
        }
        return result;
    }
    function mono_wasm_create_cs_owned_object(core_name, args, is_exception) {
        const argsRoot = mono_wasm_new_root(args), nameRoot = mono_wasm_new_root(core_name);
        try {
            const js_name = conv_string(nameRoot.value);
            if (!js_name) {
                return wrap_error(is_exception, "Invalid name @" + nameRoot.value);
            }
            const coreObj = globalThis[js_name];
            if (coreObj === null || typeof coreObj === "undefined") {
                return wrap_error(is_exception, "JavaScript host object '" + js_name + "' not found.");
            }
            try {
                const js_args = _mono_array_root_to_js_array(argsRoot);
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
                return _js_to_mono_obj(false, js_handle);
            }
            catch (ex) {
                return wrap_error(is_exception, ex);
            }
        }
        finally {
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
        const gc_handle = wrapped_cs_functions._get_js_owned_object_gc_handle(root.value);
        // see if we have js owned instance for this gc_handle already
        let result = _lookup_js_owned_object(gc_handle);
        // If the promise for this gc_handle was already collected (or was never created)
        if (!result) {
            const explicitFinalization = _use_finalization_registry
                ? undefined
                : () => _js_owned_object_finalized(gc_handle);
            const { promise, promise_control } = _create_cancelable_promise(explicitFinalization, explicitFinalization);
            // note that we do not implement promise/task roundtrip
            // With more complexity we could recover original instance when this promise is marshaled back to C#.
            result = promise;
            // register C# side of the continuation
            wrapped_cs_functions._setup_js_cont(root.value, promise_control);
            // register for GC of the Task after the JS side is done with the promise
            if (_use_finalization_registry) {
                _js_owned_object_registry.register(result, gc_handle);
            }
            // register for instance reuse
            _register_js_owned_object(gc_handle, result);
        }
        return result;
    }
    function _unbox_ref_type_root_as_js_object(root) {
        if (root.value === MonoObjectNull)
            return null;
        // this could be JSObject proxy of a js native object
        // we don't need in-flight reference as we already have it rooted here
        const js_handle = wrapped_cs_functions._try_get_cs_owned_object_js_handle(root.value, 0);
        if (js_handle) {
            if (js_handle === JSHandleDisposed) {
                throw new Error("Cannot access a disposed JSObject at " + root.value);
            }
            return mono_wasm_get_jsobj_from_js_handle(js_handle);
        }
        // otherwise this is C# only object
        // get strong reference to Object
        const gc_handle = wrapped_cs_functions._get_js_owned_object_gc_handle(root.value);
        // see if we have js owned instance for this gc_handle already
        let result = _lookup_js_owned_object(gc_handle);
        // If the JS object for this gc_handle was already collected (or was never created)
        if (!result) {
            result = {};
            // keep the gc_handle so that we could easily convert it back to original C# object for roundtrip
            result[js_owned_gc_handle_symbol] = gc_handle;
            // NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef
            if (_use_finalization_registry) {
                // register for GC of the C# object after the JS side is done with the object
                _js_owned_object_registry.register(result, gc_handle);
            }
            // register for instance reuse
            // NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef
            _register_js_owned_object(gc_handle, result);
        }
        return result;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const primitiveConverters = new Map();
    const _signature_converters = new Map();
    const _method_descriptions = new Map();
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
    function find_method(klass, name, n) {
        const result = wrapped_c_functions.mono_wasm_assembly_find_method(klass, name, n);
        if (result) {
            _method_descriptions.set(result, name);
        }
        return result;
    }
    function get_method(method_name) {
        const res = find_method(runtimeHelpers.wasm_runtime_class, method_name, -1);
        if (!res)
            throw "Can't find method " + runtimeHelpers.runtime_namespace + "." + runtimeHelpers.runtime_classname + ":" + method_name;
        return res;
    }
    function bind_runtime_method(method_name, signature) {
        const method = get_method(method_name);
        return mono_bind_method(method, null, signature, "BINDINGS_" + method_name);
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
            uriPrefix = "//# sourceURL=https://mono-wasm.invalid/" + name + "\r\n";
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
        result.set("s", { steps: [{ convert: js_string_to_mono_string.bind(BINDING$1) }], size: 0, needs_root: true });
        result.set("S", { steps: [{ convert: js_string_to_mono_string_interned.bind(BINDING$1) }], size: 0, needs_root: true });
        // note we also bind first argument to false for both _js_to_mono_obj and _js_to_mono_uri, 
        // because we will root the reference, so we don't need in-flight reference
        // also as those are callback arguments and we don't have platform code which would release the in-flight reference on C# end
        result.set("o", { steps: [{ convert: _js_to_mono_obj.bind(BINDING$1, false) }], size: 0, needs_root: true });
        result.set("u", { steps: [{ convert: _js_to_mono_uri.bind(BINDING$1, false) }], size: 0, needs_root: true });
        // result.set ('k', { steps: [{ convert: js_to_mono_enum.bind (this), indirect: 'i64'}], size: 8});
        result.set("j", { steps: [{ convert: js_to_mono_enum.bind(BINDING$1), indirect: "i32" }], size: 8 });
        result.set("i", { steps: [{ indirect: "i32" }], size: 8 });
        result.set("l", { steps: [{ indirect: "i64" }], size: 8 });
        result.set("f", { steps: [{ indirect: "float" }], size: 8 });
        result.set("d", { steps: [{ indirect: "double" }], size: 8 });
    }
    function _create_converter_for_marshal_string(args_marshal) {
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
    function _get_converter_for_marshal_string(args_marshal) {
        let converter = _signature_converters.get(args_marshal);
        if (!converter) {
            converter = _create_converter_for_marshal_string(args_marshal);
            _signature_converters.set(args_marshal, converter);
        }
        return converter;
    }
    function _compile_converter_for_marshal_string(args_marshal) {
        const converter = _get_converter_for_marshal_string(args_marshal);
        if (typeof (converter.args_marshal) !== "string")
            throw new Error("Corrupt converter for '" + args_marshal + "'");
        if (converter.compiled_function && converter.compiled_variadic_function)
            return converter;
        const converterName = args_marshal.replace("!", "_result_unmarshaled");
        converter.name = converterName;
        let body = [];
        let argumentNames = ["buffer", "rootBuffer", "method"];
        // worst-case allocation size instead of allocating dynamically, plus padding
        const bufferSizeBytes = converter.size + (args_marshal.length * 4) + 16;
        // ensure the indirect values are 8-byte aligned so that aligned loads and stores will work
        const indirectBaseOffset = ((((args_marshal.length * 4) + 7) / 8) | 0) * 8;
        const closure = {
            Module,
            _malloc: Module._malloc,
            mono_wasm_unbox_rooted: wrapped_c_functions.mono_wasm_unbox_rooted,
            setI32,
            setU32,
            setF32,
            setF64,
            setI64
        };
        let indirectLocalOffset = 0;
        body.push("if (!method) throw new Error('no method provided');", `if (!buffer) buffer = _malloc (${bufferSizeBytes});`, `let indirectStart = buffer + ${indirectBaseOffset};`, "");
        for (let i = 0; i < converter.steps.length; i++) {
            const step = converter.steps[i];
            const closureKey = "step" + i;
            const valueKey = "value" + i;
            const argKey = "arg" + i;
            argumentNames.push(argKey);
            if (step.convert) {
                closure[closureKey] = step.convert;
                body.push(`let ${valueKey} = ${closureKey}(${argKey}, method, ${i});`);
            }
            else {
                body.push(`let ${valueKey} = ${argKey};`);
            }
            if (step.needs_root) {
                body.push("if (!rootBuffer) throw new Error('no root buffer provided');");
                body.push(`rootBuffer.set (${i}, ${valueKey});`);
            }
            // HACK: needs_unbox indicates that we were passed a pointer to a managed object, and either
            //  it was already rooted by our caller or (needs_root = true) by us. Now we can unbox it and
            //  pass the raw address of its boxed value into the callee.
            // FIXME: I don't think this is GC safe
            if (step.needs_unbox)
                body.push(`${valueKey} = mono_wasm_unbox_rooted (${valueKey});`);
            if (step.indirect) {
                const offsetText = `(indirectStart + ${indirectLocalOffset})`;
                switch (step.indirect) {
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
                    case "i64":
                        body.push(`setI64(${offsetText}, ${valueKey});`);
                        break;
                    default:
                        throw new Error("Unimplemented indirect type: " + step.indirect);
                }
                body.push(`setU32(buffer + (${i} * 4), ${offsetText});`);
                indirectLocalOffset += step.size;
            }
            else {
                body.push(`setI32(buffer + (${i} * 4), ${valueKey});`);
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
            console.warn("compiling converter failed for", bodyJs, "with error", exc);
            throw exc;
        }
        argumentNames = ["existingBuffer", "rootBuffer", "method", "args"];
        const variadicClosure = {
            converter: compiledFunction
        };
        body = [
            "return converter(",
            "  existingBuffer, rootBuffer, method,"
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
            console.warn("compiling converter failed for", bodyJs, "with error", exc);
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
    function mono_bind_method(method, this_arg, args_marshal, friendly_name) {
        if (typeof (args_marshal) !== "string")
            throw new Error("args_marshal argument invalid, expected string");
        this_arg = coerceNull(this_arg);
        let converter = null;
        if (typeof (args_marshal) === "string") {
            converter = _compile_converter_for_marshal_string(args_marshal);
        }
        // FIXME
        const unbox_buffer_size = 8192;
        const unbox_buffer = Module._malloc(unbox_buffer_size);
        const token = {
            friendlyName: friendly_name,
            method,
            converter,
            scratchRootBuffer: null,
            scratchBuffer: VoidPtrNull,
            scratchResultRoot: mono_wasm_new_root(),
            scratchExceptionRoot: mono_wasm_new_root()
        };
        const closure = {
            Module,
            mono_wasm_new_root,
            _create_temp_frame,
            _get_args_root_buffer_for_method_call,
            _get_buffer_for_method_call,
            _handle_exception_for_call,
            _teardown_after_call,
            mono_wasm_try_unbox_primitive_and_get_type: wrapped_c_functions.mono_wasm_try_unbox_primitive_and_get_type,
            _unbox_mono_obj_root_with_known_nonprimitive_type,
            invoke_method: wrapped_c_functions.mono_wasm_invoke_method,
            method,
            this_arg,
            token,
            unbox_buffer,
            unbox_buffer_size,
            getI32,
            getU32,
            getF32,
            getF64
        };
        const converterKey = converter ? "converter_" + converter.name : "";
        if (converter)
            closure[converterKey] = converter;
        const argumentNames = [];
        const body = [
            "_create_temp_frame();",
            "let resultRoot = token.scratchResultRoot, exceptionRoot = token.scratchExceptionRoot;",
            "token.scratchResultRoot = null;",
            "token.scratchExceptionRoot = null;",
            "if (resultRoot === null)",
            "	resultRoot = mono_wasm_new_root ();",
            "if (exceptionRoot === null)",
            "	exceptionRoot = mono_wasm_new_root ();",
            ""
        ];
        if (converter) {
            body.push(`let argsRootBuffer = _get_args_root_buffer_for_method_call(${converterKey}, token);`, `let scratchBuffer = _get_buffer_for_method_call(${converterKey}, token);`, `let buffer = ${converterKey}.compiled_function(`, "    scratchBuffer, argsRootBuffer, method,");
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
            body.push("let argsRootBuffer = null, buffer = 0;");
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
        body.push("", "resultRoot.value = invoke_method (method, this_arg, buffer, exceptionRoot.get_address ());", `_handle_exception_for_call (${converterKey}, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);`, "", "let resultPtr = resultRoot.value, result = undefined;");
        if (converter) {
            if (converter.is_result_possibly_unmarshaled)
                body.push("if (!is_result_marshaled) ");
            if (converter.is_result_definitely_unmarshaled || converter.is_result_possibly_unmarshaled)
                body.push("    result = resultPtr;");
            if (!converter.is_result_definitely_unmarshaled)
                body.push("if (is_result_marshaled && (resultPtr !== 0)) {", 
                // For the common scenario where the return type is a primitive, we want to try and unbox it directly
                //  into our existing heap allocation and then read it out of the heap. Doing this all in one operation
                //  means that we only need to enter a gc safe region twice (instead of 3+ times with the normal,
                //  slower check-type-and-then-unbox flow which has extra checks since unbox verifies the type).
                "    let resultType = mono_wasm_try_unbox_primitive_and_get_type (resultPtr, unbox_buffer, unbox_buffer_size);", "    switch (resultType) {", `    case ${MarshalType.INT}:`, "        result = getI32(unbox_buffer); break;", `    case ${MarshalType.POINTER}:`, // FIXME: Is this right?
                `    case ${MarshalType.UINT32}:`, "        result = getU32(unbox_buffer); break;", `    case ${MarshalType.FP32}:`, "        result = getF32(unbox_buffer); break;", `    case ${MarshalType.FP64}:`, "        result = getF64(unbox_buffer); break;", `    case ${MarshalType.BOOL}:`, "        result = getI32(unbox_buffer) !== 0; break;", `    case ${MarshalType.CHAR}:`, "        result = String.fromCharCode(getI32(unbox_buffer)); break;", "    default:", "        result = _unbox_mono_obj_root_with_known_nonprimitive_type (resultRoot, resultType, unbox_buffer); break;", "    }", "}");
        }
        else {
            throw new Error("No converter");
        }
        if (friendly_name) {
            const escapeRE = /[^A-Za-z0-9_$]/g;
            friendly_name = friendly_name.replace(escapeRE, "_");
        }
        let displayName = friendly_name || ("clr_" + method);
        if (this_arg)
            displayName += "_this" + this_arg;
        body.push(`_teardown_after_call (${converterKey}, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);`, "return result;");
        const bodyJs = body.join("\r\n");
        const result = _create_named_function(displayName, argumentNames, bodyJs, closure);
        return result;
    }

    // Licensed to the .NET Foundation under one or more agreements.
    function _verify_args_for_method_call(args_marshal, args) {
        const has_args = args && (typeof args === "object") && args.length > 0;
        const has_args_marshal = typeof args_marshal === "string";
        if (has_args) {
            if (!has_args_marshal)
                throw new Error("No signature provided for method call.");
            else if (args.length > args_marshal.length)
                throw new Error("Too many parameter values. Expected at most " + args_marshal.length + " value(s) for signature " + args_marshal);
        }
        return has_args_marshal && has_args;
    }
    function _get_buffer_for_method_call(converter, token) {
        if (!converter)
            return VoidPtrNull;
        let result = VoidPtrNull;
        if (token !== null) {
            result = token.scratchBuffer || VoidPtrNull;
            token.scratchBuffer = VoidPtrNull;
        }
        else {
            result = converter.scratchBuffer || VoidPtrNull;
            converter.scratchBuffer = VoidPtrNull;
        }
        return result;
    }
    function _get_args_root_buffer_for_method_call(converter, token) {
        if (!converter)
            return undefined;
        if (!converter.needs_root_buffer)
            return undefined;
        let result = null;
        if (token !== null) {
            result = token.scratchRootBuffer;
            token.scratchRootBuffer = null;
        }
        else {
            result = converter.scratchRootBuffer;
            converter.scratchRootBuffer = null;
        }
        if (result === null) {
            // TODO: Expand the converter's heap allocation and then use
            //  mono_wasm_new_root_buffer_from_pointer instead. Not that important
            //  at present because the scratch buffer will be reused unless we are
            //  recursing through a re-entrant call
            result = mono_wasm_new_root_buffer(converter.steps.length);
            // FIXME
            result.converter = converter;
        }
        return result;
    }
    function _release_args_root_buffer_from_method_call(converter, token, argsRootBuffer) {
        if (!argsRootBuffer || !converter)
            return;
        // Store the arguments root buffer for re-use in later calls
        if (token && (token.scratchRootBuffer === null)) {
            argsRootBuffer.clear();
            token.scratchRootBuffer = argsRootBuffer;
        }
        else if (!converter.scratchRootBuffer) {
            argsRootBuffer.clear();
            converter.scratchRootBuffer = argsRootBuffer;
        }
        else {
            argsRootBuffer.release();
        }
    }
    function _release_buffer_from_method_call(converter, token, buffer) {
        if (!converter || !buffer)
            return;
        if (token && !token.scratchBuffer)
            token.scratchBuffer = buffer;
        else if (!converter.scratchBuffer)
            converter.scratchBuffer = coerceNull(buffer);
        else if (buffer)
            Module._free(buffer);
    }
    function _convert_exception_for_method_call(result, exception) {
        if (exception === MonoObjectNull)
            return null;
        const msg = conv_string(result);
        const err = new Error(msg); //the convention is that invoke_method ToString () any outgoing exception
        // console.warn (`error ${msg} at location ${err.stack});
        return err;
    }
    /*
    args_marshal is a string with one character per parameter that tells how to marshal it, here are the valid values:

    i: int32
    j: int32 - Enum with underlying type of int32
    l: int64
    k: int64 - Enum with underlying type of int64
    f: float
    d: double
    s: string
    S: interned string
    o: js object will be converted to a C# object (this will box numbers/bool/promises)
    m: raw mono object. Don't use it unless you know what you're doing

    to suppress marshaling of the return value, place '!' at the end of args_marshal, i.e. 'ii!' instead of 'ii'
    */
    function call_method(method, this_arg, args_marshal, args) {
        // HACK: Sometimes callers pass null or undefined, coerce it to 0 since that's what wasm expects
        this_arg = coerceNull(this_arg);
        // Detect someone accidentally passing the wrong type of value to method
        if (typeof method !== "number")
            throw new Error(`method must be an address in the native heap, but was '${method}'`);
        if (!method)
            throw new Error("no method specified");
        const needs_converter = _verify_args_for_method_call(args_marshal, args);
        let buffer = VoidPtrNull, converter = undefined, argsRootBuffer = undefined;
        let is_result_marshaled = true;
        // TODO: Only do this if the signature needs marshalling
        _create_temp_frame();
        // check if the method signature needs argument mashalling
        if (needs_converter) {
            converter = _compile_converter_for_marshal_string(args_marshal);
            is_result_marshaled = _decide_if_result_is_marshaled(converter, args.length);
            argsRootBuffer = _get_args_root_buffer_for_method_call(converter, null);
            const scratchBuffer = _get_buffer_for_method_call(converter, null);
            buffer = converter.compiled_variadic_function(scratchBuffer, argsRootBuffer, method, args);
        }
        return _call_method_with_converted_args(method, this_arg, converter, null, buffer, is_result_marshaled, argsRootBuffer);
    }
    function _handle_exception_for_call(converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer) {
        const exc = _convert_exception_for_method_call(resultRoot.value, exceptionRoot.value);
        if (!exc)
            return;
        _teardown_after_call(converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);
        throw exc;
    }
    function _handle_exception_and_produce_result_for_call(converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer, is_result_marshaled) {
        _handle_exception_for_call(converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);
        let result = resultRoot.value;
        if (is_result_marshaled)
            result = _unbox_mono_obj_root(resultRoot);
        _teardown_after_call(converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);
        return result;
    }
    function _teardown_after_call(converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer) {
        _release_temp_frame();
        _release_args_root_buffer_from_method_call(converter, token, argsRootBuffer);
        _release_buffer_from_method_call(converter, token, buffer);
        if (resultRoot) {
            resultRoot.value = 0;
            if ((token !== null) && (token.scratchResultRoot === null))
                token.scratchResultRoot = resultRoot;
            else
                resultRoot.release();
        }
        if (exceptionRoot) {
            exceptionRoot.value = 0;
            if ((token !== null) && (token.scratchExceptionRoot === null))
                token.scratchExceptionRoot = exceptionRoot;
            else
                exceptionRoot.release();
        }
    }
    function _call_method_with_converted_args(method, this_arg, converter, token, buffer, is_result_marshaled, argsRootBuffer) {
        const resultRoot = mono_wasm_new_root(), exceptionRoot = mono_wasm_new_root();
        resultRoot.value = wrapped_c_functions.mono_wasm_invoke_method(method, this_arg, buffer, exceptionRoot.get_address());
        return _handle_exception_and_produce_result_for_call(converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer, is_result_marshaled);
    }
    function call_static_method(fqn, args, signature) {
        bindings_lazy_init(); // TODO remove this once Blazor does better startup
        const method = mono_method_resolve(fqn);
        if (typeof signature === "undefined")
            signature = mono_method_get_call_signature(method);
        return call_method(method, undefined, signature, args);
    }
    function mono_bind_static_method(fqn, signature) {
        bindings_lazy_init(); // TODO remove this once Blazor does better startup
        const method = mono_method_resolve(fqn);
        if (typeof signature === "undefined")
            signature = mono_method_get_call_signature(method);
        return mono_bind_method(method, null, signature, fqn);
    }
    function mono_bind_assembly_entry_point(assembly, signature) {
        bindings_lazy_init(); // TODO remove this once Blazor does better startup
        const asm = wrapped_c_functions.mono_wasm_assembly_load(assembly);
        if (!asm)
            throw new Error("Could not find assembly: " + assembly);
        const method = wrapped_c_functions.mono_wasm_assembly_get_entry_point(asm);
        if (!method)
            throw new Error("Could not find entry point for assembly: " + assembly);
        if (typeof signature === "undefined")
            signature = mono_method_get_call_signature(method);
        return function (...args) {
            try {
                if (args.length > 0 && Array.isArray(args[0]))
                    args[0] = js_array_to_mono_array(args[0], true, false);
                const result = call_method(method, undefined, signature, args);
                return Promise.resolve(result);
            }
            catch (error) {
                return Promise.reject(error);
            }
        };
    }
    function mono_call_assembly_entry_point(assembly, args, signature) {
        return mono_bind_assembly_entry_point(assembly, signature)(...args);
    }
    function mono_wasm_invoke_js_with_args(js_handle, method_name, args, is_exception) {
        const argsRoot = mono_wasm_new_root(args), nameRoot = mono_wasm_new_root(method_name);
        try {
            const js_name = conv_string(nameRoot.value);
            if (!js_name || (typeof (js_name) !== "string")) {
                return wrap_error(is_exception, "ERR12: Invalid method name object '" + nameRoot.value + "'");
            }
            const obj = get_js_obj(js_handle);
            if (!obj) {
                return wrap_error(is_exception, "ERR13: Invalid JS object handle '" + js_handle + "' while invoking '" + js_name + "'");
            }
            const js_args = _mono_array_root_to_js_array(argsRoot);
            try {
                const m = obj[js_name];
                if (typeof m === "undefined")
                    throw new Error("Method: '" + js_name + "' not found for: '" + Object.prototype.toString.call(obj) + "'");
                const res = m.apply(obj, js_args);
                return _js_to_mono_obj(true, res);
            }
            catch (ex) {
                return wrap_error(is_exception, ex);
            }
        }
        finally {
            argsRoot.release();
            nameRoot.release();
        }
    }
    function mono_wasm_get_object_property(js_handle, property_name, is_exception) {
        const nameRoot = mono_wasm_new_root(property_name);
        try {
            const js_name = conv_string(nameRoot.value);
            if (!js_name) {
                return wrap_error(is_exception, "Invalid property name object '" + nameRoot.value + "'");
            }
            const obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
            if (!obj) {
                return wrap_error(is_exception, "ERR01: Invalid JS object handle '" + js_handle + "' while geting '" + js_name + "'");
            }
            try {
                const m = obj[js_name];
                return _js_to_mono_obj(true, m);
            }
            catch (ex) {
                return wrap_error(is_exception, ex);
            }
        }
        finally {
            nameRoot.release();
        }
    }
    function mono_wasm_set_object_property(js_handle, property_name, value, createIfNotExist, hasOwnProperty, is_exception) {
        const valueRoot = mono_wasm_new_root(value), nameRoot = mono_wasm_new_root(property_name);
        try {
            const property = conv_string(nameRoot.value);
            if (!property) {
                return wrap_error(is_exception, "Invalid property name object '" + property_name + "'");
            }
            const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
            if (!js_obj) {
                return wrap_error(is_exception, "ERR02: Invalid JS object handle '" + js_handle + "' while setting '" + property + "'");
            }
            let result = false;
            const js_value = _unbox_mono_obj_root(valueRoot);
            if (createIfNotExist) {
                js_obj[property] = js_value;
                result = true;
            }
            else {
                result = false;
                if (!createIfNotExist) {
                    if (!Object.prototype.hasOwnProperty.call(js_obj, property))
                        return _box_js_bool(false);
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
            return _box_js_bool(result);
        }
        finally {
            nameRoot.release();
            valueRoot.release();
        }
    }
    function mono_wasm_get_by_index(js_handle, property_index, is_exception) {
        const obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        if (!obj) {
            return wrap_error(is_exception, "ERR03: Invalid JS object handle '" + js_handle + "' while getting [" + property_index + "]");
        }
        try {
            const m = obj[property_index];
            return _js_to_mono_obj(true, m);
        }
        catch (ex) {
            return wrap_error(is_exception, ex);
        }
    }
    function mono_wasm_set_by_index(js_handle, property_index, value, is_exception) {
        const valueRoot = mono_wasm_new_root(value);
        try {
            const obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
            if (!obj) {
                return wrap_error(is_exception, "ERR04: Invalid JS object handle '" + js_handle + "' while setting [" + property_index + "]");
            }
            const js_value = _unbox_mono_obj_root(valueRoot);
            try {
                obj[property_index] = js_value;
                return true; // TODO check
            }
            catch (ex) {
                return wrap_error(is_exception, ex);
            }
        }
        finally {
            valueRoot.release();
        }
    }
    function mono_wasm_get_global_object(global_name, is_exception) {
        const nameRoot = mono_wasm_new_root(global_name);
        try {
            const js_name = conv_string(nameRoot.value);
            let globalObj;
            if (!js_name) {
                globalObj = globalThis;
            }
            else {
                globalObj = globalThis[js_name];
            }
            // TODO returning null may be useful when probing for browser features
            if (globalObj === null || typeof globalObj === undefined) {
                return wrap_error(is_exception, "Global object '" + js_name + "' not found.");
            }
            return _js_to_mono_obj(true, globalObj);
        }
        finally {
            nameRoot.release();
        }
    }
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function wrap_error(is_exception, ex) {
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
        }
        if (is_exception) {
            Module.setValue(is_exception, 1, "i32");
        }
        return js_string_to_mono_string(res);
    }
    function mono_method_get_call_signature(method, mono_obj) {
        const instanceRoot = mono_wasm_new_root(mono_obj);
        try {
            return call_method(runtimeHelpers.get_call_sig, undefined, "im", [method, instanceRoot.value]);
        }
        finally {
            instanceRoot.release();
        }
    }
    function mono_method_resolve(fqn) {
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
            throw new Error("No assembly name specified");
        if (!classname.trim())
            throw new Error("No class name specified");
        if (!methodname.trim())
            throw new Error("No method name specified");
        const asm = wrapped_c_functions.mono_wasm_assembly_load(assembly);
        if (!asm)
            throw new Error("Could not find assembly: " + assembly);
        const klass = wrapped_c_functions.mono_wasm_assembly_find_class(asm, namespace, classname);
        if (!klass)
            throw new Error("Could not find class: " + namespace + ":" + classname + " in assembly " + assembly);
        const method = find_method(klass, methodname, -1);
        if (!method)
            throw new Error("Could not find method: " + methodname);
        return method;
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
            const exceptionSystemString = wrapped_c_functions.mono_wasm_string_from_js(exceptionJsString);
            Module.setValue(exceptionMessage, exceptionSystemString, "i32"); // *exceptionMessage = exceptionSystemString;
            return 0;
        }
    }
    // code like `App.call_test_method();`
    function mono_wasm_invoke_js(code, is_exception) {
        if (code === MonoStringNull)
            return MonoStringNull;
        const js_code = conv_string(code);
        try {
            const closedEval = function (Module, MONO, BINDING, INTERNAL, code) {
                return eval(code);
            };
            const res = closedEval(Module, MONO$1, BINDING$1, INTERNAL$1, js_code);
            Module.setValue(is_exception, 0, "i32");
            if (typeof res === "undefined" || res === null)
                return MonoStringNull;
            return js_string_to_mono_string(res.toString());
        }
        catch (ex) {
            return wrap_error(is_exception, ex);
        }
    }
    // TODO is this unused code ?
    // Compiles a JavaScript function from the function data passed.
    // Note: code snippet is not a function definition. Instead it must create and return a function instance.
    // code like `return function() { App.call_test_method(); };`
    function mono_wasm_compile_function(code, is_exception) {
        if (code === MonoStringNull)
            return MonoStringNull;
        const js_code = conv_string(code);
        try {
            const closure = {
                Module, MONO: MONO$1, BINDING: BINDING$1, INTERNAL: INTERNAL$1
            };
            const fn_body_template = `const {Module, MONO, BINDING, INTERNAL} = __closure; ${js_code} ;`;
            const fn_defn = new Function("__closure", fn_body_template);
            const res = fn_defn(closure);
            if (!res || typeof res !== "function")
                return wrap_error(is_exception, "Code must return an instance of a JavaScript function. Please use `return` statement to return a function.");
            Module.setValue(is_exception, 0, "i32");
            return _js_to_mono_obj(true, res);
        }
        catch (ex) {
            return wrap_error(is_exception, ex);
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    // Creates a new typed array from pinned array address from pinned_array allocated on the heap to the typed array.
    // 	 adress of managed pinned array -> copy from heap -> typed array memory
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
    // Copy the existing typed array to the heap pointed to by the pinned array address
    // 	 typed array memory -> copy to heap -> address of managed pinned array
    function typedarray_copy_to(typed_array, pinned_array, begin, end, bytes_per_element) {
        // JavaScript typed arrays are array-like objects and provide a mechanism for accessing
        // raw binary data. (...) To achieve maximum flexibility and efficiency, JavaScript typed arrays
        // split the implementation into buffers and views. A buffer (implemented by the ArrayBuffer object)
        //  is an object representing a chunk of data; it has no format to speak of, and offers no
        // mechanism for accessing its contents. In order to access the memory contained in a buffer,
        // you need to use a view. A view provides a context — that is, a data type, starting offset,
        // and number of elements — that turns the data into an actual typed array.
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
            // offset index into the view
            const offset = begin * bytes_per_element;
            // Create a view over the heap pointed to by the pinned array address
            const heapBytes = new Uint8Array(Module.HEAPU8.buffer, pinned_array + offset, num_of_bytes);
            // Copy the bytes of the typed array to the heap.
            heapBytes.set(new Uint8Array(typed_array.buffer, typed_array.byteOffset, num_of_bytes));
            return num_of_bytes;
        }
        else {
            throw new Error("Object '" + typed_array + "' is not a typed array");
        }
    }
    // Copy the pinned array address from pinned_array allocated on the heap to the typed array.
    // 	 adress of managed pinned array -> copy from heap -> typed array memory
    function typedarray_copy_from(typed_array, pinned_array, begin, end, bytes_per_element) {
        // JavaScript typed arrays are array-like objects and provide a mechanism for accessing
        // raw binary data. (...) To achieve maximum flexibility and efficiency, JavaScript typed arrays
        // split the implementation into buffers and views. A buffer (implemented by the ArrayBuffer object)
        //  is an object representing a chunk of data; it has no format to speak of, and offers no
        // mechanism for accessing its contents. In order to access the memory contained in a buffer,
        // you need to use a view. A view provides a context — that is, a data type, starting offset,
        // and number of elements — that turns the data into an actual typed array.
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
    function mono_wasm_typed_array_copy_to(js_handle, pinned_array, begin, end, bytes_per_element, is_exception) {
        const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        if (!js_obj) {
            return wrap_error(is_exception, "ERR07: Invalid JS object handle '" + js_handle + "'");
        }
        const res = typedarray_copy_to(js_obj, pinned_array, begin, end, bytes_per_element);
        // returns num_of_bytes boxed
        return _js_to_mono_obj(false, res);
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function mono_wasm_typed_array_from(pinned_array, begin, end, bytes_per_element, type, is_exception) {
        const res = typed_array_from(pinned_array, begin, end, bytes_per_element, type);
        // returns JS typed array like Int8Array, to be wraped with JSObject proxy
        return _js_to_mono_obj(true, res);
    }
    function mono_wasm_typed_array_copy_from(js_handle, pinned_array, begin, end, bytes_per_element, is_exception) {
        const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        if (!js_obj) {
            return wrap_error(is_exception, "ERR08: Invalid JS object handle '" + js_handle + "'");
        }
        const res = typedarray_copy_from(js_obj, pinned_array, begin, end, bytes_per_element);
        // returns num_of_bytes boxed
        return _js_to_mono_obj(false, res);
    }
    function has_backing_array_buffer(js_obj) {
        return typeof SharedArrayBuffer !== "undefined"
            ? js_obj.buffer instanceof ArrayBuffer || js_obj.buffer instanceof SharedArrayBuffer
            : js_obj.buffer instanceof ArrayBuffer;
    }
    // @bytes must be a typed array. space is allocated for it in the native heap
    //  and it is copied to that location. returns the address of the allocation.
    function mono_wasm_load_bytes_into_heap(bytes) {
        const memoryOffset = Module._malloc(bytes.length);
        const heapBytes = new Uint8Array(Module.HEAPU8.buffer, memoryOffset, bytes.length);
        heapBytes.set(bytes);
        return memoryOffset;
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
        const assembly = wrapped_c_functions.mono_wasm_assembly_load(assembly_name);
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

    // Licensed to the .NET Foundation under one or more agreements.
    let runtime_is_initialized_resolve;
    let runtime_is_initialized_reject;
    const mono_wasm_runtime_is_initialized = new Promise((resolve, reject) => {
        runtime_is_initialized_resolve = resolve;
        runtime_is_initialized_reject = reject;
    });
    async function mono_wasm_pre_init() {
        const moduleExt = Module;
        if (moduleExt.configSrc) {
            try {
                // sets MONO.config implicitly
                await mono_wasm_load_config(moduleExt.configSrc);
            }
            catch (err) {
                runtime_is_initialized_reject(err);
                throw err;
            }
            if (moduleExt.onConfigLoaded) {
                try {
                    moduleExt.onConfigLoaded();
                }
                catch (err) {
                    Module.printErr("MONO_WASM: onConfigLoaded () failed: " + err);
                    Module.printErr("MONO_WASM: Stacktrace: \n");
                    Module.printErr(err.stack);
                    runtime_is_initialized_reject(err);
                    throw err;
                }
            }
        }
    }
    function mono_wasm_on_runtime_initialized() {
        const moduleExt = Module;
        if (!moduleExt.config || moduleExt.config.isError) {
            return;
        }
        mono_load_runtime_and_bcl_args(moduleExt.config);
    }
    // Set environment variable NAME to VALUE
    // Should be called before mono_load_runtime_and_bcl () in most cases
    function mono_wasm_setenv(name, value) {
        wrapped_c_functions.mono_wasm_setenv(name, value);
    }
    function mono_wasm_set_runtime_options(options) {
        const argv = Module._malloc(options.length * 4);
        let aindex = 0;
        for (let i = 0; i < options.length; ++i) {
            Module.setValue(argv + (aindex * 4), wrapped_c_functions.mono_wasm_strdup(options[i]), "i32");
            aindex += 1;
        }
        wrapped_c_functions.mono_wasm_parse_runtime_options(options.length, argv);
    }
    async function _fetch_asset(url) {
        try {
            if (typeof (fetch) === "function") {
                return fetch(url, { credentials: "same-origin" });
            }
            else if (ENVIRONMENT_IS_NODE) {
                //const fs = (<any>globalThis).require("fs");
                // eslint-disable-next-line @typescript-eslint/no-var-requires
                const fs = require("fs");
                const arrayBuffer = await fs.promises.readFile(url);
                return {
                    ok: true,
                    url,
                    arrayBuffer: () => arrayBuffer,
                    json: () => JSON.parse(arrayBuffer)
                };
            }
            else if (typeof (read) === "function") {
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
                arrayBuffer: () => { throw e; },
                json: () => { throw e; }
            };
        }
        throw new Error("No fetch implementation available");
    }
    function _handle_fetched_asset(ctx, asset, url, blob) {
        const bytes = new Uint8Array(blob);
        if (ctx.tracing)
            console.log(`MONO_WASM: Loaded:${asset.name} as ${asset.behavior} size ${bytes.length} from ${url}`);
        const virtualName = asset.virtual_path || asset.name;
        let offset = null;
        switch (asset.behavior) {
            case "resource":
            case "assembly":
                ctx.loaded_files.push({ url: url, file: virtualName });
            // falls through
            case "heap":
            case "icu":
                offset = mono_wasm_load_bytes_into_heap(bytes);
                ctx.loaded_assets[virtualName] = [offset, bytes.length];
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
                    if (ctx.tracing)
                        console.log(`MONO_WASM: Creating directory '${parentDirectory}'`);
                    ctx.createPath("/", parentDirectory, true, true // fixme: should canWrite be false?
                    );
                }
                else {
                    parentDirectory = "/";
                }
                if (ctx.tracing)
                    console.log(`MONO_WASM: Creating file '${fileName}' in directory '${parentDirectory}'`);
                if (!mono_wasm_load_data_archive(bytes, parentDirectory)) {
                    ctx.createDataFile(parentDirectory, fileName, bytes, true /* canRead */, true /* canWrite */, true /* canOwn */);
                }
                break;
            }
            default:
                throw new Error(`Unrecognized asset behavior:${asset.behavior}, for asset ${asset.name}`);
        }
        if (asset.behavior === "assembly") {
            const hasPpdb = wrapped_c_functions.mono_wasm_add_assembly(virtualName, offset, bytes.length);
            if (!hasPpdb) {
                const index = ctx.loaded_files.findIndex(element => element.file == virtualName);
                ctx.loaded_files.splice(index, 1);
            }
        }
        else if (asset.behavior === "icu") {
            if (!mono_wasm_load_icu_data(offset))
                console.error(`MONO_WASM: Error loading ICU asset ${asset.name}`);
        }
        else if (asset.behavior === "resource") {
            wrapped_c_functions.mono_wasm_add_satellite_assembly(virtualName, asset.culture, offset, bytes.length);
        }
    }
    function _apply_configuration_from_args(args) {
        for (const k in (args.environment_variables || {}))
            mono_wasm_setenv(k, args.environment_variables[k]);
        if (args.runtime_options)
            mono_wasm_set_runtime_options(args.runtime_options);
        if (args.aot_profiler_options)
            mono_wasm_init_aot_profiler(args.aot_profiler_options);
        if (args.coverage_profiler_options)
            mono_wasm_init_coverage_profiler(args.coverage_profiler_options);
    }
    function _finalize_startup(args, ctx) {
        const moduleExt = Module;
        ctx.loaded_files.forEach(value => MONO$1.loaded_files.push(value.url));
        if (ctx.tracing) {
            console.log("MONO_WASM: loaded_assets: " + JSON.stringify(ctx.loaded_assets));
            console.log("MONO_WASM: loaded_files: " + JSON.stringify(ctx.loaded_files));
        }
        console.debug("MONO_WASM: Initializing mono runtime");
        mono_wasm_globalization_init(args.globalization_mode);
        if (ENVIRONMENT_IS_SHELL || ENVIRONMENT_IS_NODE) {
            try {
                wrapped_c_functions.mono_wasm_load_runtime("unused", args.debug_level || 0);
            }
            catch (err) {
                Module.printErr("MONO_WASM: mono_wasm_load_runtime () failed: " + err);
                Module.printErr("MONO_WASM: Stacktrace: \n");
                Module.printErr(err.stack);
                runtime_is_initialized_reject(err);
                const wasm_exit = wrapped_c_functions.mono_wasm_exit;
                wasm_exit(1);
            }
        }
        else {
            wrapped_c_functions.mono_wasm_load_runtime("unused", args.debug_level || 0);
        }
        bindings_lazy_init();
        let tz;
        try {
            tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
        }
        catch (_a) {
            //swallow
        }
        mono_wasm_setenv("TZ", tz || "UTC");
        mono_wasm_runtime_ready();
        //legacy config loading
        const argsAny = args;
        if (argsAny.loaded_cb) {
            try {
                argsAny.loaded_cb();
            }
            catch (err) {
                Module.printErr("MONO_WASM: loaded_cb () failed: " + err);
                Module.printErr("MONO_WASM: Stacktrace: \n");
                Module.printErr(err.stack);
                runtime_is_initialized_reject(err);
                throw err;
            }
        }
        if (moduleExt.onDotNetReady) {
            try {
                moduleExt.onDotNetReady();
            }
            catch (err) {
                Module.printErr("MONO_WASM: onDotNetReady () failed: " + err);
                Module.printErr("MONO_WASM: Stacktrace: \n");
                Module.printErr(err.stack);
                runtime_is_initialized_reject(err);
                throw err;
            }
        }
        runtime_is_initialized_resolve();
    }
    function bindings_lazy_init() {
        if (runtimeHelpers.mono_wasm_bindings_is_ready)
            return;
        runtimeHelpers.mono_wasm_bindings_is_ready = true;
        // please keep System.Runtime.InteropServices.JavaScript.Runtime.MappedType in sync
        Object.prototype[wasm_type_symbol] = 0;
        Array.prototype[wasm_type_symbol] = 1;
        ArrayBuffer.prototype[wasm_type_symbol] = 2;
        DataView.prototype[wasm_type_symbol] = 3;
        Function.prototype[wasm_type_symbol] = 4;
        Map.prototype[wasm_type_symbol] = 5;
        if (typeof SharedArrayBuffer !== "undefined")
            SharedArrayBuffer.prototype[wasm_type_symbol] = 6;
        Int8Array.prototype[wasm_type_symbol] = 10;
        Uint8Array.prototype[wasm_type_symbol] = 11;
        Uint8ClampedArray.prototype[wasm_type_symbol] = 12;
        Int16Array.prototype[wasm_type_symbol] = 13;
        Uint16Array.prototype[wasm_type_symbol] = 14;
        Int32Array.prototype[wasm_type_symbol] = 15;
        Uint32Array.prototype[wasm_type_symbol] = 16;
        Float32Array.prototype[wasm_type_symbol] = 17;
        Float64Array.prototype[wasm_type_symbol] = 18;
        runtimeHelpers._box_buffer_size = 65536;
        runtimeHelpers._unbox_buffer_size = 65536;
        runtimeHelpers._box_buffer = Module._malloc(runtimeHelpers._box_buffer_size);
        runtimeHelpers._unbox_buffer = Module._malloc(runtimeHelpers._unbox_buffer_size);
        runtimeHelpers._class_int32 = find_corlib_class("System", "Int32");
        runtimeHelpers._class_uint32 = find_corlib_class("System", "UInt32");
        runtimeHelpers._class_double = find_corlib_class("System", "Double");
        runtimeHelpers._class_boolean = find_corlib_class("System", "Boolean");
        runtimeHelpers.bind_runtime_method = bind_runtime_method;
        const bindingAssembly = INTERNAL$1.BINDING_ASM;
        const binding_fqn_asm = bindingAssembly.substring(bindingAssembly.indexOf("[") + 1, bindingAssembly.indexOf("]")).trim();
        const binding_fqn_class = bindingAssembly.substring(bindingAssembly.indexOf("]") + 1).trim();
        const binding_module = wrapped_c_functions.mono_wasm_assembly_load(binding_fqn_asm);
        if (!binding_module)
            throw "Can't find bindings module assembly: " + binding_fqn_asm;
        if (binding_fqn_class && binding_fqn_class.length) {
            runtimeHelpers.runtime_classname = binding_fqn_class;
            if (binding_fqn_class.indexOf(".") != -1) {
                const idx = binding_fqn_class.lastIndexOf(".");
                runtimeHelpers.runtime_namespace = binding_fqn_class.substring(0, idx);
                runtimeHelpers.runtime_classname = binding_fqn_class.substring(idx + 1);
            }
        }
        runtimeHelpers.wasm_runtime_class = wrapped_c_functions.mono_wasm_assembly_find_class(binding_module, runtimeHelpers.runtime_namespace, runtimeHelpers.runtime_classname);
        if (!runtimeHelpers.wasm_runtime_class)
            throw "Can't find " + binding_fqn_class + " class";
        runtimeHelpers.get_call_sig = get_method("GetCallSignature");
        if (!runtimeHelpers.get_call_sig)
            throw "Can't find GetCallSignature method";
        _create_primitive_converters();
    }
    // Initializes the runtime and loads assemblies, debug information, and other files.
    async function mono_load_runtime_and_bcl_args(args) {
        try {
            if (args.enable_debugging)
                args.debug_level = args.enable_debugging;
            const ctx = {
                tracing: args.diagnostic_tracing || false,
                pending_count: args.assets.length,
                loaded_assets: Object.create(null),
                // dlls and pdbs, used by blazor and the debugger
                loaded_files: [],
                createPath: Module.FS_createPath,
                createDataFile: Module.FS_createDataFile
            };
            _apply_configuration_from_args(args);
            const local_fetch = typeof (args.fetch_file_cb) === "function" ? args.fetch_file_cb : _fetch_asset;
            const load_asset = async (asset) => {
                //TODO we could do module.addRunDependency(asset.name) and delay emscripten run() after all assets are loaded
                const sourcesList = asset.load_remote ? args.remote_sources : [""];
                let error = undefined;
                for (let sourcePrefix of sourcesList) {
                    // HACK: Special-case because MSBuild doesn't allow "" as an attribute
                    if (sourcePrefix === "./")
                        sourcePrefix = "";
                    let attemptUrl;
                    if (sourcePrefix.trim() === "") {
                        if (asset.behavior === "assembly")
                            attemptUrl = locateFile(args.assembly_root + "/" + asset.name);
                        else if (asset.behavior === "resource") {
                            const path = asset.culture !== "" ? `${asset.culture}/${asset.name}` : asset.name;
                            attemptUrl = locateFile(args.assembly_root + "/" + path);
                        }
                        else
                            attemptUrl = asset.name;
                    }
                    else {
                        attemptUrl = sourcePrefix + asset.name;
                    }
                    if (asset.name === attemptUrl) {
                        if (ctx.tracing)
                            console.log(`MONO_WASM: Attempting to fetch '${attemptUrl}'`);
                    }
                    else {
                        if (ctx.tracing)
                            console.log(`MONO_WASM: Attempting to fetch '${attemptUrl}' for ${asset.name}`);
                    }
                    try {
                        const response = await local_fetch(attemptUrl);
                        if (!response.ok) {
                            error = new Error(`MONO_WASM: Fetch '${attemptUrl}' for ${asset.name} failed ${response.status} ${response.statusText}`);
                            continue; // next source
                        }
                        const buffer = await response.arrayBuffer();
                        _handle_fetched_asset(ctx, asset, attemptUrl, buffer);
                        --ctx.pending_count;
                        error = undefined;
                    }
                    catch (err) {
                        error = new Error(`MONO_WASM: Fetch '${attemptUrl}' for ${asset.name} failed ${err}`);
                        continue; //next source
                    }
                    if (!error) {
                        //TODO Module.removeRunDependency(configFilePath);
                        break; // this source worked, stop searching
                    }
                }
                if (error) {
                    const isOkToFail = asset.is_optional || (asset.name.match(/\.pdb$/) && args.ignore_pdb_load_errors);
                    if (!isOkToFail)
                        throw error;
                }
            };
            const fetch_promises = [];
            // start fetching all assets in parallel
            for (const asset of args.assets) {
                fetch_promises.push(load_asset(asset));
            }
            await Promise.all(fetch_promises);
            _finalize_startup(args, ctx);
        }
        catch (err) {
            console.error("MONO_WASM: Error in mono_load_runtime_and_bcl_args:", err);
            runtime_is_initialized_reject(err);
            throw err;
        }
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
    /**
     * Loads the mono config file (typically called mono-config.json) asynchroniously
     * Note: the run dependencies are so emsdk actually awaits it in order.
     *
     * @param {string} configFilePath - relative path to the config file
     * @throws Will throw an error if the config file loading fails
     */
    async function mono_wasm_load_config(configFilePath) {
        const module = Module;
        module.addRunDependency(configFilePath);
        try {
            // NOTE: when we add nodejs make sure to include the nodejs fetch package
            const configRaw = await _fetch_asset(configFilePath);
            const config = await configRaw.json();
            runtimeHelpers.config = config;
            config.environment_variables = config.environment_variables || {};
            config.assets = config.assets || [];
            config.runtime_options = config.runtime_options || [];
            config.globalization_mode = config.globalization_mode || "auto" /* AUTO */;
        }
        catch (err) {
            const errMessage = `Failed to load config file ${configFilePath} ${err}`;
            console.error(errMessage);
            runtimeHelpers.config = { message: errMessage, error: err, isError: true };
            runtime_is_initialized_reject(err);
            throw err;
        }
        finally {
            Module.removeRunDependency(configFilePath);
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
        Module.setValue(main_argv + (aindex * 4), INTERNAL$1.mono_wasm_strdup(name), "i32");
        aindex += 1;
        for (let i = 0; i < allRuntimeArguments.length; ++i) {
            Module.setValue(main_argv + (aindex * 4), INTERNAL$1.mono_wasm_strdup(allRuntimeArguments[i]), "i32");
            aindex += 1;
        }
        wrapped_c_functions.mono_wasm_set_main_args(main_argc, main_argv);
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const timeout_queue = [];
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
        while (timeout_queue.length > 0) {
            --pump_count;
            const cb = timeout_queue.shift();
            cb();
        }
        while (pump_count > 0) {
            --pump_count;
            wrapped_c_functions.mono_background_exec();
        }
    }
    function mono_wasm_set_timeout_exec(id) {
        wrapped_c_functions.mono_set_timeout_exec(id);
    }
    function prevent_timer_throttling() {
        if (isChromium) {
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
                mono_wasm_set_timeout_exec(0);
                pump_count++;
                pump_message();
            }, delay);
        }
        spread_timers_maximum = desired_reach_time;
    }
    function schedule_background_exec() {
        ++pump_count;
        if (typeof globalThis.setTimeout === "function") {
            globalThis.setTimeout(pump_message, 0);
        }
    }
    function mono_set_timeout(timeout, id) {
        if (typeof globalThis.setTimeout === "function") {
            globalThis.setTimeout(function () {
                mono_wasm_set_timeout_exec(id);
            }, timeout);
        }
        else {
            ++pump_count;
            timeout_queue.push(function () {
                mono_wasm_set_timeout_exec(id);
            });
        }
    }

    // Licensed to the .NET Foundation under one or more agreements.
    const listener_registration_count_symbol = Symbol.for("wasm listener_registration_count");
    function mono_wasm_add_event_listener(js_handle, name, listener_gc_handle, optionsHandle) {
        const nameRoot = mono_wasm_new_root(name);
        try {
            const sName = conv_string(nameRoot.value);
            const obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
            if (!obj)
                throw new Error("ERR09: Invalid JS object handle for '" + sName + "'");
            const throttling = isChromium || obj.constructor.name !== "WebSocket"
                ? undefined
                : prevent_timer_throttling;
            const listener = _wrap_delegate_gc_handle_as_function(listener_gc_handle, throttling);
            if (!listener)
                throw new Error("ERR10: Invalid listener gc_handle");
            const options = optionsHandle
                ? mono_wasm_get_jsobj_from_js_handle(optionsHandle)
                : null;
            if (!_use_finalization_registry) {
                // we are counting registrations because same delegate could be registered into multiple sources
                listener[listener_registration_count_symbol] = listener[listener_registration_count_symbol] ? listener[listener_registration_count_symbol] + 1 : 1;
            }
            if (options)
                obj.addEventListener(sName, listener, options);
            else
                obj.addEventListener(sName, listener);
            return MonoStringNull;
        }
        catch (ex) {
            return wrap_error(null, ex);
        }
        finally {
            nameRoot.release();
        }
    }
    function mono_wasm_remove_event_listener(js_handle, name, listener_gc_handle, capture) {
        const nameRoot = mono_wasm_new_root(name);
        try {
            const obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
            if (!obj)
                throw new Error("ERR11: Invalid JS object handle");
            const listener = _lookup_js_owned_object(listener_gc_handle);
            // Removing a nonexistent listener should not be treated as an error
            if (!listener)
                return MonoStringNull;
            const sName = conv_string(nameRoot.value);
            obj.removeEventListener(sName, listener, !!capture);
            // We do not manually remove the listener from the delegate registry here,
            //  because that same delegate may have been used as an event listener for
            //  other events or event targets. The GC will automatically clean it up
            //  and trigger the FinalizationRegistry handler if it's unused
            // When FinalizationRegistry is not supported by this browser, we cleanup manuall after unregistration
            if (!_use_finalization_registry) {
                listener[listener_registration_count_symbol]--;
                if (listener[listener_registration_count_symbol] === 0) {
                    _js_owned_object_finalized(listener_gc_handle);
                }
            }
            return MonoStringNull;
        }
        catch (ex) {
            return wrap_error(null, ex);
        }
        finally {
            nameRoot.release();
        }
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
            if (this.queue.length == 0)
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
    let mono_wasm_web_socket_close_warning = false;
    let _text_decoder_utf8 = undefined;
    let _text_encoder_utf8 = undefined;
    const ws_send_buffer_blocking_threshold = 65536;
    const emptyBuffer = new Uint8Array();
    function mono_wasm_web_socket_open(uri, subProtocols, on_close, web_socket_js_handle, thenable_js_handle, is_exception) {
        const uri_root = mono_wasm_new_root(uri);
        const sub_root = mono_wasm_new_root(subProtocols);
        const on_close_root = mono_wasm_new_root(on_close);
        try {
            const js_uri = conv_string(uri_root.value);
            if (!js_uri) {
                return wrap_error(is_exception, "ERR12: Invalid uri '" + uri_root.value + "'");
            }
            const js_subs = _mono_array_root_to_js_array(sub_root);
            const js_on_close = _wrap_delegate_root_as_function(on_close_root);
            const ws = new globalThis.WebSocket(js_uri, js_subs);
            const { promise, promise_control: open_promise_control } = _create_cancelable_promise();
            ws[wasm_ws_pending_receive_event_queue] = new Queue();
            ws[wasm_ws_pending_receive_promise_queue] = new Queue();
            ws[wasm_ws_pending_open_promise] = open_promise_control;
            ws[wasm_ws_pending_send_promises] = [];
            ws[wasm_ws_pending_close_promises] = [];
            ws.binaryType = "arraybuffer";
            const local_on_open = () => {
                if (ws[wasm_ws_is_aborted])
                    return;
                open_promise_control.resolve(null);
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
                js_on_close(ev.code, ev.reason);
                // this reject would not do anything if there was already "open" before it.
                open_promise_control.reject(ev.reason);
                for (const close_promise_control of ws[wasm_ws_pending_close_promises]) {
                    close_promise_control.resolve();
                }
                // send close to any pending receivers, to wake them
                const receive_promise_queue = ws[wasm_ws_pending_receive_promise_queue];
                receive_promise_queue.drain((receive_promise_control) => {
                    const response_root = receive_promise_control.response_root;
                    Module.setValue(response_root.value + 0, 0, "i32"); // count
                    Module.setValue(response_root.value + 4, 2, "i32"); // type:close
                    Module.setValue(response_root.value + 8, 1, "i32"); // end_of_message: true
                    receive_promise_control.resolve(null);
                });
            };
            ws.addEventListener("message", local_on_message);
            ws.addEventListener("open", local_on_open, { once: true });
            ws.addEventListener("close", local_on_close, { once: true });
            const ws_js_handle = mono_wasm_get_js_handle(ws);
            Module.setValue(web_socket_js_handle, ws_js_handle, "i32");
            const { task_ptr, then_js_handle } = _wrap_js_thenable_as_task(promise);
            // task_ptr above is not rooted, we need to return it to mono without any intermediate mono call which could cause GC
            Module.setValue(thenable_js_handle, then_js_handle, "i32");
            return task_ptr;
        }
        catch (ex) {
            return wrap_error(is_exception, ex);
        }
        finally {
            uri_root.release();
            sub_root.release();
            on_close_root.release();
        }
    }
    function mono_wasm_web_socket_send(webSocket_js_handle, buffer_ptr, offset, length, message_type, end_of_message, thenable_js_handle, is_exception) {
        const buffer_root = mono_wasm_new_root(buffer_ptr);
        try {
            const ws = mono_wasm_get_jsobj_from_js_handle(webSocket_js_handle);
            if (!ws)
                throw new Error("ERR17: Invalid JS object handle " + webSocket_js_handle);
            if (ws.readyState != WebSocket.OPEN) {
                throw new Error("InvalidState: The WebSocket is not connected.");
            }
            const whole_buffer = _mono_wasm_web_socket_send_buffering(ws, buffer_root, offset, length, message_type, end_of_message);
            if (!end_of_message || !whole_buffer) {
                return MonoObjectNull; // we are done buffering synchronously, no promise
            }
            return _mono_wasm_web_socket_send_and_wait(ws, whole_buffer, thenable_js_handle);
        }
        catch (ex) {
            return wrap_error(is_exception, ex);
        }
        finally {
            buffer_root.release();
        }
    }
    function mono_wasm_web_socket_receive(webSocket_js_handle, buffer_ptr, offset, length, response_ptr, thenable_js_handle, is_exception) {
        const buffer_root = mono_wasm_new_root(buffer_ptr);
        const response_root = mono_wasm_new_root(response_ptr);
        const release_buffer = () => {
            buffer_root.release();
            response_root.release();
        };
        try {
            const ws = mono_wasm_get_jsobj_from_js_handle(webSocket_js_handle);
            if (!ws)
                throw new Error("ERR18: Invalid JS object handle " + webSocket_js_handle);
            const receive_event_queue = ws[wasm_ws_pending_receive_event_queue];
            const receive_promise_queue = ws[wasm_ws_pending_receive_promise_queue];
            const readyState = ws.readyState;
            if (readyState != WebSocket.OPEN && readyState != WebSocket.CLOSING) {
                throw new Error("InvalidState: The WebSocket is not connected.");
            }
            if (receive_event_queue.getLength()) {
                if (receive_promise_queue.getLength() != 0) {
                    throw new Error("ERR20: Invalid WS state"); // assert
                }
                // finish synchronously
                _mono_wasm_web_socket_receive_buffering(receive_event_queue, buffer_root, offset, length, response_root);
                release_buffer();
                Module.setValue(thenable_js_handle, 0, "i32");
                return MonoObjectNull;
            }
            const { promise, promise_control } = _create_cancelable_promise(release_buffer, release_buffer);
            const receive_promise_control = promise_control;
            receive_promise_control.buffer_root = buffer_root;
            receive_promise_control.buffer_offset = offset;
            receive_promise_control.buffer_length = length;
            receive_promise_control.response_root = response_root;
            receive_promise_queue.enqueue(receive_promise_control);
            const { task_ptr, then_js_handle } = _wrap_js_thenable_as_task(promise);
            // task_ptr above is not rooted, we need to return it to mono without any intermediate mono call which could cause GC
            Module.setValue(thenable_js_handle, then_js_handle, "i32");
            return task_ptr;
        }
        catch (ex) {
            return wrap_error(is_exception, ex);
        }
    }
    function mono_wasm_web_socket_close(webSocket_js_handle, code, reason, wait_for_close_received, thenable_js_handle, is_exception) {
        const reason_root = mono_wasm_new_root(reason);
        try {
            const ws = mono_wasm_get_jsobj_from_js_handle(webSocket_js_handle);
            if (!ws)
                throw new Error("ERR19: Invalid JS object handle " + webSocket_js_handle);
            if (ws.readyState == WebSocket.CLOSED) {
                return MonoObjectNull; // no promise
            }
            const js_reason = conv_string(reason_root.value);
            if (wait_for_close_received) {
                const { promise, promise_control } = _create_cancelable_promise();
                ws[wasm_ws_pending_close_promises].push(promise_control);
                if (js_reason) {
                    ws.close(code, js_reason);
                }
                else {
                    ws.close(code);
                }
                const { task_ptr, then_js_handle } = _wrap_js_thenable_as_task(promise);
                // task_ptr above is not rooted, we need to return it to mono without any intermediate mono call which could cause GC
                Module.setValue(thenable_js_handle, then_js_handle, "i32");
                return task_ptr;
            }
            else {
                if (!mono_wasm_web_socket_close_warning) {
                    mono_wasm_web_socket_close_warning = true;
                    console.warn("WARNING: Web browsers do not support closing the output side of a WebSocket. CloseOutputAsync has closed the socket and discarded any incoming messages.");
                }
                if (js_reason) {
                    ws.close(code, js_reason);
                }
                else {
                    ws.close(code);
                }
                Module.setValue(thenable_js_handle, 0, "i32");
                return MonoObjectNull; // no promise
            }
        }
        catch (ex) {
            return wrap_error(is_exception, ex);
        }
        finally {
            reason_root.release();
        }
    }
    function mono_wasm_web_socket_abort(webSocket_js_handle, is_exception) {
        try {
            const ws = mono_wasm_get_jsobj_from_js_handle(webSocket_js_handle);
            if (!ws)
                throw new Error("ERR18: Invalid JS object handle " + webSocket_js_handle);
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
            return MonoObjectNull;
        }
        catch (ex) {
            return wrap_error(is_exception, ex);
        }
    }
    function _mono_wasm_web_socket_send_and_wait(ws, buffer, thenable_js_handle) {
        // send and return promise
        ws.send(buffer);
        ws[wasm_ws_pending_send_buffer] = null;
        // if the remaining send buffer is small, we don't block so that the throughput doesn't suffer. 
        // Otherwise we block so that we apply some backpresure to the application sending large data.
        // this is different from Managed implementation
        if (ws.bufferedAmount < ws_send_buffer_blocking_threshold) {
            return MonoObjectNull; // no promise
        }
        // block the promise/task until the browser passed the buffer to OS
        const { promise, promise_control } = _create_cancelable_promise();
        const pending = ws[wasm_ws_pending_send_promises];
        pending.push(promise_control);
        let nextDelay = 1;
        const polling_check = () => {
            // was it all sent yet ?
            if (ws.bufferedAmount === 0) {
                promise_control.resolve(null);
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
        const { task_ptr, then_js_handle } = _wrap_js_thenable_as_task(promise);
        // task_ptr above is not rooted, we need to return it to mono without any intermediate mono call which could cause GC
        Module.setValue(thenable_js_handle, then_js_handle, "i32");
        return task_ptr;
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
            throw new Error("ERR20: Invalid WS state"); // assert
        }
        while (promise_queue.getLength() && event_queue.getLength()) {
            const promise_control = promise_queue.dequeue();
            _mono_wasm_web_socket_receive_buffering(event_queue, promise_control.buffer_root, promise_control.buffer_offset, promise_control.buffer_length, promise_control.response_root);
            promise_control.resolve(null);
        }
        prevent_timer_throttling();
    }
    function _mono_wasm_web_socket_receive_buffering(event_queue, buffer_root, buffer_offset, buffer_length, response_root) {
        const event = event_queue.peek();
        const count = Math.min(buffer_length, event.data.length - event.offset);
        if (count > 0) {
            const targetView = Module.HEAPU8.subarray(buffer_root.value + buffer_offset, buffer_root.value + buffer_offset + buffer_length);
            const sourceView = event.data.subarray(event.offset, event.offset + count);
            targetView.set(sourceView, 0);
            event.offset += count;
        }
        const end_of_message = event.data.length === event.offset ? 1 : 0;
        if (end_of_message) {
            event_queue.dequeue();
        }
        Module.setValue(response_root.value + 0, count, "i32");
        Module.setValue(response_root.value + 4, event.type, "i32");
        Module.setValue(response_root.value + 8, end_of_message, "i32");
    }
    function _mono_wasm_web_socket_send_buffering(ws, buffer_root, buffer_offset, length, message_type, end_of_message) {
        let buffer = ws[wasm_ws_pending_send_buffer];
        let offset = 0;
        const message_ptr = buffer_root.value + buffer_offset;
        if (buffer) {
            offset = ws[wasm_ws_pending_send_buffer_offset];
            // match desktop WebSocket behavior by copying message_type of the first part
            message_type = ws[wasm_ws_pending_send_buffer_type];
            // if not empty message, append to existing buffer
            if (length !== 0) {
                const view = Module.HEAPU8.subarray(message_ptr, message_ptr + length);
                if (offset + length > buffer.length) {
                    const newbuffer = new Uint8Array((offset + length + 50) * 1.5); // exponential growth
                    newbuffer.set(buffer, 0); // copy previous buffer
                    newbuffer.set(view, offset); // append copy at the end
                    ws[wasm_ws_pending_send_buffer] = buffer = newbuffer;
                }
                else {
                    buffer.set(view, offset); // append copy at the end
                }
                offset += length;
                ws[wasm_ws_pending_send_buffer_offset] = offset;
            }
        }
        else if (!end_of_message) {
            // create new buffer
            if (length !== 0) {
                const view = Module.HEAPU8.subarray(message_ptr, message_ptr + length);
                buffer = new Uint8Array(view); // copy
                offset = length;
                ws[wasm_ws_pending_send_buffer_offset] = offset;
                ws[wasm_ws_pending_send_buffer] = buffer;
            }
            ws[wasm_ws_pending_send_buffer_type] = message_type;
        }
        else {
            // use the buffer only localy
            if (length !== 0) {
                const memoryView = Module.HEAPU8.subarray(message_ptr, message_ptr + length);
                buffer = memoryView; // send will make a copy
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
    const MONO = {
        // current "public" MONO API
        mono_wasm_setenv,
        mono_wasm_load_bytes_into_heap,
        mono_wasm_load_icu_data,
        mono_wasm_runtime_ready,
        mono_wasm_load_data_archive,
        mono_wasm_load_config,
        mono_load_runtime_and_bcl_args,
        mono_wasm_new_root_buffer,
        mono_wasm_new_root,
        mono_wasm_release_roots,
        // for Blazor's future!
        mono_wasm_add_assembly: wrapped_c_functions.mono_wasm_add_assembly,
        mono_wasm_load_runtime: wrapped_c_functions.mono_wasm_load_runtime,
        config: runtimeHelpers.config,
        loaded_files: [],
        // generated bindings closure `library_mono`
        mono_wasm_new_root_buffer_from_pointer,
        mono_wasm_new_roots,
    };
    const BINDING = {
        //current "public" BINDING API
        mono_obj_array_new: wrapped_c_functions.mono_wasm_obj_array_new,
        mono_obj_array_set: wrapped_c_functions.mono_wasm_obj_array_set,
        js_string_to_mono_string,
        js_typed_array_to_array,
        js_to_mono_obj,
        mono_array_to_js_array,
        conv_string,
        bind_static_method: mono_bind_static_method,
        call_assembly_entry_point: mono_call_assembly_entry_point,
        unbox_mono_obj,
        // generated bindings closure `binding_support`
        // todo use the methods directly in the closure, not via BINDING
        _get_args_root_buffer_for_method_call,
        _get_buffer_for_method_call,
        invoke_method: wrapped_c_functions.mono_wasm_invoke_method,
        _handle_exception_for_call,
        mono_wasm_try_unbox_primitive_and_get_type: wrapped_c_functions.mono_wasm_try_unbox_primitive_and_get_type,
        _unbox_mono_obj_root_with_known_nonprimitive_type,
        _teardown_after_call,
    };
    // this is executed early during load of emscripten runtime
    // it exports methods to global objects MONO, BINDING and Module in backward compatible way
    // eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
    function initializeImportsAndExports(imports, exports) {
        const module = exports.module;
        const globalThisAny = globalThis;
        // we want to have same instance of MONO, BINDING and Module in dotnet iffe
        setImportsAndExports(imports, exports);
        // here we merge methods from the local objects into exported objects
        Object.assign(exports.mono, MONO);
        Object.assign(exports.binding, BINDING);
        Object.assign(exports.internal, INTERNAL);
        const api = {
            MONO: exports.mono,
            BINDING: exports.binding,
            INTERNAL: exports.internal,
            Module: module
        };
        if (module.configSrc) {
            // this could be overriden on Module
            if (!module.preInit) {
                module.preInit = [];
            }
            else if (typeof module.preInit === "function") {
                module.preInit = [module.preInit];
            }
            module.preInit.unshift(mono_wasm_pre_init);
        }
        // this could be overriden on Module
        if (!module.onRuntimeInitialized) {
            module.onRuntimeInitialized = mono_wasm_on_runtime_initialized;
        }
        if (!module.print) {
            module.print = console.log;
        }
        if (!module.printErr) {
            module.printErr = console.error;
        }
        if (imports.isGlobal || !module.disableDotNet6Compatibility) {
            Object.assign(module, api);
            // backward compatibility
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
            module.mono_bind_static_method = (fqn, signature) => {
                console.warn("Module.mono_bind_static_method is obsolete, please use BINDING.bind_static_method instead");
                return mono_bind_static_method(fqn, signature);
            };
            // here we expose objects used in tests to global namespace
            const warnWrap = (name, provider) => {
                if (typeof globalThisAny[name] !== "undefined") {
                    // it already exists in the global namespace
                    return;
                }
                let value = undefined;
                Object.defineProperty(globalThis, name, {
                    get: () => {
                        if (!value) {
                            const stack = (new Error()).stack;
                            const nextLine = stack ? stack.substr(stack.indexOf("\n", 8) + 1) : "";
                            console.warn(`global ${name} is obsolete, please use Module.${name} instead ${nextLine}`);
                            value = provider();
                        }
                        return value;
                    }
                });
            };
            globalThisAny.MONO = exports.mono;
            globalThisAny.BINDING = exports.binding;
            globalThisAny.INTERNAL = exports.internal;
            if (!imports.isGlobal) {
                globalThisAny.Module = module;
            }
            // Blazor back compat
            warnWrap("cwrap", () => module.cwrap);
            warnWrap("addRunDependency", () => module.addRunDependency);
            warnWrap("removeRunDependency", () => module.removeRunDependency);
        }
    }
    const __initializeImportsAndExports = initializeImportsAndExports; // don't want to export the type
    // the methods would be visible to EMCC linker
    // --- keep in sync with dotnet.lib.js ---
    const __linker_exports = {
        // mini-wasm.c
        mono_set_timeout,
        // mini-wasm-debugger.c
        mono_wasm_asm_loaded,
        mono_wasm_fire_debugger_agent_message,
        // mono-threads-wasm.c
        schedule_background_exec,
        // also keep in sync with driver.c
        mono_wasm_invoke_js,
        mono_wasm_invoke_js_blazor,
        // also keep in sync with corebindings.c
        mono_wasm_invoke_js_with_args,
        mono_wasm_get_object_property,
        mono_wasm_set_object_property,
        mono_wasm_get_by_index,
        mono_wasm_set_by_index,
        mono_wasm_get_global_object,
        mono_wasm_create_cs_owned_object,
        mono_wasm_release_cs_owned_object,
        mono_wasm_typed_array_to_array,
        mono_wasm_typed_array_copy_to,
        mono_wasm_typed_array_from,
        mono_wasm_typed_array_copy_from,
        mono_wasm_add_event_listener,
        mono_wasm_remove_event_listener,
        mono_wasm_cancel_promise,
        mono_wasm_web_socket_open,
        mono_wasm_web_socket_send,
        mono_wasm_web_socket_receive,
        mono_wasm_web_socket_close,
        mono_wasm_web_socket_abort,
        mono_wasm_compile_function,
        //  also keep in sync with pal_icushim_static.c
        mono_wasm_load_icu_data,
        mono_wasm_get_icudt_name,
    };
    const INTERNAL = {
        // startup
        BINDING_ASM: "[System.Private.Runtime.InteropServices.JavaScript]System.Runtime.InteropServices.JavaScript.Runtime",
        // tests
        call_static_method,
        mono_wasm_exit: wrapped_c_functions.mono_wasm_exit,
        mono_wasm_enable_on_demand_gc: wrapped_c_functions.mono_wasm_enable_on_demand_gc,
        mono_profiler_init_aot: wrapped_c_functions.mono_profiler_init_aot,
        mono_wasm_set_runtime_options,
        mono_wasm_set_main_args: mono_wasm_set_main_args,
        mono_wasm_strdup: wrapped_c_functions.mono_wasm_strdup,
        mono_wasm_exec_regression: wrapped_c_functions.mono_wasm_exec_regression,
        mono_method_resolve,
        mono_bind_static_method,
        mono_intern_string,
        // EM_JS,EM_ASM,EM_ASM_INT macros
        string_decoder,
        logging: undefined,
        // used in EM_ASM macros in debugger
        mono_wasm_add_dbg_command_received,
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
        mono_wasm_runtime_is_ready: runtimeHelpers.mono_wasm_runtime_is_ready,
        // memory accessors
        setI8,
        setI16,
        setI32,
        setI64,
        setU8,
        setU16,
        setU32,
        setF32,
        setF64,
        getI8,
        getI16,
        getI32,
        getI64,
        getU8,
        getU16,
        getU32,
        getF32,
        getF64,
    };

    exports.__initializeImportsAndExports = __initializeImportsAndExports;
    exports.__linker_exports = __linker_exports;

    Object.defineProperty(exports, '__esModule', { value: true });

    return exports;

}({}));
