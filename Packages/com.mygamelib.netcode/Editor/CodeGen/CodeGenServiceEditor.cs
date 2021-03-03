using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.TextTemplating;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;

namespace MyGameLib.NetCode.Editor
{
    [InitializeOnLoad]
    [CustomEditor(typeof(CodeGenService))]
    public partial class CodeGenServiceEditor : UnityEditor.Editor
    {
        public const string RootPath = "Packages/com.mygamelib.netcode/Editor/CodeGenTemplates/";
        public const string CommandDataSerializerPath = RootPath + "CommandDataSerializer.tt";
        public const string RpcDataSerializerPath = RootPath + "RpcCommandSerializer.tt";
        public const string GhostComponentSerializerPath = RootPath + "GhostComponentSerializer.tt";
        public const string GhostComponentSerializerSystemPath = RootPath + "GhostCollectionSerializerSystem.tt";

        private SerializedProperty __assembiesDefaultOverridesProp;
        private SerializedProperty __ignoreTypes;
        private string _curAddAssemblyName = "";
        private ReorderableList _ignoreTypesList;

        private void OnEnable()
        {
            __assembiesDefaultOverridesProp = serializedObject.FindProperty("_assembiesDefaultOverrides");
            __ignoreTypes = serializedObject.FindProperty("_ignoreTypes");

            _config = (CodeGenService) target;
            _configProp = serializedObject.FindProperty(nameof(CodeGenService.Configs));

            _ignoreTypesList = new ReorderableList(serializedObject, __ignoreTypes, true, true, true, true);
            _ignoreTypesList.drawHeaderCallback += DrawHeaderCallback;
            _ignoreTypesList.drawElementCallback += DrawElementCallback;
        }

        private void OnDisable()
        {
            _ignoreTypesList.drawHeaderCallback -= DrawHeaderCallback;
            _ignoreTypesList.drawElementCallback -= DrawElementCallback;
        }

        private void DrawElementCallback(Rect rect, int index, bool isactive, bool isfocused)
        {
            var e = __ignoreTypes.GetArrayElementAtIndex(index);
            if (isactive)
            {
                EditorGUI.PropertyField(rect, e, new GUIContent());
            }
            else
            {
                EditorGUI.LabelField(rect, e.stringValue);
            }
        }

        private void DrawHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, "忽略生成类型");
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Space(10);

            if (GUILayout.Button("生成代码"))
            {
                EditorUtility.DisplayProgressBar("Generating Ghosts", "开始生成", 0f);
                CodeGen();
                EditorUtility.ClearProgressBar();
            }

