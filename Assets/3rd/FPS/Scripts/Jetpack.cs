using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class Jetpack : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Audio source for jetpack sfx")]
    public AudioSource audioSource;
    [Tooltip("Particles for jetpack vfx")]
    public ParticleSystem[] jetpackVfx;

    [Header("Parameters")]
    [Tooltip("Whether the jetpack is unlocked at the begining or not")]
    public bool isJetpackUnlockedAtStart = false;
    [Tooltip("The strength with which the jetpack pushes the player up")]
    public float jetpackAcceleration = 7f;
    [Range(0f, 1f)]
    [Tooltip("This will affect how much using the jetpack will cancel the gravity value, to start going up faster. 0 is not at all, 1 is instant")]
    public float jetpackDownwardVelocityCancelingFactor = 1f;

    [Header("Durations")]
    [Tooltip("Time it takes to consume all the jetpack fuel")]
    public float consumeDuration = 1.5f;
    [Tooltip("Time it takes to completely refill the jetpack while on the ground")]
    public float refillDurationGrounded = 2f;
    [Tooltip("Time it takes to completely refill the jetpack while in the air")]
    public float refillDurationInTheAir = 5f;
    [Tooltip("Delay after last use before starting to refill")]
    public float refillDelay = 1f;

    [Header("Audio")]
    [Tooltip("Sound played when using the jetpack")]
    public AudioClip jetpackSFX;

    bool m_CanUseJetpack;
    PlayerCharacterController m_PlayerCharacterController;
    PlayerInputHandler m_InputHandler;
    float m_LastTimeOfUse;

    // stored ratio for jetpack resource (1 is full, 0 is empty)
    public float currentFillRatio { get; private set; }
    public bool isJetpackUnlocked { get; private set; }

    public bool isPlayergrounded() => m_PlayerCharacterController.isGrounded;

    public UnityAction<bool> onUnlockJetpack;

    void Start()
    {
        isJetpackUnlocked = isJetpackUnlockedAtStart;

        m_PlayerCharacterController = GetComponent<PlayerCharacterController>();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerCharacterController, Jetpack>(m_PlayerCharacterController, this, gameObject);

        m_InputHandler = GetComponent<PlayerInputHandler>();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, Jetpack>(m_InputHandler, this, gameObject);

        currentFillRatio = 1f;

        audioSource.clip = jetpackSFX;
        audioSource.loop = true;
    }

    void Update()
    {
        // jetpack can only be used if not grounded and jump has been pressed again once in-air
        if(isPlayergrounded())
        {
            m_CanUseJetpack = false;
        }
        else if (!m_PlayerCharacterController.hasJumpedThisFrame && m_InputHandler.GetJumpInputDown())
        {
            m_CanUseJetpack = true;
        }

        // jetpack usage
        bool jetpackIsInUse = m_CanUseJetpack && isJetpackUnlocked  && currentFillRatio > 0f && m_InputHandler.GetJumpInputHeld();
        if(jetpackIsInUse)
        {
            // store the last time of use for refill delay
            m_LastTimeOfUse = Time.time;

            float totalAcceleration = jetpackAcceleration;

            // cancel out gravity
            totalAcceleration += m_PlayerCharacterController.gravityDownForce;

            if (m_PlayerCharacterController.characterVelocity.y < 0f)
            {
                // handle making the jetpack compensate for character's downward velocity with bonus acceleration
                totalAcceleration += ((-m_PlayerCharacterController.characterVelocity.y / Time.deltaTime) * jetpackDownwardVelocityCancelingFactor);
            }

            // apply the acceleration to character's velocity
            m_PlayerCharacterController.characterVelocity += Vector3.up * totalAcceleration * Time.deltaTime;

            // consume fuel
            currentFillRatio = currentFillRatio - (Time.deltaTime / consumeDuration);

            for (int i = 0; i < jetpackVfx.Length; i++)
            {
                var emissionModulesVFX = jetpackVfx[i].emission;
                emissionModulesVFX.enabled = true;
            }

            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            // refill the meter over time
            if (isJetpackUnlocked && Time.time - m_LastTimeOfUse >= refillDelay)
            {
                float refillRate = 1 / (m_PlayerCharacterController.isGrounded ? refillDurationGrounded : refillDurationInTheAir);
                currentFillRatio = currentFillRatio + Time.deltaTime * refillRate;
            }

            for (int i = 0; i < jetpackVfx.Length; i++)
            {
                var emissionModulesVFX = jetpackVfx[i].emission;
                emissionModulesVFX.enabled = false;
            }

            // keeps the ratio between 0 and 1
            currentFillRatio = Mathf.Clamp01(currentFillRatio);

            if (audioSource.isPlaying)
                audioSource.Stop();
        }
    }

    public bool TryUnlock()
    {
        if (isJetpackUnlocked)
            return false;

        onUnlockJetpack.Invoke(true);
        isJetpackUnlocked = true;
        m_LastTimeOfUse = Time.time;
        return true;
    }
}
