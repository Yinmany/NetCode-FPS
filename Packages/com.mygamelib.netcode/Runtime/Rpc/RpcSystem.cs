using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Networking.Transport;
using UnityEngine;

namespace MyGameLib.NetCode
{
    public struct RpcExecutor
    {
        public struct Parameters
        {
            public DataStreamReader Reader;
            public Entity Connection;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            public int JobIndex;
        }

        public delegate void ExecuteDelegate(ref Parameters parameters);

        /// <summary>
        /// Helper method used to create a new entity for an RPC request T.
        /// </summary>
        public static Entity ExecuteCreateRequestComponent<TActionSerializer, TActionRequest>(ref Parameters parameters)
            where TActionSerializer : IRpcCommandSerializer<TActionRequest>
            where TActionRequest : struct, IComponentData
        {
            var rpcData = default(TActionRequest);
            var rpcSerializer = default(TActionSerializer);
            rpcSerializer.Deserialize(ref parameters.Reader, ref rpcData);
            var entity = parameters.CommandBuffer.CreateEntity(parameters.JobIndex);

            parameters.CommandBuffer.AddComponent(parameters.JobIndex, entity,
                new ReceiveRpcCommandRequestComponent {SourceConnection = parameters.Connection});
            parameters.CommandBuffer.AddComponent(parameters.JobIndex, entity, rpcData);
            return entity;
        }
    }

    [UpdateInGroup(typeof(RpcCommandRequestSystemGroup))]
    public class RpcSystem : JobComponentSystem
    {
        struct RpcData : IComparable<RpcData>
        {
            public ulong TypeHash;
            public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> Execute;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public ComponentType RpcType;
#endif
            public int CompareTo(RpcData other)
            {
                if (TypeHash < other.TypeHash)
                    return -1;
                if (TypeHash > other.TypeHash)
                    return 1;
                return 0;
            }
        }

        private BeginSimulationEntityCommandBufferSystem _barrier;
        private NetworkStreamReceiveSystem _receiveSystem;
        private NativeList<RpcData> _rpcData;
        private NativeHashMap<ulong, int> _rpcTypeHashToIndex;

        private EntityQuery rpcExecuteGroup;

        protected override void OnCreate()
        {
            _rpcData = new NativeList<RpcData>(16, Allocator.Persistent);
            _rpcTypeHashToIndex = new NativeHashMap<ulong, int>(16, Allocator.Persistent);

            RegisterRpc<RpcSetNetworkId, RpcSetNetworkId>();

            rpcExecuteGroup = GetEntityQuery(
                ComponentType.ReadWrite<IncomingRpcDataStreamBufferComponent>(),
                ComponentType.ReadWrite<OutgoingRpcDataStreamBufferComponent>(),
                ComponentType.ReadWrite<NetworkStreamConnection>(),
                ComponentType.Exclude<NetworkStreamDisconnected>());

            RequireForUpdate(rpcExecuteGroup);

            _barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            _receiveSystem = World.GetOrCreateSystem<NetworkStreamReceiveSystem>();
        }

        protected override void OnDestroy()
        {
            _rpcTypeHashToIndex.Dispose();
            _rpcData.Dispose();
        }

        public RpcQueue<TActionSerializer, TActionRequest> GetRpcQueue<TActionSerializer, TActionRequest>()
            where TActionSerializer : struct, IRpcCommandSerializer<TActionRequest>
            where TActionRequest : struct, IComponentData
        {
            if (!_rpcTypeHashToIndex.IsCreated)
            {
                throw new InvalidOperationException($"RPCSystem 还是没有创建，或者以及被销毁了.");
            }

            var hash = TypeManager.GetTypeInfo(TypeManager.GetTypeIndex<TActionRequest>()).StableTypeHash;
            if (!_rpcTypeHashToIndex.TryGetValue(hash, out _))
            {
                Debug.Log($"RPC消息类型没有注册:{typeof(TActionRequest).Name}");
            }

            return new RpcQueue<TActionSerializer, TActionRequest>
            {
                rpcType = hash,
                rpcTypeHashToIndex = _rpcTypeHashToIndex
            };
        }

        /// <summary>
        /// 注册一个Rpc消息
        /// </summary>
        /// <typeparam name="TActionSerializer"></typeparam>
        /// <typeparam name="TActionRequest"></typeparam>
        /// <exception cref="InvalidOperationException"></exception>
        public void RegisterRpc<TActionSerializer, TActionRequest>()
            where TActionSerializer : struct, IRpcCommandSerializer<TActionRequest>
            where TActionRequest : struct, IRpcCommand
        {
            RegisterRpc(ComponentType.ReadWrite<TActionRequest>(), default(TActionSerializer).CompileExecute());
        }

