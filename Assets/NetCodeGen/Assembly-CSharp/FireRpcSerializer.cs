
using AOT;
using Unity.Burst;
using Unity.Networking.Transport;
using MyGameLib.NetCode;
using Samples.MyGameLib.NetCode;

namespace Assembly_CSharp.Generated
{
    [BurstCompile]
    public struct FireRpcSerializer : IRpcCommandSerializer<FireRpc>
    {
        public void Serialize(ref DataStreamWriter writer, in FireRpc data)
        {
			writer.WriteInt32(data.OwnerGId);
			writer.WriteUInt32(data.Tick);
			writer.WriteFloat3(data.Pos);
			writer.WriteFloat3(data.Dir);
        }

        public void Deserialize(ref DataStreamReader reader, ref FireRpc data)
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
            RpcExecutor.ExecuteCreateRequestComponent<FireRpcSerializer, FireRpc>(
                ref parameters);
        }

        static PortableFunctionPointer<RpcExecutor.ExecuteDelegate> InvokeExecuteFunctionPointer =
            new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);

        public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
        {
            return InvokeExecuteFunctionPointer;
        }
    }

    public class FireRpcRequestSystem : RpcCommandRequestSystem<FireRpcSerializer, FireRpc>
    {
    }
}