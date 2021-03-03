using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameLib.NetCode.Hybrid
{
    /// <summary>
    /// 转换
    /// </summary>
    [AddComponentMenu("MyGameLib.NetCode.Hybrid/ConvertToClientServer")]
    public class ConvertToClientServer : ConvertToEntity
    {
        [SerializeField] public TargetWorld ConversionTarget = TargetWorld.ClientAndServer;

        void Awake()
        {
            foreach (World world in World.All)
            {
                bool convertToClient = world.GetExistingSystem<ClientSimulationSystemGroup>() != null;
                bool convertToServer = world.GetExistingSystem<ServerSimulationSystemGroup>() != null;

                convertToClient &= ConversionTarget.HasFlag(TargetWorld.Client);
                convertToServer &= ConversionTarget.HasFlag(TargetWorld.Server);

                if (convertToClient || convertToServer)
                {
                    Scene scene = world.GetLinkedScene();
                    foreach (Transform o in transform)
                    {
                        GameObject obj = Instantiate(o.gameObject);
                        SceneManager.MoveGameObjectToScene(obj, scene);
                    }
                }
            }

            Destroy(gameObject);
        }
    }
}