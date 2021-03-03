using Unity.Entities;

namespace MyGameLib.NetCode
{
    [UpdateInGroup(typeof(BeginGhostPredictionSystemGroup))]
    public abstract class CommandHandleSystem : ComponentSystem
    {
    }
}