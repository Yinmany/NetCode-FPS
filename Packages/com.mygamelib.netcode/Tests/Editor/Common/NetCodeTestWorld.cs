using System;
using System.Collections.Generic;
using Unity.Core;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;

namespace MyGameLib.NetCode.Tests
{
    /// <summary>
    /// 创建NetCode测试World
    /// </summary>
    public class NetCodeTestWorld : IDisposable, INetworkStreamDriverConstructor
    {
        private ClientServerBootstrap.State _oldState;
        public World DefaultWorld { get; private set; }
        public World ServerWorld { get; private set; }
        public World ClientWorld { get; private set; }

        public List<string> NetCodeAssemblies = new List<string>();

        public int DriverFixedTime = 16;

        public double ElapsedTime;

        public NetCodeTestWorld()
        {
#if UNITY_EDITOR
            // Not having a default world means RegisterUnloadOrPlayModeChangeShutdown has not been called which causes memory leaks
            DefaultWorldInitialization.DefaultLazyEditModeInitialize();
#endif
            _oldState = ClientServerBootstrap.SystemStates;
            DefaultWorld = new World("NetCodeTest");
        }

        /// <summary>
        /// 基本系统
        /// </summary>
        public void BootstrapBase(params Type[] useSystems)
        {
            NetCodeAssemblies.Add("Unity.Entities,");
            NetCodeAssemblies.Add("Unity.Transforms,");
            Bootstrap(false, useSystems);
        }

        public void Bootstrap(bool includeNetCodeSystems, params Type[] useSystems)
        {
            var systems = new List<Type>();
            if (includeNetCodeSystems)
            {
                var sysList = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);
                foreach (Type sys in sysList)
                {
                    if (sys.Assembly.FullName.StartsWith("MyGameLib.NetCode,") ||
                        sys.Assembly.FullName.StartsWith("Unity.Entities,") ||
                        sys.Assembly.FullName.StartsWith("Unity.Transforms,"))
                    {
                        systems.Add(sys);
                    }
                }
            }

            if (NetCodeAssemblies.Count > 0)
            {
                var sysList = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);
                foreach (var sys in sysList)
                {
                    bool shouldAdd = false;
                    var sysName = sys.Assembly.FullName;
                    foreach (var asm in NetCodeAssemblies)
                    {
                        shouldAdd |= sysName.StartsWith(asm);
                    }

                    if (shouldAdd)
                    {
                        systems.Add(sys);
                    }
                }
            }

            systems.AddRange(useSystems);
            ClientServerBootstrap.GenerateSystemList(systems);

            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(DefaultWorld,
                ClientServerBootstrap.ExplicitDefaultWorldSystems);
        }

        public void CreateWorlds(bool server)
        {
            var oldConstructor = NetworkStreamReceiveSystem.s_DriverConstructor;
            NetworkStreamReceiveSystem.s_DriverConstructor = this;
            if (server)
            {
                ServerWorld = ClientServerBootstrap.CreateServerWorld(DefaultWorld, "ServerTest");
            }

            ClientWorld = ClientServerBootstrap.CreateClientWorld(DefaultWorld, "ClientTest");
            NetworkStreamReceiveSystem.s_DriverConstructor = oldConstructor;
        }

        public void Tick(float dt)
        {
            ElapsedTime += dt;

            DefaultWorld.SetTime(new TimeData(ElapsedTime, dt));
            ServerWorld?.SetTime(new TimeData(ElapsedTime, dt));
            ClientWorld?.SetTime(new TimeData(ElapsedTime, dt));

            ServerWorld?.GetExistingSystem<ServerInitializationSystemGroup>().Update();
            ClientWorld.GetExistingSystem<ClientInitializationSystemGroup>().Update();

            DefaultWorld.GetExistingSystem<ChainServerSimulationSystem>().Update();
            DefaultWorld.GetExistingSystem<ChainClientSimulationSystem>().Update();

            ClientWorld.GetExistingSystem<ClientPresentationSystemGroup>().Update();
        }

        public bool Connect(float dt, int maxSteps = 4)
        {
            var ep = NetworkEndPoint.LoopbackIpv4;
            ep.Port = 7979;
            ServerWorld.GetExistingSystem<NetworkStreamReceiveSystem>().Listen(ep);
            ClientWorld.GetExistingSystem<NetworkStreamReceiveSystem>().Connect(ep);

            while (TryGetSingletonEntity<NetworkIdComponent>(ClientWorld) == Entity.Null)
            {
                if (maxSteps <= 0)
                    return false;
                --maxSteps;
                Tick(dt);
            }

            return true;
        }

        public Entity TryGetSingletonEntity<T>(World w)
        {
            var query = w.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
            int entCount = query.CalculateEntityCount();
            if (entCount != 1)
                return Entity.Null;
            return query.GetSingletonEntity();
        }

        public T TryGetSingleton<T>(World w) where T : struct, IComponentData
        {
            var query = w.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.GetSingleton<T>();
        }

        public void Dispose()
        {
            ServerWorld?.Dispose();
            ClientWorld?.Dispose();
            DefaultWorld?.Dispose();

            ServerWorld = null;
            ClientWorld = null;
            DefaultWorld = null;

            ClientServerBootstrap.SystemStates = _oldState;
        }

        public void CreateClientDriver(World world, out NetworkDriver driver, out NetworkPipeline unreliablePipeline,
            out NetworkPipeline reliablePipeline)
        {
            var reliabilityParams = new ReliableUtility.Parameters {WindowSize = 32};

            var netParams = new NetworkConfigParameter
            {
                maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts,
                connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS,
                disconnectTimeoutMS = NetworkParameterConstants.DisconnectTimeoutMS,
                maxFrameTimeMS = 100,
                fixedFrameTimeMS = DriverFixedTime
            };

            driver = new NetworkDriver(new IPCNetworkInterface(), netParams, reliabilityParams);
            unreliablePipeline = driver.CreatePipeline(typeof(NullPipelineStage));
            reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
        }

        public void CreateServerDriver(World world, out NetworkDriver driver, out NetworkPipeline unreliablePipeline,
            out NetworkPipeline reliablePipeline)
        {
            var reliabilityParams = new ReliableUtility.Parameters {WindowSize = 32};

            var netParams = new NetworkConfigParameter
            {
                maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts,
                connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS,
                disconnectTimeoutMS = NetworkParameterConstants.DisconnectTimeoutMS,
                maxFrameTimeMS = 100,
                fixedFrameTimeMS = DriverFixedTime
            };

            driver = new NetworkDriver(new IPCNetworkInterface(), netParams, reliabilityParams);
            unreliablePipeline = driver.CreatePipeline(typeof(NullPipelineStage));
            reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
        }
    }
}