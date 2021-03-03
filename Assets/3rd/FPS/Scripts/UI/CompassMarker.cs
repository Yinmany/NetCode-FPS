using UnityEngine;
using UnityEngine.UI;

public class CompassMarker : MonoBehaviour
{
    [Tooltip("Main marker image")]
    public Image mainImage;
    [Tooltip("Canvas group for the marker")]
    public CanvasGroup canvasGroup;

    [Header("Enemy element")]
    [Tooltip("Default color for the marker")]
    public Color defaultColor;
    [Tooltip("Alternative color for the marker")]
    public Color altColor;

    [Header("Direction element")]
    [Tooltip("Use this marker as a magnetic direction")]
    public bool isDirection;
    [Tooltip("Text content for the direction")]
    public TMPro.TextMeshProUGUI textContent;

    EnemyController m_EnemyController;

    public void Initialize(CompassElement compassElement, string textDirection)
    {
        if (isDirection && textContent)
        {
            textContent.text = textDirection;
        }
        else
        {
            m_EnemyController = compassElement.transform.GetComponent<EnemyController>();

            if (m_EnemyController)
            {
                m_EnemyController.onDetectedTarget += DetectTarget;
                m_EnemyController.onLostTarget += LostTarget;

                LostTarget();
            }
        }
    }

    public void DetectTarget()
    {
        mainImage.color = altColor;
    }

    public void LostTarget()
    {
        mainImage.color = defaultColor;
    }
}
