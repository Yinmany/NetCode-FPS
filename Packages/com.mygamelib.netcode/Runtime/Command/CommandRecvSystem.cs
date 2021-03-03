using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using UnityEngine;

namespace MyGameLib.NetCode
{
    [ServerWorld]
    [UpdateInGroup(typeof(NetworkReceiveSystemGroup))]
    [UpdateAfter(typeof(NetworkStreamReceiveSystem))]
    public class CommandReceiveSystemGroup : ComponentSystemGroup
    {
        public CommandReceiveSystemGroup()
        {
        }
    }

    [UpdateInGroup(typeof(CommandReceiveSystemGroup))]
    public abstract class CommandRecvSystem<TCommandSerializer, TCommandData> : SystemBase
        where TCommandSerializer : struct, ICommandDataSerializer<TCommandData>
        where TCommandData : struct, ICommandData
    {
        private EntityQuery cmdRecvGroup;

        protected override void OnCreate()
        {
            cmdRecvGroup = GetEntityQuery(
                ComponentType.ReadOnly<NetworkStreamInGame>(),
                ComponentType.ReadOnly<IncomingCommandDataStreamBufferComponent>(),
                ComponentType.ReadOnly<CommandTargetComponent>(),
                ComponentType.Exclude<NetworkStreamDisconnected>());

            RequireForUpdate(cmdRecvGroup);
        }

        protected override void OnUpdate()
        {
            CommandRecvJob job = new CommandRecvJob
            {
                entityType = GetEntityTypeHandle(),
                inBufferType = GetBufferTypeHandle<IncomingCommandDataStreamBufferComponent>(),
                commandTargetType = GetComponentTypeHandle<CommandTargetComponent>(),
                snapshotAckType = GetComponentTypeHandle<NetworkSnapshotAckComponent>(),
                commandDatas = GetBufferFromEntity<TCommandData>(),
                serverTick = World.GetExistingSystem<ServerSimulationSystemGroup>().Tick
            };

            job.Run(cmdRecvGroup);
        }

        [BurstCompile]
        struct CommandRecvJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle entityType;
            [ReadOnly] public ComponentTypeHandle<CommandTargetComponent> commandTargetType;

            public ComponentTypeHandle<NetworkSnapshotAckComponent> snapshotAckType;

            public BufferFromEntity<TCommandData> commandDatas;
            public BufferTypeHandle<IncomingCommandDataStreamBufferComponent> inBufferType;
            public uint serverTick;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entities = chunk.GetNativeArray(entityType);
                var commandTargets = chunk.GetNativeArray(commandTargetType);
                var snapshotAckComponents = chunk.GetNativeArray(snapshotAckType);

                var inBuffer = chunk.GetBufferAccessor(inBufferType);

                for (int i = 0; i < entities.Length; i++)
                {
                    if (commandTargets[i].Target == Entity.Null) continue;

                    DynamicBuffer<TCommandData> cmdBuf = commandDatas[commandTargets[i].Target];
                    NetworkSnapshotAckComponent snapshotAck = snapshotAckComponents[i];

                    DataStreamReader reader = inBuffer[i].AsDataStreamReader();
                    while (reader.GetBytesRead() < reader.Length)
                    {
                        uint tick = reader.ReadUInt();

                        var serilaizer = default(TCommandSerializer);
                        TCommandData cmd = default;
                        cmd.Tick = tick;
                        serilaizer.Deserialize(ref reader, ref cmd);

                        int age = (int) (serverTick - cmd.Tick);
                        age *= 256;
                        snapshotAck.ServerCommandAge = (snapshotAck.ServerCommandAge * 7 + age) / 8;
                        cmdBuf.AddCommandData(cmd);

                        // 读取冗余输入
                        for (uint j = 1; j <= GlobalConstants.InputBufferSendSize; ++j)
                        {
                            cmd.Tick = tick - j;
                            serilaizer.Deserialize(ref reader, ref cmd);
                            cmdBuf.AddCommandData(cmd);
                        }
                    }

                    snapshotAckComponents[i] = snapshotAck;
                    inBuffer[i].Clear();
                }
            }
        }
    }
}