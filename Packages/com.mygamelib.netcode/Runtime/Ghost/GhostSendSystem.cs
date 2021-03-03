using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Networking.Transport;
using UnityEngine;

namespace MyGameLib.NetCode
{
    /// <summary>
    /// 连接状态数据
    /// 主要记录序列化相关的数据
    /// 1.遍历每个连接，检测是否有数据要发送
    /// 2.按重要性排序块
    /// 3.发送排序后的块，如果大小超了。就及时跳出，并记录当前块发送位置，等待下一帧来接着发送。
    /// 4.每个块发送完成后，重置Age与发送位置下标。
    /// </summary>
    public struct ConnectionStateData : IDisposable
    {
        public UnsafeHashMap<ArchetypeChunk, SerializationState> State;

        public int ChunkIndex;
        public int Age;

        public void Dispose()
        {
            State.Dispose();
        }
    }

    public struct SerializationState
    {
        public int Importance;
        public int StartIndex;
    }

    [ServerWorld]
    [UpdateAfter(typeof(TickSimulationSystemGroup))]
    public class GhostSendSystem : ComponentSystem
    {
        private NetworkDriver _driver;
        private NetworkCompressionModel _compressionModel;
        private NetworkPipeline _unreliablePipeline;

        private GhostPredictionSystemGroup _ghostPredictionSystemGroup;

        // ghost生成
        private EntityQuery ghostSpawnGroup;
        private EntityQuery ghostDespawnGroup;
        private EntityQuery ghostGroup;
        private EntityQuery connectionGroup;

        private NativeHashMap<int, Entity> _newGhosts;

        // 连接状态
        private UnsafeHashMap<Entity, ConnectionStateData> _connectionStates;

        private int _ghostInstanceId;

        protected override void OnCreate()
        {
            _connectionStates = new UnsafeHashMap<Entity, ConnectionStateData>(128, Allocator.Persistent);
            _newGhosts = new NativeHashMap<int, Entity>(16, Allocator.Persistent);

            _ghostPredictionSystemGroup =
                World.GetExistingSystem<GhostPredictionSystemGroup>();

            NetworkStreamReceiveSystem networkStreamReceiveSystem =
                World.GetExistingSystem<NetworkStreamReceiveSystem>();

            _driver = networkStreamReceiveSystem.Driver;
            _compressionModel = new NetworkCompressionModel(Allocator.Persistent);
            _unreliablePipeline = networkStreamReceiveSystem.UnreliablePipeline;

            EntityQueryDesc filterSpawn = new EntityQueryDesc
            {
                All = new ComponentType[] {typeof(GhostComponent)},
                None = new ComponentType[] {typeof(GhostSystemStateComponent)}
            };

            EntityQueryDesc filterDespawn = new EntityQueryDesc
            {
                All = new ComponentType[] {typeof(GhostSystemStateComponent)},
                None = new ComponentType[] {typeof(GhostComponent)}
            };

            ghostSpawnGroup = GetEntityQuery(filterSpawn);
            ghostDespawnGroup = GetEntityQuery(filterDespawn);
            ghostGroup = GetEntityQuery(ComponentType.ReadOnly<GhostComponent>(),
                ComponentType.ReadOnly<GhostSystemStateComponent>());

            connectionGroup = GetEntityQuery(ComponentType.ReadWrite<NetworkStreamConnection>(),
                ComponentType.ReadOnly<NetworkStreamInGame>(),
                ComponentType.Exclude<NetworkStreamDisconnected>());
        }

        protected override void OnDestroy()
        {
            _newGhosts.Dispose();
            _compressionModel.Dispose();

            var values = _connectionStates.GetValueArray(Allocator.Temp);
            for (int i = 0; i < values.Length; i++)
            {
                values[i].Dispose();
            }

            values.Dispose();
            _connectionStates.Dispose();
        }

