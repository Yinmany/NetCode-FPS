using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using NetCode_Example_Scenes.Scripts.Player;
using NetCodeExample.Scripts.Player;
using Samples.MyGameLib.NetCode;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Samples.NetFPS
{
    [UpdateInGroup(typeof(GhostUpdateSystemGroup))]
    public class FireClientSystem : ComponentSystem
    {
        private GhostUpdateSystemGroup m_GhostUpdateSystemGroup;

        private NetworkTimeSystem m_NetworkTime;

        // 等到渲染到那一帧在处理
        private NativeQueue<FireRpc> m_Queue;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<EnableNetFPS>();

            m_GhostUpdateSystemGroup = World.GetOrCreateSystem<GhostUpdateSystemGroup>();
            m_NetworkTime = World.GetOrCreateSystem<NetworkTimeSystem>();
            m_Queue = new NativeQueue<FireRpc>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            m_Queue.Dispose();
        }

        protected override void OnUpdate()
        {
            Entities.WithNone<SendRpcCommandRequestComponent>().ForEach((Entity reqEnt, ref FireRpc req,
                ref ReceiveRpcCommandRequestComponent reqSrc) =>
            {
                PostUpdateCommands.DestroyEntity(reqEnt);
                m_Queue.Enqueue(req);
            });

            uint renderTick = m_NetworkTime.interpolateTargetTick;
            while (m_Queue.Count > 0)
            {
                var msg = m_Queue.Peek();
                if (msg.Tick > renderTick)
                {
                    break;
                }

                m_Queue.Dequeue();

                if (!m_GhostUpdateSystemGroup.GhostMap.TryGetValue(msg.OwnerGId, out GhostEntity item))
                {
                    return;
                }

                int localGhostId = EntityManager
                    .GetComponentData<GhostComponent>(GetSingleton<CommandTargetComponent>().Target).Id;

                // 开枪人收到确认消息
                if (localGhostId == msg.OwnerGId)
                    return;

                Transform mcc = EntityManager.GetComponentObject<Transform>(item.Entity);
                LinkedPlayerView linkedView = mcc.GetComponent<GameObjectLinked>().Target
                    .GetComponent<LinkedPlayerView>();
                linkedView.GenProjectile(World, msg.Pos, msg.Dir, false);
                
                Debug.Log($"收到开枪.");
            }

            NetDebug.Set($"开枪消息队列", m_Queue.Count);

            // 处理命中消息
            Entities.WithNone<SendRpcCommandRequestComponent>().ForEach((Entity reqEnt, ref ProjectileHit hit,
                ref ReceiveRpcCommandRequestComponent reqSrc) =>
            {
                PostUpdateCommands.DestroyEntity(reqEnt);
                if (!m_GhostUpdateSystemGroup.GhostMap.TryGetValue(hit.GId, out GhostEntity item))
                {
                    return;
                }

                var gcv = EntityManager.GetComponentObject<GameObjectLinked>(item.Entity).Target;
                gcv.GetComponent<PlayerHUD>()?.SetValue(hit.Hp / 100f);
                NetDebug.Set($"命中GhostId", $"Id={hit.GId} Hp={hit.Hp}");
            });
        }
    }
}