using ECSCarTest;
using MyGameLib.NetCode;
using Pragnesh.Dots;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

namespace DOTSCar
{
    [UpdateInGroup(typeof(FixedSimulationSystemGroup))]
    [UpdateAfter(typeof(BuildPhysicsWorld)), UpdateBefore(typeof(StepPhysicsWorld))]
    [DisableAutoCreation]
    public class DotsVehicleSystem : SystemBase
    {
        BuildPhysicsWorld _createPhysicsWorldSystem;

        EntityQuery carEntityQuery;

        protected override void OnCreate()
        {
            _createPhysicsWorldSystem   = World.GetOrCreateSystem<BuildPhysicsWorld>();

            carEntityQuery = GetEntityQuery(ComponentType.ReadWrite(typeof(WheelBaseConfig)),
                                            ComponentType.ReadOnly(typeof(WheelBaseInfo)),
                                            ComponentType.ReadOnly(typeof(PreviousParent)));
            RequireForUpdate(carEntityQuery);
        }

        struct tmpData
        {
            public Entity        entity;
            public int           parentID;
            public WheelBaseInfo wbi;
            public LocalToWorld  LocalToWorld;
            public Entity        parentEntity;
            public RaycastHit    hit;
        }

        [BurstCompile]
        struct WheelJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Entity>                      Entitys;
            [ReadOnly] public ComponentDataFromEntity<WheelBaseConfig> wbcs;
            [ReadOnly] public ComponentDataFromEntity<PreviousParent>  pps;
            [ReadOnly] public ComponentDataFromEntity<LocalToWorld>    localToWorlds;
            [ReadOnly] public ComponentDataFromEntity<WheelBaseInfo>   wbis;

            [ReadOnly] public PhysicsWorld         physicsWorld;
            [ReadOnly] public float                time;
            public            NativeArray<tmpData> TmpDatas;

            public void Execute(int index)
            {
                var entity       = Entitys[index];
                var pp           = pps[entity];
                var wbc          = wbcs[entity];
                var localToWorld = localToWorlds[entity];
                var wbi          = wbis[entity];


                wbi.MaxLength = wbc.RestLength + wbc.SpringTravel;
                wbi.MinLength = wbc.RestLength - wbc.SpringTravel;

                var rbID   = physicsWorld.GetRigidBodyIndex(pp.Value);
                var filter = physicsWorld.GetCollisionFilter(rbID);

                RaycastInput input = new RaycastInput
                {
                    Start  = localToWorld.Position,
                    End    = localToWorld.Position + localToWorld.Up * wbi.MaxLength * -1,
                    Filter = filter
                };
                if (!physicsWorld.CastRay(input, out var hit)) return;


                wbi.LastLength     = wbi.SpringLength;
                wbi.SpringLength   = math.distance(input.Start, input.End) * hit.Fraction;
                wbi.SpringLength   = math.clamp(wbi.SpringLength, wbi.MinLength, wbi.MaxLength);
                wbi.SpringVelocity = (wbi.LastLength - wbi.SpringLength) / time;

                wbi.SpringForce = wbc.SpringStiffness * (wbc.RestLength - wbi.SpringLength);
                wbi.DamperForce = wbc.DamperStiffness * wbi.SpringVelocity;


                wbi.SuspensionForce = (wbi.SpringForce + wbi.DamperForce) * localToWorld.Up;
                
                // if ((wbi.SpringForce + wbi.DamperForce) < 0)
                // {
                //     wbi.SuspensionForce = float3.zero;
                // }

                TmpDatas[index] = (new tmpData()
                {
                    entity       = entity,
                    wbi          = wbi,
                    parentID     = rbID,
                    LocalToWorld = localToWorld,
                    parentEntity = pp.Value,
                    hit          = hit
                });
            }
        }

        protected override void OnUpdate()
        {
            _createPhysicsWorldSystem.GetOutputDependency().Complete();
            PhysicsWorld world = _createPhysicsWorldSystem.PhysicsWorld;

            var entitys    = this.carEntityQuery.ToEntityArrayAsync(Allocator.TempJob, out var jobhandle);
            var wheelInfos = new NativeArray<tmpData>(entitys.Length, Allocator.TempJob);
            var wheelJob = new WheelJob()
            {
                physicsWorld  = world,
                Entitys       = entitys,
                wbcs          = GetComponentDataFromEntity<WheelBaseConfig>(),
                pps           = GetComponentDataFromEntity<PreviousParent>(),
                localToWorlds = GetComponentDataFromEntity<LocalToWorld>(),
                wbis          = GetComponentDataFromEntity<WheelBaseInfo>(),
                time          = Time.fixedDeltaTime,
                TmpDatas      = wheelInfos
            };
            wheelJob.Schedule(entitys.Length, 80, jobhandle).Complete();
            var drift = 0f;
            for (int i = 0; i < wheelInfos.Length; i++)
            {
                var data = wheelInfos[i];
                if (data.entity != Entity.Null)
                {
                    EntityManager.SetComponentData(data.entity, data.wbi);
                    world.ApplyImpulse(data.parentID, data.wbi.SuspensionForce, data.LocalToWorld.Position);

                    var v = Input.GetAxis("Vertical");
                    world.ApplyImpulse(data.parentID, data.LocalToWorld.Forward * (30) * v, data.hit.Position + (data.LocalToWorld.Position - data.hit.Position) / 10F);
                    var physicsVelocity = EntityManager.GetComponentData<PhysicsVelocity>(data.parentEntity);
                    var position        = EntityManager.GetComponentData<Translation>(data.parentEntity);
                    var rotation        = EntityManager.GetComponentData<Rotation>(data.parentEntity);

                    var h = Input.GetAxis("Horizontal");
                    world.ApplyAngularForce(data.parentID, h * data.LocalToWorld.Up * 7000);

                    var tmp = Matrix4x4.identity;
                    tmp.SetTRS(position.Value, rotation.Value, Vector3.one);

                    var linearVelocity  = world.GetLinearVelocity(data.parentID);
                    var angularVelocity = world.GetAngularVelocity(data.parentID);

                    float3 localAngleVelocity = tmp.inverse.MultiplyVector(angularVelocity);
                    localAngleVelocity.y    *= 0.9f + (drift / 10);
                    physicsVelocity.Angular =  tmp.MultiplyVector(localAngleVelocity);

                    Vector3 localVelocity = tmp.inverse.MultiplyVector(linearVelocity);
                    localVelocity.x        *= 0.9f + (drift / 10);
                    physicsVelocity.Linear =  tmp.MultiplyVector(localVelocity);

                    world.SetAngularVelocity(data.parentID, physicsVelocity.Angular);
                    world.SetLinearVelocity(data.parentID, physicsVelocity.Linear);
                }
            }


            entitys.Dispose();
            wheelInfos.Dispose();
        }
    }
}