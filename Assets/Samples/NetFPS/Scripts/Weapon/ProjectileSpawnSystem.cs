using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using NetCode_Example_Scenes.Scripts.Player;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace NetCodeExample.Scripts.Weapon
{
    [UpdateInGroup(typeof(GhostSpawnSystemGroup))]
    public class ProjectileSpawnSystem : ComponentSystem
    {
        private GameObjectManager _gameObjectManager;

        protected override void OnCreate()
        {
            _gameObjectManager = World.GetOrCreateSystem<GameObjectManager>();
        }

        protected override void OnUpdate()
        {
            // 没有Transform的实体，需要进行Mono这边的创建
            Entities.WithNone<Transform>().ForEach((Entity ent,
                ref Translation translation,
                ref Rotation rotation,
                ref ProjectileComponent p) =>
            {
                GameObject view =
                    Object.Instantiate(_gameObjectManager.GetPrefab(0));

                view.transform.position = translation.Value;
                view.transform.rotation = rotation.Value;

                Projectile projectile = view.GetComponent<Projectile>();
                projectile.SelfEntity = ent;
                projectile.CamerForword = p.CamerForword;
                projectile.CamerPos = p.CamerPos;
                projectile._physicsScene = World.GetLinkedScene().GetPhysicsScene();
                projectile.World = World;
                projectile.IsServer = World.GetExistingSystem<ServerSimulationSystemGroup>() != null;
                projectile.OnShoot();

                EntityManager.AddComponentObject(ent, view.transform);
            });
        }
    }

    // 子弹不回滚
    [UpdateInGroup(typeof(TickSimulationSystemGroup))]
    public class ProjectileSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.WithAny<ProjectileComponent>().ForEach((Entity ent, Transform transform) =>
            {
                transform.GetComponent<Projectile>().Simulate(Time.DeltaTime);
            });
        }
    }
}