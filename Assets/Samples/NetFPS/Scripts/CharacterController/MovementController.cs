using System;
using KinematicCharacterController;
using Unity.Mathematics;
using UnityEngine;

namespace Samples.NetFPS
{
    public class MovementController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor motor;

        [NonSerialized] public bool isServer;
        [NonSerialized] public float2 dir;
        [NonSerialized] public bool isSpeed;
        [NonSerialized] public bool isJump;

        /// <summary>
        /// pitch围绕x轴旋转
        /// yaw围绕y轴旋转
        /// </summary>
        [NonSerialized] public float pitch, yaw;

        [Serializable]
        public class Config
        {
            public float gravity = -19.62f;
            public float speed = 4f;
            public float jumpHeight = 2f;
        }

        public Config config;

        private void Awake()
        {
            this.motor = GetComponent<KinematicCharacterMotor>();
            this.motor.CharacterController = this;
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (math.lengthsq(dir) > 0) // 进行旋转
            {
                currentRotation = Quaternion.Euler(0, yaw, 0);
            }
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround)
            {
                Vector3 move = motor.CharacterRight * dir.x + motor.CharacterForward * dir.y;
                move *= config.speed * (isSpeed ? 2 : 1);

                if (isJump)
                {
                    motor.ForceUnground(0);
                    move.y = Mathf.Sqrt(config.jumpHeight * -2 * config.gravity);
                }

                currentVelocity = move;
            }
            else
            {
                currentVelocity.y += config.gravity * deltaTime;
            }

            // linkedView.Anim.SetFloat("Horizontal", _input.Movement.x);
            // linkedView.Anim.SetFloat("Vertical", _input.Movement.y);
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        public void PostGroundingUpdate(float deltaTime)
        {
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            Vector3 atCharacterPosition,
            Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
            
        }

        public void GetState(ref NetworkCharacterComponent state)
        {
            state.AngleH = this.pitch;
            state.AngleV = this.yaw;
            state.AimH = this.dir.x;
            state.AimV = this.dir.y;

            state.CopyFrom(motor.GetState());
        }

        public void ApplyState(ref NetworkCharacterComponent state)
        {
            this.motor.ApplyState(state.GetState());
        }
    }
}