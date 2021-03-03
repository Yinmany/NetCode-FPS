using Unity.Entities;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEngine;

namespace MyGameLib.NetCode
{
    [ClientServerWorld]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    public class NetworkReceiveSystemGroup : ComponentSystemGroup
    {
    }

    public interface INetworkStreamDriverConstructor
    {
        void CreateClientDriver(World world, out NetworkDriver driver, out NetworkPipeline unreliablePipeline,
            out NetworkPipeline reliablePipeline);

        void CreateServerDriver(World world, out NetworkDriver driver, out NetworkPipeline unreliablePipeline,
            out NetworkPipeline reliablePipeline);
    }

    [UpdateInGroup(typeof(NetworkReceiveSystemGroup))]
    [AlwaysUpdateSystem]
    public class NetworkStreamReceiveSystem : ComponentSystem, INetworkStreamDriverConstructor
    {
        public static INetworkStreamDriverConstructor s_DriverConstructor;

        public INetworkStreamDriverConstructor DriverConstructor =>
            s_DriverConstructor != null ? s_DriverConstructor : this;

        public NetworkDriver Driver;
        public int ConnectionCount { get; private set; }

        private bool _driverListening;

        public NetworkPipeline UnreliablePipeline => _unreliablePipeline;
        public NetworkPipeline ReliablePipeline => _reliablePipeline;

        private NetworkPipeline _unreliablePipeline;
        private NetworkPipeline _reliablePipeline;
        private int _connectionInstanceId;
        private BeginSimulationEntityCommandBufferSystem _barrier;
        private RpcQueue<RpcSetNetworkId, RpcSetNetworkId> _rpcQueue;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public SimulatorUtility.Parameters ClientSimulatorParameters
        {
            get
            {
                BootstrapConfig bootstrapConfig = ClientServerBootstrap.Config;

                // 延迟
                var packetDelay = bootstrapConfig.ClientPacketDelayMs;
                var jitter = bootstrapConfig.ClientPacketJitterMs;
                if (jitter > packetDelay)
                    jitter = packetDelay;

                var packetDrop = bootstrapConfig.ClientPacketDropRate;
                int networkRate = 60;

                // 所有3种数据包类型每帧存储最大延迟，安全裕量加倍
                int maxPackets = 2 * (networkRate * 3 * packetDelay + 999) / 1000;
                return new SimulatorUtility.Parameters
                {
                    MaxPacketSize = NetworkParameterConstants.MTU,
                    MaxPacketCount = maxPackets,
                    PacketDelayMs = packetDelay,
                    PacketJitterMs = jitter,
                    PacketDropPercentage = packetDrop
                };
            }
        }
#endif

        public void CreateClientDriver(World world, out NetworkDriver driver, out NetworkPipeline unreliablePipeline,
            out NetworkPipeline reliablePipeline)
        {
            var reliabilityParams = new ReliableUtility.Parameters {WindowSize = 32};

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var netParams = new NetworkConfigParameter
            {
                maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts,
                connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS,
                disconnectTimeoutMS = NetworkParameterConstants.DisconnectTimeoutMS,
                maxFrameTimeMS = 100
            };
            var simulatorParams = ClientSimulatorParameters;
            driver = NetworkDriver.Create(netParams, simulatorParams, reliabilityParams);
#else
            driver = NetworkDriver.Create(reliabilityParams);
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (simulatorParams.PacketDelayMs > 0 || simulatorParams.PacketDropInterval > 0)
            {
                unreliablePipeline =
                    driver.CreatePipeline(typeof(SimulatorPipelineStage), typeof(SimulatorPipelineStageInSend));
                reliablePipeline = driver.CreatePipeline(typeof(SimulatorPipelineStageInSend)
                    , typeof(ReliableSequencedPipelineStage), typeof(SimulatorPipelineStage));
            }
            else
#endif
            {
                unreliablePipeline = driver.CreatePipeline(typeof(NullPipelineStage));
                reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            }
        }

