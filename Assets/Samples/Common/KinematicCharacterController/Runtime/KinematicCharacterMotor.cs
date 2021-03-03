using System;
using MyGameLib.NetCode;
using Unity.Entities;
using UnityEngine;

namespace KinematicCharacterController
{
    public enum RigidbodyInteractionType
    {
        None,
        Kinematic,
        SimulatedDynamic
    }

    public enum StepHandlingMethod
    {
        None,
        Standard,
        Extra
    }

    public enum MovementSweepState
    {
        Initial,
        AfterFirstHit,
        FoundBlockingCrease,
        FoundBlockingCorner,
    }

    /// <summary>
    /// Represents the entire state of a character motor that is pertinent for simulation.
    /// Use this to save state or revert to past state
    /// </summary>
    [System.Serializable]
    public struct KinematicCharacterMotorState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 BaseVelocity;

        public bool MustUnground;
        public float MustUngroundTime;
        public bool LastMovementIterationFoundAnyGround;
        public CharacterTransientGroundingReport GroundingStatus;

        public Rigidbody AttachedRigidbody;
        public Vector3 AttachedRigidbodyVelocity;
    }

    /// <summary>
    /// Describes an overlap between the character capsule and another collider
    /// </summary>
    public struct OverlapResult
    {
        public Vector3 Normal;
        public Collider Collider;

        public OverlapResult(Vector3 normal, Collider collider)
        {
            Normal = normal;
            Collider = collider;
        }
    }

    /// <summary>
    /// Contains all the information for the motor's grounding status
    /// </summary>
    public struct CharacterGroundingReport
    {
        public bool FoundAnyGround;
        public bool IsStableOnGround;
        public bool SnappingPrevented;
        public Vector3 GroundNormal;
        public Vector3 InnerGroundNormal;
        public Vector3 OuterGroundNormal;

        public Collider GroundCollider;
        public Vector3 GroundPoint;

        public void CopyFrom(CharacterTransientGroundingReport transientGroundingReport)
        {
            FoundAnyGround = transientGroundingReport.FoundAnyGround;
            IsStableOnGround = transientGroundingReport.IsStableOnGround;
            SnappingPrevented = transientGroundingReport.SnappingPrevented;
            GroundNormal = transientGroundingReport.GroundNormal;
            InnerGroundNormal = transientGroundingReport.InnerGroundNormal;
            OuterGroundNormal = transientGroundingReport.OuterGroundNormal;

            GroundCollider = null;
            GroundPoint = Vector3.zero;
        }
    }

    /// <summary>
    /// Contains the simulation-relevant information for the motor's grounding status
    /// </summary>
    public struct CharacterTransientGroundingReport : IComponentData
    {
        public bool FoundAnyGround;
        public bool IsStableOnGround;
        public bool SnappingPrevented;
        public Vector3 GroundNormal;
        public Vector3 InnerGroundNormal;
        public Vector3 OuterGroundNormal;

        public void CopyFrom(CharacterGroundingReport groundingReport)
        {
            FoundAnyGround = groundingReport.FoundAnyGround;
            IsStableOnGround = groundingReport.IsStableOnGround;
            SnappingPrevented = groundingReport.SnappingPrevented;
            GroundNormal = groundingReport.GroundNormal;
            InnerGroundNormal = groundingReport.InnerGroundNormal;
            OuterGroundNormal = groundingReport.OuterGroundNormal;
        }
    }

    /// <summary>
    /// Contains all the information from a hit stability evaluation
    /// </summary>
    public struct HitStabilityReport
    {
        public bool IsStable;

        public Vector3 InnerNormal;
        public Vector3 OuterNormal;

        public bool ValidStepDetected;
        public Collider SteppedCollider;

        public bool LedgeDetected;
        public bool IsOnEmptySideOfLedge;
        public float DistanceFromLedge;
        public bool IsMovingTowardsEmptySideOfLedge;
        public Vector3 LedgeGroundNormal;
        public Vector3 LedgeRightDirection;
        public Vector3 LedgeFacingDirection;
    }

    /// <summary>
    /// Contains the information of hit rigidbodies during the movement phase, so they can be processed afterwards
    /// </summary>
    public struct RigidbodyProjectionHit
    {
        public Rigidbody Rigidbody;
        public Vector3 HitPoint;
        public Vector3 EffectiveHitNormal;
        public Vector3 HitVelocity;
        public bool StableOnHit;
    }

    /// <summary>
    /// Component that manages character collisions and movement solving
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public class KinematicCharacterMotor : MonoBehaviour
    {
#pragma warning disable 0414
        public PhysicsScene physics;

        [Header("Components")]
        /// <summary>
        /// The capsule collider of this motor
        /// </summary>
        [ReadOnly]
        public CapsuleCollider Capsule;

        [Header("Capsule Settings")]
        /// <summary>
        /// Radius of the character's capsule
        /// </summary>
        [SerializeField]
        [Tooltip("Radius of the Character Capsule")]
        private float CapsuleRadius = 0.5f;

        /// <summary>
        /// Height of the character's capsule
        /// </summary>
        [SerializeField] [Tooltip("Height of the Character Capsule")]
        private float CapsuleHeight = 2f;

        /// <summary>
        /// Local y position of the character's capsule center
        /// </summary>
        [SerializeField] [Tooltip("Height of the Character Capsule")]
        private float CapsuleYOffset = 1f;

        /// <summary>
        /// Physics material of the character's capsule
        /// </summary>
        [SerializeField]
        [Tooltip(
            "Physics material of the Character Capsule (Does not affect character movement. Only affects things colliding with it)")]
#pragma warning disable 0649
        private PhysicMaterial CapsulePhysicsMaterial;
#pragma warning restore 0649


        [Header("Misc settings")]
        /// <summary>
        /// Increases the range of ground detection, to allow snapping to ground at very high speeds
        /// </summary>    
        [Tooltip("Increases the range of ground detection, to allow snapping to ground at very high speeds")]
        public float GroundDetectionExtraDistance = 0f;

        /// <summary>
        /// Maximum slope angle on which the character can be stable
        /// </summary>    
        [Range(0f, 89f)] [Tooltip("Maximum slope angle on which the character can be stable")]
        public float MaxStableSlopeAngle = 60f;

        /// <summary>
        /// Which layers can the character be considered stable on
        /// </summary>    
        [Tooltip("Which layers can the character be considered stable on")]
        public LayerMask StableGroundLayers = -1;

        /// <summary>
        /// Notifies the Character Controller when discrete collisions are detected
        /// </summary>    
        [Tooltip("Notifies the Character Controller when discrete collisions are detected")]
        public bool DiscreteCollisionEvents = false;


        [Header("Step settings")]
        /// <summary>
        /// Handles properly detecting grounding status on steps, but has a performance cost.
        /// </summary>
        [Tooltip("Handles properly detecting grounding status on steps, but has a performance cost.")]
        public StepHandlingMethod StepHandling = StepHandlingMethod.Standard;

        /// <summary>
        /// Maximum height of a step which the character can climb
        /// </summary>    
        [Tooltip("Maximum height of a step which the character can climb")]
        public float MaxStepHeight = 0.5f;

        /// <summary>
        /// Can the character step up obstacles even if it is not currently stable?
        /// </summary>    
        [Tooltip("Can the character step up obstacles even if it is not currently stable?")]
        public bool AllowSteppingWithoutStableGrounding = false;

        /// <summary>
        /// Minimum length of a step that the character can step on (used in Extra stepping method. Use this to let the character step on steps that are smaller that its radius
        /// </summary>    
        [Tooltip(
            "Minimum length of a step that the character can step on (used in Extra stepping method). Use this to let the character step on steps that are smaller that its radius")]
        public float MinRequiredStepDepth = 0.1f;


        [Header("Ledge settings")]
        /// <summary>
        /// Handles properly detecting ledge information and grounding status, but has a performance cost.
        /// </summary>
        [Tooltip("Handles properly detecting ledge information and grounding status, but has a performance cost.")]
        public bool LedgeAndDenivelationHandling = true;

        /// <summary>
        /// The distance from the capsule central axis at which the character can stand on a ledge and still be stable
        /// </summary>    
        [Tooltip(
            "The distance from the capsule central axis at which the character can stand on a ledge and still be stable")]
        public float MaxStableDistanceFromLedge = 0.5f;

        /// <summary>
        /// Prevents snapping to ground on ledges beyond a certain velocity
        /// </summary>    
        [Tooltip("Prevents snapping to ground on ledges beyond a certain velocity")]
        public float MaxVelocityForLedgeSnap = 0f;

        /// <summary>
        /// The maximun downward slope angle change that the character can be subjected to and still be snapping to the ground
        /// </summary>    
        [Tooltip(
            "The maximun downward slope angle change that the character can be subjected to and still be snapping to the ground")]
        [Range(1f, 180f)]
        public float MaxStableDenivelationAngle = 180f;


        [Header("Rigidbody interaction settings")]
        /// <summary>
        /// Handles properly being pushed by and standing on PhysicsMovers or dynamic rigidbodies. Also handles pushing dynamic rigidbodies
        /// </summary>
        [Tooltip(
            "Handles properly being pushed by and standing on PhysicsMovers or dynamic rigidbodies. Also handles pushing dynamic rigidbodies")]
        public bool InteractiveRigidbodyHandling = true;

        /// <summary>
        /// How the character interacts with non-kinematic rigidbodies. \"Kinematic\" mode means the character pushes the rigidbodies with infinite force (as a kinematic body would). \"SimulatedDynamic\" pushes the rigidbodies with a simulated mass value.
        /// </summary>
        [Tooltip(
            "How the character interacts with non-kinematic rigidbodies. \"Kinematic\" mode means the character pushes the rigidbodies with infinite force (as a kinematic body would). \"SimulatedDynamic\" pushes the rigidbodies with a simulated mass value.")]
        public RigidbodyInteractionType RigidbodyInteractionType;

        /// <summary>
        /// Determines if the character preserves moving platform velocities when de-grounding from them
        /// </summary>
        [Tooltip("Determines if the character preserves moving platform velocities when de-grounding from them")]
        public bool PreserveAttachedRigidbodyMomentum = true;


        [Header("Constraints settings")]
        /// <summary>
        /// Determines if the character's movement uses the planar constraint
        /// </summary>
        [Tooltip("Determines if the character's movement uses the planar constraint")]
        public bool HasPlanarConstraint = false;

        /// <summary>
        /// Defines the plane that the character's movement is constrained on, if HasMovementConstraintPlane is active
        /// </summary>
        [Tooltip(
            "Defines the plane that the character's movement is constrained on, if HasMovementConstraintPlane is active")]
        public Vector3 PlanarConstraintAxis = Vector3.forward;

        /// <summary>
        /// Contains the current grounding information
        /// </summary>
        [System.NonSerialized] public CharacterGroundingReport GroundingStatus = new CharacterGroundingReport();

        /// <summary>
        /// Contains the previous grounding information
        /// </summary>
        [System.NonSerialized]
        public CharacterTransientGroundingReport LastGroundingStatus = new CharacterTransientGroundingReport();

        /// <summary>
        /// Specifies the LayerMask that the character's movement algorithm can detect collisions with. By default, this uses the rigidbody's layer's collision matrix
        /// </summary>
        [System.NonSerialized] public LayerMask CollidableLayers = -1;

        /// <summary>
        /// The Transform of the character motor
        /// </summary>
        public Transform Transform
        {
            get { return _transform; }
        }

        private Transform _transform;

        /// <summary>
        /// The character's goal position in its movement calculations (always up-to-date during the character update phase)
        /// </summary>
        public Vector3 TransientPosition
        {
            get { return _transientPosition; }
        }

        private Vector3 _transientPosition;

        /// <summary>
        /// The character's up direction (always up-to-date during the character update phase)
        /// </summary>
        public Vector3 CharacterUp
        {
            get { return _characterUp; }
        }

        private Vector3 _characterUp;

        /// <summary>
        /// The character's forward direction (always up-to-date during the character update phase)
        /// </summary>
        public Vector3 CharacterForward
        {
            get { return _characterForward; }
        }

        private Vector3 _characterForward;

        /// <summary>
        /// The character's right direction (always up-to-date during the character update phase)
        /// </summary>
        public Vector3 CharacterRight
        {
            get { return _characterRight; }
        }

        private Vector3 _characterRight;

        /// <summary>
        /// The character's position before the movement calculations began
        /// </summary>
        public Vector3 InitialSimulationPosition
        {
            get { return _initialSimulationPosition; }
        }

        private Vector3 _initialSimulationPosition;

        /// <summary>
        /// The character's rotation before the movement calculations began
        /// </summary>
        public Quaternion InitialSimulationRotation
        {
            get { return _initialSimulationRotation; }
        }

        private Quaternion _initialSimulationRotation;

        /// <summary>
        /// Represents the Rigidbody to stay attached to
        /// </summary>
        public Rigidbody AttachedRigidbody
        {
            get { return _attachedRigidbody; }
        }

        private Rigidbody _attachedRigidbody;

        /// <summary>
        /// Vector3 from the character transform position to the capsule center
        /// </summary>
        public Vector3 CharacterTransformToCapsuleCenter
        {
            get { return _characterTransformToCapsuleCenter; }
        }

        private Vector3 _characterTransformToCapsuleCenter;

        /// <summary>
        /// Vector3 from the character transform position to the capsule bottom
        /// </summary>
        public Vector3 CharacterTransformToCapsuleBottom
        {
            get { return _characterTransformToCapsuleBottom; }
        }

        private Vector3 _characterTransformToCapsuleBottom;

        /// <summary>
        /// Vector3 from the character transform position to the capsule top
        /// </summary>
        public Vector3 CharacterTransformToCapsuleTop
        {
            get { return _characterTransformToCapsuleTop; }
        }

        private Vector3 _characterTransformToCapsuleTop;

        /// <summary>
        /// Vector3 from the character transform position to the capsule bottom hemi center
        /// </summary>
        public Vector3 CharacterTransformToCapsuleBottomHemi
        {
            get { return _characterTransformToCapsuleBottomHemi; }
        }

        private Vector3 _characterTransformToCapsuleBottomHemi;

        /// <summary>
        /// Vector3 from the character transform position to the capsule top hemi center
        /// </summary>
        public Vector3 CharacterTransformToCapsuleTopHemi
        {
            get { return _characterTransformToCapsuleTopHemi; }
        }

        private Vector3 _characterTransformToCapsuleTopHemi;

        /// <summary>
        /// The character's velocity resulting from standing on rigidbodies or PhysicsMover
        /// </summary>
        public Vector3 AttachedRigidbodyVelocity
        {
            get { return _attachedRigidbodyVelocity; }
        }

        private Vector3 _attachedRigidbodyVelocity;

        /// <summary>
        /// The number of overlaps detected so far during character update (is reset at the beginning of the update)
        /// </summary>
        public int OverlapsCount
        {
            get { return _overlapsCount; }
        }

        private int _overlapsCount;

        /// <summary>
        /// The overlaps detected so far during character update
        /// </summary>
        public OverlapResult[] Overlaps
        {
            get { return _overlaps; }
        }

        private OverlapResult[] _overlaps = new OverlapResult[MaxRigidbodyOverlapsCount];

        /// <summary>
        /// The motor's assigned controller
        /// </summary>
        [NonSerialized] public ICharacterController CharacterController;

        /// <summary>
        /// Did the motor's last swept collision detection find a ground?
        /// </summary>
        [NonSerialized] public bool LastMovementIterationFoundAnyGround;

        /// <summary>
        /// Index of this motor in KinematicCharacterSystem arrays
        /// </summary>
        [NonSerialized] public int IndexInCharacterSystem;

        /// <summary>
        /// Remembers initial position before all simulation are done
        /// </summary>
        [NonSerialized] public Vector3 InitialTickPosition;

        /// <summary>
        /// Remembers initial rotation before all simulation are done
        /// </summary>
        [NonSerialized] public Quaternion InitialTickRotation;

        /// <summary>
        /// Specifies a Rigidbody to stay attached to
        /// </summary>
        [NonSerialized] public Rigidbody AttachedRigidbodyOverride;

        /// <summary>
        /// The character's velocity resulting from direct movement
        /// </summary>
        [NonSerialized] public Vector3 BaseVelocity;

        // Private
        private RaycastHit[] _internalCharacterHits = new RaycastHit[MaxHitsBudget];
        private Collider[] _internalProbedColliders = new Collider[MaxCollisionBudget];
        private Rigidbody[] _rigidbodiesPushedThisMove = new Rigidbody[MaxCollisionBudget];

        private RigidbodyProjectionHit[] _internalRigidbodyProjectionHits =
            new RigidbodyProjectionHit[MaxMovementSweepIterations];

        private Rigidbody _lastAttachedRigidbody;
        private bool _solveMovementCollisions = true;
        private bool _solveGrounding = true;
        private bool _movePositionDirty = false;
        private Vector3 _movePositionTarget = Vector3.zero;
        private bool _moveRotationDirty = false;
        private Quaternion _moveRotationTarget = Quaternion.identity;
        private bool _lastSolvedOverlapNormalDirty = false;
        private Vector3 _lastSolvedOverlapNormal = Vector3.forward;
        private int _rigidbodiesPushedCount = 0;
        private int _rigidbodyProjectionHitCount = 0;
        private float _internalResultingMovementMagnitude = 0f;
        private Vector3 _internalResultingMovementDirection = Vector3.zero;
        private bool _isMovingFromAttachedRigidbody = false;
        private bool _mustUnground = false;
        private float _mustUngroundTimeCounter = 0f;
        private Vector3 _cachedWorldUp = Vector3.up;
        private Vector3 _cachedWorldForward = Vector3.forward;
        private Vector3 _cachedWorldRight = Vector3.right;
        private Vector3 _cachedZeroVector = Vector3.zero;

        private Quaternion _transientRotation;

        /// <summary>
        /// The character's goal rotation in its movement calculations (always up-to-date during the character update phase)
        /// </summary>
        public Quaternion TransientRotation
        {
            get { return _transientRotation; }
            private set
            {
                _transientRotation = value;
                _characterUp = _transientRotation * _cachedWorldUp;
                _characterForward = _transientRotation * _cachedWorldForward;
                _characterRight = _transientRotation * _cachedWorldRight;
            }
        }

        /// <summary>
        /// The character's total velocity, including velocity from standing on rigidbodies or PhysicsMover
        /// </summary>
        public Vector3 Velocity
        {
            get { return BaseVelocity + _attachedRigidbodyVelocity; }
        }

        // Warning: Don't touch these constants unless you know exactly what you're doing!
        public const int MaxHitsBudget = 16;
        public const int MaxCollisionBudget = 16;
        public const int MaxGroundingSweepIterations = 2;
        public const int MaxMovementSweepIterations = 6;
        public const int MaxSteppingSweepIterations = 3;
        public const int MaxRigidbodyOverlapsCount = 16;
        public const int MaxDiscreteCollisionIterations = 3;
        public const float CollisionOffset = 0.001f;
        public const float GroundProbeReboundDistance = 0.02f;
        public const float MinimumGroundProbingDistance = 0.005f;
        public const float GroundProbingBackstepDistance = 0.1f;
        public const float SweepProbingBackstepDistance = 0.002f;
        public const float SecondaryProbesVertical = 0.02f;
        public const float SecondaryProbesHorizontal = 0.001f;
        public const float MinVelocityMagnitude = 0.01f;
        public const float SteppingForwardDistance = 0.03f;
        public const float MinDistanceForLedge = 0.05f;
        public const float CorrelationForVerticalObstruction = 0.01f;
        public const float ExtraSteppingForwardDistance = 0.01f;
        public const float ExtraStepHeightPadding = 0.01f;
#pragma warning restore 0414

        private void OnEnable()
        {
            // KinematicCharacterSystem.EnsureCreation();
            // KinematicCharacterSystem.RegisterCharacterMotor(this);
        }

        private void OnDisable()
        {
            // KinematicCharacterSystem.UnregisterCharacterMotor(this);
        }

        private void Reset()
        {
            ValidateData();
        }

        private void OnValidate()
        {
            ValidateData();
        }

        [ContextMenu("Remove Component")]
        private void HandleRemoveComponent()
        {
            CapsuleCollider tmpCapsule = gameObject.GetComponent<CapsuleCollider>();
            DestroyImmediate(this);
            DestroyImmediate(tmpCapsule, true);
        }

        /// <summary>
        /// Handle validating all required values
        /// </summary>
        public void ValidateData()
        {
            if (GetComponent<Rigidbody>())
            {
                GetComponent<Rigidbody>().hideFlags = HideFlags.None;
            }

            Capsule = GetComponent<CapsuleCollider>();
            CapsuleRadius = Mathf.Clamp(CapsuleRadius, 0f, CapsuleHeight * 0.5f);
            Capsule.isTrigger = false;
            Capsule.direction = 1;
            Capsule.sharedMaterial = CapsulePhysicsMaterial;
            SetCapsuleDimensions(CapsuleRadius, CapsuleHeight, CapsuleYOffset);

            MaxStepHeight = Mathf.Clamp(MaxStepHeight, 0f, Mathf.Infinity);
            MinRequiredStepDepth = Mathf.Clamp(MinRequiredStepDepth, 0f, CapsuleRadius);

            MaxStableDistanceFromLedge = Mathf.Clamp(MaxStableDistanceFromLedge, 0f, CapsuleRadius);

            transform.localScale = Vector3.one;

#if UNITY_EDITOR
            Capsule.hideFlags = HideFlags.NotEditable;
            if (!Mathf.Approximately(transform.lossyScale.x, 1f) || !Mathf.Approximately(transform.lossyScale.y, 1f) ||
                !Mathf.Approximately(transform.lossyScale.z, 1f))
            {
                Debug.LogError(
                    "Character's lossy scale is not (1,1,1). This is not allowed. Make sure the character's transform and all of its parents have a (1,1,1) scale.",
                    this.gameObject);
            }
#endif
        }

        /// <summary>
        /// Sets whether or not the capsule collider will detect collisions
        /// </summary>
        public void SetCapsuleCollisionsActivation(bool collisionsActive)
        {
            Capsule.isTrigger = !collisionsActive;
        }

        /// <summary>
        /// Sets whether or not the motor will solve collisions when moving (or moved onto)
        /// </summary>
        public void SetMovementCollisionsSolvingActivation(bool movementCollisionsSolvingActive)
        {
            _solveMovementCollisions = movementCollisionsSolvingActive;
        }

        /// <summary>
        /// Sets whether or not grounding will be evaluated for all hits
        /// </summary>
        public void SetGroundSolvingActivation(bool stabilitySolvingActive)
        {
            _solveGrounding = stabilitySolvingActive;
        }

        /// <summary>
        /// Sets the character's position directly
        /// </summary>
        public void SetPosition(Vector3 position, bool bypassInterpolation = true)
        {
            _transform.position = position;
            _initialSimulationPosition = position;
            _transientPosition = position;

            if (bypassInterpolation)
            {
                InitialTickPosition = position;
            }
        }

        /// <summary>
        /// Sets the character's rotation directly
        /// </summary>
        public void SetRotation(Quaternion rotation, bool bypassInterpolation = true)
        {
            _transform.rotation = rotation;
            _initialSimulationRotation = rotation;
            TransientRotation = rotation;

            if (bypassInterpolation)
            {
                InitialTickRotation = rotation;
            }
        }

        /// <summary>
        /// Sets the character's position and rotation directly
        /// </summary>
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation, bool bypassInterpolation = true)
        {
            _transform.SetPositionAndRotation(position, rotation);
            _initialSimulationPosition = position;
            _initialSimulationRotation = rotation;
            _transientPosition = position;
            TransientRotation = rotation;

            if (bypassInterpolation)
            {
                InitialTickPosition = position;
                InitialTickRotation = rotation;
            }
        }

        /// <summary>
        /// Moves the character position, taking all movement collision solving int account. The actual move is done the next time the motor updates are called
        /// </summary>
        public void MoveCharacter(Vector3 toPosition)
        {
            _movePositionDirty = true;
            _movePositionTarget = toPosition;
        }

        /// <summary>
        /// Moves the character rotation. The actual move is done the next time the motor updates are called
        /// </summary>
        public void RotateCharacter(Quaternion toRotation)
        {
            _moveRotationDirty = true;
            _moveRotationTarget = toRotation;
        }

        /// <summary>
        /// Returns all the state information of the motor that is pertinent for simulation
        /// </summary>
        public KinematicCharacterMotorState GetState()
        {
            KinematicCharacterMotorState state = new KinematicCharacterMotorState();

            state.Position = _transientPosition;
            state.Rotation = _transientRotation;

            state.BaseVelocity = BaseVelocity;
            state.AttachedRigidbodyVelocity = _attachedRigidbodyVelocity;

            state.MustUnground = _mustUnground;
            state.MustUngroundTime = _mustUngroundTimeCounter;
            state.LastMovementIterationFoundAnyGround = LastMovementIterationFoundAnyGround;
            state.GroundingStatus.CopyFrom(GroundingStatus);
            state.AttachedRigidbody = _attachedRigidbody;

            return state;
        }

        /// <summary>
        /// Applies a motor state instantly
        /// </summary>
        public void ApplyState(KinematicCharacterMotorState state, bool bypassInterpolation = true)
        {
            SetPositionAndRotation(state.Position, state.Rotation, bypassInterpolation);

            BaseVelocity = state.BaseVelocity;
            _attachedRigidbodyVelocity = state.AttachedRigidbodyVelocity;

            _mustUnground = state.MustUnground;
            _mustUngroundTimeCounter = state.MustUngroundTime;
            LastMovementIterationFoundAnyGround = state.LastMovementIterationFoundAnyGround;
            GroundingStatus.CopyFrom(state.GroundingStatus);
            _attachedRigidbody = state.AttachedRigidbody;
        }

        /// <summary>
        /// Resizes capsule. ALso caches importand capsule size data
        /// </summary>
        public void SetCapsuleDimensions(float radius, float height, float yOffset)
        {
            CapsuleRadius = radius;
            CapsuleHeight = height;
            CapsuleYOffset = yOffset;

            Capsule.radius = CapsuleRadius;
            Capsule.height = Mathf.Clamp(CapsuleHeight, CapsuleRadius * 2f, CapsuleHeight);
            Capsule.center = new Vector3(0f, CapsuleYOffset, 0f);

            _characterTransformToCapsuleCenter = Capsule.center;
            _characterTransformToCapsuleBottom = Capsule.center + (-_cachedWorldUp * (Capsule.height * 0.5f));
            _characterTransformToCapsuleTop = Capsule.center + (_cachedWorldUp * (Capsule.height * 0.5f));
            _characterTransformToCapsuleBottomHemi = Capsule.center + (-_cachedWorldUp * (Capsule.height * 0.5f)) +
                                                     (_cachedWorldUp * Capsule.radius);
            _characterTransformToCapsuleTopHemi = Capsule.center + (_cachedWorldUp * (Capsule.height * 0.5f)) +
                                                  (-_cachedWorldUp * Capsule.radius);
        }

        private void Awake()
        {
            _transform = this.transform;
            ValidateData();

            _transientPosition = _transform.position;
            TransientRotation = _transform.rotation;

            // Build CollidableLayers mask
            CollidableLayers = 0;

            for (int i = 0; i < 32; i++)
            {
                if (!Physics.GetIgnoreLayerCollision(this.gameObject.layer, i))
                {
                    CollidableLayers |= (1 << i);
                }
            }

            SetCapsuleDimensions(CapsuleRadius, CapsuleHeight, CapsuleYOffset);
        }

        /// <summary>
        /// Update phase 1 is meant to be called after physics movers have calculated their velocities, but
        /// before they have simulated their goal positions/rotations. It is responsible for:
        /// - Initializing all values for update
        /// - Handling MovePosition calls
        /// - Solving initial collision overlaps
        /// - Ground probing
        /// - Handle detecting potential interactable rigidbodies
        /// </summary>
        public void UpdatePhase1(float deltaTime)
        {
            // NaN propagation safety stop
            if (float.IsNaN(BaseVelocity.x) || float.IsNaN(BaseVelocity.y) || float.IsNaN(BaseVelocity.z))
            {
                BaseVelocity = Vector3.zero;
            }

            if (float.IsNaN(_attachedRigidbodyVelocity.x) || float.IsNaN(_attachedRigidbodyVelocity.y) ||
                float.IsNaN(_attachedRigidbodyVelocity.z))
            {
                _attachedRigidbodyVelocity = Vector3.zero;
            }

#if UNITY_EDITOR
            if (!Mathf.Approximately(_transform.lossyScale.x, 1f) ||
                !Mathf.Approximately(_transform.lossyScale.y, 1f) || !Mathf.Approximately(_transform.lossyScale.z, 1f))
            {
                Debug.LogError(
                    "Character's lossy scale is not (1,1,1). This is not allowed. Make sure the character's transform and all of its parents have a (1,1,1) scale.",
                    this.gameObject);
            }
#endif

            if (CharacterController == null)
                Debug.Log(name);
            // Before update
            CharacterController.BeforeCharacterUpdate(deltaTime);

            _transientPosition = _transform.position;
            TransientRotation = _transform.rotation;
            _initialSimulationPosition = _transientPosition;
            _initialSimulationRotation = _transientRotation;
            _rigidbodyProjectionHitCount = 0;
            _overlapsCount = 0;
            _lastSolvedOverlapNormalDirty = false;

            #region Handle Move Position

            if (_movePositionDirty)
            {
                if (_solveMovementCollisions)
                {
                    if (InternalCharacterMove((_movePositionTarget - _transientPosition), deltaTime,
                        out _internalResultingMovementMagnitude, out _internalResultingMovementDirection))
                    {
                        if (InteractiveRigidbodyHandling)
                        {
                            Vector3 tmpVelocity = Vector3.zero;
                            ProcessVelocityForRigidbodyHits(ref tmpVelocity, deltaTime);
                        }
                    }
                }
                else
                {
                    _transientPosition = _movePositionTarget;
                }

                _movePositionDirty = false;
            }

            #endregion

            LastGroundingStatus.CopyFrom(GroundingStatus);
            GroundingStatus = new CharacterGroundingReport();
            GroundingStatus.GroundNormal = _characterUp;

            if (_solveMovementCollisions)
            {
                #region Resolve initial overlaps

                Vector3 resolutionDirection = _cachedWorldUp;
                float resolutionDistance = 0f;
                int iterationsMade = 0;
                bool overlapSolved = false;
                while (iterationsMade < MaxDiscreteCollisionIterations && !overlapSolved)
                {
                    int nbOverlaps = CharacterCollisionsOverlap(_transientPosition, _transientRotation,
                        _internalProbedColliders);

                    if (nbOverlaps > 0)
                    {
                        // Solve overlaps that aren't against dynamic rigidbodies or physics movers
                        for (int i = 0; i < nbOverlaps; i++)
                        {
                            if (GetInteractiveRigidbody(_internalProbedColliders[i]) == null)
                            {
                                // Process overlap
                                Transform overlappedTransform = _internalProbedColliders[i].GetComponent<Transform>();
                                if (Physics.ComputePenetration(
                                    Capsule,
                                    _transientPosition,
                                    _transientRotation,
                                    _internalProbedColliders[i],
                                    overlappedTransform.position,
                                    overlappedTransform.rotation,
                                    out resolutionDirection,
                                    out resolutionDistance))
                                {
                                    // Resolve along obstruction direction
                                    Vector3 originalResolutionDirection = resolutionDirection;
                                    HitStabilityReport mockReport = new HitStabilityReport();
                                    mockReport.IsStable = IsStableOnNormal(resolutionDirection);
                                    resolutionDirection = GetObstructionNormal(resolutionDirection, mockReport);

                                    // Solve overlap
                                    Vector3 resolutionMovement =
                                        resolutionDirection * (resolutionDistance + CollisionOffset);
                                    _transientPosition += resolutionMovement;

                                    // Remember overlaps
                                    if (_overlapsCount < _overlaps.Length)
                                    {
                                        _overlaps[_overlapsCount] = new OverlapResult(resolutionDirection,
                                            _internalProbedColliders[i]);
                                        _overlapsCount++;
                                    }

                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        overlapSolved = true;
                    }

                    iterationsMade++;
                }

                #endregion
            }

            #region Ground Probing and Snapping

            // Handle ungrounding
            if (_solveGrounding)
            {
                if (MustUnground())
                {
                    _transientPosition += _characterUp * (MinimumGroundProbingDistance * 1.5f);
                }
                else
                {
                    // Choose the appropriate ground probing distance
                    float selectedGroundProbingDistance = MinimumGroundProbingDistance;
                    if (!LastGroundingStatus.SnappingPrevented &&
                        (LastGroundingStatus.IsStableOnGround || LastMovementIterationFoundAnyGround))
                    {
                        if (StepHandling != StepHandlingMethod.None)
                        {
                            selectedGroundProbingDistance = Mathf.Max(CapsuleRadius, MaxStepHeight);
                        }
                        else
                        {
                            selectedGroundProbingDistance = CapsuleRadius;
                        }

                        selectedGroundProbingDistance += GroundDetectionExtraDistance;
                    }

                    ProbeGround(ref _transientPosition, _transientRotation, selectedGroundProbingDistance,
                        ref GroundingStatus);
                }
            }

            LastMovementIterationFoundAnyGround = false;

            if (_mustUngroundTimeCounter > 0f)
            {
                _mustUngroundTimeCounter -= deltaTime;
            }

            _mustUnground = false;

            #endregion

            if (_solveGrounding)
            {
                CharacterController.PostGroundingUpdate(deltaTime);
            }

            if (InteractiveRigidbodyHandling)
            {
                #region Interactive Rigidbody Handling

                _lastAttachedRigidbody = _attachedRigidbody;
                if (AttachedRigidbodyOverride)
                {
                    _attachedRigidbody = AttachedRigidbodyOverride;
                }
                else
                {
                    // Detect interactive rigidbodies from grounding
                    if (GroundingStatus.IsStableOnGround && GroundingStatus.GroundCollider.attachedRigidbody)
                    {
                        Rigidbody interactiveRigidbody = GetInteractiveRigidbody(GroundingStatus.GroundCollider);
                        if (interactiveRigidbody)
                        {
                            _attachedRigidbody = interactiveRigidbody;
                        }
                    }
                    else
                    {
                        _attachedRigidbody = null;
                    }
                }

                Vector3 tmpVelocityFromCurrentAttachedRigidbody = Vector3.zero;
                if (_attachedRigidbody)
                {
                    tmpVelocityFromCurrentAttachedRigidbody =
                        GetVelocityFromRigidbodyMovement(_attachedRigidbody, _transientPosition, deltaTime);
                }

                // Conserve momentum when de-stabilized from an attached rigidbody
                if (PreserveAttachedRigidbodyMomentum && _lastAttachedRigidbody != null &&
                    _attachedRigidbody != _lastAttachedRigidbody)
                {
                    BaseVelocity += _attachedRigidbodyVelocity;
                    BaseVelocity -= tmpVelocityFromCurrentAttachedRigidbody;
                }

                // Process additionnal Velocity from attached rigidbody
                _attachedRigidbodyVelocity = _cachedZeroVector;
                if (_attachedRigidbody)
                {
                    _attachedRigidbodyVelocity = tmpVelocityFromCurrentAttachedRigidbody;

                    // Rotation from attached rigidbody
                    Vector3 newForward = Vector3
                        .ProjectOnPlane(
                            Quaternion.Euler(Mathf.Rad2Deg * _attachedRigidbody.angularVelocity * deltaTime) *
                            _characterForward, _characterUp).normalized;
                    TransientRotation = Quaternion.LookRotation(newForward, _characterUp);
                }

                // Cancel out horizontal velocity upon landing on an attached rigidbody
                if (GroundingStatus.GroundCollider &&
                    GroundingStatus.GroundCollider.attachedRigidbody &&
                    GroundingStatus.GroundCollider.attachedRigidbody == _attachedRigidbody &&
                    _attachedRigidbody != null &&
                    _lastAttachedRigidbody == null)
                {
                    BaseVelocity -= Vector3.ProjectOnPlane(_attachedRigidbodyVelocity, _characterUp);
                }

                // Movement from Attached Rigidbody
                if (_attachedRigidbodyVelocity.sqrMagnitude > 0f)
                {
                    _isMovingFromAttachedRigidbody = true;

                    if (_solveMovementCollisions)
                    {
                        // Perform the move from rgdbdy velocity
                        if (InternalCharacterMove(_attachedRigidbodyVelocity * deltaTime, deltaTime,
                            out _internalResultingMovementMagnitude, out _internalResultingMovementDirection))
                        {
                            _attachedRigidbodyVelocity =
                                (_internalResultingMovementDirection * _internalResultingMovementMagnitude) / deltaTime;
                        }
                        else
                        {
                            _attachedRigidbodyVelocity = Vector3.zero;
                        }
                    }
                    else
                    {
                        _transientPosition += _attachedRigidbodyVelocity * deltaTime;
                    }

                    _isMovingFromAttachedRigidbody = false;
                }

                #endregion
            }
        }

        /// <summary>
        /// Update phase 2 is meant to be called after physics movers have simulated their goal positions/rotations. 
        /// At the end of this, the TransientPosition/Rotation values will be up-to-date with where the motor should be at the end of its move. 
        /// It is responsible for:
        /// - Solving Rotation
        /// - Handle MoveRotation calls
        /// - Solving potential attached rigidbody overlaps
        /// - Solving Velocity
        /// - Applying planar constraint
        /// </summary>
        public void UpdatePhase2(float deltaTime)
        {
            // Handle rotation
            CharacterController.UpdateRotation(ref _transientRotation, deltaTime);
            TransientRotation = _transientRotation;

            // Handle move rotation
            if (_moveRotationDirty)
            {
                TransientRotation = _moveRotationTarget;
                _moveRotationDirty = false;
            }

            if (_solveMovementCollisions && InteractiveRigidbodyHandling)
            {
                if (InteractiveRigidbodyHandling)
                {
                    #region Solve potential attached rigidbody overlap

                    if (_attachedRigidbody)
                    {
                        float upwardsOffset = Capsule.radius;

                        RaycastHit closestHit;
                        if (CharacterGroundSweep(
                            _transientPosition + (_characterUp * upwardsOffset),
                            _transientRotation,
                            -_characterUp,
                            upwardsOffset,
                            out closestHit))
                        {
                            if (closestHit.collider.attachedRigidbody == _attachedRigidbody &&
                                IsStableOnNormal(closestHit.normal))
                            {
                                float distanceMovedUp = (upwardsOffset - closestHit.distance);
                                _transientPosition = _transientPosition + (_characterUp * distanceMovedUp) +
                                                     (_characterUp * CollisionOffset);
                            }
                        }
                    }

                    #endregion
                }

                if (InteractiveRigidbodyHandling)
                {
                    #region Resolve overlaps that could've been caused by rotation or physics movers simulation pushing the character

                    Vector3 resolutionDirection = _cachedWorldUp;
                    float resolutionDistance = 0f;
                    int iterationsMade = 0;
                    bool overlapSolved = false;
                    while (iterationsMade < MaxDiscreteCollisionIterations && !overlapSolved)
                    {
                        int nbOverlaps = CharacterCollisionsOverlap(_transientPosition, _transientRotation,
                            _internalProbedColliders);
                        if (nbOverlaps > 0)
                        {
                            for (int i = 0; i < nbOverlaps; i++)
                            {
                                // Process overlap
                                Transform overlappedTransform = _internalProbedColliders[i].GetComponent<Transform>();
                                if (Physics.ComputePenetration(
                                    Capsule,
                                    _transientPosition,
                                    _transientRotation,
                                    _internalProbedColliders[i],
                                    overlappedTransform.position,
                                    overlappedTransform.rotation,
                                    out resolutionDirection,
                                    out resolutionDistance))
                                {
                                    // Resolve along obstruction direction
                                    Vector3 originalResolutionDirection = resolutionDirection;
                                    HitStabilityReport mockReport = new HitStabilityReport();
                                    mockReport.IsStable = IsStableOnNormal(resolutionDirection);
                                    resolutionDirection = GetObstructionNormal(resolutionDirection, mockReport);

                                    // Solve overlap
                                    Vector3 resolutionMovement =
                                        resolutionDirection * (resolutionDistance + CollisionOffset);
                                    _transientPosition += resolutionMovement;

                                    // If interactiveRigidbody, register as rigidbody hit for velocity
                                    if (InteractiveRigidbodyHandling)
                                    {
                                        Rigidbody probedRigidbody =
                                            GetInteractiveRigidbody(_internalProbedColliders[i]);
                                        if (probedRigidbody != null)
                                        {
                                            HitStabilityReport tmpReport = new HitStabilityReport();
                                            tmpReport.IsStable = IsStableOnNormal(resolutionDirection);
                                            if (tmpReport.IsStable)
                                            {
                                                LastMovementIterationFoundAnyGround = tmpReport.IsStable;
                                            }

                                            if (probedRigidbody != _attachedRigidbody)
                                            {
                                                Vector3 characterCenter =
                                                    _transientPosition +
                                                    (_transientRotation * _characterTransformToCapsuleCenter);
                                                Vector3 estimatedCollisionPoint = _transientPosition;

                                                MeshCollider meshColl = _internalProbedColliders[i] as MeshCollider;
                                                if (!(meshColl && !meshColl.convex))
                                                {
                                                    Physics.ClosestPoint(characterCenter, _internalProbedColliders[i],
                                                        overlappedTransform.position, overlappedTransform.rotation);
                                                }

                                                StoreRigidbodyHit(
                                                    probedRigidbody,
                                                    Velocity,
                                                    estimatedCollisionPoint,
                                                    resolutionDirection,
                                                    tmpReport);
                                            }
                                        }
                                    }

                                    // Remember overlaps
                                    if (_overlapsCount < _overlaps.Length)
                                    {
                                        _overlaps[_overlapsCount] = new OverlapResult(resolutionDirection,
                                            _internalProbedColliders[i]);
                                        _overlapsCount++;
                                    }

                                    break;
                                }
                            }
                        }
                        else
                        {
                            overlapSolved = true;
                        }

                        iterationsMade++;
                    }

                    #endregion
                }
            }

            // Handle velocity
            CharacterController.UpdateVelocity(ref BaseVelocity, deltaTime);

            //this.CharacterController.UpdateVelocity(ref BaseVelocity, deltaTime);
            if (BaseVelocity.magnitude < MinVelocityMagnitude)
            {
                BaseVelocity = Vector3.zero;
            }

            #region Calculate Character movement from base velocity

            // Perform the move from base velocity
            if (BaseVelocity.sqrMagnitude > 0f)
            {
                if (_solveMovementCollisions)
                {
                    if (InternalCharacterMove(BaseVelocity * deltaTime, deltaTime,
                        out _internalResultingMovementMagnitude, out _internalResultingMovementDirection))
                    {
                        BaseVelocity = (_internalResultingMovementDirection * _internalResultingMovementMagnitude) /
                                       deltaTime;
                    }
                    else
                    {
                        BaseVelocity = Vector3.zero;
                    }
                }
                else
                {
                    _transientPosition += BaseVelocity * deltaTime;
                }
            }

            // Process rigidbody hits/overlaps to affect velocity
            if (InteractiveRigidbodyHandling)
            {
                ProcessVelocityForRigidbodyHits(ref BaseVelocity, deltaTime);
            }

            #endregion

            // Handle planar constraint
            if (HasPlanarConstraint)
            {
                _transientPosition = _initialSimulationPosition +
                                     Vector3.ProjectOnPlane(_transientPosition - _initialSimulationPosition,
                                         PlanarConstraintAxis.normalized);
            }

            // Discrete collision detection
            if (DiscreteCollisionEvents)
            {
                int nbOverlaps = CharacterCollisionsOverlap(_transientPosition, _transientRotation,
                    _internalProbedColliders, CollisionOffset * 2f);
                for (int i = 0; i < nbOverlaps; i++)
                {
                    CharacterController.OnDiscreteCollisionDetected(_internalProbedColliders[i]);
                }
            }

            CharacterController.AfterCharacterUpdate(deltaTime);
        }

        /// <summary>
        /// Determines if motor can be considered stable on given slope normal
        /// </summary>
        private bool IsStableOnNormal(Vector3 normal)
        {
            return Vector3.Angle(_characterUp, normal) <= MaxStableSlopeAngle;
        }

        /// <summary>
        /// Determines if motor can be considered stable on given slope normal
        /// </summary>
        private bool IsStableWithSpecialCases(ref HitStabilityReport stabilityReport, Vector3 velocity)
        {
            if (LedgeAndDenivelationHandling)
            {
                if (stabilityReport.LedgeDetected)
                {
                    if (stabilityReport.IsMovingTowardsEmptySideOfLedge)
                    {
                        // Max snap vel
                        if (velocity.magnitude >= MaxVelocityForLedgeSnap)
                        {
                            return false;
                        }

                        // Distance from ledge
                        if (stabilityReport.IsOnEmptySideOfLedge &&
                            stabilityReport.DistanceFromLedge > MaxStableDistanceFromLedge)
                        {
                            return false;
                        }
                    }
                }

                // "Launching" off of slopes of a certain denivelation angle
                if (LastGroundingStatus.FoundAnyGround && stabilityReport.InnerNormal.sqrMagnitude != 0f &&
                    stabilityReport.OuterNormal.sqrMagnitude != 0f)
                {
                    float denivelationAngle = Vector3.Angle(stabilityReport.InnerNormal, stabilityReport.OuterNormal);
                    if (denivelationAngle > MaxStableDenivelationAngle)
                    {
                        return false;
                    }
                    else
                    {
                        denivelationAngle = Vector3.Angle(LastGroundingStatus.InnerGroundNormal,
                            stabilityReport.OuterNormal);
                        if (denivelationAngle > MaxStableDenivelationAngle)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Probes for valid ground and midifies the input transientPosition if ground snapping occurs
        /// </summary>
        public void ProbeGround(ref Vector3 probingPosition, Quaternion atRotation, float probingDistance,
            ref CharacterGroundingReport groundingReport)
        {
            if (probingDistance < MinimumGroundProbingDistance)
            {
                probingDistance = MinimumGroundProbingDistance;
            }

            int groundSweepsMade = 0;
            RaycastHit groundSweepHit = new RaycastHit();
            bool groundSweepingIsOver = false;
            Vector3 groundSweepPosition = probingPosition;
            Vector3 groundSweepDirection = (atRotation * -_cachedWorldUp);
            float groundProbeDistanceRemaining = probingDistance;
            while (groundProbeDistanceRemaining > 0 && (groundSweepsMade <= MaxGroundingSweepIterations) &&
                   !groundSweepingIsOver)
            {
                // Sweep for ground detection
                if (CharacterGroundSweep(
                    groundSweepPosition, // position
                    atRotation, // rotation
                    groundSweepDirection, // direction
                    groundProbeDistanceRemaining, // distance
                    out groundSweepHit)) // hit
                {
                    Vector3 targetPosition = groundSweepPosition + (groundSweepDirection * groundSweepHit.distance);
                    HitStabilityReport groundHitStabilityReport = new HitStabilityReport();
                    EvaluateHitStability(groundSweepHit.collider, groundSweepHit.normal, groundSweepHit.point,
                        targetPosition, _transientRotation, BaseVelocity, ref groundHitStabilityReport);

                    groundingReport.FoundAnyGround = true;
                    groundingReport.GroundNormal = groundSweepHit.normal;
                    groundingReport.InnerGroundNormal = groundHitStabilityReport.InnerNormal;
                    groundingReport.OuterGroundNormal = groundHitStabilityReport.OuterNormal;
                    groundingReport.GroundCollider = groundSweepHit.collider;
                    groundingReport.GroundPoint = groundSweepHit.point;
                    groundingReport.SnappingPrevented = false;

                    // Found stable ground
                    if (groundHitStabilityReport.IsStable)
                    {
                        // Find all scenarios where ground snapping should be canceled
                        groundingReport.SnappingPrevented =
                            !IsStableWithSpecialCases(ref groundHitStabilityReport, BaseVelocity);

                        groundingReport.IsStableOnGround = true;

                        // Ground snapping
                        if (!groundingReport.SnappingPrevented)
                        {
                            targetPosition += (-groundSweepDirection * CollisionOffset);
                            probingPosition = targetPosition;
                        }

                        CharacterController.OnGroundHit(groundSweepHit.collider, groundSweepHit.normal,
                            groundSweepHit.point, ref groundHitStabilityReport);
                        groundSweepingIsOver = true;
                    }
                    else
                    {
                        // Calculate movement from this iteration and advance position
                        Vector3 sweepMovement = (groundSweepDirection * groundSweepHit.distance) +
                                                ((atRotation * _cachedWorldUp) * Mathf.Max(CollisionOffset,
                                                    groundSweepHit.distance));
                        groundSweepPosition = groundSweepPosition + sweepMovement;

                        // Set remaining distance
                        groundProbeDistanceRemaining = Mathf.Min(GroundProbeReboundDistance,
                            Mathf.Max(groundProbeDistanceRemaining - sweepMovement.magnitude, 0f));

                        // Reorient direction
                        groundSweepDirection = Vector3.ProjectOnPlane(groundSweepDirection, groundSweepHit.normal)
                            .normalized;
                    }
                }
                else
                {
                    groundSweepingIsOver = true;
                }

                groundSweepsMade++;
            }
        }

        /// <summary>
        /// Forces the character to unground itself on its next grounding update
        /// </summary>
        public void ForceUnground(float time = 0.1f)
        {
            _mustUnground = true;
            _mustUngroundTimeCounter = time;
        }

        public bool MustUnground()
        {
            return _mustUnground || _mustUngroundTimeCounter > 0f;
        }

        /// <summary>
        /// Returns the direction adjusted to be tangent to a specified surface normal relatively to the character's up direction.
        /// Useful for reorienting a direction on a slope without any lateral deviation in trajectory
        /// </summary>
        public Vector3 GetDirectionTangentToSurface(Vector3 direction, Vector3 surfaceNormal)
        {
            Vector3 directionRight = Vector3.Cross(direction, _characterUp);
            return Vector3.Cross(surfaceNormal, directionRight).normalized;
        }

        /// <summary>
        /// Moves the character's position by given movement while taking into account all physics simulation, step-handling and 
        /// velocity projection rules that affect the character motor
        /// </summary>
        /// <returns> Returns false if movement could not be solved until the end </returns>
        private bool InternalCharacterMove(Vector3 movement, float deltaTime, out float resultingMovementMagnitude,
            out Vector3 resultingMovementDirection)
        {
            _rigidbodiesPushedCount = 0;
            bool wasCompleted = true;
            float remainingMovementMagnitude = movement.magnitude;
            Vector3 remainingMovementDirection = movement.normalized;
            resultingMovementDirection = remainingMovementDirection;
            resultingMovementMagnitude = remainingMovementMagnitude;
            int sweepsMade = 0;
            bool hitSomethingThisSweepIteration = true;
            Vector3 tmpMovedPosition = _transientPosition;
            Vector3 originalMoveDirection = remainingMovementDirection;
            Vector3 previousMovementHitNormal = _cachedZeroVector;
            MovementSweepState sweepState = MovementSweepState.Initial;

            // Project movement against current overlaps
            for (int i = 0; i < _overlapsCount; i++)
            {
                Vector3 overlapNormal = _overlaps[i].Normal;
                if (Vector3.Dot(remainingMovementDirection, overlapNormal) < 0f)
                {
                    InternalHandleMovementProjection(
                        IsStableOnNormal(overlapNormal) && !MustUnground(),
                        overlapNormal,
                        overlapNormal,
                        originalMoveDirection,
                        ref sweepState,
                        ref previousMovementHitNormal,
                        ref resultingMovementMagnitude,
                        ref remainingMovementDirection,
                        ref remainingMovementMagnitude);
                }
            }

            // Sweep the desired movement to detect collisions
            while (remainingMovementMagnitude > 0f &&
                   (sweepsMade <= MaxMovementSweepIterations) &&
                   hitSomethingThisSweepIteration)
            {
                if (CharacterCollisionsSweep(
                        tmpMovedPosition, // position
                        _transientRotation, // rotation
                        remainingMovementDirection, // direction
                        remainingMovementMagnitude + CollisionOffset, // distance
                        out RaycastHit closestSweepHit, // closest hit
                        _internalCharacterHits) // all hits
                    > 0)
                {
                    // Calculate movement from this iteration
                    Vector3 targetPositionAfterSweep =
                        tmpMovedPosition + (remainingMovementDirection * closestSweepHit.distance) +
                        (closestSweepHit.normal * CollisionOffset);
                    Vector3 sweepMovement = targetPositionAfterSweep - tmpMovedPosition;
                    float sweepMovementMagnitude = sweepMovement.magnitude;

                    Vector3 movementVelocity = default;
                    if (deltaTime > 0f)
                    {
                        movementVelocity = sweepMovement / deltaTime;
                    }

                    // Evaluate if hit is stable
                    HitStabilityReport moveHitStabilityReport = new HitStabilityReport();
                    EvaluateHitStability(closestSweepHit.collider, closestSweepHit.normal, closestSweepHit.point,
                        targetPositionAfterSweep, _transientRotation, movementVelocity, ref moveHitStabilityReport);

                    // Handle stepping up steps points higher than bottom capsule radius
                    bool foundValidStepHit = false;
                    if (_solveGrounding && StepHandling != StepHandlingMethod.None &&
                        moveHitStabilityReport.ValidStepDetected)
                    {
                        float obstructionCorrelation = Mathf.Abs(Vector3.Dot(closestSweepHit.normal, _characterUp));
                        if (obstructionCorrelation <= CorrelationForVerticalObstruction)
                        {
                            Vector3 stepForwardDirection =
                                Vector3.ProjectOnPlane(-closestSweepHit.normal, _characterUp).normalized;
                            Vector3 stepCastStartPoint =
                                (targetPositionAfterSweep + (stepForwardDirection * SteppingForwardDistance)) +
                                (_characterUp * MaxStepHeight);

                            // Cast downward from the top of the stepping height
                            int nbStepHits = CharacterCollisionsSweep(
                                stepCastStartPoint, // position
                                _transientRotation, // rotation
                                -_characterUp, // direction
                                MaxStepHeight, // distance
                                out RaycastHit closestStepHit, // closest hit
                                _internalCharacterHits,
                                0f,
                                true); // all hits 

                            // Check for hit corresponding to stepped collider
                            for (int i = 0; i < nbStepHits; i++)
                            {
                                if (_internalCharacterHits[i].collider == moveHitStabilityReport.SteppedCollider)
                                {
                                    Vector3 endStepPosition =
                                        stepCastStartPoint +
                                        (-_characterUp * (_internalCharacterHits[i].distance - CollisionOffset));
                                    tmpMovedPosition = endStepPosition;
                                    foundValidStepHit = true;

                                    // Consume magnitude for step
                                    remainingMovementMagnitude =
                                        Mathf.Max(remainingMovementMagnitude - sweepMovementMagnitude, 0f);

                                    // Project movement on ground normal at step
                                    Vector3 tmpRemainingMovement =
                                        remainingMovementMagnitude * remainingMovementDirection;
                                    tmpRemainingMovement = Vector3.ProjectOnPlane(tmpRemainingMovement, CharacterUp);
                                    remainingMovementMagnitude = tmpRemainingMovement.magnitude;
                                    remainingMovementDirection = tmpRemainingMovement.normalized;

                                    resultingMovementMagnitude = remainingMovementMagnitude;

                                    break;
                                }
                            }
                        }
                    }

                    // Handle movement solving
                    if (!foundValidStepHit)
                    {
                        Collider closestSweepCollider = closestSweepHit.collider;
                        Vector3 closestSweepPoint = closestSweepHit.point;
                        Vector3 closestSweepNormal = closestSweepHit.normal;

                        // Apply the actual movement
                        tmpMovedPosition = targetPositionAfterSweep;
                        remainingMovementMagnitude = Mathf.Max(remainingMovementMagnitude - sweepMovementMagnitude, 0f);

                        // Movement hit callback
                        CharacterController.OnMovementHit(closestSweepCollider, closestSweepNormal, closestSweepPoint,
                            ref moveHitStabilityReport);
                        Vector3 obstructionNormal = GetObstructionNormal(closestSweepNormal, moveHitStabilityReport);

                        // Handle remembering rigidbody hits
                        if (InteractiveRigidbodyHandling && closestSweepCollider.attachedRigidbody)
                        {
                            StoreRigidbodyHit(
                                closestSweepCollider.attachedRigidbody,
                                (remainingMovementDirection * resultingMovementMagnitude) / deltaTime,
                                closestSweepPoint,
                                obstructionNormal,
                                moveHitStabilityReport);
                        }

                        // Project movement
                        InternalHandleMovementProjection(
                            moveHitStabilityReport.IsStable && !MustUnground(),
                            closestSweepNormal,
                            obstructionNormal,
                            originalMoveDirection,
                            ref sweepState,
                            ref previousMovementHitNormal,
                            ref resultingMovementMagnitude,
                            ref remainingMovementDirection,
                            ref remainingMovementMagnitude);
                    }
                }
                // If we hit nothing...
                else
                {
                    hitSomethingThisSweepIteration = false;
                }

                // Safety for exceeding max sweeps allowed
                sweepsMade++;
                if (sweepsMade > MaxMovementSweepIterations)
                {
                    remainingMovementMagnitude = 0;
                    wasCompleted = false;
                }
            }

            // Move position for the remainder of the movement
            _transientPosition = tmpMovedPosition + (remainingMovementDirection * remainingMovementMagnitude);
            resultingMovementDirection = remainingMovementDirection;

            return wasCompleted;
        }

        /// <summary>
        /// Gets the effective normal for movement obstruction depending on current grounding status
        /// </summary>
        private Vector3 GetObstructionNormal(Vector3 hitNormal, HitStabilityReport hitStabilityReport)
        {
            // Find hit/obstruction/offset normal
            Vector3 obstructionNormal = hitNormal;
            if (GroundingStatus.IsStableOnGround && !MustUnground() && !hitStabilityReport.IsStable)
            {
                Vector3 obstructionLeftAlongGround =
                    Vector3.Cross(GroundingStatus.GroundNormal, obstructionNormal).normalized;
                obstructionNormal = Vector3.Cross(obstructionLeftAlongGround, _characterUp).normalized;
            }

            // Catch cases where cross product between parallel normals returned 0
            if (obstructionNormal.sqrMagnitude == 0f)
            {
                obstructionNormal = hitNormal;
            }

            return obstructionNormal;
        }

        /// <summary>
        /// Remembers a rigidbody hit for processing later
        /// </summary>
        private void StoreRigidbodyHit(Rigidbody hitRigidbody, Vector3 hitVelocity, Vector3 hitPoint,
            Vector3 obstructionNormal, HitStabilityReport hitStabilityReport)
        {
            if (_rigidbodyProjectionHitCount < _internalRigidbodyProjectionHits.Length)
            {
                if (!hitRigidbody.GetComponent<KinematicCharacterMotor>())
                {
                    RigidbodyProjectionHit rph = new RigidbodyProjectionHit();
                    rph.Rigidbody = hitRigidbody;
                    rph.HitPoint = hitPoint;
                    rph.EffectiveHitNormal = obstructionNormal;
                    rph.HitVelocity = hitVelocity;
                    rph.StableOnHit = hitStabilityReport.IsStable;

                    _internalRigidbodyProjectionHits[_rigidbodyProjectionHitCount] = rph;
                    _rigidbodyProjectionHitCount++;
                }
            }
        }

        /// <summary>
        /// Processes movement projection upon detecting a hit
        /// </summary>
        private void InternalHandleMovementProjection(bool stableOnHit, Vector3 hitNormal, Vector3 obstructionNormal,
            Vector3 originalMoveDirection, ref MovementSweepState sweepState,
            ref Vector3 previousObstructionNormal, ref float resultingMovementMagnitude,
            ref Vector3 remainingMovementDirection, ref float remainingMovementMagnitude)
        {
            if (remainingMovementMagnitude <= 0f)
            {
                return;
            }

            Vector3 remainingMovement = originalMoveDirection * remainingMovementMagnitude;
            float remainingMagnitudeBeforeProj = remainingMovementMagnitude;
            if (stableOnHit)
            {
                LastMovementIterationFoundAnyGround = true;
            }

            // Blocking-corner handling
            if (sweepState == MovementSweepState.FoundBlockingCrease)
            {
                remainingMovementMagnitude = 0f;
                resultingMovementMagnitude = 0f;

                sweepState = MovementSweepState.FoundBlockingCorner;
            }
            // Handle projection
            else
            {
                HandleMovementProjection(ref remainingMovement, obstructionNormal, stableOnHit);

                remainingMovementMagnitude = remainingMovement.magnitude;
                remainingMovementDirection = remainingMovement.normalized;
                resultingMovementMagnitude = (remainingMovementMagnitude / remainingMagnitudeBeforeProj) *
                                             resultingMovementMagnitude;

                // Blocking corner handling
                if (sweepState == MovementSweepState.Initial)
                {
                    sweepState = MovementSweepState.AfterFirstHit;
                }
                else if (sweepState == MovementSweepState.AfterFirstHit)
                {
                    // Detect blocking corners
                    if (Vector3.Dot(previousObstructionNormal, remainingMovementDirection) < 0f)
                    {
                        Vector3 cornerVector = Vector3.Cross(previousObstructionNormal, obstructionNormal).normalized;
                        remainingMovement = Vector3.Project(remainingMovement, cornerVector);
                        remainingMovementMagnitude = remainingMovement.magnitude;
                        remainingMovementDirection = remainingMovement.normalized;
                        resultingMovementMagnitude = (remainingMovementMagnitude / remainingMagnitudeBeforeProj) *
                                                     resultingMovementMagnitude;

                        sweepState = MovementSweepState.FoundBlockingCrease;
                    }
                }
            }

            previousObstructionNormal = obstructionNormal;
        }

        /// <summary>
        /// Allows you to override the way velocity is projected on an obstruction
        /// </summary>
        public virtual void HandleMovementProjection(ref Vector3 movement, Vector3 obstructionNormal, bool stableOnHit)
        {
            if (GroundingStatus.IsStableOnGround && !MustUnground())
            {
                // On stable slopes, simply reorient the movement without any loss
                if (stableOnHit)
                {
                    movement = GetDirectionTangentToSurface(movement, obstructionNormal) * movement.magnitude;
                }
                // On blocking hits, project the movement on the obstruction while following the grounding plane
                else
                {
                    Vector3 obstructionRightAlongGround =
                        Vector3.Cross(obstructionNormal, GroundingStatus.GroundNormal).normalized;
                    Vector3 obstructionUpAlongGround =
                        Vector3.Cross(obstructionRightAlongGround, obstructionNormal).normalized;
                    movement = GetDirectionTangentToSurface(movement, obstructionUpAlongGround) * movement.magnitude;
                    movement = Vector3.ProjectOnPlane(movement, obstructionNormal);
                }
            }
            else
            {
                if (stableOnHit)
                {
                    // Handle stable landing
                    movement = Vector3.ProjectOnPlane(movement, CharacterUp);
                    movement = GetDirectionTangentToSurface(movement, obstructionNormal) * movement.magnitude;
                }
                // Handle generic obstruction
                else
                {
                    movement = Vector3.ProjectOnPlane(movement, obstructionNormal);
                }
            }
        }

        /// <summary>
        /// Allows you to override the way hit rigidbodies are pushed / interacted with. 
        /// ProcessedVelocity is what must be modified if this interaction affects the character's velocity.
        /// </summary>
        public virtual void HandleSimulatedRigidbodyInteraction(ref Vector3 processedVelocity,
            RigidbodyProjectionHit hit, float deltaTime)
        {
            float simulatedCharacterMass = 0.2f;

            // Handle pushing rigidbodies in SimulatedDynamic mode
            if (simulatedCharacterMass > 0f &&
                !hit.StableOnHit &&
                !hit.Rigidbody.isKinematic)
            {
                float massRatio = simulatedCharacterMass / hit.Rigidbody.mass;
                Vector3 effectiveHitRigidbodyVelocity =
                    GetVelocityFromRigidbodyMovement(hit.Rigidbody, hit.HitPoint, deltaTime);
                Vector3 relativeVelocity = Vector3.Project(hit.HitVelocity, hit.EffectiveHitNormal) -
                                           effectiveHitRigidbodyVelocity;

                hit.Rigidbody.AddForceAtPosition(massRatio * relativeVelocity, hit.HitPoint, ForceMode.VelocityChange);
            }

            // Compensate character's own velocity against the moving rigidbodies
            if (!hit.StableOnHit)
            {
                Vector3 effectiveRigidbodyVelocity =
                    GetVelocityFromRigidbodyMovement(hit.Rigidbody, hit.HitPoint, deltaTime);
                Vector3 projRigidbodyVelocity = Vector3.Project(effectiveRigidbodyVelocity, hit.EffectiveHitNormal);
                Vector3 projCharacterVelocity = Vector3.Project(processedVelocity, hit.EffectiveHitNormal);
                processedVelocity += projRigidbodyVelocity - projCharacterVelocity;
            }
        }

        /// <summary>
        /// Takes into account rigidbody hits for adding to the velocity
        /// </summary>
        private void ProcessVelocityForRigidbodyHits(ref Vector3 processedVelocity, float deltaTime)
        {
            for (int i = 0; i < _rigidbodyProjectionHitCount; i++)
            {
                if (_internalRigidbodyProjectionHits[i].Rigidbody)
                {
                    // Keep track of the unique rigidbodies we pushed this update, to avoid doubling their effect
                    bool alreadyPushedThisRigidbody = false;
                    for (int j = 0; j < _rigidbodiesPushedCount; j++)
                    {
                        if (_rigidbodiesPushedThisMove[j] == _internalRigidbodyProjectionHits[j].Rigidbody)
                        {
                            alreadyPushedThisRigidbody = true;
                            break;
                        }
                    }

                    if (!alreadyPushedThisRigidbody &&
                        _internalRigidbodyProjectionHits[i].Rigidbody != _attachedRigidbody)
                    {
                        if (_rigidbodiesPushedCount < _rigidbodiesPushedThisMove.Length)
                        {
                            // Remember we hit this rigidbody
                            _rigidbodiesPushedThisMove[_rigidbodiesPushedCount] =
                                _internalRigidbodyProjectionHits[i].Rigidbody;
                            _rigidbodiesPushedCount++;

                            if (RigidbodyInteractionType == RigidbodyInteractionType.SimulatedDynamic)
                            {
                                HandleSimulatedRigidbodyInteraction(ref processedVelocity,
                                    _internalRigidbodyProjectionHits[i], deltaTime);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines if the input collider is valid for collision processing
        /// </summary>
        /// <returns> Returns true if the collider is valid </returns>
        private bool CheckIfColliderValidForCollisions(Collider coll)
        {
            // Ignore self
            if (coll == Capsule)
            {
                return false;
            }

            if (!InternalIsColliderValidForCollisions(coll))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if the input collider is valid for collision processing
        /// </summary>
        private bool InternalIsColliderValidForCollisions(Collider coll)
        {
            Rigidbody colliderAttachedRigidbody = coll.attachedRigidbody;
            if (colliderAttachedRigidbody)
            {
                bool isRigidbodyKinematic = colliderAttachedRigidbody.isKinematic;

                // If movement is made from AttachedRigidbody, ignore the AttachedRigidbody
                if (_isMovingFromAttachedRigidbody &&
                    (!isRigidbodyKinematic || colliderAttachedRigidbody == _attachedRigidbody))
                {
                    return false;
                }

                // don't collide with dynamic rigidbodies if our RigidbodyInteractionType is kinematic
                if (RigidbodyInteractionType == RigidbodyInteractionType.Kinematic && !isRigidbodyKinematic)
                {
                    return false;
                }
            }

            // Custom checks
            bool colliderValid = CharacterController.IsColliderValidForCollisions(coll);
            if (!colliderValid)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if the motor is considered stable on a given hit
        /// </summary>
        public void EvaluateHitStability(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            Vector3 atCharacterPosition, Quaternion atCharacterRotation, Vector3 withCharacterVelocity,
            ref HitStabilityReport stabilityReport)
        {
            if (!_solveGrounding)
            {
                stabilityReport.IsStable = false;
                return;
            }

            Vector3 atCharacterUp = atCharacterRotation * _cachedWorldUp;
            Vector3 innerHitDirection = Vector3.ProjectOnPlane(hitNormal, atCharacterUp).normalized;

            stabilityReport.IsStable = this.IsStableOnNormal(hitNormal);
            stabilityReport.InnerNormal = hitNormal;
            stabilityReport.OuterNormal = hitNormal;

            // Ledge handling
            if (LedgeAndDenivelationHandling)
            {
                float ledgeCheckHeight = MinDistanceForLedge;
                if (StepHandling != StepHandlingMethod.None)
                {
                    ledgeCheckHeight = MaxStepHeight;
                }

                bool isStableLedgeInner = false;
                bool isStableLedgeOuter = false;

                if (CharacterCollisionsRaycast(
                    hitPoint + (atCharacterUp * SecondaryProbesVertical) +
                    (innerHitDirection * SecondaryProbesHorizontal),
                    -atCharacterUp,
                    ledgeCheckHeight + SecondaryProbesVertical,
                    out RaycastHit innerLedgeHit,
                    _internalCharacterHits) > 0)
                {
                    Vector3 innerLedgeNormal = innerLedgeHit.normal;
                    stabilityReport.InnerNormal = innerLedgeNormal;
                    isStableLedgeInner = IsStableOnNormal(innerLedgeNormal);
                }

                if (CharacterCollisionsRaycast(
                    hitPoint + (atCharacterUp * SecondaryProbesVertical) +
                    (-innerHitDirection * SecondaryProbesHorizontal),
                    -atCharacterUp,
                    ledgeCheckHeight + SecondaryProbesVertical,
                    out RaycastHit outerLedgeHit,
                    _internalCharacterHits) > 0)
                {
                    Vector3 outerLedgeNormal = outerLedgeHit.normal;
                    stabilityReport.OuterNormal = outerLedgeNormal;
                    isStableLedgeOuter = IsStableOnNormal(outerLedgeNormal);
                }

                stabilityReport.LedgeDetected = (isStableLedgeInner != isStableLedgeOuter);
                if (stabilityReport.LedgeDetected)
                {
                    stabilityReport.IsOnEmptySideOfLedge = isStableLedgeOuter && !isStableLedgeInner;
                    stabilityReport.LedgeGroundNormal =
                        isStableLedgeOuter ? stabilityReport.OuterNormal : stabilityReport.InnerNormal;
                    stabilityReport.LedgeRightDirection =
                        Vector3.Cross(hitNormal, stabilityReport.OuterNormal).normalized;
                    stabilityReport.LedgeFacingDirection = Vector3
                        .Cross(stabilityReport.LedgeGroundNormal, stabilityReport.LedgeRightDirection).normalized;
                    stabilityReport.DistanceFromLedge = Vector3
                        .ProjectOnPlane(
                            (hitPoint - (atCharacterPosition +
                                         (atCharacterRotation * _characterTransformToCapsuleBottom))), atCharacterUp)
                        .magnitude;
                    stabilityReport.IsMovingTowardsEmptySideOfLedge = Vector3.Dot(withCharacterVelocity,
                        Vector3.ProjectOnPlane(
                            stabilityReport.LedgeFacingDirection,
                            CharacterUp)) > 0f;
                }

                if (stabilityReport.IsStable)
                {
                    stabilityReport.IsStable = IsStableWithSpecialCases(ref stabilityReport, withCharacterVelocity);
                }
            }

            // Step handling
            if (StepHandling != StepHandlingMethod.None && !stabilityReport.IsStable)
            {
                // Stepping not supported on dynamic rigidbodies
                Rigidbody hitRigidbody = hitCollider.attachedRigidbody;
                if (!(hitRigidbody && !hitRigidbody.isKinematic))
                {
                    DetectSteps(atCharacterPosition, atCharacterRotation, hitPoint, innerHitDirection,
                        ref stabilityReport);

                    if (stabilityReport.ValidStepDetected)
                    {
                        stabilityReport.IsStable = true;
                    }
                }
            }

            CharacterController.ProcessHitStabilityReport(hitCollider, hitNormal, hitPoint, atCharacterPosition,
                atCharacterRotation, ref stabilityReport);
        }

        private void DetectSteps(Vector3 characterPosition, Quaternion characterRotation, Vector3 hitPoint,
            Vector3 innerHitDirection, ref HitStabilityReport stabilityReport)
        {
            int nbStepHits = 0;
            Collider tmpCollider;
            RaycastHit outerStepHit;
            Vector3 characterUp = characterRotation * _cachedWorldUp;
            Vector3 verticalCharToHit = Vector3.Project((hitPoint - characterPosition), characterUp);
            Vector3 stepCheckStartPos = (hitPoint - verticalCharToHit) + (characterUp * MaxStepHeight);

            // Do outer step check with capsule cast on hit point
            nbStepHits = CharacterCollisionsSweep(
                stepCheckStartPos,
                characterRotation,
                -characterUp,
                MaxStepHeight + CollisionOffset,
                out outerStepHit,
                _internalCharacterHits,
                0f,
                true);

            // Check for overlaps and obstructions at the hit position
            if (CheckStepValidity(nbStepHits, characterPosition, characterRotation, innerHitDirection,
                stepCheckStartPos, out tmpCollider))
            {
                stabilityReport.ValidStepDetected = true;
                stabilityReport.SteppedCollider = tmpCollider;
            }

            if (StepHandling == StepHandlingMethod.Extra && !stabilityReport.ValidStepDetected)
            {
                // Do min reach step check with capsule cast on hit point
                stepCheckStartPos = characterPosition + (characterUp * MaxStepHeight) +
                                    (-innerHitDirection * MinRequiredStepDepth);
                nbStepHits = CharacterCollisionsSweep(
                    stepCheckStartPos,
                    characterRotation,
                    -characterUp,
                    MaxStepHeight - CollisionOffset,
                    out outerStepHit,
                    _internalCharacterHits,
                    0f,
                    true);

                // Check for overlaps and obstructions at the hit position
                if (CheckStepValidity(nbStepHits, characterPosition, characterRotation, innerHitDirection,
                    stepCheckStartPos, out tmpCollider))
                {
                    stabilityReport.ValidStepDetected = true;
                    stabilityReport.SteppedCollider = tmpCollider;
                }
            }
        }

        private bool CheckStepValidity(int nbStepHits, Vector3 characterPosition, Quaternion characterRotation,
            Vector3 innerHitDirection, Vector3 stepCheckStartPos, out Collider hitCollider)
        {
            hitCollider = null;
            Vector3 characterUp = characterRotation * Vector3.up;

            // Find the farthest valid hit for stepping
            bool foundValidStepPosition = false;

            while (nbStepHits > 0 && !foundValidStepPosition)
            {
                // Get farthest hit among the remaining hits
                RaycastHit farthestHit = new RaycastHit();
                float farthestDistance = 0f;
                int farthestIndex = 0;
                for (int i = 0; i < nbStepHits; i++)
                {
                    float hitDistance = _internalCharacterHits[i].distance;
                    if (hitDistance > farthestDistance)
                    {
                        farthestDistance = hitDistance;
                        farthestHit = _internalCharacterHits[i];
                        farthestIndex = i;
                    }
                }

                Vector3 characterBottom = characterPosition + (characterRotation * _characterTransformToCapsuleBottom);
                float sqrHitHeight = Vector3.Project(farthestHit.point - characterBottom, characterUp).sqrMagnitude;

                Vector3 characterPositionAtHit =
                    stepCheckStartPos + (-characterUp * (farthestHit.distance - CollisionOffset));

                int atStepOverlaps = CharacterCollisionsOverlap(characterPositionAtHit, characterRotation,
                    _internalProbedColliders);
                if (atStepOverlaps <= 0)
                {
                    // Check for outer hit slope normal stability at the step position
                    if (CharacterCollisionsRaycast(
                        farthestHit.point + (characterUp * SecondaryProbesVertical) +
                        (-innerHitDirection * SecondaryProbesHorizontal),
                        -characterUp,
                        MaxStepHeight + SecondaryProbesVertical,
                        out RaycastHit outerSlopeHit,
                        _internalCharacterHits,
                        true) > 0)
                    {
                        if (IsStableOnNormal(outerSlopeHit.normal))
                        {
                            // Cast upward to detect any obstructions to moving there
                            if (CharacterCollisionsSweep(
                                    characterPosition, // position
                                    characterRotation, // rotation
                                    characterUp, // direction
                                    MaxStepHeight - farthestHit.distance, // distance
                                    out RaycastHit tmpUpObstructionHit, // closest hit
                                    _internalCharacterHits) // all hits
                                <= 0)
                            {
                                // Do inner step check...
                                bool innerStepValid = false;
                                RaycastHit innerStepHit;

                                if (AllowSteppingWithoutStableGrounding)
                                {
                                    innerStepValid = true;
                                }
                                else
                                {
                                    // At the capsule center at the step height
                                    if (CharacterCollisionsRaycast(
                                        characterPosition +
                                        Vector3.Project((characterPositionAtHit - characterPosition), characterUp),
                                        -characterUp,
                                        MaxStepHeight,
                                        out innerStepHit,
                                        _internalCharacterHits,
                                        true) > 0)
                                    {
                                        if (IsStableOnNormal(innerStepHit.normal))
                                        {
                                            innerStepValid = true;
                                        }
                                    }
                                }

                                if (!innerStepValid)
                                {
                                    // At inner step of the step point
                                    if (CharacterCollisionsRaycast(
                                        farthestHit.point + (innerHitDirection * SecondaryProbesHorizontal),
                                        -characterUp,
                                        MaxStepHeight,
                                        out innerStepHit,
                                        _internalCharacterHits,
                                        true) > 0)
                                    {
                                        if (IsStableOnNormal(innerStepHit.normal))
                                        {
                                            innerStepValid = true;
                                        }
                                    }
                                }

                                // Final validation of step
                                if (innerStepValid)
                                {
                                    hitCollider = farthestHit.collider;
                                    foundValidStepPosition = true;
                                    return true;
                                }
                            }
                        }
                    }
                }

                // Discard hit if not valid step
                if (!foundValidStepPosition)
                {
                    nbStepHits--;
                    if (farthestIndex < nbStepHits)
                    {
                        _internalCharacterHits[farthestIndex] = _internalCharacterHits[nbStepHits];
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get true linear velocity (taking into account rotational velocity) on a given point of a rigidbody
        /// </summary>
        public Vector3 GetVelocityFromRigidbodyMovement(Rigidbody interactiveRigidbody, Vector3 atPoint,
            float deltaTime)
        {
            if (deltaTime > 0f)
            {
                Vector3 effectiveMoverVelocity = interactiveRigidbody.velocity;

                if (interactiveRigidbody.angularVelocity != Vector3.zero)
                {
                    Vector3 centerOfRotation = interactiveRigidbody.position + interactiveRigidbody.centerOfMass;

                    Vector3 centerOfRotationToPoint = atPoint - centerOfRotation;
                    Quaternion rotationFromInteractiveRigidbody =
                        Quaternion.Euler(Mathf.Rad2Deg * interactiveRigidbody.angularVelocity * deltaTime);
                    Vector3 finalPointPosition =
                        centerOfRotation + (rotationFromInteractiveRigidbody * centerOfRotationToPoint);
                    effectiveMoverVelocity += (finalPointPosition - atPoint) / deltaTime;
                }

                return effectiveMoverVelocity;
            }
            else
            {
                return Vector3.zero;
            }
        }

        /// <summary>
        /// Determines if a collider has an attached interactive rigidbody
        /// </summary>
        private Rigidbody GetInteractiveRigidbody(Collider onCollider)
        {
            Rigidbody colliderAttachedRigidbody = onCollider.attachedRigidbody;
            if (colliderAttachedRigidbody)
            {
                if (!colliderAttachedRigidbody.isKinematic)
                {
                    return colliderAttachedRigidbody;
                }
            }

            return null;
        }

        /// <summary>
        /// Calculates the velocity required to move the character to the target position over a specific deltaTime.
        /// Useful for when you wish to work with positions rather than velocities in the UpdateVelocity callback 
        /// </summary>
        public Vector3 GetVelocityForMovePosition(Vector3 fromPosition, Vector3 toPosition, float deltaTime)
        {
            if (deltaTime > 0)
            {
                return (toPosition - fromPosition) / deltaTime;
            }
            else
            {
                return Vector3.zero;
            }
        }

        /// <summary>
        /// Trims a vector to make it restricted against a plane
        /// </summary>
        private void RestrictVectorToPlane(ref Vector3 vector, Vector3 toPlane)
        {
            if (vector.x > 0 != toPlane.x > 0)
            {
                vector.x = 0;
            }

            if (vector.y > 0 != toPlane.y > 0)
            {
                vector.y = 0;
            }

            if (vector.z > 0 != toPlane.z > 0)
            {
                vector.z = 0;
            }
        }

        /// <summary>
        /// Detect if the character capsule is overlapping with anything collidable
        /// </summary>
        /// <returns> Returns number of overlaps </returns>
        public int CharacterCollisionsOverlap(Vector3 position, Quaternion rotation, Collider[] overlappedColliders,
            float inflate = 0f, bool acceptOnlyStableGroundLayer = false)
        {
            int queryLayers = CollidableLayers;
            if (acceptOnlyStableGroundLayer)
            {
                queryLayers = CollidableLayers & StableGroundLayers;
            }

            Vector3 bottom = position + (rotation * _characterTransformToCapsuleBottomHemi);
            Vector3 top = position + (rotation * _characterTransformToCapsuleTopHemi);
            if (inflate != 0f)
            {
                bottom += (rotation * Vector3.down * inflate);
                top += (rotation * Vector3.up * inflate);
            }

            int nbHits = 0;
            int nbUnfilteredHits = physics.OverlapCapsule(
                bottom,
                top,
                Capsule.radius + inflate,
                overlappedColliders,
                queryLayers,
                QueryTriggerInteraction.Ignore);

            // Filter out invalid colliders
            nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                if (!CheckIfColliderValidForCollisions(overlappedColliders[i]))
                {
                    nbHits--;
                    if (i < nbHits)
                    {
                        overlappedColliders[i] = overlappedColliders[nbHits];
                    }
                }
            }

            return nbHits;
        }

        /// <summary>
        /// Detect if the character capsule is overlapping with anything
        /// </summary>
        /// <returns> Returns number of overlaps </returns>
        public int CharacterOverlap(Vector3 position, Quaternion rotation, Collider[] overlappedColliders,
            LayerMask layers, QueryTriggerInteraction triggerInteraction, float inflate = 0f)
        {
            Vector3 bottom = position + (rotation * _characterTransformToCapsuleBottomHemi);
            Vector3 top = position + (rotation * _characterTransformToCapsuleTopHemi);
            if (inflate != 0f)
            {
                bottom += (rotation * Vector3.down * inflate);
                top += (rotation * Vector3.up * inflate);
            }

            int nbHits = 0;
            int nbUnfilteredHits = physics.OverlapCapsule(
                bottom,
                top,
                Capsule.radius + inflate,
                overlappedColliders,
                layers,
                triggerInteraction);

            // Filter out the character capsule itself
            nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                if (overlappedColliders[i] == Capsule)
                {
                    nbHits--;
                    if (i < nbHits)
                    {
                        overlappedColliders[i] = overlappedColliders[nbHits];
                    }
                }
            }

            return nbHits;
        }

        /// <summary>
        /// Sweeps the capsule's volume to detect collision hits
        /// </summary>
        /// <returns> Returns the number of hits </returns>
        public int CharacterCollisionsSweep(Vector3 position, Quaternion rotation, Vector3 direction, float distance,
            out RaycastHit closestHit, RaycastHit[] hits, float inflate = 0f, bool acceptOnlyStableGroundLayer = false)
        {
            int queryLayers = CollidableLayers;
            if (acceptOnlyStableGroundLayer)
            {
                queryLayers = CollidableLayers & StableGroundLayers;
            }

            Vector3 bottom = position + (rotation * _characterTransformToCapsuleBottomHemi) -
                             (direction * SweepProbingBackstepDistance);
            Vector3 top = position + (rotation * _characterTransformToCapsuleTopHemi) -
                          (direction * SweepProbingBackstepDistance);
            if (inflate != 0f)
            {
                bottom += (rotation * Vector3.down * inflate);
                top += (rotation * Vector3.up * inflate);
            }

            // Capsule cast
            int nbHits = 0;
            int nbUnfilteredHits = physics.CapsuleCast(
                bottom,
                top,
                Capsule.radius + inflate,
                direction,
                hits,
                distance + SweepProbingBackstepDistance,
                queryLayers,
                QueryTriggerInteraction.Ignore);

            // Hits filter
            closestHit = new RaycastHit();
            float closestDistance = Mathf.Infinity;
            nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                hits[i].distance -= SweepProbingBackstepDistance;

                RaycastHit hit = hits[i];
                float hitDistance = hit.distance;

                // Filter out the invalid hits
                if (hitDistance <= 0f ||
                    !CheckIfColliderValidForCollisions(hit.collider))
                {
                    nbHits--;
                    if (i < nbHits)
                    {
                        hits[i] = hits[nbHits];
                    }
                }
                else
                {
                    // Remember closest valid hit
                    if (hitDistance < closestDistance)
                    {
                        closestHit = hit;
                        closestDistance = hitDistance;
                    }
                }
            }

            return nbHits;
        }

        /// <summary>
        /// Sweeps the capsule's volume to detect hits
        /// </summary>
        /// <returns> Returns the number of hits </returns>
        public int CharacterSweep(Vector3 position, Quaternion rotation, Vector3 direction, float distance,
            out RaycastHit closestHit, RaycastHit[] hits, LayerMask layers, QueryTriggerInteraction triggerInteraction,
            float inflate = 0f)
        {
            closestHit = new RaycastHit();

            Vector3 bottom = position + (rotation * _characterTransformToCapsuleBottomHemi);
            Vector3 top = position + (rotation * _characterTransformToCapsuleTopHemi);
            if (inflate != 0f)
            {
                bottom += (rotation * Vector3.down * inflate);
                top += (rotation * Vector3.up * inflate);
            }

            // Capsule cast
            int nbHits = 0;
            int nbUnfilteredHits = physics.CapsuleCast(
                bottom,
                top,
                Capsule.radius + inflate,
                direction,
                hits,
                distance,
                layers,
                triggerInteraction);

            // Hits filter
            float closestDistance = Mathf.Infinity;
            nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                RaycastHit hit = hits[i];

                // Filter out the character capsule
                if (hit.distance <= 0f || hit.collider == Capsule)
                {
                    nbHits--;
                    if (i < nbHits)
                    {
                        hits[i] = hits[nbHits];
                    }
                }
                else
                {
                    // Remember closest valid hit
                    float hitDistance = hit.distance;
                    if (hitDistance < closestDistance)
                    {
                        closestHit = hit;
                        closestDistance = hitDistance;
                    }
                }
            }

            return nbHits;
        }

        /// <summary>
        /// Casts the character volume in the character's downward direction to detect ground
        /// </summary>
        /// <returns> Returns the number of hits </returns>
        private bool CharacterGroundSweep(Vector3 position, Quaternion rotation, Vector3 direction, float distance,
            out RaycastHit closestHit)
        {
            closestHit = new RaycastHit();

            // Capsule cast
            int nbUnfilteredHits = physics.CapsuleCast(
                position + (rotation * _characterTransformToCapsuleBottomHemi) -
                (direction * GroundProbingBackstepDistance),
                position + (rotation * _characterTransformToCapsuleTopHemi) -
                (direction * GroundProbingBackstepDistance),
                Capsule.radius,
                direction,
                _internalCharacterHits,
                distance + GroundProbingBackstepDistance,
                CollidableLayers & StableGroundLayers,
                QueryTriggerInteraction.Ignore);

            // Hits filter
            bool foundValidHit = false;
            float closestDistance = Mathf.Infinity;
            for (int i = 0; i < nbUnfilteredHits; i++)
            {
                RaycastHit hit = _internalCharacterHits[i];
                float hitDistance = hit.distance;

                // Find the closest valid hit
                if (hitDistance > 0f && CheckIfColliderValidForCollisions(hit.collider))
                {
                    if (hitDistance < closestDistance)
                    {
                        closestHit = hit;
                        closestHit.distance -= GroundProbingBackstepDistance;
                        closestDistance = hitDistance;

                        foundValidHit = true;
                    }
                }
            }

            return foundValidHit;
        }

        /// <summary>
        /// Raycasts to detect collision hits
        /// </summary>
        /// <returns> Returns the number of hits </returns>
        public int CharacterCollisionsRaycast(Vector3 position, Vector3 direction, float distance,
            out RaycastHit closestHit, RaycastHit[] hits, bool acceptOnlyStableGroundLayer = false)
        {
            int queryLayers = CollidableLayers;
            if (acceptOnlyStableGroundLayer)
            {
                queryLayers = CollidableLayers & StableGroundLayers;
            }

            // Raycast
            int nbHits = 0;
            int nbUnfilteredHits = physics.Raycast(
                position,
                direction,
                hits,
                distance,
                queryLayers,
                QueryTriggerInteraction.Ignore);

            // Hits filter
            closestHit = new RaycastHit();
            float closestDistance = Mathf.Infinity;
            nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                RaycastHit hit = hits[i];
                float hitDistance = hit.distance;

                // Filter out the invalid hits
                if (hitDistance <= 0f ||
                    !CheckIfColliderValidForCollisions(hit.collider))
                {
                    nbHits--;
                    if (i < nbHits)
                    {
                        hits[i] = hits[nbHits];
                    }
                }
                else
                {
                    // Remember closest valid hit
                    if (hitDistance < closestDistance)
                    {
                        closestHit = hit;
                        closestDistance = hitDistance;
                    }
                }
            }

            return nbHits;
        }
    }
}