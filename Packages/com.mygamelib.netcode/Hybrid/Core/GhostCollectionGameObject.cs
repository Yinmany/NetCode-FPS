using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace MyGameLib.NetCode.Hybrid
{
    [RequireComponent(typeof(GhostCollectionAuthoringComponent))]
    public class GhostCollectionGameObject : MonoBehaviour, IConvertGameObjectToEntity
    {
        private GhostCollectionAuthoringComponent _authoring;

        private GhostCollectionConfig _config;

        private void Awake()
        {
            _authoring = GetComponent<GhostCollectionAuthoringComponent>();
            _config = _authoring.ghostCollectionConfig;
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var conversionTarget = GhostCollectionAuthoringComponent.GetConversionTarget(dstManager.World);

            var mgr = dstManager.World.GetOrCreateSystem<GameObjectManager>();
            mgr.InitPrefabs(_authoring.ghostCollectionConfig.Prefabs.Count);
            for (int i = 0; i < _config.Prefabs.Count; i++)
            {
                mgr.AddPrefab(i, _config.Prefabs[i]);
            }

            Transform hidden = new GameObject().transform;
            hidden.gameObject.SetActive(false);
            hidden.name = $"__{conversionTarget}__";
            // Ghost处理
            GhostCollectionConfig.Ghost[] ghosts = _authoring.GetGhosts();
            mgr.InitGhosts(ghosts.Length);
            for (int i = 0; i < ghosts.Length; i++)
            {
                var item = ghosts[i];
                var originType = item.prefab.Type;

                GameObject tmpServer = Instantiate(item.prefab.gameObject, hidden);
                tmpServer.name = $"{item.prefab.name}[SERVER]";
                RemoveComponent(tmpServer, TargetWorld.Server);
                mgr.AddGhostToServer(i, tmpServer);

                if (conversionTarget != TargetWorld.Client)
                    continue;

                item.prefab.Type = GhostAuthoringComponent.ClientInstanceType.Predicted;
                GameObject tmpPrediction = Instantiate(item.prefab.gameObject, hidden);
                tmpPrediction.name = $"{item.prefab.name}[CP]";
                RemoveComponent(tmpPrediction, conversionTarget);
                mgr.AddGhostToPredicted(i, tmpPrediction);

                item.prefab.Type = GhostAuthoringComponent.ClientInstanceType.Interpolated;
                GameObject tmpInterpolated = Instantiate(item.prefab.gameObject, hidden);
                tmpInterpolated.name = $"{item.prefab.name}[CI]";
                RemoveComponent(tmpInterpolated, conversionTarget);
                mgr.AddGhostToInterpolated(i, tmpInterpolated);

                item.prefab.Type = originType;
            }
        }

        protected void RemoveComponent(GameObject prefab, TargetWorld target)
        {
            var ghostAuthoring = prefab.GetComponent<GhostAuthoringComponent>();
            var toRemove = new HashSet<string>();
            if (target == TargetWorld.Server)
            {
                foreach (GhostAuthoringComponent.GhostComponentInfo ghostComponent in ghostAuthoring.Components)
                {
                    if (!ghostComponent.server)
                    {
                        toRemove.Add(ghostComponent.name);
                    }
                }
            }
            else if (target == TargetWorld.Client)
            {
                if (ghostAuthoring.Type == GhostAuthoringComponent.ClientInstanceType.Interpolated)
                {
                    foreach (GhostAuthoringComponent.GhostComponentInfo ghostComponent in ghostAuthoring.Components
                    )
                    {
                        if (!ghostComponent.interpolatedClient)
                        {
                            toRemove.Add(ghostComponent.name);
                        }
                    }
                }
                else
                {
                    foreach (GhostAuthoringComponent.GhostComponentInfo ghostComponent in ghostAuthoring.Components
                    )
                    {
                        if (!ghostComponent.predictedClient)
                        {
                            toRemove.Add(ghostComponent.name);
                        }
                    }
                }
            }


            var monoComponents = ghostAuthoring.GetComponents(typeof(Component));
            foreach (var comp in monoComponents)
            {
                // 移除DOTS转换组件
                if (toRemove.Contains(comp.GetType().FullName) ||
                    (comp is IConvertGameObjectToEntity && !(comp is IConvertNotRemove)))
                {
                    Destroy(comp);
                }
            }
        }
    }
}