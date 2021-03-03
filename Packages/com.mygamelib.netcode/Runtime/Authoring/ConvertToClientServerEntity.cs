using Unity.Entities;
using UnityEngine;

namespace MyGameLib.NetCode
{
    /// <summary>
    /// 
    /// </summary>
    public class ConvertToClientServerEntity : ConvertToEntity
    {
        [SerializeField] public TargetWorld ConversionTarget = TargetWorld.Default;

        void Awake()
        {
            var system = World.DefaultGameObjectInjectionWorld.GetExistingSystem<ConvertToEntitySystem>();

            if (ConversionTarget.HasFlag(TargetWorld.Default))
            {
                system.AddToBeConverted(World.DefaultGameObjectInjectionWorld, this);
            }

            foreach (World world in World.All)
            {
                bool convertToClient = world.GetExistingSystem<ClientSimulationSystemGroup>() != null;
                bool convertToServer = world.GetExistingSystem<ServerSimulationSystemGroup>() != null;

                convertToClient &= ConversionTarget.HasFlag(TargetWorld.Client);
                convertToServer &= ConversionTarget.HasFlag(TargetWorld.Server);

                if (convertToClient || convertToServer)
                {
                    system.AddToBeConverted(world, this);
                }
            }
        }
    }
}