            serializedObject.Update();
            OnAssembiesGUI();
            _ignoreTypesList.DoLayoutList();
            OnComponentOverrideGUI();
            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }

        private void OnAssembiesGUI()
        {
            var codeGenService = (target as CodeGenService);

            if (codeGenService != null)
            {
                EditorGUILayout.Separator();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField("AssembliesDefaultOverrides");
                EditorGUILayout.BeginHorizontal("box");
                _curAddAssemblyName = EditorGUILayout.TextField(_curAddAssemblyName);
                if (GUILayout.Button("+", GUILayout.Width(30f)))
                {
                    if (!string.IsNullOrEmpty(_curAddAssemblyName) &&
                        !codeGenService.AssembliesDefaultOverrides.Contains(_curAddAssemblyName))
                    {
                        codeGenService.AssembliesDefaultOverrides.Add(_curAddAssemblyName);
                        _curAddAssemblyName = "";
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < __assembiesDefaultOverridesProp.arraySize; i++)
                {
                    var e = __assembiesDefaultOverridesProp.GetArrayElementAtIndex(i);

                    EditorGUILayout.BeginHorizontal("box");
                    GUILayout.Label($"{e.stringValue}");
                    if (GUILayout.Button("X", GUILayout.Width(30f)))
                    {
                        codeGenService.AssembliesDefaultOverrides.Remove(e.stringValue);
                        serializedObject.ApplyModifiedProperties();
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();

                // EditorGUILayout.BeginFoldoutHeaderGroup(true, "", EditorStyles.helpBox);
                // EditorGUILayout.InspectorTitlebar(,);
            }
        }

        private void SetProgressBar(string info, float f)
        {
            EditorUtility.DisplayProgressBar("Generating Ghosts", info, f);
        }
    }

    public partial class CodeGenServiceEditor
    {
        private CodeGenService _config;

        private SerializedProperty _configProp;

        public void OnComponentOverrideGUI()
        {
            int removeIdx = -1;
            for (int i = 0; i < _configProp.arraySize; i++)
            {
                var ghostCompProp = _configProp.GetArrayElementAtIndex(i);
                if (OnGhostComponentGUI(ghostCompProp, i))
                {
                    removeIdx = i;
                }
            }

            if (removeIdx != -1)
                _configProp.DeleteArrayElementAtIndex(removeIdx);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add GhostComponentOverride"))
            {
                _config.Configs.Add(new CodeGenService.GhostComponent
                {
                    IsUpdateValue = false,
                    SendType = GhostSendType.All,
                    PrefabType = GhostPrefabType.All,
                    Enable = true,
                    Fields = new List<CodeGenService.GhostComponentField>()
                });
                EditorUtility.SetDirty(target);
            }

            GUILayout.EndHorizontal();
        }

        private bool OnGhostComponentGUI(SerializedProperty ghostCompProp, int index)
        {
            var isExpandedProp =
                ghostCompProp.FindPropertyRelative(nameof(CodeGenService.GhostComponent.IsExpanded));
            var nameProp = ghostCompProp.FindPropertyRelative(nameof(CodeGenService.GhostComponent.Name));
            var fieldsProp = ghostCompProp.FindPropertyRelative(nameof(CodeGenService.GhostComponent.Fields));
            var enableProp = ghostCompProp.FindPropertyRelative(nameof(CodeGenService.GhostComponent.Enable));
            var isUpdateValue = ghostCompProp.FindPropertyRelative(nameof(CodeGenService.GhostComponent.IsUpdateValue));
            var prefabType = ghostCompProp.FindPropertyRelative(nameof(CodeGenService.GhostComponent.PrefabType));
            var sendType = ghostCompProp.FindPropertyRelative(nameof(CodeGenService.GhostComponent.SendType));

            string title = nameProp.stringValue;

            GUIStyle style = null;
            if (!enableProp.boolValue)
            {
                style = new GUIStyle(EditorStyles.foldoutHeader);
                style.fontStyle = FontStyle.Normal;
            }

            // if (!enableProp.boolValue)
            // {
            //     title += "[x]";
            // }

            isExpandedProp.boolValue =
                EditorGUILayout.BeginFoldoutHeaderGroup(isExpandedProp.boolValue, title, style);

            bool isRemove = false;
            if (isExpandedProp.boolValue)
            {
                ++EditorGUI.indentLevel;

                GUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.PropertyField(enableProp);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(nameProp, new GUIContent("FullName"));
                    isRemove = GUILayout.Button("X", "button", GUILayout.Width(20));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.PropertyField(isUpdateValue);
                    EditorGUILayout.PropertyField(prefabType);
                    EditorGUILayout.PropertyField(sendType);
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Fields[{fieldsProp.arraySize}]");
                    if (GUILayout.Button("Add Field"))
                    {
                        fieldsProp.InsertArrayElementAtIndex(fieldsProp.arraySize);
                        var tmpValue = fieldsProp.GetArrayElementAtIndex(fieldsProp.arraySize - 1);
                        tmpValue.FindPropertyRelative(nameof(CodeGenService.GhostComponentField.SendData))
                            .boolValue = true;
                    }

                    GUILayout.EndHorizontal();


                    // 显示Fields
                    ++EditorGUI.indentLevel;
                    int removeIdx = -1;
                    for (int i = 0; i < fieldsProp.arraySize; i++)
                    {
                        var fieldItem = fieldsProp.GetArrayElementAtIndex(i);
                        if (OnShowFieldGUI(fieldItem)) removeIdx = i;
                    }

                    if (removeIdx != -1)
                        fieldsProp.DeleteArrayElementAtIndex(removeIdx);

                    --EditorGUI.indentLevel;
                }
                GUILayout.EndVertical();
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            return isRemove;
        }

        private bool OnShowFieldGUI(SerializedProperty field)
        {
            var nameProp = field.FindPropertyRelative(nameof(CodeGenService.GhostComponentField.Name));
            var interpolateProp =
                field.FindPropertyRelative(nameof(CodeGenService.GhostComponentField.Interpolate));
            var sendDataProp = field.FindPropertyRelative(nameof(CodeGenService.GhostComponentField.SendData));

            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(nameProp);
            bool isRemove = GUILayout.Button("X", "button", GUILayout.Width(20));
            GUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(interpolateProp);
            EditorGUILayout.PropertyField(sendDataProp);

            GUILayout.EndVertical();

            GUILayout.Space(1f);

            return isRemove;
        }
    }

    public partial class CodeGenServiceEditor
    {
        private void CodeGen()
        {
            GhostPostProcessorResolver assemblyResolver = new GhostPostProcessorResolver();

            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            List<Assembly> processAssemblies = new List<Assembly>();
            foreach (Assembly assembly in assemblies)
            {
                if (WillProcess(assembly))
                {
                    processAssemblies.Add(assembly);
                    assemblyResolver.resolvePaths.Add(Path.GetDirectoryName(assembly.outputPath));
                    Debug.Log($"[程序集] -> {assembly.name}");
                }
            }

            // 扫描要生成的类型
            foreach (Assembly processAssembly in processAssemblies)
            {
                using (AssemblyDefinition assemblyDefinition =
                    assemblyResolver.Resolve(processAssembly.name, new ReaderParameters
                    {
                        ReadSymbols = false,
                        ReadWrite = false
                    }))
                {
                    CodeGenAssembly genAssembly = new CodeGenAssembly(assemblyDefinition.Name.Name);

                    foreach (var typeDefinition in assemblyDefinition.MainModule.Types)
                    {
                        if (!typeDefinition.IsValueType)
                            continue;

                        if (_config.IgnoreTypes.Any(f => f == typeDefinition.FullName))
                        {
                            continue;
                        }

                        // 重载优先
                        if (IsOverrideGhostComponent(typeDefinition, _config.Configs, out var config))
                        {
                            if (config.Fields.Count > 0)
                            {
                                genAssembly.AddType(new GhostComponentGenType(typeDefinition, config));
                            }
                        }
                        else if (typeDefinition.IsGhostComponent() && typeDefinition.IsSyncGhostComponent())
                        {
                            // 频断是否有同步字段
                            genAssembly.AddType(new GhostComponentGenType(typeDefinition));
                        }

                        // 关闭自动生成
                        if (typeDefinition.HasAttribute<DisableCommandCodeGenAttribute>())
                            continue;

                        if (typeDefinition.IsICommandData())
                        {
                            genAssembly.AddType(new CommandGenType(typeDefinition));
                        }
                        else if (typeDefinition.IsIRpcCommand())
                        {
                            genAssembly.AddType(new RpcCommandGenType(typeDefinition));
                        }
                    }

                    genAssembly.AddType(
                        new GhostCollectionSerializerSystemCodeGenType(assemblyDefinition, genAssembly));

                    genAssembly.ProcessAction = SetProgressBar;
                    genAssembly.StartGen(GetOutDirectory());

                    if (genAssembly.TemplateGenerator.Errors.HasErrors)
                    {
                        foreach (CompilerError compilerError in genAssembly.TemplateGenerator.Errors)
                        {
                            Debug.LogError(compilerError.ToString());
                        }
                    }
                }
            }
        }

        private string GetOutDirectory(string append = "")
        {
            string path = Path.Combine("Assets", "NetCodeGen", append);
            return path;
        }

        private bool WillProcess(Assembly editorAssembly)
        {
            if (editorAssembly.name.Contains(".Generated"))
                return false;

            for (int i = 0; i < __assembiesDefaultOverridesProp.arraySize; i++)
            {
                var name = __assembiesDefaultOverridesProp.GetArrayElementAtIndex(i).stringValue;
                if (editorAssembly.name == name)
                    return true;
            }

            if (!editorAssembly.assemblyReferences.Any(a => a.name == "Unity.Entities"))
                return false;
            if (!editorAssembly.assemblyReferences.Any(a => a.name == "MyGameLib.NetCode"))
                return false;

            if (editorAssembly.flags.HasFlag(AssemblyFlags.EditorAssembly))
                return false;
            if (editorAssembly.compiledAssemblyReferences.Any(a => a.EndsWith("nunit.framework.dll")))
                return false;

            return true;
        }

        private static bool IsOverrideGhostComponent(TypeDefinition typeDefinition,
            List<CodeGenService.GhostComponent> components, out CodeGenService.GhostComponent config)
        {
            config = components.FirstOrDefault(f => f.Name == typeDefinition.FullName);
            return config != null && config.Enable;
        }

        private static string GetNamespace(string s) => s.Replace("-", "_").Replace(" ", "_");
    }
}