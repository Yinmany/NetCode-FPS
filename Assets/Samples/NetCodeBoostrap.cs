using System.IO;
using MyGameLib.NetCode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetCodeBoostrap : ClientServerBootstrap
{
    public class AppConfig
    {
        public int UseServer;
        public string[] ServerList;
    }

    public override bool Initialize(string defaultWorldName)
    {
        if (SceneManager.GetActiveScene().name != "NetCodeExampleScene" &&
            SceneManager.GetActiveScene().name != "NetCube" &&
            SceneManager.GetActiveScene().name != "NetFPS")
        {
            base.CreateDefaultWorld(defaultWorldName);
            return true;
        }

        return base.Initialize(defaultWorldName);
    }

    protected override BootstrapConfig GetBootstrapConfig()
    {
        BootstrapConfig config = base.GetBootstrapConfig();

        string path = Path.Combine(Application.streamingAssetsPath, "App.json");
        if (File.Exists(path))
        {
            string cfgStr = File.ReadAllText(path);
            AppConfig appConfig = JsonUtility.FromJson<AppConfig>(cfgStr);
            string useServer = appConfig.ServerList[appConfig.UseServer];
            Debug.Log($"UseServer:{useServer}");
            string[] ips = useServer.Split(':');
            config.IP = ips[0];
            config.Port = ushort.Parse(ips[1]);
        }

        return config;
    }
}