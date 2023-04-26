import { dotnet } from './dotnet.js';
import { webgl } from './thinjs.webgl.js';

const { setModuleImports, getAssemblyExports, getConfig } = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

webgl(setModuleImports);
setModuleImports('main.js', {
    getCanvas: () => document.getElementById('out'),
    loadImage: url => new Promise((resolve, reject) => {
        const image = new Image();
        image.onload = () => resolve(image);
        image.onerror = reject;
        image.src = url;
    }),
    onMouseDown: (element, f) => element.addEventListener('mousedown', e => f(e.button, e.x, e.y)),
    onMouseUp: (element, f) => element.addEventListener('mouseup', e => f(e.button, e.x, e.y)),
    onMouseMove: (element, f) => element.addEventListener('mousemove', e => f(e.x, e.y))
});

await dotnet.run();
