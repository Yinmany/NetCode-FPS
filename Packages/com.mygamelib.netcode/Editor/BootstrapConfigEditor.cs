using System;
using System.Net;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MyGameLib.NetCode.Editor
{
    [CustomEditor(typeof(BootstrapConfig))]
    public class BootstrapConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty _startupWorldProp;
        private SerializedProperty _clientPacketDelayMs;
        private SerializedProperty _clientPacketJitterMs;
        private SerializedProperty _clientPacketDropRate;
        private SerializedProperty _ipProp;
        private SerializedProperty _portProp;
        private SerializedProperty _clientNumProp;
        
        private ReorderableList _list;

        private void OnEnable()
        {
            _startupWorldProp = serializedObject.FindProperty(nameof(BootstrapConfig.StartupWorld));
            _clientPacketDelayMs = serializedObject.FindProperty(nameof(BootstrapConfig.ClientPacketDelayMs));
            _clientPacketJitterMs = serializedObject.FindProperty(nameof(BootstrapConfig.ClientPacketJitterMs));
            _clientPacketDropRate = serializedObject.FindProperty(nameof(BootstrapConfig.ClientPacketDropRate));
            _ipProp = serializedObject.FindProperty(nameof(BootstrapConfig.IP));
            _portProp = serializedObject.FindProperty(nameof(BootstrapConfig.Port));
            _clientNumProp = serializedObject.FindProperty(nameof(BootstrapConfig.ClientNum));
            
            // _list = new ReorderableList((target as BootstrapConfig).Connections, typeof(BootstrapConfig), true, true,
            //     true, true);
            // _list.drawHeaderCallback += DrawHeaderCallback;
            // _list.drawElementCallback += DrawElementCallback;
            // _list.onAddCallback += OnAddCallback;
        }

        private void OnAddCallback(ReorderableList list)
        {
            var config = target as BootstrapConfig;
            config.Connections.Add(new BootstrapConfig.ConnectionStr());
            EditorUtility.SetDirty(target);
        }

        private void DrawElementCallback(Rect rect, int index, bool isactive, bool isfocused)
        {
            var config = target as BootstrapConfig;
            var item = config.Connections[index];
            EditorGUI.BeginChangeCheck();
            rect.height -= 2;

            float w = rect.width;

            rect.width = 80;

            item.Name = EditorGUI.TextField(rect, item.Name);

            rect.x += rect.width + 2;
            rect.width = w - rect.width - 30;

            item.IP = EditorGUI.TextField(rect, item.IP);

            rect.x += rect.width + 3;
            rect.width = rect.height;

            item.Enabled = EditorGUI.Toggle(rect, item.Enabled);
            if (item.Enabled)
            {
                _ipProp.stringValue = item.IP;
            }

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(target);
        }

        private void DrawHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, $"连接地址({_ipProp.stringValue})");
        }

        public static string[] options = {"Client", "Server", "Client & Server"};

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space(20);
            ShowMode();
            EditorGUILayout.PropertyField(_clientNumProp);
            EditorGUILayout.PropertyField(_ipProp);
            EditorGUILayout.PropertyField(_portProp);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(_clientPacketDelayMs);
            EditorGUILayout.PropertyField(_clientPacketJitterMs);
            EditorGUILayout.PropertyField(_clientPacketDropRate);

            EditorGUILayout.Space(10);
            // _list.DoLayoutList();

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void ShowMode()
        {
            BootstrapConfig config = (target as BootstrapConfig);
            int index = 0;

            switch (config.StartupWorld)
            {
                case TargetWorld.ClientAndServer:
                    index = 2;
                    break;
                case TargetWorld.Server:
                    index = 1;
                    break;
            }

            index = EditorGUILayout.Popup("Mode", index, options);

            switch (index)
            {
                case 2:
                    _startupWorldProp.intValue = (int) TargetWorld.ClientAndServer;
                    break;
                case 1:
                    _startupWorldProp.intValue = (int) TargetWorld.Server;
                    break;
                case 0:
                    _startupWorldProp.intValue = (int) TargetWorld.Client;
                    break;
            }
        }
    }
}