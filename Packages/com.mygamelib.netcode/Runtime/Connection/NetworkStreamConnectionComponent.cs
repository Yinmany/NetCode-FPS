using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Networking.Transport;

namespace MyGameLib.NetCode
{
    public struct NetworkStreamConnection : IComponentData
    {
        public NetworkConnection Value;
    }

    public struct NetworkStreamInGame : IComponentData
    {

    }

    public struct NetworkStreamDisconnected : IComponentData
    {
    }


    public struct IncomingCommandDataStreamBufferComponent : IBufferElementData
    {
        public byte Value;
    }

    public struct IncomingSnapshotDataStreamBufferComponent : IBufferElementData
    {
        public byte Value;
    }

    public static class NetCodeBufferComponentExtensions
    {
        public static unsafe DataStreamReader AsDataStreamReader<T>(this DynamicBuffer<T> self)
            where T : struct, IBufferElementData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (UnsafeUtility.SizeOf<T>() != 1)
                throw new System.InvalidOperationException(
                    "Can only convert DynamicBuffers of size 1 to DataStreamWriters");
#endif

            var na = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(self.GetUnsafePtr(), self.Length,
                Allocator.Invalid);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(self.AsNativeArray());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref na, safety);
#endif
            return new DataStreamReader(na);
        }

        public static unsafe void Add<T>(this DynamicBuffer<T> self, ref DataStreamReader reader)
            where T : struct, IBufferElementData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (UnsafeUtility.SizeOf<T>() != 1)
                throw new System.InvalidOperationException(
                    "Can only Add to DynamicBuffers of size 1 from DataStreamReaders");
#endif

            var oldLen = self.Length;
            var len = reader.Length - reader.GetBytesRead();
            self.ResizeUninitialized(len + oldLen);
            reader.ReadBytes((byte*) self.GetUnsafePtr() + oldLen, len);
        }
    }
}