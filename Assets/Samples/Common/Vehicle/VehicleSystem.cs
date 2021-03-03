using Base.Vehicle.Authoring;
using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ECSCarTest
{
    [UpdateInGroup(typeof(FixedSimulationSystemGroup))]
    public class VehicleSystem : ComponentSystem
    {
        // [ReadOnly] public readonly float drift = 0F;

        protected override void OnUpdate()
        {
            Scene scene = World.GetLinkedScene();
            PhysicsScene physicsScene = default;
            if (scene.IsValid())
            {
                physicsScene = scene.GetPhysicsScene();
            }
            else
            {
                physicsScene = Physics.defaultPhysicsScene;
            }

            var time = Time.DeltaTime;
            Entities.ForEach((Transform root, Rigidbody rigidbody, Vehicle vehicle) =>
            {
                foreach (Transform child in root)
                {
                    F(physicsScene, time, root, rigidbody, child, vehicle);
                }
            });
        }

        private void F(PhysicsScene physicsScene, float deltaTime, Transform root, Rigidbody rigidbody, Transform wheel,
            Vehicle vehicle)
        {
            WheelBaseConfigAuthoring wbc = wheel.GetComponent<WheelBaseConfigAuthoring>();
            WheelBaseInfoAuthoring wbi = wheel.GetComponent<WheelBaseInfoAuthoring>();

            float drift = 0F;
            wbi.MaxLength = wbc.RestLength + wbc.SpringTravel;
            wbi.MinLength = wbc.RestLength - wbc.SpringTravel;

            RaycastInput input = new RaycastInput
            {
                Start = wheel.position,
                End = root.transform.up * -1,
            };

            // Debug.DrawRay(input.Start, input.End * wbi.MaxLength, Color.red);
            if (!physicsScene.Raycast(input.Start, input.End, out var hit, wbi.MaxLength, wbc.layerMask)) return;

            wbi.LastLength = wbi.SpringLength;
            wbi.SpringLength = hit.distance;
            wbi.SpringLength = math.clamp(wbi.SpringLength, wbi.MinLength, wbi.MaxLength);
            wbi.SpringVelocity = (wbi.LastLength - wbi.SpringLength) / deltaTime;

            wbi.SpringForce = wbc.SpringStiffness * (wbc.RestLength - wbi.SpringLength);
            wbi.DamperForce = wbc.DamperStiffness * wbi.SpringVelocity;
            wbi.SuspensionForce = (wbi.SpringForce + wbi.DamperForce) * rigidbody.transform.up;

            rigidbody.AddForceAtPosition(wbi.SuspensionForce, wheel.position);

            //=========================================================================================================
            quaternion rootRotation = root.rotation;

            var up = math.mul(rootRotation, new float3(0, 1, 0));
            var forward = math.mul(rootRotation, new float3(0, 0, 1));

            var v = vehicle.vehicleInput.v;

            rigidbody.AddForceAtPosition(forward * (1000F) * v,
                hit.point + (wheel.position - hit.point) / 4f);

            var h = vehicle.vehicleInput.h;
            rigidbody.AddTorque(h * up * vehicle.turn);

            var linearVelocity = rigidbody.velocity;
            var angularVelocity = rigidbody.angularVelocity;

            float3 localAngleVelocity = root.InverseTransformVector(angularVelocity);
            localAngleVelocity.y *= 0.9f + (drift / 10);
            angularVelocity = root.TransformVector(localAngleVelocity);

            Vector3 localVelocity = root.InverseTransformVector(linearVelocity);
            localVelocity.x *= 0.9f + (drift / 10);
            linearVelocity = root.TransformVector(localVelocity);

            rigidbody.velocity = linearVelocity;
            rigidbody.angularVelocity = angularVelocity;
        }
    }
}