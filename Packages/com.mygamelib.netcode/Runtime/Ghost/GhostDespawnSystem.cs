using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport.Utilities;

namespace MyGameLib.NetCode
{
    [ClientWorld]
    [UpdateInGroup(typeof(GhostUpdateSystemGroup))]
    public class GhostDespawnSystem : ComponentSystem
    {
        private NetworkTimeSystem _networkTimeSystem;
        private GhostReceiveSystem _ghostReceiveSystem;
        private BeginSimulationEntityCommandBufferSystem _barrier;

        private NativeQueue<DelayedDespawnGhost> _interpolatedDespawnQueue;
        private NativeQueue<DelayedDespawnGhost> _predictedDespawnQueue;

        internal struct DelayedDespawnGhost
        {
            public SpawnedGhost Ghost;
            public uint Tick;
        }

        protected override void OnCreate()
        {
            _interpolatedDespawnQueue = new NativeQueue<DelayedDespawnGhost>(Allocator.Persistent);
            _predictedDespawnQueue = new NativeQueue<DelayedDespawnGhost>(Allocator.Persistent);

            _networkTimeSystem = World.GetOrCreateSystem<NetworkTimeSystem>();
            _ghostReceiveSystem = World.GetOrCreateSystem<GhostReceiveSystem>();
            _barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnDestroy()
        {
            _interpolatedDespawnQueue.Dispose();
            _predictedDespawnQueue.Dispose();
        }

        internal void AddToPredicted(DelayedDespawnGhost ghost) => _predictedDespawnQueue.Enqueue(ghost);
        internal void AddToInterpolated(DelayedDespawnGhost ghost) => _interpolatedDespawnQueue.Enqueue(ghost);

        protected override void OnUpdate()
        {
            uint interpolatedTick = _networkTimeSystem.interpolateTargetTick;
            uint predictedTick = _networkTimeSystem.predictTargetTick;
            var commandBuffer = _barrier.CreateCommandBuffer();

            // 插值类型的Ghost销毁
            while (_interpolatedDespawnQueue.Count > 0 &&
                   !SequenceHelpers.IsNewer(_interpolatedDespawnQueue.Peek().Tick, interpolatedTick))
            {
                var desspawnGhost = _interpolatedDespawnQueue.Dequeue();
                if (_ghostReceiveSystem.SpawnedGhostEntityMap.TryGetValue(desspawnGhost.Ghost, out Entity ent))
                {
                    commandBuffer.DestroyEntity(ent);
                    _ghostReceiveSystem.SpawnedGhostEntityMap.Remove(desspawnGhost.Ghost);
                }
            }

            // 预测类型的Ghost销毁
            while (_predictedDespawnQueue.Count > 0 &&
                   !SequenceHelpers.IsNewer(_predictedDespawnQueue.Peek().Tick, predictedTick))
            {
                var desspawnGhost = _predictedDespawnQueue.Dequeue();
                if (_ghostReceiveSystem.SpawnedGhostEntityMap.TryGetValue(desspawnGhost.Ghost, out Entity ent))
                {
                    commandBuffer.DestroyEntity(ent);
                    _ghostReceiveSystem.SpawnedGhostEntityMap.Remove(desspawnGhost.Ghost);
                }
            }
        }
    }
}