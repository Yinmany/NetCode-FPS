using MyGameLib.NetCode;
using Unity.Entities;
using UnityEngine;

namespace Samples.Common.Smooth
{
    public class HistoryStateDataAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            TargetWorld targetWorld = GhostAuthoringConversion.GetConversionTarget(dstManager.World);

            if (targetWorld != TargetWorld.Client) return;

            if (GetComponent<GhostAuthoringComponent>().Type !=
                GhostAuthoringComponent.ClientInstanceType.Interpolated)
            {
                dstManager.AddBuffer<HistoryStateData>(entity);
            }
        }
    }
}