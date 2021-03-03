using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TextTemplating;
using Mono.Cecil;
using Mono.TextTemplating;
using Unity.Mathematics;
using UnityEngine;

namespace MyGameLib.NetCode.Editor
{
    /// <summary>
    /// 一个生成程序集
    /// </summary>
    public class CodeGenAssembly : IEnumerable<CodeGenBaseType>
    {
        private string _name;

        private List<CodeGenBaseType> _types = new List<CodeGenBaseType>();

        public int TypeCount => _types.Count;

        public Action<string, float> ProcessAction;

        public CodeGenAssembly(string assemblyName)
        {
            _name = assemblyName;
            TemplateGenerator = new TemplateGenerator();
        }

        public TemplateGenerator TemplateGenerator { get; }

        public void AddType(CodeGenBaseType type)
        {
            _types.Add(type);
        }

        public void StartGen(string parentPath)
        {
            parentPath = Path.Combine(parentPath, _name);

            var session = TemplateGenerator.GetOrCreateSession();

            float count = _types.Count;
            var i = 0;
            foreach (CodeGenBaseType codeGenType in _types)
            {
                ProcessAction?.Invoke($"{_name} -> {codeGenType.Name}", i / count);
                session.Clear();
                codeGenType.TemplateGenerator = TemplateGenerator;
                codeGenType.Session = session;
                codeGenType.GenCode(parentPath);
                ++i;
            }
        }

