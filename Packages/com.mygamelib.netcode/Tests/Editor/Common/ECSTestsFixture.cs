using NUnit.Framework;
using Unity.Entities;

namespace MyGameLib.NetCode.Tests
{
    public abstract class ECSTestsFixture
    {
        protected World World;
        protected EntityManager EntityManager => World.EntityManager;

        [SetUp]
        public void SetUp()
        {
            World.DisposeAllWorlds();
            World = new World("Tests World");
        }

        [TearDown]
        public void TearDown()
        {
            World.Dispose();
            World = null;
        }
    }
}