using BepuUtilities;
using BepuUtilities.Memory;
using DemoContentLoader;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using GL = ThinJS.WebGL2;

namespace DemoRenderer.ShapeDrawing
{
    //These are out here because generic types (including nongeneric types held within generic classes) cannot have explicit layouts.
    [StructLayout(LayoutKind.Explicit)]
    struct RayTracedVertexConstants
    {
        [FieldOffset(0)]
        public Matrix Projection;
        [FieldOffset(64)]
        public Vector3 CameraPosition;
        [FieldOffset(76)]
        public float NearClip;
        [FieldOffset(80)]
        public Vector3 CameraRight;
        [FieldOffset(96)]
        public Vector3 CameraUp;
        [FieldOffset(112)]
        public Vector3 CameraBackward;
    }
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    struct RayTracedPixelConstants
    {
        [FieldOffset(0)]
        public Vector3 CameraRight;
        [FieldOffset(12)]
        public float NearClip;
        [FieldOffset(16)]
        public Vector3 CameraUp;
        [FieldOffset(28)]
        public float FarClip;
        [FieldOffset(32)]
        public Vector3 CameraBackward;
        [FieldOffset(48)]
        public Vector2 PixelSizeAtUnitPlane;
    }
    public abstract class RayTracedRenderer<TInstance> : Shader where TInstance : unmanaged
    {
        private readonly IndexBuffer indices;
        protected readonly InstanceBuffer<TInstance> instances;

        public RayTracedRenderer(GL context) : base(context)
        {
            indices = new(context);
            instances = new(context);
        }
        protected override void DoDispose()
        {
            indices.Dispose();
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
            context.UniformBlockBinding(program, context.GetUniformBlockIndex(program, "type_PixelConstants"), 1);
            indices.Initialize(Helpers.GetBoxIndices(1));
            instances.Allocate(maximumInstancesPerDraw);
        }
        public void Render(Buffer<TInstance> instances, int start, int count)
        {
            Use();
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
    public class SphereRenderer : RayTracedRenderer<SphereInstance>
    {
        public SphereRenderer(GL context) : base(context) { }
        public void Initialize(ContentArchive content)
        {
            base.Initialize(content, @"ShapeDrawing\RenderSpheres");
            instances.VertexAttribPointer(0, 3, 0);
            instances.VertexAttribPointer(1, 1, sizeof(float) * 3);
            instances.VertexAttribPointer(2, 3, sizeof(float) * 4);
            instances.VertexAttribIPointer(3, 1, sizeof(float) * 7);
        }
    }
    public class CapsuleRenderer : RayTracedRenderer<CapsuleInstance>
    {
        public CapsuleRenderer(GL context) : base(context) { }
        public void Initialize(ContentArchive content)
        {
            base.Initialize(content, @"ShapeDrawing\RenderCapsules");
            instances.VertexAttribPointer(0, 3, 0);
            instances.VertexAttribPointer(1, 1, sizeof(float) * 3);
            instances.VertexAttribIPointer(2, 2, sizeof(float) * 4);
            instances.VertexAttribPointer(3, 1, sizeof(float) * 4 + sizeof(uint) * 2);
            instances.VertexAttribIPointer(4, 1, sizeof(float) * 5 + sizeof(uint) * 2);
        }
    }
    public class CylinderRenderer : RayTracedRenderer<CylinderInstance>
    {
        public CylinderRenderer(GL context) : base(context) { }
        public void Initialize(ContentArchive content)
        {
            base.Initialize(content, @"ShapeDrawing\RenderCylinders");
            instances.VertexAttribPointer(0, 3, 0);
            instances.VertexAttribPointer(1, 1, sizeof(float) * 3);
            instances.VertexAttribIPointer(2, 2, sizeof(float) * 4);
            instances.VertexAttribPointer(3, 1, sizeof(float) * 4 + sizeof(uint) * 2);
            instances.VertexAttribIPointer(4, 1, sizeof(float) * 5 + sizeof(uint) * 2);
        }
    }
}