        public IEnumerator<CodeGenBaseType> GetEnumerator()
        {
            return _types.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// 最基础生成类型
    /// </summary>
    public abstract class CodeGenBaseType
    {
        protected CommonT4Info templateArgs;
        protected string templateFilePath = "";
        protected string outFileName;
        public TemplateGenerator TemplateGenerator;
        public ITextTemplatingSession Session;

        public string Name => templateArgs.TypeName;

        protected static string ToUpper(string s) => s.Substring(0, 1).ToUpper() + s.Substring(1);

        public static string GetNamespace(string s) => s.Replace("-", "_").Replace(" ", "_");

        public virtual void GenCode(string parentPath)
        {
            Debug.Log($"[开始生成] {this.ToString()}...");
            if (!Directory.Exists(parentPath))
                Directory.CreateDirectory(parentPath);
            Session.Add("m", templateArgs);
            string outFilePath = Path.Combine(parentPath, outFileName);
            TemplateGenerator.ProcessTemplate(templateFilePath, outFilePath);
            Debug.Log($"[生成成功] {this.ToString()}");
        }

        public override string ToString()
        {
            return $"{templateArgs.CurNamespace}.{templateArgs.TypeName}";
        }
    }

    public class GhostCollectionSerializerSystemCodeGenType : CodeGenBaseType
    {
        public GhostCollectionSerializerSystemCodeGenType(AssemblyDefinition assemblyDefinition,
            CodeGenAssembly assemblies)
        {
            templateFilePath = CodeGenServiceEditor.GhostComponentSerializerSystemPath;

            templateArgs = new CommonT4Info
            {
                ToNamespace = GetNamespace(assemblyDefinition.Name.Name),
                Fields = new List<FieldT4Info>()
            };

            foreach (var codeGenBaseType in assemblies)
            {
                if (codeGenBaseType is GhostComponentGenType type)
                {
                    templateArgs.Fields.Add(new FieldT4Info
                    {
                        Name = type.Name
                    });
                }
            }

            outFileName = $"GhostCollectionSerializerSystem.cs";
        }

        public override void GenCode(string parentPath)
        {
            if (templateArgs.Fields.Count > 0)
            {
                base.GenCode(parentPath);
            }
        }

        public override string ToString()
        {
            return outFileName;
        }
    }

    /// <summary>
    /// 基础类型
    /// </summary>
    public abstract class CodeGenType : CodeGenBaseType
    {
        private CodeGenService.GhostComponent _overrideConfig;


        public bool IsOverride => _overrideConfig != null;

        public CodeGenType(TypeDefinition typeDefinition, CodeGenService.GhostComponent config = null)
        {
            _overrideConfig = config;

            // 模板参数
            templateArgs = new CommonT4Info
            {
                TypeName = typeDefinition.Name,
                CurNamespace = typeDefinition.Namespace,
                ToNamespace = GetNamespace(typeDefinition.Module.Assembly.Name.Name),
                Fields = new List<FieldT4Info>()
            };

            outFileName = $"{templateArgs.TypeName}Serializer.cs";
        }
    }

    /// <summary>
    /// Command生成
    /// </summary>
    public class CommandGenType : CodeGenType
    {
        public CommandGenType(TypeDefinition typeDefinition, CodeGenService.GhostComponent config = null) : base(
            typeDefinition, config)
        {
            templateFilePath = CodeGenServiceEditor.CommandDataSerializerPath;

            foreach (var fieldDefinition in typeDefinition.Fields)
            {
                if (!fieldDefinition.FieldType.IsValueType || fieldDefinition.Name.StartsWith("<"))
                    continue;

                TypeReference fieldType = fieldDefinition.FieldType;

                templateArgs.Fields.Add(new FieldT4Info
                {
                    Name = fieldDefinition.Name,
                    TypeName1 = ToUpper(fieldType.Name),
                });
            }
        }
    }

    public class RpcCommandGenType : CommandGenType
    {
        public RpcCommandGenType(TypeDefinition typeDefinition, CodeGenService.GhostComponent config = null) : base(
            typeDefinition, config)
        {
            templateFilePath = CodeGenServiceEditor.RpcDataSerializerPath;
        }
    }

    public class GhostComponentGenType : CodeGenType
    {
        public GhostComponentGenType(TypeDefinition typeDefinition, CodeGenService.GhostComponent config = null) : base(
            typeDefinition, config)
        {
            templateFilePath = CodeGenServiceEditor.GhostComponentSerializerPath;

            GhostComponentAttribute ghostComponentAttribute = null;
            if (config != null)
            {
                ghostComponentAttribute = config.Attribute;
            }
            else
            {
                ghostComponentAttribute = CecilExtensions.GetGhostComponentAttribute(typeDefinition);
            }

            if (ghostComponentAttribute == null)
            {
                Debug.LogError(typeDefinition.FullName);
            }

            templateArgs.IsUpdateValue = ghostComponentAttribute.IsUpdateValue ? "true" : "false";
            templateArgs.SendType = ghostComponentAttribute.SendType.ToString();
            templateArgs.Fields = new List<FieldT4Info>();

            foreach (var fieldDefinition in typeDefinition.Fields)
            {
                if (!fieldDefinition.FieldType.IsValueType || fieldDefinition.Name.StartsWith("<"))
                    continue;

                bool isGhostField = fieldDefinition.IsGhostField();

                CodeGenService.GhostComponentField field = null;
                if (config != null)
                {
                    field = config.Fields.FirstOrDefault(f => f.Name == fieldDefinition.Name);
                    isGhostField = field != null;
                }

                if (!isGhostField)
                    continue;

                GhostFieldAttribute attr = null;
                if (field != null)
                {
                    attr = field.Attribute;
                }
                else
                {
                    attr = CecilExtensions.GetGhostFieldAttribute(typeDefinition, fieldDefinition);
                }

                TypeReference fieldType = fieldDefinition.FieldType;

                string type = ConvertBaseValueType(fieldType.Name);
                templateArgs.Fields.Add(new FieldT4Info
                {
                    Name = fieldDefinition.Name,
                    TypeName = type,
                    TypeName1 = ToUpper(type),
                    Interpolate = attr.Interpolate,
                    SendData = attr.SendData,
                    InterpolateMethodStr = GetInterpolateMethodStr(type)
                });
            }
        }

        private string ConvertBaseValueType(string typeName)
        {
            if (typeName == nameof(Int32))
            {
                return "int";
            }

            if (typeName == nameof(Single))
            {
                return "float";
            }


            return typeName;
        }

        private string GetInterpolateMethodStr(string typeName)
        {
            if (typeName == nameof(quaternion))
            {
                return "math.slerp";
            }

            return "math.lerp";
        }
    }
}