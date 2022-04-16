using System;
using System.Numerics;
using DemoContentLoader;
using DemoRenderer.Background;
using DemoRenderer.UI;
using DemoRenderer.PostProcessing;
using DemoRenderer.ShapeDrawing;
using DemoRenderer.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using DemoUtilities;
using Blazor.Extensions.Canvas.WebGL;

namespace DemoRenderer
{
    public class Renderer : Disposable
    {
        private readonly ParallelLooper looper = new();
        private readonly BufferPool pool = new();
        private readonly WebGL2Context context;
        public WebGL2Context Context => context;

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

        private WebGLTexture? depthBuffer;
        //Technically we could get away with rendering directly to the backbuffer, but a dedicated color buffer simplifies some things- 
        //you aren't bound by the requirements of the swapchain's buffer during rendering, and post processing is nicer.
        //Not entirely necessary for the demos, but hey, you could add tonemapping if you wanted?
        private WebGLTexture? colorBuffer;
        private WebGLFramebuffer? framebuffer;
        //private WebGLTexture? resolvedColorBuffer;
        //private WebGLFramebuffer? resolvedFramebuffer;
        private int width;
        public int Width => width;
        private int height;
        public int Height => height;

        public Renderer(WebGL2Context context)
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
        }
        public async Task InitializeAsync()
        {
            depthBuffer = await context.CreateTextureAsync();
            colorBuffer = await context.CreateTextureAsync();
            framebuffer = await context.CreateFramebufferAsync();
            //resolvedColorBuffer = await context.CreateTextureAsync();
            //resolvedFramebuffer = await context.CreateFramebufferAsync();
            ContentArchive content;
            using (var stream = typeof(Renderer).Assembly.GetManifestResourceStream("BPDemosBW.DemoRenderer.DemoRenderer.contentarchive")) content = ContentArchive.Load(stream);
            await Background.InitializeAsync(content);
            await rayTracedVertexConstants.InitializeAsync();
            await rayTracedPixelConstants.InitializeAsync();
            await SphereRenderer.InitializeAsync(content);
            await CapsuleRenderer.InitializeAsync(content);
            await CylinderRenderer.InitializeAsync(content);
            await rasterizedVertexConstants.InitializeAsync();
            await BoxRenderer.InitializeAsync(content);
            await TriangleRenderer.InitializeAsync(content);
            await Shapes.InitializeAsync();
            await MeshRenderer.InitializeAsync(content);
            await LineRenderer.InitializeAsync(content);
            await ImageRenderer.InitializeAsync(content);
            await UILineRenderer.InitializeAsync(content);
            await GlyphRenderer.InitializeAsync(content);
            await CompressToSwap.InitializeAsync(content);
            await context.BindVertexArrayAsync(null);
        }
        protected override async ValueTask DoDisposeAsync()
        {
            await context.DeleteFramebufferAsync(framebuffer);
            await context.DeleteTextureAsync(depthBuffer);
            await context.DeleteTextureAsync(colorBuffer);
            //await context.DeleteFramebufferAsync(resolvedFramebuffer);
            //await context.DeleteTextureAsync(resolvedColorBuffer);
            await Background.DisposeAsync();
            await rayTracedVertexConstants.DisposeAsync();
            await rayTracedPixelConstants.DisposeAsync();
            await SphereRenderer.DisposeAsync();
            await CapsuleRenderer.DisposeAsync();
            await CylinderRenderer.DisposeAsync();
            await rasterizedVertexConstants.DisposeAsync();
            await BoxRenderer.DisposeAsync();
            await TriangleRenderer.DisposeAsync();
            await MeshRenderer.DisposeAsync();
            await Shapes.DisposeAsync();
            await LineRenderer.DisposeAsync();
            Lines.Dispose();
            await ImageRenderer.DisposeAsync();
            await UILineRenderer.DisposeAsync();
            await GlyphRenderer.DisposeAsync();
            await CompressToSwap.DisposeAsync();
        }
        public async Task ResizeAsync(int width, int height)
        {
            this.width = width;
            this.height = height;
            await context.ViewportAsync(0, 0, width, height);

            var resolution = new Int2(width, height);
            TextBatcher.Resolution = resolution;
            ImageBatcher.Resolution = resolution;
            UILineBatcher.Resolution = resolution;

            await context.BindFramebufferAsync(FramebufferType.FRAMEBUFFER, framebuffer);
            await context.DeleteTextureAsync(depthBuffer);
            depthBuffer = await context.CreateTextureAsync();
            //await context.BindTextureAsync(TextureType.TEXTURE_2D_MULTISAMPLE, depthBuffer);
            //await context.TexStorage2DMultisampleAsync(Texture2DMultisampleType.TEXTURE_2D_MULTISAMPLE, 4, PixelFormat.DEPTH_COMPONENT, width, height, false);
            //await context.FramebufferTexture2DAsync(FramebufferType.FRAMEBUFFER, FramebufferAttachment.DEPTH_ATTACHMENT, Texture2DType.TEXTURE_2D_MULTISAMPLE, depthBuffer, 0);
            await context.BindTextureAsync(TextureType.TEXTURE_2D, depthBuffer);
            await context.TexStorage2DAsync(Texture2DType.TEXTURE_2D, 1, PixelFormat.DEPTH_COMPONENT32F, width, height);
            await context.FramebufferTexture2DAsync(FramebufferType.FRAMEBUFFER, FramebufferAttachment.DEPTH_ATTACHMENT, Texture2DType.TEXTURE_2D, depthBuffer, 0);
            await context.DeleteTextureAsync(colorBuffer);
            colorBuffer = await context.CreateTextureAsync();
            //await context.BindTextureAsync(TextureType.TEXTURE_2D_MULTISAMPLE, colorBuffer);
            //await context.TexStorage2DMultisampleAsync(Texture2DMultisampleType.TEXTURE_2D_MULTISAMPLE, 4, PixelFormat.RGBA16F, width, height, false);
            //await context.FramebufferTexture2DAsync(FramebufferType.FRAMEBUFFER, FramebufferAttachment.COLOR_ATTACHMENT0, Texture2DType.TEXTURE_2D_MULTISAMPLE, colorBuffer, 0);
            //await context.BindTextureAsync(TextureType.TEXTURE_2D_MULTISAMPLE, null);
            await context.BindTextureAsync(TextureType.TEXTURE_2D, colorBuffer);
            await context.TexStorage2DAsync(Texture2DType.TEXTURE_2D, 1, PixelFormat.RGBA8, width, height);
            await context.FramebufferTexture2DAsync(FramebufferType.FRAMEBUFFER, FramebufferAttachment.COLOR_ATTACHMENT0, Texture2DType.TEXTURE_2D, colorBuffer, 0);
            //await context.BindFramebufferAsync(FramebufferType.FRAMEBUFFER, resolvedFramebuffer);
            //await context.DeleteTextureAsync(resolvedColorBuffer);
            //resolvedColorBuffer = await context.CreateTextureAsync();
            //await context.BindTextureAsync(TextureType.TEXTURE_2D, resolvedColorBuffer);
            //await context.TexStorage2DAsync(Texture2DType.TEXTURE_2D, 1, PixelFormat.RGBA8, width, height);
            //await context.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_MIN_FILTER, (int)TextureParameterValue.NEAREST);
            //await context.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_MAG_FILTER, (int)TextureParameterValue.NEAREST);
            //await context.FramebufferTexture2DAsync(FramebufferType.FRAMEBUFFER, FramebufferAttachment.COLOR_ATTACHMENT0, Texture2DType.TEXTURE_2D, resolvedColorBuffer, 0);
            await context.BindTextureAsync(TextureType.TEXTURE_2D, null);

            await context.BindFramebufferAsync(FramebufferType.FRAMEBUFFER, null);
        }
        public async Task RenderAsync(Camera camera)
        {
            await context.BeginBatchAsync();
            await Shapes.MeshCache.FlushPendingUploadsAsync();

            await context.BindFramebufferAsync(FramebufferType.FRAMEBUFFER, framebuffer);
            //Note reversed depth.
            await context.ClearDepthAsync(0.0f);
            await context.ClearAsync(BufferBits.COLOR_BUFFER_BIT | BufferBits.DEPTH_BUFFER_BIT);
            await context.EnableAsync(EnableCap.CULL_FACE);
            await context.EnableAsync(EnableCap.DEPTH_TEST);
            await context.DepthFuncAsync(CompareFunction.GREATER);

            //All ray traced shapes use analytic coverage writes to get antialiasing.
            await context.EnableAsync(EnableCap.SAMPLE_ALPHA_TO_COVERAGE);
            await rayTracedVertexConstants.UpdateAsync(0, new()
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
            await rayTracedPixelConstants.UpdateAsync(1, new()
            {
                CameraRight = camera.Right,
                NearClip = camera.NearClip,
                CameraUp = camera.Up,
                FarClip = camera.FarClip,
                CameraBackward = camera.Backward,
                PixelSizeAtUnitPlane = new(viewportWidth / resolution.X, viewportHeight / resolution.Y)
            });
            await SphereRenderer.RenderAsync(Shapes.ShapeCache.Spheres.Span, 0, Shapes.ShapeCache.Spheres.Count);
            await CapsuleRenderer.RenderAsync(Shapes.ShapeCache.Capsules.Span, 0, Shapes.ShapeCache.Capsules.Count);
            await CylinderRenderer.RenderAsync(Shapes.ShapeCache.Cylinders.Span, 0, Shapes.ShapeCache.Cylinders.Count);

            //Non-raytraced shapes just use regular opaque rendering.
            await context.DisableAsync(EnableCap.SAMPLE_ALPHA_TO_COVERAGE);
            await rasterizedVertexConstants.UpdateAsync(0, new()
            {
                Projection = camera.Projection,
                CameraPosition = camera.Position,
                CameraRight = camera.Right,
                CameraUp = camera.Up,
                CameraBackward = camera.Backward
            });
            await BoxRenderer.RenderAsync(Shapes.ShapeCache.Boxes.Span, 0, Shapes.ShapeCache.Boxes.Count);
            await TriangleRenderer.RenderAsync(Shapes.ShapeCache.Triangles.Span, 0, Shapes.ShapeCache.Triangles.Count);
            await MeshRenderer.RenderAsync(Shapes.ShapeCache.Meshes.Span, 0, Shapes.ShapeCache.Meshes.Count);
            await LineRenderer.RenderAsync(camera, resolution, Lines.lines.Span, 0, Lines.lines.Count);

            await Background.RenderAsync(camera);
            await context.DisableAsync(EnableCap.CULL_FACE);
            await context.DisableAsync(EnableCap.DEPTH_TEST);

            //Resolve MSAA rendering down to a single sample buffer for screenspace work.
            //Note that we're not bothering to properly handle tonemapping during the resolve. That's going to hurt quality a little, but the demos don't make use of very wide ranges.
            //(If for some reason you end up expanding the demos to make use of wider HDR, you can make this a custom resolve pretty easily.)
            //await context.BindFramebufferAsync(FramebufferType.DRAW_FRAMEBUFFER, resolvedFramebuffer);
            //await context.BlitFramebufferAsync(0, 0, width, height, 0, 0, width, height, BufferBits.COLOR_BUFFER_BIT, BlitFilter.NEAREST);

            //Glyph and screenspace line drawing rely on the same premultiplied alpha blending transparency. We'll handle their state out here.
            await context.EnableAsync(EnableCap.BLEND);
            await context.BlendFuncAsync(BlendingMode.ONE, BlendingMode.ONE_MINUS_SRC_ALPHA);
            await context.BlendEquationAsync(BlendingEquation.FUNC_ADD);
            await ImageRenderer.PreparePipelineAsync();
            await ImageBatcher.FlushAsync(resolution, ImageRenderer);
            await UILineBatcher.FlushAsync(resolution, UILineRenderer);
            await GlyphRenderer.PreparePipelineAsync();
            await TextBatcher.FlushAsync(resolution, GlyphRenderer);
            await context.DisableAsync(EnableCap.BLEND);

            await context.BindFramebufferAsync(FramebufferType.FRAMEBUFFER, null);
            //await CompressToSwap.RenderAsync(resolvedColorBuffer!);
            await CompressToSwap.RenderAsync(colorBuffer!);
            await context.EndBatchAsync();
        }
        public Vector2 GetNormalizedMousePosition(Int2 position) => new((float)position.X / width, (float)position.Y / height);
    }
}
