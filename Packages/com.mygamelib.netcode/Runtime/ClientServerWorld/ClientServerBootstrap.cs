using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace MyGameLib.NetCode
{
    public class ClientServerBootstrap : ICustomBootstrap
    {
        public static BootstrapConfig Config { get; private set; } = ScriptableObject.CreateInstance<BootstrapConfig>();

        public static List<Type> DefaultWorldSystems => SystemStates.DefaultWorldSystems;
        public static List<Type> ExplicitDefaultWorldSystems => SystemStates.ExplicitDefaultWorldSystems;

        internal struct State
        {
            public List<Type> DefaultWorldSystems;
            public List<Type> ExplicitDefaultWorldSystems;
            public List<Type> ClientSystems;
            public List<Type> ServerSystems;
        }

        internal static State SystemStates;

        /// <summary>
        /// 重载指定World
        /// </summary>
        public static Dictionary<Type, UpdateInWorldAttribute> OverridesUpdateInWorld =
            new Dictionary<Type, UpdateInWorldAttribute>
            {
                {typeof(InitializationSystemGroup), new DefaultWorldAttribute()},
                {typeof(SimulationSystemGroup), new DefaultWorldAttribute()},
                {typeof(PresentationSystemGroup), new DefaultWorldAttribute()},
                {typeof(ConvertToEntitySystem), new DefaultWorldAttribute()}
            };

        public virtual bool Initialize(string defaultWorldName)
        {
            World defaultWorld = new World(defaultWorldName);
            World.DefaultGameObjectInjectionWorld = defaultWorld;

            var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);

            GenerateSystemList(systems);

            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(defaultWorld,
                SystemStates.ExplicitDefaultWorldSystems);
            ScriptBehaviourUpdateOrder.AddWorldToCurrentPlayerLoop(defaultWorld);

            Config = GetBootstrapConfig();

#if !UNITY_SERVER || UNITY_EDITOR
            if (Config.StartupWorld.HasFlag(TargetWorld.Client))
            {
                for (int i = 0; i < Config.ClientNum; i++)
                {
                    CreateClientWorld(defaultWorld, "ClientWorld" + i);
                }
            }
#endif


#if UNITY_SERVER || UNITY_EDITOR
            if (Config.StartupWorld.HasFlag(TargetWorld.Server))
            {
                CreateServerWorld(defaultWorld, "ServerWorld");
            }
#endif
            return true;
        }

        protected virtual BootstrapConfig GetBootstrapConfig()
        {
            BootstrapConfig config = Resources.Load<BootstrapConfig>(nameof(BootstrapConfig));
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BootstrapConfig>();
            }

            return config;
        }

        protected virtual World CreateDefaultWorld(string defaultWorldName)
        {
            World world = new World(defaultWorldName);
            World.DefaultGameObjectInjectionWorld = world;

            var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);

            GenerateSystemList(systems);

            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world,
                SystemStates.ExplicitDefaultWorldSystems.Concat(SystemStates.DefaultWorldSystems));

            ScriptBehaviourUpdateOrder.AddWorldToCurrentPlayerLoop(world);

            return world;
        }

#if !UNITY_SERVER || UNITY_EDITOR
        public static World CreateClientWorld(World defaultWorld, string worldName)
        {
            World world = new World(worldName);
            var simulationTickSystem = defaultWorld.GetOrCreateSystem<ChainClientSimulationSystem>();
            var clientInitializationSystemGroup = world.CreateSystem<ClientInitializationSystemGroup>();
            var clientSimulationSystemGroup = world.CreateSystem<ClientSimulationSystemGroup>();
            var clientPresentationSystemGroup = world.CreateSystem<ClientPresentationSystemGroup>();

            var systems = SystemStates.ClientSystems.Concat(DefaultWorldSystems);
            foreach (Type type in systems)
            {
                if (type == typeof(InitializationSystemGroup) ||
                    type == typeof(SimulationSystemGroup) ||
                    type == typeof(PresentationSystemGroup) ||
                    type == typeof(ChainClientSimulationSystem) ||
                    type == typeof(ClientInitializationSystemGroup) ||
                    type == typeof(ClientSimulationSystemGroup) ||
                    type == typeof(ClientPresentationSystemGroup))
                {
                    continue;
                }

                var system = world.GetOrCreateSystem(type);
                UpdateInGroupAttribute attr = GetSystemAttribute<UpdateInGroupAttribute>(type);
                if (attr == null)
                {
                    clientSimulationSystemGroup.AddSystemToUpdateList(system);
                }
                else
                {
                    if (attr.GroupType == typeof(InitializationSystemGroup))
                    {
                        clientInitializationSystemGroup.AddSystemToUpdateList(system);
                    }
                    else if (attr.GroupType == typeof(PresentationSystemGroup))
                    {
                        clientPresentationSystemGroup.AddSystemToUpdateList(system);
                    }
                    else if (attr.GroupType == typeof(SimulationSystemGroup))
                    {
                        clientSimulationSystemGroup.AddSystemToUpdateList(system);
                    }
                    else if (attr.GroupType == typeof(ClientAndServerInitializationSystemGroup))
                    {
                        clientInitializationSystemGroup.AddSystemToUpdateList(system);
                    }
                    else
                    {
                        var group = (ComponentSystemGroup) world.GetOrCreateSystem(attr.GroupType);
                        group.AddSystemToUpdateList(system);
                    }
                }
            }

            clientInitializationSystemGroup.SortSystems();
            clientSimulationSystemGroup.SortSystems();
            clientPresentationSystemGroup.SortSystems();

            defaultWorld.GetExistingSystem<InitializationSystemGroup>()
                .AddSystemToUpdateList(clientInitializationSystemGroup);

            defaultWorld.GetExistingSystem<PresentationSystemGroup>()
                .AddSystemToUpdateList(clientPresentationSystemGroup);

            clientSimulationSystemGroup.ParentChainSystem = simulationTickSystem;
            simulationTickSystem.AddSystemToUpdateList(clientSimulationSystemGroup);
            return world;
        }
