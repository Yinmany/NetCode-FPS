using MyGameLib.NetCode;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace MyGameLib.NetCode
{
    /// <summary>
    /// 物理世界碰撞体的历史记录
    /// 用于延迟补偿
    /// </summary>
    [DisableAutoCreation]
    [UpdateInWorld(TargetWorld.Server)]
    [UpdateInGroup(typeof(FixedSimulationSystemGroup))]
    [UpdateAfter(typeof(EndFramePhysicsSystem))]
    public class PhysicsWorldHistory : ComponentSystem
    {
        private GhostPredictionSystemGroup _ghostPredictionSystemGroup;

        private BuildPhysicsWorld _buildPhysicsWorld;

        private const int Capacity = 16;
        public CollisionWorld[] _history = new CollisionWorld[Capacity];

        private bool _initialized = false;

        protected override void OnCreate()
        {
            _ghostPredictionSystemGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();
        }

        public bool CastRay(uint renderTick, in RaycastInput input)
        {
            int idx = (int) renderTick % Capacity;
            bool isHit = _history[idx].CastRay(input);
            return isHit;
        }
        
        public bool CastRay(uint renderTick, in RaycastInput input, out RaycastHit closestHit)
        {
            int idx = (int) renderTick % Capacity;
            bool isHit = _history[idx].CastRay(input, out closestHit);
            return isHit;
        }

        public CollisionWorld GetCollisionWorld(uint renderTick)
        {
            int idx = (int) renderTick % Capacity;
            return _history[idx];
        }
        
        protected override void OnUpdate()
        {
            if (_buildPhysicsWorld == null)
            {
                _buildPhysicsWorld = World.GetExistingSystem<BuildPhysicsWorld>();
                return;
            }
            
            _buildPhysicsWorld.GetOutputDependency().Complete();

            if (!_initialized)
            {
                for (int i = 0; i < Capacity; i++)
                {
                    _history[i] = _buildPhysicsWorld.PhysicsWorld.CollisionWorld.Clone();
                }

                _initialized = true;
            }
            else
            {
                int idx = (int) _ghostPredictionSystemGroup.PredictingTick % Capacity;
                _history[idx].Dispose();
                _history[idx] = _buildPhysicsWorld.PhysicsWorld.CollisionWorld.Clone();
            }

            // for (int i = 0; i < Capacity; i++)
            // {
            //     for (int j = 0; j < _history[i].StaticBodies.Length; j++)
            //     {
            //         PhysicsWorldHistoryDebug.list.Add(_history[i].StaticBodies[j].WorldFromBody.pos);
            //     }
            // }

            // int index = (int) _ghostPredictionSystemGroup.PredictingTick % Capacity;
            // if (_history[index].StaticBodies.Length > 0)
            // {
            //     PhysicsWorldHistoryDebug.lastPos = _history[index].StaticBodies[0].WorldFromBody.pos;
            // }
        }

        protected override void OnDestroy()
        {
            for (int i = 0; i < _history.Length; i++)
            {
                _history[i].Dispose();
            }

            _history = null;
        }
    }
}