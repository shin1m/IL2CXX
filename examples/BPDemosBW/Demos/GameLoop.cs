using DemoRenderer;
using DemoUtilities;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using BepuUtilities;
using BepuUtilities.Memory;
using Blazor.Extensions.Canvas.WebGL;

namespace Demos
{
    public class GameLoop : Disposable
    {
        public readonly BufferPool Pool = new();
        public readonly Input Input = new();
        public readonly WebGL2Context Context;
        public readonly Renderer Renderer;
        public readonly Camera Camera;
        public DemoHarness? DemoHarness;

        public GameLoop(WebGL2Context context)
        {
            Context = context;
            Renderer = new(context);
            Camera = new(1.0f, (float)Math.PI / 3, 0.01f, 100000);
        }
        public Task InitializeAsync() => Renderer.InitializeAsync();

        public async Task UpdateAsync(double elapsed)
        {
            if (DemoHarness != null)
            {
                //We'll let the delegate's logic handle the variable time steps.
                await DemoHarness.UpdateAsync((float)elapsed);
                //At the moment, rendering just follows sequentially. Later on we might want to distinguish it a bit more with fixed time stepping or something. Maybe.
                DemoHarness.Render(Renderer);
            }
            await Renderer.RenderAsync(Camera);
            Input.End();
        }

        public async Task ResizeAsync(int width, int height)
        {
            //We just don't support true fullscreen in the demos. Would be pretty pointless.
            await Renderer.ResizeAsync(width, height);
            Camera.AspectRatio = width / (float)height;
            DemoHarness?.OnResize();
        }

        protected override async ValueTask DoDisposeAsync()
        {
            await Renderer.DisposeAsync();
            Pool.Clear();
        }
    }
}
