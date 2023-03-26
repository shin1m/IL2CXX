using System;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using DemoContentLoader;
using DemoRenderer.Background;
using DemoRenderer.UI;
using DemoRenderer.PostProcessing;
using DemoRenderer.ShapeDrawing;
using DemoRenderer.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using DemoUtilities;
using GL = WebGL2;

namespace DemoRenderer
{
    public class Renderer : Disposable
    {
        private readonly ParallelLooper looper = new();
        private readonly BufferPool pool = new();
        private readonly GL context;
        public GL Context => context;

        public readonly BackgroundRenderer Background;
        //TODO: Down the road, the sphere renderer will be joined by a bunch of other types. 
        //They'll likely be stored in an array indexed by a shape type rather than just being a swarm of properties.
        private readonly ConstantsBuffer<RayTracedVertexConstants> rayTracedVertexConstants;
        private readonly ConstantsBuffer<RayTracedPixelConstants> rayTracedPixelConstants;
        public readonly SphereRenderer SphereRenderer;
        public readonly CapsuleRenderer CapsuleRenderer;
        public readonly CylinderRenderer CylinderRenderer;
        private readonly ConstantsBuffer<RasterizedVertexConstants> rasterizedVertexConstants;
        public readonly BoxRenderer BoxRenderer;
        public readonly TriangleRenderer TriangleRenderer;
        public readonly ShapesExtractor Shapes;
        public readonly MeshRenderer MeshRenderer;
        public readonly LineExtractor Lines;
        public readonly LineRenderer LineRenderer;
        public readonly ImageRenderer ImageRenderer;
        public readonly ImageBatcher ImageBatcher;
        public readonly UILineRenderer UILineRenderer;
        public readonly UILineBatcher UILineBatcher;
        public readonly GlyphRenderer GlyphRenderer;
        public readonly TextBatcher TextBatcher;
        public readonly CompressToSwap CompressToSwap;

        private readonly JSObject? framebuffer;
        private JSObject? depthBuffer;
        //Technically we could get away with rendering directly to the backbuffer, but a dedicated color buffer simplifies some things- 
        //you aren't bound by the requirements of the swapchain's buffer during rendering, and post processing is nicer.
        //Not entirely necessary for the demos, but hey, you could add tonemapping if you wanted?
        private JSObject? colorBuffer;
        //private JSObject? resolvedColorBuffer;
        //private JSObject? resolvedFramebuffer;
        private int width;
        public int Width => width;
        private int height;
        public int Height => height;

