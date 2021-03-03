using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GrenadeLauncherScriptLPFP : MonoBehaviour {

	//Animator component attached to weapon
	Animator anim;

	[Header("Gun Camera")]
	//Main gun camera
	public Camera gunCamera;

	[Header("Gun Camera Options")]
	//How fast the camera field of view changes when aiming 
	[Tooltip("How fast the camera field of view changes when aiming.")]
	public float fovSpeed = 15.0f;
	//Default camera field of view
	[Tooltip("Default value for camera field of view (40 is recommended).")]
	public float defaultFov = 40.0f;

	[Header("UI Weapon Name")]
	[Tooltip("Name of the current weapon, shown in the game UI.")]
	public string weaponName;

	[Header("Weapon Attachments (Only use one scope attachment)")]
	[Space(10)]
	//Toggle weapon attachments (loads at start)
	//Toggle scope 01
	public bool scope1;
	public Sprite scope1Texture;
	public float scope1TextureSize = 0.0045f;
	//Scope 01 camera fov
	[Range(5, 40)]
	public float scope1AimFOV = 10;
	[Space(10)]
	//Toggle scope 02
	public bool scope2;
	public Sprite scope2Texture;
	public float scope2TextureSize = 0.01f;
	//Scope 02 camera fov
	[Range(5, 40)]
	public float scope2AimFOV = 25;
	[Space(10)]
	//Toggle scope 03
	public bool scope3;
	public Sprite scope3Texture;
	public float scope3TextureSize = 0.006f;
	//Scope 03 camera fov
	[Range(5, 40)]
	public float scope3AimFOV = 20;
	[Space(10)]
	//Toggle scope 04
	public bool scope4;
	public Sprite scope4Texture;
	public float scope4TextureSize = 0.0025f;
	//Scope 04 camera fov
	[Range(5, 40)]
	public float scope4AimFOV = 12;
	[Space(10)]
	//Toggle iron sights
	public bool ironSights;
	public bool alwaysShowIronSights;
	//Iron sights camera fov
	[Range(5, 40)]
	public float ironSightsAimFOV = 16;
	//Weapon attachments components
	[System.Serializable]
	public class weaponAttachmentRenderers 
	{
		[Header("Scope Model Renderers")]
		[Space(10)]
		//All attachment renderer components
		public SkinnedMeshRenderer scope1Renderer;
		public SkinnedMeshRenderer scope2Renderer;
		public SkinnedMeshRenderer scope3Renderer;
		public SkinnedMeshRenderer scope4Renderer;
		public SkinnedMeshRenderer ironSightsRenderer;
		[Header("Scope Sight Mesh Renderers")]
		[Space(10)]
		//Scope render meshes
		public GameObject scope1RenderMesh;
		public GameObject scope2RenderMesh;
		public GameObject scope3RenderMesh;
		public GameObject scope4RenderMesh;
		[Header("Scope Sight Sprite Renderers")]
		[Space(10)]
		//Scope sight textures
		public SpriteRenderer scope1SpriteRenderer;
		public SpriteRenderer scope2SpriteRenderer;
		public SpriteRenderer scope3SpriteRenderer;
		public SpriteRenderer scope4SpriteRenderer;
	}
	public weaponAttachmentRenderers WeaponAttachmentRenderers;

	[Header("Weapon Sway")]
	//Enables weapon sway
	[Tooltip("Toggle weapon sway.")]
	public bool weaponSway;

	public float swayAmount = 0.02f;
	public float maxSwayAmount = 0.06f;
	public float swaySmoothValue = 4.0f;

	private Vector3 initialSwayPosition;

	[Header("Weapon Settings")]

	public float autoReloadDelay;

	//Check if reloading
	private bool isReloading;

	//Holstering weapon
	private bool hasBeenHolstered = false;
	//If weapon is holstered
	private bool holstered;
	//Check if running
	private bool isRunning;
	//Check if aiming
	private bool isAiming;
	//Check if walking
	private bool isWalking;
	//Check if inspecting weapon
	private bool isInspecting;

	//How much ammo is currently left
	private int currentAmmo;
	//Totalt amount of ammo
	private int ammo = 1;
	//Check if out of ammo
	private bool outOfAmmo;

	[Header("Grenade Settings")]
	public float grenadeSpawnDelay = 0.35f;

	[Header("Muzzleflash Settings")]
	public bool randomMuzzleflash = false;
	//min should always bee 1
	private int minRandomValue = 1;

	[Range(2, 25)]
	public int maxRandomValue = 5;

	private int randomMuzzleflashValue;

	public bool enableMuzzleFlash;
	public ParticleSystem muzzleParticles;
	public bool enableSparks;
	public ParticleSystem sparkParticles;
	public int minSparkEmission = 1;
	public int maxSparkEmission = 7;

	[Header("Muzzleflash Light Settings")]
	public Light muzzleFlashLight;
	public float lightDuration = 0.02f;

	[Header("Audio Source")]
	//Main audio source
	public AudioSource mainAudioSource;
	//Audio source used for shoot sound
	public AudioSource shootAudioSource;

	[Header("UI Components")]
	public Text timescaleText;
	public Text currentWeaponText;
	public Text currentAmmoText;
	public Text totalAmmoText;

	[System.Serializable]
	public class prefabs
	{  
		[Header("Prefabs")]
		public Transform projectilePrefab;
		public Transform grenadePrefab;
	}
	public prefabs Prefabs;
	
	[System.Serializable]
	public class spawnpoints
	{  
		[Header("Spawnpoints")]
		//Array holding casing spawn points 
		//(some weapons use more than one casing spawn)
		//Bullet prefab spawn from this point
		public Transform bulletSpawnPoint;

		public Transform grenadeSpawnPoint;
	}
	public spawnpoints Spawnpoints;

	[System.Serializable]
	public class soundClips
	{
		public AudioClip shootSound;
		public AudioClip takeOutSound;
		public AudioClip holsterSound;
		public AudioClip reloadSound;
		public AudioClip aimSound;
	}
	public soundClips SoundClips;

	private bool soundHasPlayed = false;

	private void Awake () {
		
		//Set the animator component
		anim = GetComponent<Animator>();
		//Set current ammo to total ammo value
		currentAmmo = ammo;

		muzzleFlashLight.enabled = false;

		//Show in log if another scope is being used with iron sights
		if (ironSights && scope1 == true || 
			ironSights && scope2 == true || 
			ironSights && scope3 == true || 
			ironSights && scope4) 
		{
			Debug.Log 
			("Only use one scope attachment, animations won't work " +
				"properly if several scope attachments are being used");
		}

		//Weapon attachments
		//If scope1 is true
		if (scope1 == true && WeaponAttachmentRenderers.scope1Renderer != null) 
		{
			//If scope1 is true, enable scope renderer
			WeaponAttachmentRenderers.scope1Renderer.GetComponent
			<SkinnedMeshRenderer> ().enabled = true;
			//Also enable the scope sight render mesh
			WeaponAttachmentRenderers.scope1RenderMesh.SetActive(true);
			//Set the scope sight texture
			WeaponAttachmentRenderers.scope1SpriteRenderer.GetComponent
			<SpriteRenderer>().sprite = scope1Texture;
			//Set the scope texture size
			WeaponAttachmentRenderers.scope1SpriteRenderer.transform.localScale = new Vector3 
				(scope1TextureSize, scope1TextureSize, scope1TextureSize);
		} 
		else if (WeaponAttachmentRenderers.scope1Renderer != null)
		{
			//If scope1 is false, disable scope renderer
			WeaponAttachmentRenderers.scope1Renderer.GetComponent<
			SkinnedMeshRenderer> ().enabled = false;
			//Also disable the scope sight render mesh
			WeaponAttachmentRenderers.scope1RenderMesh.SetActive(false);
		}
		//If scope 2 is true
		if (scope2 == true && WeaponAttachmentRenderers.scope2Renderer != null) 
		{
			//If scope2 is true, enable scope renderer
			WeaponAttachmentRenderers.scope2Renderer.GetComponent
			<SkinnedMeshRenderer> ().enabled = true;
			//Also enable the scope sight render mesh
			WeaponAttachmentRenderers.scope2RenderMesh.SetActive(true);
			//Set the scope sight texture
			WeaponAttachmentRenderers.scope2SpriteRenderer.GetComponent
			<SpriteRenderer>().sprite = scope2Texture;
			//Set the scope texture size
			WeaponAttachmentRenderers.scope2SpriteRenderer.transform.localScale = new Vector3 
				(scope2TextureSize, scope2TextureSize, scope2TextureSize);
		} 
		else if (WeaponAttachmentRenderers.scope2Renderer != null)
		{
			//If scope2 is false, disable scope renderer
			WeaponAttachmentRenderers.scope2Renderer.GetComponent
			<SkinnedMeshRenderer> ().enabled = false;
			//Also disable the scope sight render mesh
			WeaponAttachmentRenderers.scope2RenderMesh.SetActive(false);
		}
		//If scope 3 is true
		if (scope3 == true && WeaponAttachmentRenderers.scope3Renderer != null) 
		{
			//If scope3 is true, enable scope renderer
			WeaponAttachmentRenderers.scope3Renderer.GetComponent
			<SkinnedMeshRenderer> ().enabled = true;
			//Also enable the scope sight render mesh
			WeaponAttachmentRenderers.scope3RenderMesh.SetActive(true);
			//Set the scope sight texture
			WeaponAttachmentRenderers.scope3SpriteRenderer.GetComponent
			<SpriteRenderer>().sprite = scope3Texture;
			//Set the scope texture size
			WeaponAttachmentRenderers.scope3SpriteRenderer.transform.localScale = new Vector3 
				(scope3TextureSize, scope3TextureSize, scope3TextureSize);
		} 
		else if (WeaponAttachmentRenderers.scope3Renderer != null)
		{
			//If scope3 is false, disable scope renderer
			WeaponAttachmentRenderers.scope3Renderer.GetComponent
			<SkinnedMeshRenderer> ().enabled = false;
			//Also disable the scope sight render mesh
			WeaponAttachmentRenderers.scope3RenderMesh.SetActive(false);
		}
		//If scope 4 is true
		if (scope4 == true && WeaponAttachmentRenderers.scope4Renderer != null) 
		{
			//If scope4 is true, enable scope renderer
			WeaponAttachmentRenderers.scope4Renderer.GetComponent
			<SkinnedMeshRenderer> ().enabled = true;
			//Also enable the scope sight render mesh
			WeaponAttachmentRenderers.scope4RenderMesh.SetActive(true);
			//Set the scope sight texture
			WeaponAttachmentRenderers.scope4SpriteRenderer.GetComponent
			<SpriteRenderer>().sprite = scope4Texture;
			//Set the scope texture size
			WeaponAttachmentRenderers.scope4SpriteRenderer.transform.localScale = new Vector3 
				(scope4TextureSize, scope4TextureSize, scope4TextureSize);
		} 
		else if (WeaponAttachmentRenderers.scope4Renderer != null)
		{
			//If scope4 is false, disable scope renderer
			WeaponAttachmentRenderers.scope4Renderer.GetComponent
			<SkinnedMeshRenderer> ().enabled = false;
			//Also enable the scope sight render mesh
			WeaponAttachmentRenderers.scope4RenderMesh.SetActive(false);
		}

		//If alwaysShowIronSights is true
		if (alwaysShowIronSights == true && WeaponAttachmentRenderers.ironSightsRenderer != null) {
			WeaponAttachmentRenderers.ironSightsRenderer.GetComponent
			<SkinnedMeshRenderer> ().enabled = true;
		}

		//If ironSights is true
		if (ironSights == true && WeaponAttachmentRenderers.ironSightsRenderer != null) 
		{
			//If scope1 is true, enable scope renderer
			WeaponAttachmentRenderers.ironSightsRenderer.GetComponent
			<SkinnedMeshRenderer> ().enabled = true;
			//If always show iron sights is enabled, don't disable 
			//Do not use if iron sight renderer is not assigned in inspector
		} else if (!alwaysShowIronSights && 
			WeaponAttachmentRenderers.ironSightsRenderer != null) {
			//If scope1 is false, disable scope renderer
			WeaponAttachmentRenderers.ironSightsRenderer.GetComponent
			<SkinnedMeshRenderer> ().enabled = false;
		}
	}

	private void Start () {
		
		//Get weapon name from string to text
		currentWeaponText.text = weaponName;
		//Set total ammo text from total ammo int
		totalAmmoText.text = ammo.ToString();

		//Weapon sway
		initialSwayPosition = transform.localPosition;

		//Set the shoot sound to audio source
		shootAudioSource.clip = SoundClips.shootSound;
	}

	private void LateUpdate () {
		
		//Weapon sway
		if (weaponSway == true) 
		{
			float movementX = -Input.GetAxis ("Mouse X") * swayAmount;
			float movementY = -Input.GetAxis ("Mouse Y") * swayAmount;
			//Clamp movement to min and max values
			movementX = Mathf.Clamp 
				(movementX, -maxSwayAmount, maxSwayAmount);
			movementY = Mathf.Clamp 
				(movementY, -maxSwayAmount, maxSwayAmount);
			//Lerp local pos
			Vector3 finalSwayPosition = new Vector3 
				(movementX, movementY, 0);
			transform.localPosition = Vector3.Lerp 
				(transform.localPosition, finalSwayPosition + 
					initialSwayPosition, Time.deltaTime * swaySmoothValue);
		}
	}
	
	private void Update () {
		
		//Aiming
		//Toggle camera FOV when right click is held down
		if(Input.GetButton("Fire2") && !isReloading && !isRunning && !isInspecting) 
		{
			if (ironSights == true) 
			{
				gunCamera.fieldOfView = Mathf.Lerp (gunCamera.fieldOfView,
					ironSightsAimFOV, fovSpeed * Time.deltaTime);
			}
			if (scope1 == true) 
			{
				gunCamera.fieldOfView = Mathf.Lerp (gunCamera.fieldOfView,
					scope1AimFOV, fovSpeed * Time.deltaTime);
			}
			if (scope2 == true) 
			{
				gunCamera.fieldOfView = Mathf.Lerp (gunCamera.fieldOfView,
					scope2AimFOV, fovSpeed * Time.deltaTime);
			}
			if (scope3 == true) 
			{
				gunCamera.fieldOfView = Mathf.Lerp (gunCamera.fieldOfView,
					scope3AimFOV, fovSpeed * Time.deltaTime);
			}
			if (scope4 == true) 
			{
				gunCamera.fieldOfView = Mathf.Lerp (gunCamera.fieldOfView,
					scope4AimFOV, fovSpeed * Time.deltaTime);
			}

			isAiming = true;

			//If iron sights are enabled, use normal aim
			if (ironSights == true) 
			{
				anim.SetBool ("Aim", true);
			}
			//If scope 1 is enabled, use scope 1 aim in animation
			if (scope1 == true) 
			{
				anim.SetBool ("Aim Scope 1", true);
			}
			//If scope 2 is enabled, use scope 2 aim in animation
			if (scope2 == true) 
			{
				anim.SetBool ("Aim Scope 2", true);
			}
			//If scope 3 is enabled, use scope 3 aim in animation
			if (scope3 == true) 
			{
				anim.SetBool ("Aim Scope 3", true);
			}
			//If scope 4 is enabled, use scope 4 aim in animation
			if (scope4 == true) 
			{
				anim.SetBool ("Aim Scope 4", true);
			}

			if (!soundHasPlayed) 
			{
				mainAudioSource.clip = SoundClips.aimSound;
				mainAudioSource.Play ();

				soundHasPlayed = true;
			}

			//If scope 1 is true, show scope sight texture when aiming
			if (scope1 == true) 
			{
				WeaponAttachmentRenderers.scope1SpriteRenderer.GetComponent
				<SpriteRenderer> ().enabled = true;
			}
			//If scope 2 is true, show scope sight texture when aiming
			if (scope2 == true) 
			{
				WeaponAttachmentRenderers.scope2SpriteRenderer.GetComponent
				<SpriteRenderer> ().enabled = true;
			}
			//If scope 3 is true, show scope sight texture when aiming
			if (scope3 == true) 
			{
				WeaponAttachmentRenderers.scope3SpriteRenderer.GetComponent
				<SpriteRenderer> ().enabled = true;
			}
			//If scope 4 is true, show scope sight texture when aiming
			if (scope4 == true) 
			{
				WeaponAttachmentRenderers.scope4SpriteRenderer.GetComponent
				<SpriteRenderer> ().enabled = true;
			}
		} 
		else 
		{
			//When right click is released
			gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView,
				defaultFov,fovSpeed * Time.deltaTime);

			isAiming = false;

			//If iron sights are enabled, use normal aim out
			if (ironSights == true) 
			{
				anim.SetBool ("Aim", false);
			}
			//If scope 1 is enabled, use scope 1 aim out animation
			if (scope1 == true) 
			{
				anim.SetBool ("Aim Scope 1", false) ;
			}
			//If scope 2 is enabled, use scope 2 aim out animation
			if (scope2 == true) 
			{
				anim.SetBool ("Aim Scope 2", false);
			}
			//If scope 3 is enabled, use scope 3 aim out animation
			if (scope3 == true) 
			{
				anim.SetBool ("Aim Scope 3", false) ;
			}

			//If scope 4 is enabled, use scope 4 aim out animation
			if (scope4 == true) 
			{
				anim.SetBool ("Aim Scope 4", false) ;
			}
				
			soundHasPlayed = false;

			//If scope 1 is true, disable scope sight texture when not aiming
			if (scope1 == true) 
			{
				WeaponAttachmentRenderers.scope1SpriteRenderer.GetComponent
				<SpriteRenderer> ().enabled = false;
			}
			//If scope 2 is true, disable scope sight texture when not aiming
			if (scope2 == true) 
			{
				WeaponAttachmentRenderers.scope2SpriteRenderer.GetComponent
				<SpriteRenderer> ().enabled = false;
			}
			//If scope 3 is true, disable scope sight texture when not aiming
			if (scope3 == true) 
			{
				WeaponAttachmentRenderers.scope3SpriteRenderer.GetComponent
				<SpriteRenderer> ().enabled = false;
			}
			//If scope 4 is true, disable scope sight texture when not aiming
			if (scope4 == true) 
			{
				WeaponAttachmentRenderers.scope4SpriteRenderer.GetComponent
				<SpriteRenderer> ().enabled = false;
			}
		}
		//Aiming end

		//If randomize muzzleflash is true, genereate random int values
		if (randomMuzzleflash == true) 
		{
			randomMuzzleflashValue = Random.Range (minRandomValue, maxRandomValue);
		}

		//Timescale settings
		//Change timescale to normal when 1 key is pressed
		if (Input.GetKeyDown (KeyCode.Alpha1)) 
		{
			Time.timeScale = 1.0f;
			timescaleText.text = "1.0";
		}
		//Change timescale to 50% when 2 key is pressed
		if (Input.GetKeyDown (KeyCode.Alpha2)) 
		{
			Time.timeScale = 0.5f;
			timescaleText.text = "0.5";
		}
		//Change timescale to 25% when 3 key is pressed
		if (Input.GetKeyDown (KeyCode.Alpha3)) 
		{
			Time.timeScale = 0.25f;
			timescaleText.text = "0.25";
		}
		//Change timescale to 10% when 4 key is pressed
		if (Input.GetKeyDown (KeyCode.Alpha4)) 
		{
			Time.timeScale = 0.1f;
			timescaleText.text = "0.1";
		}
		//Pause game when 5 key is pressed
		if (Input.GetKeyDown (KeyCode.Alpha5)) 
		{
			Time.timeScale = 0.0f;
			timescaleText.text = "0.0";
		}

		//Set current ammo text from ammo int
		currentAmmoText.text = currentAmmo.ToString ();

		//Continosuly check which animation 
		//is currently playing
		AnimationCheck ();

		//Play knife attack 1 animation when Q key is pressed
		if (Input.GetKeyDown (KeyCode.Q) && !isInspecting) 
		{
			anim.Play ("Knife Attack 1", 0, 0f);
		}
		//Play knife attack 2 animation when F key is pressed
		if (Input.GetKeyDown (KeyCode.F) && !isInspecting) 
		{
			anim.Play ("Knife Attack 2", 0, 0f);
		}
			
		//Throw grenade when pressing G key
		if (Input.GetKeyDown (KeyCode.G) && !isInspecting) 
		{
			StartCoroutine (GrenadeSpawnDelay ());
			//Play grenade throw animation
			anim.Play("GrenadeThrow", 0, 0.0f);
		}

		if (currentAmmo <= 0 && !outOfAmmo) 
		{
			outOfAmmo = true;

			StartCoroutine (AutoReload ());
		}
			
		//Shooting
		if (Input.GetMouseButtonDown (0) && !outOfAmmo && !isReloading && !isInspecting && !isRunning) 
		{
			anim.Play ("Fire", 0, 0f);
		
			muzzleParticles.Emit (1);

			//Spawn projectile prefab
			Instantiate (
				Prefabs.projectilePrefab, 
				Spawnpoints.bulletSpawnPoint.transform.position, 
				Spawnpoints.bulletSpawnPoint.transform.rotation);
				
			//Remove 1 bullet from ammo
			currentAmmo -= 1;
	
			shootAudioSource.clip = SoundClips.shootSound;
			shootAudioSource.Play ();

			//Light flash start
			StartCoroutine(MuzzleFlashLight());

			if (!isAiming) //if not aiming
			{
				anim.Play ("Fire", 0, 0f);

				muzzleParticles.Emit (1);

				if (enableSparks == true) 
				{
					//Emit random amount of spark particles
					sparkParticles.Emit (Random.Range (1, 6));
				}
			} 
			else //if aiming
			{
				anim.Play ("Aim Fire", 0, 0f);

				//If random muzzle is false
				if (!randomMuzzleflash) {
					muzzleParticles.Emit (1);
					//If random muzzle is true
				} 
				else if (randomMuzzleflash == true) 
				{
					//Only emit if random value is 1
					if (randomMuzzleflashValue == 1) 
					{
						if (enableSparks == true) 
						{
							//Emit random amount of spark particles
							sparkParticles.Emit (Random.Range (1, 6));
						}
						if (enableMuzzleFlash == true) 
						{
							muzzleParticles.Emit (1);
							//Light flash start
							StartCoroutine (MuzzleFlashLight ());
						}
					}
				}
			}
		}

		//Inspect weapon when pressing T key
		if (Input.GetKeyDown (KeyCode.T)) 
		{
			anim.SetTrigger ("Inspect");
		}

		//Toggle weapon holster with E key
		if (Input.GetKeyDown (KeyCode.E) && !hasBeenHolstered) 
		{
			holstered = true;

			mainAudioSource.clip = SoundClips.holsterSound;
			mainAudioSource.Play();

			hasBeenHolstered = true;
		} 
		else if (Input.GetKeyDown (KeyCode.E) && hasBeenHolstered) 
		{
			holstered = false;

			mainAudioSource.clip = SoundClips.takeOutSound;
			mainAudioSource.Play ();

			hasBeenHolstered = false;
		}

		//Holster anim toggle
		if (holstered == true) {
			anim.SetBool ("Holster", true);
		} else {
			anim.SetBool ("Holster", false);
		}

		//Walking when pressing down WASD keys
		if (Input.GetKey (KeyCode.W) && !isRunning || 
			Input.GetKey (KeyCode.A) && !isRunning || 
			Input.GetKey (KeyCode.S) && !isRunning || 
			Input.GetKey (KeyCode.D) && !isRunning) 
		{
			anim.SetBool ("Walk", true);
		} else {
			anim.SetBool ("Walk", false);
		}

		//Running when pressing down W and Left Shift key
		if ((Input.GetKey (KeyCode.W) && Input.GetKey (KeyCode.LeftShift))) 
		{
			isRunning = true;
		} else {
			isRunning = false;
		}
		
		//Run anim toggle
		if (isRunning == true) 
		{
			anim.SetBool ("Run", true);
		} 
		else 
		{
			anim.SetBool ("Run", false);
		}
	}

	private IEnumerator GrenadeSpawnDelay () {
		//Wait for set amount of time before spawning grenade
		yield return new WaitForSeconds (grenadeSpawnDelay);
		//Spawn grenade prefab at spawnpoint
		Instantiate(Prefabs.grenadePrefab, 
			Spawnpoints.grenadeSpawnPoint.transform.position, 
			Spawnpoints.grenadeSpawnPoint.transform.rotation);
	}

	private IEnumerator AutoReload () {
		//Wait set amount of time
		yield return new WaitForSeconds (autoReloadDelay);

		if (outOfAmmo == true) 
		{
			//Play diff anim if out of ammo
			anim.Play ("Reload", 0, 0f);

			mainAudioSource.clip = SoundClips.reloadSound;
			mainAudioSource.Play ();

		} 
		//Restore ammo when reloading
		currentAmmo = ammo;
		outOfAmmo = false;
	}

	//Show light when shooting, then disable after set amount of time
	private IEnumerator MuzzleFlashLight () {
		
		muzzleFlashLight.enabled = true;
		yield return new WaitForSeconds (lightDuration);
		muzzleFlashLight.enabled = false;
	}

	//Check current animation playing
	private void AnimationCheck () {

		//Check if reloading
		//Check both animations
		if (anim.GetCurrentAnimatorStateInfo (0).IsName ("Reload")) 
		{
			isReloading = true;
		} 
		else 
		{
			isReloading = false;
		}

		//Check if inspecting weapon
		if (anim.GetCurrentAnimatorStateInfo (0).IsName ("Inspect")) 
		{
			isInspecting = true;
		} 
		else 
		{
			isInspecting = false;
		}
	}
}