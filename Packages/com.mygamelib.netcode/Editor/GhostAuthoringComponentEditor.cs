using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEditorInternal;

namespace MyGameLib.NetCode.Editor
{
    [CustomEditor(typeof(GhostAuthoringComponent))]
    public class GhostAuthoringComponentEditor : UnityEditor.Editor
    {
        private GhostAuthoringComponent ghost;

        private SerializedProperty defaultClientInstantiationType;
        private SerializedProperty ghostComponents;
        private SerializedProperty ghostFields;

        private ReorderableList _ghostList;
        private IList _serializers;

        public void OnEnable()
        {
            ghost = (GhostAuthoringComponent) this.target;

            defaultClientInstantiationType = serializedObject.FindProperty(nameof(GhostAuthoringComponent.Type));
            ghostComponents = serializedObject.FindProperty(nameof(GhostAuthoringComponent.Components));

            // _serializers = ghost.Components.Where(f => f.isSerializer).ToList();
            //
            // _ghostList = new ReorderableList(_serializers,
            //     typeof(ComponentType),
            //     false, true, false, false);
            // _ghostList.drawHeaderCallback += DrawHeaderCallback;
            // _ghostList.drawElementCallback += DrawElementCallback;
        }

        private void DrawElementCallback(Rect rect, int index, bool isactive, bool isfocused)
        {
            var serializerComponent = (GhostAuthoringComponent.GhostComponentInfo) _ghostList.list[index];
            rect.width -= rect.height + 3;
            EditorGUI.LabelField(rect, serializerComponent.name);

            EditorGUI.BeginChangeCheck();
            rect.x = rect.width - 50;
            rect.width = 80;
            // EditorGUI.LabelField(rect, "预测:");

            rect.x += 30;
            serializerComponent.predictedClient = EditorGUI.Toggle(rect, serializerComponent.predictedClient);

            rect.x += 27;
            // EditorGUI.LabelField(rect, "插值:");
            rect.x += 27;
            serializerComponent.interpolatedClient = EditorGUI.Toggle(rect, serializerComponent.interpolatedClient);
            _ghostList.list[index] = serializerComponent;
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(target);
        }

        private void DrawHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, "序列化组件:");
        }

        private void ShowComponent(SerializedProperty comp)
        {
            // var isSerializer =
            //     comp.FindPropertyRelative(nameof(GhostAuthoringComponent.GhostComponentInfo.isSerializer));
            //
            // if (isSerializer.boolValue)
            // {
            //     return;
            // }

            var fieldName = comp.FindPropertyRelative(nameof(GhostAuthoringComponent.GhostComponentInfo.name));
            var server = comp.FindPropertyRelative(nameof(GhostAuthoringComponent.GhostComponentInfo.server));
            var interpolatedClient =
                comp.FindPropertyRelative(nameof(GhostAuthoringComponent.GhostComponentInfo.interpolatedClient));
            var predictedClient =
                comp.FindPropertyRelative(nameof(GhostAuthoringComponent.GhostComponentInfo.predictedClient));

            GUIStyle style = null;
            style = new GUIStyle(EditorStyles.foldoutHeader);
            style.fontStyle = FontStyle.Normal;


            string title = System.String.Format("{0}{1} ({2}/{3}/{4})",
                "",
                fieldName.stringValue,
                server.boolValue ? "S" : "-",
                interpolatedClient.boolValue ? "IC" : "-",
                predictedClient.boolValue ? "PC" : "-");

            comp.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(comp.isExpanded, title, style);
            if (comp.isExpanded)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(server);
                EditorGUILayout.PropertyField(interpolatedClient);
                EditorGUILayout.PropertyField(predictedClient);
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(defaultClientInstantiationType);

            for (int i = 0; i < ghostComponents.arraySize; i++)
            {
                ShowComponent(ghostComponents.GetArrayElementAtIndex(i));
            }

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Separator();

            // _ghostList.DoLayoutList();

            if (GUILayout.Button("Update Component List"))
            {
                SyncComponent();
            }
        }

        private void SyncComponent()
        {
            // _serializers.Clear();

            Dictionary<string, GhostAuthoringComponent.GhostComponentInfo> newComp =
                new Dictionary<string, GhostAuthoringComponent.GhostComponentInfo>();

            foreach (var component in ghost.gameObject.GetComponents(typeof(Component)))
            {
                string name = component.GetType().FullName;

                if (name == typeof(GhostAuthoringComponent).FullName) continue;

                if (component is IConvertGameObjectToEntity && !(component is IConvertNotRemove)) continue;

                var ghostConfig = new GhostAuthoringComponent.GhostComponentInfo
                {
                    name = name,
                    server = true,
                    interpolatedClient = true,
                    predictedClient = true
                };
                newComp.Add(name, ghostConfig);

                // if (ghostConfig.isSerializer)
                //     _serializers.Add(ghostConfig);
            }

            // 使用之前的配置覆盖新扫描出来的数据
            foreach (var key in newComp.Keys.ToArray())
            {
                for (int i = 0; i < ghost.Components.Length; i++)
                {
                    if (ghost.Components[i].name == key)
                    {
                        var value = newComp[key];
                        value.server = ghost.Components[i].server;
                        value.interpolatedClient = ghost.Components[i].interpolatedClient;
                        value.predictedClient = ghost.Components[i].predictedClient;
                        newComp[key] = value;
                    }
                }
            }

            ghost.Components = newComp.Values.ToArray();

            EditorUtility.SetDirty(ghost);

            // DOTS
            // using (var tmpWorld = new World("TempGhostConversion"))
            // using (var blobAssetStore = new BlobAssetStore())
            // {
            //     var convertedEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(ghost.gameObject,
            //         GameObjectConversionSettings.FromWorld(tmpWorld, blobAssetStore));
            //
            //     NativeArray<ComponentType> componentTypes =
            //         tmpWorld.EntityManager.GetComponentTypes(convertedEntity);
            //
            //     ghost.SerializerComponents = componentTypes.ToArray();
            // }
        }

        private static bool HasInterface(Component component)
        {
            var interfaces = component.GetType().GetInterfaces();
            bool isContinue = false;
            for (int i = 0; i < interfaces.Length; i++)
            {
                if (interfaces[i] == typeof(IConvertGameObjectToEntity))
                {
                    isContinue = true;
                    break;
                }
            }

            if (isContinue) return true;
            return false;
        }
    }
}