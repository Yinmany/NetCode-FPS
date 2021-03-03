using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using MyGameLib.NetCode.Serializer;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Samples.NetFPS
{
    [ClientWorld]
    [UpdateInGroup(typeof(GhostSpawnSystemGroup))]
    [UpdateBefore(typeof(GhostSpawnSystem))]
    [UpdateAfter(typeof(GhostSpawnClassificationSystem))]
    public class GrenadeGhostSpawnSystem : ComponentSystem
    {
        private BeginSimulationEntityCommandBufferSystem _barrier;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<GhostSpawnQueueComponent>();
            RequireSingletonForUpdate<PredictedGhostSpawnList>();

            _barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var spawnListEnt = GetSingletonEntity<PredictedGhostSpawnList>();
            var spawnList = EntityManager.GetBuffer<PredictedGhostSpawn>(spawnListEnt);

            var commandBuffer = _barrier.CreateCommandBuffer();

            Entities.ForEach((DynamicBuffer<GhostSpawnBuffer> ghosts, DynamicBuffer<SnapshotDataBuffer> data) =>
            {
                for (int i = 0; i < ghosts.Length; i++)
                {
                    var ghost = ghosts[i];
                    if (ghost.SpawnType == GhostSpawnBuffer.Type.Predicted)
                    {
                        for (int j = 0; j < spawnList.Length; j++)
                        {
                            if (ghost.GhostType == spawnList[j].GhostType)
                            {
                                ghost.PredictedSpawnEntity = spawnList[j].Entity;
                                spawnList[j] = spawnList[spawnList.Length - 1];
                                spawnList.RemoveAt(spawnList.Length - 1);

                                commandBuffer.RemoveComponent<PredictedGhostSpawnPendingComponent>(
                                    ghost.PredictedSpawnEntity);
                                break;
                            }
                        }

                        ghosts[i] = ghost;
                    }
                }
            });
        }
    }

    [ClientWorld]
    [UpdateInGroup(typeof(EndTickSystemGroup))]
    public class GrenadeSnapshotLocalCopy : ComponentSystem
    {
        protected override void OnUpdate()
        {
            var store = World.GetOrCreateSystem<GameObjectManager>();

            uint tick = World.GetOrCreateSystem<GhostPredictionSystemGroup>().PredictingTick;

            Entities.WithAllReadOnly<PredictedGhostSpawnPendingComponent>().ForEach((Entity ent, ref Translation trans,
                ref Rotation rot, ref NetworkRigidbody rigid) =>
            {
                if (!store.TryGetValue(ent, out GameObject view))
                {
                    return;
                }

                trans.Value = view.transform.position;
                rot.Value = view.transform.rotation;
                rigid.velocity = view.GetComponent<Rigidbody>().velocity;
                rigid.angularVelocity = view.GetComponent<Rigidbody>().angularVelocity;

                // Debug.Log($"[C] [{tick}] 手雷本地记录:{trans.Value} {rigid.velocity}");
            });
        }
    }

    [ClientWorld]
    [UpdateInGroup(typeof(EndPredictionAfterSystemGroup))]
    public class GrenadeIgnoreRestore : ComponentSystem
    {
        private GhostPredictionSystemGroup _ghostPredictionSystemGroup;

        protected override void OnCreate()
        {
            _ghostPredictionSystemGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();
        }

        protected override void OnUpdate()
        {
            if (!_ghostPredictionSystemGroup.IsRewind)
            {
                return;
            }

            var store = World.GetOrCreateSystem<GameObjectManager>();
            uint tick = _ghostPredictionSystemGroup.PredictingTick;
            Entities.WithAllReadOnly<PredictedGhostSpawnPendingComponent>().ForEach((Entity ent,
                DynamicBuffer<SnapshotDataBuffer> buffer,
                ref SnapshotData snapshotData,
                ref Translation trans,
                ref Rotation rot,
                ref NetworkRigidbody rigid) =>
            {
                uint latestTick = snapshotData.GetLatestTick(buffer);
                if (tick > latestTick)
                {
                    return;
                }

                if (!store.TryGetValue(ent, out var go))
                {
                    return;
                }

                var rig = go.GetComponent<Rigidbody>();
                rig.transform.position = trans.Value;
                rig.transform.rotation = rot.Value;
                rig.velocity = rigid.velocity;
                rig.angularVelocity = rigid.angularVelocity;
            });
        }
    }
}