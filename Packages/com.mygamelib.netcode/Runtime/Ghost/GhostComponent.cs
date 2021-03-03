using Unity.Entities;

namespace MyGameLib.NetCode
{
    public struct GhostComponent : IComponentData
    {
        public int Id;
        public int GhostType;
        public uint SpawnTick;
    }

    public struct GhostSystemStateComponent : ISystemStateComponentData
    {
        public int ghostId;
        public int ghostTypeIndex;
    }

    public struct PredictedGhostSpawnRequestComponent : IComponentData
    {
    }

    public struct PredictedGhostSpawnPendingComponent : IComponentData
    {
    }
}