using System;
using Unity.Entities;
using UnityEngine;

namespace MyGameLib.NetCode
{
    public struct GhostSpawnQueueComponent : IComponentData
    {
    }

    public struct GhostSpawnBuffer : IBufferElementData
    {
        public enum Type
        {
            Unknown,
            Interpolated,
            Predicted
        }

        public Type SpawnType;
        public int GhostType;
        public int GhostId;
        public int DataOffset;
        public uint ClientSpawnTick;
        public uint ServerSpawnTick;
        public Entity PredictedSpawnEntity;
    }

    [ClientWorld]
    [UpdateInGroup(typeof(GhostSpawnSystemGroup))]
    [UpdateBefore(typeof(GhostSpawnSystem))]
    public class GhostSpawnClassificationSystem : ComponentSystem
    {
        private GhostCollectionSystem _ghostCollectionSystem;
        public static bool OwnerInterpolate = true;
        public static PredictionOwnerDelegate OverridePredictionOwner;

        public delegate void PredictionOwnerDelegate(int netId, int ownerId, ref GhostSpawnBuffer ghost);

        protected override void OnCreate()
        {
            _ghostCollectionSystem = World.GetOrCreateSystem<GhostCollectionSystem>();
            var ent = EntityManager.CreateEntity();
            EntityManager.AddComponentData(ent, default(GhostSpawnQueueComponent));
            EntityManager.AddBuffer<GhostSpawnBuffer>(ent);
            EntityManager.AddBuffer<SnapshotDataBuffer>(ent);
            RequireSingletonForUpdate<NetworkIdComponent>();
        }

        protected override unsafe void OnUpdate()
        {
            if (!_ghostCollectionSystem.GhostTypeCollection.IsCreated)
                return;

            var ghostTypes = _ghostCollectionSystem.GhostTypeCollection;
            var networkId = GetSingleton<NetworkIdComponent>().Value;

            Entities
                .WithAll<GhostSpawnQueueComponent>()
                .ForEach((DynamicBuffer<GhostSpawnBuffer> ghosts, DynamicBuffer<SnapshotDataBuffer> data) =>
                {
                    for (var i = 0; i < ghosts.Length; i++)
                    {
                        var ghost = ghosts[i];
                        if (ghost.SpawnType != GhostSpawnBuffer.Type.Unknown) continue;

                        ghost.SpawnType = ghostTypes[ghost.GhostType].FallbackPredictionMode;
                        if (ghostTypes[ghost.GhostType].PredictionOwnerOffset != -1)
                        {
                            var dataPtr = (byte*) data.GetUnsafePtr();
                            dataPtr += ghost.DataOffset;
                            int ownerId = *(int*) (dataPtr + ghostTypes[ghost.GhostType].PredictionOwnerOffset);
                            // Debug.Log($"{ownerId} == {networkId} [{ghost.GhostType},{ghostTypes[ghost.GhostType].PredictionOwnerOffset}]");
                            if (ownerId == networkId)
                                ghost.SpawnType = GhostSpawnBuffer.Type.Predicted;
                            else if (OwnerInterpolate)
                            {
                                ghost.SpawnType = GhostSpawnBuffer.Type.Interpolated;
                            }

                            OverridePredictionOwner?.Invoke(networkId, ownerId, ref ghost);
                        }

                        ghosts[i] = ghost;
                    }
                });
        }
    }
}