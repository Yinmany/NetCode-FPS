using MyGameLib.NetCode;
using Unity.Mathematics;

namespace Samples.MyGameLib.NetCode
{
    public struct ProjectileHit : IRpcCommand
    {
        public int GId;
        public float3 Point;
        public float3 Normal;
        public int Hp;
    }
}