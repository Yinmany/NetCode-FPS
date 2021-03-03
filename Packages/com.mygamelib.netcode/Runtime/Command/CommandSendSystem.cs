using Unity.Entities;

namespace MyGameLib.NetCode
{
    [ClientWorld]
    [UpdateInGroup(typeof(TickSimulationSystemGroup))]
    [UpdateAfter(typeof(GhostSimulationSystemGroup))]
    public class CommandSendSystemGroup : ComponentSystemGroup
    {
        public CommandSendSystemGroup()
        {
        }
    }

    [UpdateInGroup(typeof(CommandSendSystemGroup))]
    public abstract class CommandSendSystem<TCommandDataSerializer, TCommandData> : ComponentSystem
        where TCommandDataSerializer : struct, ICommandDataSerializer<TCommandData>
        where TCommandData : struct, ICommandData
    {
        private TickSimulationSystemGroup _clientTickSimulationSystemGroup;

        protected override void OnCreate()
        {
            RequireForUpdate(EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TCommandData>()));
            _clientTickSimulationSystemGroup = World.GetOrCreateSystem<TickSimulationSystemGroup>();
        }

        protected override void OnUpdate()
        {
            uint tick = _clientTickSimulationSystemGroup.ServerTick;

            var localPlayer = GetSingleton<CommandTargetComponent>().Target;
            var inputFromEntity = GetBufferFromEntity<TCommandData>(true);
            var input = inputFromEntity[localPlayer];

            if (!input.GetDataAtTick(tick, out TCommandData cmd))
            {
                return;
            }

            var localTime = NetworkTimeSystem.TimestampMS;
            var ack = GetSingleton<NetworkSnapshotAckComponent>();
            var net = World.GetExistingSystem<NetworkStreamReceiveSystem>();
            var writer = net.Driver.BeginSend(net.UnreliablePipeline, GetSingleton<NetworkStreamConnection>().Value);

            // 写入头
            writer.WriteByte((byte) NetworkStreamProtocol.Command);
            writer.WriteUInt(ack.LastReceivedSnapshotByLocal);
            writer.WriteUInt(ack.ReceivedSnapshotByLocalMask);
            writer.WriteUInt(localTime); // localTime
            uint returnTime = ack.LastReceivedRemoteTime;
            if (returnTime != 0)
                returnTime -= (localTime - ack.LastReceiveTimestamp);
            writer.WriteUInt(returnTime);
            writer.WriteUInt(tick);

            var serializer = default(TCommandDataSerializer);
            serializer.Serialize(ref writer, cmd);

            // 发送冗余输入
            for (uint i = 1; i <= GlobalConstants.InputBufferSendSize; ++i)
            {
                input.GetDataAtTick(tick - i, out cmd);
                serializer.Serialize(ref writer, cmd);
            }

            writer.Flush();
            net.Driver.EndSend(writer);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NetDebug.CommandMS = writer.Length;
            NetDebug.UpCount += writer.Length;
#endif
        }
    }
}