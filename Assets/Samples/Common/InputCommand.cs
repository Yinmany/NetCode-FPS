using MyGameLib.NetCode;
using Unity.Mathematics;

namespace Samples.MyGameLib.NetCode
{
    public struct InputCommand : ICommandData
    {
        public uint Tick { get; set; }

        public float2 Movement;
        public float Pitch, Yaw;
        public bool Jump;
        public bool Speed;
        public bool Fire;
        public bool MouseRight;
        public bool R, T, G;

        // 开火位置以及开火方向
        public float3 FirePos;
        public float3 FireDir;
    }
}