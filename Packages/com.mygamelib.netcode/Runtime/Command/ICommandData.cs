using Unity.Entities;
using Unity.Networking.Transport;

namespace MyGameLib.NetCode
{
    public interface ICommandDataSerializer<T> where T : struct, ICommandData
    {
        void Serialize(ref DataStreamWriter writer, in T data);

        void Deserialize(ref DataStreamReader reader, ref T data);
    }

    /// <summary>
    /// 命令数据
    /// </summary>
    public interface ICommandData : IBufferElementData
    {
        uint Tick { get; set; }
    }
}