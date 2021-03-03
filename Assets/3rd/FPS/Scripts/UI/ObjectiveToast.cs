using UnityEngine;
using UnityEngine.UI;

public class ObjectiveToast : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Text content that will display the title")]
    public TMPro.TextMeshProUGUI titleTextContent;
    [Tooltip("Text content that will display the description")]
    public TMPro.TextMeshProUGUI descriptionTextContent;
    [Tooltip("Text content that will display the counter")]
    public TMPro.TextMeshProUGUI counterTextContent;

    [Tooltip("Rect that will display the description")]
    public RectTransform subTitleRect;
    [Tooltip("Canvas used to fade in and out the content")]
    public CanvasGroup canvasGroup;

    [Tooltip("Layout group containing the objective")]
    public HorizontalOrVerticalLayoutGroup layoutGroup;

    [Header("Transitions")]
    [Tooltip("Delay before moving complete")]
    public float completionDelay;
    [Tooltip("Duration of the fade in")]
    public float fadeInDuration = 0.5f;
    [Tooltip("Duration of the fade out")]
    public float fadeOutDuration = 2f;

    [Header("Sound")]
    [Tooltip("Sound that will be player on initialization")]
    public AudioClip initSound;
    [Tooltip("Sound that will be player on completion")]
    public AudioClip completedSound;

    [Header("Movement")]
    [Tooltip("Time it takes to move in the screen")]
    public float moveInDuration = 0.5f;
    [Tooltip("Animation curve for move in, position in x over time")]
    public AnimationCurve moveInCurve;

    [Tooltip("Time it takes to move out of the screen")]
    public float moveOutDuration = 2f;
    [Tooltip("Animation curve for move out, position in x over time")]
    public AnimationCurve moveOutCurve;

    float m_StartFadeTime;
    bool m_IsFadingIn;
    bool m_IsFadingOut;
    bool m_IsMovingIn;
    bool m_IsMovingOut;
    AudioSource m_AudioSource;
    RectTransform m_RectTransform;

    public void Initialize(string titleText, string descText, string counterText, bool isOptionnal, float delay)
    {
        // set the description for the objective, and forces the content size fitter to be recalculated
        Canvas.ForceUpdateCanvases();

        titleTextContent.text = titleText;
        descriptionTextContent.text = descText;
        counterTextContent.text = counterText;

        if (GetComponent<RectTransform>())
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }

        m_StartFadeTime = Time.time + delay;
        // start the fade in
        m_IsFadingIn = true;
        m_IsMovingIn = true;
    }

    public void Complete()
    {
        m_StartFadeTime = Time.time + completionDelay;
        m_IsFadingIn = false;
        m_IsMovingIn = false;

        // if a sound was set, play it
        PlaySound(completedSound);

        // start the fade out
        m_IsFadingOut = true;
        m_IsMovingOut = true;
    }

    void Update()
    {
        float timeSinceFadeStarted = Time.time - m_StartFadeTime;

        subTitleRect.gameObject.SetActive(!string.IsNullOrEmpty(descriptionTextContent.text));

        if (m_IsFadingIn && !m_IsFadingOut)
        {
            // fade in
            if (timeSinceFadeStarted < fadeInDuration)
            {
                // calculate alpha ratio
                canvasGroup.alpha = timeSinceFadeStarted / fadeInDuration;
            }
            else
            {
                canvasGroup.alpha = 1f;
                // end the fade in
                m_IsFadingIn = false;

                PlaySound(initSound);
            }
        }

        if (m_IsMovingIn && !m_IsMovingOut)
        { 
            // move in
            if (timeSinceFadeStarted < moveInDuration)
            {
                layoutGroup.padding.left = (int)moveInCurve.Evaluate(timeSinceFadeStarted / moveInDuration);

                if (GetComponent<RectTransform>())
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
                }
            }
            else
            {
                // making sure the position is exact
                layoutGroup.padding.left = 0;

                if (GetComponent<RectTransform>())
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
                }

                m_IsMovingIn = false;
            }

        }

        if (m_IsFadingOut)
        {
            // fade out
            if (timeSinceFadeStarted < fadeOutDuration)
            {
                // calculate alpha ratio
                canvasGroup.alpha = 1 - (timeSinceFadeStarted) / fadeOutDuration;
            }
            else
            {
                canvasGroup.alpha = 0f;

                // end the fade out, then destroy the object
                m_IsFadingOut = false;
                Destroy(gameObject);
            }
        }

        if (m_IsMovingOut)
        { 
            // move out
            if (timeSinceFadeStarted < moveOutDuration)
            {
                layoutGroup.padding.left = (int)moveOutCurve.Evaluate(timeSinceFadeStarted / moveOutDuration);

                if (GetComponent<RectTransform>())
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
                }
            }
            else
            {
                m_IsMovingOut = false;
            }
        }
    }

    void PlaySound(AudioClip sound)
    {
        if (!sound)
            return;

        if (!m_AudioSource)
        {
            m_AudioSource = gameObject.AddComponent<AudioSource>();
            m_AudioSource.outputAudioMixerGroup = AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.HUDObjective);
        }

        m_AudioSource.PlayOneShot(sound);
    }
}
