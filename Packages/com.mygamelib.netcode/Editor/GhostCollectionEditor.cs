using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MyGameLib.NetCode.Editor
{
    [CustomEditor(typeof(GhostCollectionConfig), true)]
    public class GhostCollectionEditor : UnityEditor.Editor
    {
        private SerializedProperty _classFile;
        private SerializedProperty _className;
        private SerializedProperty _classNamespace;

        private ReorderableList _prefabList;
        private ReorderableList _ghostList;

        private bool _isChange;

        private void OnEnable()
        {
            var config = target as GhostCollectionConfig;
            _classFile = serializedObject.FindProperty(nameof(config.ClassFile));
            _className = serializedObject.FindProperty(nameof(config.ClassName));
            _classNamespace = serializedObject.FindProperty(nameof(config.ClassNamespace));

            _prefabList = new ReorderableList(config.Prefabs, typeof(GameObject), true, true, true, true);
            _prefabList.drawHeaderCallback += PrefabDrawHeaderCallback;
            _prefabList.drawElementCallback += PrefabDrawElementCallback;
            _prefabList.onAddCallback += PrefabAddCallback;
            _prefabList.onRemoveCallback += PrefabRemoveCallback;


            _ghostList = new ReorderableList(config.Ghosts, typeof(GhostCollectionConfig.Ghost),
                true, true, true, true);
            _ghostList.drawHeaderCallback += DrawHeaderCallback;
            _ghostList.drawElementCallback += DrawElementCallback;
            _ghostList.onAddCallback += OnAddCallback;
            _ghostList.onRemoveCallback += OnRemoveCallback;

            _isChange = false;
        }

        private void OnDisable()
        {
            _prefabList.drawHeaderCallback -= PrefabDrawHeaderCallback;
            _prefabList.drawElementCallback -= PrefabDrawElementCallback;
            _prefabList.onAddCallback -= PrefabAddCallback;
            _prefabList.onRemoveCallback -= PrefabRemoveCallback;

            _ghostList.drawHeaderCallback -= DrawHeaderCallback;
            _ghostList.drawElementCallback -= DrawElementCallback;
            _ghostList.onAddCallback -= OnAddCallback;
            _ghostList.onRemoveCallback -= OnRemoveCallback;
        }

        #region Prefabs

        private void PrefabDrawElementCallback(Rect rect, int index, bool isactive, bool isfocused)
        {
            var collectionTarget = target as GhostCollectionConfig;
            var ghost = collectionTarget.Prefabs[index];

            EditorGUI.BeginChangeCheck();
            rect.width -= rect.height + 3;
            ghost =
                EditorGUI.ObjectField(rect, ghost, typeof(GameObject), true) as GameObject;
            collectionTarget.Prefabs[index] = ghost;
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
                _isChange = true;
            }
        }

        private void PrefabRemoveCallback(ReorderableList list)
        {
            var collectionTarget = target as GhostCollectionConfig;
            collectionTarget.Prefabs.RemoveAt(list.index);
            EditorUtility.SetDirty(target);
            _isChange = true;
        }

        private void PrefabAddCallback(ReorderableList list)
        {
            var collectionTarget = target as GhostCollectionConfig;
            collectionTarget.Prefabs.Add(null);
            EditorUtility.SetDirty(target);
            _isChange = true;
        }

        private void PrefabDrawHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, "Prefabs");
        }

        #endregion

        #region Ghosts

        private void OnRemoveCallback(ReorderableList list)
        {
            var collectionTarget = target as GhostCollectionConfig;
            collectionTarget.Ghosts.RemoveAt(list.index);
            EditorUtility.SetDirty(target);

            _isChange = true;
        }

        private void OnAddCallback(ReorderableList list)
        {
            var collectionTarget = target as GhostCollectionConfig;
            collectionTarget.Ghosts.Add(new GhostCollectionConfig.Ghost {enabled = true});
            EditorUtility.SetDirty(target);
            _isChange = true;
        }

        private void DrawElementCallback(Rect rect, int index, bool isactive, bool isfocused)
        {
            var collectionTarget = target as GhostCollectionConfig;
            var ghost = collectionTarget.Ghosts[index];

            var tmpColor = GUI.color;
            if (ghost.prefab != null &&
                ghost.prefab.Type == GhostAuthoringComponent.ClientInstanceType.OwnerPredicted &&
                ghost.prefab.GetComponent<GhostOwnerAuthoringComponent>())
            {
                GUI.color = Color.green;
            }

            EditorGUI.BeginChangeCheck();
            rect.width -= rect.height + 3;
            ghost.prefab =
                EditorGUI.ObjectField(rect, ghost.prefab, typeof(GhostAuthoringComponent), true) as
                    GhostAuthoringComponent;
            rect.x += rect.width + 3;
            rect.width = rect.height;
            ghost.enabled = EditorGUI.Toggle(rect, ghost.enabled);
            collectionTarget.Ghosts[index] = ghost;
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
                _isChange = true;
            }

            GUI.color = tmpColor;
        }

        private void DrawHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, "Ghosts");
        }

        #endregion

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space(10);
            _ghostList.DoLayoutList();

            EditorGUILayout.Space(10);
            _prefabList.DoLayoutList();

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        public static void AddAllNewGhosts(GhostCollectionConfig collectionConfigTarget)
        {
            var list = collectionConfigTarget.Ghosts;
            var alreadyAdded = new HashSet<GhostAuthoringComponent>();
            bool hasEmpty = false;
            foreach (var ghost in list)
            {
                if (ghost.prefab != null)
                    alreadyAdded.Add(ghost.prefab);
                else
                    hasEmpty = true;
            }

            if (hasEmpty)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i].prefab == null)
                    {
                        list.RemoveAt(i);
                        --i;
                        EditorUtility.SetDirty(collectionConfigTarget);
                    }
                }
            }

            var prefabGuids = AssetDatabase.FindAssets("t:" + typeof(GameObject).Name);
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var ghost = go.GetComponent<GhostAuthoringComponent>();
                if (ghost != null && !alreadyAdded.Contains(ghost))
                {
                    list.Add(new GhostCollectionConfig.Ghost {prefab = ghost, enabled = true});
                    EditorUtility.SetDirty(collectionConfigTarget);
                }
            }
        }

        private readonly GUIContent m_WrapperCodePathLabel = EditorGUIUtility.TrTextContent("C# Class File");
        private readonly GUIContent m_WrapperClassNameLabel = EditorGUIUtility.TrTextContent("C# Class Name");
        private readonly GUIContent m_WrapperCodeNamespaceLabel = EditorGUIUtility.TrTextContent("C# Class Namespace");
    }
}