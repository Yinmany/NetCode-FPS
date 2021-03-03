using System.Collections.Generic;

namespace MyGameLib.NetCode.Editor
{
    public struct CommonT4Info
    {
        public string TypeName; // 当前生成类型名称

        public string ToNamespace; // 生成的类放这个命名空间中
        public string CurNamespace; // 被生成类型的命名空间

        // ghosts
        public string IsUpdateValue;
        public string SendType;

        public List<FieldT4Info> Fields;
    }

    public struct FieldT4Info
    {
        public string Name;
        public string TypeName; // int
        public string TypeName1; //  Int 首字母大小除此之外与TypeName一样
        public bool Interpolate;
        public bool SendData;

        public string InterpolateMethodStr; // 此类型使用的插值方法Str
    }
}