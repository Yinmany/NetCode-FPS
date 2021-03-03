using Unity.Entities;

namespace MyGameLib.NetCode
{
    [UpdateAfter(typeof(NetworkStreamReceiveSystem))]
    [UpdateInGroup(typeof(NetworkReceiveSystemGroup))]
    public class NetworkStreamCloseSystem : ComponentSystem
    {
        protected override void OnCreate()
        {

        }

        protected override void OnUpdate()
        {
            Entities.WithAll<NetworkStreamDisconnected>().ForEach(session =>
            {
                PostUpdateCommands.DestroyEntity(session);
            });
        }
    }
}