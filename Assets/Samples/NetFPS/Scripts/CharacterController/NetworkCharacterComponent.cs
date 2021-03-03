using KinematicCharacterController;
using MyGameLib.NetCode;
using Unity.Entities;
using Unity.Mathematics;

namespace Samples.NetFPS
{
    [GenerateAuthoringComponent]
    [GhostComponent]
    public struct NetworkCharacterComponent : IComponentData
    {
        [GhostField(Interpolate = true)] public float3 Position;
        [GhostField(Interpolate = true)] public quaternion Rotation;
        [GhostField] public float3 BaseVelocity;

        [GhostField] public bool MustUnground;
        [GhostField] public float MustUngroundTime;
        [GhostField] public bool LastMovementIterationFoundAnyGround;

        // public Rigidbody AttachedRigidbody;
        // public float3 AttachedRigidbodyVelocity;

        [GhostField] public bool FoundAnyGround;
        [GhostField] public bool IsStableOnGround;
        [GhostField] public bool SnappingPrevented;
        [GhostField] public float3 GroundNormal;
        [GhostField] public float3 InnerGroundNormal;
        [GhostField] public float3 OuterGroundNormal;

        [GhostField] public float Pitch;
        [GhostField(Interpolate = true)] public float AngleH;
        [GhostField(Interpolate = true)] public float AngleV;

        [GhostField(Interpolate = true)] public float AimH;
        [GhostField(Interpolate = true)] public float AimV;

        public void CopyFrom(KinematicCharacterMotorState state)
        {
            Position = state.Position;
            Rotation = state.Rotation;
            BaseVelocity = state.BaseVelocity;
            MustUnground = state.MustUnground;
            MustUngroundTime = state.MustUngroundTime;
            LastMovementIterationFoundAnyGround = state.LastMovementIterationFoundAnyGround;

            FoundAnyGround = state.GroundingStatus.FoundAnyGround;
            IsStableOnGround = state.GroundingStatus.IsStableOnGround;
            SnappingPrevented = state.GroundingStatus.SnappingPrevented;
            GroundNormal = state.GroundingStatus.GroundNormal;
            InnerGroundNormal = state.GroundingStatus.InnerGroundNormal;
            OuterGroundNormal = state.GroundingStatus.OuterGroundNormal;
        }

        public KinematicCharacterMotorState GetState()
        {
            KinematicCharacterMotorState state = new KinematicCharacterMotorState();
            state.Position = Position;
            state.Rotation = Rotation;
            state.BaseVelocity = BaseVelocity;
            state.MustUnground = MustUnground;
            state.MustUngroundTime = MustUngroundTime;
            state.LastMovementIterationFoundAnyGround = LastMovementIterationFoundAnyGround;
            state.GroundingStatus = new CharacterTransientGroundingReport
            {
                FoundAnyGround = this.FoundAnyGround,
                IsStableOnGround = this.IsStableOnGround,
                SnappingPrevented = this.SnappingPrevented,
                GroundNormal = this.GroundNormal,
                InnerGroundNormal = this.InnerGroundNormal,
                OuterGroundNormal = this.OuterGroundNormal
            };

            return state;
        }
    }
}