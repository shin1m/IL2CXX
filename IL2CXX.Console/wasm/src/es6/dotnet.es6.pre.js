if (ENVIRONMENT_IS_PTHREAD) {
    Module.instantiateWasm = async (imports, done) => {
        Module.addRunDependency("instantiateWasm");
        Module.ENVIRONMENT_IS_PTHREAD = true;
        Module = await (await import("./dotnet.js")).default(Module);
        DOTNET_setup();
        Module.wasmModule = wasmModule;
        Module.instantiateWasm(imports, async (instance, module) => {
                done(instance, module);
		while (Module.preInit.length > 0) await Module.preInit.shift()();
                Module.removeRunDependency("instantiateWasm");
        });
    };
} else {
    if (_nativeModuleLoaded) throw new Error("Native module already loaded");
    _nativeModuleLoaded = true;
    Module = moduleArg();
}
