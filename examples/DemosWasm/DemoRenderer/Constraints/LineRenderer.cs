using BepuUtilities;
using BepuUtilities.Memory;
using DemoContentLoader;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GL = WebGL2;

namespace DemoRenderer.Constraints
{
    /// <summary>
    /// GPU-relevant information for the rendering of a single line instance.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct LineInstance
    {
        [FieldOffset(0)]
        public Vector3 Start;
        [FieldOffset(12)]
        public uint PackedBackgroundColor;
        [FieldOffset(16)]
        public Vector3 End;
        [FieldOffset(28)]
        public uint PackedColor;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LineInstance(in Vector3 start, in Vector3 end, in Vector3 color, in Vector3 backgroundColor)
        {
            Start = start;
            PackedBackgroundColor = Helpers.PackColor(backgroundColor);
            End = end;
            PackedColor = Helpers.PackColor(color);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LineInstance(in Vector3 start, in Vector3 end, uint packedColor, uint packedBackgroundColor)
        {
            Start = start;
            PackedBackgroundColor = packedBackgroundColor;
            End = end;
            PackedColor = packedColor;
        }
    }

    public class LineRenderer : Shader
    {
        [StructLayout(LayoutKind.Explicit)]
        struct VertexConstants
        {
            [FieldOffset(0)]
            public Matrix ViewProjection;
            [FieldOffset(64)]
            public Vector2 NDCToScreenScale;
            [FieldOffset(80)]
            public Vector3 CameraForward;
            [FieldOffset(92)]
            public float TanAnglePerPixel;
            [FieldOffset(96)]
            public Vector3 CameraRight;
            [FieldOffset(112)]
            public Vector3 CameraPosition;
        }

        private readonly ConstantsBuffer<VertexConstants> vertexConstants;
        private readonly IndexBuffer indices;
        private readonly InstanceBuffer<LineInstance> instances;

        public LineRenderer(GL context) : base(context)
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
        public void Initialize(ContentArchive content, int maximumInstancesPerDraw = 16384)
        {
            var program = Build(
                content.Load<GLSLContent>(@"Constraints\RenderLines.glvs").Source,
                content.Load<GLSLContent>(@"Constraints\RenderLines.glfs").Source
            );
            context.UniformBlockBinding(program, context.GetUniformBlockIndex(program, "type_VertexConstants"), 0);
            vertexConstants.Initialize();
            indices.Initialize(Helpers.GetBoxIndices(1));
            instances.Allocate(maximumInstancesPerDraw);
            instances.VertexAttribPointer(0, 3, 0);
            instances.VertexAttribIPointer(1, 1, sizeof(float) * 3);
            instances.VertexAttribPointer(2, 3, sizeof(float) * 3 + sizeof(uint));
            instances.VertexAttribIPointer(3, 1, sizeof(float) * 6 + sizeof(uint));
        }
        public void Render(Camera camera, Int2 resolution, Buffer<LineInstance> instances, int start, int count)
        {
            Use();
            vertexConstants.Update(0, new VertexConstants
            {
                ViewProjection = camera.ViewProjection,
                NDCToScreenScale = new Vector2(resolution.X / 2f, resolution.Y / 2f),
                CameraForward = camera.Forward,
                TanAnglePerPixel = (float)Math.Tan(camera.FieldOfView / resolution.Y),
                CameraRight = camera.Right,
                CameraPosition = camera.Position
            });
            while (count > 0)
            {
                var batchCount = Math.Min(this.instances.Capacity, count);
                this.instances.Update(instances.Slice(start, batchCount).ToArray());
                context.DrawElementsInstanced(GL.TRIANGLES, 36, indices.Type, 0, batchCount);
                count -= batchCount;
                start += batchCount;
            }
        }
    }
}
