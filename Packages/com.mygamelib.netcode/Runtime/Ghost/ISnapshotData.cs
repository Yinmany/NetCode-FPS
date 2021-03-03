using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport.Utilities;
using UnityEngine;

namespace MyGameLib.NetCode
{
    /// <summary>
    /// 存放快照数据
    /// </summary>
    public struct SnapshotDataBuffer : IBufferElementData
    {
        public byte Value;
    }

    /// <summary>
    /// 每个Ghost的快照数据
    /// </summary>
    public struct SnapshotData : IComponentData
    {
        public struct DataAtTick
        {
            public IntPtr SnapshotBefore;
            public IntPtr SnapshotAfter;
            public float InterpolationFactor;
            public uint Tick;
        }

        // 一种Ghost完整快照大小（包含tick的大小）
        public int SnapshotSize;
        public int LatestIndex;

        /// <summary>
        /// 获取最新的tick
        /// </summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        public unsafe uint GetLatestTick(in DynamicBuffer<SnapshotDataBuffer> buf)
        {
            if (buf.Length == 0)
                return 0;
            byte* ptr = (byte*) buf.GetUnsafePtr();
            ptr += LatestIndex %
                GlobalConstants.SnapshotHistorySize * SnapshotSize;
            return *(uint*) ptr;
        }

        /// <summary>
        /// 获取最新的快照
        /// </summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        public unsafe IntPtr GetLatest(in DynamicBuffer<SnapshotDataBuffer> buf)
        {
            if (buf.Length == 0)
                return IntPtr.Zero;
            byte* ptr = (byte*) buf.GetUnsafePtr();
            ptr += LatestIndex %
                GlobalConstants.SnapshotHistorySize * SnapshotSize;
            return new IntPtr(ptr);
        }

        /// <summary>
        /// 获取目标tick的两个快照
        /// 从后往前找
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="targetTick"></param>
        /// <param name="targetTickFraction">Update与FixedUpdate的比</param>
        /// <param name="data"></param>
        /// <returns></returns>
        public unsafe bool GetDataAtTick(in DynamicBuffer<SnapshotDataBuffer> buf, uint targetTick,
            float targetTickFraction, out DataAtTick data)
        {
            data = default;

            int beforeIndex = 0, afterIndex = 0;
            uint beforeTick = 0, afterTick = 0;

            // 长度 / 大小 = 快照数量
            int nums = buf.Length / SnapshotSize;
            for (int i = 0; i < nums; i++)
            {
                // 到0的时候，就需要单独处理了.
                int curIdx = (LatestIndex + GlobalConstants.SnapshotHistorySize - i) %
                             GlobalConstants.SnapshotHistorySize;
                byte* snap = (byte*) buf.GetUnsafePtr() + curIdx * SnapshotSize;
                uint tick = *(uint*) snap;
                if (tick == 0)
                    continue;

                // 从最新tick向旧tick遍历
                if (SequenceHelpers.IsNewer(tick, targetTick))
                {
                    afterIndex = curIdx;
                    afterTick = tick;
                }
                else
                {
                    beforeIndex = curIdx;
                    beforeTick = tick;
                    break;
                }
            }

            if (beforeTick == 0)
                return false;

            // 插值
            data.SnapshotBefore = (IntPtr) ((byte*) buf.GetUnsafePtr() + beforeIndex * SnapshotSize);
            data.Tick = beforeTick;
            if (afterTick == 0)
            {
                data.SnapshotAfter = data.SnapshotBefore;
                data.InterpolationFactor = 0;
            }
            else
            {
                data.SnapshotAfter = (IntPtr) ((byte*) buf.GetUnsafePtr() + afterIndex * SnapshotSize);
                float relativeTime = targetTick - beforeTick + targetTickFraction;
                data.InterpolationFactor = math.clamp(relativeTime / (afterTick - beforeTick), 0f, 1f);
            }

            return true;
        }
    }
}