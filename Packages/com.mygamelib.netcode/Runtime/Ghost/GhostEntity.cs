using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace MyGameLib.NetCode
{
    public struct GhostEntity
    {
        public Entity Entity;
        public int GhostId;
        public int GhostType;
        public uint RemoveFlags;
    }

    /// <summary>
    /// 生成GhostEntity数据
    /// </summary>
    public struct SpawnGhostEntity : IDisposable
    {
        public int GhostId;
        public int GhostType;

        // FIXME: 临时快照内存，创建时的那一个快照。创建完成后，释放此内存。
        // 以后可以使用循环Buffer来作为临时快照存储区，不用每次都分配释放内存。
        public IntPtr TmpSnapshotData;

        public unsafe void Dispose()
        {
            if (TmpSnapshotData != IntPtr.Zero)
                UnsafeUtility.Free((void*) TmpSnapshotData, Allocator.Temp);
        }
    }
}