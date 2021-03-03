using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace MyGameLib.NetCode.Tests
{
    [TestFixture]
    public class ClientServerBootstrapTest
    {
        [DisableAutoCreation]
        [DefaultWorld]
        class TestSystem : ComponentSystem
        {
            protected override void OnCreate()
            {
                Debug.Log($"创建测试系统.");
            }

            protected override void OnUpdate()
            {
            }
        }

        [UpdateInGroup(typeof(NetworkReceiveSystemGroup))]
        [DisableAutoCreation]
        class Test1System : ComponentSystem
        {
            protected override void OnCreate()
            {
                Debug.Log($"{World.Name}");
            }

            protected override void OnUpdate()
            {
            }
        }

        [Test]
        public void FindTargetWorldChainTest()
        {
            Type[] chains =
            {
                typeof(ChainClientSimulationSystem),
                typeof(ChainServerSimulationSystem),
            };

            foreach (var t in chains)
            {
                var targetWorld = ClientServerBootstrap.FindTargetWorld(t, out var explicitWorld);
                Assert.IsTrue(targetWorld == TargetWorld.Default && explicitWorld);
            }
        }

        [Test]
        public void FindTargetWorldRootTest()
        {
            // 客户端与服务端World顶级组
            Type[] clientRoots =
            {
                typeof(ClientInitializationSystemGroup),
                typeof(ClientSimulationSystemGroup),
                typeof(ClientPresentationSystemGroup),
            };

            Type[] serverRoots =
            {
                typeof(ServerInitializationSystemGroup),
                typeof(ServerSimulationSystemGroup)
            };

            foreach (var t in clientRoots)
            {
                var targetWorld = ClientServerBootstrap.FindTargetWorld(t, out var explicitWorld);
                Assert.IsTrue(targetWorld == TargetWorld.Client && explicitWorld);
            }

            foreach (var t in serverRoots)
            {
                var targetWorld = ClientServerBootstrap.FindTargetWorld(t, out var explicitWorld);
                Assert.IsTrue(targetWorld == TargetWorld.Server && explicitWorld);
            }
        }

        [Test]
        public void PutsSystemInDefaultWorld()
        {
            var old = ClientServerBootstrap.SystemStates;

            ClientServerBootstrap.SystemStates = default;
            var system = new List<Type>
            {
                typeof(ClientInitializationSystemGroup),
                typeof(ClientSimulationSystemGroup),
                typeof(ClientPresentationSystemGroup),
                typeof(ServerInitializationSystemGroup),
                typeof(ServerSimulationSystemGroup)
            };

            ClientServerBootstrap.GenerateSystemList(system);
            Assert.True(
                ClientServerBootstrap.SystemStates.ClientSystems.Contains(typeof(ClientInitializationSystemGroup)));
            Assert.True(ClientServerBootstrap.SystemStates.ClientSystems.Contains(typeof(ClientSimulationSystemGroup)));
            Assert.True(
                ClientServerBootstrap.SystemStates.ClientSystems.Contains(typeof(ClientPresentationSystemGroup)));
            Assert.True(
                ClientServerBootstrap.SystemStates.ServerSystems.Contains(typeof(ServerInitializationSystemGroup)));
            Assert.True(ClientServerBootstrap.SystemStates.ServerSystems.Contains(typeof(ServerSimulationSystemGroup)));

            ClientServerBootstrap.SystemStates = old;
        }

        [Test]
        public void BootstrapNetCodeTestWorld()
        {
            using (var world = new NetCodeTestWorld())
            {
                world.Bootstrap(true, typeof(TestSystem));
                world.CreateWorlds(true);
                world.Tick(1000);
            }
        }
    }
}