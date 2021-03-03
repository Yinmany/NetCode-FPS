using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Networking.Transport;

namespace MyGameLib.NetCode
{
    public struct RpcQueue<TActionSerializer, TActionRequest>
        where TActionSerializer : struct, IRpcCommandSerializer<TActionRequest>
        where TActionRequest : struct, IComponentData
    {
        internal ulong rpcType;
        [ReadOnly] internal NativeHashMap<ulong, int> rpcTypeHashToIndex;

        /// <summary>
        /// 把输入写入发送buffer中
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="data"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public unsafe void Schedule(DynamicBuffer<OutgoingRpcDataStreamBufferComponent> buffer, TActionRequest data)
        {
            TActionSerializer serializer = default;

            // 1字节:Rpc协议头 2字节:rpcIndex
            DataStreamWriter writer =
                new DataStreamWriter(UnsafeUtility.SizeOf<TActionRequest>() + 1 + 2, Allocator.Temp);

            if (buffer.Length == 0)
                writer.WriteByte((byte) NetworkStreamProtocol.Rpc);
            if (!rpcTypeHashToIndex.TryGetValue(rpcType, out var rpcIndex))
                throw new InvalidOperationException("Could not find RPC index for type");
            writer.WriteUShort((ushort) rpcIndex);
            serializer.Serialize(ref writer, data);

            // 把DataStreamWriter内存数据Copy到Buffer中
            var prevLen = buffer.Length;
            buffer.ResizeUninitialized(buffer.Length + writer.Length);
            byte* ptr = (byte*) buffer.GetUnsafePtr();
            ptr += prevLen;
            UnsafeUtility.MemCpy(ptr, writer.AsNativeArray().GetUnsafeReadOnlyPtr(), writer.Length);
        }
    }
}