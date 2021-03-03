using Unity.Mathematics;
using UnityEngine;

namespace Base.Vehicle.Authoring
{
    public class WheelBaseInfoAuthoring : MonoBehaviour
    {
        public float MinLength;
        public float MaxLength;
        public float LastLength;
        public float SpringLength;
        public float SpringVelocity;
        public float SpringForce;
        public float DamperForce;

        public float3 SuspensionForce;
    }
}