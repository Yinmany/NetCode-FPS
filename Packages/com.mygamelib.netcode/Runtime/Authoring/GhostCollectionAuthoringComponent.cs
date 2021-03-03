using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace MyGameLib.NetCode
{
    /// <summary>
    /// Ghost集合
    /// </summary>
    [DisallowMultipleComponent]
    public class GhostCollectionAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity,
        IDeclareReferencedPrefabs
    {
        [FormerlySerializedAs("GhostCollections")]
        public GhostCollectionConfig ghostCollectionConfig;

        [NonSerialized] private GhostCollectionConfig.Ghost[] _enabledGhosts = null;

        public GhostCollectionConfig.Ghost[] EnabledGhosts => _enabledGhosts;

        public Type FindComponentWithName(string name)
        {
            var allTypes = TypeManager.GetAllTypes();
            foreach (TypeManager.TypeInfo componentType in allTypes)
            {
                if (componentType.Type != null && componentType.Type.Name == name)
                {
                    return componentType.Type;
                }
            }

            return null;
        }

        public static TargetWorld GetConversionTarget(World world)
        {
            if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
            {
                return TargetWorld.Client;
            }

            if (world.GetExistingSystem<ServerSimulationSystemGroup>() != null)
            {
                return TargetWorld.Server;
            }

            return TargetWorld.Default;
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.AddRange(this.ghostCollectionConfig.Prefabs);
        }

        /// <summary>
        /// 获取可用的Ghosts
        /// </summary>
        /// <returns></returns>
        public GhostCollectionConfig.Ghost[] GetGhosts()
        {
            return this.ghostCollectionConfig.Ghosts.Where(f => f.prefab != null && f.enabled).ToArray();
        }

        public virtual void Convert(Entity entity, EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
        {
            // 客户端与服务端都需要注册序列化器.
            var collection = default(GhostPrefabCollectionComponent);

            var conversionTarget = GetConversionTarget(dstManager.World);

            // 查询出可用的Ghost预制体
            _enabledGhosts = GetGhosts();

            collection.ServerPrefabs = dstManager.CreateEntity();
            var serverPrefabs = new NativeList<GhostPrefabBuffer>(Allocator.Temp);
            int ghostType = 0;
            foreach (var ghost in _enabledGhosts)
            {
                var orig = ghost.prefab.Type;
                Entity ent =
                    GameObjectConversionUtility.ConvertGameObjectHierarchy(ghost.prefab.gameObject,
                        conversionSystem.ForkSettings(1));

                dstManager.SetComponentData(ent, new GhostComponent {GhostType = ghostType});

                GhostPrefabType prefabType = orig == GhostAuthoringComponent.ClientInstanceType.Interpolated
                    ? GhostPrefabType.InterpolatedClient
                    : GhostPrefabType.PredictedClient;

                var ghostPrefab = new GhostPrefabBuffer
                {
                    Value = ent,
                    GhostType = ghostType,
                    PrefabType = prefabType,
                    IsOwner = orig == GhostAuthoringComponent.ClientInstanceType.OwnerPredicted &&
                              ghost.prefab.GetComponent<GhostOwnerAuthoringComponent>()
                };

                NativeArray<ComponentType> componentTypes = dstManager.GetComponentTypes(ent);
                componentTypes.Dispose();

                serverPrefabs.Add(ghostPrefab);
                ++ghostType;
            }

            dstManager.AddBuffer<GhostPrefabBuffer>(collection.ServerPrefabs).AddRange(serverPrefabs);
            serverPrefabs.Dispose();


            //=======================================================================================================
            // 普通的预制体转换后的Entity
            PrefabCollectionSystem prefabCollectionSystem =
                dstManager.World.GetOrCreateSystem<PrefabCollectionSystem>();

            prefabCollectionSystem.Prefabs =
                new NativeArray<PrefabItem>(this.ghostCollectionConfig.Prefabs.Count, Allocator.Persistent);

            for (int i = 0; i < this.ghostCollectionConfig.Prefabs.Count; i++)
            {
                Entity ent = conversionSystem.GetPrimaryEntity(ghostCollectionConfig.Prefabs[i]);


                var item = new PrefabItem
                {
                    Value = ent,
                    Type = i
                };
                prefabCollectionSystem.Prefabs[i] = item;
            }

            // Debug.Log($"转换Prefabs: {prefabCollectionSystem.Prefabs.Length}");
            //=======================================================================================================


            if (conversionTarget == TargetWorld.Client)
            {
                ghostType = 0;
                collection.ClientInterpolatedPrefabs = dstManager.CreateEntity();
                collection.ClientPredictedPrefabs = dstManager.CreateEntity();
                var predictedList = new NativeList<GhostPrefabBuffer>(Allocator.Temp);
                var interpolatedList = new NativeList<GhostPrefabBuffer>(Allocator.Temp);

                foreach (var ghost in _enabledGhosts)
                {
                    var orig = ghost.prefab.Type;

                    ghost.prefab.Type = GhostAuthoringComponent.ClientInstanceType.Interpolated;
                    Entity interpolatedEnt =
                        GameObjectConversionUtility.ConvertGameObjectHierarchy(ghost.prefab.gameObject,
                            conversionSystem.ForkSettings(2));
                    dstManager.SetComponentData(interpolatedEnt, new GhostComponent {GhostType = ghostType});
                    interpolatedList.Add(new GhostPrefabBuffer
                    {
                        Value = interpolatedEnt,
                        GhostType = ghostType,
                        PrefabType = GhostPrefabType.InterpolatedClient
                    });

                    ghost.prefab.Type = GhostAuthoringComponent.ClientInstanceType.Predicted;
                    Entity predictedEnt =
                        GameObjectConversionUtility.ConvertGameObjectHierarchy(ghost.prefab.gameObject,
                            conversionSystem.ForkSettings(2));
                    dstManager.SetComponentData(predictedEnt, new GhostComponent {GhostType = ghostType});
                    predictedList.Add(new GhostPrefabBuffer
                    {
                        Value = predictedEnt,
                        GhostType = ghostType,
                        PrefabType = GhostPrefabType.PredictedClient
                    });

                    ghost.prefab.Type = orig;
                    ++ghostType;
                }

                dstManager.AddBuffer<GhostPrefabBuffer>(collection.ClientPredictedPrefabs).AddRange(predictedList);
                dstManager.AddBuffer<GhostPrefabBuffer>(collection.ClientInterpolatedPrefabs)
                    .AddRange(interpolatedList);

                predictedList.Dispose();
                interpolatedList.Dispose();
            }

            dstManager.AddComponentData(entity, collection);
        }
    }
}