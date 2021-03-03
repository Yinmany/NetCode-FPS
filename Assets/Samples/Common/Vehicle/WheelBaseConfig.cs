using Unity.Entities;

namespace ECSCarTest
{
    public struct WheelBaseConfig : IComponentData
    {
        public float RestLength;
        public float SpringTravel;
        public float SpringStiffness;
        public float DamperStiffness;

        public float wheelRadius;
    }
}