using System.Collections.Generic;
using UnityEngine;

namespace MyGameLib.NetCode
{
    /// <summary>
    /// 延迟补偿Debug
    /// </summary>
    public class NetDebug : MonoBehaviour
    {
        public GUIStyle style;


        public static uint RenderTick;
        public static uint ServerTick;
        public static uint ClientTick;

        public static uint RTT;
        public static uint Jitter;

        public static float LastRollbackTime;

        //==========================
        // 流量
        public static float CommandMS, SnapMS, RpcMS1, RpcMS2, DownCount, UpCount;

        //==========================

        private static Dictionary<string, object> _debugValue;

        private void Awake()
        {
            style = new GUIStyle {fontSize = 20};
            _debugValue = new Dictionary<string, object>();
        }

        public static void Set(string key, object value)
        {
            if (_debugValue != null)
                _debugValue[key] = value;
        }

        private void OnGUI()
        {
            GUILayout.Label($"RenderTick:{RenderTick}", style);
            GUILayout.Label($"ServerTick:{ServerTick}", style);
            GUILayout.Label($"ClientTick:{ClientTick}", style);
            GUILayout.Label($"LastRollbackTime:{LastRollbackTime}", style);

            foreach (var item in _debugValue)
            {
                GUILayout.Label($"{item.Key}:{item.Value}", style);
            }
        }

        public static void Log(object log)
        {
            Debug.Log(log);
        }

        public static void CLog(object log)
        {
            Debug.Log($"<color=#0056FF>{log}</color>");
        }

        public static void SLog(object log)
        {
            Debug.Log($"<color=#FF008E>{log}</color>");
        }
    }
}