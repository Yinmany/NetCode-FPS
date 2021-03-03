using Unity.Mathematics;

namespace Unity.Networking.Transport
{
    public static class DataStreamExtensions
    {
        #region Write

        public static void WriteBoolean(this ref DataStreamWriter writer, bool b) =>
            writer.WriteByte(b ? (byte) 1 : (byte) 0);

        public static void WriteInt32(this ref DataStreamWriter writer, int v) => writer.WriteInt(v);
        public static void WriteUInt32(this ref DataStreamWriter writer, uint v) => writer.WriteUInt(v);

        public static void WriteSingle(this ref DataStreamWriter writer, float f) => writer.WriteFloat(f);

        public static void WriteFloat2(this ref DataStreamWriter writer, float2 v) => writer.Write(v);

        public static void WriteFloat3(this ref DataStreamWriter writer, float3 v) => writer.Write(v);

        public static void WriteQuaternion(this ref DataStreamWriter writer, quaternion qua) => writer.Write(qua);

        public static void WritePackedBoolean(this ref DataStreamWriter writer, bool b,
            NetworkCompressionModel compressionModel) => writer.WritePackedInt(b ? 1 : 0, compressionModel);

        public static void WritePackedQuaternion(this ref DataStreamWriter writer, quaternion rot,
            NetworkCompressionModel compressionModel)
        {
            writer.WritePackedFloat(rot.value.x, compressionModel);
            writer.WritePackedFloat(rot.value.y, compressionModel);
            writer.WritePackedFloat(rot.value.z, compressionModel);
            writer.WritePackedFloat(rot.value.w, compressionModel);
        }


        public static void WritePackedFloat3(this ref DataStreamWriter writer, float3 v,
            NetworkCompressionModel compressionModel)
        {
            writer.WritePackedFloat(v.x, compressionModel);
            writer.WritePackedFloat(v.y, compressionModel);
            writer.WritePackedFloat(v.z, compressionModel);
        }

        #endregion


        #region Read

        public static bool ReadBoolean(this ref DataStreamReader reader) => reader.ReadByte() == 1;
        public static int ReadInt32(this ref DataStreamReader reader) => reader.ReadInt();
        public static uint ReadUInt32(this ref DataStreamReader reader) => reader.ReadUInt();

        public static bool
            ReadPackedBoolean(this ref DataStreamReader reader, NetworkCompressionModel compressionModel) =>
            reader.ReadPackedInt(compressionModel) == 1;

        public static float ReadSingle(this ref DataStreamReader reader) => reader.ReadFloat();

        #endregion


        public static void Write(this ref DataStreamWriter writer, int v)
        {
            writer.WriteInt(v);
        }

        public static void WritePacked(this ref DataStreamWriter writer, int v,
            NetworkCompressionModel compressionModel)
        {
            writer.WritePackedInt(v, compressionModel);
        }

        public static int ReadInt(this ref DataStreamReader reader) => reader.ReadInt();

        public static void Write(this ref DataStreamWriter writer, float2 v)
        {
            writer.WriteFloat(v.x);
            writer.WriteFloat(v.y);
        }

        public static void Write(this ref DataStreamWriter writer, float3 v)
        {
            writer.WriteFloat(v.x);
            writer.WriteFloat(v.y);
            writer.WriteFloat(v.z);
        }

        public static void WritePacked(this ref DataStreamWriter writer, float3 v,
            NetworkCompressionModel compressionModel)
        {
            writer.WritePackedFloat(v.x, compressionModel);
            writer.WritePackedFloat(v.y, compressionModel);
            writer.WritePackedFloat(v.z, compressionModel);
        }

        public static void Write(this ref DataStreamWriter writer, bool b)
        {
            writer.WriteByte(b ? (byte) 1 : (byte) 0);
        }

        public static void Write(this ref DataStreamWriter writer, quaternion rot)
        {
            writer.WriteFloat(rot.value.x);
            writer.WriteFloat(rot.value.y);
            writer.WriteFloat(rot.value.z);
            writer.WriteFloat(rot.value.w);
        }

        public static void WritePacked(this ref DataStreamWriter writer, quaternion rot,
            NetworkCompressionModel compressionModel)
        {
            writer.WritePackedFloat(rot.value.x, compressionModel);
            writer.WritePackedFloat(rot.value.y, compressionModel);
            writer.WritePackedFloat(rot.value.z, compressionModel);
            writer.WritePackedFloat(rot.value.w, compressionModel);
        }

        public static bool ReadBool(this ref DataStreamReader reader)
        {
            byte b = reader.ReadByte();
            return b == 1;
        }

        public static float2 ReadFloat2(this ref DataStreamReader reader)
        {
            float2 v = default;
            v.x = reader.ReadFloat();
            v.y = reader.ReadFloat();
            return v;
        }

        public static float3 ReadFloat3(this ref DataStreamReader reader)
        {
            float3 v = default;
            v.x = reader.ReadFloat();
            v.y = reader.ReadFloat();
            v.z = reader.ReadFloat();
            return v;
        }

        public static float3 ReadPackedFloat3(this ref DataStreamReader reader,
            NetworkCompressionModel compressionModel)
        {
            float3 v = default;
            v.x = reader.ReadPackedFloat(compressionModel);
            v.y = reader.ReadPackedFloat(compressionModel);
            v.z = reader.ReadPackedFloat(compressionModel);
            return v;
        }

        public static quaternion ReadQuaternion(this ref DataStreamReader reader)
        {
            quaternion rot = default;
            rot.value.x = reader.ReadFloat();
            rot.value.y = reader.ReadFloat();
            rot.value.z = reader.ReadFloat();
            rot.value.w = reader.ReadFloat();
            return rot;
        }

        public static quaternion ReadPackedQuaternion(this ref DataStreamReader reader,
            NetworkCompressionModel compressionModel)
        {
            quaternion rot = default;
            rot.value.x = reader.ReadPackedFloat(compressionModel);
            rot.value.y = reader.ReadPackedFloat(compressionModel);
            rot.value.z = reader.ReadPackedFloat(compressionModel);
            rot.value.w = reader.ReadPackedFloat(compressionModel);
            return rot;
        }
    }
}