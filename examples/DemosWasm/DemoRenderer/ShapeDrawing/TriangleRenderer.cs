using DemoContentLoader;
using System.Numerics;
using System.Runtime.InteropServices;
using GL = ThinJS.WebGL2;

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
        public TriangleRenderer(GL context) : base(context) { }
        public void Initialize(ContentArchive content, int maximumInstancesPerDraw = 2048)
        {
            base.Initialize(content, @"ShapeDrawing\RenderTriangles", maximumInstancesPerDraw);
            instances.VertexAttribPointer(0, 3, 0);
            instances.VertexAttribIPointer(1, 1, sizeof(float) * 3);
            instances.VertexAttribPointer(2, 3, sizeof(float) * 3 + sizeof(uint));
            instances.VertexAttribPointer(3, 1, sizeof(float) * 6 + sizeof(uint));
            instances.VertexAttribPointer(4, 3, sizeof(float) * 7 + sizeof(uint));
            instances.VertexAttribPointer(5, 1, sizeof(float) * 10 + sizeof(uint));
            instances.VertexAttribIPointer(6, 2, sizeof(float) * 11 + sizeof(uint));
            instances.VertexAttribPointer(7, 1, sizeof(float) * 11 + sizeof(uint) * 3);
        }
        protected override void OnBatchDraw(int batchCount) => context.DrawArraysInstanced(GL.TRIANGLES, 0, 3, batchCount);
    }
}
