let ENVIRONMENT_IS_GLOBAL = false;
var require = require || undefined;
var __dirname = __dirname || '';
var __callbackAPI = { MONO, BINDING, INTERNAL, IMPORTS };
if (typeof moduleArg === "function") {
    __callbackAPI.Module = Module = { ready: Module.ready };
    const extension = moduleArg(__callbackAPI)
    if (extension.ready) {
        throw new Error("MONO_WASM: Module.ready couldn't be redefined.")
    }
    Object.assign(Module, extension);
    moduleArg = Module;
    if (!moduleArg.locateFile) moduleArg.locateFile = moduleArg.__locateFile = (path) => scriptDirectory + path;
}
else if (typeof moduleArg === "object") {
    __callbackAPI.Module = Module = { ready: Module.ready, __undefinedConfig: Object.keys(moduleArg).length === 1 };
    Object.assign(Module, moduleArg);
    moduleArg = Module;
    if (!moduleArg.locateFile) moduleArg.locateFile = moduleArg.__locateFile = (path) => scriptDirectory + path;
}
else {
    throw new Error("MONO_WASM: Can't use moduleFactory callback of moduleArg function.")
}
