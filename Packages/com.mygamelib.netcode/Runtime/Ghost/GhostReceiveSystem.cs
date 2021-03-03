using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Networking.Transport;
using UnityEngine;

namespace MyGameLib.NetCode
{
    public struct SpawnedGhost : IEquatable<SpawnedGhost>
    {
        public int GhostId;
        public uint SpawnTick;

        public override int GetHashCode()
        {
            return GhostId;
        }

        public bool Equals(SpawnedGhost other)
        {
            return other.GhostId == GhostId && other.SpawnTick == SpawnTick;
        }
    }

    internal struct SpawnedGhostMapping
    {
        public SpawnedGhost Ghost;
        public Entity Entity;
        public Entity PreviousEntity;
    }

    internal struct NonSpawnedGhostMapping
    {
        public int GhostId;
        public Entity Entity;
    }

    /// <summary>
    /// 幽灵接收系统
    /// </summary>
    [UpdateAfter(typeof(NetworkReceiveSystemGroup))]
    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    public class GhostReceiveSystem : ComponentSystem
    {
        private EntityQuery ghostRecvGroup;
        private BeginSimulationEntityCommandBufferSystem _barrier;
        private NetworkCompressionModel _networkCompressionModel;
        private GhostDespawnSystem _ghostDespawnSystem;

        public NativeHashMap<SpawnedGhost, Entity> SpawnedGhostEntityMap => _spawnedGhostEntityMap;
        private NativeHashMap<SpawnedGhost, Entity> _spawnedGhostEntityMap;
        private NativeHashMap<int, Entity> _ghostEntityMap;

        protected override void OnCreate()
        {
            _barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            _networkCompressionModel = new NetworkCompressionModel(Allocator.Persistent);
            _ghostEntityMap = new NativeHashMap<int, Entity>(2048, Allocator.Persistent);
            _spawnedGhostEntityMap = new NativeHashMap<SpawnedGhost, Entity>(2048, Allocator.Persistent);
            _ghostDespawnSystem = World.GetOrCreateSystem<GhostDespawnSystem>();

            ghostRecvGroup = GetEntityQuery(ComponentType.ReadOnly<NetworkStreamInGame>(),
                ComponentType.ReadOnly<IncomingSnapshotDataStreamBufferComponent>(),
                ComponentType.ReadOnly<CommandTargetComponent>(),
                ComponentType.Exclude<NetworkStreamDisconnected>());

            RequireForUpdate(ghostRecvGroup);
            RequireSingletonForUpdate<GhostPrefabCollectionComponent>();
        }

        protected override void OnDestroy()
        {
            _networkCompressionModel.Dispose();
            _spawnedGhostEntityMap.Dispose();
            _ghostEntityMap.Dispose();
        }

        internal void AddNonSpawnedGhosts(NativeArray<NonSpawnedGhostMapping> ghosts)
        {
            for (int i = 0; i < ghosts.Length; i++)
            {
                var ghostId = ghosts[i].GhostId;
                var ent = ghosts[i].Entity;
                if (!_ghostEntityMap.TryAdd(ghostId, ent))
                {
                    _ghostEntityMap.Remove(ghostId);
                    _ghostEntityMap.TryAdd(ghostId, ent);
                }
            }
        }

        internal void AddSpawnedGhosts(NativeArray<SpawnedGhostMapping> ghosts)
        {
            for (int i = 0; i < ghosts.Length; i++)
            {
                var ghost = ghosts[i].Ghost;
                var ent = ghosts[i].Entity;
                if (!_ghostEntityMap.TryAdd(ghost.GhostId, ent))
                {
                    _ghostEntityMap.Remove(ghost.GhostId);
                    _ghostEntityMap.TryAdd(ghost.GhostId, ent);
                }

                if (_spawnedGhostEntityMap.TryAdd(ghost, ent))
                {
                    _spawnedGhostEntityMap.Remove(ghost);
                    _spawnedGhostEntityMap.TryAdd(ghost, ent);
                }
            }
        }

        internal void UpdateSpawnedGhosts(NativeArray<SpawnedGhostMapping> ghosts)
        {
            for (int i = 0; i < ghosts.Length; i++)
            {
                var ghost = ghosts[i].Ghost;
                var ent = ghosts[i].Entity;
                var prevEnt = ghosts[i].PreviousEntity;
                if (_ghostEntityMap.TryGetValue(ghost.GhostId, out var existing) && existing == prevEnt)
                {
                    _ghostEntityMap.Remove(ghost.GhostId);
                    _ghostEntityMap.TryAdd(ghost.GhostId, ent);
                }

                if (!_spawnedGhostEntityMap.TryAdd(ghost, ent))
                {
                    _spawnedGhostEntityMap.Remove(ghost);
                    _spawnedGhostEntityMap.TryAdd(ghost, ent);
                }
            }
        }

        protected override unsafe void OnUpdate()
        {
            var session = GetSingletonEntity<CommandTargetComponent>();
            var inBuffer = EntityManager.GetBuffer<IncomingSnapshotDataStreamBufferComponent>(session);

            var reader = inBuffer.AsDataStreamReader();
            if (reader.Length == 0) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NetDebug.SnapMS = reader.Length;
            NetDebug.DownCount += reader.Length;
#endif

            uint serverTick = reader.ReadUInt();

            var ack = EntityManager.GetComponentData<NetworkSnapshotAckComponent>(session);
            if (ack.IsOldWithLastReceivedSnapshotByLocal(serverTick))
                return;
            ack.UpdateLocalValues(serverTick);
            PostUpdateCommands.SetComponent(session, ack);

            uint destroyLen = reader.ReadUInt();
            uint len = reader.ReadUInt();

            for (int i = 0; i < destroyLen; i++)
            {
                int ghostId = reader.ReadPackedInt(_networkCompressionModel);
                if (!_ghostEntityMap.TryGetValue(ghostId, out Entity ent))
                    continue;

                var s = new GhostDespawnSystem.DelayedDespawnGhost
                {
                    Ghost = new SpawnedGhost
                    {
                        GhostId = ghostId,
                        SpawnTick = EntityManager.GetComponentData<GhostComponent>(ent).SpawnTick
                    },
                    Tick = serverTick
                };

                _ghostEntityMap.Remove(ghostId);
                if (EntityManager.HasComponent<GhostPredictionComponent>(ent))
                {
                    _ghostDespawnSystem.AddToPredicted(s);
                }
                else
                {
                    _ghostDespawnSystem.AddToInterpolated(s);
                }
            }

            var ghostSerializerCollectionSystem = World.GetExistingSystem<GhostCollectionSystem>();

            var ghostSpawnEntity = GetSingletonEntity<GhostSpawnQueueComponent>();
            var bufferFromEntity = GetBufferFromEntity<SnapshotDataBuffer>();
            var spawnBufferFromEntity = GetBufferFromEntity<GhostSpawnBuffer>();
            var compFromEntity = GetComponentDataFromEntity<SnapshotData>();

            for (int i = 0; i < len; i++)
            {
                int ghostType = reader.ReadPackedInt(_networkCompressionModel);
                int ghostId = reader.ReadPackedInt(_networkCompressionModel);

                if (ghostType < 0 || ghostType >= ghostSerializerCollectionSystem.GhostTypeCollection.Length)
                {
                    throw new Exception($"GhostRecvSystem:GhostType={ghostType}, GhostId={ghostId}");
                }

                // 序列化组件信息
                var typeState = ghostSerializerCollectionSystem.GhostTypeCollection[ghostType];
                var baseOffset = typeState.FirstComponent;
                var numBaseComponents = typeState.NumComponents;

                byte* snapshotData;
                DynamicBuffer<SnapshotDataBuffer> snapshotDataBuffer;
                SnapshotData snapshotDataComponent;

                bool existingGhost = _ghostEntityMap.TryGetValue(ghostId, out Entity gent);

                if (existingGhost && bufferFromEntity.HasComponent(gent) &&
                    compFromEntity.HasComponent(gent))
                {
                    snapshotDataBuffer = bufferFromEntity[gent];
                    snapshotData = (byte*) snapshotDataBuffer.GetUnsafePtr();
                    snapshotDataComponent = compFromEntity[gent];
                    snapshotDataComponent.LatestIndex =
                        (snapshotDataComponent.LatestIndex + 1) % GlobalConstants.SnapshotHistorySize;
                    compFromEntity[gent] = snapshotDataComponent;
                }
                else
                {
                    var ghostSpawnBuffer = spawnBufferFromEntity[ghostSpawnEntity];
                    snapshotDataBuffer = bufferFromEntity[ghostSpawnEntity];
                    var snapshotDataBufferOffset = snapshotDataBuffer.Length;
                    ghostSpawnBuffer.Add(new GhostSpawnBuffer
                    {
                        GhostType = ghostType,
                        GhostId = ghostId,
                        ClientSpawnTick = serverTick,
                        ServerSpawnTick = serverTick,
                        DataOffset = snapshotDataBufferOffset
                    });

                    snapshotDataBuffer.ResizeUninitialized(snapshotDataBufferOffset + typeState.SnapshotSize);
                    snapshotData = (byte*) snapshotDataBuffer.GetUnsafePtr() + snapshotDataBufferOffset;
                    UnsafeUtility.MemClear(snapshotData, typeState.SnapshotSize);
                    snapshotDataComponent = new SnapshotData
                    {
                        SnapshotSize = typeState.SnapshotSize,
                        LatestIndex = 0
                    };
                }

                // 把快照放到对应内存中
                snapshotData += typeState.SnapshotSize * snapshotDataComponent.LatestIndex;
                *((uint*) snapshotData) = serverTick;
                snapshotData += GlobalConstants.TickSize;

                // 放到快照对应内存位置
                for (int j = 0; j < numBaseComponents; j++)
                {
                    var compIdx = ghostSerializerCollectionSystem.IndexCollection[baseOffset + j].ComponentIndex;
                    var serializer =
                        ghostSerializerCollectionSystem.Serializers[compIdx];

                    serializer.Deserialize.Ptr.Invoke((IntPtr) snapshotData, ref reader, ref _networkCompressionModel);
                    snapshotData += serializer.DataSize;
                }
            }


#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NetDebug.Set(nameof(ack.LastReceivedSnapshotByLocal), ack.LastReceivedSnapshotByLocal);
            NetDebug.Set(nameof(ack.EstimatedRTT), ack.EstimatedRTT);
            NetDebug.Set(nameof(ack.DeviationRTT), ack.DeviationRTT);
            NetDebug.RTT = (uint) ack.EstimatedRTT;
            NetDebug.Jitter = (uint) ack.DeviationRTT;
#endif

            inBuffer.Clear();
        }
    }
}