        public void CreateServerDriver(World world, out NetworkDriver driver, out NetworkPipeline unreliablePipeline,
            out NetworkPipeline reliablePipeline)
        {
            var reliabilityParams = new ReliableUtility.Parameters {WindowSize = 32};

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var netParams = new NetworkConfigParameter
            {
                maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts,
                connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS,
                disconnectTimeoutMS = NetworkParameterConstants.DisconnectTimeoutMS,
                maxFrameTimeMS = 100
            };
            driver = NetworkDriver.Create(netParams, reliabilityParams);
#else
            driver = NetworkDriver.Create(reliabilityParams);
#endif
            unreliablePipeline = driver.CreatePipeline(typeof(NullPipelineStage));
            reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
        }

        protected override void OnCreate()
        {
            if (World.GetExistingSystem<ServerSimulationSystemGroup>() != null)
            {
                DriverConstructor.CreateServerDriver(World, out Driver, out _unreliablePipeline, out _reliablePipeline);
            }
            else
            {
                DriverConstructor.CreateClientDriver(World, out Driver, out _unreliablePipeline, out _reliablePipeline);
            }

            _driverListening = false;
            _rpcQueue = World.GetOrCreateSystem<RpcSystem>().GetRpcQueue<RpcSetNetworkId, RpcSetNetworkId>();
            _barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnDestroy()
        {
            Driver.Dispose();
        }

        public Entity Connect(NetworkEndPoint endpoint)
        {
            var session = EntityManager.CreateEntity();
            EntityManager.AddComponentData(session,
                new NetworkStreamConnection {Value = Driver.Connect(endpoint)});
            EntityManager.AddComponentData(session, new CommandTargetComponent());
            EntityManager.AddBuffer<IncomingRpcDataStreamBufferComponent>(session);
            EntityManager.AddBuffer<OutgoingRpcDataStreamBufferComponent>(session);
            EntityManager.AddBuffer<IncomingSnapshotDataStreamBufferComponent>(session);
            EntityManager.AddComponentData(session, new NetworkSnapshotAckComponent());
            return session;
        }

        public bool Listen(NetworkEndPoint endpoint)
        {
            if (Driver.Bind(endpoint) != 0)
            {
                Debug.Log($"Failed to bind to port {endpoint.Port}!");
                return false;
            }

            if (Driver.Listen() != 0)
            {
                Debug.Log($"Listen Error!");
            }

            _driverListening = true;
            return true;
        }

        private void OnAccept()
        {
            // 处理连接
            NetworkConnection c;
            while ((c = Driver.Accept()) != default)
            {
                // New connection can never have any events, if this one does - just close it
                if (c.PopEvent(Driver, out _) != NetworkEvent.Type.Empty)
                {
                    c.Disconnect(Driver);
                    continue;
                }

                Debug.Log($"Accepted a connection:{Driver.RemoteEndPoint(c).Address}");

                ++ConnectionCount;
                ++_connectionInstanceId;

                // 创建session 
                var session = PostUpdateCommands.CreateEntity();
                PostUpdateCommands.AddComponent(session, new NetworkSnapshotAckComponent());
                PostUpdateCommands.AddComponent(session, new NetworkStreamConnection {Value = c});
                PostUpdateCommands.AddComponent(session, new CommandTargetComponent());
                PostUpdateCommands.AddComponent(session, new NetworkIdComponent {Value = _connectionInstanceId});
                PostUpdateCommands.AddBuffer<IncomingRpcDataStreamBufferComponent>(session);
                PostUpdateCommands.AddBuffer<IncomingCommandDataStreamBufferComponent>(session);
                var outBuffer = PostUpdateCommands.AddBuffer<OutgoingRpcDataStreamBufferComponent>(session);

                // 发送id
                _rpcQueue.Schedule(outBuffer, new RpcSetNetworkId {id = _connectionInstanceId});
            }
        }

        protected override void OnUpdate()
        {
            Driver.ScheduleUpdate().Complete();
            if (_driverListening)
            {
                OnAccept();
            }
            else
            {
                if (!HasSingleton<ClientServerTickRate>())
                {
                    var newEntity = World.EntityManager.CreateEntity();
                    var tickRate = new ClientServerTickRate();
                    tickRate.ResolveDefaults();
                    EntityManager.AddComponentData(newEntity, tickRate);
                }

                var tickRateEntity = GetSingletonEntity<ClientServerTickRate>();
                Entities.WithNone<NetworkStreamDisconnected>()
                    .ForEach((Entity ent, ref ClientServerTickRateRefreshRequest req) =>
                    {
                        var dataFromEntity = GetComponentDataFromEntity<ClientServerTickRate>();
                        var tickRate = dataFromEntity[tickRateEntity];
                        tickRate.MaxSimulationStepsPerFrame = req.MaxSimulationStepsPerFrame;
                        tickRate.NetworkTickRate = req.NetworkTickRate;
                        tickRate.SimulationTickRate = req.SimulationTickRate;
                        dataFromEntity[tickRateEntity] = tickRate;
                        PostUpdateCommands.RemoveComponent<ClientServerTickRateRefreshRequest>(ent);
                    });
            }

            var rpcBuffer = GetBufferFromEntity<IncomingRpcDataStreamBufferComponent>();
            var cmdBuffer = GetBufferFromEntity<IncomingCommandDataStreamBufferComponent>();
            var localTime = NetworkTimeSystem.TimestampMS;
            var snapshotBuffer = GetBufferFromEntity<IncomingSnapshotDataStreamBufferComponent>();

            Entities.WithNone<NetworkStreamDisconnected>().ForEach(
                (Entity session, ref NetworkStreamConnection connection, ref NetworkSnapshotAckComponent snapshotAck) =>
                {
                    if (!connection.Value.IsCreated)
                        return;

                    NetworkEvent.Type cmd;
                    while ((cmd = connection.Value.PopEvent(Driver, out DataStreamReader reader)) !=
                           NetworkEvent.Type.Empty)
                    {
                        switch (cmd)
                        {
                            case NetworkEvent.Type.Connect:
                            {
                                Debug.Log("We are now connected to the server:" + connection.Value.InternalId);
                                break;
                            }
                            case NetworkEvent.Type.Disconnect:
                            {
                                if (rpcBuffer.HasComponent(session))
                                    rpcBuffer[session].Clear();

                                if (_driverListening)
                                {
                                    if (cmdBuffer.HasComponent(session))
                                        cmdBuffer[session].Clear();

                                    --ConnectionCount;
                                }

                                connection.Value = default;
                                PostUpdateCommands.AddComponent(session, new NetworkStreamDisconnected());

                                Debug.Log("We are now disconnect:" + connection.Value.InternalId);
                                break;
                            }
                            case NetworkEvent.Type.Data:
                            {
                                switch ((NetworkStreamProtocol) reader.ReadByte())
                                {
                                    case NetworkStreamProtocol.Snapshot:
                                    {
                                        uint remoteTime = reader.ReadUInt();
                                        uint localTimeMinusRTT = reader.ReadUInt();
                                        int commandServerAge = reader.ReadInt();
                                        snapshotAck.ServerCommandAge = commandServerAge;
                                        snapshotAck.UpdateRemoteTime(remoteTime, localTimeMinusRTT, localTime);

                                        var buffer = snapshotBuffer[session];
                                        // buffer.Clear();
                                        buffer.Add(ref reader);
                                        break;
                                    }
                                    case NetworkStreamProtocol.Command:
                                    {
                                        var buffer = cmdBuffer[session];
                                        uint snapshot = reader.ReadUInt();
                                        uint snapshotMask = reader.ReadUInt();
                                        snapshotAck.UpdateReceiveByRemote(snapshot, snapshotMask);

                                        uint remoteTime = reader.ReadUInt();
                                        uint localTimeMinusRTT = reader.ReadUInt();
                                        snapshotAck.UpdateRemoteTime(remoteTime, localTimeMinusRTT, localTime);

                                        // buffer.Clear();
                                        buffer.Add(ref reader);

                                        break;
                                    }
                                    case NetworkStreamProtocol.Rpc:
                                    {
                                        var buffer = rpcBuffer[session];
                                        buffer.Add(ref reader);
                                        break;
                                    }
                                }

                                break;
                            }
                        }
                    }
                });
        }
    }
}