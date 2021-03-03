using KinematicCharacterController;
using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using MyGameLib.NetCode.Serializer;
using NetCode_Example_Scenes.Scripts.Player;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Samples.NetFPS
{
    public class HybridSnapshotSnapshotCopySystem : SnapshotCopySystem
    {
        private EntityQuery _ownerPredicationQuery;

        private EntityQuery _grenadeQuery;
        private EntityQuery _ballQuery;
        private EntityQuery _d1Query;

        private GameObjectManager _storeSystem;

        protected override void OnCreate()
        {
            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();

            RequireSingletonForUpdate<EnableNetFPS>();

            _ownerPredicationQuery = GetEntityQuery(ComponentType.ReadOnly<GhostPredictionComponent>(),
                ComponentType.ReadWrite<GhostOwnerComponent>(),
                ComponentType.ReadWrite<GhostComponent>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<AbilityMovement>());

            _grenadeQuery = GetEntityQuery(ComponentType.ReadOnly<GrenadeTagComponent>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<NetworkRigidbody>());

            _ballQuery = GetEntityQuery(ComponentType.ReadOnly<SphereTagComponent>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<NetworkRigidbody>());


            _d1Query = GetEntityQuery(ComponentType.ReadOnly<D1Tag>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>());
        }

        protected override void OnUpdate()
        {
            uint tick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;

            Entities.With(_ownerPredicationQuery).ForEach(
                (Entity ent, ref NetworkCharacterComponent state) =>
                {
                    if (!_storeSystem.TryGetValue(ent, out var trans))
                    {
                        return;
                    }

                    var movement = trans.GetComponent<MovementController>();
                    movement.GetState(ref state);
                });

            Entities.With(_grenadeQuery).ForEach(
                (Entity ent, ref Translation pos, ref Rotation rot, ref NetworkRigidbody rig) =>
                {
                    if (!_storeSystem.TryGetValue(ent, out var trans))
                    {
                        return;
                    }

                    var r = trans.GetComponent<Rigidbody>();
                    pos.Value = r.position;
                    rot.Value = r.rotation;
                    rig.velocity = r.velocity;
                    rig.angularVelocity = r.angularVelocity;

                    // Debug.Log($"[S] {tick} Copy: {pos.Value} {rig.velocity}");
                });

            Entities.With(_ballQuery).ForEach((Entity ent, ref Translation pos, ref Rotation rot,
                ref NetworkRigidbody rig) =>
            {
                if (!_storeSystem.TryGetValue(ent, out var trans))
                {
                    return;
                }

                var r = trans.GetComponent<Rigidbody>();
                pos.Value = r.position;
                rot.Value = r.rotation;
                rig.velocity = r.velocity;
                rig.angularVelocity = r.angularVelocity;
            });

            Entities.With(_d1Query).ForEach((Entity ent, ref Translation pos, ref Rotation rot) =>
            {
                if (!_storeSystem.TryGetValue(ent, out var trans))
                {
                    return;
                }

                var r = trans.GetComponent<Transform>();
                pos.Value = r.position;
                rot.Value = r.rotation;
            });
        }

        public float Quantization(float f) => (float) ((int) (f * 100)) / 100;
    }


    public class HybridPredictedCopySystem : SnapshotRestoreSystem
    {
        private EntityQuery _ownerPredicationQuery;

        private EntityQuery _grenadeQuery;

        private EntityQuery _ballQuery;
        private EntityQuery _d1Query;

        private GameObjectManager _storeSystem;

        protected override void OnCreate()
        {
            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();

            RequireSingletonForUpdate<EnableNetFPS>();

            _ownerPredicationQuery = GetEntityQuery(ComponentType.ReadOnly<GhostPredictionComponent>(),
                ComponentType.ReadWrite<GhostOwnerComponent>(),
                ComponentType.ReadWrite<GhostComponent>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<AbilityMovement>());

            _grenadeQuery = GetEntityQuery(ComponentType.ReadOnly<GrenadeTagComponent>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<NetworkRigidbody>());

            _ballQuery = GetEntityQuery(ComponentType.ReadOnly<SphereTagComponent>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<NetworkRigidbody>());


            _d1Query = GetEntityQuery(ComponentType.ReadOnly<D1Tag>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>());
        }

        protected override void OnUpdate()
        {
            NetworkSnapshotAckComponent ack = GetSingleton<NetworkSnapshotAckComponent>();
            if (World.GetExistingSystem<GhostPredictionSystemGroup>().LastAppliedSnapshotTick >=
                ack.LastReceivedSnapshotByLocal)
            {
                return;
            }

            Entities.With(_ownerPredicationQuery).ForEach(
                (Entity ent, ref NetworkCharacterComponent state) =>
                {
                    if (!_storeSystem.TryGetValue(ent, out var trans))
                    {
                        return;
                    }

                    var movement = trans.GetComponent<MovementController>();
                    movement.ApplyState(ref state);
                });

            Entities.With(_grenadeQuery).ForEach(
                (Entity ent, ref Translation pos, ref Rotation rot, ref NetworkRigidbody rig) =>
                {
                    if (!_storeSystem.TryGetValue(ent, out var trans))
                    {
                        return;
                    }

                    var r = trans.GetComponent<Rigidbody>();
                    r.position = pos.Value;
                    r.rotation = rot.Value;
                    r.velocity = rig.velocity;
                    r.angularVelocity = rig.angularVelocity;

                    // Debug.Log($"[C] {ack.LastReceivedSnapshotByLocal} Restore: {pos.Value} {rig.velocity}");
                });

            Entities.With(_ballQuery).ForEach(
                (Entity ent, ref Translation pos, ref Rotation rot, ref NetworkRigidbody rig) =>
                {
                    if (!_storeSystem.TryGetValue(ent, out var trans))
                    {
                        return;
                    }

                    var r = trans.GetComponent<Rigidbody>();
                    r.position = pos.Value;
                    r.rotation = rot.Value;
                    r.velocity = rig.velocity;
                    r.angularVelocity = rig.angularVelocity;
                });

            Entities.With(_d1Query).ForEach(
                (Entity ent, ref Translation pos, ref Rotation rot) =>
                {
                    if (!_storeSystem.TryGetValue(ent, out var trans))
                    {
                        return;
                    }

                    var r = trans.GetComponent<Transform>();
                    r.position = pos.Value;
                    r.rotation = rot.Value;
                });
        }
    }

    public class HybridInterpolatedSystem : SnapshotInterpolatedCopySystem
    {
        private EntityQuery _ownerPredicationQuery;
        private GameObjectManager _storeSystem;

        protected override void OnCreate()
        {
            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();

            RequireSingletonForUpdate<EnableNetFPS>();

            _ownerPredicationQuery = GetEntityQuery(ComponentType.Exclude<GhostPredictionComponent>(),
                ComponentType.ReadWrite<GhostOwnerComponent>(),
                ComponentType.ReadWrite<GhostComponent>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<NetworkCharacterComponent>());
        }

        protected override void OnUpdate()
        {
            // 玩家对象 插值
            Entities.With(_ownerPredicationQuery)
                .ForEach((Entity ent,
                    ref NetworkCharacterComponent n) =>
                {
                    if (!_storeSystem.TryGetValue(ent, out var trans))
                    {
                        return;
                    }

                    trans.GetComponent<KinematicCharacterMotor>().SetPositionAndRotation(n.Position, n.Rotation);
                    LinkedPlayerView linkedPlayerView = trans.GetComponent<GameObjectLinked>().Target
                        .GetComponent<LinkedPlayerView>();

                    var rot = Quaternion.Euler(n.AngleH, n.AngleV, 0);

                    linkedPlayerView.Camera.transform.rotation = rot;
                    linkedPlayerView.Camera.transform.position =
                        trans.transform.position + rot * linkedPlayerView.targetCamOffset;

                    linkedPlayerView.Anim.SetFloat("Horizontal", n.AimH);
                    linkedPlayerView.Anim.SetFloat("Vertical", n.AimV);
                });
        }
    }

    /// <summary>
    /// 记录预测数据的系统
    /// </summary>
    [ClientWorld]
    [UpdateInGroup(typeof(EndPredictionAfterSystemGroup))]
    public class PredictionHistorySystem : ComponentSystem
    {
        private GhostPredictionSystemGroup _clientSimulationSystemGroup;
        private GameObjectManager _storeSystem;

        protected override void OnCreate()
        {
            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();
            RequireSingletonForUpdate<CommandTargetComponent>();
            RequireSingletonForUpdate<EnableNetFPS>();
            _clientSimulationSystemGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();
        }

        protected override void OnUpdate()
        {
            var tick = _clientSimulationSystemGroup.PredictingTick;
            Entities.WithAllReadOnly<AbilityMovement>().ForEach((
                Entity entity,
                DynamicBuffer<HistoryStateData> buffer) =>
            {
                if (!_storeSystem.TryGetValue(entity, out var trans))
                {
                    return;
                }

                var motor = trans.GetComponent<MovementController>().motor;
                HistoryStateData stateData = default;
                stateData.Tick = tick;
                stateData.pos = motor.TransientPosition;
                stateData.rot = motor.TransientRotation;
                buffer.AddHistoryStateData(stateData);
            });

            Entities.WithAllReadOnly<SphereTagComponent>().ForEach((
                Entity entity,
                DynamicBuffer<HistoryStateData> buffer) =>
            {
                if (!_storeSystem.TryGetValue(entity, out var trans))
                {
                    return;
                }

                var motor = trans.GetComponent<Rigidbody>();

                HistoryStateData stateData = default;
                stateData.Tick = tick;
                stateData.pos = motor.position;
                stateData.rot = motor.rotation;
                buffer.AddHistoryStateData(stateData);
            });
        }
    }
}