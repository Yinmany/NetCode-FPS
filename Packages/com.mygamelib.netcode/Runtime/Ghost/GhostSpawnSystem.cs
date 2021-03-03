using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Networking.Transport.Utilities;
using UnityEngine;

namespace MyGameLib.NetCode
{
    /// <summary>
    /// 标识此Ghost延迟生成
    /// </summary>
    public struct GhostDelaySpawnComponent : IComponentData
    {
    }

    [ClientWorld]
    [UpdateInGroup(typeof(GhostSpawnSystemGroup))]
    public class GhostSpawnSystem : SystemBase
    {
        private struct DelayedSpawnGhost
        {
            public int GhostId;
            public int GhostType;
            public uint ClientSpawnTick;
            public uint ServerSpawnTick;
            public Entity OldEntity;
        }

        private GhostCollectionSystem _ghostCollectionSystem;
        private NativeQueue<DelayedSpawnGhost> _delayedSpawnQueue;
        private GhostReceiveSystem _ghostRecvSystem;
        private NetworkTimeSystem _networkTimeSystem;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<NetworkSnapshotAckComponent>();
            RequireSingletonForUpdate<GhostPrefabCollectionComponent>();

            _ghostCollectionSystem = World.GetOrCreateSystem<GhostCollectionSystem>();
            _delayedSpawnQueue = new NativeQueue<DelayedSpawnGhost>(Allocator.Persistent);
            _ghostRecvSystem = World.GetOrCreateSystem<GhostReceiveSystem>();
            _networkTimeSystem = World.GetOrCreateSystem<NetworkTimeSystem>();
        }

        protected override void OnDestroy()
        {
            _delayedSpawnQueue.Dispose();
        }

