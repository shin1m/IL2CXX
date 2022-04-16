using BepuUtilities;
using BepuUtilities.Memory;
using DemoContentLoader;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Blazor.Extensions.Canvas.WebGL;

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
        protected readonly InstanceBuffer instances;

        public RayTracedRenderer(WebGL2Context context) : base(context)
        {
            indices = new(context);
            instances = new(context);
        }
        protected async Task InitializeAsync(ContentArchive content, string shaderPath, int maximumInstancesPerDraw = 2048)
        {
            await InitializeAsync(
                content.Load<GLSLContent>($"{shaderPath}.glvs").Source,
                content.Load<GLSLContent>($"{shaderPath}.glfs").Source
            );
            await context.UniformBlockBindingAsync(program, await context.GetUniformBlockIndexAsync(program, "type_VertexConstants"), 0);
            await context.UniformBlockBindingAsync(program, await context.GetUniformBlockIndexAsync(program, "type_PixelConstants"), 1);
            await indices.InitializeAsync(Helpers.GetBoxIndices(1));
            await instances.InitializeAsync<TInstance>(maximumInstancesPerDraw);
        }
        protected override async ValueTask DoDisposeAsync()
        {
            await indices.DisposeAsync();
            await instances.DisposeAsync();
            await base.DoDisposeAsync();
        }
        public async Task RenderAsync(Buffer<TInstance> instances, int start, int count)
        {
            await UseAsync();
            while (count > 0)
            {
                var batchCount = Math.Min(this.instances.Capacity, count);
                await this.instances.UpdateAsync(instances.Slice(start, batchCount).ToArray());
                await context.DrawElementsInstancedAsync(Primitive.TRIANGLES, 36, indices.Type, 0, batchCount);
                count -= batchCount;
                start += batchCount;
            }
        }
    }
    public class SphereRenderer : RayTracedRenderer<SphereInstance>
    {
        public SphereRenderer(WebGL2Context context) : base(context) { }
        public async Task InitializeAsync(ContentArchive content)
        {
            await base.InitializeAsync(content, @"ShapeDrawing\RenderSpheres");
            await instances.VertexAttribPointerAsync(0, 3, 0);
            await instances.VertexAttribPointerAsync(1, 1, sizeof(float) * 3);
            await instances.VertexAttribPointerAsync(2, 3, sizeof(float) * 4);
            await instances.VertexAttribIPointerAsync(3, 1, sizeof(float) * 7);
        }
    }
    public class CapsuleRenderer : RayTracedRenderer<CapsuleInstance>
    {
        public CapsuleRenderer(WebGL2Context context) : base(context) { }
        public async Task InitializeAsync(ContentArchive content)
        {
            await base.InitializeAsync(content, @"ShapeDrawing\RenderCapsules");
            await instances.VertexAttribPointerAsync(0, 3, 0);
            await instances.VertexAttribPointerAsync(1, 1, sizeof(float) * 3);
            await instances.VertexAttribIPointerAsync(2, 2, sizeof(float) * 4);
            await instances.VertexAttribPointerAsync(3, 1, sizeof(float) * 4 + sizeof(uint) * 2);
            await instances.VertexAttribIPointerAsync(4, 1, sizeof(float) * 5 + sizeof(uint) * 2);
        }
    }
    public class CylinderRenderer : RayTracedRenderer<CylinderInstance>
    {
        public CylinderRenderer(WebGL2Context context) : base(context) { }
        public async Task InitializeAsync(ContentArchive content)
        {
            await base.InitializeAsync(content, @"ShapeDrawing\RenderCylinders");
            await instances.VertexAttribPointerAsync(0, 3, 0);
            await instances.VertexAttribPointerAsync(1, 1, sizeof(float) * 3);
            await instances.VertexAttribIPointerAsync(2, 2, sizeof(float) * 4);
            await instances.VertexAttribPointerAsync(3, 1, sizeof(float) * 4 + sizeof(uint) * 2);
            await instances.VertexAttribIPointerAsync(4, 1, sizeof(float) * 5 + sizeof(uint) * 2);
        }
    }
}
