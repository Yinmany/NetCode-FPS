using Unity.Entities;

namespace MyGameLib.NetCode.Hybrid
{
    /// <summary>
    /// 服务端CopyGameObject到ComponentData上以便进行快照的发送
    /// </summary>
    [ServerWorld]
    [UpdateInGroup(typeof(EndTickSystemGroup))]
    public abstract class SnapshotCopySystem : ComponentSystem
    {
    }

    /// <summary>
    /// 客户端回滚Ghost预测对象
    /// </summary>
    [UpdateInGroup(typeof(GhostUpdateSystemGroup))]
    public abstract class SnapshotRestoreSystem : ComponentSystem
    {
    }

    /// <summary>
    /// 客户端插值Ghost对象
    /// </summary>
    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    [UpdateAfter(typeof(GhostInterpolatedUpdateSystem))]
    public abstract class SnapshotInterpolatedCopySystem : ComponentSystem
    {
    }
}