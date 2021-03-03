using System;
using UnityEngine;
using System.Collections;
using MyGameLib.NetCode.Hybrid;
using Unity.Entities;
using Unity.Mathematics;

public class GrenadeScript : MonoBehaviour
{
    [Header("Timer")]
    //Time before the grenade explodes
    [Tooltip("Time before the grenade explodes")]
    public float grenadeTimer = 5.0f;

    [Header("Explosion Prefabs")]
    //Explosion prefab
    public Transform explosionPrefab;

    [Header("Explosion Options")]
    //Radius of the explosion
    [Tooltip("The radius of the explosion force")]
    public float radius = 25.0F;

    //Intensity of the explosion
    [Tooltip("The intensity of the explosion force")]
    public float power = 350.0F;

    [Header("Throw Force")] [Tooltip("Minimum throw force")]
    public float minimumForce = 1500.0f;

    [Tooltip("Maximum throw force")] public float maximumForce = 2500.0f;
    private float throwForce;

    [Header("Audio")] public AudioSource impactSound;

    private PhysicsScene physicsScene;

    public bool isServer;

    public GameObject canvas;

    private void Awake()
    {
        //Generate random throw force
        //based on min and max values
        throwForce = maximumForce;

        //Random rotation of the grenade
        // GetComponent<Rigidbody>().AddRelativeTorque 
        //    (45, //X Axis
        // 	Random.Range(0,0), 		 //Y Axis
        // 	Random.Range(0,0)  		 //Z Axis
        // 	* Time.deltaTime * 5000);
    }

    public void Init(World world, bool isServer)
    {
        this.isServer = isServer;
        physicsScene = world.GetLinkedScene().GetPhysicsScene();
        var rigid = GetComponent<Rigidbody>();
        rigid.AddForce(gameObject.transform.forward * throwForce);
    }

    private void Start()
    {
        //Launch the projectile forward by adding force to it at start
        // GetComponent<Rigidbody>().AddForce(gameObject.transform.forward * throwForce);

        //Start the explosion timer
        StartCoroutine(ExplosionTimer());

        if (GetComponent<EntityHold>() && GetComponent<EntityHold>().World.IsServer())
        {
            canvas.SetActive(false);
            // GetComponent<MeshRenderer>().enabled = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isServer)
        {
            //Play the impact sound on every collision
            impactSound.Play();
        }
    }

    private void OnDestroy()
    {
        Instantiate(explosionPrefab, transform.position,
            Quaternion.FromToRotation(Vector3.forward, transform.up));
    }

    private IEnumerator ExplosionTimer()
    {
        //Wait set amount of time
        yield return new WaitForSeconds(grenadeTimer);

        //Raycast downwards to check ground
        // RaycastHit checkGround;
        // if (physicsScene.Raycast(transform.position, Vector3.down, out checkGround, 50))
        // {
        //     //Instantiate metal explosion prefab on ground
        //     Instantiate(explosionPrefab, checkGround.point,
        //         Quaternion.FromToRotation(Vector3.forward, checkGround.normal));
        // }

        //Explosion force
        Vector3 explosionPos = transform.position;

        //Use overlapshere to check for nearby colliders
        Collider[] colliders = new Collider[10];
        int len = physicsScene.OverlapSphere(explosionPos, radius, colliders, Physics.AllLayers,
            QueryTriggerInteraction.UseGlobal);

        for (int i = 0; i < len; i++)
        {
            Collider hit = colliders[i];

            Rigidbody rb = hit.GetComponent<Rigidbody>();

            //Add force to nearby rigidbodies
            if (rb != null)
                rb.AddExplosionForce(power * 5, explosionPos, radius, 3.0F);

            //If the explosion hits "Target" tag and isHit is false
            if (hit.GetComponent<Collider>().tag == "Target"
                && hit.gameObject.GetComponent<TargetScript>().isHit == false)
            {
                //Animate the target 
                hit.gameObject.GetComponent<Animation>().Play("target_down");
                //Toggle "isHit" on target object
                hit.gameObject.GetComponent<TargetScript>().isHit = true;
            }

            //If the explosion hits "ExplosiveBarrel" tag
            if (hit.GetComponent<Collider>().tag == "ExplosiveBarrel")
            {
                //Toggle "explode" on explosive barrel object
                hit.gameObject.GetComponent<ExplosiveBarrelScript>().explode = true;
            }

            //If the explosion hits "GasTank" tag
            if (hit.GetComponent<Collider>().tag == "GasTank")
            {
                //Toggle "isHit" on gas tank object
                hit.gameObject.GetComponent<GasTankScript>().isHit = true;
                //Reduce explosion timer on gas tank object to make it explode faster
                hit.gameObject.GetComponent<GasTankScript>().explosionTimer = 0.05f;
            }
        }
        //Destroy the grenade object on explosion
        // Destroy(gameObject);

        // 由服务端来进行手雷的销毁（爆炸）
        if (GetComponent<EntityHold>())
        {
            var entityFlag = GetComponent<EntityHold>();
            if (entityFlag.World.IsServer())
            {
                entityFlag.World.EntityManager.DestroyEntity(entityFlag.Ent);
            }
        }
    }
}