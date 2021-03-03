using System;
using MyGameLib.NetCode;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using AOT;

using Samples.NetFPS;

namespace Assembly_CSharp.Generated
{
    [BurstCompile]
    public struct NetworkCharacterComponentSerializer
    {
        public static GhostComponentSerializer Serializer;

        static NetworkCharacterComponentSerializer()
        {
            Serializer = new GhostComponentSerializer
            {
                IsUpdateValue = false,
                SendType = GhostSendType.All,
                ComponentType = ComponentType.ReadWrite<NetworkCharacterComponent>(),
                ComponentSize = UnsafeUtility.SizeOf<NetworkCharacterComponent>(),
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
			public float3 Position;
			public quaternion Rotation;
			public float3 BaseVelocity;
			public Boolean MustUnground;
			public float MustUngroundTime;
			public Boolean LastMovementIterationFoundAnyGround;
			public Boolean FoundAnyGround;
			public Boolean IsStableOnGround;
			public Boolean SnappingPrevented;
			public float3 GroundNormal;
			public float3 InnerGroundNormal;
			public float3 OuterGroundNormal;
			public float Pitch;
			public float AngleH;
			public float AngleV;
			public float AimH;
			public float AimV;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyToSnapshotDelegate))]
        static void CopyToSnapshot(IntPtr compPtr, IntPtr dataPtr)
        {
            ref NetworkCharacterComponent comp = ref GhostComponentSerializer.TypeCast<NetworkCharacterComponent>(compPtr);
            ref Snapshot snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(dataPtr);

			snapshot.Position = comp.Position;
			snapshot.Rotation = comp.Rotation;
			snapshot.BaseVelocity = comp.BaseVelocity;
			snapshot.MustUnground = comp.MustUnground;
			snapshot.MustUngroundTime = comp.MustUngroundTime;
			snapshot.LastMovementIterationFoundAnyGround = comp.LastMovementIterationFoundAnyGround;
			snapshot.FoundAnyGround = comp.FoundAnyGround;
			snapshot.IsStableOnGround = comp.IsStableOnGround;
			snapshot.SnappingPrevented = comp.SnappingPrevented;
			snapshot.GroundNormal = comp.GroundNormal;
			snapshot.InnerGroundNormal = comp.InnerGroundNormal;
			snapshot.OuterGroundNormal = comp.OuterGroundNormal;
			snapshot.Pitch = comp.Pitch;
			snapshot.AngleH = comp.AngleH;
			snapshot.AngleV = comp.AngleV;
			snapshot.AimH = comp.AimH;
			snapshot.AimV = comp.AimV;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyFromSnapshotDelegate))]
        static void CopyFromSnapshot(IntPtr compPtr, IntPtr dataPtr, int offset)
        {
            ref NetworkCharacterComponent comp = ref GhostComponentSerializer.TypeCast<NetworkCharacterComponent>(compPtr);
            ref SnapshotData.DataAtTick dataAtTick =
                ref GhostComponentSerializer.TypeCast<SnapshotData.DataAtTick>(dataPtr);

            // 不管需不需要插值，这个数据都是需要的。
            ref Snapshot after = ref GhostComponentSerializer.TypeCast<Snapshot>(dataAtTick.SnapshotAfter, offset);

			ref Snapshot before = ref GhostComponentSerializer.TypeCast<Snapshot>(dataAtTick.SnapshotBefore, offset);
			comp.Position = math.lerp(before.Position, after.Position, dataAtTick.InterpolationFactor);
			comp.Rotation = math.slerp(before.Rotation, after.Rotation, dataAtTick.InterpolationFactor);
			comp.BaseVelocity = after.BaseVelocity;
			comp.MustUnground = after.MustUnground;
			comp.MustUngroundTime = after.MustUngroundTime;
			comp.LastMovementIterationFoundAnyGround = after.LastMovementIterationFoundAnyGround;
			comp.FoundAnyGround = after.FoundAnyGround;
			comp.IsStableOnGround = after.IsStableOnGround;
			comp.SnappingPrevented = after.SnappingPrevented;
			comp.GroundNormal = after.GroundNormal;
			comp.InnerGroundNormal = after.InnerGroundNormal;
			comp.OuterGroundNormal = after.OuterGroundNormal;
			comp.Pitch = after.Pitch;
			comp.AngleH = math.lerp(before.AngleH, after.AngleH, dataAtTick.InterpolationFactor);
			comp.AngleV = math.lerp(before.AngleV, after.AngleV, dataAtTick.InterpolationFactor);
			comp.AimH = math.lerp(before.AimH, after.AimH, dataAtTick.InterpolationFactor);
			comp.AimV = math.lerp(before.AimV, after.AimV, dataAtTick.InterpolationFactor);
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.RestoreFromBackupDelegate))]
        static void RestoreFromBackup(IntPtr compPtr, IntPtr backupData)
        {
            ref NetworkCharacterComponent comp = ref GhostComponentSerializer.TypeCast<NetworkCharacterComponent>(compPtr);
            ref Snapshot snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(backupData);

			comp.Position = snapshot.Position;
			comp.Rotation = snapshot.Rotation;
			comp.BaseVelocity = snapshot.BaseVelocity;
			comp.MustUnground = snapshot.MustUnground;
			comp.MustUngroundTime = snapshot.MustUngroundTime;
			comp.LastMovementIterationFoundAnyGround = snapshot.LastMovementIterationFoundAnyGround;
			comp.FoundAnyGround = snapshot.FoundAnyGround;
			comp.IsStableOnGround = snapshot.IsStableOnGround;
			comp.SnappingPrevented = snapshot.SnappingPrevented;
			comp.GroundNormal = snapshot.GroundNormal;
			comp.InnerGroundNormal = snapshot.InnerGroundNormal;
			comp.OuterGroundNormal = snapshot.OuterGroundNormal;
			comp.Pitch = snapshot.Pitch;
			comp.AngleH = snapshot.AngleH;
			comp.AngleV = snapshot.AngleV;
			comp.AimH = snapshot.AimH;
			comp.AimV = snapshot.AimV;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.SerializeDelegate))]
        static void Serialize(IntPtr dataPtr, ref DataStreamWriter writer,
            ref NetworkCompressionModel compressionModel)
        {
            ref Snapshot snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(dataPtr);
			writer.WritePackedFloat3(snapshot.Position, compressionModel);
			writer.WritePackedQuaternion(snapshot.Rotation, compressionModel);
			writer.WritePackedFloat3(snapshot.BaseVelocity, compressionModel);
			writer.WritePackedBoolean(snapshot.MustUnground, compressionModel);
			writer.WritePackedFloat(snapshot.MustUngroundTime, compressionModel);
			writer.WritePackedBoolean(snapshot.LastMovementIterationFoundAnyGround, compressionModel);
			writer.WritePackedBoolean(snapshot.FoundAnyGround, compressionModel);
			writer.WritePackedBoolean(snapshot.IsStableOnGround, compressionModel);
			writer.WritePackedBoolean(snapshot.SnappingPrevented, compressionModel);
			writer.WritePackedFloat3(snapshot.GroundNormal, compressionModel);
			writer.WritePackedFloat3(snapshot.InnerGroundNormal, compressionModel);
			writer.WritePackedFloat3(snapshot.OuterGroundNormal, compressionModel);
			writer.WritePackedFloat(snapshot.Pitch, compressionModel);
			writer.WritePackedFloat(snapshot.AngleH, compressionModel);
			writer.WritePackedFloat(snapshot.AngleV, compressionModel);
			writer.WritePackedFloat(snapshot.AimH, compressionModel);
			writer.WritePackedFloat(snapshot.AimV, compressionModel);
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.DeserializeDelegate))]
        static void Deserialize(IntPtr dataPtr, ref DataStreamReader reader,
            ref NetworkCompressionModel compressionModel)
        {
            ref Snapshot snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(dataPtr);
			snapshot.Position = reader.ReadPackedFloat3(compressionModel);
			snapshot.Rotation = reader.ReadPackedQuaternion(compressionModel);
			snapshot.BaseVelocity = reader.ReadPackedFloat3(compressionModel);
			snapshot.MustUnground = reader.ReadPackedBoolean(compressionModel);
			snapshot.MustUngroundTime = reader.ReadPackedFloat(compressionModel);
			snapshot.LastMovementIterationFoundAnyGround = reader.ReadPackedBoolean(compressionModel);
			snapshot.FoundAnyGround = reader.ReadPackedBoolean(compressionModel);
			snapshot.IsStableOnGround = reader.ReadPackedBoolean(compressionModel);
			snapshot.SnappingPrevented = reader.ReadPackedBoolean(compressionModel);
			snapshot.GroundNormal = reader.ReadPackedFloat3(compressionModel);
			snapshot.InnerGroundNormal = reader.ReadPackedFloat3(compressionModel);
			snapshot.OuterGroundNormal = reader.ReadPackedFloat3(compressionModel);
			snapshot.Pitch = reader.ReadPackedFloat(compressionModel);
			snapshot.AngleH = reader.ReadPackedFloat(compressionModel);
			snapshot.AngleV = reader.ReadPackedFloat(compressionModel);
			snapshot.AimH = reader.ReadPackedFloat(compressionModel);
			snapshot.AimV = reader.ReadPackedFloat(compressionModel);
        }
    }
}