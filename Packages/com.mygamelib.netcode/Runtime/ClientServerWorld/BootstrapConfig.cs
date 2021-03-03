using System.Collections.Generic;
using UnityEngine;

namespace MyGameLib.NetCode
{
    /// <summary>
    /// 启动配置
    /// </summary>
    [CreateAssetMenu(fileName = "BootstrapConfig", menuName = "NetCode/BootstrapConfig")]
    public class BootstrapConfig : ScriptableObject
    {
        public TargetWorld StartupWorld = TargetWorld.ClientAndServer;
        
        public int ClientNum = 1;
        
        // 连接
        public string IP = "127.0.0.1";
        public ushort Port = 9000;
        
        // 网络调试
        [Header("固定延迟")] public int ClientPacketDelayMs = 0;
        [Header("抖动")] public int ClientPacketJitterMs = 0;

        [Header("丢包率(%)")] public int ClientPacketDropRate = 0;

        public class ConnectionStr
        {
            public string Name;
            public string IP = "";
            public bool Enabled = false;
        }

        public List<ConnectionStr> Connections = new List<ConnectionStr>();

#if UNITY_EDITOR
        public bool isEditor = false;
#endif
    }
}