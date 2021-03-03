using Unity.Entities;

namespace MyGameLib.NetCode
{
    public static class CommandBufferExtensions
    {
        private const int CommandDataMaxSize = 1024;

        public static void AddCommandData<T>(this DynamicBuffer<T> self, T command) where T : struct, ICommandData
        {
            uint targetTick = command.Tick;
            int oldestIdx = 0;
            uint oldestTick = 0;

            for (int i = 0; i < self.Length; ++i)
            {
                uint tick = self[i].Tick;
                if (tick == targetTick)
                {
                    self[i] = command;
                    return;
                }

                if (oldestTick == 0 || tick < oldestTick)
                {
                    oldestIdx = i;
                    oldestTick = tick;
                }
            }

            if (self.Length < CommandDataMaxSize)
            {
                self.Add(command);
            }
            else
            {
                self[oldestIdx] = command;
            }
        }

        public static bool GetDataAtTick<T>(this DynamicBuffer<T> self, uint targetTick, out T data)
            where T : struct, ICommandData
        {
            uint beforeTick = 0;
            int beforeIdx = 0;

            for (int i = 0; i < self.Length; i++)
            {
                uint tick = self[i].Tick;
                if (tick == targetTick)
                {
                    data = self[i];
                    return true;
                }

                if (beforeTick == 0 || beforeTick < tick)
                {
                    beforeIdx = i;
                    beforeTick = tick;
                }
            }

            if (beforeTick == 0)
                data = default;
            else
                data = self[beforeIdx];
            return false;
        }
    }
}