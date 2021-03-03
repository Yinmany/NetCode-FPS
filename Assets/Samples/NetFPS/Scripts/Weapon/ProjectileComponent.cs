using Unity.Entities;
using Unity.Mathematics;

namespace NetCodeExample.Scripts.Weapon
{
    public struct ProjectileComponent : IComponentData
    {
        public float3 CamerPos;
        public float3 CamerForword;
    }
}