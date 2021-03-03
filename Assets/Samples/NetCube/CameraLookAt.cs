using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using NetCode_Example_Scenes.Scripts.Player;
using Unity.Entities;
using UnityEngine;

namespace Samples.NetCube
{
    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    [UpdateAfter(typeof(SmoothCubeMotion))]
    public class CameraLookAt : ComponentSystem
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<EnableNetCube>();
        }

        protected override void OnUpdate()
        {
            var camera = Camera.main.transform;
            Entities.WithAny<LocalPlayerComponent>().ForEach((Transform t) =>
            {
                Transform transform = t.GetComponent<GameObjectLinked>().Target.transform;

                Vector3 pos = camera.position;
                pos.x = transform.position.x;
                camera.position = pos;
            });
        }
    }
}