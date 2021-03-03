using MyGameLib.NetCode;
using Samples.MyGameLib.NetCode;
using Unity.Entities;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Samples.NetCube
{
    public struct EnableNetCube : IComponentData
    {
    }

    [DefaultWorld]
    public class Game : ComponentSystem
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<EnableNetCube>();
            if (SceneManager.GetActiveScene().name != "NetCube")
            {
                return;
            }

            EntityManager.CreateEntity(typeof(EnableNetCube));
        }

        protected override void OnUpdate()
        {
            EntityManager.DestroyEntity(GetSingletonEntity<EnableNetCube>());

            foreach (World world in World.All)
            {
                if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
                {
                    world.EntityManager.CreateEntity(ComponentType.ReadOnly<EnableNetCube>());

                    // 初始化网络
                    NetworkEndPoint ep =
                        NetworkEndPoint.Parse(ClientServerBootstrap.Config.IP, ClientServerBootstrap.Config.Port);
                    world.GetExistingSystem<NetworkStreamReceiveSystem>().Connect(ep);
                }

                if (world.GetExistingSystem<ServerSimulationSystemGroup>() != null)
                {
                    world.EntityManager.CreateEntity(ComponentType.ReadOnly<EnableNetCube>());

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
            RequireSingletonForUpdate<EnableNetCube>();
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

                Debug.Log($"[{nameof(EnableNetCube)}] RPC->GoInGameRequest...");
            });
        }
    }

    [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
    public class GoInGameServerSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<EnableNetCube>();
        }

        protected override void OnDestroy()
        {
        }

        protected override void OnUpdate()
        {
            Entities.WithNone<SendRpcCommandRequestComponent>().ForEach((Entity reqEnt, ref GoInGameRequest req,
                ref ReceiveRpcCommandRequestComponent reqSrc) =>
            {
                Debug.Log($"[{nameof(EnableNetCube)}] RPC->GoInGame:" + req.value);
                PostUpdateCommands.DestroyEntity(reqEnt);

                PostUpdateCommands.AddComponent<NetworkStreamInGame>(reqSrc.SourceConnection);

                var collection = GetSingleton<GhostPrefabCollectionComponent>();
                var buffer = EntityManager.GetBuffer<GhostPrefabBuffer>(collection.ServerPrefabs);

                if (!buffer.FindOwner(out GhostPrefabBuffer ownerGhostPrefab))
                {
                    Debug.LogError($"无法找到Owner预制体!!!");
                    return;
                }

                Entity entity = EntityManager.Instantiate(ownerGhostPrefab.Value);

                // 组件
                var networkId = EntityManager.GetComponentData<NetworkIdComponent>(reqSrc.SourceConnection).Value;
                EntityManager.AddBuffer<InputCommand>(entity);
                EntityManager.AddComponentData(entity, new GhostOwnerComponent() {Value = networkId});
                EntityManager.AddComponentData(reqSrc.SourceConnection, new CommandTargetComponent {Target = entity});
            });
        }
    }
}