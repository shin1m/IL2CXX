using System;
using System.Runtime.InteropServices.JavaScript;
using GL = WebGL2;

namespace DemoRenderer
{
    public class IndexBuffer : Disposable
    {
        private readonly GL context;
        private readonly JSObject? buffer;

        public int Type => GL.UNSIGNED_INT;

        public IndexBuffer(GL context)
        {
            this.context = context;
            buffer = context.CreateBuffer();
        }
        protected override void DoDispose()
        {
            context.DeleteBuffer(buffer);
            buffer?.Dispose();
        }
        public void Initialize(Span<uint> indices)
        {
            context.BindBuffer(GL.ELEMENT_ARRAY_BUFFER, buffer);
            context.BufferData(GL.ELEMENT_ARRAY_BUFFER, indices, GL.STATIC_DRAW);
        }
    }
}
