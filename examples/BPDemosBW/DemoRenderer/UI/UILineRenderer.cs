using BepuUtilities;
using DemoContentLoader;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Blazor.Extensions.Canvas.WebGL;

namespace DemoRenderer.UI
{
    /// <summary>
    /// GPU-relevant information for the rendering of a single screenspace line instance.
    /// </summary>
    public struct UILineInstance
    {
        /// <summary>
        /// Start location. X is stored in the lower 16 bits, Y in the upper 16. Should be scaled by the PackedToScreenScale.
        /// </summary>
        public uint PackedStart;
        /// <summary>
        /// End location. X is stored in the lower 16 bits, Y in the upper 16. Should be scaled by the PackedToScreenScale.
        /// </summary>
        public uint PackedEnd;
        /// <summary>
        /// Radius of the line in screen pixels.
        /// </summary>
        public float Radius;
        /// <summary>
        /// Color, packed in a UNORM manner such that bits 0 through 10 are R, bits 11 through 21 are G, and bits 22 through 31 are B.
        /// </summary>
        public uint PackedColor;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UILineInstance(in Vector2 start, in Vector2 end, float radius, in Vector3 color, in Vector2 screenToPackedScale)
        {
            // screenspace of OpenGL +1 is up.
            PackedStart = (uint)(start.X * screenToPackedScale.X) | ((uint)(65535f - start.Y * screenToPackedScale.Y) << 16);
            PackedEnd = (uint)(end.X * screenToPackedScale.X) | ((uint)(65535f - end.Y * screenToPackedScale.Y) << 16);
            Radius = radius;
            PackedColor = Helpers.PackColor(color);
        }
    }

    public class UILineRenderer : Shader
    {
        struct VertexConstants
        {
            //Splitting these two scales (Packed->Screen followed by Screen->NDC) makes handling the radius easy, since it's in uniform screen pixels.
            public Vector2 PackedToScreenScale;
            public Vector2 ScreenToNDCScale;
        }

        private readonly ConstantsBuffer<VertexConstants> vertexConstants;
        private readonly IndexBuffer indices;
        private readonly InstanceBuffer instances;

        public UILineRenderer(WebGL2Context context) : base(context)
        {
            vertexConstants = new(context);
            indices = new(context);
            instances = new(context);
        }
        public async Task InitializeAsync(ContentArchive content, int maximumLinesPerDraw = 2048)
        {
            await InitializeAsync(
                content.Load<GLSLContent>(@"UI\RenderUILines.glvs").Source,
                content.Load<GLSLContent>(@"UI\RenderUILines.glfs").Source
            );
            await context.UniformBlockBindingAsync(program, await context.GetUniformBlockIndexAsync(program, "type_VertexConstants"), 0);
            await vertexConstants.InitializeAsync();
            await indices.InitializeAsync(Helpers.GetQuadIndices(1));
            await instances.InitializeAsync<UILineInstance>(maximumLinesPerDraw);
            await instances.VertexAttribIPointerAsync(0, 1, 0);
            await instances.VertexAttribIPointerAsync(1, 1, sizeof(uint));
            await instances.VertexAttribPointerAsync(2, 1, sizeof(uint) * 2);
            await instances.VertexAttribIPointerAsync(3, 1, sizeof(float) + sizeof(uint) * 2);
        }
        protected override async ValueTask DoDisposeAsync()
        {
            await vertexConstants.DisposeAsync();
            await indices.DisposeAsync();
            await instances.DisposeAsync();
            await base.DoDisposeAsync();
        }
        public async Task RenderAsync(Int2 screenResolution, UILineInstance[] lines, int start, int count)
        {
            await UseAsync();
            await vertexConstants.UpdateAsync(0, new()
            {
                //The packed minimum must permit subpixel locations. So, distribute the range 0 to 65535 over the pixel range 0 to resolution.
                PackedToScreenScale = new(screenResolution.X / 65535f, screenResolution.Y / 65535f),
                ScreenToNDCScale = new(2f / screenResolution.X, 2f / screenResolution.Y)
            });
            while (count > 0)
            {
                var batchCount = Math.Min(instances.Capacity, count);
                await instances.UpdateAsync(new ArraySegment<UILineInstance>(lines, start, batchCount).ToArray());
                await context.DrawElementsInstancedAsync(Primitive.TRIANGLES, 6, indices.Type, 0, batchCount);
                count -= batchCount;
                start += batchCount;
            }
        }
    }
}
