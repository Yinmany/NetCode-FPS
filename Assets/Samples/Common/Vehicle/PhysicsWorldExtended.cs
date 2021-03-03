using UnityEngine;
using Unity.Physics;
using Unity.Mathematics;


namespace Pragnesh.Dots
{
    /// <summary>
    /// Class to Extend Methods To apply Force On RigidBody in Unity Dots
    /// https://github.com/PragneshRathod901/UnityDotsUtility.git
    /// </summary>
    public static class PhysicsWorldExtended
    {
        /// <summary>
        /// Add acceleration to Velocity considering deltaTime (in world space)
        /// </summary>
        /// <param name="world"></param>
        /// <param name="rigidBodyIndex"></param>
        /// <param name="acceleration"></param>
        public static void ApplyLinearAcceleration(this PhysicsWorld world, int rigidBodyIndex, float3 acceleration)
        {
            if (!(0 <= rigidBodyIndex && rigidBodyIndex < world.NumDynamicBodies)) return;

            Unity.Collections.NativeSlice<MotionVelocity> motionVelocities = world.MotionVelocities;
            MotionVelocity mv = motionVelocities[rigidBodyIndex];
            mv.LinearVelocity += acceleration;
            motionVelocities[rigidBodyIndex] = mv;
        }

        /// <summary>
        ///  Add to the angular velocity of a rigid body considering deltaTime (in world space)
        /// </summary>
        /// <param name="world"></param>
        /// <param name="rigidBodyIndex"></param>
        /// <param name="angularVelocity"></param>
        public static void ApplyAngularAcceleration(this PhysicsWorld world, int rigidBodyIndex, float3 angularVelocity)
        {
            if (!(0 <= rigidBodyIndex && rigidBodyIndex < world.NumDynamicBodies)) return;

            MotionData md = world.MotionDatas[rigidBodyIndex];
            float3 angularVelocityMotionSpace = math.rotate(math.inverse(md.WorldFromMotion.rot), angularVelocity);

            Unity.Collections.NativeSlice<MotionVelocity> motionVelocities = world.MotionVelocities;
            MotionVelocity mv = motionVelocities[rigidBodyIndex];
            mv.AngularVelocity += angularVelocityMotionSpace * Time.deltaTime;
            motionVelocities[rigidBodyIndex] = mv;
        }


        /// <summary>
        ///  Apply an Acceleration to a rigid body at a point considering mass (in world space)
        /// </summary>
        /// <param name="world"></param>
        /// <param name="rigidBodyIndex"></param>
        /// <param name="linearImpulse"></param>
        /// <param name="point"></param>
        public static void ApplyAccelerationImpulse(this PhysicsWorld world, int rigidBodyIndex, float3 linearImpulse, float3 point)
        {
            if (!(0 <= rigidBodyIndex && rigidBodyIndex < world.NumDynamicBodies)) return;

            MotionData md = world.MotionDatas[rigidBodyIndex];
            float3 angularImpulseWorldSpace = math.cross(point - md.WorldFromMotion.pos, linearImpulse);
            float3 angularImpulseMotionSpace = math.rotate(math.inverse(md.WorldFromMotion.rot), angularImpulseWorldSpace);

            Unity.Collections.NativeSlice<MotionVelocity> motionVelocities = world.MotionVelocities;
            MotionVelocity mv = motionVelocities[rigidBodyIndex];
            mv.LinearVelocity += (linearImpulse) * Time.deltaTime;
            mv.AngularVelocity += (angularImpulseMotionSpace) * Time.deltaTime;
            motionVelocities[rigidBodyIndex] = mv;
        }

        /// <summary>
        ///  Apply a linear constant force to a rigid body considering deltaTime(in world space)
        /// </summary>
        /// <param name="world"></param>
        /// <param name="rigidBodyIndex"></param>
        /// <param name="linearImpulse"></param>
        public static void ApplyLinearForce(this PhysicsWorld world, int rigidBodyIndex, float3 linearImpulse)
        {
            if (!(0 <= rigidBodyIndex && rigidBodyIndex < world.NumDynamicBodies)) return;

            Unity.Collections.NativeSlice<MotionVelocity> motionVelocities = world.MotionVelocities;
            MotionVelocity mv = motionVelocities[rigidBodyIndex];
            mv.LinearVelocity += (linearImpulse)*mv.InverseMass * Time.deltaTime;
            motionVelocities[rigidBodyIndex] = mv;
        }

        /// <summary>
        ///Apply an angular constant force to a rigidBodyIndex considering mass (in world space)
        /// </summary>
        /// <param name="world"></param>
        /// <param name="rigidBodyIndex"></param>
        /// <param name="angularImpulse"></param>
        public static void ApplyAngularForce(this PhysicsWorld world, int rigidBodyIndex, float3 angularImpulse)
        {
            if (!(0 <= rigidBodyIndex && rigidBodyIndex < world.NumDynamicBodies)) return;

            MotionData md = world.MotionDatas[rigidBodyIndex];
            float3 angularImpulseInertiaSpace = math.rotate(math.inverse(md.WorldFromMotion.rot), angularImpulse);

            Unity.Collections.NativeSlice<MotionVelocity> motionVelocities = world.MotionVelocities;
            MotionVelocity mv = motionVelocities[rigidBodyIndex];
            mv.AngularVelocity += (angularImpulseInertiaSpace) * mv.InverseMass * Time.deltaTime;
            motionVelocities[rigidBodyIndex] = mv;
        }



    }
}