using System;
using MyGameLib.NetCode;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using AOT;

using MyGameLib.NetCode.Serializer;
using UnityEngine;

namespace Assembly_CSharp.Generated
{
    [BurstCompile]
    public struct NetworkRigidbodySerializer
    {
        public static GhostComponentSerializer Serializer;

        static NetworkRigidbodySerializer()
        {
            Serializer = new GhostComponentSerializer
            {
                IsUpdateValue = false,
                SendType = GhostSendType.All,
                ComponentType = ComponentType.ReadWrite<NetworkRigidbody>(),
                ComponentSize = UnsafeUtility.SizeOf<NetworkRigidbody>(),
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
			public float3 velocity;
			public float3 angularVelocity;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyToSnapshotDelegate))]
        static void CopyToSnapshot(IntPtr compPtr, IntPtr dataPtr)
        {
            ref NetworkRigidbody comp = ref GhostComponentSerializer.TypeCast<NetworkRigidbody>(compPtr);
            ref Snapshot snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(dataPtr);

			snapshot.velocity = comp.velocity;
			snapshot.angularVelocity = comp.angularVelocity;
            // Debug.Log(snapshot.velocity);
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyFromSnapshotDelegate))]
        static void CopyFromSnapshot(IntPtr compPtr, IntPtr dataPtr, int offset)
        {
            ref NetworkRigidbody comp = ref GhostComponentSerializer.TypeCast<NetworkRigidbody>(compPtr);
            ref SnapshotData.DataAtTick dataAtTick =
                ref GhostComponentSerializer.TypeCast<SnapshotData.DataAtTick>(dataPtr);

            // 不管需不需要插值，这个数据都是需要的。
            ref Snapshot after = ref GhostComponentSerializer.TypeCast<Snapshot>(dataAtTick.SnapshotAfter, offset);

			ref Snapshot before = ref GhostComponentSerializer.TypeCast<Snapshot>(dataAtTick.SnapshotBefore, offset);
			comp.velocity = after.velocity;
			comp.angularVelocity = after.angularVelocity;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.RestoreFromBackupDelegate))]
        static void RestoreFromBackup(IntPtr compPtr, IntPtr backupData)
        {
            ref NetworkRigidbody comp = ref GhostComponentSerializer.TypeCast<NetworkRigidbody>(compPtr);
            ref Snapshot snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(backupData);

			comp.velocity = snapshot.velocity;
			comp.angularVelocity = snapshot.angularVelocity;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.SerializeDelegate))]
        static void Serialize(IntPtr dataPtr, ref DataStreamWriter writer,
            ref NetworkCompressionModel compressionModel)
        {
            ref Snapshot snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(dataPtr);
			writer.WritePackedFloat3(snapshot.velocity, compressionModel);
			writer.WritePackedFloat3(snapshot.angularVelocity, compressionModel);
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.DeserializeDelegate))]
        static void Deserialize(IntPtr dataPtr, ref DataStreamReader reader,
            ref NetworkCompressionModel compressionModel)
        {
            ref Snapshot snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(dataPtr);
			snapshot.velocity = reader.ReadPackedFloat3(compressionModel);
			snapshot.angularVelocity = reader.ReadPackedFloat3(compressionModel);
        }
    }
}