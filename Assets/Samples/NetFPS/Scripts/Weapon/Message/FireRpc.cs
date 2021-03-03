using MyGameLib.NetCode;
using Unity.Mathematics;

namespace Samples.MyGameLib.NetCode
{
    /// <summary>
    /// 响应给客户端的
    /// </summary>
    public struct FireRpc : IRpcCommand
    {
        public int OwnerGId;
        public uint Tick;
        public float3 Pos;
        public float3 Dir;
    }
}