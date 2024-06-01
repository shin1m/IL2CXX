if (_nativeModuleLoaded) throw new Error("Native module already loaded");
_nativeModuleLoaded = true;
moduleArg = Module = moduleArg(Module);
Module["getWasmIndirectFunctionTable"] = function () { return wasmTable; }
Module["getMemory"] = function () { return wasmMemory; }
