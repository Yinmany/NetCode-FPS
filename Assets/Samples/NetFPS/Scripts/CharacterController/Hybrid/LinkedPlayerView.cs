using System;
using MyGameLib.NetCode;
using MyGameLib.NetCode.Hybrid;
using NetCodeExample.Scripts.Weapon;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetCode_Example_Scenes.Scripts.Player
{
    /// <summary>
    /// 连接的玩家的视图表现对象
    /// </summary>
    public class LinkedPlayerView : MonoBehaviour
    {
        public Transform hud;

        // 客户端相机是要进行插值过渡，所有这个才是游戏逻辑位置。
        public Quaternion cameraRot;
        public Vector3 cameraPos;

        public Camera Camera;
        public Transform TargetIk;
        public Animator Anim;

        [Header("Audio Clips")] public AudioClip shootSound;
        [Header("Audio Sources")] public AudioSource shootAudioSource;

        [Header("Spawnpoints")] public Transform casingSpawnpoint;
        public Transform bulletSpawnpoint;
        public Transform grenadeSpawnpoint;

        [Header("Prefabs")] public Transform casingPrefab;
        public Transform bulletPrefab;
        public float bulletForce;
        public Transform grenadePrefab;
        public float grenadeSpawnDelay;

        public Vector3 bulletPoint;
        public Quaternion bulletRot;

        public Vector3 grenadePoint;
        public Quaternion grenadeRot;

        [Obsolete("就demo使用")] public Transform weaponMuzzle;

        public Vector3 targetCamOffset;

        [Header("Weapon Components")] public ParticleSystem muzzleflashParticles;
        public Light muzzleflashLight;

        private void Awake()
        {
            Camera.enabled = false;
            Camera.GetComponent<AudioListener>().enabled = false;
            targetCamOffset = Camera.transform.position - transform.position;
        }

        public void ShowHUD() => hud.gameObject.SetActive(true);

        public void ActiveLocalPlayerCamera()
        {
            if (!Camera.enabled)
            {
                Camera.enabled = true;
                Camera.GetComponent<AudioListener>().enabled = true;
                Camera.transform.SetParent(null);

                Debug.LogError($"启用客户端相机.");
            }
        }

        public void Fire()
        {
            //Play shoot sound 
            shootAudioSource.clip = shootSound;
            shootAudioSource.Play();

            //Play from second layer, from the beginning
            Anim.Play("Fire", 1, 0.0f);

            //Spawn casing at spawnpoint
            Instantiate(casingPrefab,
                casingSpawnpoint.transform.position,
                casingSpawnpoint.transform.rotation);

            var bullet = (Transform) Instantiate(
                bulletPrefab,
                bulletPoint,
                bulletRot);

            //Add velocity to the bullet
            bullet.GetComponent<Rigidbody>().velocity =
                bullet.transform.forward * bulletForce;
        }

        /// <summary>
        /// 生成子弹
        /// </summary>
        public void GenProjectile(World world, float3 pos, float3 dir, bool isServer = false)
        {
            //Play shoot sound 
            if (!isServer)
            {
                shootAudioSource.clip = shootSound;
                shootAudioSource.Play();
            }

            //Play from second layer, from the beginning
            Anim.Play("Fire", 1, 0.0f);

            var commands = world.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>()
                .CreateCommandBuffer();

            // 实例化子弹
            var collections = world.GetOrCreateSystem<PrefabCollectionSystem>();
            Entity ent = commands.Instantiate(collections.Prefabs[0].Value);
            commands.AddComponent(ent, new ProjectileComponent
            {
                CamerPos = pos,
                CamerForword = dir
            });

            var spawnPoint = new Translation {Value = pos};
            commands.SetComponent(ent, spawnPoint);
            commands.SetComponent(ent,
                new Rotation {Value = Quaternion.LookRotation(dir)});
        }

        public void GenGrenade(World world, float3 pos, float3 dir, bool isServer = false)
        {
            //Play grenade throw animation
            Anim.Play("Grenade_Throw", 1, 0.0f);

            //Spawn grenade prefab at spawnpoint
            var trans = Instantiate(grenadePrefab,
                pos,
                Quaternion.LookRotation(dir));

            trans.GetComponent<GrenadeScript>().Init(world, isServer);
            SceneManager.MoveGameObjectToScene(trans.gameObject, world.GetLinkedScene());
        }

        private void FixedUpdate()
        {
            bulletPoint = bulletSpawnpoint.position;
            bulletRot = bulletSpawnpoint.rotation;
            grenadePoint = grenadeSpawnpoint.position;
            grenadeRot = grenadeSpawnpoint.rotation;
        }
    }
}