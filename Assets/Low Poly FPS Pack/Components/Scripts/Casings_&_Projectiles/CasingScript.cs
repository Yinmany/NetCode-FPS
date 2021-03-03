using UnityEngine;
using System.Collections;

public class CasingScript : MonoBehaviour {

	[Header("Force X")]
	[Tooltip("Minimum force on X axis")]
	public float minimumXForce;		
	[Tooltip("Maimum force on X axis")]
	public float maximumXForce;
	[Header("Force Y")]
	[Tooltip("Minimum force on Y axis")]
	public float minimumYForce;
	[Tooltip("Maximum force on Y axis")]
	public float maximumYForce;
	[Header("Force Z")]
	[Tooltip("Minimum force on Z axis")]
	public float minimumZForce;
	[Tooltip("Maximum force on Z axis")]
	public float maximumZForce;
	[Header("Rotation Force")]
	[Tooltip("Minimum initial rotation value")]
	public float minimumRotation;
	[Tooltip("Maximum initial rotation value")]
	public float maximumRotation;
	[Header("Despawn Time")]
	[Tooltip("How long after spawning that the casing is destroyed")]
	public float despawnTime;

	[Header("Audio")]
	public AudioClip[] casingSounds;
	public AudioSource audioSource;

	[Header("Spin Settings")]
	//How fast the casing spins
	[Tooltip("How fast the casing spins over time")]
	public float speed = 2500.0f;

	//Launch the casing at start
	private void Awake () 
	{
		//Random rotation of the casing
		GetComponent<Rigidbody>().AddRelativeTorque (
			Random.Range(minimumRotation, maximumRotation), //X Axis
			Random.Range(minimumRotation, maximumRotation), //Y Axis
			Random.Range(minimumRotation, maximumRotation)  //Z Axis
			* Time.deltaTime);

		//Random direction the casing will be ejected in
		GetComponent<Rigidbody>().AddRelativeForce (
			Random.Range (minimumXForce, maximumXForce),  //X Axis
			Random.Range (minimumYForce, maximumYForce),  //Y Axis
			Random.Range (minimumZForce, maximumZForce)); //Z Axis		     
	}

	private void Start () 
	{
		//Start the remove/destroy coroutine
		StartCoroutine (RemoveCasing ());
		//Set random rotation at start
		transform.rotation = Random.rotation;
		//Start play sound coroutine
		StartCoroutine (PlaySound ());
	}

	private void FixedUpdate () 
	{
		//Spin the casing based on speed value
		transform.Rotate (Vector3.right, speed * Time.deltaTime);
		transform.Rotate (Vector3.down, speed * Time.deltaTime);
	}

	private IEnumerator PlaySound () 
	{
		//Wait for random time before playing sound clip
		yield return new WaitForSeconds (Random.Range(0.25f, 0.85f));
		//Get a random casing sound from the array 
		audioSource.clip = casingSounds
			[Random.Range(0, casingSounds.Length)];
		//Play the random casing sound
		audioSource.Play();
	}

	private IEnumerator RemoveCasing () 
	{
		//Destroy the casing after set amount of seconds
		yield return new WaitForSeconds (despawnTime);
		//Destroy casing object
		Destroy (gameObject);
	}
}