using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.NetFPS
{
    [GhostComponent(PrefabType = GhostPrefabType.Server | GhostPrefabType.PredictedClient)]
    public struct AbilityGrenadeComponent : IComponentData
    {
        public uint LastFireTick;
        public int CurFireRate;
    }

    public struct GrenadeRpc : IRpcCommand
    {
        public int OwnerGId;
        public uint Tick;
        public float3 Pos;
        public float3 Dir;
    }

    [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    public class AbilityGrenadeSystem : ComponentSystem
    {
        private GhostPredictionSystemGroup _ghostPredictionSystemGroup;

        private int fireRate = 10;
        private bool isServer = false;

        private Entity grendePrefab;

        protected override void OnCreate()
        {
            _ghostPredictionSystemGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();
            isServer = World.GetExistingSystem<ServerSimulationSystemGroup>() != null;
        }

        protected override void OnUpdate()
        {
            if (grendePrefab == Entity.Null)
            {
                var collection = GetSingleton<GhostPrefabCollectionComponent>();
                var buffer = EntityManager.GetBuffer<GhostPrefabBuffer>(collection.ServerPrefabs);
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (!EntityManager.HasComponent<GrenadeTagComponent>(buffer[i].Value)) continue;

                    grendePrefab =
                        GhostCollectionSystem.CreatePredictedSpawnPrefab(EntityManager, buffer[i].Value);
                    break;
                }
            }

            Entities.ForEach((
                ref GhostComponent ghost,
                ref GhostOwnerComponent ghostOwner,
                ref AbilityGrenadeComponent ability,
                ref PlayerControlledState state) =>
            {
                // 在回滚时，进行跳过，仍手雷操作是不需要进行回滚的。
                if (_ghostPredictionSystemGroup.PredictingTick <= ability.LastFireTick)
                    return;

                ability.LastFireTick = _ghostPredictionSystemGroup.PredictingTick;

                ++ability.CurFireRate;

                // 测试雷
                if (state.Command.R && ability.CurFireRate > fireRate && ability.LastFireTick % 5 == 0)
                {
                    ability.CurFireRate = 0;

                    var ent = EntityManager.Instantiate(grendePrefab);

                    if (isServer)
                    {
                        EntityManager.SetComponentData(ent, ghostOwner);
                    }

                    // 同时创建GameObject
                    var linkSystem = World.GetOrCreateSystem<GameObjectManager>();
                    var gameObject = linkSystem.CreateDefaultGhostGameObject(ent);
                    gameObject.transform.position = state.Command.FirePos;
                    gameObject.transform.rotation = Quaternion.LookRotation(state.Command.FireDir);
                    gameObject.GetComponent<GrenadeScript>().Init(World, this.isServer);
                }
            });
        }
    }
}