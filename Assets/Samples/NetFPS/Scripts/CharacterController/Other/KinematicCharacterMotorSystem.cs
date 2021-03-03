using KinematicCharacterController;
using MyGameLib.NetCode;
using Unity.Entities;
using UnityEngine;

namespace Samples.NetFPS
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedSimulationSystemGroup))]
    public class KinematicCharacterMotorSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            var tmp = GetEntityQuery(new ComponentType(typeof(KinematicCharacterMotor)));

            RequireForUpdate(tmp);
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((Entity entity, KinematicCharacterMotor motor) => { motor.Simulate(Time.DeltaTime); });
        }
    }

    // 两帧之前的平滑视图
    [DisableAutoCreation]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public class SmoothnessPlayerControlPositionSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            var tmp = GetEntityQuery(new ComponentType(typeof(KinematicCharacterMotor)));

            RequireForUpdate(tmp);
        }

        private float dur = 0;

        protected override void OnUpdate()
        {
            dur += Time.DeltaTime;
            dur %= Time.fixedDeltaTime;

            Entities.ForEach((Entity entity, KinematicCharacterMotor motor) =>
            {
                float interpolationFactor = dur / Time.fixedDeltaTime;
                motor.Transform.SetPositionAndRotation(
                    Vector3.Lerp(motor.InitialTickPosition, motor.TransientPosition, interpolationFactor),
                    Quaternion.Slerp(motor.InitialTickRotation, motor.TransientRotation, interpolationFactor));
            });
        }
    }
}