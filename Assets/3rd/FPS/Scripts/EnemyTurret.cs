using UnityEngine;

[RequireComponent(typeof(EnemyController))]
public class EnemyTurret : MonoBehaviour
{
    public enum AIState
    {
        Idle,
        Attack,
    }

    public Transform turretPivot;
    public Transform turretAimPoint;
    public Animator animator;
    public float aimRotationSharpness = 5f;
    public float lookAtRotationSharpness = 2.5f;
    public float detectionFireDelay = 1f;
    public float aimingTransitionBlendTime = 1f;

    [Tooltip("The random hit damage effects")]
    public ParticleSystem[] randomHitSparks;
    public ParticleSystem[] onDetectVFX;
    public AudioClip onDetectSFX;

    public AIState aiState { get; private set; }

    EnemyController m_EnemyController;
    Health m_Health;
    Quaternion m_RotationWeaponForwardToPivot;
    float m_TimeStartedDetection;
    float m_TimeLostDetection;
    Quaternion m_PreviousPivotAimingRotation;
    Quaternion m_PivotAimingRotation;

    const string k_AnimOnDamagedParameter = "OnDamaged";
    const string k_AnimIsActiveParameter = "IsActive";

    void Start()
    {
        m_Health = GetComponent<Health>();
        DebugUtility.HandleErrorIfNullGetComponent<Health, EnemyTurret>(m_Health, this, gameObject);
        m_Health.onDamaged += OnDamaged;

        m_EnemyController = GetComponent<EnemyController>();
        DebugUtility.HandleErrorIfNullGetComponent<EnemyController, EnemyTurret>(m_EnemyController, this, gameObject);

        m_EnemyController.onDetectedTarget += OnDetectedTarget;
        m_EnemyController.onLostTarget += OnLostTarget;

        // Remember the rotation offset between the pivot's forward and the weapon's forward
        m_RotationWeaponForwardToPivot = Quaternion.Inverse(m_EnemyController.GetCurrentWeapon().weaponMuzzle.rotation) * turretPivot.rotation;

        // Start with idle
        aiState = AIState.Idle;

        m_TimeStartedDetection = Mathf.NegativeInfinity;
        m_PreviousPivotAimingRotation = turretPivot.rotation;
    }

    void Update()
    {
        UpdateCurrentAIState();
    }

    void LateUpdate()
    {
        UpdateTurretAiming();
    }

    void UpdateCurrentAIState()
    {
        // Handle logic 
        switch (aiState)
        {
            case AIState.Attack:
                bool mustShoot = Time.time > m_TimeStartedDetection + detectionFireDelay;
                // Calculate the desired rotation of our turret (aim at target)
                Vector3 directionToTarget = (m_EnemyController.knownDetectedTarget.transform.position - turretAimPoint.position).normalized;
                Quaternion offsettedTargetRotation = Quaternion.LookRotation(directionToTarget) * m_RotationWeaponForwardToPivot;
                m_PivotAimingRotation = Quaternion.Slerp(m_PreviousPivotAimingRotation, offsettedTargetRotation, (mustShoot ? aimRotationSharpness : lookAtRotationSharpness) * Time.deltaTime);
                
                // shoot
                if (mustShoot)
                {
                    Vector3 correctedDirectionToTarget = (m_PivotAimingRotation * Quaternion.Inverse(m_RotationWeaponForwardToPivot)) * Vector3.forward;

                    m_EnemyController.TryAtack(turretAimPoint.position + correctedDirectionToTarget);
                }

                break;
        }
    }

    void UpdateTurretAiming()
    {
        switch (aiState)
        {
            case AIState.Attack:
                turretPivot.rotation = m_PivotAimingRotation;
                break;
            default:
                // Use the turret rotation of the animation
                turretPivot.rotation = Quaternion.Slerp(m_PivotAimingRotation, turretPivot.rotation, (Time.time - m_TimeLostDetection) / aimingTransitionBlendTime);
                break;
        }

        m_PreviousPivotAimingRotation = turretPivot.rotation;
    }

    void OnDamaged(float dmg, GameObject source)
    {
        if (randomHitSparks.Length > 0)
        {
            int n = Random.Range(0, randomHitSparks.Length - 1);
            randomHitSparks[n].Play();
        }

        animator.SetTrigger(k_AnimOnDamagedParameter);
    }

    void OnDetectedTarget()
    {
        if(aiState == AIState.Idle)
        {
            aiState = AIState.Attack;
        }

        for (int i = 0; i < onDetectVFX.Length; i++)
        {
            onDetectVFX[i].Play();
        }

        if (onDetectSFX)
        {
            AudioUtility.CreateSFX(onDetectSFX, transform.position, AudioUtility.AudioGroups.EnemyDetection, 1f);
        }

        animator.SetBool(k_AnimIsActiveParameter, true);
        m_TimeStartedDetection = Time.time;
    }

    void OnLostTarget()
    {
        if (aiState == AIState.Attack)
        {
            aiState = AIState.Idle;
        }

        for (int i = 0; i < onDetectVFX.Length; i++)
        {
            onDetectVFX[i].Stop();
        }

        animator.SetBool(k_AnimIsActiveParameter, false);
        m_TimeLostDetection = Time.time;
    }
}
