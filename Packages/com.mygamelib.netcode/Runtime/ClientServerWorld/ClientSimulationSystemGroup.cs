using Unity.Entities;

namespace MyGameLib.NetCode
{
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    [ClientWorld]
    public class ClientInitializationSystemGroup : InitializationSystemGroup
    {
        
    }
    
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    [ClientWorld]
    public class ClientPresentationSystemGroup : PresentationSystemGroup
    {
        
    }
    
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    [ClientWorld]
    public class ClientSimulationSystemGroup : SimulationSystemGroup
    {
#if !UNITY_SERVER || UNITY_EDITOR
        internal ChainClientSimulationSystem ParentChainSystem;
        protected override void OnDestroy()
        {
            if (ParentChainSystem != null)
            {
                ParentChainSystem.RemoveSystemFromUpdateList(this);
            }
        }
#endif
    }

#if !UNITY_SERVER || UNITY_EDITOR
#if !UNITY_CLIENT || UNITY_SERVER || UNITY_EDITOR
    [UpdateAfter(typeof(ChainServerSimulationSystem))]
#endif
    [AlwaysUpdateSystem]
    [DefaultWorld]
    public class ChainClientSimulationSystem : ComponentSystemGroup
    {
        protected override void OnDestroy()
        {
            foreach (var sys in Systems)
            {
                var grp = sys as ClientSimulationSystemGroup;
                if (grp != null)
                    grp.ParentChainSystem = null;
            }
        }
    }

#endif
}