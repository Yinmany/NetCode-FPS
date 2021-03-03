using Samples.MyGameLib.NetCode.Base;
using Unity.Entities;
using UnityEngine;

namespace Samples.NetFPS
{
    public class CharacterAbilityAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public int hp = 100;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<AbilityMovement>(entity);
            dstManager.AddComponent<AbilityFireComponent>(entity);
            dstManager.AddComponent<AbilityGrenadeComponent>(entity);
            dstManager.AddComponentData(entity, new HealthComponent {Hp = 100});
        }
    }
}