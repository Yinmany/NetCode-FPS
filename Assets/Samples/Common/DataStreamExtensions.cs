using System;
using Unity.Networking.Transport;
using UnityEngine;

namespace Unity.Networking.Transport
{
    public static class DataStreamExtensions
    {
        public static void WritePackedVector3(this DataStreamWriter writer, Vector3 b,
            NetworkCompressionModel compressionModel) => writer.WritePackedFloat3(b, compressionModel);

        public static Vector3 ReadPackedVector3(this DataStreamReader reader, NetworkCompressionModel compressionModel) =>
            reader.ReadPackedFloat3(compressionModel);
    }
}