        public Renderer(GL context)
        {
            this.context = context;
            Background = new(context);
            rayTracedVertexConstants = new(context);
            rayTracedPixelConstants = new(context);
            SphereRenderer = new(context);
            CapsuleRenderer = new(context);
            CylinderRenderer = new(context);
            rasterizedVertexConstants = new(context);
            BoxRenderer = new(context);
            TriangleRenderer = new(context);
            Shapes = new(context, looper, pool);
            MeshRenderer = new(context, Shapes.MeshCache);
            Lines = new(pool, looper);
            LineRenderer = new(context);
            ImageRenderer = new(context);
            ImageBatcher = new(pool);
            UILineRenderer = new(context);
            UILineBatcher = new();
            GlyphRenderer = new(context);
            TextBatcher = new();
            CompressToSwap = new(context);
            framebuffer = context.CreateFramebuffer();
            depthBuffer = context.CreateTexture();
            colorBuffer = context.CreateTexture();
            //resolvedColorBuffer = context.CreateTexture();
            //resolvedFramebuffer = context.CreateFramebuffer();
        }
        protected override void DoDispose()
        {
            context.DeleteFramebuffer(framebuffer);
            framebuffer?.Dispose();
            context.DeleteTexture(depthBuffer);
            depthBuffer?.Dispose();
            context.DeleteTexture(colorBuffer);
            colorBuffer?.Dispose();
            //context.DeleteFramebuffer(resolvedFramebuffer);
            //resolvedFramebuffer?.Dispose();
            //context.DeleteTexture(resolvedColorBuffer);
            //resolvedColorBuffer?.Dispose();
            Background.Dispose();
            rayTracedVertexConstants.Dispose();
            rayTracedPixelConstants.Dispose();
            SphereRenderer.Dispose();
            CapsuleRenderer.Dispose();
            CylinderRenderer.Dispose();
            rasterizedVertexConstants.Dispose();
            BoxRenderer.Dispose();
            TriangleRenderer.Dispose();
            MeshRenderer.Dispose();
            Shapes.Dispose();
            LineRenderer.Dispose();
            Lines.Dispose();
            ImageRenderer.Dispose();
            UILineRenderer.Dispose();
            GlyphRenderer.Dispose();
            CompressToSwap.Dispose();
        }
        public void Initialize()
        {
            ContentArchive content;
            using (var stream = typeof(Renderer).Assembly.GetManifestResourceStream("DemosWasm.DemoRenderer.DemoRenderer.contentarchive")) content = ContentArchive.Load(stream);
            Background.Initialize(content);
            rayTracedVertexConstants.Initialize();
            rayTracedPixelConstants.Initialize();
            SphereRenderer.Initialize(content);
            CapsuleRenderer.Initialize(content);
            CylinderRenderer.Initialize(content);
            rasterizedVertexConstants.Initialize();
            BoxRenderer.Initialize(content);
            TriangleRenderer.Initialize(content);
            Shapes.Initialize();
            MeshRenderer.Initialize(content);
            LineRenderer.Initialize(content);
            ImageRenderer.Initialize(content);
            UILineRenderer.Initialize(content);
            GlyphRenderer.Initialize(content);
            CompressToSwap.Initialize(content);
            context.BindVertexArray(null);
        }
        public void Resize(int width, int height)
        {
            this.width = width;
            this.height = height;
            context.Viewport(0, 0, width, height);

            var resolution = new Int2(width, height);
            TextBatcher.Resolution = resolution;
            ImageBatcher.Resolution = resolution;
            UILineBatcher.Resolution = resolution;

            context.BindFramebuffer(GL.FRAMEBUFFER, framebuffer);
            context.DeleteTexture(depthBuffer);
            depthBuffer = context.CreateTexture();
            //context.BindTexture(GL.TEXTURE_2D_MULTISAMPLE, depthBuffer);
            //context.TexStorage2DMultisample(GL.TEXTURE_2D_MULTISAMPLE, 4, GL.DEPTH_COMPONENT, width, height, false);
            //context.FramebufferTexture2D(GL.FRAMEBUFFER, GL.DEPTH_ATTACHMENT, GL.TEXTURE_2D_MULTISAMPLE, depthBuffer, 0);
            context.BindTexture(GL.TEXTURE_2D, depthBuffer);
            context.TexStorage2D(GL.TEXTURE_2D, 1, GL.DEPTH_COMPONENT32F, width, height);
            context.FramebufferTexture2D(GL.FRAMEBUFFER, GL.DEPTH_ATTACHMENT, GL.TEXTURE_2D, depthBuffer, 0);
            context.DeleteTexture(colorBuffer);
            colorBuffer = context.CreateTexture();
            //context.BindTexture(GL.TEXTURE_2D_MULTISAMPLE, colorBuffer);
            //context.TexStorage2DMultisample(GL.TEXTURE_2D_MULTISAMPLE, 4, GL.RGBA16F, width, height, false);
            //context.FramebufferTexture2D(GL.FRAMEBUFFER, GL.COLOR_ATTACHMENT0, GL.TEXTURE_2D_MULTISAMPLE, colorBuffer, 0);
            //context.BindTexture(GL.TEXTURE_2D_MULTISAMPLE, null);
            context.BindTexture(GL.TEXTURE_2D, colorBuffer);
            context.TexStorage2D(GL.TEXTURE_2D, 1, GL.RGBA8, width, height);
            context.FramebufferTexture2D(GL.FRAMEBUFFER, GL.COLOR_ATTACHMENT0, GL.TEXTURE_2D, colorBuffer, 0);
            //context.BindFramebuffer(GL.FRAMEBUFFER, resolvedFramebuffer);
            //context.DeleteTexture(resolvedColorBuffer);
            //resolvedColorBuffer = context.CreateTexture();
            //context.BindTexture(GL.TEXTURE_2D, resolvedColorBuffer);
            //context.TexStorage2D(GL.TEXTURE_2D, 1, GL.RGBA8, width, height);
            //context.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, GL.NEAREST);
            //context.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, GL.NEAREST);
            //context.FramebufferTexture2D(GL.FRAMEBUFFER, GL.COLOR_ATTACHMENT0, GL.TEXTURE_2D, resolvedColorBuffer, 0);
            context.BindTexture(GL.TEXTURE_2D, null);

            context.BindFramebuffer(GL.FRAMEBUFFER, null);
        }
        public void Render(Camera camera)
        {
            Shapes.MeshCache.FlushPendingUploads();

            context.BindFramebuffer(GL.FRAMEBUFFER, framebuffer);
            //Note reversed depth.
            context.ClearDepth(0.0f);
            context.Clear(GL.COLOR_BUFFER_BIT | GL.DEPTH_BUFFER_BIT);
            context.Enable(GL.CULL_FACE);
            context.Enable(GL.DEPTH_TEST);
            context.DepthFunc(GL.GREATER);

            //All ray traced shapes use analytic coverage writes to get antialiasing.
            context.Enable(GL.SAMPLE_ALPHA_TO_COVERAGE);
            rayTracedVertexConstants.Update(0, new()
            {
                Projection = camera.Projection,
                CameraPosition = camera.Position,
                CameraRight = camera.Right,
                NearClip = camera.NearClip,
                CameraUp = camera.Up,
                CameraBackward = camera.Backward
            });
            var viewportHeight = 2 * (float)Math.Tan(camera.FieldOfView / 2);
            var viewportWidth = viewportHeight * camera.AspectRatio;
            var resolution = new Int2(width, height);
            rayTracedPixelConstants.Update(1, new()
            {
                CameraRight = camera.Right,
                NearClip = camera.NearClip,
                CameraUp = camera.Up,
                FarClip = camera.FarClip,
                CameraBackward = camera.Backward,
                PixelSizeAtUnitPlane = new(viewportWidth / resolution.X, viewportHeight / resolution.Y)
            });
            SphereRenderer.Render(Shapes.ShapeCache.Spheres.Span, 0, Shapes.ShapeCache.Spheres.Count);
            CapsuleRenderer.Render(Shapes.ShapeCache.Capsules.Span, 0, Shapes.ShapeCache.Capsules.Count);
            CylinderRenderer.Render(Shapes.ShapeCache.Cylinders.Span, 0, Shapes.ShapeCache.Cylinders.Count);

            //Non-raytraced shapes just use regular opaque rendering.
            context.Disable(GL.SAMPLE_ALPHA_TO_COVERAGE);
            rasterizedVertexConstants.Update(0, new()
            {
                Projection = camera.Projection,
                CameraPosition = camera.Position,
                CameraRight = camera.Right,
                CameraUp = camera.Up,
                CameraBackward = camera.Backward
            });
            BoxRenderer.Render(Shapes.ShapeCache.Boxes.Span, 0, Shapes.ShapeCache.Boxes.Count);
            TriangleRenderer.Render(Shapes.ShapeCache.Triangles.Span, 0, Shapes.ShapeCache.Triangles.Count);
            MeshRenderer.Render(Shapes.ShapeCache.Meshes.Span, 0, Shapes.ShapeCache.Meshes.Count);
            LineRenderer.Render(camera, resolution, Lines.lines.Span, 0, Lines.lines.Count);

            Background.Render(camera);
            context.Disable(GL.CULL_FACE);
            context.Disable(GL.DEPTH_TEST);

            //Resolve MSAA rendering down to a single sample buffer for screenspace work.
            //Note that we're not bothering to properly handle tonemapping during the resolve. That's going to hurt quality a little, but the demos don't make use of very wide ranges.
            //(If for some reason you end up expanding the demos to make use of wider HDR, you can make this a custom resolve pretty easily.)
            //context.BindFramebuffer(GL.DRAW_FRAMEBUFFER, resolvedFramebuffer);
            //context.BlitFramebuffer(0, 0, width, height, 0, 0, width, height, GL.COLOR_BUFFER_BIT, GL.NEAREST);

            //Glyph and screenspace line drawing rely on the same premultiplied alpha blending transparency. We'll handle their state out here.
            context.Enable(GL.BLEND);
            context.BlendFunc(GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
            context.BlendEquation(GL.FUNC_ADD);
            ImageRenderer.PreparePipeline();
            ImageBatcher.Flush(resolution, ImageRenderer);
            UILineBatcher.Flush(resolution, UILineRenderer);
            GlyphRenderer.PreparePipeline();
            TextBatcher.Flush(resolution, GlyphRenderer);
            context.Disable(GL.BLEND);

            context.BindFramebuffer(GL.FRAMEBUFFER, null);
            //CompressToSwap.Render(resolvedColorBuffer);
            CompressToSwap.Render(colorBuffer);
        }
        public Vector2 GetNormalizedMousePosition(Int2 position) => new((float)position.X / width, (float)position.Y / height);
    }
}
