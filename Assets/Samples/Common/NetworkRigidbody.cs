using Unity.Entities;
using Unity.Mathematics;

namespace MyGameLib.NetCode.Serializer
{
    [GhostComponent]
    public struct NetworkRigidbody : IComponentData
    {
        [GhostField] public float3 velocity;
        [GhostField] public float3 angularVelocity;
    }
}