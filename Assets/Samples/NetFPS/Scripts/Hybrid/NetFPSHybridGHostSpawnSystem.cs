using MyGameLib.NetCode.Hybrid;
using NetCode_Example_Scenes.Scripts.Player;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Samples.NetFPS
{
    public class NetFPSHybridGHostSpawnSystem : GameObjectLinkSystem
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireSingletonForUpdate<EnableNetFPS>();
        }

        protected override void OnCreatedGameObject(int ghostType, GameObject view, GameObjectManager system)
        {
        }

        protected override void OnCreatedLinkTarget(int ghostType, GameObject view, GameObjectManager system)
        {
            if (!system.IsServer && ghostType == 0)
                view.GetComponent<LinkedPlayerView>().ShowHUD();
        }
    }
}