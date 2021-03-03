using Unity.Burst;
using Unity.Entities;
using Unity.Networking.Transport;
using UnityEngine;

namespace MyGameLib.NetCode
{
    public struct NetworkIdComponent : IComponentData
    {
        public int Value;
    }

    /// <summary>
    /// 有服务端发送，客户端接收处理.
    /// </summary>
    [BurstCompile]
    [DisableCommandCodeGen]
    public struct RpcSetNetworkId : IRpcCommand, IRpcCommandSerializer<RpcSetNetworkId>
    {
        public int id;
        public int simTickRate;
        public int netTickRate;
        public int simMaxSteps;

        public void Serialize(ref DataStreamWriter writer, in RpcSetNetworkId data)
        {
            writer.WriteInt(data.id);
            writer.WriteInt(data.simTickRate);
            writer.WriteInt(data.netTickRate);
            writer.WriteInt(data.simMaxSteps);
        }

        public void Deserialize(ref DataStreamReader reader, ref RpcSetNetworkId data)
        {
            data.id = reader.ReadInt();
            data.simTickRate = reader.ReadInt();
            data.netTickRate = reader.ReadInt();
            data.simMaxSteps = reader.ReadInt();
        }

        public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
        {
            return InvokeExecuteFunctionPointer;
        }

        static readonly PortableFunctionPointer<RpcExecutor.ExecuteDelegate> InvokeExecuteFunctionPointer =
            new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(RpcExecutor.ExecuteDelegate))]
        private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
        {
            RpcSetNetworkId rpcData = default;
            RpcSetNetworkId rpcSerializer = default;
            rpcSerializer.Deserialize(ref parameters.Reader, ref rpcData);
            parameters.CommandBuffer.AddComponent(parameters.JobIndex, parameters.Connection,
                new NetworkIdComponent {Value = rpcData.id});
            Debug.Log($"设置NetId={rpcData.id}");
            Entity ent = parameters.CommandBuffer.CreateEntity(parameters.JobIndex);
            parameters.CommandBuffer.AddComponent(parameters.JobIndex, ent, new ClientServerTickRateRefreshRequest
            {
                SimulationTickRate = rpcData.simTickRate,
                NetworkTickRate = rpcData.netTickRate,
                MaxSimulationStepsPerFrame = rpcData.simMaxSteps
            });
        }
    }
}