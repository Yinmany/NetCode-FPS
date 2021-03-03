using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(Health), typeof(Actor), typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [System.Serializable]
    public struct RendererIndexData
    {
        public Renderer renderer;
        public int materialIndex;

        public RendererIndexData(Renderer renderer, int index)
        {
            this.renderer = renderer;
            this.materialIndex = index;
        }
    }

    [Header("Parameters")]
    [Tooltip("The Y height at which the enemy will be automatically killed (if it falls off of the level)")]
    public float selfDestructYHeight = -20f;
    [Tooltip("The distance at which the enemy considers that it has reached its current path destination point")]
    public float pathReachingRadius = 2f;
    [Tooltip("The speed at which the enemy rotates")]
    public float orientationSpeed = 10f;
    [Tooltip("Delay after death where the GameObject is destroyed (to allow for animation)")]
    public float deathDuration = 0f;


    [Header("Weapons Parameters")]
    [Tooltip("Allow weapon swapping for this enemy")]
    public bool swapToNextWeapon = false;
    [Tooltip("Time delay between a weapon swap and the next attack")]
    public float delayAfterWeaponSwap = 0f;

    [Header("Eye color")]
    [Tooltip("Material for the eye color")]
    public Material eyeColorMaterial;
    [Tooltip("The default color of the bot's eye")]
    [ColorUsageAttribute(true, true)]
    public Color defaultEyeColor;
    [Tooltip("The attack color of the bot's eye")]
    [ColorUsageAttribute(true, true)]
    public Color attackEyeColor;

    [Header("Flash on hit")]
    [Tooltip("The material used for the body of the hoverbot")]
    public Material bodyMaterial;
    [Tooltip("The gradient representing the color of the flash on hit")]
    [GradientUsageAttribute(true)]
    public Gradient onHitBodyGradient;
    [Tooltip("The duration of the flash on hit")]
    public float flashOnHitDuration = 0.5f;

    [Header("Sounds")]
    [Tooltip("Sound played when recieving damages")]
    public AudioClip damageTick;

    [Header("VFX")]
    [Tooltip("The VFX prefab spawned when the enemy dies")]
    public GameObject deathVFX;
    [Tooltip("The point at which the death VFX is spawned")]
    public Transform deathVFXSpawnPoint;

    [Header("Loot")]
    [Tooltip("The object this enemy can drop when dying")]
    public GameObject lootPrefab;
    [Tooltip("The chance the object has to drop")]
    [Range(0, 1)]
    public float dropRate = 1f;

    [Header("Debug Display")]
    [Tooltip("Color of the sphere gizmo representing the path reaching range")]
    public Color pathReachingRangeColor = Color.yellow;
    [Tooltip("Color of the sphere gizmo representing the attack range")]
    public Color attackRangeColor = Color.red;
    [Tooltip("Color of the sphere gizmo representing the detection range")]
    public Color detectionRangeColor = Color.blue;

    public UnityAction onAttack;
    public UnityAction onDetectedTarget;
    public UnityAction onLostTarget;
    public UnityAction onDamaged;


    List<RendererIndexData> m_BodyRenderers = new List<RendererIndexData>();
    MaterialPropertyBlock m_BodyFlashMaterialPropertyBlock;
    float m_LastTimeDamaged = float.NegativeInfinity;

    RendererIndexData m_EyeRendererData;
    MaterialPropertyBlock m_EyeColorMaterialPropertyBlock;

    public PatrolPath patrolPath { get; set; }
    public GameObject knownDetectedTarget => m_DetectionModule.knownDetectedTarget;
    public bool isTargetInAttackRange => m_DetectionModule.isTargetInAttackRange;
    public bool isSeeingTarget => m_DetectionModule.isSeeingTarget;
    public bool hadKnownTarget => m_DetectionModule.hadKnownTarget;
    public NavMeshAgent m_NavMeshAgent { get; private set; }
    public DetectionModule m_DetectionModule { get; private set; }

    int m_PathDestinationNodeIndex;
    EnemyManager m_EnemyManager;
    ActorsManager m_ActorsManager;
    Health m_Health;
    Actor m_Actor;
    Collider[] m_SelfColliders;
    GameFlowManager m_GameFlowManager;
    bool m_WasDamagedThisFrame;
    float m_LastTimeWeaponSwapped = Mathf.NegativeInfinity;
    int m_CurrentWeaponIndex;
    WeaponController m_CurrentWeapon;
    WeaponController[] m_Weapons;
    NavigationModule m_NavigationModule;

    void Start()
    {
        m_EnemyManager = FindObjectOfType<EnemyManager>();
        DebugUtility.HandleErrorIfNullFindObject<EnemyManager, EnemyController>(m_EnemyManager, this);

        m_ActorsManager = FindObjectOfType<ActorsManager>();
        DebugUtility.HandleErrorIfNullFindObject<ActorsManager, EnemyController>(m_ActorsManager, this);

        m_EnemyManager.RegisterEnemy(this);

        m_Health = GetComponent<Health>();
        DebugUtility.HandleErrorIfNullGetComponent<Health, EnemyController>(m_Health, this, gameObject);

        m_Actor = GetComponent<Actor>();
        DebugUtility.HandleErrorIfNullGetComponent<Actor, EnemyController>(m_Actor, this, gameObject);

        m_NavMeshAgent = GetComponent<NavMeshAgent>();
        m_SelfColliders = GetComponentsInChildren<Collider>();

        m_GameFlowManager = FindObjectOfType<GameFlowManager>();
        DebugUtility.HandleErrorIfNullFindObject<GameFlowManager, EnemyController>(m_GameFlowManager, this);

        // Subscribe to damage & death actions
        m_Health.onDie += OnDie;
        m_Health.onDamaged += OnDamaged;

        // Find and initialize all weapons
        FindAndInitializeAllWeapons();
        var weapon = GetCurrentWeapon();
        weapon.ShowWeapon(true);

        var detectionModules = GetComponentsInChildren<DetectionModule>();
        DebugUtility.HandleErrorIfNoComponentFound<DetectionModule, EnemyController>(detectionModules.Length, this, gameObject);
        DebugUtility.HandleWarningIfDuplicateObjects<DetectionModule, EnemyController>(detectionModules.Length, this, gameObject);
        // Initialize detection module
        m_DetectionModule = detectionModules[0];
        m_DetectionModule.onDetectedTarget += OnDetectedTarget;
        m_DetectionModule.onLostTarget += OnLostTarget;
        onAttack += m_DetectionModule.OnAttack;

        var navigationModules = GetComponentsInChildren<NavigationModule>();
        DebugUtility.HandleWarningIfDuplicateObjects<DetectionModule, EnemyController>(detectionModules.Length, this, gameObject);
        // Override navmesh agent data
        if (navigationModules.Length > 0)
        {
            m_NavigationModule = navigationModules[0];
            m_NavMeshAgent.speed = m_NavigationModule.moveSpeed;
            m_NavMeshAgent.angularSpeed = m_NavigationModule.angularSpeed;
            m_NavMeshAgent.acceleration = m_NavigationModule.acceleration;
        }

        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                if (renderer.sharedMaterials[i] == eyeColorMaterial)
                {
                    m_EyeRendererData = new RendererIndexData(renderer, i);
                }

                if (renderer.sharedMaterials[i] == bodyMaterial)
                {
                    m_BodyRenderers.Add(new RendererIndexData(renderer, i));
                }
            }
        }

        m_BodyFlashMaterialPropertyBlock = new MaterialPropertyBlock();

        // Check if we have an eye renderer for this enemy
        if (m_EyeRendererData.renderer != null)
        {
            m_EyeColorMaterialPropertyBlock = new MaterialPropertyBlock();
            m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", defaultEyeColor);
            m_EyeRendererData.renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock, m_EyeRendererData.materialIndex);
        }
    }

    void Update()
    {
        EnsureIsWithinLevelBounds();

        m_DetectionModule.HandleTargetDetection(m_Actor, m_SelfColliders);

        Color currentColor = onHitBodyGradient.Evaluate((Time.time - m_LastTimeDamaged) / flashOnHitDuration);
        m_BodyFlashMaterialPropertyBlock.SetColor("_EmissionColor", currentColor);
        foreach (var data in m_BodyRenderers)
        {
            data.renderer.SetPropertyBlock(m_BodyFlashMaterialPropertyBlock, data.materialIndex);
        }

        m_WasDamagedThisFrame = false;
    }

    void EnsureIsWithinLevelBounds()
    {
        // at every frame, this tests for conditions to kill the enemy
        if (transform.position.y < selfDestructYHeight)
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnLostTarget()
    {
        onLostTarget.Invoke();

        // Set the eye attack color and property block if the eye renderer is set
        if (m_EyeRendererData.renderer != null)
        {
            m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", defaultEyeColor);
            m_EyeRendererData.renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock, m_EyeRendererData.materialIndex);
        }
    }

    void OnDetectedTarget()
    {
        onDetectedTarget.Invoke();

        // Set the eye default color and property block if the eye renderer is set
        if (m_EyeRendererData.renderer != null)
        {
            m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", attackEyeColor);
            m_EyeRendererData.renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock, m_EyeRendererData.materialIndex);
        }
    }

    public void OrientTowards(Vector3 lookPosition)
    {
        Vector3 lookDirection = Vector3.ProjectOnPlane(lookPosition - transform.position, Vector3.up).normalized;
        if (lookDirection.sqrMagnitude != 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * orientationSpeed);
        }
    }

    private bool IsPathValid()
    {
        return patrolPath && patrolPath.pathNodes.Count > 0;
    }

    public void ResetPathDestination()
    {
        m_PathDestinationNodeIndex = 0;
    }

    public void SetPathDestinationToClosestNode()
    {
        if (IsPathValid())
        {
            int closestPathNodeIndex = 0;
            for (int i = 0; i < patrolPath.pathNodes.Count; i++)
            {
                float distanceToPathNode = patrolPath.GetDistanceToNode(transform.position, i);
                if (distanceToPathNode < patrolPath.GetDistanceToNode(transform.position, closestPathNodeIndex))
                {
                    closestPathNodeIndex = i;
                }
            }

            m_PathDestinationNodeIndex = closestPathNodeIndex;
        }
        else
        {
            m_PathDestinationNodeIndex = 0;
        }
    }

    public Vector3 GetDestinationOnPath()
    {
        if (IsPathValid())
        {
            return patrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
        }
        else
        {
            return transform.position;
        }
    }

    public void SetNavDestination(Vector3 destination)
    {
        if (m_NavMeshAgent)
        {
            m_NavMeshAgent.SetDestination(destination);
        }
    }

    public void UpdatePathDestination(bool inverseOrder = false)
    {
        if (IsPathValid())
        {
            // Check if reached the path destination
            if ((transform.position - GetDestinationOnPath()).magnitude <= pathReachingRadius)
            {
                // increment path destination index
                m_PathDestinationNodeIndex = inverseOrder ? (m_PathDestinationNodeIndex - 1) : (m_PathDestinationNodeIndex + 1);
                if (m_PathDestinationNodeIndex < 0)
                {
                    m_PathDestinationNodeIndex += patrolPath.pathNodes.Count;
                }
                if (m_PathDestinationNodeIndex >= patrolPath.pathNodes.Count)
                {
                    m_PathDestinationNodeIndex -= patrolPath.pathNodes.Count;
                }
            }
        }
    }

    void OnDamaged(float damage, GameObject damageSource)
    {
        // test if the damage source is the player
        if (damageSource && damageSource.GetComponent<PlayerCharacterController>())
        {
            // pursue the player
            m_DetectionModule.OnDamaged(damageSource);

            if (onDamaged != null)
            {
                onDamaged.Invoke();
            }
            m_LastTimeDamaged = Time.time;

            // play the damage tick sound
            if (damageTick && !m_WasDamagedThisFrame)
                AudioUtility.CreateSFX(damageTick, transform.position, AudioUtility.AudioGroups.DamageTick, 0f);

            m_WasDamagedThisFrame = true;
        }
    }

    void OnDie()
    {
        // spawn a particle system when dying
        var vfx = Instantiate(deathVFX, deathVFXSpawnPoint.position, Quaternion.identity);
        Destroy(vfx, 5f);

        // tells the game flow manager to handle the enemy destuction
        m_EnemyManager.UnregisterEnemy(this);

        // loot an object
        if (TryDropItem())
        {
            Instantiate(lootPrefab, transform.position, Quaternion.identity);
        }

        // this will call the OnDestroy function
        Destroy(gameObject, deathDuration);
    }

    private void OnDrawGizmosSelected()
    {
        // Path reaching range
        Gizmos.color = pathReachingRangeColor;
        Gizmos.DrawWireSphere(transform.position, pathReachingRadius);

        if (m_DetectionModule != null)
        {
            // Detection range
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, m_DetectionModule.detectionRange);

            // Attack range
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, m_DetectionModule.attackRange);
        }
    }

    public void OrientWeaponsTowards(Vector3 lookPosition)
    {
        for (int i = 0; i < m_Weapons.Length; i++)
        {
            // orient weapon towards player
            Vector3 weaponForward = (lookPosition - m_Weapons[i].weaponRoot.transform.position).normalized;
            m_Weapons[i].transform.forward = weaponForward;
        }
    }

    public bool TryAtack(Vector3 enemyPosition)
    {
        if (m_GameFlowManager.gameIsEnding)
            return false;

        OrientWeaponsTowards(enemyPosition);

        if ((m_LastTimeWeaponSwapped + delayAfterWeaponSwap) >= Time.time)
            return false;

        // Shoot the weapon
        bool didFire = GetCurrentWeapon().HandleShootInputs(false, true, false);

        if (didFire && onAttack != null)
        {
            onAttack.Invoke();

            if (swapToNextWeapon && m_Weapons.Length > 1)
            {
                int nextWeaponIndex = (m_CurrentWeaponIndex + 1) % m_Weapons.Length;
                SetCurrentWeapon(nextWeaponIndex);
            }
        }

        return didFire;
    }

    public bool TryDropItem()
    {
        if (dropRate == 0 || lootPrefab == null)
            return false;
        else if (dropRate == 1)
            return true;
        else
            return (Random.value <= dropRate);
    }

    void FindAndInitializeAllWeapons()
    {
        // Check if we already found and initialized the weapons
        if (m_Weapons == null)
        {
            m_Weapons = GetComponentsInChildren<WeaponController>();
            DebugUtility.HandleErrorIfNoComponentFound<WeaponController, EnemyController>(m_Weapons.Length, this, gameObject);

            for (int i = 0; i < m_Weapons.Length; i++)
            {
                m_Weapons[i].owner = gameObject;
            }
        }
    }

    public WeaponController GetCurrentWeapon()
    {
        FindAndInitializeAllWeapons();
        // Check if no weapon is currently selected
        if (m_CurrentWeapon == null)
        {
            // Set the first weapon of the weapons list as the current weapon
            SetCurrentWeapon(0);
        }
        DebugUtility.HandleErrorIfNullGetComponent<WeaponController, EnemyController>(m_CurrentWeapon, this, gameObject);

        return m_CurrentWeapon;
    }

    void SetCurrentWeapon(int index)
    {
        m_CurrentWeaponIndex = index;
        m_CurrentWeapon = m_Weapons[m_CurrentWeaponIndex];
        if (swapToNextWeapon)
        {
            m_LastTimeWeaponSwapped = Time.time;
        }
        else
        {
            m_LastTimeWeaponSwapped = Mathf.NegativeInfinity;
        }
    }
}
