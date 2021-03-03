using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using Unity.Entities;
using UnityEngine;

namespace Samples.NetFPS
{
    [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    public class D1Motion : ComponentSystem
    {
        private GameObjectManager _gameObjectManager;

        protected override void OnCreate()
        {
            _gameObjectManager = World.GetOrCreateSystem<GameObjectManager>();
        }

        protected override void OnUpdate()
        {
            Entities.WithAllReadOnly<D1Tag>().ForEach((Entity ent) =>
            {
                if (!_gameObjectManager.TryGetValue(ent, out var go))
                {
                    return;
                }

                go.transform.Rotate(Vector3.up, 50f * Time.DeltaTime);
            });
        }
    }
}