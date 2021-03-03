using Unity.Entities;

namespace MyGameLib.NetCode
{
    public interface IHistoryStateData : IBufferElementData
    {
        uint Tick { get; set; }
    }

    public static class HistoryStateDataExtensions
    {
        public static bool GetHistoryStateData<T>(this DynamicBuffer<T> self, uint targetTick, out T data)
            where T : struct, IHistoryStateData
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

        public static void AddHistoryStateData<T>(this DynamicBuffer<T> self, T data)
            where T : struct, IHistoryStateData
        {
            uint targetTick = data.Tick;
            int oldestIdx = 0;
            uint oldestTick = 0;

            for (int i = 0; i < self.Length; ++i)
            {
                uint tick = self[i].Tick;
                if (tick == targetTick)
                {
                    self[i] = data;
                    return;
                }

                if (oldestTick == 0 || tick < oldestTick)
                {
                    oldestIdx = i;
                    oldestTick = tick;
                }
            }

            if (self.Length < GlobalConstants.SnapshotHistorySize)
            {
                self.Add(data);
            }
            else
            {
                self[oldestIdx] = data;
            }
        }
    }
}