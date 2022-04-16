using System;
using System.Runtime.CompilerServices;
using Blazor.Extensions.Canvas.WebGL;

namespace DemoRenderer
{
    public class ArrayBuffer : Disposable
    {
        protected readonly WebGL2Context context;
        private int stride;
        private WebGLBuffer? buffer;
        public int Capacity { get; private set; }

        public ArrayBuffer(WebGL2Context context) => this.context = context;
        public async Task InitializeAsync<T>(int capacity)
        {
            stride = Unsafe.SizeOf<T>();
            buffer = await context.CreateBufferAsync();
            await AllocateAsync(capacity);
        }
        protected override async ValueTask DoDisposeAsync() => await context.DeleteBufferAsync(buffer);
        public async Task AllocateAsync(int capacity)
        {
            Capacity = capacity;
            await context.BindBufferAsync(BufferType.ARRAY_BUFFER, buffer);
            await context.BufferDataAsync(BufferType.ARRAY_BUFFER, stride * capacity, BufferUsageHint.DYNAMIC_DRAW);
        }
        public async Task VertexAttribPointerAsync(uint index, int size, long offset)
        {
            await context.BindBufferAsync(BufferType.ARRAY_BUFFER, buffer);
            await context.VertexAttribPointerAsync(index, size, DataType.FLOAT, false, stride, offset);
            await context.EnableVertexAttribArrayAsync(index);
        }
        public async Task VertexAttribIPointerAsync(uint index, int size, long offset)
        {
            await context.BindBufferAsync(BufferType.ARRAY_BUFFER, buffer);
            await context.VertexAttribIPointerAsync(index, size, DataType.UNSIGNED_INT, stride, offset);
            await context.EnableVertexAttribArrayAsync(index);
        }
        public async Task UpdateAsync<T>(T[] data, int offset = 0) where T : unmanaged
        {
            await context.BindBufferAsync(BufferType.ARRAY_BUFFER, buffer);
            await context.BufferSubDataAsync(BufferType.ARRAY_BUFFER, (uint)(offset * stride), data);
        }
    }
    public class InstanceBuffer : ArrayBuffer
    {
        public InstanceBuffer(WebGL2Context context) : base(context) { }
        public new async Task VertexAttribPointerAsync(uint index, int size, long offset)
        {
            await base.VertexAttribPointerAsync(index, size, offset);
            await context.VertexAttribDivisorAsync(index, 1);
        }
        public new async Task VertexAttribIPointerAsync(uint index, int size, long offset)
        {
            await base.VertexAttribIPointerAsync(index, size, offset);
            await context.VertexAttribDivisorAsync(index, 1);
        }
    }
}
