using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using NetCode_Example_Scenes.Scripts.Player;
using Samples.MyGameLib.NetCode;
using Unity.Entities;
using UnityEngine;

namespace Samples.NetFPS
{
    [ClientWorld]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public class CameraSmoothMotionSystem : ComponentSystem
    {
        private CameraInterpolatedSystem _interpolatedSystem;
        private TickSimulationSystemGroup _tickSimulationSystemGroup;
        private GameObjectManager _storeSystem;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<EnableNetFPS>();
            this._interpolatedSystem = World.GetOrCreateSystem<CameraInterpolatedSystem>();
            _tickSimulationSystemGroup = World.GetOrCreateSystem<TickSimulationSystemGroup>();

            _storeSystem = World.GetOrCreateSystem<GameObjectManager>();
        }

        protected override void OnUpdate()
        {
            Entities.WithAny<LocalPlayerComponent>().ForEach((Entity ent) =>
            {
                if (!_storeSystem.TryGetValue(ent, out var trans))
                {
                    return;
                }

                Quaternion pre = Quaternion.Euler(_interpolatedSystem.previous.Pitch,
                    _interpolatedSystem.previous.Yaw, 0);
                Quaternion cur = Quaternion.Euler(_interpolatedSystem.current.Pitch,
                    _interpolatedSystem.current.Yaw, 0);

                var playerView = trans.GetComponent<GameObjectLinked>().Target.GetComponent<LinkedPlayerView>();
                playerView.ActiveLocalPlayerCamera();
                Quaternion aimRotation = Quaternion.Lerp(pre, cur, _tickSimulationSystemGroup.Interpolate);
                playerView.Camera.transform.rotation = aimRotation;
                playerView.Camera.transform.position = playerView.transform.position +
                                                       aimRotation * playerView.targetCamOffset;
            });
        }
    }

    [ClientWorld]
    [UpdateInGroup(typeof(EndTickSystemGroup))]
    public class CameraInterpolatedSystem : ComponentSystem
    {
        public InputCommand previous;
        public InputCommand current;

        private GhostPredictionSystemGroup _ghostPredictionSystemGroup;

        protected override void OnCreate()
        {
            _ghostPredictionSystemGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();
        }

        protected override void OnUpdate()
        {
            Entity localPlayer = GetSingleton<CommandTargetComponent>().Target;
            if (localPlayer == Entity.Null) return;

            previous = current;

            var buf = EntityManager.GetBuffer<InputCommand>(localPlayer);
            buf.GetDataAtTick(_ghostPredictionSystemGroup.PredictingTick, out current);
        }
    }
}