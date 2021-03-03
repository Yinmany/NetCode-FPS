using ECSCarTest;
using Unity.Entities;
using UnityEngine;

namespace Base.Vehicle.Authoring
{
    public class WheelBaseConfigAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float RestLength = 1;
        public float SpringTravel = 0.5f;
        public float SpringStiffness = 30000f;
        public float DamperStiffness = 2500f;

        public float wheelRadius = 0.33f;

        public LayerMask layerMask = -1;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new WheelBaseInfo {layerMask = layerMask});
            dstManager.AddComponentData(entity, new WheelBaseConfig
            {
                RestLength = RestLength,
                SpringTravel = SpringTravel,
                SpringStiffness = SpringStiffness,
                DamperStiffness = DamperStiffness,
                wheelRadius = wheelRadius
            });
        }
    }
}