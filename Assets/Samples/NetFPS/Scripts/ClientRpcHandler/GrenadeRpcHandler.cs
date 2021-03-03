using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using NetCode_Example_Scenes.Scripts.Player;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Samples.NetFPS
{
    [UpdateInGroup(typeof(GhostUpdateSystemGroup))]
    public class GrenadeRpcHandler : ComponentSystem
    {
        private GhostUpdateSystemGroup m_GhostUpdateSystemGroup;

        private NetworkTimeSystem m_NetworkTime;

        // 等到渲染到那一帧在处理
        private NativeQueue<GrenadeRpc> m_Queue;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<EnableNetFPS>();

            m_GhostUpdateSystemGroup = World.GetOrCreateSystem<GhostUpdateSystemGroup>();
            m_NetworkTime = World.GetOrCreateSystem<NetworkTimeSystem>();
            m_Queue = new NativeQueue<GrenadeRpc>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            m_Queue.Dispose();
        }

        protected override void OnUpdate()
        {
            Entities.WithNone<SendRpcCommandRequestComponent>().ForEach((Entity reqEnt, ref GrenadeRpc msg,
                ref ReceiveRpcCommandRequestComponent reqSrc) =>
            {
                PostUpdateCommands.DestroyEntity(reqEnt);
                m_Queue.Enqueue(msg);
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
                linkedView.GenGrenade(World, msg.Pos, msg.Dir, false);
            }
        }
    }
}