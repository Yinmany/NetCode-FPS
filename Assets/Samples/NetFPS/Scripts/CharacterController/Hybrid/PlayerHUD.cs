using UnityEngine;
using UnityEngine.UI;

namespace NetCodeExample.Scripts.Player
{
    public class PlayerHUD : MonoBehaviour
    {
        public Canvas Canvas;
        public Image Image;

        public bool Detach;

        private Vector3 m_Offset;

        private void Awake()
        {
            if (Detach)
            {
                m_Offset = Canvas.transform.position - transform.position;

                Canvas.transform.SetParent(null);
            }
        }

        public void SetValue(float f)
        {
            Image.fillAmount = f;
        }

        private void LateUpdate()
        {
            if (Detach)
            {
                Canvas.transform.position = transform.position + m_Offset;
            }
        }
    }
}