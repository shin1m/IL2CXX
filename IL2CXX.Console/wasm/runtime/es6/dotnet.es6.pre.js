if (ENVIRONMENT_IS_PTHREAD) {
    Module.instantiateWasm = (_, done) => new Promise(resolve => {
        wasmModuleReceived = async module => {
            Module.ENVIRONMENT_IS_PTHREAD = true;
            Module = await (await import("./dotnet.js")).default(Module);
            DOTNET_setup();
            while (Module.preInit.length > 0) Module.preInit.pop()();
            Module.wasmModule = module;
            Module.instantiateWasm(getWasmImports(), done);
            resolve();
        };
    });
} else {
    if (_nativeModuleLoaded) throw new Error("Native module already loaded");
    _nativeModuleLoaded = true;
    Module = moduleArg();
    Module["getWasmIndirectFunctionTable"] = function () { return wasmTable; }
    Module["getMemory"] = function () { return wasmMemory; }
}
