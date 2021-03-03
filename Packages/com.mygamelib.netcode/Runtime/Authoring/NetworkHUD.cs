using UnityEngine;

namespace MyGameLib.NetCode
{
    public class NetworkHUD : MonoBehaviour
    {
        private string ip;

        private void OnGUI()
        {
            ip = GUILayout.TextField(ip, GUILayout.Width(200));

            if (GUILayout.Button("Start Client", GUILayout.Width(200)))
            {
            }
        }
    }
}