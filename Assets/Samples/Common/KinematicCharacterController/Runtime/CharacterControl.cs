// using KinematicCharacterController;
// using MyGameLib.NetCode;
// using MyGameLib.NetCode.Hybrid;
// using RootMotion.FinalIK;
// using Test;
// using Unity.Entities;
// using UnityEngine;
//
// namespace Player.Component
// {
//     public class CharacterControl : MonoBehaviour, ICharacterController, IConvertGameObjectToEntity
//     {
//         public Vector3 Gravity = Vector3.down * 9.83F * 3;
//
//         [Header("Ground Move")] public float GroundMoveSpeed = 10;
//         public float StableMovementSharpness = 15;
//         public float MoveOrientationSharpness = 10;
//         public float IdleOrientationSharpness = 5;
//         [Header("Air Move")] public float AirMoveSpeed = 5;
//         public float AirAccelerationSpeed = 5f;
//         public float Drag = 0.1F;
//         public float TurnSpeed = 10;
//         public float JumpForce = 10;
//         private KinematicCharacterMotor _motor;
//
//         public void Awake()
//         {
//             this._motor = this.GetComponent<KinematicCharacterMotor>();
//             this._motor.CharacterController = this;
//         }
//
//         private Quaternion cameraPlanarRotation;
//         public Vector3 IKTargetPos { get; set; }
//         public Vector3 _moveDir;
//         private Quaternion _cameraRot;
//         private bool rotationLock = true;
//
//         public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
//         {
//             var motor = this.GetComponent<KinematicCharacterMotor>();
//             SceneSystem sceneSystem = dstManager.World.GetExistingSystem<SceneSystem>();
//             if (sceneSystem == null)
//             {
//                 motor.physics = Physics.defaultPhysicsScene;
//             }
//             else
//             {
//                 motor.physics = sceneSystem.Scene.GetPhysicsScene();
//             }
//         }
//
//         public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
//         {
//             var lookInputVector = Vector3.zero;
//
//             Vector3 cameraPlanarDirection = Vector3
//                 .ProjectOnPlane(_cameraRot * Vector3.forward, _motor.CharacterUp).normalized;
//             if (cameraPlanarDirection.sqrMagnitude == 0f)
//             {
//                 cameraPlanarDirection = Vector3
//                     .ProjectOnPlane(_cameraRot * Vector3.up, _motor.CharacterUp).normalized;
//             }
//
//             cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, _motor.CharacterUp);
//
//             if (Vector3.SqrMagnitude(_motor.Velocity) < 0.5F)
//             {
//                 if (Vector3.Angle(cameraPlanarDirection, this.transform.forward) < 50 && rotationLock)
//                 {
//                     return;
//                 }
//                 else
//                 {
//                     rotationLock = false;
//
//                      Vector3 smoothedLookInputDirection = Vector3.Slerp(_motor.CharacterForward, cameraPlanarDirection,
//                          1 - Mathf.Exp(-IdleOrientationSharpness * deltaTime)).normalized;
//
//                     currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, _motor.CharacterUp);
//
//                     //currentRotation = Quaternion.LookRotation(cameraPlanarDirection, _motor.CharacterUp);
//                     if (1F - Vector3.Dot(cameraPlanarDirection, _motor.CharacterForward) < 0.1F)
//                     {
//                         rotationLock = true;
//                     }
//
//                     return;
//                 }
//             }
//
//             // 移动时
//             lookInputVector = cameraPlanarDirection;
//
//             if (lookInputVector != Vector3.zero && MoveOrientationSharpness > 0f)
//             {
//                 Vector3 smoothedLookInputDirection = Vector3.Slerp(_motor.CharacterForward, lookInputVector,
//                     1 - Mathf.Exp(-MoveOrientationSharpness * deltaTime)).normalized;
//
//                 currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, _motor.CharacterUp);
//             }
//         }
//
//         public void SetInput(ref InputCommand input)
//         {
//             _moveDir.x = input.dir.x;
//             _moveDir.z = input.dir.y;
//             IKTargetPos = input.targetPos;
//             _cameraRot = input.cameraRot;
//         }
//         
//         public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
//         {
//             var moveInput = _moveDir;
//
//             moveInput = Vector3.ClampMagnitude(moveInput, 1);
//             moveInput = cameraPlanarRotation * moveInput;
//
//             var targetMoverVelocity = Vector3.zero;
//             if (_motor.GroundingStatus.IsStableOnGround)
//             {
//                 currentVelocity =
//                     _motor.GetDirectionTangentToSurface(currentVelocity, _motor.GroundingStatus.GroundNormal) *
//                     currentVelocity.magnitude;
//                 var playerTangent = Vector3.Cross(moveInput, this._motor.CharacterUp);
//                 var reorientedInput = Vector3.Cross(_motor.GroundingStatus.GroundNormal, playerTangent).normalized *
//                                       moveInput.magnitude;
//
//                 targetMoverVelocity = reorientedInput * GroundMoveSpeed;
//                 currentVelocity = targetMoverVelocity;
//             }
//             else
//             {
//                 if (moveInput.sqrMagnitude > 0)
//                 {
//                     targetMoverVelocity = moveInput * this.AirMoveSpeed;
//                     if (_motor.GroundingStatus.FoundAnyGround)
//                     {
//                         Vector3 perpenticularObstructionNormal =
//                             Vector3.Cross(Vector3.Cross(_motor.CharacterUp, _motor.GroundingStatus.GroundNormal),
//                                 _motor.CharacterUp).normalized;
//                         targetMoverVelocity =
//                             Vector3.ProjectOnPlane(targetMoverVelocity, perpenticularObstructionNormal);
//                     }
//
//                     Vector3 velocityDiff = Vector3.ProjectOnPlane(targetMoverVelocity - currentVelocity, Gravity);
//                     currentVelocity += velocityDiff * AirAccelerationSpeed * deltaTime;
//                 }
//
//                 currentVelocity += Gravity * deltaTime;
//                 currentVelocity *= (1 / (1 + (this.Drag * deltaTime)));
//             }
//         }
//
//         public void BeforeCharacterUpdate(float deltaTime)
//         {
//             // 看向目标
//             GetComponent<GhostViewAuthoringComponent>().view.GetComponent<PlayerInfo>().GetComponent<AimIK>().solver
//                 .SetIKPosition(IKTargetPos);
//         }
//
//         public void PostGroundingUpdate(float deltaTime)
//         {
//         }
//
//         public void AfterCharacterUpdate(float deltaTime)
//         {
//         }
//
//         public bool IsColliderValidForCollisions(Collider coll)
//         {
//             return true;
//         }
//
//         public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
//             ref HitStabilityReport hitStabilityReport)
//         {
//         }
//
//         public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
//             ref HitStabilityReport hitStabilityReport)
//         {
//         }
//
//         public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
//             Vector3 atCharacterPosition, Quaternion atCharacterRotation,
//             ref HitStabilityReport hitStabilityReport)
//         {
//         }
//
//         public void OnDiscreteCollisionDetected(Collider hitCollider)
//         {
//         }
//     }
// }