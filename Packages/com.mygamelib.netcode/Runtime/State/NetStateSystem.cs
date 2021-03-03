using Unity.Entities;

namespace MyGameLib.NetCode.State
{
    [UpdateInWorld(TargetWorld.ClientAndServer)]
    public class NetStateSystem : ComponentSystem
    {
        
        
        protected override void OnUpdate()
        {
        }
    }
}