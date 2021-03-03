using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine;

namespace MyGameLib.NetCode
{
    [UpdateInGroup(typeof(FixedSimulationSystemGroup))]
#if UNITY_SERVER
    [UpdateBefore(typeof(BuildPhysicsWorld))]
#endif
    public class PhysicsSimulationSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            UnityEngine.Physics.autoSimulation = false;
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((ref PhysicsSceneComponent p) =>
            {
                p.physicsScene.Simulate(Time.DeltaTime);
            });
        }
    }
}