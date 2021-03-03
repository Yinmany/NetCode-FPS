using KinematicCharacterController;
using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using NetCode_Example_Scenes.Scripts.Player;
using Unity.Entities;
using UnityEngine;

namespace Samples.NetFPS
{
    [GhostComponent(PrefabType = GhostPrefabType.Server | GhostPrefabType.PredictedClient)]
    public struct AbilityMovement : IComponentData
    {
    }

    [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    public class AbilityMovementSystem : ComponentSystem
    {
        private bool isServer;

        private GameObjectManager _storeSystem;

        protected override void OnCreate()
        {
            isServer = World.GetExistingSystem<ServerSimulationSystemGroup>() != null;

            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();
        }

        protected override void OnUpdate()
        {
            PhysicsScene currentPhysicsScene = World.GetLinkedScene().GetPhysicsScene();
            Entities.WithAnyReadOnly<AbilityMovement>().ForEach(
                (Entity ent, ref PlayerControlledState state) =>
                {
                    if (!_storeSystem.TryGetValue(ent, out var trans))
                    {
                        return;
                    }

                    var motor = trans.GetComponent<KinematicCharacterMotor>();
                    var movement = trans.GetComponent<MovementController>();
                    movement.isServer = isServer;
                    movement.dir = state.Command.Movement;
                    movement.yaw = state.Command.Yaw;
                    movement.pitch = state.Command.Pitch;
                    movement.isJump = state.Command.Jump;
                    movement.isSpeed = state.Command.Speed;

                    motor.physics = currentPhysicsScene;
                    motor.Simulate(Time.DeltaTime);

                    if (state.Command.T && isServer)
                    {
                        motor.SetPosition((Vector3.up * 5) + Random.insideUnitSphere * 2);
                    }

                    // 动画
                    var view = trans.GetComponent<GameObjectLinked>().Target.GetComponent<LinkedPlayerView>();
                    view.Anim.SetFloat("Horizontal", state.Command.Movement.x);
                    view.Anim.SetFloat("Vertical", state.Command.Movement.y);
                });
        }
    }
}