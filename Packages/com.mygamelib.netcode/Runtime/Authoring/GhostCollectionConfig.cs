using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGameLib.NetCode
{
    [CreateAssetMenu(fileName = "GhostCollection", menuName = "NetCode/GhostCollection", order = 0)]
    public class GhostCollectionConfig : ScriptableObject
    {
        public string ClassFile = "";
        public string ClassName = "";
        public string ClassNamespace = "";

        // 普通需要转换的预制体
        public List<GameObject> Prefabs = new List<GameObject>();
        public List<Ghost> Ghosts = new List<Ghost>();

        [Serializable]
        public struct Ghost
        {
            public GhostAuthoringComponent prefab;
            public bool enabled;
        }
    }
}