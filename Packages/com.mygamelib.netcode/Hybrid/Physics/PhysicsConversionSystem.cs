using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace MyGameLib.NetCode.Hybrid
{
    /// <summary>
    /// 转换为Unity.Physics用作延迟补偿.
    /// 去掉动态组件，使之成为静态Collider，只用做射线检测.
    /// </summary>
    [UpdateInWorld(TargetWorld.Server)]
    [UpdateAfter(typeof(LegacyRigidbodyConversionSystem))]
    public class PhysicsConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Rigidbody body) => Init(body.gameObject));
            Entities.ForEach(
                (PhysicsBodyAuthoring body) => Init(body.gameObject));
        }

        private void Init(GameObject gameObject)
        {
            Entity ent = GetPrimaryEntity(gameObject);
            DstEntityManager.RemoveComponent<PhysicsVelocity>(ent);
            DstEntityManager.RemoveComponent<PhysicsDamping>(ent);
            DstEntityManager.RemoveComponent<PhysicsMass>(ent);
        }
    }

    
    // 不需要了，由同步System来复制
    // [UpdateInWorld(TargetWorld.Server)]
    // [AlwaysUpdateSystem]
    // [UpdateInGroup(typeof(FixedSimulationSystemGroup), OrderFirst = true)]
    // [UpdateBefore(typeof(BuildPhysicsWorld))]
    // public class CopyTransformFromGameObjectSystem : ComponentSystem
    // {
    //     protected override void OnUpdate()
    //     {
    //         Entities.ForEach(
    //             (Transform transform, ref PhysicsCollider collider, ref Translation pos, ref Rotation rot) =>
    //             {
    //                 pos.Value = transform.position;
    //                 rot.Value = transform.rotation;
    //             });
    //     }
    // }
}