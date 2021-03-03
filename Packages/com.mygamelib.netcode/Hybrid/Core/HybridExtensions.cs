using Unity.Entities;
using UnityEngine.SceneManagement;

namespace MyGameLib.NetCode.Hybrid
{
    public static class HybridExtensions
    {
        /// <summary>
        /// get关联的场景
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public static Scene GetLinkedScene(this World world)
        {
            return world.GetOrCreateSystem<GameObjectManager>().Scene;
        }

        public static bool IsServer(this World world) => world.GetExistingSystem<ServerSimulationSystemGroup>() != null;
    }
}