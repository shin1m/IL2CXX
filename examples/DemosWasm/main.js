import { dotnet } from './dotnet.js';
import { webgl } from './webgl.js';

const { setModuleImports, getAssemblyExports, getConfig } = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

webgl(setModuleImports);
const canvas = document.getElementById('canvas');
const message = document.getElementById('message');
const preventDefaultKeys = new Set(['Tab', 'F1', 'F3']);
setModuleImports('main.js', {
    getCanvas: () => canvas,
    onResize: f => {
        canvas.focus();
        function resize() {
            const {width, height} = canvas.getBoundingClientRect();
            canvas.width = width;
            canvas.height = height;
            f(width, height);
        }
        resize();
        window.addEventListener('resize', resize);
    },
    onKeyDown: f => canvas.addEventListener('keydown', e => {
        if (preventDefaultKeys.has(e.code)) e.preventDefault();
        f(e.code, e.key);
    }),
    onKeyUp: f => canvas.addEventListener('keyup', e => {
        e.preventDefault();
        f(e.code);
    }),
    onMouseDown: f => canvas.addEventListener('mousedown', e => {
        e.preventDefault();
        canvas.focus();
        f(e.button, e.x, e.y);
    }),
    onMouseUp: f => canvas.addEventListener('mouseup', e => {
        e.preventDefault();
        f(e.button, e.x, e.y);
    }),
    onMouseMove: f => canvas.addEventListener('mousemove', e => {
        e.preventDefault();
        f(e.x, e.y);
    }),
    onMouseWheel: f => canvas.addEventListener('wheel', e => {
        e.preventDefault();
        f(e.deltaX, e.deltaY);
    }),
    onPointerMove: (locked, f) => {
        const pointerMove = e => f(e.movementX, e.movementY);
        let lock = null;
        document.addEventListener('pointerlockchange', () => {
            if (lock) lock.removeEventListener('mousemove', pointerMove);
            lock = document.pointerLockElement === canvas ? canvas : null;
            if (lock) lock.addEventListener('mousemove', pointerMove);
            locked(lock !== null);
        });
    },
    requestPointerLock: () => canvas.requestPointerLock(),
    message: value => message.innerText = value
});

await dotnet.run();
