using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport.Utilities;

namespace MyGameLib.NetCode
{
    public struct NetworkSnapshotAckComponent : IComponentData
    {
        // 记录客户端收到的快照tick
        public uint LastReceivedSnapshotByRemote;

        // 从服务端最后一次收到的快照的Tick(服务端的tick)
        public uint LastReceivedSnapshotByLocal;
        public uint ReceivedSnapshotByLocalMask;

        // 最近256个客户端快照接收情况
        public ulong ReceivedSnapshotByRemoteMask0;
        public ulong ReceivedSnapshotByRemoteMask1;
        public ulong ReceivedSnapshotByRemoteMask2;
        public ulong ReceivedSnapshotByRemoteMask3;

        /// <summary>
        /// 更新mask
        /// 跟踪客户端最近收到的快照.
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="mask"></param>
        public void UpdateReceiveByRemote(uint tick, uint mask)
        {
            if (tick == 0) // 初始化
            {
                ReceivedSnapshotByRemoteMask0 = 0;
                ReceivedSnapshotByRemoteMask1 = 0;
                ReceivedSnapshotByRemoteMask2 = 0;
                ReceivedSnapshotByRemoteMask3 = 0;
                LastReceivedSnapshotByRemote = 0;
            }
            // 服务端第一次收到来自客户端的信息
            else if (LastReceivedSnapshotByRemote == 0)
            {
                ReceivedSnapshotByRemoteMask3 = 0;
                ReceivedSnapshotByRemoteMask2 = 0;
                ReceivedSnapshotByRemoteMask1 = 0;
                ReceivedSnapshotByRemoteMask0 = mask;
                LastReceivedSnapshotByRemote = tick;
            }
            // 第n次收到，只要客户端发上来的tick，大于服务端记录上次来自客户端的tick(tick是客户端收到的服务端快照里面的tick)
            else if (SequenceHelpers.IsNewer(tick, LastReceivedSnapshotByRemote))
            {
                // 位移位数
                int shamt = (int) (tick - LastReceivedSnapshotByRemote);
                if (shamt >= 256)
                {
                    ReceivedSnapshotByRemoteMask3 = 0;
                    ReceivedSnapshotByRemoteMask2 = 0;
                    ReceivedSnapshotByRemoteMask1 = 0;
                    ReceivedSnapshotByRemoteMask0 = mask;
                }
                else
                {
                    // 位数大于64就依次往后挪
                    while (shamt >= 64)
                    {
                        ReceivedSnapshotByRemoteMask3 = ReceivedSnapshotByRemoteMask2;
                        ReceivedSnapshotByRemoteMask2 = ReceivedSnapshotByRemoteMask1;
                        ReceivedSnapshotByRemoteMask1 = ReceivedSnapshotByRemoteMask0;
                        ReceivedSnapshotByRemoteMask0 = 0;
                        shamt -= 64;
                    }

                    // 
                    if (shamt == 0)
                        ReceivedSnapshotByRemoteMask0 |= mask;
                    else
                    {
                        // 丢掉尾巴并拼接上一段位数(4段二进制依次往左位移shamt位并拼接前一段丢掉的部分，最后拼接mask)
                        ReceivedSnapshotByRemoteMask3 = (ReceivedSnapshotByRemoteMask3 << shamt) |
                                                        (ReceivedSnapshotByRemoteMask2 >> (64 - shamt));
                        ReceivedSnapshotByRemoteMask2 = (ReceivedSnapshotByRemoteMask2 << shamt) |
                                                        (ReceivedSnapshotByRemoteMask1 >> (64 - shamt));
                        ReceivedSnapshotByRemoteMask1 = (ReceivedSnapshotByRemoteMask1 << shamt) |
                                                        (ReceivedSnapshotByRemoteMask0 >> (64 - shamt));
                        ReceivedSnapshotByRemoteMask0 = (ReceivedSnapshotByRemoteMask0 << shamt) |
                                                        mask;
                    }
                }

                LastReceivedSnapshotByRemote = tick;
            }
        }


        public int ServerCommandAge;

        // 最后一次收到的远程时间
        public uint LastReceivedRemoteTime;
        public uint LastReceiveTimestamp;

        // 估计的往返延迟
        public float EstimatedRTT;

        // 偏差RTT(抖动)
        public float DeviationRTT;

        /// <summary>
        /// 更新远程时间(两端都调用)
        /// </summary>
        /// <param name="remoteTime"></param>
        /// <param name="localTimeMinusRTT"></param>
        /// <param name="localTime"></param>
        public void UpdateRemoteTime(uint remoteTime, uint localTimeMinusRTT, uint localTime)
        {
            if (remoteTime != 0 && SequenceHelpers.IsNewer(remoteTime, LastReceivedRemoteTime))
            {
                LastReceivedRemoteTime = remoteTime;
                LastReceiveTimestamp = localTime;
                if (localTimeMinusRTT == 0)
                {
                    return;
                }

                uint lastRecvRTT = localTime - localTimeMinusRTT;
                if (EstimatedRTT == 0)
                    EstimatedRTT = lastRecvRTT;
                else
                    EstimatedRTT = EstimatedRTT * 0.875f + lastRecvRTT * 0.125f;
                DeviationRTT = DeviationRTT * 0.75f + math.abs(lastRecvRTT - EstimatedRTT) * 0.25f;
            }
        }
    }

    /// <summary>
    /// 扩展方法相关此组件的逻辑
    /// </summary>
    public static class NetworkSnapshotAckComponentExtensions
    {
        /// <summary>
        /// 频断自己是否是旧的
        /// </summary>
        /// <param name="self"></param>
        /// <param name="serverTick">与之比较的值</param>
        /// <returns></returns>
        public static bool IsOldWithLastReceivedSnapshotByLocal(ref this NetworkSnapshotAckComponent self,
            uint serverTick)
        {
            return self.LastReceivedSnapshotByLocal != 0 &&
                   !SequenceHelpers.IsNewer(serverTick, self.LastReceivedSnapshotByLocal);
        }

        /// <summary>
        /// 更新本地相关的值
        /// </summary>
        /// <param name="self"></param>
        /// <param name="serverTick"></param>
        public static void UpdateLocalValues(ref this NetworkSnapshotAckComponent self, uint serverTick)
        {
            if (self.LastReceivedSnapshotByLocal != 0)
            {
                // 位移位数
                var shamt = (int) (serverTick - self.LastReceivedSnapshotByLocal);

                // 客户端只跟踪最近32个快照接收情况
                if (shamt < 32)
                {
                    self.ReceivedSnapshotByLocalMask <<= shamt;
                }
                else
                {
                    self.ReceivedSnapshotByLocalMask = 0;
                }
            }

            self.ReceivedSnapshotByLocalMask |= 1;
            self.LastReceivedSnapshotByLocal = serverTick;
        }
    }
}