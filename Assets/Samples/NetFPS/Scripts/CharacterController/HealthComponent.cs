using Unity.Entities;

namespace Samples.MyGameLib.NetCode.Base
{
    public struct HealthComponent : IComponentData
    {
        public int Hp;
    }
}