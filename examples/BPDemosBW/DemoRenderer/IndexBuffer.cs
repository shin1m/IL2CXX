using Blazor.Extensions.Canvas.WebGL;

namespace DemoRenderer
{
    public class IndexBuffer : Disposable
    {
        private readonly WebGLContext context;
        private WebGLBuffer? buffer;

        public DataType Type => DataType.UNSIGNED_INT;

        public IndexBuffer(WebGLContext context) => this.context = context;
        public async Task InitializeAsync(uint[] indices)
        {
            buffer = await context.CreateBufferAsync();
            await context.BindBufferAsync(BufferType.ELEMENT_ARRAY_BUFFER, buffer);
            await context.BufferDataAsync(BufferType.ELEMENT_ARRAY_BUFFER, indices, BufferUsageHint.STATIC_DRAW);
        }
        protected override async ValueTask DoDisposeAsync() => await context.DeleteBufferAsync(buffer);
    }
}
