
using AOT;
using Unity.Burst;
using Unity.Networking.Transport;
using MyGameLib.NetCode;
using Samples.NetFPS;

namespace Assembly_CSharp.Generated
{
    [BurstCompile]
    public struct GrenadeRpcSerializer : IRpcCommandSerializer<GrenadeRpc>
    {
        public void Serialize(ref DataStreamWriter writer, in GrenadeRpc data)
        {
			writer.WriteInt32(data.OwnerGId);
			writer.WriteUInt32(data.Tick);
			writer.WriteFloat3(data.Pos);
			writer.WriteFloat3(data.Dir);
        }

        public void Deserialize(ref DataStreamReader reader, ref GrenadeRpc data)
        {
			data.OwnerGId = reader.ReadInt32();
			data.Tick = reader.ReadUInt32();
			data.Pos = reader.ReadFloat3();
			data.Dir = reader.ReadFloat3();
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(RpcExecutor.ExecuteDelegate))]
        private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
        {
            RpcExecutor.ExecuteCreateRequestComponent<GrenadeRpcSerializer, GrenadeRpc>(
                ref parameters);
        }

        static PortableFunctionPointer<RpcExecutor.ExecuteDelegate> InvokeExecuteFunctionPointer =
            new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);

        public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
        {
            return InvokeExecuteFunctionPointer;
        }
    }

    public class GrenadeRpcRequestSystem : RpcCommandRequestSystem<GrenadeRpcSerializer, GrenadeRpc>
    {
    }
}