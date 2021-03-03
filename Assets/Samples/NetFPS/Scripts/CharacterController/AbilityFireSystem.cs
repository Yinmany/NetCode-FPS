using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using NetCode_Example_Scenes.Scripts.Player;
using Samples.MyGameLib.NetCode;
using Samples.MyGameLib.NetCode.Base;
using Unity.Entities;

namespace Samples.NetFPS
{
    [GhostComponent(PrefabType = GhostPrefabType.Server | GhostPrefabType.PredictedClient)]
    public struct AbilityFireComponent : IComponentData
    {
        public uint LastFireTick;
        public int CurFireRate;
    }

    [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    public class AbilityFireSystem : ComponentSystem
    {
        private GhostPredictionSystemGroup _ghostPredictionSystemGroup;

        private int fireRate = 10;
        private bool isServer = false;

        protected override void OnCreate()
        {
            _ghostPredictionSystemGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();
            isServer = World.GetExistingSystem<ServerSimulationSystemGroup>() != null;
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((
                Entity ent,
                ref GhostComponent ghost,
                ref AbilityFireComponent ability,
                ref PlayerControlledState state) =>
            {
                // 在回滚时，可以跳过.开火，因为这种开火不需要回滚.
                if (_ghostPredictionSystemGroup.PredictingTick <= ability.LastFireTick)
                    return;

                ability.LastFireTick = _ghostPredictionSystemGroup.PredictingTick;

                ++ability.CurFireRate;
                if (!state.Command.Fire || ability.CurFireRate < fireRate)
                {
                    return;
                }

                ability.CurFireRate = 0;

                var trans = World.GetOrCreateSystem<GameObjectManager>()[ent].transform;

                // 动画相关
                var view = trans.GetComponent<GameObjectLinked>().Target.GetComponent<LinkedPlayerView>();
                view.GenProjectile(World, state.Command.FirePos, state.Command.FireDir, isServer);

                if (this.isServer)
                {
                    // 广播给客户端
                    this.World.GetExistingSystem<BroadcastSystem>().Add(new FireRpc
                    {
                        OwnerGId = ghost.Id,
                        Tick = ability.LastFireTick,
                        Pos = state.Command.FirePos,
                        Dir = state.Command.FireDir
                    });
                }
            });
        }
    }
}