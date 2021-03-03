using Unity.Entities;
using UnityEngine;
using System;
using System.Reflection;
using Unity.Collections;

namespace MyGameLib.NetCode
{
    /// <summary>
    /// 网络实体
    /// </summary>
    [DisallowMultipleComponent]
    public class  GhostAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
    {
        [Serializable]
        public struct GhostComponentInfo
        {
            public string name;
            public bool interpolatedClient;
            public bool predictedClient;
            public bool server;
        }

        /// <summary>
        /// 客户端实例
        /// </summary>
        public enum ClientInstanceType
        {
            Interpolated,
            Predicted,
            OwnerPredicted
        }

        public ClientInstanceType Type;
        public GhostComponentInfo[] Components = new GhostComponentInfo[0];

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent(entity, ComponentType.ReadOnly<GhostComponent>());
        }
    }

    [UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
    public class GhostAuthoringConversion : GameObjectConversionSystem
    {
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

        protected override void OnUpdate()
        {
            Entities.ForEach((GhostAuthoringComponent ghostAuthoring) =>
            {
                var entity = GetPrimaryEntity(ghostAuthoring);
                var target = GetConversionTarget(this.DstEntityManager.World);

                var prefagType = GhostPrefabType.All;

                // 客户端和服务端都需要此组件标识
                if (ghostAuthoring.Type != GhostAuthoringComponent.ClientInstanceType.Interpolated)
                {
                    DstEntityManager.AddComponent<GhostPredictionComponent>(entity);
                }

                if (target == TargetWorld.Server)
                {
                    prefagType = GhostPrefabType.Server;
                }
                else if (target == TargetWorld.Client)
                {
                    if (ghostAuthoring.Type != GhostAuthoringComponent.ClientInstanceType.Interpolated)
                    {
                        prefagType = GhostPrefabType.PredictedClient;
                        DstEntityManager.AddComponentData(entity, new GhostPredictionSmoothComponent());
                    }
                    else
                    {
                        prefagType = GhostPrefabType.InterpolatedClient;
                    }

                    // 都需要快照Buffer
                    DstEntityManager.AddComponentData(entity, new SnapshotData());
                    DstEntityManager.AddBuffer<SnapshotDataBuffer>(entity);
                }

                NativeList<ComponentType> removes = new NativeList<ComponentType>(Allocator.Temp);

                NativeArray<ComponentType> types = DstEntityManager.GetComponentTypes(entity);
                foreach (ComponentType type in types)
                {
                    GhostComponentAttribute attr = type.GetManagedType().GetCustomAttribute<GhostComponentAttribute>();
                    if (attr == null) continue;

                    if (!attr.PrefabType.HasFlag(prefagType))
                    {
                        removes.Add(type);
                    }
                }

                types.Dispose();

                foreach (ComponentType componentType in removes)
                {
                    DstEntityManager.RemoveComponent(entity, componentType);
                }

                removes.Dispose();
            });
        }
    }
}