using UnityEngine;
using UnityEngine.UI;

public class FeedbackFlashHUD : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Image component of the flash")]
    public Image flashImage;
    [Tooltip("CanvasGroup to fade the damage flash, used when recieving damage end healing")]
    public CanvasGroup flashCanvasGroup;
    [Tooltip("CanvasGroup to fade the critical health vignette")]
    public CanvasGroup vignetteCanvasGroup;

    [Header("Damage")]
    [Tooltip("Color of the damage flash")]
    public Color damageFlashColor;
    [Tooltip("Duration of the damage flash")]
    public float damageFlashDuration;
    [Tooltip("Max alpha of the damage flash")]
    public float damageFlashMaxAlpha = 1f;

    [Header("Critical health")]
    [Tooltip("Max alpha of the critical vignette")]
    public float criticaHealthVignetteMaxAlpha = .8f;
    [Tooltip("Frequency at which the vignette will pulse when at critical health")]
    public float pulsatingVignetteFrequency = 4f;

    [Header("Heal")]
    [Tooltip("Color of the heal flash")]
    public Color healFlashColor;
    [Tooltip("Duration of the heal flash")]
    public float healFlashDuration;
    [Tooltip("Max alpha of the heal flash")]
    public float healFlashMaxAlpha = 1f;

    bool m_FlashActive;
    float m_LastTimeFlashStarted = Mathf.NegativeInfinity;
    Health m_PlayerHealth;
    GameFlowManager m_GameFlowManager;

    void Start()
    {
        // Subscribe to player damage events
        PlayerCharacterController playerCharacterController = FindObjectOfType<PlayerCharacterController>();
        DebugUtility.HandleErrorIfNullFindObject<PlayerCharacterController, FeedbackFlashHUD>(playerCharacterController, this);

        m_PlayerHealth = playerCharacterController.GetComponent<Health>();
        DebugUtility.HandleErrorIfNullGetComponent<Health, FeedbackFlashHUD>(m_PlayerHealth, this, playerCharacterController.gameObject);

        m_GameFlowManager = FindObjectOfType<GameFlowManager>();
        DebugUtility.HandleErrorIfNullFindObject<GameFlowManager, FeedbackFlashHUD>(m_GameFlowManager, this);

        m_PlayerHealth.onDamaged += OnTakeDamage;
        m_PlayerHealth.onHealed += OnHealed;
    }

    private void Update()
    {
        if (m_PlayerHealth.isCritical())
        {
            vignetteCanvasGroup.gameObject.SetActive(true);
            float vignetteAlpha = (1 - (m_PlayerHealth.currentHealth / m_PlayerHealth.maxHealth / m_PlayerHealth.criticalHealthRatio)) * criticaHealthVignetteMaxAlpha;

            if (m_GameFlowManager.gameIsEnding)
                vignetteCanvasGroup.alpha = vignetteAlpha;
            else
                vignetteCanvasGroup.alpha = ((Mathf.Sin(Time.time * pulsatingVignetteFrequency) / 2) + 0.5f) * vignetteAlpha;
        }
        else
        {
            vignetteCanvasGroup.gameObject.SetActive(false);
        }


        if (m_FlashActive)
        {
            float normalizedTimeSinceDamage = (Time.time - m_LastTimeFlashStarted) / damageFlashDuration;

            if (normalizedTimeSinceDamage < 1f)
            {
                float flashAmount = damageFlashMaxAlpha * (1f - normalizedTimeSinceDamage);
                flashCanvasGroup.alpha = flashAmount;
            }
            else
            {
                flashCanvasGroup.gameObject.SetActive(false);
                m_FlashActive = false;
            }
        }
    }

    void ResetFlash()
    {
        m_LastTimeFlashStarted = Time.time;
        m_FlashActive = true;
        flashCanvasGroup.alpha = 0f;
        flashCanvasGroup.gameObject.SetActive(true);
    }

    void OnTakeDamage(float dmg, GameObject damageSource)
    {
        ResetFlash();
        flashImage.color = damageFlashColor;
    }

    void OnHealed(float amount)
    {
        ResetFlash();
        flashImage.color = healFlashColor;
    }
}
