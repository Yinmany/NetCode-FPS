using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace MyGameLib.NetCode
{
    [ClientWorld]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
    [UpdateBefore(typeof(GhostPredictionSystemGroup))]
    [AlwaysUpdateSystem]
    public unsafe class GhostUpdateSystemGroup : ComponentSystemGroup
    {
        public NativeHashMap<int, GhostEntity> GhostMap { get; private set; }

        private EntityQuery _ghostQuery;

        private uint _lastRollbackTick;

        protected override void OnCreate()
        {
            base.OnCreate();
            GhostMap = new NativeHashMap<int, GhostEntity>(2048, Allocator.Persistent);

            _ghostQuery = GetEntityQuery(
                ComponentType.ReadWrite<GhostPredictionComponent>(),
                ComponentType.ReadWrite<GhostComponent>(),
                ComponentType.ReadWrite<SnapshotData>(),
                ComponentType.ReadWrite<SnapshotDataBuffer>());
        }

        protected override void OnUpdate()
        {
            NetworkSnapshotAckComponent ack = GetSingleton<NetworkSnapshotAckComponent>();

            // 有新快照数据，可以进行回滚.
            if (_lastRollbackTick < ack.LastReceivedSnapshotByLocal)
            {
                Rollback();
                _lastRollbackTick = ack.LastReceivedSnapshotByLocal;
            }

            base.OnUpdate();
        }

        private void Rollback()
        {
            // 刷新快照的值到组件上
            NativeArray<ArchetypeChunk> ghostChunks =
                _ghostQuery.CreateArchetypeChunkArrayAsync(Allocator.TempJob, out var ghostChunkHandle);
            ghostChunkHandle.Complete();

            var ghostSerializerCollection = World.GetExistingSystem<GhostCollectionSystem>();
            RollbackJob rollbackJob = new RollbackJob
            {
                GhostChunks = ghostChunks,
                EntityTypeHandle = GetEntityTypeHandle(),
                GhostComponentTypeHandle = GetComponentTypeHandle<GhostComponent>(),
                SnapshotBufferTypeHandle = GetBufferTypeHandle<SnapshotDataBuffer>(),
                SnapshotComponentTypeHandle = GetComponentTypeHandle<SnapshotData>(),
                ComponentSerializers = ghostSerializerCollection.Serializers,
                GhostTypeCollection = ghostSerializerCollection.GhostTypeCollection,
                ComponentIndex = ghostSerializerCollection.IndexCollection
            };
            var listLen = ghostSerializerCollection.Serializers.Length;
            if (listLen <= 32)
            {
                var updateJob32 = new RollbackJob32 {Job = rollbackJob};
                DynamicTypeList.PopulateList(this, ghostSerializerCollection.Serializers, false,
                    ref updateJob32.List);
                updateJob32.Schedule().Complete();
            }

            ghostChunks.Dispose();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GhostMap.Dispose();
        }

        [BurstCompile]
        struct RollbackJob32 : IJob
        {
            public DynamicTypeList32 List;
            public RollbackJob Job;

            public void Execute()
            {
                Job.Execute(List.GetData(), List.Length);
            }
        }

        [BurstCompile]
        struct RollbackJob
        {
            [ReadOnly] public NativeArray<ArchetypeChunk> GhostChunks;
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;
            [ReadOnly] public ComponentTypeHandle<GhostComponent> GhostComponentTypeHandle;
            [WriteOnly] public BufferTypeHandle<SnapshotDataBuffer> SnapshotBufferTypeHandle;
            public ComponentTypeHandle<SnapshotData> SnapshotComponentTypeHandle;

            [ReadOnly] public NativeArray<GhostComponentSerializer> ComponentSerializers;
            [ReadOnly] public NativeList<GhostCollectionSystem.GhostTypeState> GhostTypeCollection;
            [ReadOnly] public NativeList<GhostCollectionSystem.GhostComponentIndex> ComponentIndex;

            public void Execute(DynamicComponentTypeHandle* ghostChunkComponentTypesPtr,
                int ghostChunkComponentTypesLength)
            {
                for (int i = 0; i < GhostChunks.Length; i++)
                {
                    ArchetypeChunk chunk = GhostChunks[i];
                    var entities = chunk.GetNativeArray(EntityTypeHandle);
                    var buffers = chunk.GetBufferAccessor(SnapshotBufferTypeHandle);
                    var datas = chunk.GetNativeArray(SnapshotComponentTypeHandle);
                    var ghosts = chunk.GetNativeArray(GhostComponentTypeHandle);

                    for (int ent = 0; ent < entities.Length; ent++)
                    {
                        GhostComponent ghostComponent = ghosts[ent];

                        var typeState = GhostTypeCollection[ghostComponent.GhostType];

                        SnapshotData snapshotData = datas[ent];
                        DynamicBuffer<SnapshotDataBuffer> snapshotDataBuffer = buffers[ent];

                        if (snapshotData.SnapshotSize == 0)
                            continue;

                        IntPtr latestSnapshot = snapshotData.GetLatest(snapshotDataBuffer);
                        
                        int baseOffset = typeState.FirstComponent;
                        int numBaseComponents = typeState.NumComponents;
                        
                        // 跳过Tick
                        int offset = GlobalConstants.TickSize;
                        for (int j = 0; j < numBaseComponents; j++)
                        {
                            int cmpIdx = ComponentIndex[baseOffset + j].ComponentIndex;
                            var serializer = ComponentSerializers[cmpIdx];
                            
                            
                            DynamicComponentTypeHandle dynamicType =
                                ghostChunkComponentTypesPtr[cmpIdx];
                            if (chunk.Has(dynamicType))
                            {
                                IntPtr compPtr = (IntPtr) chunk
                                    .GetDynamicComponentDataArrayReinterpret<byte>(dynamicType,
                                        serializer.ComponentSize).GetUnsafePtr();

                                // 不回滚只更新值
                                if (serializer.IsUpdateValue)
                                {
                                    var dataAtTick = new SnapshotData.DataAtTick
                                    {
                                        Tick = snapshotData.GetLatestTick(snapshotDataBuffer),
                                        InterpolationFactor = 1,
                                        SnapshotAfter = latestSnapshot,
                                        SnapshotBefore = latestSnapshot
                                    };

                                    serializer.CopyFromSnapshot.Ptr.Invoke(
                                        compPtr + serializer.ComponentSize * ent,
                                        (IntPtr) (&dataAtTick), offset);
                                }
                                else
                                {
                                    serializer.RestoreFromBackup.Ptr.Invoke(
                                        compPtr + serializer.ComponentSize * ent,
                                        latestSnapshot + offset);
                                }
                            }

                            offset += serializer.DataSize;
                        }
                    }
                }
            }
        }
    }

    [ClientWorld]
    [UpdateAfter(typeof(NetworkReceiveSystemGroup))]
    public unsafe class GhostInterpolatedUpdateSystem : ComponentSystem
    {
        private EntityQuery _ghostQuery;
        private NetworkTimeSystem _networkTime;

        protected override void OnCreate()
        {
            base.OnCreate();

            _networkTime = World.GetOrCreateSystem<NetworkTimeSystem>();

            _ghostQuery = GetEntityQuery(
                ComponentType.Exclude<GhostPredictionComponent>(),
                ComponentType.ReadWrite<GhostComponent>(),
                ComponentType.ReadWrite<SnapshotData>(),
                ComponentType.ReadWrite<SnapshotDataBuffer>());

            RequireSingletonForUpdate<NetworkStreamInGame>();
        }

        protected override void OnUpdate()
        {
            // 刷新快照的值到组件上
            NativeArray<ArchetypeChunk> ghostChunks =
                _ghostQuery.CreateArchetypeChunkArrayAsync(Allocator.TempJob, out var ghostChunkHandle);
            ghostChunkHandle.Complete();

            var ghostSerializerCollection = World.GetExistingSystem<GhostCollectionSystem>();
            InterpolatedUpdateJob interpolatedUpdateJob = new InterpolatedUpdateJob
            {
                GhostChunks = ghostChunks,
                EntityTypeHandle = GetEntityTypeHandle(),
                GhostComponentTypeHandle = GetComponentTypeHandle<GhostComponent>(),
                SnapshotBufferTypeHandle = GetBufferTypeHandle<SnapshotDataBuffer>(),
                SnapshotComponentTypeHandle = GetComponentTypeHandle<SnapshotData>(),
                InterpolatedTargetTick = _networkTime.interpolateTargetTick,
                InterpolatedTargetTickFraction = _networkTime.subInterpolateTargetTick,
                ComponentSerializers = ghostSerializerCollection.Serializers,
                GhostTypeCollection = ghostSerializerCollection.GhostTypeCollection,
                ComponentIndex = ghostSerializerCollection.IndexCollection
            };
            var listLen = ghostSerializerCollection.Serializers.Length;
            if (listLen <= 32)
            {
                var updateJob32 = new UpdateJob32 {Job = interpolatedUpdateJob};
                DynamicTypeList.PopulateList(this, ghostSerializerCollection.Serializers, false,
                    ref updateJob32.List);
                updateJob32.Schedule().Complete();
            }

            ghostChunks.Dispose();
        }

        [BurstCompile]
        struct UpdateJob32 : IJob
        {
            public DynamicTypeList32 List;
            public InterpolatedUpdateJob Job;

            public void Execute()
            {
                Job.Execute(List.GetData(), List.Length);
            }
        }

        [BurstCompile]
        struct InterpolatedUpdateJob
        {
            [ReadOnly] public NativeArray<ArchetypeChunk> GhostChunks;
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;
            [ReadOnly] public ComponentTypeHandle<GhostComponent> GhostComponentTypeHandle;
            [WriteOnly] public BufferTypeHandle<SnapshotDataBuffer> SnapshotBufferTypeHandle;
            public ComponentTypeHandle<SnapshotData> SnapshotComponentTypeHandle;

            public uint InterpolatedTargetTick;
            public float InterpolatedTargetTickFraction;

            [ReadOnly] public NativeArray<GhostComponentSerializer> ComponentSerializers;
            [ReadOnly] public NativeList<GhostCollectionSystem.GhostTypeState> GhostTypeCollection;
            [ReadOnly] public NativeList<GhostCollectionSystem.GhostComponentIndex> ComponentIndex;

            public void Execute(DynamicComponentTypeHandle* ghostChunkComponentTypesPtr,
                int ghostChunkComponentTypesLength)
            {
                for (int i = 0; i < GhostChunks.Length; i++)
                {
                    ArchetypeChunk chunk = GhostChunks[i];
                    var entities = chunk.GetNativeArray(EntityTypeHandle);
                    var buffers = chunk.GetBufferAccessor(SnapshotBufferTypeHandle);
                    var datas = chunk.GetNativeArray(SnapshotComponentTypeHandle);
                    var ghosts = chunk.GetNativeArray(GhostComponentTypeHandle);

                    for (int ent = 0; ent < entities.Length; ent++)
                    {
                        GhostComponent ghostComponent = ghosts[ent];
                        var typeState = GhostTypeCollection[ghostComponent.GhostType];

                        SnapshotData snapshotData = datas[ent];
                        DynamicBuffer<SnapshotDataBuffer> snapshotDataBuffer = buffers[ent];

                        if (snapshotData.SnapshotSize == 0)
                            continue;

                        bool isGet = snapshotData.GetDataAtTick(snapshotDataBuffer, InterpolatedTargetTick,
                            InterpolatedTargetTickFraction,
                            out var dataAtTick);

                        int baseOffset = typeState.FirstComponent;
                        int numBaseComponents = typeState.NumComponents;

                        // 跳过Tick
                        int offset = GlobalConstants.TickSize;

                        for (int j = 0; j < numBaseComponents; j++)
                        {
                            int compIdx = ComponentIndex[baseOffset + j].ComponentIndex;
                            GhostComponentSerializer serializer = ComponentSerializers[compIdx];
                            DynamicComponentTypeHandle dynamicType =
                                ghostChunkComponentTypesPtr[compIdx];

                            if (chunk.Has(dynamicType))
                            {
                                IntPtr compPtr = (IntPtr) chunk
                                    .GetDynamicComponentDataArrayReinterpret<byte>(dynamicType,
                                        serializer.ComponentSize).GetUnsafePtr();

                                if (isGet)
                                {
                                    serializer.CopyFromSnapshot.Ptr.Invoke(
                                        compPtr + serializer.ComponentSize * ent,
                                        (IntPtr) (&dataAtTick), offset);
                                }
                            }

                            offset += serializer.DataSize;
                        }
                    }
                }
            }
        }
    }
}