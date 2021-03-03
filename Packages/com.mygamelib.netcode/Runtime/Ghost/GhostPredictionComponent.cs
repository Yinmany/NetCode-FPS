using Unity.Entities;
using Unity.Mathematics;

namespace MyGameLib.NetCode
{
    /// <summary>
    /// 预测组件Tag
    /// </summary>
    public struct GhostPredictionComponent : IComponentData
    {
    }

    /// <summary>
    /// 用于预测平滑视图的数据
    /// </summary>
    public struct GhostPredictionSmoothComponent : IComponentData
    {
        /// <summary>
        /// 回滚前预测物体数据
        /// </summary>
        public float3 PrevPos;

        public quaternion PrevRot;

        /// <summary>
        /// 待修正数据
        /// </summary>
        public float3 PosError;

        public quaternion RotError;
    }
}