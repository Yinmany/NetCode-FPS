using Unity.Entities;
using Unity.Transforms;

namespace MyGameLib.NetCode
{
    [UpdateInGroup(typeof(TickSimulationSystemGroup))]
    public class GhostSimulationSystemGroup : ComponentSystemGroup
    {
    }

    [ClientServerWorld]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    public class GhostSpawnSystemGroup : ComponentSystemGroup
    {
    }

    // [UpdateAfter(typeof(GhostSimulationSystemGroup))]
    [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    public class FixedSimulationSystemGroup : ComponentSystemGroup
    {
    }
}