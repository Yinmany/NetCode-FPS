using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace MyGameLib.NetCode.Hybrid
{
    [DisallowMultipleComponent]
    public abstract class NetworkBehaviour : MonoBehaviour, INetworkBehaviour
    {
        public World World { get; set; }

        public Entity SelfEntity { get; set; }

        public bool IsServer { get; set; }

        protected bool IsClient => !IsServer;

        public bool IsOwner { get; set; }

        public void Simulate(float dt)
        {
            OnSimulate(dt);
        }

        public void NetworkAwake()
        {
            OnNetworkAwake();
        }

        protected virtual void OnNetworkAwake()
        {
        }

        protected abstract void OnSimulate(float dt);
    }

    public interface INetworkBehaviour
    {
        void NetworkAwake();

        void Simulate(float dt);
    }
}