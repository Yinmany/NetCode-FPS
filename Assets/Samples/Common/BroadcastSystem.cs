using MyGameLib.NetCode;
using Samples.NetFPS;
using Unity.Collections;
using Unity.Entities;

namespace Samples.MyGameLib.NetCode.Base
{
    [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
    public class BroadcastSystem : ComponentSystem
    {
        private NativeArray<Entity> m_Connections;
        private EntityQuery m_SessionQuery;

        private NativeList<FireRpc> m_FireRpcs;
        private NativeList<GrenadeRpc> _grenadeRpcs;
        public NativeList<ProjectileHit> m_ProjectileHit;

        protected override void OnCreate()
        {
            m_FireRpcs = new NativeList<FireRpc>(16, Allocator.Persistent);
            m_ProjectileHit = new NativeList<ProjectileHit>(16, Allocator.Persistent);
            _grenadeRpcs = new NativeList<GrenadeRpc>(16, Allocator.Persistent);

            m_SessionQuery = GetEntityQuery(
                ComponentType.ReadWrite<NetworkStreamConnection>(),
                ComponentType.ReadOnly<NetworkStreamInGame>(),
                ComponentType.Exclude<NetworkStreamDisconnected>());
        }

        public void Add(FireRpc msg)
        {
            m_FireRpcs.Add(msg);
        }

        public void Add(ProjectileHit hit) => m_ProjectileHit.Add(hit);
        public void Add(GrenadeRpc msg) => _grenadeRpcs.Add(msg);

        protected override void OnDestroy()
        {
            if (m_Connections.IsCreated)
                m_Connections.Dispose();
            if (m_FireRpcs.IsCreated)
                m_FireRpcs.Dispose();
            if (m_ProjectileHit.IsCreated)
                m_ProjectileHit.Dispose();
            if (_grenadeRpcs.IsCreated)
                _grenadeRpcs.Dispose();
        }

        protected override void OnUpdate()
        {
            Entities.With(m_SessionQuery).ForEach(ent =>
            {
                Send(ent, m_FireRpcs);
                Send(ent, m_ProjectileHit);
                Send(ent, _grenadeRpcs);
            });

            m_FireRpcs.Clear();
            m_ProjectileHit.Clear();
            _grenadeRpcs.Clear();
        }

        private void Send<T>(Entity connection, NativeList<T> list) where T : struct, IRpcCommand
        {
            for (int j = 0; j < list.Length; j++)
            {
                T msg = list[j];
                Entity ack = PostUpdateCommands.CreateEntity();
                PostUpdateCommands.AddComponent(ack, new SendRpcCommandRequestComponent
                {
                    TargetConnection = connection
                });

                PostUpdateCommands.AddComponent(ack, msg);
            }
        }
    }
}