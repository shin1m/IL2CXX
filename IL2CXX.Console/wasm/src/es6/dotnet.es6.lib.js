// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/* eslint-disable no-undef */

"use strict";

// -- this javascript file is evaluated by emcc during compilation! --

// because we can't pass custom define symbols to acorn optimizer, we use environment variables to pass other build options
const DISABLE_LEGACY_JS_INTEROP = process.env.DISABLE_LEGACY_JS_INTEROP === "1";
const WASM_ENABLE_SIMD = process.env.WASM_ENABLE_SIMD === "1";
const WASM_ENABLE_EH = process.env.WASM_ENABLE_EH === "1";
const ENABLE_BROWSER_PROFILER = process.env.ENABLE_BROWSER_PROFILER === "1";
const ENABLE_AOT_PROFILER = process.env.ENABLE_AOT_PROFILER === "1";
var methodIndexByName = undefined;
var gitHash = undefined;

function setup(linkerSetup) {
    const pthreadReplacements = {};
    const dotnet_replacements = {
        fetch: globalThis.fetch,
        require,
        updateMemoryViews,
        pthreadReplacements,
        scriptDirectory,
        noExitRuntime
    };
    // USE_PTHREADS is emscripten's define symbol, which is passed to acorn optimizer, so we could use it here
    #if USE_PTHREADS
    pthreadReplacements.loadWasmModuleToWorker = PThread.loadWasmModuleToWorker;
    pthreadReplacements.threadInitTLS = PThread.threadInitTLS;
    pthreadReplacements.allocateUnusedWorker = PThread.allocateUnusedWorker;
    #else
    const ENVIRONMENT_IS_PTHREAD = false;
    #endif

    Module.__dotnet_runtime.passEmscriptenInternals({
        isPThread: ENVIRONMENT_IS_PTHREAD,
        quit_, ExitStatus,
        ...linkerSetup
    });
    Module.__dotnet_runtime.initializeReplacements(dotnet_replacements);

    #if USE_PTHREADS
    if (ENVIRONMENT_IS_PTHREAD) {
        Module.config = {};
        Module.__dotnet_runtime.configureWorkerStartup(Module);
    } else {
        #endif
        Module.__dotnet_runtime.configureEmscriptenStartup(Module);
        #if USE_PTHREADS
    }
    #endif

    updateMemoryViews = dotnet_replacements.updateMemoryViews;
    noExitRuntime = dotnet_replacements.noExitRuntime;
    fetch = dotnet_replacements.fetch;
    require = dotnet_replacements.require;
    _scriptDir = __dirname = scriptDirectory = dotnet_replacements.scriptDirectory;
    #if USE_PTHREADS
    PThread.loadWasmModuleToWorker = pthreadReplacements.loadWasmModuleToWorker;
    PThread.threadInitTLS = pthreadReplacements.threadInitTLS;
    PThread.allocateUnusedWorker = pthreadReplacements.allocateUnusedWorker;
    #endif
}

const DotnetSupportLib = {
    $DOTNET: { setup },
    icudt68_dat: function () { throw new Error('dummy link symbol') },
};

function createWasmImportStubsFrom(collection) {
    for (let functionName in collection) {
        if (functionName in DotnetSupportLib) throw new Error(`Function ${functionName} is already defined`);
        const runtime_idx = collection[functionName]
        const stub_fn = new Function(`return {runtime_idx:${runtime_idx}};//${functionName}`);
        DotnetSupportLib[functionName] = stub_fn;
    }
}

// the JS methods would be visible to EMCC linker and become imports of the WASM module
// we generate simple stub for each exported function so that emcc will include them in the final output
// we will replace them with the real implementation in replace_linker_placeholders
function injectDependencies() {
    createWasmImportStubsFrom(methodIndexByName.mono_wasm_imports);

    #if USE_PTHREADS
    createWasmImportStubsFrom(methodIndexByName.mono_wasm_threads_imports);
    #endif

    if (!DISABLE_LEGACY_JS_INTEROP) {
        createWasmImportStubsFrom(methodIndexByName.mono_wasm_legacy_interop_imports);
    }

    DotnetSupportLib["$DOTNET__postset"] = `DOTNET.setup({ ` +
        `linkerDisableLegacyJsInterop: ${DISABLE_LEGACY_JS_INTEROP ? "true" : "false"},` +
        `linkerWasmEnableSIMD: ${WASM_ENABLE_SIMD ? "true" : "false"},` +
        `linkerWasmEnableEH: ${WASM_ENABLE_EH ? "true" : "false"},` +
        `linkerEnableAotProfiler: ${ENABLE_AOT_PROFILER ? "true" : "false"}, ` +
        `linkerEnableBrowserProfiler: ${ENABLE_BROWSER_PROFILER ? "true" : "false"}, ` +
        `gitHash: "${gitHash}", ` +
        `});`;

    autoAddDeps(DotnetSupportLib, "$DOTNET");
    mergeInto(LibraryManager.library, DotnetSupportLib);
}


// var methodIndexByName wil be appended below by the MSBuild in wasm.proj

    var gitHash = "51e720c15a3fec9070535947b9c5074cdd7002cf";
    var methodIndexByName = {
  "mono_wasm_imports": {
    "mono_wasm_schedule_timer": 0,
    "mono_wasm_asm_loaded": 1,
    "mono_wasm_debugger_log": 2,
    "mono_wasm_add_dbg_command_received": 3,
    "mono_wasm_fire_debugger_agent_message_with_data": 4,
    "mono_wasm_fire_debugger_agent_message_with_data_to_pause": 5,
    "schedule_background_exec": 6,
    "mono_wasm_profiler_enter": 7,
    "mono_wasm_profiler_leave": 8,
    "mono_wasm_trace_logger": 9,
    "mono_wasm_set_entrypoint_breakpoint": 10,
    "mono_wasm_event_pipe_early_startup_callback": 11,
    "mono_wasm_release_cs_owned_object": 12,
    "mono_wasm_bind_js_function": 13,
    "mono_wasm_invoke_bound_function": 14,
    "mono_wasm_invoke_import": 15,
    "mono_wasm_bind_cs_function": 16,
    "mono_wasm_marshal_promise": 17,
    "mono_wasm_change_case_invariant": 18,
    "mono_wasm_change_case": 19,
    "mono_wasm_compare_string": 20,
    "mono_wasm_starts_with": 21,
    "mono_wasm_ends_with": 22,
    "mono_wasm_index_of": 23,
    "mono_wasm_get_calendar_info": 24,
    "mono_wasm_get_culture_info": 25,
    "mono_wasm_get_first_day_of_week": 26,
    "mono_wasm_get_first_week_of_year": 27
  },
  "mono_wasm_threads_imports": {},
  "mono_wasm_legacy_interop_imports": {}
};
    injectDependencies();
    