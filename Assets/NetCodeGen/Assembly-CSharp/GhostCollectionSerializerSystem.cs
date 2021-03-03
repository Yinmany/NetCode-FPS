
using MyGameLib.NetCode;
using Unity.Entities;

namespace Assembly_CSharp.Generated
{
    [UpdateInGroup(typeof(ClientAndServerInitializationSystemGroup))]
    internal class GhostCollectionSerializerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            var ghostCollectionSystem = World.GetOrCreateSystem<GhostCollectionSystem>();

            ghostCollectionSystem.Register(NetworkRigidbodySerializer.Serializer);
            ghostCollectionSystem.Register(NetworkCharacterComponentSerializer.Serializer);
        }

        protected override void OnUpdate()
        {
            var parentGroup = World.GetExistingSystem<InitializationSystemGroup>();
            parentGroup?.RemoveSystemFromUpdateList(this);
        }
    }
}