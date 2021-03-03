using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using Samples.MyGameLib.NetCode;
using Unity.Entities;
using UnityEngine;

namespace Samples.NetFPS
{
    public class InputCommandSystem : CommandHandleSystem
    {
        private GhostPredictionSystemGroup _ghostPredictionSystemGroup;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<EnableNetFPS>();

            _ghostPredictionSystemGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();
        }

        protected override void OnUpdate()
        {
            uint tick = _ghostPredictionSystemGroup.PredictingTick;


            // 获取输入
            Entities.WithAllReadOnly<GhostPredictionComponent>().ForEach((Entity ent,
                DynamicBuffer<InputCommand> inputBuffer,
                ref PlayerControlledState state,
                ref GhostComponent ghost) =>
            {
                if (!inputBuffer.GetDataAtTick(tick, out InputCommand input))
                {
                    if (World.IsServer())
                    {
                        // Debug.LogError($"{tick} 输入丢失");
                        return;
                    }
                }

                state.Command = input;
            });
        }
    }
}