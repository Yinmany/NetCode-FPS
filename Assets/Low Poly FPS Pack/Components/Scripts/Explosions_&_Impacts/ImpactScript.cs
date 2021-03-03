using UnityEngine;
using System.Collections;

public class ImpactScript : MonoBehaviour {

	[Header("Impact Despawn Timer")]
	//How long before the impact is destroyed
	public float despawnTimer = 10.0f;

	[Header("Audio")]
	public AudioClip[] impactSounds;
	public AudioSource audioSource;

	private void Start () {
		// Start the despawn timer
		StartCoroutine (DespawnTimer ());

		//Get a random impact sound from the array
		audioSource.clip = impactSounds
			[Random.Range(0, impactSounds.Length)];
		//Play the random impact sound
		audioSource.Play();
	}
	
	private IEnumerator DespawnTimer() {
		//Wait for set amount of time
		yield return new WaitForSeconds (despawnTimer);
		//Destroy the impact gameobject
		Destroy (gameObject);
	}
}