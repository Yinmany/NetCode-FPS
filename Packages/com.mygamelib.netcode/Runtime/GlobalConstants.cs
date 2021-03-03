namespace MyGameLib.NetCode
{
    public class GlobalConstants
    {
        // 冗余发送输入
        public const int InputBufferSendSize = 2;

        // Tick所占字节数大小
        public const int TickSize = 4;

        // 快照历史数量
        public const int SnapshotHistorySize = 32;

        // 命令数量
        public const int CommandDataMaxSize = 64;

        // 一个快照包大小,注意网络Writer是带有压缩的。
        public const int TargetPacketSize = 1200;
    }
}