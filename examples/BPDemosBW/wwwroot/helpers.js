export async function start(instance, element) {
    async function resize() {
        const bounds = element.getBoundingClientRect();
        await instance.invokeMethodAsync("ResizeAsync", bounds.width, bounds.height);
    }
    await resize();
    let previous = performance.now();
    let pausing = false;
    async function step(timestamp) {
        if (pausing) return;
        const elapsed = Math.min(Math.max(timestamp - previous, 0.0), 1000.0);
        previous = timestamp;
        window.requestAnimationFrame(step);
        await instance.invokeMethodAsync("UpdateAsync", elapsed);
    }
    window.addEventListener("resize", resize);
    const pointerMove = e => instance.invokeMethod("PointerMove", e.movementX, e.movementY);
    let lock = null;
    document.addEventListener("pointerlockchange", () => {
        if (lock) lock.removeEventListener("mousemove", pointerMove);
        lock = document.pointerLockElement === element ? element : null;
        if (lock) lock.addEventListener("mousemove", pointerMove);
        instance.invokeMethod("PointerLockChange", lock !== null);
    });
    window.requestAnimationFrame(step);
    return {
        pause(value) {
            pausing = value;
            if (!pausing) window.requestAnimationFrame(step);
        },
        lockPointer() {
            element.requestPointerLock();
        }
    };
}
