using System;
using Unity.Entities;

namespace MyGameLib.NetCode
{
    [Flags]
    public enum TargetWorld
    {
        Default = 1 << 1,
        Client = 1 << 2,
        Server = 1 << 3,
        ClientAndServer = Client | Server
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class UpdateInWorldAttribute : Attribute
    {
        public TargetWorld World { get; }

        public UpdateInWorldAttribute(TargetWorld w)
        {
            World = w;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DefaultWorldAttribute : UpdateInWorldAttribute
    {
        public DefaultWorldAttribute() : base(TargetWorld.Default)
        {
        }
    }
    
    [AttributeUsage(AttributeTargets.Class)]
    public class ClientWorldAttribute : UpdateInWorldAttribute
    {
        public ClientWorldAttribute() : base(TargetWorld.Client)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ServerWorldAttribute : UpdateInWorldAttribute
    {
        public ServerWorldAttribute() : base(TargetWorld.Server)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ClientServerWorldAttribute : UpdateInWorldAttribute
    {
        public ClientServerWorldAttribute() : base(TargetWorld.ClientAndServer)
        {
        }
    }

    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    public class ClientAndServerInitializationSystemGroup : ComponentSystemGroup
    {
    }
}