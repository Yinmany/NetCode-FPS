using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabReplacer : MonoBehaviour
{
    [System.Serializable]
    public struct ReplacementDefinition
    {
        public GameObject SourcePrefab;
        public GameObject TargetPrefab;
    }

    public bool switchOrder;
    public List<ReplacementDefinition> replacements = new List<ReplacementDefinition>();
}