using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Networking.Transport;
using UnityEngine;

namespace MyGameLib.NetCode
{
    public unsafe struct GhostComponentSerializer
    {
        public bool IsUpdateValue;
        public GhostSendType SendType;
        public ComponentType ComponentType;
        public int ComponentSize;
        public int DataSize;
        public PortableFunctionPointer<CopyToSnapshotDelegate> CopyToSnapshot;
        public PortableFunctionPointer<CopyFromSnapshotDelegate> CopyFromSnapshot;
        public PortableFunctionPointer<RestoreFromBackupDelegate> RestoreFromBackup;
        public PortableFunctionPointer<SerializeDelegate> Serialize;
        public PortableFunctionPointer<DeserializeDelegate> Deserialize;

        public delegate void CopyToSnapshotDelegate(IntPtr compPtr, IntPtr dataPtr);

        /// <summary>
        /// 插值
        /// </summary>
        /// <param name="compPtr"></param>
        /// <param name="dataAtTick"></param>
        /// <param name="offset">一个快照中有多个组件，这是组件的偏移。因为在取DataAtTick里面的两个快照时，需要使用。</param>
        public delegate void CopyFromSnapshotDelegate(IntPtr compPtr, IntPtr dataAtTick,
            int offset);

        public delegate void SerializeDelegate(IntPtr snapshotData, ref DataStreamWriter writer,
            ref NetworkCompressionModel compressionModel);

        public delegate void DeserializeDelegate(IntPtr snapshotData, ref DataStreamReader reader,
            ref NetworkCompressionModel compressionModel);

        public delegate void RestoreFromBackupDelegate(IntPtr compPtr, IntPtr backupData);

        public static ref T TypeCast<T>(IntPtr ptr, int offset = 0) where T : struct
        {
            return ref UnsafeUtility.AsRef<T>((ptr + offset).ToPointer());
        }
    }

    [UpdateInWorld(TargetWorld.ClientAndServer)]
    public class GhostCollectionSystem : ComponentSystem
    {
        public struct GhostComponentIndex
        {
            public int ComponentIndex;
        }

        public struct GhostTypeState
        {
            public int FirstComponent;
            public int SnapshotSize;
            public int PredictionOwnerOffset;
            public int NumComponents;
            public GhostSpawnBuffer.Type FallbackPredictionMode;
        }

        struct ComponentComparer : IComparer<GhostComponentSerializer>
        {
            public int Compare(GhostComponentSerializer x, GhostComponentSerializer y)
            {
                if (x.ComponentType < y.ComponentType)
                    return -1;
                if (x.ComponentType > y.ComponentType)
                    return 1;
                return 0;
            }
        }

        private readonly List<GhostComponentSerializer> _pendingGhostComponentSerializers =
            new List<GhostComponentSerializer>();

        public NativeArray<GhostComponentSerializer> Serializers;
        public NativeList<GhostTypeState> GhostTypeCollection;
        public NativeList<GhostComponentIndex> IndexCollection;

        protected override void OnDestroy()
        {
            if (Serializers.IsCreated)
                Serializers.Dispose();

            if (GhostTypeCollection.IsCreated)
                GhostTypeCollection.Dispose();

            if (IndexCollection.IsCreated)
                IndexCollection.Dispose();
        }

        public void Register(GhostComponentSerializer info)
        {
            _pendingGhostComponentSerializers.Add(info);
        }

        protected override void OnUpdate()
        {
            if (!HasSingleton<GhostPrefabCollectionComponent>())
            {
                return;
            }

            if (!Serializers.IsCreated)
            {
                CreateSerializersCollection();
            }

            if (GhostTypeCollection.IsCreated)
                return;

            GhostTypeCollection = new NativeList<GhostTypeState>(16, Allocator.Persistent);
            IndexCollection = new NativeList<GhostComponentIndex>(16, Allocator.Persistent);
            GhostTypeCollection.Clear();
            IndexCollection.Clear();

            var ghostCollection = GetSingleton<GhostPrefabCollectionComponent>();
            var prefabList = EntityManager.GetBuffer<GhostPrefabBuffer>(ghostCollection.ServerPrefabs);

            for (int prefab = 0; prefab < prefabList.Length; prefab++)
            {
                var prefabEntity = prefabList[prefab].Value;
                var fallbackPredictionMode = GhostSpawnBuffer.Type.Interpolated;
                if (prefabList[prefab].PrefabType == GhostPrefabType.PredictedClient)
                    fallbackPredictionMode = GhostSpawnBuffer.Type.Predicted;
                bool isOwnerPredicted = prefabList[prefab].IsOwner;
                var ghostType = new GhostTypeState
                {
                    FirstComponent = IndexCollection.Length,
                    NumComponents = 0,
                    PredictionOwnerOffset = -1,
                    SnapshotSize = 0,
                    FallbackPredictionMode = fallbackPredictionMode
                };

                var components = EntityManager.GetComponentTypes(prefabEntity);
                AddComponents(ref ghostType, in components);
                components.Dispose();

                // 处理Owner的偏移
                if (!isOwnerPredicted)
                    ghostType.PredictionOwnerOffset = -1;
                else
                {
                    ghostType.PredictionOwnerOffset += GlobalConstants.TickSize;
                    // Debug.Log($"ghostType.PredictionOwnerOffset={ghostType.PredictionOwnerOffset}");
                }

                // 留出Tick空间.快照在Client对于每个Ghost来说，都要在每个快照的头上存放Tick。
                ghostType.SnapshotSize += GlobalConstants.TickSize;
                GhostTypeCollection.Add(ghostType);
            }
        }

        /// <summary>
        /// 16字节对齐
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static int SnapshotSizeAligned(int size)
        {
            return (size + 15) & (~15);
        }

        public void AddComponents(ref GhostTypeState ghostType, in NativeArray<ComponentType> components)
        {
            for (int i = 0; i < components.Length; i++)
            {
                // 偏移GhostOwner组件位置
                if (components[i] == ComponentType.ReadWrite<GhostOwnerComponent>())
                    ghostType.PredictionOwnerOffset = ghostType.SnapshotSize;

                for (int j = 0; j < Serializers.Length; j++)
                {
                    var serial = Serializers[j];
                    if (components[i] != serial.ComponentType)
                        continue;

                    ++ghostType.NumComponents;
                    ghostType.SnapshotSize += serial.DataSize;
                    IndexCollection.Add(new GhostComponentIndex
                    {
                        ComponentIndex = j
                    });
                }
            }
        }

        public void CreateSerializersCollection()
        {
            Serializers =
                new NativeArray<GhostComponentSerializer>(_pendingGhostComponentSerializers.Count,
                    Allocator.Persistent);
            for (int i = 0; i < _pendingGhostComponentSerializers.Count; i++)
            {
                Serializers[i] = _pendingGhostComponentSerializers[i];
            }

            Serializers.Sort(default(ComponentComparer));
            _pendingGhostComponentSerializers.Clear();
        }

        public static Entity CreatePredictedSpawnPrefab(EntityManager entityManager, Entity prefab)
        {
            if (entityManager.World.GetExistingSystem<ServerSimulationSystemGroup>() != null)
                return prefab;
            prefab = entityManager.Instantiate(prefab);
            entityManager.AddComponentData(prefab, default(Prefab));
            entityManager.AddComponentData(prefab, default(PredictedGhostSpawnRequestComponent));
            entityManager.AddComponentData(prefab, default(PredictedGhostSpawnPendingComponent));

            return prefab;
        }
    }
}