using DemoContentLoader;
using System.Numerics;
using System.Runtime.InteropServices;
using Blazor.Extensions.Canvas.WebGL;

namespace DemoRenderer.ShapeDrawing
{
    //Could get this down to 32 bytes with some extra packing (e.g. 4 byte quaternion), but it would require some effort with really, really questionable gains.
    [StructLayout(LayoutKind.Explicit, Size = 48)]
    public struct BoxInstance
    {
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(12)]
        public uint PackedColor;
        [FieldOffset(16)]
        public Quaternion Orientation;
        [FieldOffset(32)]
        public float HalfWidth;
        [FieldOffset(36)]
        public float HalfHeight;
        [FieldOffset(40)]
        public float HalfLength;
    }
    public class BoxRenderer : RasterizedRenderer<BoxInstance>
    {
        private readonly IndexBuffer indices;

        public BoxRenderer(WebGL2Context context) : base(context) => indices = new(context);
        public async Task InitializeAsync(ContentArchive content, int maximumInstancesPerDraw = 2048)
        {
            await base.InitializeAsync(content, @"ShapeDrawing\RenderBoxes", maximumInstancesPerDraw);
            await indices.InitializeAsync(Helpers.GetBoxIndices(1));
            await instances.VertexAttribPointerAsync(0, 3, 0);
            await instances.VertexAttribIPointerAsync(1, 1, sizeof(float) * 3);
            await instances.VertexAttribPointerAsync(2, 4, sizeof(float) * 3 + sizeof(uint));
            await instances.VertexAttribPointerAsync(3, 3, sizeof(float) * 7 + sizeof(uint));
        }
        protected override async ValueTask DoDisposeAsync()
        {
            await indices.DisposeAsync();
            await base.DoDisposeAsync();
        }
        protected override Task OnBatchDrawAsync(int batchCount) => context.DrawElementsInstancedAsync(Primitive.TRIANGLES, 36, indices.Type, 0, batchCount);
    }
}
