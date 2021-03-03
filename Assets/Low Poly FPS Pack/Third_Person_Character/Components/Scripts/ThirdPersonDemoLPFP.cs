using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonDemoLPFP : MonoBehaviour {

	[Header("Camera")]
	public Camera mainCamera;
	[Header("Camera FOV Settings")]
	public float zoomedFOV;
	public float defaultFOV;
	[Tooltip("How fast the camera zooms in")]
	public float fovSpeed;

	private Animator anim;

	[Header("Weapon Settings")]
	public bool semi;
	public bool auto;

	//Used for fire rate
	private float lastFired;
	//How fast the weapon fires, higher value means faster rate of fire
	[Tooltip("How fast the weapon fires, higher value means faster rate of fire.")]
	public float fireRate;

	[Header("Weapon Components")]
	public ParticleSystem muzzleflashParticles;
	public Light muzzleflashLight;

	[Header("Prefabs")]
	public Transform casingPrefab;
	public Transform bulletPrefab;
	public float bulletForce;
	public Transform grenadePrefab;
	public float grenadeSpawnDelay;

	[Header("Spawnpoints")]
	public Transform casingSpawnpoint;
	public Transform bulletSpawnpoint;
	public Transform grenadeSpawnpoint;

	[Header("Audio Clips")]
	public AudioClip shootSound;

	[Header("Audio Sources")]
	public AudioSource shootAudioSource;

	private void Start () 
	{
		//Assign animator component
		anim = gameObject.GetComponent<Animator> ();
		//Disable muzzleflash light at start
		muzzleflashLight.enabled = false;
	}

	private void Update () 
	{
		//Aim in with right click hold
		if (Input.GetMouseButton (1)) 
		{
			//Increase camera field of view
			mainCamera.fieldOfView = Mathf.Lerp (mainCamera.fieldOfView,
				zoomedFOV, fovSpeed * Time.deltaTime);
		} 
		else 
		{
			//Restore camera field of view
			mainCamera.fieldOfView = Mathf.Lerp (mainCamera.fieldOfView,
				defaultFOV, fovSpeed * Time.deltaTime);
		}

		//---------- The movement code is used to preview the different animations in the demo scene, should not actually be used for your games :) ---------//
		//Idle
		if (Input.GetKeyDown (KeyCode.T)) 
		{
			anim.SetFloat ("Vertical", 0.0f, 0, Time.deltaTime);
			anim.SetFloat ("Horizontal", 0.0f, 0, Time.deltaTime);
		}
		//Run forward
		if (Input.GetKeyDown (KeyCode.W)) 
		{
			anim.SetFloat ("Vertical", 1.0f, 0, Time.deltaTime);
			anim.SetFloat ("Horizontal", 0.0f, 0, Time.deltaTime);
		}
		//Run 45 up right
		if (Input.GetKeyDown (KeyCode.E)) 
		{
			anim.SetFloat ("Vertical", 1.0f, 0, Time.deltaTime);
			anim.SetFloat ("Horizontal", 1.0f, 0, Time.deltaTime);
		}
		//Run strafe right
		if (Input.GetKeyDown (KeyCode.D)) 
		{
			anim.SetFloat ("Vertical", 0.0f, 0, Time.deltaTime);
			anim.SetFloat ("Horizontal", 1.0f, 0, Time.deltaTime);
		}
		//Run 45 back right
		if (Input.GetKeyDown (KeyCode.X)) 
		{
			anim.SetFloat ("Vertical", -1.0f, 0, Time.deltaTime);
			anim.SetFloat ("Horizontal", 1.0f, 0, Time.deltaTime);
		}
		//Run backwards
		if (Input.GetKeyDown (KeyCode.S)) 
		{
			anim.SetFloat ("Vertical", -1.0f, 0, Time.deltaTime);
			anim.SetFloat ("Horizontal", 0.0f, 0, Time.deltaTime);
		}
		//Run 45 back left
		if (Input.GetKeyDown (KeyCode.Z)) 
		{
			anim.SetFloat ("Vertical", -1.0f, 0, Time.deltaTime);
			anim.SetFloat ("Horizontal", -1.0f, 0, Time.deltaTime);
		}
		//Run strafe left
		if (Input.GetKeyDown (KeyCode.A)) 
		{
			anim.SetFloat ("Vertical", 0.0f, 0, Time.deltaTime);
			anim.SetFloat ("Horizontal", -1.0f, 0, Time.deltaTime);
		}
		//Run 45 up left
		if (Input.GetKeyDown (KeyCode.Q)) 
		{
			anim.SetFloat ("Vertical", 1.0f, 0, Time.deltaTime);
			anim.SetFloat ("Horizontal", -1.0f, 0, Time.deltaTime);
		}
		//---------- The movement code is used to preview the different animations in the demo scene, should not actually be used for your games :) ---------//

		//Single fire with left click
		if (Input.GetMouseButtonDown (0) && semi == true) 
		{
			//Play shoot sound 
			shootAudioSource.clip = shootSound;
			shootAudioSource.Play ();

			//Play from second layer, from the beginning
			anim.Play ("Fire", 1, 0.0f);
			//Play muzzleflash particles
			muzzleflashParticles.Emit (1);
			//Play light flash
			StartCoroutine (MuzzleflashLight ());

			//Spawn casing at spawnpoint
			Instantiate (casingPrefab, 
				casingSpawnpoint.transform.position, 
				casingSpawnpoint.transform.rotation);
		}

		//AUtomatic fire
		//Left click hold 
		if (Input.GetMouseButton (0) && auto == true) 
		{
			//Shoot automatic
			if (Time.time - lastFired > 1 / fireRate) 
			{
				lastFired = Time.time;
				//Play shoot sound
				shootAudioSource.clip = shootSound;
				shootAudioSource.Play ();

				//Play from second layer, from the beginning
				anim.Play ("Fire", 1, 0.0f);
				//Play muzzleflash particles
				muzzleflashParticles.Emit (1);
				//Play light flash
				StartCoroutine (MuzzleflashLight ());

				//Spawn casing at spawnpoint
				Instantiate (casingPrefab, 
					casingSpawnpoint.transform.position, 
					casingSpawnpoint.transform.rotation);

				//Spawn bullet from bullet spawnpoint
				var bullet = (Transform)Instantiate (
					bulletPrefab,
					bulletSpawnpoint.transform.position,
					bulletSpawnpoint.transform.rotation);

				//Add velocity to the bullet
				bullet.GetComponent<Rigidbody> ().velocity = 
					bullet.transform.forward * bulletForce;
			}
		}

		//Reload with R key for testing
		if (Input.GetKeyDown (KeyCode.R)) {
			//Play reload animation
			anim.Play("Reload", 1, 0.0f);
		}

		//Throw grenade when pressing G key
		if (Input.GetKeyDown (KeyCode.G)) 
		{
			StartCoroutine (GrenadeSpawnDelay ());
			//Play grenade throw animation
			anim.Play("Grenade_Throw", 1, 0.0f);
		}
	}

	private IEnumerator GrenadeSpawnDelay () 
	{
		//Wait for set amount of time before spawning grenade
		yield return new WaitForSeconds (grenadeSpawnDelay);
		//Spawn grenade prefab at spawnpoint
		Instantiate(grenadePrefab, 
			grenadeSpawnpoint.transform.position, 
			grenadeSpawnpoint.transform.rotation);
	}

	IEnumerator MuzzleflashLight () 
	{
		muzzleflashLight.enabled = true;
		yield return new WaitForSeconds (0.02f);
		muzzleflashLight.enabled = false;
	}
}