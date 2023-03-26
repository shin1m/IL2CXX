using BepuUtilities;
using BepuUtilities.Memory;
using DemoContentLoader;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using GL = WebGL2;

namespace DemoRenderer.ShapeDrawing
{
    [StructLayout(LayoutKind.Explicit)]
    struct RasterizedVertexConstants
    {
        [FieldOffset(0)]
        public Matrix Projection;
        [FieldOffset(64)]
        public Vector3 CameraPosition;
        [FieldOffset(80)]
        public Vector3 CameraRight;
        [FieldOffset(96)]
        public Vector3 CameraUp;
        [FieldOffset(112)]
        public Vector3 CameraBackward;
    }
    public abstract class RasterizedRenderer<TInstance> : Shader where TInstance : unmanaged
    {
        protected readonly InstanceBuffer<TInstance> instances;

        public RasterizedRenderer(GL context) : base(context) => instances = new(context);
        protected override void DoDispose()
        {
            instances.Dispose();
            base.DoDispose();
        }
        protected void Initialize(ContentArchive content, string shaderPath, int maximumInstancesPerDraw = 2048)
        {
            var program = Build(
                content.Load<GLSLContent>($"{shaderPath}.glvs").Source,
                content.Load<GLSLContent>($"{shaderPath}.glfs").Source
            );
            context.UniformBlockBinding(program, context.GetUniformBlockIndex(program, "type_VertexConstants"), 0);
            instances.Allocate(maximumInstancesPerDraw);
        }
        protected abstract void OnBatchDraw(int batchCount);
        public void Render(Buffer<TInstance> instances, int start, int count)
        {
            Use();
            while (count > 0)
            {
                var batchCount = Math.Min(this.instances.Capacity, count);
                this.instances.Update(instances.Slice(start, batchCount).ToArray());
                OnBatchDraw(batchCount);
                count -= batchCount;
                start += batchCount;
            }
        }
    }
}
