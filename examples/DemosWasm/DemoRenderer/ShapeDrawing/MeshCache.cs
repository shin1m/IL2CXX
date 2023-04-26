﻿using BepuUtilities.Collections;
using BepuUtilities.Memory;
using System;
using System.Numerics;
using GL = ThinJS.WebGL2;

namespace DemoRenderer.ShapeDrawing
{
    /// <summary>
    /// Stores references to triangle data between usages to avoid the need to regather it every frame.
    /// </summary>
    public class MeshCache : Disposable
    {
        Buffer<Vector4> vertices;
        public readonly ArrayBuffer<Vector4> TriangleBuffer;
        QuickSet<ulong, PrimitiveComparer<ulong>> previouslyAllocatedIds;
        QuickList<ulong> requestedIds;

        struct UploadRequest
        {
            public int Start;
            public int Count;
        }
        QuickList<UploadRequest> pendingUploads;

        public readonly BufferPool Pool;
        private Allocator? allocator;

        public MeshCache(GL context, BufferPool pool)
        {
            Pool = pool;
            TriangleBuffer = new(context);
            pendingUploads = new(128, pool);
            requestedIds = new(128, pool);
            previouslyAllocatedIds = new(256, pool);
        }
        protected override void DoDispose()
        {
            TriangleBuffer.Dispose();
            pendingUploads.Dispose(Pool);
            Pool.Return(ref vertices);
            requestedIds.Dispose(Pool);
            previouslyAllocatedIds.Dispose(Pool);
            allocator?.Dispose();
        }
        public void Initialize(int initialSizeInVertices = 1 << 22)
        {
            Pool.TakeAtLeast(initialSizeInVertices, out vertices);
            TriangleBuffer.Allocate(initialSizeInVertices);
            allocator = new Allocator(initialSizeInVertices, Pool);
        }

        public bool TryGetExistingMesh(ulong id, out int start, out Buffer<Vector4> vertices)
        {
            if (allocator!.TryGetAllocationRegion(id, out var allocation))
            {
                start = (int)allocation.Start;
                vertices = this.vertices.Slice(start, (int)(allocation.End - start));
                return true;
            }
            start = default;
            vertices = default;
            return false;
        }

        public bool Allocate(ulong id, int vertexCount, out int start, out Buffer<Vector4> vertices)
        {
            if (TryGetExistingMesh(id, out start, out vertices))
            {
                return false;
            }
            if (allocator!.Allocate(id, vertexCount, out var longStart))
            {
                start = (int)longStart;
                vertices = this.vertices.Slice(start, vertexCount);
                pendingUploads.Add(new UploadRequest { Start = start, Count = vertexCount }, Pool);
                return true;
            }
            //Didn't fit. We need to resize.
            var copyCount = TriangleBuffer.Capacity + vertexCount;
            var newSize = 1 << SpanHelper.GetContainingPowerOf2(copyCount);
            Pool.ResizeToAtLeast(ref this.vertices, newSize, copyCount);
            allocator.Capacity = newSize;
            allocator.Allocate(id, vertexCount, out longStart);
            start = (int)longStart;
            vertices = this.vertices.Slice(start, vertexCount);
            //A resize forces an upload of everything, so any previous pending uploads are unnecessary.
            pendingUploads.Count = 0;
            pendingUploads.Add(new UploadRequest { Start = 0, Count = copyCount }, Pool);
            return true;
        }

        public void FlushPendingUploads()
        {
            if (allocator!.Capacity > TriangleBuffer.Capacity)
            {
                TriangleBuffer.Allocate((int)allocator.Capacity);
            }
            for (int i = 0; i < pendingUploads.Count; ++i)
            {
                var upload = pendingUploads[i];
                TriangleBuffer.Update(vertices.Slice(upload.Start, upload.Count).ToArray(), upload.Start);
            }
            pendingUploads.Count = 0;

            //Get rid of any stale allocations.
            for (int i = 0; i < requestedIds.Count; ++i)
            {
                previouslyAllocatedIds.FastRemove(requestedIds[i]);
            }
            for (int i = 0; i < previouslyAllocatedIds.Count; ++i)
            {
                allocator.Deallocate(previouslyAllocatedIds[i]);
            }
            previouslyAllocatedIds.FastClear();
            for (int i = 0; i < requestedIds.Count; ++i)
            {
                previouslyAllocatedIds.Add(requestedIds[i], Pool);
            }
            requestedIds.Count = 0;

            //This executes at the end of the frame. The next frame will read the compacted location, which will be valid because the pending upload will be handled.
            if (allocator.IncrementalCompact(out var compactedId, out var compactedSize, out var oldStart, out var newStart))
            {
                vertices.CopyTo((int)oldStart, vertices, (int)newStart, (int)compactedSize);
                pendingUploads.Add(new UploadRequest { Start = (int)newStart, Count = (int)compactedSize }, Pool);
            }

        }
    }
}
