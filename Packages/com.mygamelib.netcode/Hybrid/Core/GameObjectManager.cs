using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MyGameLib.NetCode.Hybrid
{
    /// <summary>
    /// GameObject管理器
    /// </summary>
    [ClientServerWorld]
    [UpdateInGroup(typeof(GhostSpawnSystemGroup))]
    [DisableAutoCreation]
    public class GameObjectManager : ComponentSystem
    {
        private Scene _scene;
        public bool IsServer { get; private set; }

        private GameObject[] Prefabs;

        public GameObject[] ServerPrefabs;
        public GameObject[] ClientInterpolatedPrefabs;
        public GameObject[] ClientPredictedPrefabs;

        public Scene Scene => _scene;

        #region 实例存放

        private Dictionary<Entity, GameObject> _linkeds;

        public GameObject this[Entity key]
        {
            get
            {
                _linkeds.TryGetValue(key, out var go);
                return go;
            }
        }

        public void Add(Entity key, GameObject value) => _linkeds.Add(key, value);
        public void Remove(Entity key) => _linkeds.Remove(key);
        public bool TryGetValue(Entity key, out GameObject value) => _linkeds.TryGetValue(key, out value);

        public delegate void CreatedGameObjectDelegate(int ghostType, GameObject go, GameObjectManager system);

        #endregion

        protected override void OnCreate()
        {
#if UNITY_EDITOR
            // 兼容UnitTest, Editor下不进行混合对象的创建。
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif
            if (World.GetExistingSystem<ClientSimulationSystemGroup>() != null)
                _scene = SceneManager.GetActiveScene();

            if (World.GetExistingSystem<ServerSimulationSystemGroup>() != null)
            {
                _scene = SceneManager.CreateScene(World.Name, new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            }

            Debug.Log($"[Hybrid] CreateScene ==> {World.Name}");
            Entity ent = EntityManager.CreateEntity(ComponentType.ReadOnly<PhysicsSceneComponent>());
            EntityManager.AddComponentData(ent,
                new PhysicsSceneComponent {physicsScene = _scene.GetPhysicsScene()});
            _linkeds = new Dictionary<Entity, GameObject>();
            IsServer = World.IsServer();
        }

        public void InitPrefabs(int len)
        {
            if (Prefabs != null)
            {
                throw new Exception("GameObjectManager.Prefabs以及初始化过了.");
            }

            Prefabs = new GameObject[len];
        }

        public void AddPrefab(int index, GameObject gameObject)
        {
            Prefabs[index] = gameObject;
        }

        public GameObject GetPrefab(int index) => Prefabs[index];
        public GameObject GetByServer(int index) => ServerPrefabs[index];
        public GameObject GetByInterpolated(int index) => ClientInterpolatedPrefabs[index];
        public GameObject GetByPrediction(int index) => ClientPredictedPrefabs[index];

        public void InitGhosts(int len)
        {
            if (ServerPrefabs != null)
            {
                throw new Exception("GameObjectManager.Ghosts以及初始化过了.");
            }

            ServerPrefabs = new GameObject[len];
            ClientInterpolatedPrefabs = new GameObject[len];
            ClientPredictedPrefabs = new GameObject[len];
        }

        public void AddGhostToServer(int index, GameObject gameObject)
        {
            ServerPrefabs[index] = gameObject;
        }

        public void AddGhostToPredicted(int index, GameObject gameObject) => ClientPredictedPrefabs[index] = gameObject;

        public void AddGhostToInterpolated(int index, GameObject gameObject) =>
            ClientInterpolatedPrefabs[index] = gameObject;

        protected override void OnUpdate()
        {
        }


        public GameObject CreateDefaultGhostGameObject(Entity ent,
            CreatedGameObjectDelegate onCreatedGameObject = null,
            CreatedGameObjectDelegate onCreatedLinkTarget = null)
        {
            var ghost = EntityManager.GetComponentData<GhostComponent>(ent);

            bool isInterpolatedClient = !EntityManager.HasComponent<GhostPredictionComponent>(ent);
            bool isLocalOwner = false;

            GameObject view;

            if (IsServer)
            {
                view = this.GetByServer(ghost.GhostType);
            }
            else
            {
                // 客户端频断是否是LocalOwner
                if (HasSingleton<NetworkIdComponent>() && EntityManager.HasComponent<GhostOwnerComponent>(ent))
                {
                    NetworkIdComponent networkIdComponent = GetSingleton<NetworkIdComponent>();
                    GhostOwnerComponent ownerComponent =
                        EntityManager.GetComponentData<GhostOwnerComponent>(ent);

                    // [客户端特有]
                    isLocalOwner = ownerComponent.Value == networkIdComponent.Value;
                }

                // 预制体集合取
                view = isInterpolatedClient
                    ? this.GetByInterpolated(ghost.GhostType)
                    : this.GetByPrediction(ghost.GhostType);
            }

            view = Object.Instantiate(view);

            // Entity标识,以便碰撞检测能找到对应的Entity
            var entityFlag = view.AddComponent<EntityHold>();
            entityFlag.Ent = ent;
            entityFlag.World = World;

            SceneManager.MoveGameObjectToScene(view, World.GetLinkedScene());
            EntityManager.AddComponentData(ent, new GhostGameObjectSystemState());
            this.Add(ent, view);
            onCreatedGameObject?.Invoke(ghost.GhostType, view, this);

            var linkedGameObject = CreateLinkedGameObject(isInterpolatedClient, IsServer, view);
            if (linkedGameObject)
                onCreatedLinkTarget?.Invoke(ghost.GhostType, linkedGameObject, this);

            // 只能由一个此NetworkBehaviour
            NetworkBehaviour networkBehaviours = view.GetComponent<NetworkBehaviour>();
            if (networkBehaviours)
            {
                networkBehaviours.World = World;
                networkBehaviours.SelfEntity = ent;
                networkBehaviours.IsServer = IsServer;
                networkBehaviours.IsOwner = isLocalOwner;
                EntityManager.AddComponentObject(ent, networkBehaviours);
                networkBehaviours.NetworkAwake();
            }

            return view;
        }

        protected virtual GameObject CreateLinkedGameObject(bool isInterpolatedClient, bool isServer, GameObject view)
        {
            // 注入LinkedGameObject
            GameObjectLinked gameObjectLinked = view.GetComponent<GameObjectLinked>();
            if (gameObjectLinked && gameObjectLinked.Target)
            {
                gameObjectLinked.Target = Object.Instantiate(gameObjectLinked.Target);
                SceneManager.MoveGameObjectToScene(gameObjectLinked.Target, World.GetLinkedScene());

                // 插值物体与服务端不需要分离
                if (isInterpolatedClient || isServer)
                {
                    gameObjectLinked.Target.transform.SetParent(view.transform, false);
#if UNITY_EDITOR
                    if (isServer && !gameObjectLinked.IsServerShow)
                    {
                        foreach (var meshRenderer in gameObjectLinked.Target.GetComponentsInChildren<Renderer>(true))
                        {
                            Object.Destroy(meshRenderer);
                        }
                    }
#endif
                }

                var target = gameObjectLinked.Target.AddComponent<GameObjectLinked>();
                target.Target = view;
            }

            return gameObjectLinked == null ? null : gameObjectLinked.Target;
        }
    }
}