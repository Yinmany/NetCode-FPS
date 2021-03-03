using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using Samples.NetCube;
using Samples.NetFPS;
using Unity.Entities;
using UnityEngine;

namespace Samples.NetCube
{
    /// <summary>
    /// 记录预测数据的系统
    /// </summary>
    [ClientWorld]
    [UpdateInGroup(typeof(EndPredictionAfterSystemGroup))]
    public class PredictionHistorySystem : ComponentSystem
    {
        private GhostPredictionSystemGroup _clientSimulationSystemGroup;
        private GameObjectManager _storeSystem;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<CommandTargetComponent>();
            RequireSingletonForUpdate<EnableNetCube>();
            _clientSimulationSystemGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();
            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();
        }

        protected override void OnUpdate()
        {
            var tick = _clientSimulationSystemGroup.PredictingTick;
            Entities.WithAny<GhostPredictionComponent>().ForEach((
                Entity entity,
                DynamicBuffer<HistoryStateData> buffer) =>
            {
                if (!_storeSystem.TryGetValue(entity, out var go))
                {
                    return;
                }

                var trans = go.GetComponent<Transform>();
                HistoryStateData stateData = default;
                stateData.Tick = tick;
                stateData.pos = trans.position;
                stateData.rot = trans.rotation;
                buffer.AddHistoryStateData(stateData);
            });
        }
    }
}