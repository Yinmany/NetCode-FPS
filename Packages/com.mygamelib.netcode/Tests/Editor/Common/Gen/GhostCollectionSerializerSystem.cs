using Unity.Entities;
using Unity.Transforms.Generated;

namespace MyGameLib.NetCode.Tests
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(ClientAndServerInitializationSystemGroup))]
    internal class GhostCollectionSerializerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            var ghostCollectionSystem = World.GetOrCreateSystem<GhostCollectionSystem>();

            ghostCollectionSystem.Register(GhostOwnerComponentSerializer.Serializer);
            ghostCollectionSystem.Register(RotationSerializer.Serializer);
            ghostCollectionSystem.Register(TranslationSerializer.Serializer);
        }

        protected override void OnUpdate()
        {
            var parentGroup = World.GetExistingSystem<InitializationSystemGroup>();
            parentGroup?.RemoveSystemFromUpdateList(this);
        }
    }
}