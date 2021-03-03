using UnityEngine;
using System.Collections;

public class LightningScript : MonoBehaviour {

	[Header("Light Intensity")]
	public float minIntensity = 1.0f;
	public float maxIntensity = 3.0f;

	[Header("Light Duration")]
	//How long the light should be visible
	public float lightDuration = 0.025f;

	[Header("Delay Between Flashes")]
	//Delay between lightning flashes
	public float minFlashDelay = 0.05f;
	public float maxFlashDelay = 2.0f;

	[Header("Total Delay")]
	//Time between ligthing effect
	//15 seconds default
	public float minDelay = 5.0f;
	public float maxDelay = 15.0f;

	//Total delay time
	float delay;
	//Delay time between lightning one
	//and lightning two
	float flashDelay;

	bool isWaiting = false;

	[Header("Background Color")]
	//Default background color
	public Color mainBackgroundColor;
	//Lightning background color
	public Color lightningBackgroundColor;

	[Header("Lightning Size")]
	//Minimum size of the lightning sprite
	public float minSize;
	//Maximum size of the lightning sprite
	public float maxSize;

	[Header("Components")]
	//Gun camera
	public Camera gunCamera;
	//The light component
	public Light lightObject;
	//Audio source
	public AudioSource lightningSound;

	//Array holding the lightning sprites
	public Sprite[] lightningSprites;
	//Sprite renderer
	public SpriteRenderer lightningSpriteRenderer;

	//Position and scale values for 
	//the lightning sprite renderer
	float x;
	float y;
	Vector3 lightningPos;
	float lightningScale;

	private void Start () {
		//Make sure its off at start
		lightObject.enabled = false;

		//Set the background color of the gun camera
		gunCamera.backgroundColor = mainBackgroundColor;
	}

	private void Update () {
		//Random value for how long the waiting should be
		delay = (Random.Range (minDelay, maxDelay));
		//Random value for how long the delay between the flashes should be
		flashDelay = (Random.Range (minFlashDelay, maxFlashDelay));
		//If is waiting is false, and the random value is 15
		if (!isWaiting) {
			//Start light flash one
			StartCoroutine (LightFlashOne ());
			//Is waiting
			isWaiting = true;
		}
	}

	//First light flash
	private IEnumerator LightFlashOne () {
		//Enable the light
		lightObject.enabled = true;
		//Set a random intensity value
		lightObject.intensity = (Random.Range (minIntensity, maxIntensity));
		//Wait for set amount of time

		//Set the background color of the gun camera
		gunCamera.backgroundColor = lightningBackgroundColor;

		//Enable the lightning sprite renderer
		lightningSpriteRenderer.enabled = true;
		//Show a random lightning sprite from the array
		lightningSpriteRenderer.sprite = lightningSprites 
			[Random.Range (0, lightningSprites.Length)];

		//Get random position for lightning sprite renderer
		x = Random.Range (-100, 100);
		y = Random.Range (12, 28);
		lightningPos = new Vector3 (x, y, 75);
		//Choose random scale value
		lightningScale = Random.Range(minSize, maxSize);

		//Move the sprite renderer to the new position
		lightningSpriteRenderer.transform.position = lightningPos;
		//Set the sprite renderer to the new scale
		lightningSpriteRenderer.transform.localScale = new Vector3 
			(lightningScale,lightningScale,lightningScale);

		yield return new WaitForSeconds (lightDuration);
		//Disable the light
		lightObject.enabled = false;

		//Set the background color of the gun camera
		gunCamera.backgroundColor = mainBackgroundColor;

		//Disable the lightning sprite renderer
		lightningSpriteRenderer.enabled = false;

		//Start the flash delay
		StartCoroutine (FlashDelay ());
	}

	//Delay between LightFlashOne one and LightFlashTwo
	private IEnumerator FlashDelay () {
		//Wait for set amount of time
		yield return new WaitForSeconds (flashDelay);
		//Start light flash two
		StartCoroutine (LightFlashTwo ());
	}

	//Second light flash
	private IEnumerator LightFlashTwo () {
		//Enable the light
		lightObject.enabled = true;
		//Set a random intensity value
		lightObject.intensity = (Random.Range (minIntensity, maxIntensity));

		//Set the background color of the gun camera
		gunCamera.backgroundColor = lightningBackgroundColor;

		//Enable the lightning sprite renderer
		lightningSpriteRenderer.enabled = true;
		//Show a random lightning sprite from the array
		lightningSpriteRenderer.sprite = lightningSprites 
			[Random.Range (0, lightningSprites.Length)];

		//Get random position for lightning sprite renderer
		x = Random.Range (-100, 100);
		y = Random.Range (12, 28);
		lightningPos = new Vector3 (x, y, 75);
		//Choose random scale value
		lightningScale = Random.Range(minSize, maxSize);

		//Move the sprite renderer to the new position
		lightningSpriteRenderer.transform.position = lightningPos;
		//Set the sprite renderer to the new scale
		lightningSpriteRenderer.transform.localScale = new Vector3 
			(lightningScale,lightningScale,lightningScale);

		//Play sound
		lightningSound.Play();

		//Wait for set amount of time
		yield return new WaitForSeconds (lightDuration);
		//Disable the light
		lightObject.enabled = false;

		//Set the background color of the gun camera
		gunCamera.backgroundColor = mainBackgroundColor;

		//Disable the lightning sprite renderer
		lightningSpriteRenderer.enabled = false;

		//Start the waiting timer
		StartCoroutine(Timer());
	}

	//Time between lightnings
	private IEnumerator Timer () {
		//Wait for set amount of time
		yield return new WaitForSeconds (delay);
		//Is not waiting
		isWaiting = false;
	}
}