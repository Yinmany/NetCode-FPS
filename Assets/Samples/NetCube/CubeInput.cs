using MyGameLib.NetCode;
using NetCode_Example_Scenes.Scripts.Player;
using Samples.Common;
using Samples.MyGameLib.NetCode;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Samples.NetCube
{
    /// <summary>
    /// 玩家输入系统 客户端
    /// </summary>
    [UpdateInGroup(typeof(GhostUpdateSystemGroup))]
    public class CubeInputSystem : ComponentSystem
    {
        private GameInput _gameInput;

        protected override void OnCreate()
        {
            _gameInput = new GameInput();
            // 必须存在此组件才会更新Update
            RequireSingletonForUpdate<NetworkIdComponent>();
            RequireSingletonForUpdate<EnableNetCube>();
        }

        protected override void OnUpdate()
        {
            _gameInput.Update(Time.DeltaTime);

            // 只会执行一次
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Keyboard.current.f12Key.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            var localPlayer = GetSingleton<CommandTargetComponent>().Target;
            if (localPlayer == Entity.Null)
            {
                // 找到LocalPlayer 
                Entities.ForEach((Entity ent, ref GhostOwnerComponent playerIdComponent) =>
                {
                    if (playerIdComponent.Value != GetSingleton<NetworkIdComponent>().Value) return;
                    // 把localPlayer与Session 关联 , 并添加InputBuffer到PLocalPlayerS很伤
                    PostUpdateCommands.SetComponent(GetSingletonEntity<CommandTargetComponent>(),
                        new CommandTargetComponent() {Target = ent});
                    PostUpdateCommands.AddBuffer<InputCommand>(ent);
                    PostUpdateCommands.AddComponent(ent, new LocalPlayerComponent());
                    Debug.Log($"关联LocalPlayer.");
                });
                return;
            }

            var input = _gameInput.GetInputCommand();
            input.Tick = World.GetExistingSystem<TickSimulationSystemGroup>().ServerTick;
            var inputBuffer = EntityManager.GetBuffer<InputCommand>(localPlayer);
            inputBuffer.AddCommandData(input);
        }
    }
}