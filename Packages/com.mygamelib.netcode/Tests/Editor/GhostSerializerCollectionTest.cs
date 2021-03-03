using NUnit.Framework;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace MyGameLib.NetCode.Tests
{
    [TestFixture]
    public class GhostSerializerCollectionTest : ECSTestsFixture
    {
        public void MockPrefabs()
        {
            var prefabCollectionEnt = EntityManager.CreateEntity();
            var serverPrefabsEnt = EntityManager.CreateEntity();

            // 添加3个预制体
            var a = EntityManager.CreateEntity(
                ComponentType.ReadOnly<Prefab>(),
                ComponentType.ReadWrite<Translation>());

            var b = EntityManager.CreateEntity(
                ComponentType.ReadOnly<Prefab>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<GhostOwnerComponent>());

            var serverPrefabs = EntityManager.AddBuffer<GhostPrefabBuffer>(serverPrefabsEnt);
            serverPrefabs.Add(new GhostPrefabBuffer
            {
                GhostType = 0,
                IsOwner = false,
                PrefabType = GhostPrefabType.InterpolatedClient,
                Value = a
            });

            serverPrefabs.Add(new GhostPrefabBuffer
            {
                GhostType = 1,
                IsOwner = true,
                PrefabType = GhostPrefabType.PredictedClient,
                Value = b
            });

            EntityManager.AddComponentData(prefabCollectionEnt, new GhostPrefabCollectionComponent
            {
                ServerPrefabs = serverPrefabsEnt
            });
        }

        [Test]
        public void GhostComponentIndexTest()
        {
            MockPrefabs();
            var system = World.GetOrCreateSystem<GhostCollectionSystem>();
            World.CreateSystem<GhostCollectionSerializerSystem>();
            system.Update();

            // 跟预制体对应数量
            Assert.AreEqual(system.GhostTypeCollection.Length, 2);
            Assert.AreEqual(system.GhostTypeCollection[0].FallbackPredictionMode, GhostSpawnBuffer.Type.Interpolated);
            Assert.AreEqual(system.GhostTypeCollection[1].FallbackPredictionMode, GhostSpawnBuffer.Type.Predicted);

            // 原型排好序的
            Assert.AreEqual(system.GhostTypeCollection[1].PredictionOwnerOffset, 0);
        }

        [Test]
        public void SnapshotSizeAlignedTest()
        {
            int size = 4;
            size = (size + 15) & (~15);
            Debug.Log($"{size}");
        }
    }
}