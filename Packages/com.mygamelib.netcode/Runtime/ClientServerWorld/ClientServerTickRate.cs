using Unity.Entities;

namespace MyGameLib.NetCode
{
    public struct ClientServerTickRate : IComponentData
    {
        public enum FrameRateMode
        {
            Auto,
            BusyWait,
            Sleep
        }

        public int SimulationTickRate;
        public int NetworkTickRate;
        public int MaxSimulationStepsPerFrame;
        public FrameRateMode TargetFrameRateMode;

        public void ResolveDefaults()
        {
            if (SimulationTickRate <= 0)
                SimulationTickRate = 60;
            if (NetworkTickRate <= 0)
                NetworkTickRate = 30;
            if (MaxSimulationStepsPerFrame <= 0)
                MaxSimulationStepsPerFrame = 4;
        }
    }
    
    public struct ClientServerTickRateRefreshRequest : IComponentData
    {
        public int SimulationTickRate;
        public int NetworkTickRate;
        public int MaxSimulationStepsPerFrame;
    }
}