        public void RegisterRpc(ComponentType type, PortableFunctionPointer<RpcExecutor.ExecuteDelegate> exec)
        {
            if (!exec.Ptr.IsCreated)
            {
                throw new InvalidOperationException(
                    $"不能注册Rpc类型 {type.GetManagedType()}: Ptr 属性没有创建 (null)" +
                    "Check CompileExecute() and verify you are initializing the PortableFunctionPointer with a valid static function delegate, decorated with [BurstCompile] attribute");
            }

            var hash = TypeManager.GetTypeInfo(type.TypeIndex).StableTypeHash;
            if (hash == 0)
                throw new InvalidOperationException($"Unexpected 0 hash for type {type.GetManagedType()}");

            if (_rpcTypeHashToIndex.TryGetValue(hash, out var index))
            {
                var rpcData = _rpcData[index];
                if (rpcData.TypeHash != 0)
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (rpcData.RpcType == type)
                        throw new InvalidOperationException(
                            String.Format("Registering RPC {0} multiple times is not allowed", type.GetManagedType()));
                    throw new InvalidOperationException(
                        String.Format("Type hash collision between types {0} and {1}", type.GetManagedType(),
                            rpcData.RpcType.GetManagedType()));
#else
                    throw new InvalidOperationException(
                        String.Format("Hash collision or multiple registrations for {0}", type.GetManagedType()));
#endif
                }

                rpcData.TypeHash = hash;
                rpcData.Execute = exec;
                _rpcData[index] = rpcData;
            }
            else
            {
                _rpcTypeHashToIndex.Add(hash, _rpcData.Length);
                _rpcData.Add(new RpcData
                {
                    TypeHash = hash,
                    Execute = exec,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    RpcType = type
#endif
                });
            }

            // Debug.Log("注册Rpc:" + index + " " + World.Name);
        }

        [BurstCompile]
        struct RpcExecJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter commandBuffer;
            [ReadOnly] public EntityTypeHandle entityType;
            [ReadOnly] public ComponentTypeHandle<NetworkStreamConnection> connectionType;

            public BufferTypeHandle<IncomingRpcDataStreamBufferComponent> InBufferType;
            public BufferTypeHandle<OutgoingRpcDataStreamBufferComponent> OutBufferType;

            public NetworkPipeline reliablePipeline;
            public NetworkDriver.Concurrent driver;
            [ReadOnly] public NativeList<RpcData> execute;

            public unsafe void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entities = chunk.GetNativeArray(entityType);
                var connections = chunk.GetNativeArray(connectionType);
                var inBuffers = chunk.GetBufferAccessor(InBufferType);
                var outBuffers = chunk.GetBufferAccessor(OutBufferType);

                for (int i = 0; i < inBuffers.Length; i++)
                {
                    if (driver.GetConnectionState(connections[i].Value) != NetworkConnection.State.Connected)
                    {
                        continue;
                    }

                    var parameters = new RpcExecutor.Parameters
                    {
                        Reader = inBuffers[i].AsDataStreamReader(),
                        CommandBuffer = commandBuffer,
                        Connection = entities[i],
                        JobIndex = chunkIndex
                    };

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    RpcMS2(parameters.Reader.Length);
#endif
                    while (parameters.Reader.GetBytesRead() < parameters.Reader.Length)
                    {
                        var rpcIndex = parameters.Reader.ReadUShort();
                        if (rpcIndex >= execute.Length)
                        {
                        }
                        else
                        {
                            //TODO 安卓调用闪退
                            execute[rpcIndex].Execute.Ptr.Invoke(ref parameters);
                        }
                    }

                    inBuffers[i].Clear();

                    var sendBuf = outBuffers[i];
                    if (sendBuf.Length > 0)
                    {
                        DataStreamWriter tmp = driver.BeginSend(reliablePipeline, connections[i].Value);
                        if (!tmp.IsCreated)
                            return;
                        tmp.WriteBytes((byte*) sendBuf.GetUnsafePtr(), sendBuf.Length);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        RpcMS1(tmp.Length);
#endif
                        driver.EndSend(tmp);
                        sendBuf.Clear();
                    }
                }
            }

            [BurstDiscard]
            public static void RpcMS1(int len)
            {
                NetDebug.RpcMS1 = len;
                NetDebug.UpCount += len;
            }

            [BurstDiscard]
            public static void RpcMS2(int len)
            {
                if (len > 0)
                    NetDebug.RpcMS2 = len;
                NetDebug.DownCount += len;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            EntityCommandBuffer.ParallelWriter parallelWriter = _barrier.CreateCommandBuffer().AsParallelWriter();
            NetworkDriver.Concurrent concurrent = _receiveSystem.Driver.ToConcurrent();
            NetworkPipeline networkPipeline = _receiveSystem.ReliablePipeline;

            RpcExecJob job = new RpcExecJob
            {
                commandBuffer = parallelWriter,
                entityType = GetEntityTypeHandle(),
                connectionType = GetComponentTypeHandle<NetworkStreamConnection>(),
                InBufferType = GetBufferTypeHandle<IncomingRpcDataStreamBufferComponent>(),
                OutBufferType = GetBufferTypeHandle<OutgoingRpcDataStreamBufferComponent>(),
                execute = _rpcData,
                driver = concurrent,
                reliablePipeline = networkPipeline
            };

            var jobHandle = job.ScheduleParallel(rpcExecuteGroup, inputDeps);
            _barrier.AddJobHandleForProducer(jobHandle);
            return jobHandle;
        }
    }
}