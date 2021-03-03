using System;
using MyGameLib.NetCode;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using AOT;

using MyGameLib.NetCode;
using UnityEngine;

namespace MyGameLib.NetCode.Generated
{
    [BurstCompile]
    public struct GhostOwnerComponentSerializer
    {
        public static GhostComponentSerializer Serializer;

        static GhostOwnerComponentSerializer()
        {
            Serializer = new GhostComponentSerializer
            {
                IsUpdateValue = true,
                SendType = GhostSendType.All,
                ComponentType = ComponentType.ReadWrite<GhostOwnerComponent>(),
                ComponentSize = UnsafeUtility.SizeOf<GhostOwnerComponent>(),
                DataSize = UnsafeUtility.SizeOf<Snapshot>(),
                CopyToSnapshot =
                    new PortableFunctionPointer<GhostComponentSerializer.CopyToSnapshotDelegate>(CopyToSnapshot),
                CopyFromSnapshot =
                    new PortableFunctionPointer<GhostComponentSerializer.CopyFromSnapshotDelegate>(CopyFromSnapshot),
                Serialize = new PortableFunctionPointer<GhostComponentSerializer.SerializeDelegate>(Serialize),
                Deserialize = new PortableFunctionPointer<GhostComponentSerializer.DeserializeDelegate>(Deserialize),
                RestoreFromBackup =
                    new PortableFunctionPointer<GhostComponentSerializer.RestoreFromBackupDelegate>(RestoreFromBackup)
            };
        }

        struct Snapshot
        {
			public int Value;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyToSnapshotDelegate))]
        static void CopyToSnapshot(IntPtr compPtr, IntPtr dataPtr)
        {
            ref GhostOwnerComponent comp = ref GhostComponentSerializer.TypeCast<GhostOwnerComponent>(compPtr);
            ref Snapshot snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(dataPtr);

			snapshot.Value = comp.Value;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyFromSnapshotDelegate))]
        static void CopyFromSnapshot(IntPtr compPtr, IntPtr dataPtr, int offset)
        {
            ref GhostOwnerComponent comp = ref GhostComponentSerializer.TypeCast<GhostOwnerComponent>(compPtr);
            ref SnapshotData.DataAtTick dataAtTick =
                ref GhostComponentSerializer.TypeCast<SnapshotData.DataAtTick>(dataPtr);

            // 不管需不需要插值，这个数据都是需要的。
            ref Snapshot after = ref GhostComponentSerializer.TypeCast<Snapshot>(dataAtTick.SnapshotAfter, offset);

			comp.Value = after.Value;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.RestoreFromBackupDelegate))]
        static void RestoreFromBackup(IntPtr compPtr, IntPtr backupData)
        {
            ref GhostOwnerComponent comp = ref GhostComponentSerializer.TypeCast<GhostOwnerComponent>(compPtr);
            ref Snapshot snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(backupData);

			comp.Value = snapshot.Value;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.SerializeDelegate))]
        static void Serialize(IntPtr dataPtr, ref DataStreamWriter writer,
            ref NetworkCompressionModel compressionModel)
        {
            ref Snapshot snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(dataPtr);
			writer.WritePackedInt(snapshot.Value, compressionModel);
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.DeserializeDelegate))]
        static void Deserialize(IntPtr dataPtr, ref DataStreamReader reader,
            ref NetworkCompressionModel compressionModel)
        {
            ref Snapshot snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(dataPtr);
			snapshot.Value = reader.ReadPackedInt(compressionModel);
        }
    }
}