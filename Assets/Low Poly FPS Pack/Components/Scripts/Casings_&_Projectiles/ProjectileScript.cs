using UnityEngine;
using System.Collections;

public class ProjectileScript : MonoBehaviour {

	private bool explodeSelf;
	[Tooltip("Enable to use constant force, instead of force at launch only")]
	public bool useConstantForce;
	[Tooltip("How fast the projectile moves")]
	public float constantForceSpeed;
	[Tooltip("How long after spawning that the projectile self destructs")]
	public float explodeAfter;
	private bool hasStartedExplode;

	private bool hasCollided;

	[Header("Explosion Prefabs")]
	//Explosion prefab
	public Transform explosionPrefab;

	[Header("Customizable Options")]
	//Force added at start
	[Tooltip("Initial launch force")]
	public float force = 5000f;
	//Time before the projectile is destroyed
	[Tooltip("How long after spawning should the projectile object destroy")]
	public float despawnTime = 30f;

	[Header("Explosion Options")]
	//Radius of the explosion
	[Tooltip("Explosion radius")]
	public float radius = 50.0F;
	//Intensity of the explosion
	[Tooltip("Explosion intensity")]
	public float power = 250.0F;

	[Header("Rocket Launcher Projectile")]
	[Tooltip("Enabled if the projectile has particle effects")]
	public bool usesParticles;
	public ParticleSystem smokeParticles;
	public ParticleSystem flameParticles;
	[Tooltip("Added delay to let particle effects finish playing, " +
		"before destroying object")]
	public float destroyDelay;

	private void Start () 
	{
		//If not using constant force (grenade launcher projectile)
		if (!useConstantForce) 
		{
			//Launch the projectile forward by adding force to it at start
			GetComponent<Rigidbody> ().AddForce 
				(gameObject.transform.forward * force);
		}

		//Start the destroy timer
		StartCoroutine (DestroyTimer ());
	}
		
	private void FixedUpdate()
	{
		//Rotates the projectile according to the direction it is going
		if(GetComponent<Rigidbody>().velocity != Vector3.zero)
			GetComponent<Rigidbody>().rotation = 
				Quaternion.LookRotation(GetComponent<Rigidbody>().velocity);  

		//If using constant force
		if (useConstantForce == true && !hasStartedExplode) {
			//Launch the projectile forward with a constant force (used for rockets)
			GetComponent<Rigidbody>().AddForce 
				(gameObject.transform.forward * constantForceSpeed);

			//Start self explode
			StartCoroutine (ExplodeSelf ());

			//Stop looping
			hasStartedExplode = true;
		}
	}

	//Used for when the rocket is flying into the sky for example, 
	//it should blow up after some time
	private IEnumerator ExplodeSelf () 
	{
		//Wait set amount of time
		yield return new WaitForSeconds (explodeAfter);
		//Spawn explosion particle prefab
		if (!hasCollided) {
			Instantiate (explosionPrefab, transform.position, transform.rotation);
		}
		//Hide projectile
		gameObject.GetComponent<MeshRenderer> ().enabled = false;
		//Freeze object
		gameObject.GetComponent<Rigidbody> ().isKinematic = true;
		//Disable collider
		gameObject.GetComponent<BoxCollider>().isTrigger = true;
		//Stop particles and let them finish playing before destroying
		if (usesParticles == true) {
			flameParticles.GetComponent <ParticleSystem> ().Stop ();
			smokeParticles.GetComponent<ParticleSystem> ().Stop ();
		}
		//Wait more to let particle systems disappear
		yield return new WaitForSeconds (destroyDelay);
		//Destroy projectile
		Destroy (gameObject);
	}

	private IEnumerator DestroyTimer () 
	{
		//Destroy the projectile after set amount of time
		yield return new WaitForSeconds (despawnTime);
		//Destroy gameobject
		Destroy (gameObject);
	}

	private IEnumerator DestroyTimerAfterCollision () 
	{
		//Wait set amount of time after collision to destroy projectile
		yield return new WaitForSeconds (destroyDelay);
		//Destroy gameobject
		Destroy (gameObject);
	}

	//If the projectile collides with anything
	private void OnCollisionEnter (Collision collision) 
	{

		hasCollided = true;

		//Hide projectile
		gameObject.GetComponent<MeshRenderer> ().enabled = false;
		//Freeze object
		gameObject.GetComponent<Rigidbody> ().isKinematic = true;
		//Disable collider
		gameObject.GetComponent<BoxCollider>().isTrigger = true;

		if (usesParticles == true) {
			flameParticles.GetComponent <ParticleSystem> ().Stop ();
			smokeParticles.GetComponent<ParticleSystem> ().Stop ();
		}

		StartCoroutine (DestroyTimerAfterCollision ());

		//Instantiate explosion prefab at collision position
		Instantiate(explosionPrefab,collision.contacts[0].point,
			Quaternion.LookRotation(collision.contacts[0].normal));

		//If the projectile hit the tag "Target", and if "isHit" is false
		if (collision.gameObject.tag == "Target" && 
		    	collision.gameObject.GetComponent<TargetScript>().isHit == false) {
			
			//Spawn explosion prefab on surface
			Instantiate(explosionPrefab,collision.contacts[0].point,
			            Quaternion.LookRotation(collision.contacts[0].normal));

			//Animate the target 
			collision.gameObject.transform.gameObject.GetComponent
				<Animation> ().Play("target_down");
			//Toggle the isHit bool on the target object
			collision.gameObject.transform.gameObject.GetComponent
				<TargetScript>().isHit = true;
		}

		//Explosion force
		Vector3 explosionPos = transform.position;
		//Use overlapshere to check for colliders in range
		Collider[] colliders = Physics.OverlapSphere(explosionPos, radius);
		foreach (Collider hit in colliders) {
			Rigidbody rb = hit.GetComponent<Rigidbody> ();

			//Add force to nearby rigidbodies
			if (rb != null)
				rb.AddExplosionForce (power * 50, explosionPos, radius, 3.0F);

			//If the explosion hit the tags "Target", and "isHit" is false
			if (hit.GetComponent<Collider>().tag == "Target" && 
			    	hit.GetComponent<TargetScript>().isHit == false) {

				//Animate the target 
				hit.gameObject.GetComponent<Animation> ().Play("target_down");
				//Toggle the isHit bool on the target object
				hit.gameObject.GetComponent<TargetScript>().isHit = true;
			}

			//If the projectile explosion hits barrels with the tag "ExplosiveBarrel"
			if (hit.transform.tag == "ExplosiveBarrel") {
				
				//Toggle the explode bool on the explosive barrel object
				hit.transform.gameObject.GetComponent<ExplosiveBarrelScript>().explode = true;
			}

			//If the projectile explosion hits objects with "GasTank" tag
			if (hit.GetComponent<Collider>().tag == "GasTank") 
			{
				//If gas tank is within radius, explode it
				hit.gameObject.GetComponent<GasTankScript> ().isHit = true;
				hit.gameObject.GetComponent<GasTankScript> ().explosionTimer = 0.05f;
			}
		}
	}
}