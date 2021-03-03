using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using Samples.MyGameLib.NetCode;
using Unity.Entities;
using UnityEngine;

namespace Samples.NetCube
{
    /// <summary>
    /// 玩家预测系统
    /// </summary>
    public class MoveCubeSystem : CommandHandleSystem
    {
        private GameObjectManager _storeSystem;
        
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<EnableNetCube>();
            
            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();
        }

        protected override void OnUpdate()
        {
            uint tick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;

            // 获取输入
            Entities.ForEach((Entity ent,
                DynamicBuffer<InputCommand> inputBuffer,
                ref GhostComponent ghostComponent) =>
            {
                if (!inputBuffer.GetDataAtTick(tick, out InputCommand input))
                {
                    if (World.GetExistingSystem<ServerSimulationSystemGroup>() != null)
                    {
                        // this.LogError($"{tick} 输入丢失");
                    }
                }

                if (!_storeSystem.TryGetValue(ent, out var go))
                {
                    return;
                }
                
                Transform transform = go.GetComponent<Transform>();

                if (World.GetExistingSystem<ServerSimulationSystemGroup>() != null)
                {
                    if (input.Fire)
                    {
                        transform.position = Vector3.zero;
                    }
                }
                else
                {
                    // if ( World.GetExistingSystem<GhostPredictionSystemGroup>().IsRewind)
                    // {
                    //     Debug.Log($"回滚中...");
                    // }
                }

                transform.GetComponent<Rigidbody>()
                    .AddForce(new Vector3(input.Movement.x, input.Jump ? 1 : 0, input.Movement.y), ForceMode.Impulse);

                // transform.Translate(new Vector3(input.dir.x, 0, input.dir.y) * 10 * Time.DeltaTime);


                // this.Log($"{tick} Move {(float3) transform.position}");
            });
        }
    }
}