        protected override unsafe void OnUpdate()
        {
            if (!HasSingleton<GhostPrefabCollectionComponent>() || !HasSingleton<GhostSpawnQueueComponent>())
                return;

            var collections = GetSingleton<GhostPrefabCollectionComponent>();

            var ghostSpawnEntity = GetSingletonEntity<GhostSpawnQueueComponent>();
            var ghostSpawnBufferComponent = EntityManager.GetBuffer<GhostSpawnBuffer>(ghostSpawnEntity);
            var snapshotDataBufferComponent = EntityManager.GetBuffer<SnapshotDataBuffer>(ghostSpawnEntity);
            var ghostSpawnBuffer = ghostSpawnBufferComponent.ToNativeArray(Allocator.Temp);
            var snapshotDataBuffer = snapshotDataBufferComponent.ToNativeArray(Allocator.Temp);
            ghostSpawnBufferComponent.ResizeUninitialized(0);
            snapshotDataBufferComponent.ResizeUninitialized(0);

            var spawnedGhosts = new NativeList<SpawnedGhostMapping>(16, Allocator.Temp);
            var nonSpawnedGhosts = new NativeList<NonSpawnedGhostMapping>(16, Allocator.Temp);

            for (int i = 0; i < ghostSpawnBuffer.Length; i++)
            {
                var clientPredictionPrefabs =
                    EntityManager.GetBuffer<GhostPrefabBuffer>(collections.ClientPredictedPrefabs);

                var ghost = ghostSpawnBuffer[i];
                Entity entity = Entity.Null;
                byte* snapshotData = null;

                // 包含tick
                var snapshotSize = _ghostCollectionSystem.GhostTypeCollection[ghost.GhostType].SnapshotSize;

                // Debug.Log($"SpawnGhost:{ghost.GhostId} {ghost.GhostType} {ghost.SpawnType}");
                if (ghost.SpawnType == GhostSpawnBuffer.Type.Interpolated)
                {
                    // 为了可以延迟创建
                    entity = EntityManager.CreateEntity(ComponentType.ReadOnly<GhostDelaySpawnComponent>());
                    EntityManager.AddComponentData(entity, new GhostComponent
                    {
                        Id = ghost.GhostId,
                        GhostType = ghost.GhostType,
                        SpawnTick = ghost.ServerSpawnTick
                    });
                    var newBuffer = EntityManager.AddBuffer<SnapshotDataBuffer>(entity);
                    newBuffer.ResizeUninitialized(snapshotSize * GlobalConstants.SnapshotHistorySize);
                    snapshotData = (byte*) newBuffer.GetUnsafePtr();
                    EntityManager.AddComponentData(entity,
                        new SnapshotData {SnapshotSize = snapshotSize, LatestIndex = 0});

                    _delayedSpawnQueue.Enqueue(new DelayedSpawnGhost
                    {
                        GhostId = ghost.GhostId,
                        GhostType = ghost.GhostType,
                        ClientSpawnTick = ghost.ClientSpawnTick,
                        ServerSpawnTick = ghost.ServerSpawnTick,
                        OldEntity = entity
                    });

                    nonSpawnedGhosts.Add(new NonSpawnedGhostMapping
                    {
                        GhostId = ghost.GhostId,
                        Entity = entity
                    });
                }
                else if (ghost.SpawnType == GhostSpawnBuffer.Type.Predicted)
                {
                    entity = ghost.PredictedSpawnEntity != Entity.Null
                        ? ghost.PredictedSpawnEntity
                        : EntityManager.Instantiate(clientPredictionPrefabs[ghost.GhostType].Value);
                    EntityManager.SetComponentData(entity, new GhostComponent
                    {
                        Id = ghost.GhostId,
                        GhostType = ghost.GhostType,
                        SpawnTick = ghost.ServerSpawnTick,
                    });

                    var newBuffer = EntityManager.GetBuffer<SnapshotDataBuffer>(entity);
                    newBuffer.ResizeUninitialized(snapshotSize * GlobalConstants.SnapshotHistorySize);
                    snapshotData = (byte*) newBuffer.GetUnsafePtr();
                    EntityManager.SetComponentData(entity,
                        new SnapshotData {SnapshotSize = snapshotSize, LatestIndex = 0});
                    spawnedGhosts.Add(new SpawnedGhostMapping
                    {
                        Ghost = new SpawnedGhost
                        {
                            GhostId = ghost.GhostId,
                            SpawnTick = ghost.ServerSpawnTick
                        },
                        Entity = entity
                    });
                }

                if (entity != Entity.Null)
                {
                    UnsafeUtility.MemClear(snapshotData, snapshotSize * GlobalConstants.SnapshotHistorySize);
                    UnsafeUtility.MemCpy(snapshotData,
                        (byte*) snapshotDataBuffer.GetUnsafeReadOnlyPtr() + ghost.DataOffset, snapshotSize);
                }
            }

            // 添加到接收系统中
            _ghostRecvSystem.AddNonSpawnedGhosts(nonSpawnedGhosts);
            _ghostRecvSystem.AddSpawnedGhosts(spawnedGhosts);

            spawnedGhosts.Clear();

            while (_delayedSpawnQueue.Count > 0 && SequenceHelpers.IsNewer(_networkTimeSystem.interpolateTargetTick,
                _delayedSpawnQueue.Peek().ClientSpawnTick))
            {
                var clientInterpolatedPrefabs =
                    EntityManager.GetBuffer<GhostPrefabBuffer>(collections.ClientInterpolatedPrefabs);


                var ghost = _delayedSpawnQueue.Dequeue();
                Entity entity = EntityManager.Instantiate(clientInterpolatedPrefabs[ghost.GhostType].Value);
                EntityManager.SetComponentData(entity, EntityManager.GetComponentData<SnapshotData>(ghost.OldEntity));
                var ghostCompData = EntityManager.GetComponentData<GhostComponent>(ghost.OldEntity);
                EntityManager.SetComponentData(entity, ghostCompData);
                var oldBuffer = EntityManager.GetBuffer<SnapshotDataBuffer>(ghost.OldEntity)
                    .ToNativeArray(Allocator.Temp);
                var newBuffer = EntityManager.GetBuffer<SnapshotDataBuffer>(entity);
                newBuffer.ResizeUninitialized(oldBuffer.Length);
                UnsafeUtility.MemCpy(newBuffer.GetUnsafePtr(), oldBuffer.GetUnsafeReadOnlyPtr(), oldBuffer.Length);
                EntityManager.DestroyEntity(ghost.OldEntity);
                spawnedGhosts.Add(new SpawnedGhostMapping
                {
                    Ghost = new SpawnedGhost
                    {
                        GhostId = ghost.GhostId,
                        SpawnTick = ghostCompData.SpawnTick
                    },
                    Entity = entity,
                    PreviousEntity = ghost.OldEntity
                });
            }

            _ghostRecvSystem.UpdateSpawnedGhosts(spawnedGhosts);
        }

        private unsafe bool GetOwnerId(in SpawnGhostEntity spawnGhostEntity, out int ownerId)
        {
            ownerId = 0;

            var netId = GetSingleton<NetworkIdComponent>().Value;

            var ghostSerializerCollections = World.GetExistingSystem<GhostCollectionSystem>();
            var typeState = ghostSerializerCollections.GhostTypeCollection[spawnGhostEntity.GhostType];
            var ownerOffset = typeState.PredictionOwnerOffset;

            ownerId = *(int*) (spawnGhostEntity.TmpSnapshotData + GlobalConstants.TickSize + ownerOffset);
            return netId == ownerId;
        }
    }
}