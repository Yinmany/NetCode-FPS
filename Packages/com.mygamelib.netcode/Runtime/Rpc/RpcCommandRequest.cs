using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace MyGameLib.NetCode
{
    public struct SendRpcCommandRequestComponent : IComponentData
    {
        public Entity TargetConnection;
    }

    public struct ReceiveRpcCommandRequestComponent : IComponentData
    {
        public Entity SourceConnection;
    }

    [UpdateInGroup(typeof(NetworkReceiveSystemGroup))]
    public class RpcCommandRequestSystemGroup : ComponentSystemGroup
    {
        public RpcCommandRequestSystemGroup()
        {
        }
    }

    [UpdateBefore(typeof(RpcSystem))]
    [UpdateInGroup(typeof(RpcCommandRequestSystemGroup))]
    public abstract class RpcCommandRequestSystem<TActionSerializer, TActionRequest> : SystemBase
        where TActionSerializer : struct, IRpcCommandSerializer<TActionRequest>
        where TActionRequest : struct, IRpcCommand
    {
        private RpcQueue<TActionSerializer, TActionRequest> _rpcQueue;
        private BeginSimulationEntityCommandBufferSystem _barrier;
        private EntityQuery requestRpcQuery;

        protected override void OnCreate()
        {
            var rpcSystem = World.GetOrCreateSystem<RpcSystem>();
            rpcSystem.RegisterRpc<TActionSerializer, TActionRequest>();

            _rpcQueue = rpcSystem.GetRpcQueue<TActionSerializer, TActionRequest>();
            _barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();

            requestRpcQuery = GetEntityQuery(
                ComponentType.ReadOnly<SendRpcCommandRequestComponent>(),
                ComponentType.ReadOnly<TActionRequest>());
        }

        protected override void OnUpdate()
        {
            SendRpcJob job = new SendRpcJob
            {
                commandBuffer = _barrier.CreateCommandBuffer().AsParallelWriter(),
                entitiesType = GetEntityTypeHandle(),
                reqType = GetComponentTypeHandle<TActionRequest>(),
                rpcQueue = _rpcQueue,
                rpcFromEntity = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>(),
                rpcRequestType = GetComponentTypeHandle<SendRpcCommandRequestComponent>()
            };

            this.Dependency = job.ScheduleSingle(requestRpcQuery, this.Dependency);
            _barrier.AddJobHandleForProducer(this.Dependency);
        }

        [BurstCompile]
        struct SendRpcJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter commandBuffer;
            [ReadOnly] public EntityTypeHandle entitiesType;
            [ReadOnly] public ComponentTypeHandle<SendRpcCommandRequestComponent> rpcRequestType;
            [ReadOnly] public ComponentTypeHandle<TActionRequest> reqType;
            public BufferFromEntity<OutgoingRpcDataStreamBufferComponent> rpcFromEntity;
            public RpcQueue<TActionSerializer, TActionRequest> rpcQueue;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entities = chunk.GetNativeArray(entitiesType);
                var rpcRequests = chunk.GetNativeArray(rpcRequestType);
                var requests = chunk.GetNativeArray(reqType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    commandBuffer.DestroyEntity(chunkIndex, entities[i]);
                    if (rpcRequests[i].TargetConnection != Entity.Null)
                    {
                        var buffer = rpcFromEntity[rpcRequests[i].TargetConnection];
                        rpcQueue.Schedule(buffer, requests[i]);
                    }
                }
            }
        }
    }
}