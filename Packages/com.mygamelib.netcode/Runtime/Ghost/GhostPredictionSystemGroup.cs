using Unity.Entities;
using Unity.Networking.Transport.Utilities;
using UnityEngine;

namespace MyGameLib.NetCode
{
    [UpdateInGroup(typeof(GhostPredictionSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(EndPredictionAfterSystemGroup))]
    public class BeginGhostPredictionSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(GhostPredictionSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(BeginGhostPredictionSystemGroup))]
    public class EndPredictionAfterSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
    public class GhostPredictionSystemGroup : ComponentSystemGroup
    {
        public uint PredictingTick { get; set; }

        // 倒带中
        public bool IsRewind = false;

        private bool _isServer;

        /// <summary>
        /// 最近一次应用的快照Tick
        /// </summary>
        public uint LastAppliedSnapshotTick { get; private set; }

        private TickSimulationSystemGroup _tickSimulationSystemGroup;

        public bool IsFixError { get; set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            this.SortSystems();

            _isServer = World.GetExistingSystem<ServerSimulationSystemGroup>() != null;
            if (_isServer) return;
            _tickSimulationSystemGroup = World.GetExistingSystem<TickSimulationSystemGroup>();
            RequireSingletonForUpdate<NetworkSnapshotAckComponent>();
        }

        protected override void OnUpdate()
        {
            if (_isServer)
            {
                this.PredictingTick = World.GetExistingSystem<ServerSimulationSystemGroup>().Tick;
                NetDebug.ServerTick = this.PredictingTick;
                base.OnUpdate();
                return;
            }

            //====================================================================================
            IsFixError = false;

            NetworkSnapshotAckComponent ack = GetSingleton<NetworkSnapshotAckComponent>();
            uint targetTick = _tickSimulationSystemGroup.ServerTick;

            if (!SequenceHelpers.IsNewer(targetTick, ack.LastReceivedSnapshotByLocal))
            {
                return;
            }

            if (!SequenceHelpers.IsNewer(ack.LastReceivedSnapshotByLocal, LastAppliedSnapshotTick))
            {
                LitUpdate(targetTick);
                return;
            }

            LastAppliedSnapshotTick = ack.LastReceivedSnapshotByLocal;
            if (targetTick - LastAppliedSnapshotTick > GlobalConstants.CommandDataMaxSize)
            {
                LastAppliedSnapshotTick = targetTick - GlobalConstants.CommandDataMaxSize;
            }

            IsRewind = true;
            // Debug.Log($"开始回滚：{LastAppliedSnapshotTick + 1} to {targetTick}, PredictingTick={PredictingTick}");
            for (uint i = LastAppliedSnapshotTick + 1; i != targetTick; ++i)
            {
                LitUpdate(i);
            }

            IsRewind = false;
            // Debug.Log($"回滚结束：{PredictingTick}");

            IsFixError = true;
            LitUpdate(targetTick);
        }

        private void LitUpdate(uint tick)
        {
            PredictingTick = tick;
            NetDebug.ClientTick = this.PredictingTick;
            base.OnUpdate();
        }
    }
}