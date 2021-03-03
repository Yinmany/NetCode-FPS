using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace MyGameLib.NetCode.Hybrid
{
    public class RemoveRenderMesh : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.RemoveComponent<RenderBounds>(entity);
            dstManager.RemoveComponent<RenderMesh>(entity);
        }
    }
}