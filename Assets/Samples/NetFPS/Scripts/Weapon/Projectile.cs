using System;
using System.Collections;
using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using Samples.MyGameLib.NetCode;
using Samples.MyGameLib.NetCode.Base;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NetCode_Example_Scenes.Scripts.Player
{
    public class Projectile : NetworkBehaviour
    {
        public float Speed = 100f;
        public Vector3 CamerForword, CamerPos;
        public float trajectoryCorrectionDistance = -1;

        private Vector3 _trajectoryCorrectionVector, _consumedTrajectoryCorrectionVector;
        private Vector3 _velocity;
        private bool m_HasTrajectoryOverride;
        private Vector3 m_LastRootPosition;

        public float maxLifetime = 5f;
        public GameObject fx;

        public PhysicsScene _physicsScene;

        public float offset = 5f;

        public float checkCode;
        [Header("Impact Effect Prefabs")] public Transform[] bloodImpactPrefabs;
        public Transform[] metalImpactPrefabs;

        public void Awake()
        {
            GetComponent<MeshRenderer>().enabled = false;
        }

        private IEnumerator Show()
        {
            yield return new WaitForSeconds(0.1f);
            GetComponent<MeshRenderer>().enabled = true;
        }

        public void OnShoot()
        {
            // StartCoroutine(Show());

            _velocity = CamerForword * Speed;

            // 相机到枪口向量
            Vector3 cameraToMuzzle = transform.position - CamerPos;

            _trajectoryCorrectionVector = Vector3.ProjectOnPlane(-cameraToMuzzle, CamerForword);

            m_HasTrajectoryOverride = true;
            Destroy(gameObject, maxLifetime);

            transform.position += _velocity.normalized * offset;

            m_LastRootPosition = transform.position;

            // if (_physicsScene.Raycast(CamerPos, cameraToMuzzle.normalized,
            //     out RaycastHit hit, cameraToMuzzle.magnitude + _velocity.magnitude))
            // {
            //     Debug.DrawLine(CamerPos, hit.point, Color.blue, 5f);
            //     OnHit(hit.point, hit.normal);
            // }
        }

        protected override void OnSimulate(float dt)
        {
            checkCode += dt;

            transform.position += _velocity * dt;

            if (m_HasTrajectoryOverride && _consumedTrajectoryCorrectionVector.sqrMagnitude <
                _trajectoryCorrectionVector.sqrMagnitude)
            {
                var correctionLeft = _trajectoryCorrectionVector - _consumedTrajectoryCorrectionVector;
                float distanceThisFrame = (transform.position - m_LastRootPosition).magnitude;
                Vector3 correctionThisFrame =
                    (distanceThisFrame / trajectoryCorrectionDistance) * _trajectoryCorrectionVector;

                correctionThisFrame = Vector3.ClampMagnitude(correctionThisFrame, correctionLeft.magnitude);

                // if (!IsServer)
                //     Debug.Log($"{(float3) correctionThisFrame},{(float3) correctionLeft} {correctionLeft.magnitude}");

                _consumedTrajectoryCorrectionVector += correctionThisFrame;

                if (_consumedTrajectoryCorrectionVector.sqrMagnitude == _trajectoryCorrectionVector.sqrMagnitude)
                {
                    m_HasTrajectoryOverride = false;
                }

                transform.position += correctionThisFrame;
            }
            else
            {
                if (!this.IsServer)
                {
                    GetComponent<MeshRenderer>().enabled = true;
                }
            }

            // Orient towards velocity
            transform.forward = _velocity.normalized;

            // add gravity to the projectile velocity for ballistic effect
            _velocity += Vector3.down * 10f * dt;

            // 碰撞检测
            Vector3 displacementSinceLastFrame = transform.position - m_LastRootPosition;

            RaycastHit[] result = new RaycastHit[1];

            int len = _physicsScene.SphereCast(m_LastRootPosition, 0.1f, displacementSinceLastFrame.normalized, result,
                displacementSinceLastFrame.magnitude, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            if (len > 0)
            {
                // uint tick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;
                // if (this.IsServer)
                // {
                //     NetDebug.SLog(
                //         $"{tick} 击中物体:{result[0].collider.name} {(float3) result[0].point} {(float3) transform.position} {math.lengthsq((float3) result[0].point)} {checkCode}");
                // }
                // else
                // {
                //     NetDebug.CLog(
                //         $"{tick} 击中物体:{result[0].collider.name} {(float3) result[0].point} {(float3) transform.position}  {math.lengthsq((float3) result[0].point)}  {checkCode}");
                // }

                OnHit(result[0].collider, result[0].point, result[0].normal);
            }

            m_LastRootPosition = transform.position;
        }

        void OnHit(Collider target, Vector3 point, Vector3 normal)
        {
            Destroy(gameObject);

            if (!IsServer)
            {
                // var vfxTmp = Instantiate(fx, point, Quaternion.LookRotation(normal));
                // Destroy(vfxTmp, 5f);

                //If bullet collides with "Metal" tag
                if (target.transform.CompareTag("Metal"))
                {
                    //Instantiate random impact prefab from array
                    Instantiate(metalImpactPrefabs[Random.Range
                            (0, bloodImpactPrefabs.Length)], point,
                        Quaternion.LookRotation(normal));
                }
            }
            else // 服务端处理
            {
                if (target == null || !target.CompareTag("Player")) // 击中的是环境
                {
                    World.GetExistingSystem<BroadcastSystem>().Add(new ProjectileHit
                    {
                        GId = -1,
                        Point = point,
                        Normal = normal
                    });

                    // Debug.Log($"命中环境.");
                }
                else
                {
                    EntityManager dstManager = World.EntityManager;
                    Entity targetEnt = target.GetComponent<EntityHold>().Ent;

                    GhostComponent ghost = dstManager.GetComponentData<GhostComponent>(targetEnt);
                    HealthComponent health = dstManager.GetComponentData<HealthComponent>(targetEnt);
                    health.Hp -= 10;
                    dstManager.SetComponentData(targetEnt, health);

                    World.EntityManager.World.GetExistingSystem<BroadcastSystem>().Add(new ProjectileHit
                    {
                        GId = ghost.Id,
                        Hp = health.Hp,
                        Point = point,
                        Normal = normal
                    });

                    // Debug.Log($"命中GHOST: Id={ghost.Id} Hp={health.Hp}");
                }
            }
        }

        private void OnDestroy()
        {
            World.EntityManager.DestroyEntity(this.SelfEntity);
            Destroy(this.gameObject);
        }
    }
}