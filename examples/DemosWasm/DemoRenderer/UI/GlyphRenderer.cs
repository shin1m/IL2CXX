using BepuUtilities;
using DemoContentLoader;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using GL = ThinJS.WebGL2;

namespace DemoRenderer.UI
{
    /// <summary>
    /// GPU-relevant information for the rendering of a single character glyph instance.
    /// </summary>
    public struct GlyphInstance
    {
        /// <summary>
        /// Packed location of the minimum corner of the glyph. Lower 16 bits is X, upper 16 bits is Y. Should be scaled by PackedToScreen.
        /// </summary>
        public uint PackedMinimum;
        /// <summary>
        /// Packed horizontal axis used by the glyph. Lower 16 bits is X, upper 16 bits is Y. UNORM packed across a range from -1.0 at 0 to 1.0 at 65534.
        /// </summary>
        public uint PackedHorizontalAxis;
        /// <summary>
        /// The combination of two properties: scale to apply to the source glyph. UNORM packed across a range of 0.0 at 0 to 16.0 at 65535, stored in the lower 16 bits,
        /// and the id of the glyph type in the font stored in the upper 16 bits.
        /// </summary>
        public uint PackedScaleAndSourceId;
        /// <summary>
        /// RGBA color, packed in a UNORM manner such that bits 0 through 7 are R, bits 8 through 15 are G, bits 16 through 23 are B, and bits 24 through 31 are A.
        /// </summary>
        public uint PackedColor;

        public GlyphSource Source;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GlyphInstance(ref Vector2 start, ref Vector2 horizontalAxis, float scale, int sourceId, ref Vector4 color, ref Vector2 screenToPackedScale, ref GlyphSource source)
        {
            //Note that this can do some weird stuff if the position is outside of the target range. For the sake of the demos, we just assume everything's in frame.
            //If you want to use this for a game where you can't guarantee that everything's in frame, this packing range would need to be modified.
            //One simple option is to just set the mapped region to extend beyond the rendered target. It reduces the precision density a bit, but that's not too important.
            PackedMinimum = (uint)(start.X * screenToPackedScale.X) | ((uint)(start.Y * screenToPackedScale.Y) << 16);
            var scaledAxisX = (uint)(horizontalAxis.X * 32767f + 32767f);
            var scaledAxisY = (uint)(horizontalAxis.Y * 32767f + 32767f);
            Debug.Assert(scaledAxisX <= 65534);
            Debug.Assert(scaledAxisY <= 65534);
            PackedHorizontalAxis = scaledAxisX | (scaledAxisY << 16);
            var packScaledScale = scale * (65535f / 16f);
            Debug.Assert(packScaledScale >= 0);
            if (packScaledScale > 65535f)
                packScaledScale = 65535f;
            Debug.Assert(sourceId >= 0 && sourceId < 65536);
            PackedScaleAndSourceId = (uint)packScaledScale | (uint)(sourceId << 16);
            PackedColor = Helpers.PackColor(color);
            Source = source;
        }
    }

    public class GlyphRenderer : Shader
    {
        struct VertexConstants
        {
            public Vector2 PackedToScreenScale;
            public Vector2 ScreenToNDCScale;
            public Vector2 InverseAtlasResolution;
        }

        private readonly ConstantsBuffer<VertexConstants> vertexConstants;
        private readonly IndexBuffer indices;
        private readonly InstanceBuffer<GlyphInstance> instances;

        public GlyphRenderer(GL context) : base(context)
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
        public void Initialize(ContentArchive content, int maximumInstancesPerDraw = 2048)
        {
            var program = Build(
                content.Load<GLSLContent>(@"UI\RenderGlyphs.glvs").Source,
                content.Load<GLSLContent>(@"UI\RenderGlyphs.glfs").Source
            );
            context.UniformBlockBinding(program, context.GetUniformBlockIndex(program, "type_VertexConstants"), 0);
            vertexConstants.Initialize();
            indices.Initialize(Helpers.GetQuadIndices(1));
            instances.Allocate(maximumInstancesPerDraw);
            instances.VertexAttribIPointer(0, 1, 0);
            instances.VertexAttribIPointer(1, 1, sizeof(uint));
            instances.VertexAttribIPointer(2, 1, sizeof(uint) * 2);
            instances.VertexAttribIPointer(3, 1, sizeof(uint) * 3);
            instances.VertexAttribPointer(4, 2, sizeof(uint) * 4);
            instances.VertexAttribIPointer(5, 1, sizeof(float) * 2 + sizeof(uint) * 4);
            instances.VertexAttribPointer(6, 1, sizeof(float) * 2 + sizeof(uint) * 5);
        }
        /// <summary>
        /// Sets up the rendering pipeline with any glyph rendering specific render state that can be shared across all glyph batches drawn using the GlyphRenderer.Render function.
        /// </summary>
        public void PreparePipeline() => Use();
        public void Render(Font font, Int2 screenResolution, ArraySegment<GlyphInstance> glyphs)
        {
            font.Use();
            vertexConstants.Update(0, new()
            {
                //These first two scales could be uploaded once, but it would require another buffer. Not important enough.
                //The packed minimum must permit subpixel locations. So, distribute the range 0 to 65535 over the pixel range 0 to resolution.
                PackedToScreenScale = new(screenResolution.X / 65535f, screenResolution.Y / 65535f),
                ScreenToNDCScale = new(2f / screenResolution.X, -2f / screenResolution.Y),
                InverseAtlasResolution = new(1f / font.Content.Atlas.Width, 1f / font.Content.Atlas.Height)
            });
            var count = glyphs.Count;
            var start = 0;
            while (count > 0)
            {
                var batchCount = Math.Min(instances.Capacity, count);
                instances.Update(glyphs.Slice(start, batchCount).ToArray());
                context.DrawElementsInstanced(GL.TRIANGLES, 6, indices.Type, 0, batchCount);
                count -= batchCount;
                start += batchCount;
            }
        }
    }
}
