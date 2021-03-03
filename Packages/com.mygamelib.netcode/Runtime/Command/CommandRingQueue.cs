using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace MyGameLib.NetCode
{
    /// <summary>
    /// 环形队列头部数据
    /// </summary>
    internal struct CommandRingQueueHead
    {
        // UnsafeRingQueue<>
        public int Capacity { get; set; }
        public int Read { get; set; }
        public int Write { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// 命令环形队列
    /// </summary>
    public unsafe struct CommandRingQueue<T, B> where T : unmanaged, ICommandData where B : struct, IBufferElementData
    {
        private CommandRingQueueHead* _head;
        private T* _data;

        public int Count => _head->Count;
        
        /// <summary>
        /// 初始化队列
        /// </summary>
        /// <param name="dynamicBuffer"></param>
        /// <param name="capacity"></param>
        public static void Init(DynamicBuffer<B> dynamicBuffer, int capacity = 64)
        {
            if (UnsafeUtility.SizeOf<B>() != 1)
            {
                throw new InvalidOperationException("BufferElementData Size 只能是1字节");
            }

            // 分配大小
            int size = UnsafeUtility.SizeOf<CommandRingQueueHead>() + UnsafeUtility.SizeOf<T>() * capacity;
            dynamicBuffer.ResizeUninitialized(size);
            CommandRingQueueHead* head = (CommandRingQueueHead*)dynamicBuffer.GetUnsafePtr();
            head->Capacity = capacity;
            head->Count = 0;
            head->Read = 0;
            head->Write = 0;
        }

        public CommandRingQueue(DynamicBuffer<B> dynamicBuffer)
        {
            int headSize = UnsafeUtility.SizeOf<CommandRingQueueHead>();
            if (headSize > dynamicBuffer.Length)
            {
                throw new InvalidOperationException("无法创建队列，Buffer长度不对: err -1");
            }

            _head = (CommandRingQueueHead*) dynamicBuffer.GetUnsafePtr();

            if (UnsafeUtility.SizeOf<T>() * _head->Capacity != dynamicBuffer.Length - headSize)
            {
                throw new InvalidOperationException("无法创建队列，Buffer长度不对.err -2");
            }

            _data = (T*) ((byte*) dynamicBuffer.GetUnsafePtr() + headSize);
        }

        public void Enqueue(T t)
        {
            _head->Write += 1;
            int slot = _head->Write % _head->Capacity;
            _data[slot] = t;
            if (_head->Count == _head->Capacity)
            {
                _head->Read = (_head->Read + 1) % _head->Capacity;
            }
            else
            {
                ++_head->Count;
            }
        }

        public bool TryDequeue(out T d)
        {
            d = default;

            if (_head->Count == 0) return false;

            _head->Read += 1;
            int slot = _head->Read % _head->Capacity;
            d = _data[slot];
            --_head->Count;
            return true;
        }
    }
}