        protected override void OnUpdate()
        {
            uint tick = _ghostPredictionSystemGroup.PredictingTick;

            // 新添加的ghost
            Entities.With(ghostSpawnGroup).ForEach((Entity ent, ref GhostComponent ghostComponent) =>
            {
                ++_ghostInstanceId;
                ghostComponent.Id = _ghostInstanceId;
                PostUpdateCommands.AddComponent(ent, new GhostSystemStateComponent {ghostId = ghostComponent.Id});
                _newGhosts.Add(ghostComponent.Id, ent);
            });

            ClientServerTickRate tickRate = default;
            if (HasSingleton<ClientServerTickRate>())
            {
                tickRate = GetSingleton<ClientServerTickRate>();
            }

            tickRate.ResolveDefaults();
            int networkTickInterval = tickRate.SimulationTickRate / tickRate.NetworkTickRate;
            if (tick % networkTickInterval != 0)
            {
                return;
            }

            var localTime = NetworkTimeSystem.TimestampMS;

            // 需要发送快照的ghost chunk
            // 已经按照原型分块了
            NativeArray<ArchetypeChunk> ghostChunks =
                ghostGroup.CreateArchetypeChunkArrayAsync(Allocator.TempJob, out JobHandle ghostChunksHandle);
            NativeArray<ArchetypeChunk> despawnChunks =
                ghostDespawnGroup.CreateArchetypeChunkArrayAsync(Allocator.TempJob, out JobHandle despawnChunksHandle);

            JobHandle.CompleteAll(ref ghostChunksHandle, ref despawnChunksHandle);

            var connections = connectionGroup.ToEntityArray(Allocator.TempJob);

            // 获取动态组件类型
            var ghostSerializerCollectionSystem = World.GetExistingSystem<GhostCollectionSystem>();

            for (int i = 0; i < connections.Length; i++)
            {
                SerializeJob serializeJob = new SerializeJob
                {
                    ConnectionEntity = connections[i],
                    ConnectionFromEntity = GetComponentDataFromEntity<NetworkStreamConnection>(),
                    AckFromEntity = GetComponentDataFromEntity<NetworkSnapshotAckComponent>(),
                    LocalTime = localTime,
                    Tick = tick,
                    NetDriver = _driver.ToConcurrent(),
                    CompressionModel = _compressionModel,
                    UnreliablePipeline = _unreliablePipeline,
                    GhostChunks = ghostChunks,
                    DespawnChunks = despawnChunks,
                    EntityTypeHandle = GetEntityTypeHandle(),
                    GhostTypeHandle = GetComponentTypeHandle<GhostComponent>(),
                    GhostSystemStateTypeHandle = GetComponentTypeHandle<GhostSystemStateComponent>(),
                    GhostTypeCollection = ghostSerializerCollectionSystem.GhostTypeCollection,
                    GhostComponentIndex = ghostSerializerCollectionSystem.IndexCollection,
                    GhostComponentSerializers = ghostSerializerCollectionSystem.Serializers,
                };

                // FIXME
                var listLength = ghostSerializerCollectionSystem.Serializers.Length;
                if (listLength <= 32)
                {
                    var dynamicListJob = new SerializeJob32 {Job = serializeJob};
                    DynamicTypeList.PopulateList(this, ghostSerializerCollectionSystem.Serializers, true,
                        ref dynamicListJob.List);

                    dynamicListJob.Schedule().Complete();
                }
            }


            // 移除的ghost
            Entities.With(ghostDespawnGroup).ForEach((Entity ent, ref GhostSystemStateComponent state) =>
            {
                _newGhosts.Remove(state.ghostId);
                PostUpdateCommands.RemoveComponent<GhostSystemStateComponent>(ent);
            });

            connections.Dispose();
            ghostChunks.Dispose();
            despawnChunks.Dispose();
        }

        [BurstCompile]
        unsafe struct SerializeJob32 : IJob
        {
            public DynamicTypeList32 List;
            public SerializeJob Job;

            public void Execute()
            {
                Job.Execute(List.GetData(), List.Length);
            }
        }

        [BurstCompile]
        struct SerializeJob
        {
            public Entity ConnectionEntity;
            [ReadOnly] public ComponentDataFromEntity<NetworkStreamConnection> ConnectionFromEntity;
            [ReadOnly] public ComponentDataFromEntity<NetworkSnapshotAckComponent> AckFromEntity;

            public uint LocalTime;
            public uint Tick;
            public NetworkDriver.Concurrent NetDriver;
            public NetworkCompressionModel CompressionModel;
            public NetworkPipeline UnreliablePipeline;

            [ReadOnly] public NativeArray<ArchetypeChunk> GhostChunks;
            [ReadOnly] public NativeArray<ArchetypeChunk> DespawnChunks;
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;
            [ReadOnly] public ComponentTypeHandle<GhostComponent> GhostTypeHandle;
            [ReadOnly] public ComponentTypeHandle<GhostSystemStateComponent> GhostSystemStateTypeHandle;

            [ReadOnly] public NativeList<GhostCollectionSystem.GhostTypeState> GhostTypeCollection;
            [ReadOnly] public NativeList<GhostCollectionSystem.GhostComponentIndex> GhostComponentIndex;
            [ReadOnly] public NativeArray<GhostComponentSerializer> GhostComponentSerializers;

