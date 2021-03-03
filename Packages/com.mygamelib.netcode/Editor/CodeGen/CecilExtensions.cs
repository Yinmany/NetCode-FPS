using System;
using System.Linq;

namespace MyGameLib.NetCode.Editor
{
    public static class CecilExtensions
    {
        public static bool HasAttribute<T>(this Mono.Cecil.ICustomAttributeProvider type) where T : Attribute
        {
            return type.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(T).FullName);
        }

        public static Mono.Cecil.CustomAttribute GetAttribute<T>(this Mono.Cecil.ICustomAttributeProvider type)
            where T : Attribute
        {
            return type.CustomAttributes.Where(a => a.AttributeType.FullName == typeof(T).FullName).FirstOrDefault();
        }

        public static GhostFieldAttribute GetGhostFieldAttribute(Mono.Cecil.TypeReference parentType,
            Mono.Cecil.FieldDefinition componentField)
        {
            var attribute = componentField.GetAttribute<GhostFieldAttribute>();
            if (attribute != null)
            {
                var fieldAttribute = new GhostFieldAttribute();
                if (attribute.HasProperties)
                {
                    foreach (var a in attribute.Properties)
                    {
                        typeof(GhostFieldAttribute).GetProperty(a.Name)?.SetValue(fieldAttribute, a.Argument.Value);
                    }
                }

                return fieldAttribute;
            }

            return default(GhostFieldAttribute);
        }

        public static GhostComponentAttribute GetGhostComponentAttribute(Mono.Cecil.TypeDefinition managedType)
        {
            var attribute = managedType.GetAttribute<GhostComponentAttribute>();
            if (attribute != null)
            {
                var ghostAttribute = new GhostComponentAttribute();
                if (attribute.HasProperties)
                {
                    foreach (var a in attribute.Properties)
                    {
                        typeof(GhostComponentAttribute).GetProperty(a.Name)?.SetValue(ghostAttribute, a.Argument.Value);
                    }
                }

                return ghostAttribute;
            }

            return null;
        }

        public static bool IsGhostField(this Mono.Cecil.FieldDefinition fieldDefinition)
        {
            return fieldDefinition.HasAttribute<GhostFieldAttribute>();
        }

        public static bool IsGhostComponent(this Mono.Cecil.TypeReference typeReference)
        {
            var resolvedType = typeReference.Resolve();
            if (resolvedType == null) return false;

            return resolvedType.CustomAttributes.Any(a =>
                a.AttributeType.FullName == typeof(GhostComponentAttribute).FullName);
        }

        /// <summary>
        /// 只要同步组件有一个字段需要同步就需要生成代码.
        /// </summary>
        /// <param name="typeDefinition"></param>
        /// <returns></returns>
        public static bool IsSyncGhostComponent(this Mono.Cecil.TypeDefinition typeDefinition)
        {
            bool isTure = false;
            foreach (var fieldDefinition in typeDefinition.Fields)
            {
                if (!fieldDefinition.FieldType.IsValueType || fieldDefinition.Name.StartsWith("<"))
                    continue;
                isTure = fieldDefinition.IsGhostField();
                if (isTure)
                {
                    break;
                }
            }

            return isTure;
        }

        public static bool IsIRpcCommand(this Mono.Cecil.TypeReference typeReference)
        {
            var resolvedType = typeReference.Resolve();
            if (resolvedType == null) return false;
            return resolvedType.Interfaces.Any(i => i.InterfaceType.Name == typeof(IRpcCommand).Name &&
                                                    i.InterfaceType.Namespace == typeof(IRpcCommand).Namespace);
        }

        public static bool IsICommandData(this Mono.Cecil.TypeReference typedReference)
        {
            var resolvedType = typedReference.Resolve();
            if (resolvedType == null) return false;
            return resolvedType.Interfaces.Any(i => i.InterfaceType.Name == typeof(ICommandData).Name &&
                                                    i.InterfaceType.Namespace == typeof(ICommandData).Namespace);
        }
    }
}