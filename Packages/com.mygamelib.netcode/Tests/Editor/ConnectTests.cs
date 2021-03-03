using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace MyGameLib.NetCode.Tests
{
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    public class CheckConnectionSystem : ComponentSystem
    {
        public static int IsConnected;

        protected override void OnUpdate()
        {
            if (HasSingleton<NetworkStreamConnection>())
            {
                if (World.GetExistingSystem<ServerSimulationSystemGroup>() != null)
                    IsConnected |= 2;
                else
                    IsConnected |= 1;
            }
        }
    }

    public class ConnectTests
    {
        [Test]
        public void ConnectSingleClient()
        {
            using (var world = new NetCodeTestWorld())
            {
                world.Bootstrap(true, typeof(CheckConnectionSystem));
                world.CreateWorlds(true);

                CheckConnectionSystem.IsConnected = 0;
                if (CheckConnectionSystem.IsConnected != 3)
                {
                    world.Connect(16f / 1000f, 16);
                }

                Assert.AreEqual(3, CheckConnectionSystem.IsConnected);
                Debug.Log("NetId = " + world.TryGetSingleton<NetworkIdComponent>(world.ClientWorld).Value);
            }
        }
    }
}