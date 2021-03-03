using Unity.Entities;

namespace ECSCarTest
{
    [GenerateAuthoringComponent]
    public struct VehicleConfigComponent : IComponentData
    {
        public float Drift;
    }
}