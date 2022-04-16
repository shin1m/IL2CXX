using System;
using System.Runtime.CompilerServices;
using Blazor.Extensions.Canvas.WebGL;

namespace DemoRenderer
{
    public class ConstantsBuffer<T> : Disposable where T : unmanaged
    {
        private static readonly int alignedSize;

        static ConstantsBuffer()
        {
            var size = Unsafe.SizeOf<T>();
            alignedSize = (size >> 4) << 4;
            if (alignedSize < size) alignedSize += 16;
        }

        private readonly WebGL2Context context;
        private WebGLBuffer? buffer;

        public ConstantsBuffer(WebGL2Context context) => this.context = context;
        public async Task InitializeAsync()
        {
            buffer = await context.CreateBufferAsync();
            await context.BindBufferAsync(BufferType.UNIFORM_BUFFER, buffer);
            await context.BufferDataAsync(BufferType.UNIFORM_BUFFER, alignedSize, BufferUsageHint.DYNAMIC_DRAW);
        }
        protected override async ValueTask DoDisposeAsync() => await context.DeleteBufferAsync(buffer);
        public async Task UpdateAsync(uint index, T data)
        {
            await context.BindBufferBaseAsync(BufferType.UNIFORM_BUFFER, index, buffer);
            await context.BufferSubDataAsync(BufferType.UNIFORM_BUFFER, 0, new[] { data });
        }
    }
}
