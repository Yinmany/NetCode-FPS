using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace MyGameLib.NetCode
{
    [UpdateInGroup(typeof(TickSimulationSystemGroup), OrderFirst = true)]
    public class BeginTickSystemGroup : ComponentSystemGroup
    {
    }


    [UpdateInGroup(typeof(TickSimulationSystemGroup), OrderLast = true)]
    public class EndTickSystemGroup : ComponentSystemGroup
    {
    }

    [ClientServerWorld]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public class TickSimulationSystemGroup : ComponentSystemGroup
    {
        private NetworkTimeSystem _networkTimeSystem;
        private uint previousServerTick;
        private uint _serverTickFraction = 1;


        private double _mostRecentTime;

        public float ServerTickDeltaTime { get; private set; }

        public uint ServerTick { get; private set; }
        public float Interpolate { get; private set; }

        public bool isServer = false;

        protected override void OnCreate()
        {
            base.OnCreate();
            SortSystems();

            isServer = World.GetExistingSystem<ClientSimulationSystemGroup>() == null;
            if (!isServer)
            {
                _networkTimeSystem = World.GetOrCreateSystem<NetworkTimeSystem>();
            }
        }

        protected override void OnUpdate()
        {
            if (isServer)
            {
                ServerTick = World.GetExistingSystem<ServerSimulationSystemGroup>().Tick;
                base.OnUpdate();
                return;
            }

            var tickRate = default(ClientServerTickRate);
            if (HasSingleton<ClientServerTickRate>())
            {
                tickRate = GetSingleton<ClientServerTickRate>();
            }

            tickRate.ResolveDefaults();
            var previousTime = Time;

            float fixedTimeStep = 1.0f / tickRate.SimulationTickRate;
            ServerTickDeltaTime = fixedTimeStep;

            // 当前预测的服务器帧
            var curServerTick = _networkTimeSystem.predictTargetTick;

            // 需要跑的帧数
            uint deltaTicks = curServerTick - previousServerTick;

            double currentTime = Time.ElapsedTime;
            float networkDeltaTime = Time.DeltaTime;

            if (curServerTick != 0)
            {
                _serverTickFraction = 1;
                var fraction = _networkTimeSystem.subPredictTargetTick;
                if (fraction < 1)
                {
                    currentTime -= fraction * fixedTimeStep;
                }

                networkDeltaTime = fixedTimeStep;
                if (deltaTicks > (uint)tickRate.MaxSimulationStepsPerFrame)
                {
                    deltaTicks = (uint)tickRate.MaxSimulationStepsPerFrame;
                }
            }
            else
            {
                deltaTicks = 1;
            }

            previousServerTick = curServerTick;

            // if (deltaTicks > 1)
            // {
            //     Debug.LogError($"多帧:{deltaTicks}");
            // }

            for (uint i = 0; i < deltaTicks; i++)
            {
                var tickAge = deltaTicks - 1 - i;
                ServerTick = curServerTick - tickAge;
                TimeData timeData = new TimeData(currentTime - fixedTimeStep * tickAge, networkDeltaTime);
                _mostRecentTime = timeData.ElapsedTime;
                World.SetTime(timeData);
                base.OnUpdate();
            }

            World.SetTime(previousTime);

            // this.Interpolate = _networkTimeSystem.subPredictTargetTick / tickRate.SimulationTickRate / fixedTimeStep;
            this.Interpolate = math.clamp((float)(Time.ElapsedTime - _mostRecentTime) / networkDeltaTime, 0, 1);
        }
    }
}