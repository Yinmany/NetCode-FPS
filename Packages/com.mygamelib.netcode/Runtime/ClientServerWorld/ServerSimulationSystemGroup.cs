using Unity.Core;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;

namespace MyGameLib.NetCode
{
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    [ServerWorld]
    public class ServerInitializationSystemGroup : InitializationSystemGroup
    {
    }

    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    [ServerWorld]
    public class ServerSimulationSystemGroup : SimulationSystemGroup
    {
#if !UNITY_CLIENT || UNITY_SERVER || UNITY_EDITOR
        internal ChainServerSimulationSystem ParentChainSystem;
        protected override void OnDestroy()
        {
            if (ParentChainSystem != null)
                ParentChainSystem.RemoveSystemFromUpdateList(this);
        }
#endif
        internal struct FixedTimeLoop
        {
            public float accumulatedTime;
            public float fixedTimeStep;
            public int maxTimeSteps;

            public int GetUpdateCount(float deltaTime)
            {
                accumulatedTime += deltaTime;
                int updateCount = (int) (accumulatedTime / fixedTimeStep);
                accumulatedTime %= fixedTimeStep;
                if (updateCount > maxTimeSteps)
                    updateCount = maxTimeSteps;
                return updateCount;
            }
        }

        public uint Tick { get; private set; }
        public float AccumulatedTime => _fixedTimeLoop.accumulatedTime;
        public float FixedTimeStep => _fixedTimeLoop.fixedTimeStep;

        private FixedTimeLoop _fixedTimeLoop;
        private ProfilerMarker _fixedUpdateMarker;

        protected override void OnCreate()
        {
            base.OnCreate();
            Tick = 1;
            _fixedUpdateMarker = new ProfilerMarker("TickFixedStepUpdate");
        }

        protected override void OnUpdate()
        {
            var tickRate = default(ClientServerTickRate);
            if (HasSingleton<ClientServerTickRate>())
            {
                tickRate = GetSingleton<ClientServerTickRate>();
            }

            tickRate.ResolveDefaults();

            var previousTime = Time;

            _fixedTimeLoop.maxTimeSteps = tickRate.MaxSimulationStepsPerFrame;
            _fixedTimeLoop.fixedTimeStep = 1.0f / tickRate.SimulationTickRate;
            int updateCount = _fixedTimeLoop.GetUpdateCount(Time.DeltaTime);
            for (int tickAge = updateCount - 1; tickAge >= 0; --tickAge)
            {
                using (_fixedUpdateMarker.Auto())
                {
                    World.SetTime(
                        new TimeData(previousTime.ElapsedTime - _fixedTimeLoop.accumulatedTime -
                                     _fixedTimeLoop.fixedTimeStep * tickAge, _fixedTimeLoop.fixedTimeStep));
                    base.OnUpdate();
                    ++Tick;
                    if (Tick == 0)
                        ++Tick;
                }
            }

            World.SetTime(previousTime);
#if UNITY_SERVER
            if (tickRate.TargetFrameRateMode != ClientServerTickRate.FrameRateMode.BusyWait)
#else
            if (tickRate.TargetFrameRateMode == ClientServerTickRate.FrameRateMode.Sleep)
#endif
            {
                AdjustTargetFrameRate(tickRate.SimulationTickRate);
            }
        }

        void AdjustTargetFrameRate(int tickRate)
        {
            //如果无头运行，则来回轻推Application.targetFramerate
            //围绕实际帧速率-始终尝试将剩余时间保留为半帧
            //目标是使上述while循环精确计时1次

            // If running as headless we nudge the Application.targetFramerate back and forth
            // around the actual framerate -- always trying to have a remaining time of half a frame
            // The goal is to have the while loop above tick exactly 1 time

            //使用targetFramerate的原因是允许Unity在帧之间休眠
            //减少服务器上的CPU使用率。
            // The reason for using targetFramerate is to allow Unity to sleep between frames
            // reducing cpu usage on server.

            int rate = tickRate;
            if (_fixedTimeLoop.accumulatedTime > 0.75f * _fixedTimeLoop.fixedTimeStep)
                rate += 2; // higher rate means smaller deltaTime which means remaining accumulatedTime gets smaller
            else if (_fixedTimeLoop.accumulatedTime < 0.25f * _fixedTimeLoop.fixedTimeStep)
                rate -= 2; // lower rate means bigger deltaTime which means remaining accumulatedTime gets bigger

            Application.targetFrameRate = rate;
        }
    }

#if !UNITY_CLIENT || UNITY_SERVER || UNITY_EDITOR
    [AlwaysUpdateSystem]
    [UpdateInWorld(TargetWorld.Default)]
    public class ChainServerSimulationSystem : ComponentSystemGroup
    {
        protected override void OnDestroy()
        {
            foreach (var sys in Systems)
            {
                var grp = sys as ServerSimulationSystemGroup;
                if (grp != null)
                    grp.ParentChainSystem = null;
            }
        }
    }
#endif
}