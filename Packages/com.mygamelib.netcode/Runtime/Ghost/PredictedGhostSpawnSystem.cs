using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Networking.Transport.Utilities;
using UnityEngine;

namespace MyGameLib.NetCode
{
    public struct PredictedGhostSpawnList : IComponentData
    {
    }

    public struct PredictedGhostSpawn : IBufferElementData
    {
        public Entity Entity;
        public int GhostType;
        public uint SpawnTick;
    }

    [ClientWorld]
    [UpdateInGroup(typeof(GhostSpawnSystemGroup))]
    [UpdateAfter(typeof(GhostSpawnSystem))]
    public class PredictedGhostSpawnSystem : ComponentSystem
    {
        private EntityQuery _ghostInitQuery;
        private EntityQuery _ghostQuery;

        private BeginSimulationEntityCommandBufferSystem _barrier;
        private uint _spawnTick;
        private NetworkTimeSystem _networkTimeSystem;
        private GhostCollectionSystem _ghostCollectionSystem;
        private GhostPredictionSystemGroup _ghostPredictionSystemGroup;


        protected override void OnCreate()
        {
            var ent = EntityManager.CreateEntity();
            EntityManager.AddComponentData(ent, default(PredictedGhostSpawnList));
            EntityManager.AddBuffer<PredictedGhostSpawn>(ent);

            RequireSingletonForUpdate<PredictedGhostSpawnList>();

            _ghostInitQuery = GetEntityQuery(ComponentType.ReadOnly<PredictedGhostSpawnRequestComponent>(),
                ComponentType.ReadWrite<GhostComponent>());
            _ghostQuery = GetEntityQuery(ComponentType.ReadWrite<GhostComponent>(),
                ComponentType.ReadOnly<PredictedGhostSpawnPendingComponent>());

            _barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            _networkTimeSystem = World.GetOrCreateSystem<NetworkTimeSystem>();
            _ghostCollectionSystem = World.GetOrCreateSystem<GhostCollectionSystem>();
            _ghostPredictionSystemGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();
        }

        protected override void OnUpdate()
        {
            var ent = GetSingletonEntity<PredictedGhostSpawnList>();
            var spawnList = EntityManager.GetBuffer<PredictedGhostSpawn>(ent);
            var commandBuffer = _barrier.CreateCommandBuffer();

            if (!_ghostInitQuery.IsEmptyIgnoreFilter)
            {
                var ghostChunks = _ghostInitQuery.CreateArchetypeChunkArray(Allocator.TempJob);

                var initGhostJob = new InitGhostJob
                {
                    SpawnTick = _spawnTick,
                    GhostChunks = ghostChunks,
                    EntityTypeHandle = GetEntityTypeHandle(),
                    GhostTypeCollection = _ghostCollectionSystem.GhostTypeCollection,
                    GhostTypeHandle = GetComponentTypeHandle<GhostComponent>(),
                    SnapshotDataTypeHandle = GetComponentTypeHandle<SnapshotData>(),
                    SnapshotDataBufferTypeHandle = GetBufferTypeHandle<SnapshotDataBuffer>(),
                    CommandBuffer = commandBuffer,
                    SpawnListEntity = ent,
                    SpawnList = GetBufferFromEntity<PredictedGhostSpawn>()
                };

                initGhostJob.Schedule().Complete();
                ghostChunks.Dispose();
            }

            // 本地复制到快照中
            if (!_ghostQuery.IsEmptyIgnoreFilter && _ghostPredictionSystemGroup.PredictingTick % 5 == 0)
            {
                var ghostChunks = _ghostQuery.CreateArchetypeChunkArray(Allocator.TempJob);

                var localCopyToSnapshotJob = new LocalCopyToSnapshotJob
                {
                    Tick = _spawnTick,
                    GhostChunks = ghostChunks,
                    EntityTypeHandle = GetEntityTypeHandle(),
                    GhostTypeCollection = _ghostCollectionSystem.GhostTypeCollection,
                    GhostTypeHandle = GetComponentTypeHandle<GhostComponent>(),
                    SnapshotDataTypeHandle = GetComponentTypeHandle<SnapshotData>(),
                    SnapshotDataBufferTypeHandle = GetBufferTypeHandle<SnapshotDataBuffer>(),
                    GhostComponentIndex = _ghostCollectionSystem.IndexCollection,
                    GhostComponentSerializers = _ghostCollectionSystem.Serializers
                };

                var listLength = _ghostCollectionSystem.Serializers.Length;
                if (listLength <= 32)
                {
                    var dynamicListJob = new LocalCopyToSnapshotJob32 {Job = localCopyToSnapshotJob};
                    DynamicTypeList.PopulateList(this, _ghostCollectionSystem.Serializers, true,
                        ref dynamicListJob.List);
                    dynamicListJob.Schedule().Complete();
                }

                ghostChunks.Dispose();
            }

            // 验证预测生成的Ghost列表中的所有Ghost，并销毁那些过旧的Ghost
            uint interpolatedTick = World.GetExistingSystem<NetworkTimeSystem>().interpolateTargetTick;
            for (int i = 0; i < spawnList.Length; i++)
            {
                var ghost = spawnList[i];
                if (SequenceHelpers.IsNewer(interpolatedTick, ghost.SpawnTick))
                {
                    commandBuffer.DestroyEntity(ghost.Entity);
                    spawnList[i] = spawnList[spawnList.Length - 1];
                    spawnList.RemoveAt(spawnList.Length - 1);
                    --i;
                }
            }

            _spawnTick = _ghostPredictionSystemGroup.PredictingTick;
        }

