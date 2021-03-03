using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using Samples.MyGameLib.NetCode;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Samples.NetFPS
{
    public struct EnableNetFPS : IComponentData
    {
    }

    [UpdateInWorld(TargetWorld.Default)]
    public class Game : ComponentSystem
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<EnableNetFPS>();
            if (SceneManager.GetActiveScene().name != "NetFPS")
            {
                return;
            }

            EntityManager.CreateEntity(typeof(EnableNetFPS));
        }

        protected override void OnUpdate()
        {
            EntityManager.DestroyEntity(GetSingletonEntity<EnableNetFPS>());

            foreach (World world in World.All)
            {
                if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
                {
                    world.EntityManager.CreateEntity(ComponentType.ReadOnly<EnableNetFPS>());

                    // 初始化网络
                    NetworkEndPoint ep =
                        NetworkEndPoint.Parse(ClientServerBootstrap.Config.IP, ClientServerBootstrap.Config.Port);
                    world.GetExistingSystem<NetworkStreamReceiveSystem>().Connect(ep);
                }

                if (world.GetExistingSystem<ServerSimulationSystemGroup>() != null)
                {
                    world.EntityManager.CreateEntity(ComponentType.ReadOnly<EnableNetFPS>());

                    // 初始化网络
                    NetworkEndPoint ep = NetworkEndPoint.AnyIpv4;
                    ep.Port = ClientServerBootstrap.Config.Port;
                    world.GetExistingSystem<NetworkStreamReceiveSystem>().Listen(ep);
                }
            }
        }
    }

    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    public class GoInGameClientSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<EnableNetFPS>();
        }

        protected override void OnUpdate()
        {
            Entities.WithNone<NetworkStreamInGame>().ForEach((Entity session, ref NetworkIdComponent id) =>
            {
                PostUpdateCommands.AddComponent<NetworkStreamInGame>(session);

                // rpc
                var req = PostUpdateCommands.CreateEntity();
                PostUpdateCommands.AddComponent(req, new GoInGameRequest {value = 100});
                PostUpdateCommands.AddComponent(req, new SendRpcCommandRequestComponent {TargetConnection = session});

                Debug.Log($"[{nameof(EnableNetFPS)}] RPC->GoInGameRequest...");
            });
        }
    }

    [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
    public class GoInGameServerSystem : ComponentSystem
    {
        private bool initScene = false;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<EnableNetFPS>();
        }

        protected override void OnDestroy()
        {
        }

        protected override void OnUpdate()
        {
            Entities.WithNone<SendRpcCommandRequestComponent>().ForEach((Entity reqEnt, ref GoInGameRequest req,
                ref ReceiveRpcCommandRequestComponent reqSrc) =>
            {
                Debug.Log($"[{nameof(EnableNetFPS)}] RPC->GoInGame:" + req.value);
                PostUpdateCommands.DestroyEntity(reqEnt);

                PostUpdateCommands.AddComponent<NetworkStreamInGame>(reqSrc.SourceConnection);

                var collection = GetSingleton<GhostPrefabCollectionComponent>();
                var buffer = EntityManager.GetBuffer<GhostPrefabBuffer>(collection.ServerPrefabs);

                if (!buffer.FindOwner(out GhostPrefabBuffer ownerGhostPrefab))
                {
                    Debug.LogError($"无法找到Owner预制体!!!");
                    return;
                }

                if (!initScene)
                {
                    Entity ball = Entity.Null;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (EntityManager.HasComponent<SphereTagComponent>(buffer[i].Value))
                        {
                            ball = buffer[i].Value;
                        }
                    }

                    Entity d1 = Entity.Null;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (EntityManager.HasComponent<D1Tag>(buffer[i].Value))
                        {
                            d1 = buffer[i].Value;
                        }
                    }

                    EntityManager.Instantiate(ball);
                    EntityManager.Instantiate(d1);
                    initScene = true;
                }

                Entity entity = EntityManager.Instantiate(ownerGhostPrefab.Value);

                // 组件
                var networkId = EntityManager.GetComponentData<NetworkIdComponent>(reqSrc.SourceConnection).Value;
                EntityManager.AddBuffer<InputCommand>(entity);
                EntityManager.AddComponent<PlayerControlledState>(entity);
                EntityManager.AddComponentData(entity, new GhostOwnerComponent {Value = networkId});
                EntityManager.AddComponentData(reqSrc.SourceConnection, new CommandTargetComponent {Target = entity});
            });
        }
    }
}