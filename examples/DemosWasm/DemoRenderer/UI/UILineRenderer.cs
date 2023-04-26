using BepuUtilities;
using DemoContentLoader;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using GL = ThinJS.WebGL2;

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
        private readonly InstanceBuffer<UILineInstance> instances;

        public UILineRenderer(GL context) : base(context)
        {
            vertexConstants = new(context);
            indices = new(context);
            instances = new(context);
        }
        protected override void DoDispose()
        {
            vertexConstants.Dispose();
            indices.Dispose();
            instances.Dispose();
            base.DoDispose();
        }
        public void Initialize(ContentArchive content, int maximumLinesPerDraw = 2048)
        {
            var program = Build(
                content.Load<GLSLContent>(@"UI\RenderUILines.glvs").Source,
                content.Load<GLSLContent>(@"UI\RenderUILines.glfs").Source
            );
            context.UniformBlockBinding(program, context.GetUniformBlockIndex(program, "type_VertexConstants"), 0);
            vertexConstants.Initialize();
            indices.Initialize(Helpers.GetQuadIndices(1));
            instances.Allocate(maximumLinesPerDraw);
            instances.VertexAttribIPointer(0, 1, 0);
            instances.VertexAttribIPointer(1, 1, sizeof(uint));
            instances.VertexAttribPointer(2, 1, sizeof(uint) * 2);
            instances.VertexAttribIPointer(3, 1, sizeof(float) + sizeof(uint) * 2);
        }
        public void Render(Int2 screenResolution, UILineInstance[] lines, int start, int count)
        {
            Use();
            vertexConstants.Update(0, new()
            {
                //The packed minimum must permit subpixel locations. So, distribute the range 0 to 65535 over the pixel range 0 to resolution.
                PackedToScreenScale = new(screenResolution.X / 65535f, screenResolution.Y / 65535f),
                ScreenToNDCScale = new(2f / screenResolution.X, 2f / screenResolution.Y)
            });
            while (count > 0)
            {
                var batchCount = Math.Min(instances.Capacity, count);
                instances.Update(new ArraySegment<UILineInstance>(lines, start, batchCount).ToArray());
                context.DrawElementsInstanced(GL.TRIANGLES, 6, indices.Type, 0, batchCount);
                count -= batchCount;
                start += batchCount;
            }
        }
    }
}
