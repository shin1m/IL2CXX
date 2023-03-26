using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using GL = WebGL2;

namespace DemoRenderer
{
    public class ArrayBuffer<T> : Disposable where T : unmanaged
    {
        private static int stride = Unsafe.SizeOf<T>();

        protected readonly GL context;
        private readonly JSObject? buffer;
        public int Capacity { get; private set; }

        public ArrayBuffer(GL context)
        {
            this.context = context;
            buffer = context.CreateBuffer();
        }
        protected override void DoDispose()
        {
            context.DeleteBuffer(buffer);
            buffer?.Dispose();
        }
        public void Allocate(int capacity)
        {
            Capacity = capacity;
            context.BindBuffer(GL.ARRAY_BUFFER, buffer);
            context.BufferData(GL.ARRAY_BUFFER, stride * capacity, GL.DYNAMIC_DRAW);
        }
        public void VertexAttribPointer(int index, int size, long offset)
        {
            context.BindBuffer(GL.ARRAY_BUFFER, buffer);
            context.VertexAttribPointer(index, size, GL.FLOAT, false, stride, offset);
            context.EnableVertexAttribArray(index);
        }
        public void VertexAttribIPointer(int index, int size, long offset)
        {
            context.BindBuffer(GL.ARRAY_BUFFER, buffer);
            context.VertexAttribIPointer(index, size, GL.UNSIGNED_INT, stride, offset);
            context.EnableVertexAttribArray(index);
        }
        public void Update(Span<T> data, int offset = 0)
        {
            context.BindBuffer(GL.ARRAY_BUFFER, buffer);
            context.BufferSubData(GL.ARRAY_BUFFER, offset * stride, data);
        }
    }
    public class InstanceBuffer<T> : ArrayBuffer<T> where T : unmanaged
    {
        public InstanceBuffer(GL context) : base(context) { }
        public new void VertexAttribPointer(int index, int size, long offset)
        {
            base.VertexAttribPointer(index, size, offset);
            context.VertexAttribDivisor(index, 1);
        }
        public new void VertexAttribIPointer(int index, int size, long offset)
        {
            base.VertexAttribIPointer(index, size, offset);
            context.VertexAttribDivisor(index, 1);
        }
    }
}
