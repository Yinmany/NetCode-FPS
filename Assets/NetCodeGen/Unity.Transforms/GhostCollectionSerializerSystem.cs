
using MyGameLib.NetCode;
using Unity.Entities;

namespace Unity.Transforms.Generated
{
    [UpdateInGroup(typeof(ClientAndServerInitializationSystemGroup))]
    internal class GhostCollectionSerializerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            var ghostCollectionSystem = World.GetOrCreateSystem<GhostCollectionSystem>();

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