using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using MyGameLib.NetCode.Serializer;
using Samples.MyGameLib.NetCode;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.NetCube
{
    public class NetCubeLinkSystem : GameObjectLinkSystem
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireSingletonForUpdate<EnableNetCube>();
        }
    }

    public class HybridServerCopySystem : SnapshotCopySystem
    {
        private EntityQuery _ownerPredicationQuery;
        private GameObjectManager _storeSystem;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<EnableNetCube>();

            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();

            _ownerPredicationQuery = GetEntityQuery(ComponentType.ReadOnly<GhostPredictionComponent>(),
                ComponentType.ReadWrite<GhostOwnerComponent>(),
                ComponentType.ReadWrite<GhostComponent>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<NetworkRigidbody>());
        }

        protected override void OnUpdate()
        {
            uint tick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;

            Entities.With(_ownerPredicationQuery).ForEach(
                (Entity ent, ref Translation pos, ref Rotation rot, ref NetworkRigidbody r) =>
                {
                    if (!_storeSystem.TryGetValue(ent, out var go))
                    {
                        return;
                    }

                    Rigidbody rigid = go.GetComponent<Rigidbody>();

                    pos.Value = rigid.position;
                    rot.Value = rigid.rotation;

                    r.velocity = rigid.velocity;
                    r.angularVelocity = rigid.angularVelocity;

                    pos.Value.x = Quantization(rigid.position.x);
                    pos.Value.y = Quantization(rigid.position.y);
                    pos.Value.z = Quantization(rigid.position.z);

                    // this.Log($"{tick} Copy {(float3) rigid.position}");
                });
        }

        public float Quantization(float f) => (float) ((int) (f * 100)) / 100;
    }

    public class HybridPredictedCopySystem : SnapshotRestoreSystem
    {
        private EntityQuery _ownerPredicationQuery;
        private GameObjectManager _storeSystem;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<EnableNetCube>();
            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();
            _ownerPredicationQuery = GetEntityQuery(ComponentType.ReadOnly<GhostPredictionComponent>(),
                ComponentType.ReadWrite<GhostOwnerComponent>(),
                ComponentType.ReadWrite<GhostComponent>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<NetworkRigidbody>());
        }

        protected override void OnUpdate()
        {
            NetworkSnapshotAckComponent ack = GetSingleton<NetworkSnapshotAckComponent>();
            if (World.GetExistingSystem<GhostPredictionSystemGroup>().LastAppliedSnapshotTick <
                ack.LastReceivedSnapshotByLocal)
            {
                Entities.With(_ownerPredicationQuery).ForEach(
                    (Entity ent, ref Translation pos, ref Rotation rot, ref NetworkRigidbody r) =>
                    {
                        if (!_storeSystem.TryGetValue(ent, out var go))
                        {
                            return;
                        }

                        Rigidbody rigid = go.GetComponent<Rigidbody>();
                        rigid.position = pos.Value;
                        rigid.rotation = rot.Value;
                        rigid.velocity = r.velocity;
                        rigid.angularVelocity = r.angularVelocity;
                        // Debug.Log($"回滚前的设置值.");
                    });
            }
        }
    }

    public class HybridInterpolatedSystem : SnapshotInterpolatedCopySystem
    {
        private EntityQuery _ownerPredicationQuery;
        private GameObjectManager _storeSystem;

        protected override void OnCreate()
        {
            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();

            RequireSingletonForUpdate<EnableNetCube>();

            _ownerPredicationQuery = GetEntityQuery(ComponentType.Exclude<GhostPredictionComponent>(),
                ComponentType.ReadWrite<GhostOwnerComponent>(),
                ComponentType.ReadWrite<GhostComponent>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>());
        }

        protected override void OnUpdate()
        {
            // 玩家对象 插值
            Entities.With(_ownerPredicationQuery)
                .ForEach((Entity ent,
                    ref GhostOwnerComponent n,
                    ref Translation pos,
                    ref Rotation rot) =>
                {
                    if (!_storeSystem.TryGetValue(ent, out var trans))
                    {
                        return;
                    }

                    trans.GetComponent<Transform>().SetPositionAndRotation(pos.Value, rot.Value);
                });
        }
    }
}