using Unity.Entities;
using UnityEngine;

namespace MyGameLib.NetCode
{
    /// <summary>
    /// 物理场景组件
    /// </summary>
    public struct PhysicsSceneComponent : IComponentData
    {
        public long Id;
        public PhysicsScene physicsScene;
    }
}