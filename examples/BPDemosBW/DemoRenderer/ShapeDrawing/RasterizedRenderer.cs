using BepuUtilities;
using BepuUtilities.Memory;
using DemoContentLoader;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Blazor.Extensions.Canvas.WebGL;

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
        protected readonly InstanceBuffer instances;

        public RasterizedRenderer(WebGL2Context context) : base(context) => instances = new(context);
        protected async Task InitializeAsync(ContentArchive content, string shaderPath, int maximumInstancesPerDraw = 2048)
        {
            await InitializeAsync(
                content.Load<GLSLContent>($"{shaderPath}.glvs").Source,
                content.Load<GLSLContent>($"{shaderPath}.glfs").Source
            );
            await context.UniformBlockBindingAsync(program, await context.GetUniformBlockIndexAsync(program, "type_VertexConstants"), 0);
            await instances.InitializeAsync<TInstance>(maximumInstancesPerDraw);
        }
        protected override async ValueTask DoDisposeAsync()
        {
            await instances.DisposeAsync();
            await base.DoDisposeAsync();
        }
        protected abstract Task OnBatchDrawAsync(int batchCount);
        public async Task RenderAsync(Buffer<TInstance> instances, int start, int count)
        {
            await UseAsync();
            while (count > 0)
            {
                var batchCount = Math.Min(this.instances.Capacity, count);
                await this.instances.UpdateAsync(instances.Slice(start, batchCount).ToArray());
                await OnBatchDrawAsync(batchCount);
                count -= batchCount;
                start += batchCount;
            }
        }
    }
}
