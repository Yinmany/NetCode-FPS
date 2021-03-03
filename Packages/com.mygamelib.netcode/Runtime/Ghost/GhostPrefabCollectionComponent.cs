using System;
using Unity.Entities;

namespace MyGameLib.NetCode
{
    /// <summary>
    /// Ghost预制体集合直接
    /// 单实例组件
    /// 3种Entity，Buffer中分别存放了不同形态的Prefab。以及一种普通预制体，如一些在游戏中需要用到预制体。
    /// 其中ServerPrefabs客户端与服务端都会存在，记录了状态同步公共的数据。
    /// </summary>
    public struct GhostPrefabCollectionComponent : IComponentData
    {
        public Entity ServerPrefabs;
        public Entity ClientInterpolatedPrefabs;
        public Entity ClientPredictedPrefabs;
    }

    /// <summary>
    /// Ghost预制体类型
    /// 客户端插值、客户端预测、服务端
    /// </summary>
    [Flags]
    public enum GhostPrefabType : short
    {
        None = 0,
        InterpolatedClient = 1,
        PredictedClient = 1 << 1,
        Server = 1 << 2,
        All = InterpolatedClient | PredictedClient | Server
    }

    /// <summary>
    /// 普通预制体
    /// </summary>
    public struct PrefabItem : IBufferElementData
    {
        public Entity Value;

        // 其实就是在Buffer中的下标
        public int Type;
    }

    /// <summary>
    /// 一个Ghost预制体
    /// </summary>
    public struct GhostPrefabBuffer : IBufferElementData
    {
        // 预制体Entity，里面存放了GameObject预制体。混合模式
        public Entity Value;

        // 预制体的类型，由转换时数组下标决定。
        public int GhostType;

        // 是否是客户端主人预测
        public bool IsOwner;

        // 预制体类型
        public GhostPrefabType PrefabType;
    }

    public static class GhostPrefabsExtensions
    {
        /// <summary>
        /// 寻找Owner幽灵预制体
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="ownerGhostPrefabBuffer"></param>
        /// <returns></returns>
        public static bool FindOwner(this DynamicBuffer<GhostPrefabBuffer> buffer,
            out GhostPrefabBuffer ownerGhostPrefabBuffer)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                if (!buffer[i].IsOwner) continue;
                ownerGhostPrefabBuffer = buffer[i];
                return true;
            }

            ownerGhostPrefabBuffer = default;
            return false;
        }
    }
}