import { dotnet } from './_framework/dotnet.js'
import { webgl } from './thinjs.webgl.js';

const { setModuleImports } = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

webgl(setModuleImports);
setModuleImports('main.js', {
    getCanvas: () => document.getElementById('out')
});

await dotnet.run();