            public unsafe void Execute(DynamicComponentTypeHandle* ghostChunkComponentTypesPtr,
                int ghostChunkComponentTypesLength)
            {
                var writer = NetDriver.BeginSend(UnreliablePipeline, ConnectionFromEntity[ConnectionEntity].Value,
                    GlobalConstants.TargetPacketSize);
                WriteHeader(ref writer, AckFromEntity[ConnectionEntity]);

                var lenWriter = writer;
                writer.WriteUInt(0);
                writer.WriteUInt(0);

                // 销毁的entity
                uint despawnLen = 0;
                uint updateLen = 0;

                despawnLen = SerializeDespawnEntities(ref writer, DespawnChunks);

                if (writer.HasFailedWrites)
                {
                    NetDriver.AbortSend(writer);
                    throw new InvalidOperationException("单个快照中无法包含所有需要销毁的GhostId!");
                }

                updateLen = SerializeEntities(ref writer, GhostChunks, default, ghostChunkComponentTypesPtr,
                    ghostChunkComponentTypesLength);

                if (writer.HasFailedWrites)
                {
                    NetDriver.AbortSend(writer);
                    throw new InvalidOperationException("Size limitation on snapshot did not prevent all errors");
                }

                writer.Flush();
                lenWriter.WriteUInt(despawnLen);
                lenWriter.WriteUInt(updateLen);
                NetDriver.EndSend(writer);
            }

            void WriteHeader(ref DataStreamWriter writer, in NetworkSnapshotAckComponent snapshotAck)
            {
                writer.WriteByte((byte) NetworkStreamProtocol.Snapshot);

                // 时间
                writer.WriteUInt(LocalTime);
                uint returnTime = snapshotAck.LastReceivedRemoteTime;
                if (returnTime != 0)
                    returnTime -= (LocalTime - snapshotAck.LastReceiveTimestamp);
                writer.WriteUInt(returnTime);
                writer.WriteInt(snapshotAck.ServerCommandAge);
                writer.WriteUInt(Tick);
            }

            private uint SerializeDespawnEntities(ref DataStreamWriter writer, NativeArray<ArchetypeChunk> ghostChunks)
            {
                uint despawnLen = 0;

                for (int i = 0; i < ghostChunks.Length; i++)
                {
                    var entities = ghostChunks[i].GetNativeArray(EntityTypeHandle);
                    var ghosts = ghostChunks[i].GetNativeArray(GhostSystemStateTypeHandle);
                    for (int ent = 0; ent < entities.Length; ++ent, ++despawnLen)
                    {
                        writer.WritePackedInt(ghosts[ent].ghostId, CompressionModel);
                    }
                }

                return despawnLen;
            }

            private unsafe uint SerializeEntities(ref DataStreamWriter writer,
                NativeArray<ArchetypeChunk> ghostChunks, ConnectionStateData connectionStateData,
                DynamicComponentTypeHandle* ghostChunkComponentTypesPtr,
                int ghostChunkComponentTypesLength)
            {
                uint updateLen = 0;
                for (int i = 0; i < ghostChunks.Length; i++)
                {
                    var chunk = ghostChunks[i];
                    var entities = ghostChunks[i].GetNativeArray(EntityTypeHandle);
                    var ghosts = ghostChunks[i].GetNativeArray(GhostTypeHandle);

                    // 当前Chunk中的所有Entity
                    for (int ent = 0; ent < entities.Length; ++ent, ++updateLen)
                    {
                        int ghostType = ghosts[ent].GhostType;

                        // 序列化器列表
                        var typeState = GhostTypeCollection[ghostType];

                        writer.WritePackedInt(ghostType, CompressionModel);
                        writer.WritePackedInt(ghosts[ent].Id, CompressionModel);

                        // 类型中包含了Tick大小(客户端需要)，所以在服务端需要排除掉。
                        int snapshotSize = typeState.SnapshotSize - GlobalConstants.TickSize;

                        // FIXME:应避免频繁分配内存
                        IntPtr dataPtr =
                            (IntPtr) UnsafeUtility.Malloc(snapshotSize, 4, Allocator.Temp);

                        int baseOffset = typeState.FirstComponent;
                        int numBaseComponents = typeState.NumComponents;
                        int offset = 0;
                        for (int j = 0; j < numBaseComponents; j++)
                        {
                            int compIdx = GhostComponentIndex[baseOffset + j].ComponentIndex;

                            GhostComponentSerializer serializer = GhostComponentSerializers[compIdx];
                            var dynamicType = ghostChunkComponentTypesPtr[compIdx];
                            if (chunk.Has(dynamicType))
                            {
                                IntPtr compPrt = (IntPtr)
                                    chunk.GetDynamicComponentDataArrayReinterpret<byte>(dynamicType,
                                        serializer.ComponentSize).GetUnsafePtr();

                                serializer.CopyToSnapshot.Ptr.Invoke(compPrt + serializer.ComponentSize * ent,
                                    dataPtr + offset);

                                serializer.Serialize.Ptr.Invoke(dataPtr + offset, ref writer,
                                    ref CompressionModel);
                            }

                            offset += serializer.DataSize;
                        }

                        UnsafeUtility.Free(dataPtr.ToPointer(), Allocator.Temp);
                    }
                }

                return updateLen;
            }
        }
    }
}