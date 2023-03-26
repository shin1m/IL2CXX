using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using DemoContentLoader;
using Demos;
using GL = WebGL2;

using var canvas = Host.GetCanvas();
using var context = new GL(canvas);
using var loop = new GameLoop(context);
loop.Initialize();
using (var stream = typeof(Host).Assembly.GetManifestResourceStream("DemosWasm.Demos.Demos.contentarchive"))
{
    loop.DemoHarness = new(loop, ContentArchive.Load(stream));
    loop.DemoHarness.Initialize();
}
var then = 0.0;
var pausing = false;
var done = new TaskCompletionSource();
void step(double now)
{
    if (pausing) return;
    loop.Update(Math.Min(Math.Max(now - then, 0.0), 1000.0) * 0.001);
    then = now;
    Host.RequestAnimationFrame(step);
}
void update()
{
    Host.Message(pausing ? "Pausing... Press Esc to unpause." : "Press Esc to pause.");
    if (!pausing) Host.RequestAnimationFrame(step);
}
Host.OnResize((width, height) => loop.Resize((int)width, (int)height));
Host.OnKeyDown((code, key) =>
{
    if (pausing) loop.Input.End();
    loop.Input.KeyDown(code);
    if (loop.DemoHarness.controls.Exit.WasTriggered(loop.Input))
    {
        pausing ^= true;
        update();
    }
    else if (loop.DemoHarness.controls.LockMouse.WasTriggered(loop.Input))
    {
        Host.RequestPointerLock();
    }
    loop.Input.KeyPress(key);
});
Host.OnKeyUp(loop.Input.KeyUp);
Host.OnMouseDown((button, x, y) => loop.Input.MouseDown(button));
Host.OnMouseUp((button, x, y) => loop.Input.MouseUp(button));
Host.OnMouseMove(loop.Input.MouseMove);
Host.OnMouseWheel(loop.Input.MouseWheel);
Host.OnPointerMove(locked => loop.Input.MouseLocked = locked, loop.Input.PointerMove);
update();
await done.Task;

partial class Host
{
    [JSImport("globalThis.requestAnimationFrame")]
    internal static partial int RequestAnimationFrame([JSMarshalAs<JSType.Function<JSType.Number>>]Action<double> action);
    [JSImport("getCanvas", "main.js")]
    internal static partial JSObject GetCanvas();
    [JSImport("onResize", "main.js")]
    internal static partial void OnResize([JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]Action<double, double> action);
    [JSImport("onKeyDown", "main.js")]
    internal static partial void OnKeyDown([JSMarshalAs<JSType.Function<JSType.String, JSType.String>>]Action<string, string> action);
    [JSImport("onKeyUp", "main.js")]
    internal static partial void OnKeyUp([JSMarshalAs<JSType.Function<JSType.String>>]Action<string> action);
    [JSImport("onMouseDown", "main.js")]
    internal static partial void OnMouseDown([JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>]Action<int, int, int> action);
    [JSImport("onMouseUp", "main.js")]
    internal static partial void OnMouseUp([JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>]Action<int, int, int> action);
    [JSImport("onMouseMove", "main.js")]
    internal static partial void OnMouseMove([JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]Action<int, int> action);
    [JSImport("onMouseWheel", "main.js")]
    internal static partial void OnMouseWheel([JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]Action<double, double> action);
    [JSImport("onPointerMove", "main.js")]
    internal static partial void OnPointerMove([JSMarshalAs<JSType.Function<JSType.Boolean>>]Action<bool> locked, [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]Action<int, int> action);
    [JSImport("requestPointerLock", "main.js")]
    internal static partial void RequestPointerLock();
    [JSImport("message", "main.js")]
    internal static partial void Message(string value);
}
