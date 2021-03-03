using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using Samples.MyGameLib.NetCode.Base;
using Unity.Entities;
using UnityEngine;

namespace Samples.NetFPS
{
    [ServerWorld]
    [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    [UpdateAfter(typeof(AbilityFireSystem))]
    public class AbilityDeathSystem : ComponentSystem
    {
        private GameObjectManager _storeSystem;

        protected override void OnCreate()
        {
            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();
        }

        protected override void OnUpdate()
        {
            Entities.WithAny<GhostComponent>().ForEach((Entity ent, ref HealthComponent health) =>
            {
                // 死亡
                if (health.Hp <= 0)
                {
                    if (!_storeSystem.TryGetValue(ent, out var trans))
                    {
                        return;
                    }

                    trans.GetComponent<MovementController>().motor
                        .SetPosition((Vector3.up * 5) + Random.insideUnitSphere * 2);
                    health.Hp = 100;
                }
            });
        }
    }
}