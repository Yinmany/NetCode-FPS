using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using NetCode_Example_Scenes.Scripts.Player;
using Samples.NetCube;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Samples.NetFPS
{
    /// <summary>
    /// 平滑逻辑物体
    /// </summary>
    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    public class SmoothCharacterMotion : ComponentSystem
    {
        private TickSimulationSystemGroup _tickSimulationSystemGroup;
        private GameObjectManager _storeSystem;

        protected override void OnCreate()
        {
            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();

            _tickSimulationSystemGroup = World.GetOrCreateSystem<TickSimulationSystemGroup>();
            RequireSingletonForUpdate<EnableNetFPS>();
        }

        protected override void OnUpdate()
        {
            Entities.WithAll<GhostPredictionComponent>().ForEach((Entity ent, ref SmoothComponent s) =>
            {
                if (!_storeSystem.TryGetValue(ent, out var trans))
                {
                    return;
                }

                var smooth = trans.GetComponent<GameObjectLinked>().Target;

                var f = _tickSimulationSystemGroup.Interpolate;

                var pos = Vector3.Lerp(s.PreviousPos, s.CurPos, f);
                var rot = Quaternion.Slerp(s.PreviousRot, s.CurRot, f);

                smooth.transform.position = pos;
                smooth.transform.rotation = rot;
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
    public class UpdateCharacterInterpolated : ComponentSystem
    {
        private GhostPredictionSystemGroup _ghostPredictionSystem;
        private GameObjectManager _storeSystem;

        protected override void OnCreate()
        {
            _ghostPredictionSystem = World.GetExistingSystem<GhostPredictionSystemGroup>();
            RequireSingletonForUpdate<EnableNetFPS>();
            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();
        }

        protected override void OnUpdate()
        {
            Entities.WithAll<GhostPredictionComponent>().ForEach(
                (Entity ent,
                    ref SmoothComponent s,
                    DynamicBuffer<HistoryStateData> history) =>
                {
                    if (!_storeSystem.TryGetValue(ent, out var go))
                    {
                        return;
                    }

                    s.PreviousPos = s.CurPos;
                    s.PreviousRot = s.CurRot;

                    // 如果当前这帧倒带过，就要计算误差.
                    if (_ghostPredictionSystem.IsFixError)
                    {
                        // 倒带后，计算客户端回滚前最新位置与重新预测后相同帧的误差.
                        if (history.GetHistoryStateData(_ghostPredictionSystem.PredictingTick - 1, out var data))
                        {
                            // 误差累计
                            s.PosError = s.PreviousPos - (Vector3) data.pos;
                            s.RotError = Quaternion.Inverse(data.rot) * s.PreviousRot;
                        }
                    }

                    // 修复错误
                    s.PosError *= 0.9f;
                    s.RotError = Quaternion.Slerp(s.RotError, quaternion.identity, 0.1f);

                    // 累计误差
                    s.CurPos = go.transform.position + s.PosError;
                    s.CurRot = go.transform.rotation * s.RotError;
                });
        }
    }
}