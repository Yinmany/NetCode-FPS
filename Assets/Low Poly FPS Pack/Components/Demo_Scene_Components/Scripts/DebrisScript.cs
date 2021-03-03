using UnityEngine;
using System.Collections;

public class DebrisScript : MonoBehaviour {

	[Header("Audio")]
	public AudioClip[] debrisSounds;
	public AudioSource audioSource;

	//If the debris collides with anything
	private void OnCollisionEnter (Collision collision) {
		//Play the random sound if the collision speed is high enough
		if (collision.relativeVelocity.magnitude > 50) 
		{
			//Get a random debris sound from the array every collision
			audioSource.clip = debrisSounds
				[Random.Range (0, debrisSounds.Length)];
			//Play the random debris sound
			audioSource.Play ();
		}
	}
}