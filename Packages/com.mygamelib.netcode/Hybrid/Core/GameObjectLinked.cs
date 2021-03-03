using UnityEngine;

namespace MyGameLib.NetCode.Hybrid
{
    /// <summary>
    /// 关联的GameObject
    /// </summary>
    public class GameObjectLinked : MonoBehaviour
    {
        public GameObject Target;

#if UNITY_EDITOR
        public bool IsServerShow;
#endif

        private void OnDestroy()
        {
            Target = null;
        }
    }
}