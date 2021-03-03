
using AOT;
using Unity.Burst;
using Unity.Networking.Transport;
using MyGameLib.NetCode;
using Samples.MyGameLib.NetCode;

namespace Assembly_CSharp.Generated
{
    [BurstCompile]
    public struct ProjectileHitSerializer : IRpcCommandSerializer<ProjectileHit>
    {
        public void Serialize(ref DataStreamWriter writer, in ProjectileHit data)
        {
			writer.WriteInt32(data.GId);
			writer.WriteFloat3(data.Point);
			writer.WriteFloat3(data.Normal);
			writer.WriteInt32(data.Hp);
        }

        public void Deserialize(ref DataStreamReader reader, ref ProjectileHit data)
        {
			data.GId = reader.ReadInt32();
			data.Point = reader.ReadFloat3();
			data.Normal = reader.ReadFloat3();
			data.Hp = reader.ReadInt32();
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(RpcExecutor.ExecuteDelegate))]
        private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
        {
            RpcExecutor.ExecuteCreateRequestComponent<ProjectileHitSerializer, ProjectileHit>(
                ref parameters);
        }

        static PortableFunctionPointer<RpcExecutor.ExecuteDelegate> InvokeExecuteFunctionPointer =
            new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);

        public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
        {
            return InvokeExecuteFunctionPointer;
        }
    }

    public class ProjectileHitRequestSystem : RpcCommandRequestSystem<ProjectileHitSerializer, ProjectileHit>
    {
    }
}