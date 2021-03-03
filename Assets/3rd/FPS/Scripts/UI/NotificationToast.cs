using UnityEngine;

public class NotificationToast : MonoBehaviour
{
    [Tooltip("Text content that will display the notification text")]
    public TMPro.TextMeshProUGUI textContent;
    [Tooltip("Canvas used to fade in and out the content")]
    public CanvasGroup canvasGroup;
    [Tooltip("How long it will stay visible")]
    public float visibleDuration = 3f;
    [Tooltip("Duration of the fade in")]
    public float fadeInDuration = 0.5f;
    [Tooltip("Duration of the fade out")]
    public float fadeOutDuration = 2f;

    float m_InitTime;
    bool m_WasInit;


    public void Initialize(string text)
    {
        textContent.text = text;

        m_InitTime = Time.time;
        // start the fade out
        m_WasInit = true;
    }

    void Update()
    {
        if (m_WasInit)
        {
            float timeSinceInit = Time.time - m_InitTime;
            if (timeSinceInit < fadeInDuration)
            {
                // fade in
                canvasGroup.alpha = timeSinceInit / fadeInDuration;
            }
            else if (timeSinceInit < fadeInDuration + visibleDuration)
            {
                // stay visible
                canvasGroup.alpha = 1f;
            }
            else if (timeSinceInit < fadeInDuration + visibleDuration + fadeOutDuration)
            {
                // fade out
                canvasGroup.alpha = 1 - (timeSinceInit - fadeInDuration - visibleDuration) / fadeOutDuration;
            }
            else
            {
                canvasGroup.alpha = 0f;

                // fade out over, destroy the object
                m_WasInit = false;
                Destroy(gameObject);
            }
        }
    }
}
