using Unity.Burst;
using Unity.Entities;
using Unity.Networking.Transport;

namespace MyGameLib.NetCode
{
    [BurstCompile]
    [DisableCommandCodeGen]
    struct HeartbeatComponent : IRpcCommand, IRpcCommandSerializer<HeartbeatComponent>
    {
        public void Serialize(ref DataStreamWriter writer, in HeartbeatComponent data)
        {
        }

        public void Deserialize(ref DataStreamReader reader, ref HeartbeatComponent data)
        {
        }

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(RpcExecutor.ExecuteDelegate))]
        private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
        {
            RpcExecutor.ExecuteCreateRequestComponent<HeartbeatComponent, HeartbeatComponent>(ref parameters);
        }

        static PortableFunctionPointer<RpcExecutor.ExecuteDelegate> InvokeExecuteFunctionPointer =
            new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);

        public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
        {
            return InvokeExecuteFunctionPointer;
        }
    }

    // class HeartbeatComponentRpcCommandRequestSystem : RpcCommandRequestSystem<HeartbeatComponent>
    // {
    // }

    [UpdateInGroup(typeof(NetworkReceiveSystemGroup))]
    [UpdateInWorld(TargetWorld.Client)]
    public class HeartbeatSendSystem : ComponentSystem
    {
        private uint _lastSend;
        private EntityQuery _connectionQuery;

        protected override void OnCreate()
        {
            _connectionQuery = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkIdComponent>(),
                ComponentType.Exclude<NetworkStreamDisconnected>(),
                ComponentType.Exclude<NetworkStreamInGame>());
        }

        protected override void OnUpdate()
        {
        }
    }
}