        /// <summary>
        /// 给Ghost初始化第一份快照
        /// </summary>
        [BurstCompile]
        unsafe struct InitGhostJob : IJob
        {
            [ReadOnly] public NativeArray<ArchetypeChunk> GhostChunks;
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;
            [ReadOnly] public ComponentTypeHandle<GhostComponent> GhostTypeHandle;
            public BufferTypeHandle<SnapshotDataBuffer> SnapshotDataBufferTypeHandle;
            public ComponentTypeHandle<SnapshotData> SnapshotDataTypeHandle;

            [ReadOnly] public NativeList<GhostCollectionSystem.GhostTypeState> GhostTypeCollection;

            public uint SpawnTick;

            public EntityCommandBuffer CommandBuffer;
            public Entity SpawnListEntity;
            public BufferFromEntity<PredictedGhostSpawn> SpawnList;

            public void Execute()
            {
                for (int i = 0; i < GhostChunks.Length; i++)
                {
                    var chunk = GhostChunks[i];
                    var entities = chunk.GetNativeArray(EntityTypeHandle);
                    var ghosts = chunk.GetNativeArray(GhostTypeHandle);
                    var snapshots = chunk.GetNativeArray(SnapshotDataTypeHandle);
                    var snapshotBuffers = chunk.GetBufferAccessor(SnapshotDataBufferTypeHandle);

                    for (int ent = 0; ent < entities.Length; ent++)
                    {
                        var ghost = ghosts[ent];
                        var snapshot = snapshots[ent];
                        var snapshotBuffer = snapshotBuffers[ent];

                        var typeState = GhostTypeCollection[ghost.GhostType];
                        snapshot.LatestIndex = 0;
                        snapshot.SnapshotSize = typeState.SnapshotSize;
                        snapshots[ent] = snapshot;

                        // 重新分配大小，一个快照就行（用作初始化）。因为在客户端预测生成物体，与快照连接时会重新分配大小.
                        snapshotBuffer.ResizeUninitialized(typeState.SnapshotSize *
                                                           GlobalConstants.SnapshotHistorySize);

                        CommandBuffer.RemoveComponent<PredictedGhostSpawnRequestComponent>(entities[ent]);
                        SpawnList[SpawnListEntity].Add(new PredictedGhostSpawn
                        {
                            Entity = entities[ent],
                            GhostType = ghost.GhostType,
                            SpawnTick = SpawnTick
                        });
                    }
                }
            }
        }

        unsafe struct LocalCopyToSnapshotJob32 : IJob
        {
            public DynamicTypeList32 List;
            public LocalCopyToSnapshotJob Job;

            public void Execute()
            {
                Job.Execute(List.GetData(), List.Length);
            }
        }

        [BurstCompile]
        unsafe struct LocalCopyToSnapshotJob
        {
            [ReadOnly] public NativeArray<ArchetypeChunk> GhostChunks;
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;
            [ReadOnly] public ComponentTypeHandle<GhostComponent> GhostTypeHandle;
            public BufferTypeHandle<SnapshotDataBuffer> SnapshotDataBufferTypeHandle;
            public ComponentTypeHandle<SnapshotData> SnapshotDataTypeHandle;

            [ReadOnly] public NativeList<GhostCollectionSystem.GhostTypeState> GhostTypeCollection;
            [ReadOnly] public NativeList<GhostCollectionSystem.GhostComponentIndex> GhostComponentIndex;
            [ReadOnly] public NativeArray<GhostComponentSerializer> GhostComponentSerializers;

            public uint Tick;

            public void Execute(DynamicComponentTypeHandle* ghostChunkComponentTypesPtr,
                int ghostChunkComponentTypesLength)
            {
                for (int i = 0; i < GhostChunks.Length; i++)
                {
                    var chunk = GhostChunks[i];
                    var entities = chunk.GetNativeArray(EntityTypeHandle);
                    var ghosts = chunk.GetNativeArray(GhostTypeHandle);
                    var snapshots = chunk.GetNativeArray(SnapshotDataTypeHandle);
                    var snapshotBuffers = chunk.GetBufferAccessor(SnapshotDataBufferTypeHandle);

                    for (int ent = 0; ent < entities.Length; ent++)
                    {
                        var ghost = ghosts[ent];
                        var snapshot = snapshots[ent];
                        var snapshotBuffer = snapshotBuffers[ent];

                        var typeState = GhostTypeCollection[ghost.GhostType];
                        snapshot.LatestIndex = (snapshot.LatestIndex + 1) % GlobalConstants.SnapshotHistorySize;
                        snapshots[ent] = snapshot;

                        byte* snapshotData = (byte*) snapshotBuffer.GetUnsafePtr() +
                                             snapshot.SnapshotSize * snapshot.LatestIndex;

                        // 写入Tick
                        *(uint*) snapshotData = Tick;
                        snapshotData += GlobalConstants.TickSize;

                        int baseOffset = typeState.FirstComponent;
                        int numBaseComponents = typeState.NumComponents;

                        for (int j = 0; j < numBaseComponents; j++)
                        {
                            var compIdx = GhostComponentIndex[baseOffset + j].ComponentIndex;
                            var serializer = GhostComponentSerializers[compIdx];
                            var dynamicType = ghostChunkComponentTypesPtr[compIdx];

                            if (chunk.Has(dynamicType))
                            {
                                var compPrt = (IntPtr)
                                    chunk.GetDynamicComponentDataArrayReinterpret<byte>(dynamicType,
                                        serializer.ComponentSize).GetUnsafePtr();

                                serializer.CopyToSnapshot.Ptr.Invoke(compPrt + serializer.ComponentSize * ent,
                                    (IntPtr) snapshotData);
                            }

                            snapshotData += serializer.DataSize;
                        }
                    }
                }
            }
        }
    }
}