using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Samples.NetCube
{
    /// <summary>
    /// 平滑逻辑物体
    /// </summary>
    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    public class SmoothCubeMotion : ComponentSystem
    {
        private TickSimulationSystemGroup _tickSimulationSystemGroup;
        private GameObjectManager _storeSystem;
        
        protected override void OnCreate()
        {
            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();
            _tickSimulationSystemGroup = World.GetOrCreateSystem<TickSimulationSystemGroup>();
            RequireSingletonForUpdate<EnableNetCube>();
        }

        protected override void OnUpdate()
        {
            Entities.WithAny<GhostPredictionComponent>().ForEach((Entity ent, ref SmoothComponent s) =>
            {
                if (!_storeSystem.TryGetValue(ent, out var trans))
                {
                    return;
                }
                
                var smooth = trans.GetComponent<GameObjectLinked>().Target;

                float f = _tickSimulationSystemGroup.Interpolate;

                smooth.transform.position =
                    Vector3.Lerp(s.PreviousPos, s.CurPos, f);
                smooth.transform.rotation = Quaternion.Slerp(s.PreviousRot, s.CurRot, f);
            });
        }
    }

    /// <summary>
    /// 两个功能：
    ///     没有回滚时: 记录两帧的数据
    ///        回滚时: 计算预测帧与回滚后数据的误差，并逐渐修复误差。
    /// </summary>
    [ClientWorld]
    [UpdateInGroup(typeof(EndTickSystemGroup))]
    public class UpdateCubeInterpolatedData : ComponentSystem
    {
        private GhostPredictionSystemGroup _ghostPredictionSystem;
        private GameObjectManager _storeSystem;

        protected override void OnCreate()
        {
            _ghostPredictionSystem = World.GetExistingSystem<GhostPredictionSystemGroup>();
            RequireSingletonForUpdate<EnableNetCube>();
            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();
        }

        protected override void OnUpdate()
        {
            Entities.WithAny<GhostPredictionComponent>().ForEach(
                (Entity ent,
                    ref SmoothComponent s,
                    DynamicBuffer<HistoryStateData> history) =>
                {
                    if (!_storeSystem.TryGetValue(ent, out var go))
                    {
                        return;
                    }

                    var trans = go.GetComponent<Transform>();

                    s.PreviousPos = s.CurPos;
                    s.PreviousRot = s.CurRot;

                    // 如果当前这帧倒带过，就要计算误差.
                    if (_ghostPredictionSystem.IsFixError)
                    {
                        // this.Log($"{_ghostPredictionSystem.PredictingTick} CI {(float3) s._previosuPos}");

                        // 倒带后，计算客户端回滚前最新位置与重新预测后相同帧的误差.
                        if (history.GetHistoryStateData(_ghostPredictionSystem.PredictingTick - 1, out var data))
                        {
                            // 25 26 +1
                            // 26 27
                            // 误差累计
                            s.PosError = s.PreviousPos - (Vector3) data.pos;
                            s.RotError = Quaternion.Inverse(data.rot) * s.PreviousRot;
                        }
                    }

                    // 修复错误
                    s.PosError *= 0.9f;
                    s.RotError = Quaternion.Slerp(s.RotError, quaternion.identity, 0.1f);

                    // 累计误差WA
                    s.CurPos = trans.position + s.PosError;
                    s.CurRot = trans.rotation * s.RotError;
                });
        }
    }
}