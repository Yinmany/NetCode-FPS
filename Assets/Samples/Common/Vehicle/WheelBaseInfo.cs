using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECSCarTest
{
    public struct WheelBaseInfo : IComponentData
    {
        public float MinLength;
        public float MaxLength;
        public float LastLength;
        public float SpringLength;
        public float SpringVelocity;
        public float SpringForce;
        public float DamperForce;

        public float3 SuspensionForce;
        public LayerMask layerMask;
    }
}