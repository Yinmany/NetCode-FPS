using Unity.Entities;
using UnityEngine;

namespace MyGameLib.NetCode
{
    [DisallowMultipleComponent]
    public class GhostOwnerAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new GhostOwnerComponent());
        }
    }
}