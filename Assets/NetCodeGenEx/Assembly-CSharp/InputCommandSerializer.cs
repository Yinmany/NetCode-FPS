using MyGameLib.NetCode;
using Unity.Networking.Transport;
using Samples.MyGameLib.NetCode;

namespace Assembly_CSharp.Generated
{
    public struct InputCommandSerializer : ICommandDataSerializer<InputCommand>
    {
        public void Serialize(ref DataStreamWriter writer, in InputCommand data)
        {
            writer.WriteFloat2(data.Movement);
            writer.WriteSingle(data.Pitch);
            writer.WriteSingle(data.Yaw);
            writer.WriteBoolean(data.Jump);
            writer.WriteBoolean(data.Speed);
            writer.WriteBoolean(data.Fire);
            writer.WriteBoolean(data.MouseRight);
            writer.WriteBoolean(data.R);
            writer.WriteBoolean(data.T);
            writer.WriteBoolean(data.G);

            if (data.Fire || data.G || data.R)
            {
                writer.WriteFloat3(data.FirePos);
                writer.WriteFloat3(data.FireDir);
            }
        }

        public void Deserialize(ref DataStreamReader reader, ref InputCommand data)
        {
            data.Movement = reader.ReadFloat2();
            data.Pitch = reader.ReadSingle();
            data.Yaw = reader.ReadSingle();
            data.Jump = reader.ReadBoolean();
            data.Speed = reader.ReadBoolean();
            data.Fire = reader.ReadBoolean();
            data.MouseRight = reader.ReadBoolean();
            data.R = reader.ReadBoolean();
            data.T = reader.ReadBoolean();
            data.G = reader.ReadBoolean();

            if (data.Fire || data.G || data.R)
            {
                data.FirePos = reader.ReadFloat3();
                data.FireDir = reader.ReadFloat3();
            }
        }
    }

    public class InputCommandSendSystem : CommandSendSystem<InputCommandSerializer, InputCommand>
    {
    }

    public class InputCommandRecvSystem : CommandRecvSystem<InputCommandSerializer, InputCommand>
    {
    }
}