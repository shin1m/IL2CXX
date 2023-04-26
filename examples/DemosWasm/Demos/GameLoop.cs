using DemoRenderer;
using DemoUtilities;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using BepuUtilities;
using BepuUtilities.Memory;
using GL = ThinJS.WebGL2;

namespace Demos
{
    public class GameLoop : Disposable
    {
        public readonly BufferPool Pool = new();
        public readonly Input Input = new();
        public readonly GL Context;
        public readonly Renderer Renderer;
        public readonly Camera Camera;
        public DemoHarness? DemoHarness;

        public GameLoop(GL context)
        {
            Context = context;
            Renderer = new(context);
            Camera = new(1.0f, (float)Math.PI / 3, 0.01f, 100000);
        }
        public void Initialize() => Renderer.Initialize();

        public void Update(double elapsed)
        {
            if (DemoHarness != null)
            {
                //We'll let the delegate's logic handle the variable time steps.
                DemoHarness.Update((float)elapsed);
                //At the moment, rendering just follows sequentially. Later on we might want to distinguish it a bit more with fixed time stepping or something. Maybe.
                DemoHarness.Render(Renderer);
            }
            Renderer.Render(Camera);
            Input.End();
        }

        public void Resize(int width, int height)
        {
            //We just don't support true fullscreen in the demos. Would be pretty pointless.
            Renderer.Resize(width, height);
            Camera.AspectRatio = width / (float)height;
            DemoHarness?.OnResize();
        }

        protected override void DoDispose()
        {
            Renderer.Dispose();
            Pool.Clear();
        }
    }
}
