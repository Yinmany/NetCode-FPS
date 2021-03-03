using System;
using MyGameLib.NetCode;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using AOT;

using Unity.Physics;

namespace Unity.Physics.Generated
{
    [BurstCompile]
    public struct PhysicsVelocitySerializer
    {
        public static GhostComponentSerializer Serializer;

        static PhysicsVelocitySerializer()
        {
            Serializer = new GhostComponentSerializer
            {
                IsUpdateValue = false,
                SendType = GhostSendType.Predicted,
                ComponentType = ComponentType.ReadWrite<PhysicsVelocity>(),
                ComponentSize = UnsafeUtility.SizeOf<PhysicsVelocity>(),
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
			public float3 Linear;
			public float3 Angular;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyToSnapshotDelegate))]
        static void CopyToSnapshot(IntPtr compPtr, IntPtr dataPtr)
        {
            ref PhysicsVelocity comp = ref GhostComponentSerializer.TypeCast<PhysicsVelocity>(compPtr);
            ref Snapshot snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(dataPtr);

			snapshot.Linear = comp.Linear;
			snapshot.Angular = comp.Angular;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyFromSnapshotDelegate))]
        static void CopyFromSnapshot(IntPtr compPtr, IntPtr dataPtr, int offset)
        {
            ref PhysicsVelocity comp = ref GhostComponentSerializer.TypeCast<PhysicsVelocity>(compPtr);
            ref SnapshotData.DataAtTick dataAtTick =
                ref GhostComponentSerializer.TypeCast<SnapshotData.DataAtTick>(dataPtr);

            // 不管需不需要插值，这个数据都是需要的。
            ref Snapshot after = ref GhostComponentSerializer.TypeCast<Snapshot>(dataAtTick.SnapshotAfter, offset);

			ref Snapshot before = ref GhostComponentSerializer.TypeCast<Snapshot>(dataAtTick.SnapshotBefore, offset);
			comp.Linear = after.Linear;
			comp.Angular = after.Angular;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.RestoreFromBackupDelegate))]
        static void RestoreFromBackup(IntPtr compPtr, IntPtr backupData)
        {
            ref PhysicsVelocity comp = ref GhostComponentSerializer.TypeCast<PhysicsVelocity>(compPtr);
            ref Snapshot snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(backupData);

			comp.Linear = snapshot.Linear;
			comp.Angular = snapshot.Angular;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.SerializeDelegate))]
        static void Serialize(IntPtr dataPtr, ref DataStreamWriter writer,
            ref NetworkCompressionModel compressionModel)
        {
            ref Snapshot snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(dataPtr);
			writer.WritePackedFloat3(snapshot.Linear, compressionModel);
			writer.WritePackedFloat3(snapshot.Angular, compressionModel);
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.DeserializeDelegate))]
        static void Deserialize(IntPtr dataPtr, ref DataStreamReader reader,
            ref NetworkCompressionModel compressionModel)
        {
            ref Snapshot snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(dataPtr);
			snapshot.Linear = reader.ReadPackedFloat3(compressionModel);
			snapshot.Angular = reader.ReadPackedFloat3(compressionModel);
        }
    }
}