using Unity.Entities;
using UnityEngine;

namespace MyGameLib.NetCode.Hybrid
{
    public class GhostViewAuthoringComponent : MonoBehaviour
    {
        public GameObject view;

        public bool isServer;

#if UNITY_EDITOR
        public Material serverDebugMat;
#endif

        /// <summary>
        /// 由SceneSystem调用并完成转换
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="dstManager"></param>
        public void Convert(Entity entity, EntityManager dstManager)
        {
            bool isSmoothView = dstManager.World.GetExistingSystem<ClientSimulationSystemGroup>() != null &&
                                transform.GetComponent<GhostAuthoringComponent>().Type !=
                                GhostAuthoringComponent.ClientInstanceType.Interpolated;

            // 需要平滑视图
            if (isSmoothView)
            {
#if false
                
                // 移动到root节点，用作平滑，不直接受Ghost的位置影响。
                view.transform.SetParent(null);
                view.transform.SetPositionAndRotation(transform.position, transform.rotation);

                // 创建平滑对象
                var viewEntity = dstManager.CreateEntity();
                dstManager.AddComponentObject(viewEntity, view.GetComponent<Transform>());
                dstManager.AddComponentData(entity, new SmoothViewComponent {SmoothViewEntity = viewEntity});
                // dstManager.World.GetExistingSystem<SceneSystem>().AddToConvert(view);

#endif

                return;
            }

            view.transform.localPosition = Vector3.zero;
            view.transform.localRotation = Quaternion.identity;

#if UNITY_EDITOR
            if (isServer)
            {
                if (serverDebugMat)
                    DebugMat();
            }
#endif

            if (!isServer && dstManager.World.GetExistingSystem<ServerSimulationSystemGroup>() != null)
            {
                HideMeshRender(view.transform);
            }
        }

        private void HideMeshRender(Transform transform)
        {
            foreach (var meshRenderer in transform.GetComponentsInChildren<Renderer>(true))
            {
                Destroy(meshRenderer);
            }
        }

#if UNITY_EDITOR
        private void DebugMat()
        {
            foreach (var meshRenderer in transform.GetComponentsInChildren<MeshRenderer>(false))
            {
                var m = new Material[meshRenderer.materials.Length];

                for (int i = 0; i < meshRenderer.materials.Length; i++)
                {
                    m[i] = serverDebugMat;
                }

                meshRenderer.materials = m;
            }

            foreach (var meshRenderer in transform.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                var m = new Material[meshRenderer.materials.Length];

                for (int i = 0; i < meshRenderer.materials.Length; i++)
                {
                    m[i] = serverDebugMat;
                }

                meshRenderer.materials = m;
            }
        }
#endif
    }
}