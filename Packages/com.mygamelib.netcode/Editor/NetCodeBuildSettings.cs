using System;
using Unity.Build;
using Unity.Build.Classic;
using Unity.Properties.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyGameLib.NetCode.Editor
{
    public class NetCodeBuildSettings : IBuildComponent
    {
        public string IP = "127.0.0.1";
        public ushort Port = 9000;
        public bool Client;
        public bool Server;

        public BootstrapConfig config;
    }

    public class NetCodeBuildSettingsUI : Inspector<NetCodeBuildSettings>
    {
        public override VisualElement Build()
        {
            var root = new VisualElement();
            DoDefaultGui(root, nameof(NetCodeBuildSettings.config));

            if (Target.config != null)
            {
                var prop = new TextField {value = $"{Target.config.IP} [{Target.config.StartupWorld}]"};
                prop.SetEnabled(false);
                root.Add(prop);
            }

            DoDefaultGui(root, nameof(NetCodeBuildSettings.Client));
            DoDefaultGui(root, nameof(NetCodeBuildSettings.Server));
            if(!Target.Server)
                DoDefaultGui(root, nameof(NetCodeBuildSettings.IP));
            DoDefaultGui(root, nameof(NetCodeBuildSettings.Port));

            return root;
        }
    }

    class NetCodeBuildSettingsCustomizer : ClassicBuildPipelineCustomizer
    {
        public override Type[] UsedComponents { get; } =
        {
            typeof(NetCodeBuildSettings)
        };

        private string oldIP;
        private ushort oldPort;
        public TargetWorld oldTargetWorld;

        public override BuildOptions ProvideBuildOptions()
        {
            return base.ProvideBuildOptions();
        }

        public override void OnBeforeBuild()
        {
            if (Context.HasComponent<NetCodeBuildSettings>())
            {
                NetCodeBuildSettings netCodeBuildSettings = Context.GetComponentOrDefault<NetCodeBuildSettings>();
                oldIP = netCodeBuildSettings.config.IP;
                oldPort = netCodeBuildSettings.config.Port;
                oldTargetWorld = netCodeBuildSettings.config.StartupWorld;

                netCodeBuildSettings.config.IP = netCodeBuildSettings.IP;
                netCodeBuildSettings.config.Port = netCodeBuildSettings.Port;

                if (netCodeBuildSettings.Client && netCodeBuildSettings.Server)
                {
                    netCodeBuildSettings.config.StartupWorld = TargetWorld.ClientAndServer;
                }
                else if (netCodeBuildSettings.Client)
                {
                    netCodeBuildSettings.config.StartupWorld = TargetWorld.Client;
                }
                else if (netCodeBuildSettings.Server)
                {
                    netCodeBuildSettings.config.StartupWorld = TargetWorld.Server;
                }
            }
        }

        public override string[] ProvidePlayerScriptingDefines()
        {
            return base.ProvidePlayerScriptingDefines();
        }

        public override void RegisterAdditionalFilesToDeploy(Action<string, string> registerAdditionalFileToDeploy)
        {
            if (Context.HasComponent<NetCodeBuildSettings>())
            {
                NetCodeBuildSettings netCodeBuildSettings = Context.GetComponentOrDefault<NetCodeBuildSettings>();
                netCodeBuildSettings.config.Port = oldPort;
                netCodeBuildSettings.config.IP = oldIP;
                netCodeBuildSettings.config.StartupWorld = oldTargetWorld;

                Debug.Log("恢复 BootstrapConfig");
            }
        }
    }
}