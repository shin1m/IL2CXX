using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using DemoContentLoader;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GL = ThinJS.WebGL2;

namespace DemoRenderer.ShapeDrawing
{
    [StructLayout(LayoutKind.Explicit, Size = 48)]
    public struct MeshInstance
    {
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(12)]
        public uint PackedColor;
        [FieldOffset(16)]
        public ulong PackedOrientation;
        [FieldOffset(24)]
        public int VertexStart;
        [FieldOffset(28)]
        public int VertexCount;
        [FieldOffset(32)]
        public Vector3 Scale;
    }
    public class MeshRenderer : Shader
    {
        private readonly MeshCache meshCache;
        private readonly InstanceBuffer<MeshInstance> instances;

        public MeshRenderer(GL context, MeshCache meshCache) : base(context)
        {
            this.meshCache = meshCache;
            instances = new(context);
        }
        protected override void DoDispose()
        {
            instances.Dispose();
            base.DoDispose();
        }
        public void Initialize(ContentArchive content, int maximumInstancesPerDraw = 2048)
        {
            var program = Build(
                content.Load<GLSLContent>(@"ShapeDrawing\RenderMeshes.glvs").Source,
                content.Load<GLSLContent>(@"ShapeDrawing\RenderMeshes.glfs").Source
            );
            context.UniformBlockBinding(program, context.GetUniformBlockIndex(program, "type_VertexConstants"), 0);
            instances.Allocate(maximumInstancesPerDraw);
            instances.VertexAttribPointer(0, 3, 0);
            instances.VertexAttribIPointer(1, 1, sizeof(float) * 3);
            instances.VertexAttribIPointer(2, 2, sizeof(float) * 3 + sizeof(uint));
            instances.VertexAttribPointer(3, 3, sizeof(float) * 3 + sizeof(uint) * 5);
            meshCache.TriangleBuffer.VertexAttribPointer(4, 3, 0);
        }
        public void Render(Buffer<MeshInstance> instances, int start, int count)
        {
            //Examine the set of instances and batch them into groups using the same mesh data.
            void build(Buffer<MeshInstance> instances, int i, int end, ref QuickDictionary<ulong, QuickList<MeshInstance>, PrimitiveComparer<ulong>> batches)
            {
                for (; i < end; ++i)
                {
                    ref var instance = ref instances[i];
                    ref var id = ref Unsafe.As<int, ulong>(ref instance.VertexStart);

                    if (batches.GetTableIndices(ref id, out var tableIndex, out var elementIndex))
                    {
                        //The id was already present.
                        batches.Values[elementIndex].Add(instance, meshCache.Pool);
                    }
                    else
                    {
                        //There is no batch for this vertex region, so create one.
                        var newCount = batches.Count + 1;
                        if (newCount > batches.Keys.Length)
                        {
                            batches.Resize(newCount, meshCache.Pool);
                            //Resizing will change the table indices, so we have to grab it again.
                            batches.GetTableIndices(ref id, out tableIndex, out _);
                        }
                        batches.Keys[batches.Count] = id;
                        ref var listSlot = ref batches.Values[batches.Count];
                        listSlot = new QuickList<MeshInstance>(64, meshCache.Pool);
                        listSlot.Add(instance, meshCache.Pool);
                        batches.Table[tableIndex] = newCount;
                        batches.Count = newCount;
                    }
                }
            }
            var batches = new QuickDictionary<ulong, QuickList<MeshInstance>, PrimitiveComparer<ulong>>(16, meshCache.Pool);
            build(instances, start, start + count, ref batches);

            Use();
            for (int i = 0; i < batches.Count; ++i)
            {
                var batch = batches.Values[i];
                var batchVertexStart = batch[0].VertexStart;
                var batchVertexCount = batch[0].VertexCount;
                while (batch.Count > 0)
                {
                    var subbatchStart = Math.Max(0, batch.Count - this.instances.Capacity);
                    var subbatchCount = batch.Count - subbatchStart;
                    this.instances.Update(batch.Span.Slice(subbatchStart, subbatchCount).ToArray());
                    context.DrawArraysInstanced(GL.TRIANGLES, batchVertexStart, batchVertexCount, subbatchCount);
                    batch.Count -= subbatchCount;
                }
                batch.Dispose(meshCache.Pool);
            }
            batches.Dispose(meshCache.Pool);
        }
    }
}
