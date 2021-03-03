using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MyGameLib.NetCode.Hybrid
{
    public class EntityHold : MonoBehaviour
    {
        public Entity Ent;
        public World World;

        private void OnDestroy()
        {
            Ent = Entity.Null;
            World = null;
        }
    }


    [ClientServerWorld]
    [UpdateInGroup(typeof(GhostSpawnSystemGroup))]
    public abstract class GameObjectLinkSystem : ComponentSystem
    {
        public bool IsServer { get; private set; }

        private GameObjectManager _gameObjectManager;

        private EntityQuery _createQuery;
        private EntityQuery _destroyQuery;

        public EntityQuery CreateQuery => _createQuery;

        public EntityQuery DestroyQuery => _destroyQuery;

        protected override void OnCreate()
        {
            IsServer = World.IsServer();
            _gameObjectManager = World.GetOrCreateSystem<GameObjectManager>();

            EntityQueryDesc initFilter = new EntityQueryDesc
            {
                All = new ComponentType[] {typeof(GhostComponent)},
                None = new ComponentType[] {typeof(GhostGameObjectSystemState), typeof(GhostDelaySpawnComponent)}
            };

            EntityQueryDesc destroyFilter = new EntityQueryDesc
            {
                All = new ComponentType[] {typeof(GhostGameObjectSystemState)},
                None = new ComponentType[] {typeof(GhostComponent)}
            };

            _createQuery = GetEntityQuery(initFilter);
            _destroyQuery = GetEntityQuery(destroyFilter);
        }

        protected virtual void OnCreatedGameObject(int ghostType, GameObject view, GameObjectManager system)
        {
        }

        protected virtual void OnCreatedLinkTarget(int ghostType, GameObject view, GameObjectManager system)
        {
        }

        protected override void OnUpdate()
        {
            Entities.With(_createQuery).ForEach((Entity ent) =>
            {
                _gameObjectManager.CreateDefaultGhostGameObject(ent, OnCreatedGameObject, OnCreatedLinkTarget);
            });

            Entities.With(_destroyQuery).ForEach(ent =>
            {
                if (_gameObjectManager.TryGetValue(ent, out var go))
                {
                    Object.Destroy(go);
                    _gameObjectManager.Remove(ent);
                }

                EntityManager.RemoveComponent<GhostGameObjectSystemState>(ent);
            });
        }
    }
}