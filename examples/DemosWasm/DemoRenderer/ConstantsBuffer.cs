using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using GL = WebGL2;

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

        private readonly GL context;
        private readonly JSObject? buffer;

        public ConstantsBuffer(GL context)
        {
            this.context = context;
            buffer = context.CreateBuffer();
        }
        protected override void DoDispose()
        {
            context.DeleteBuffer(buffer);
            buffer?.Dispose();
        }
        public void Initialize()
        {
            context.BindBuffer(GL.UNIFORM_BUFFER, buffer);
            context.BufferData(GL.UNIFORM_BUFFER, alignedSize, GL.DYNAMIC_DRAW);
        }
        public void Update(int index, T data)
        {
            context.BindBufferBase(GL.UNIFORM_BUFFER, index, buffer);
            context.BufferSubData(GL.UNIFORM_BUFFER, 0, new Span<T>(ref data));
        }
    }
}
