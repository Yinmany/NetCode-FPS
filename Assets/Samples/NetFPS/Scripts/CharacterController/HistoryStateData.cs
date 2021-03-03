using Unity.Mathematics;

namespace MyGameLib.NetCode
{
    public struct HistoryStateData : IHistoryStateData
    {
        public uint Tick { get; set; }

        public float3 pos;
        public quaternion rot;
    }
}