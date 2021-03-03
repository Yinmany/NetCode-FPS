using Unity.Entities;

namespace MyGameLib.NetCode
{
    public struct CommandTargetComponent : IComponentData
    {
        public Entity Target;
    }
}