using Unity.Entities;
using UnityEngine;

namespace MyGameLib.NetCode.Serializer
{
    [DisallowMultipleComponent]
    public class NetworkRigidbodyAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new NetworkRigidbody());
        }
    }
}