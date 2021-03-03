using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyGameLib.NetCode.Editor
{
    [CreateAssetMenu(fileName = "CodeGenService", menuName = "NetCode/CodeGenService", order = 0)]
    public class CodeGenService : ScriptableObject, ISerializationCallbackReceiver
    {
        public HashSet<string> AssembliesDefaultOverrides;

        [SerializeField] private string[] _assembiesDefaultOverrides =
        {
            "MyGameLib.NetCode",
            "Unity.Transforms",
        };

        [SerializeField] private string[] _ignoreTypes = new string[0];

        public string[] IgnoreTypes => _ignoreTypes;
        
        [Serializable]
        public class GhostComponentField
        {
            public string Name;
            public bool Interpolate = false;
            public bool SendData = true;

            public GhostFieldAttribute Attribute => new GhostFieldAttribute
            {
                Interpolate = Interpolate,
                SendData = SendData
            };
        }

        [Serializable]
        public class GhostComponent
        {
            /// <summary>
            /// 要重载的组件名称
            /// </summary>
            public string Name;

            public bool Enable = true;

            public GhostPrefabType PrefabType;
            public GhostSendType SendType;

            /// <summary>
            /// GhostUpdate刷新预测对象的值
            /// </summary>
            public bool IsUpdateValue;

            public GhostComponentAttribute Attribute => new GhostComponentAttribute
            {
                IsUpdateValue = IsUpdateValue,
                PrefabType = PrefabType,
                SendType = SendType
            };

            public List<GhostComponentField> Fields;
            public bool IsExpanded;
        }

        public List<GhostComponent> Configs = new List<GhostComponent>()
        {
            new GhostComponent
            {
                Enable = true,
                Name = "Unity.Transforms.Translation",
                IsUpdateValue = false,
                PrefabType = GhostPrefabType.None,
                SendType = GhostSendType.None,
                Fields = new List<GhostComponentField>
                {
                    new GhostComponentField
                    {
                        Name = "Value",
                        Interpolate = true,
                        SendData = true
                    }
                }
            },
            new GhostComponent
            {
                Enable = true,
                Name = "Unity.Transforms.Rotation",
                IsUpdateValue = false,
                PrefabType = GhostPrefabType.All,
                SendType = GhostSendType.Predicted,
                Fields = new List<GhostComponentField>
                {
                    new GhostComponentField
                    {
                        Name = "Value",
                        Interpolate = true,
                        SendData = true
                    }
                }
            }
        };

        // static CodeGenService()
        // {
        //     CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        // }

        public void OnBeforeSerialize()
        {
            _assembiesDefaultOverrides = AssembliesDefaultOverrides.ToArray();
        }

        public void OnAfterDeserialize()
        {
            AssembliesDefaultOverrides = new HashSet<string>(_assembiesDefaultOverrides);
        }
    }
}