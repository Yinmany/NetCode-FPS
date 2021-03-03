using Unity.Collections;
using Unity.Entities;

namespace MyGameLib.NetCode
{
    /// <summary>
    /// 预制体集合系统
    /// </summary>
    [DisableAutoCreation]
    public class PrefabCollectionSystem : ComponentSystem
    {
        public NativeArray<PrefabItem> Prefabs;
        public NativeArray<GhostPrefabBuffer> ServerPrefabs;
        public NativeArray<GhostPrefabBuffer> ClientInterpolatedPrefabs;
        public NativeArray<GhostPrefabBuffer> ClientPredictedPrefabs;

        protected override void OnUpdate()
        {
        }

        protected override void OnDestroy()
        {
            if (Prefabs.IsCreated)
                Prefabs.Dispose();
            if (ServerPrefabs.IsCreated)
                ServerPrefabs.Dispose();
            if (ClientInterpolatedPrefabs.IsCreated)
                ClientInterpolatedPrefabs.Dispose();
            if (ClientPredictedPrefabs.IsCreated)
                ClientPredictedPrefabs.Dispose();
        }
    }
}