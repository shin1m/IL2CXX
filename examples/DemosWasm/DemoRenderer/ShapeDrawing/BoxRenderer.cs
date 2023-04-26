using DemoContentLoader;
using System.Numerics;
using System.Runtime.InteropServices;
using GL = ThinJS.WebGL2;

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

        public BoxRenderer(GL context) : base(context) => indices = new(context);
        protected override void DoDispose()
        {
            indices.Dispose();
            base.DoDispose();
        }
        public void Initialize(ContentArchive content, int maximumInstancesPerDraw = 2048)
        {
            base.Initialize(content, @"ShapeDrawing\RenderBoxes", maximumInstancesPerDraw);
            indices.Initialize(Helpers.GetBoxIndices(1));
            instances.VertexAttribPointer(0, 3, 0);
            instances.VertexAttribIPointer(1, 1, sizeof(float) * 3);
            instances.VertexAttribPointer(2, 4, sizeof(float) * 3 + sizeof(uint));
            instances.VertexAttribPointer(3, 3, sizeof(float) * 7 + sizeof(uint));
        }
        protected override void OnBatchDraw(int batchCount) => context.DrawElementsInstanced(GL.TRIANGLES, 36, indices.Type, 0, batchCount);
    }
}
