
using AOT;
using Unity.Burst;
using Unity.Networking.Transport;
using MyGameLib.NetCode;
using Samples;

namespace Assembly_CSharp.Generated
{
    [BurstCompile]
    public struct GoInGameRequestSerializer : IRpcCommandSerializer<GoInGameRequest>
    {
        public void Serialize(ref DataStreamWriter writer, in GoInGameRequest data)
        {
			writer.WriteInt32(data.value);
        }

        public void Deserialize(ref DataStreamReader reader, ref GoInGameRequest data)
        {
			data.value = reader.ReadInt32();
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(RpcExecutor.ExecuteDelegate))]
        private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
        {
            RpcExecutor.ExecuteCreateRequestComponent<GoInGameRequestSerializer, GoInGameRequest>(
                ref parameters);
        }

        static PortableFunctionPointer<RpcExecutor.ExecuteDelegate> InvokeExecuteFunctionPointer =
            new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);

        public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
        {
            return InvokeExecuteFunctionPointer;
        }
    }

    public class GoInGameRequestRequestSystem : RpcCommandRequestSystem<GoInGameRequestSerializer, GoInGameRequest>
    {
    }
}