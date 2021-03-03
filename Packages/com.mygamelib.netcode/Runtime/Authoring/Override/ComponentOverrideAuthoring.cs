using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace MyGameLib.NetCode
{
    // public class ComponentOverrideAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    // {
    //     public ComponentOverrideConfig Config;
    //
    //     public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    //     {
    //     }
    // }
    //
    // [UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
    // public class ComponentOverrideConversion : GameObjectConversionSystem
    // {
    //     protected override void OnUpdate()
    //     {
    //         Entities.ForEach((ComponentOverrideAuthoring comp) =>
    //         {
    //             if (comp.Config == null) return;
    //
    //             Entity ent = GetPrimaryEntity(comp);
    //
    //             var ghostAuthoringComponent = comp.GetComponent<GhostAuthoringComponent>();
    //             if (ghostAuthoringComponent == null)
    //             {
    //                 List<string> toRemove = new List<string>();
    //                 HandlePrefab(comp.Config, toRemove);
    //                 RemoveComponents(ent, toRemove);
    //             }
    //         });
    //     }
    //
    //     private void HandlePrefab(ComponentOverrideConfig config, List<string> toRemove)
    //     {
    //         foreach (ComponentOverrideConfig item in config.Includes)
    //         {
    //             HandlePrefab(item, toRemove);
    //         }
    //
    //         foreach (ComponentOverrideConfig.GhostComponent item in config.Configs)
    //         {
    //             if (item.Attribute.PrefabType != GhostPrefabType.None) continue;
    //
    //             // 移除此组件
    //             if (!string.IsNullOrEmpty(item.Name))
    //             {
    //                 Debug.Log($"PrefabType = {item.Attribute.PrefabType} {item.Name}");
    //                 toRemove.Add(item.Name);
    //             }
    //         }
    //     }
    //
    //     private void RemoveComponents(Entity ent, List<string> toRemove)
    //     {
    //         NativeArray<ComponentType> componentTypes = DstEntityManager.GetComponentTypes(ent);
    //         foreach (ComponentType type in componentTypes)
    //         {
    //             Debug.Log($"移除组件==========:{type.GetManagedType().FullName}");
    //             if (toRemove.Contains(type.GetManagedType().FullName))
    //             {
    //                 DstEntityManager.RemoveComponent(ent, type);
    //             }
    //         }
    //
    //         componentTypes.Dispose();
    //     }
    // }
}