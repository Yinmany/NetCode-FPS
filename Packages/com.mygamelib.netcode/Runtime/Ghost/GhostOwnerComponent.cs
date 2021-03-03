using Unity.Entities;

namespace MyGameLib.NetCode
{
    [GhostComponent(IsUpdateValue = true)]
    public struct GhostOwnerComponent : IComponentData
    {
        [GhostField] public int Value;
    }
}