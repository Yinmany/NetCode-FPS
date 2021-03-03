using System;

namespace MyGameLib.NetCode
{
    [Flags]
    public enum GhostSendType : short
    {
        None = 0,
        Interpolated = 1,
        Predicted = 1 << 2,
        All = Interpolated | Predicted
    }

    /// <summary>
    /// 表示在需要同步的组件上
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public class GhostComponentAttribute : Attribute
    {
        public GhostPrefabType PrefabType { get; set; }
        public GhostSendType SendType { get; set; }

        /// <summary>
        /// GhostUpdate刷新预测对象的值
        /// </summary>
        public bool IsUpdateValue { get; set; }

        public GhostComponentAttribute()
        {
            PrefabType = GhostPrefabType.All;
            SendType = GhostSendType.All;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class GhostFieldAttribute : Attribute
    {
        public int Quantization { get; set; }
        public bool Interpolate { get; set; }
        public bool SendData { get; set; }

        public GhostFieldAttribute()
        {
            Quantization = -1;
            Interpolate = false;
            SendData = true;
        }
    }

    /// <summary>
    /// This attribute is used to disable code generation for a struct implementing ICommandData or IRpcCommand
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class DisableCommandCodeGenAttribute : Attribute
    {
    }
}