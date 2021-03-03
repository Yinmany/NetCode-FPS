using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using NetCode_Example_Scenes.Scripts.Player;
using Samples.Common;
using Samples.MyGameLib.NetCode;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Samples.NetFPS
{
    /// <summary>
    /// 玩家输入系统 客户端
    /// </summary>
    [UpdateInGroup(typeof(GhostUpdateSystemGroup))]
    public class FPSInputSystem : ComponentSystem
    {
        private GameInput _gameInput;
        private GameObjectManager _storeSystem;

        protected override void OnCreate()
        {
            _gameInput = new GameInput();

            // 必须存在此组件才会更新Update
            RequireSingletonForUpdate<NetworkIdComponent>();
            RequireSingletonForUpdate<EnableNetFPS>();

            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();
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
                    EntityManager.SetComponentData(GetSingletonEntity<CommandTargetComponent>(),
                        new CommandTargetComponent() {Target = ent});
                    EntityManager.AddBuffer<InputCommand>(ent);
                    EntityManager.AddComponent<PlayerControlledState>(ent);
                    EntityManager.AddComponent<LocalPlayerComponent>(ent);
                    Debug.Log($"关联LocalPlayer.");
                });
                return;
            }

            if (!_storeSystem.TryGetValue(localPlayer, out var trans))
            {
                return;
            }

            var input = _gameInput.GetInputCommand();
            input.Tick = World.GetExistingSystem<TickSimulationSystemGroup>().ServerTick;

            // 开火操作
            if (input.Fire)
            {
                var view = trans.GetComponent<GameObjectLinked>().Target.GetComponent<LinkedPlayerView>();
                input.FirePos = view.bulletPoint;
                input.FireDir = view.bulletRot * Vector3.forward;
            }

            if (input.G || input.R)
            {
                var view = trans.GetComponent<GameObjectLinked>().Target.GetComponent<LinkedPlayerView>();
                input.FirePos = view.grenadePoint;
                input.FireDir = view.grenadeRot * Vector3.forward;
            }

            var inputBuffer = EntityManager.GetBuffer<InputCommand>(localPlayer);
            inputBuffer.AddCommandData(input);
        }
    }
}