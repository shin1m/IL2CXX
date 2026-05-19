import { dotnet } from './_framework/dotnet.js'
import { webgl } from './thinjs.webgl.js';

const { setModuleImports, runMain } = await dotnet
    .withApplicationArgumentsFromQuery()
    .create();

webgl(setModuleImports);
setModuleImports('main.js', {
    getCanvas: () => document.getElementById('out')
});

await runMain();