#endif

#if UNITY_SERVER || UNITY_EDITOR
        public static World CreateServerWorld(World defaultWorld, string worldName)
        {
            World world = new World(worldName);
            var serverInitializationSystemGroup = world.CreateSystem<ServerInitializationSystemGroup>();
            var serverSimulationSystemGroup = world.CreateSystem<ServerSimulationSystemGroup>();
            var simulationTickSystem = defaultWorld.GetOrCreateSystem<ChainServerSimulationSystem>();

            var systems = SystemStates.ServerSystems.Concat(DefaultWorldSystems);
            foreach (Type type in systems)
            {
                if (type == typeof(InitializationSystemGroup) ||
                    type == typeof(SimulationSystemGroup) ||
                    type == typeof(PresentationSystemGroup) ||
                    type == typeof(ServerInitializationSystemGroup) ||
                    type == typeof(ServerSimulationSystemGroup) ||
                    type == typeof(ChainServerSimulationSystem))
                {
                    continue;
                }

                var system = world.GetOrCreateSystem(type);
                UpdateInGroupAttribute attr = GetSystemAttribute<UpdateInGroupAttribute>(type);
                if (attr == null)
                {
                    serverSimulationSystemGroup.AddSystemToUpdateList(system);
                }
                else
                {
                    if (attr.GroupType == typeof(InitializationSystemGroup))
                    {
                        serverInitializationSystemGroup.AddSystemToUpdateList(system);
                    }
                    else if (attr.GroupType == typeof(PresentationSystemGroup))
                    {
                    }
                    else if (attr.GroupType == typeof(SimulationSystemGroup))
                    {
                        serverSimulationSystemGroup.AddSystemToUpdateList(system);
                    }
                    else if (attr.GroupType == typeof(ClientAndServerInitializationSystemGroup))
                    {
                        serverInitializationSystemGroup.AddSystemToUpdateList(system);
                    }
                    else
                    {
                        var group = (ComponentSystemGroup) world.GetOrCreateSystem(attr.GroupType);
                        group.AddSystemToUpdateList(system);
                    }
                }
            }

            serverInitializationSystemGroup.SortSystems();
            serverSimulationSystemGroup.SortSystems();
            simulationTickSystem.SortSystems();

            defaultWorld.GetExistingSystem<InitializationSystemGroup>()
                .AddSystemToUpdateList(serverInitializationSystemGroup);
            serverSimulationSystemGroup.ParentChainSystem = simulationTickSystem;
            simulationTickSystem.AddSystemToUpdateList(serverSimulationSystemGroup);

            return world;
        }
#endif

        /// <summary>
        /// 生成系统列表
        /// </summary>
        /// <param name="systems"></param>
        protected internal static void GenerateSystemList(IReadOnlyList<Type> systems)
        {
            SystemStates.ExplicitDefaultWorldSystems = new List<Type>();
            SystemStates.DefaultWorldSystems = new List<Type>();
            SystemStates.ClientSystems = new List<Type>();
            SystemStates.ServerSystems = new List<Type>();

            foreach (Type type in systems)
            {
                if (OverridesUpdateInWorld.TryGetValue(type, out var updateInWorldAttr))
                {
                    AddToSystems(type, updateInWorldAttr.World);
                    continue;
                }

                var targetWorld = FindTargetWorld(type, out var explicitWorld);
                AddToSystems(type, targetWorld, explicitWorld);
            }
        }

        /// <summary>
        /// 分类系统
        /// </summary>
        /// <param name="type"></param>
        /// <param name="target"></param>
        /// <param name="explicitWorld"></param>
        protected internal static void AddToSystems(Type type, TargetWorld target, bool explicitWorld = true)
        {
            if (target.HasFlag(TargetWorld.Default))
            {
                if (explicitWorld)
                    SystemStates.ExplicitDefaultWorldSystems.Add(type);
                else
                    SystemStates.DefaultWorldSystems.Add(type);
            }

            if (target.HasFlag(TargetWorld.Client))
            {
                SystemStates.ClientSystems.Add(type);
            }

            if (target.HasFlag(TargetWorld.Server))
            {
                SystemStates.ServerSystems.Add(type);
            }
        }

        /// <summary>
        /// 递归找出当前System属于哪个World
        /// </summary>
        /// <param name="type">System类型</param>
        /// <param name="explicitWorld">是否显示指定UpdateInWorldAttribute</param>
        /// <returns></returns>
        internal static TargetWorld FindTargetWorld(Type type, out bool explicitWorld)
        {
            while (true)
            {
                explicitWorld = true;
                var updateInWorldAttr = GetSystemAttribute<UpdateInWorldAttribute>(type);
                if (updateInWorldAttr != null)
                {
                    return updateInWorldAttr.World;
                }

                var groupAttr = GetSystemAttribute<UpdateInGroupAttribute>(type);
                if (groupAttr != null)
                {
                    if (groupAttr.GroupType == typeof(ClientAndServerInitializationSystemGroup))
                    {
                        return TargetWorld.ClientAndServer;
                    }

                    type = groupAttr.GroupType;
                    continue;
                }

                explicitWorld = false;
                return TargetWorld.Default;
            }
        }

        static T GetSystemAttribute<T>(Type systemType)
            where T : Attribute
        {
            var attribs = TypeManager.GetSystemAttributes(systemType, typeof(T));
            if (attribs.Length != 1)
                return null;
            return attribs[0] as T;
        }
    }
}