using DemoContentLoader;
using System.Numerics;
using System.Runtime.InteropServices;
using Blazor.Extensions.Canvas.WebGL;

namespace DemoRenderer.ShapeDrawing
{
    //This isn't exactly an efficient representation, but isolated triangle rendering is mainly just for small scale testing anyway.
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct TriangleInstance
    {
        [FieldOffset(0)]
        public Vector3 A;
        [FieldOffset(12)]
        public uint PackedColor;
        [FieldOffset(16)]
        public Vector3 B;
        [FieldOffset(28)]
        public float X;
        [FieldOffset(32)]
        public Vector3 C;
        [FieldOffset(44)]
        public float Y;
        [FieldOffset(48)]
        public ulong PackedOrientation;
        [FieldOffset(56)]
        public float Z;
    }
    public class TriangleRenderer : RasterizedRenderer<TriangleInstance>
    {
        public TriangleRenderer(WebGL2Context context) : base(context) { }
        public async Task InitializeAsync(ContentArchive content, int maximumInstancesPerDraw = 2048)
        {
            await base.InitializeAsync(content, @"ShapeDrawing\RenderTriangles", maximumInstancesPerDraw);
            await instances.VertexAttribPointerAsync(0, 3, 0);
            await instances.VertexAttribIPointerAsync(1, 1, sizeof(float) * 3);
            await instances.VertexAttribPointerAsync(2, 3, sizeof(float) * 3 + sizeof(uint));
            await instances.VertexAttribPointerAsync(3, 1, sizeof(float) * 6 + sizeof(uint));
            await instances.VertexAttribPointerAsync(4, 3, sizeof(float) * 7 + sizeof(uint));
            await instances.VertexAttribPointerAsync(5, 1, sizeof(float) * 10 + sizeof(uint));
            await instances.VertexAttribIPointerAsync(6, 2, sizeof(float) * 11 + sizeof(uint));
            await instances.VertexAttribPointerAsync(7, 1, sizeof(float) * 11 + sizeof(uint) * 3);
        }
        protected override Task OnBatchDrawAsync(int batchCount) => context.DrawArraysInstancedAsync(Primitive.TRIANGLES, 0, 3, batchCount);
    }
}
