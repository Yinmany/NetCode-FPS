using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlinkingLightLPFP : MonoBehaviour {

	[Header("Light Component")]
	public Light blinkingLight;

	[Header("Timers")]
	[Tooltip("How long the light is enabled")]
	public float blinkTimer = 0.03f;
	[Tooltip("How much time there is inbetween blinks")]
	public float blinkDuration = 2.5f;

	private void Start () 
	{
		//Disable light at start
		blinkingLight.enabled = false;
		//Start timer
		StartCoroutine (BlinkTimer ());
	}

	private IEnumerator BlinkTimer () 
	{
		//Wait for set amount of time
		yield return new WaitForSeconds (blinkDuration);
		//Start blinking
		StartCoroutine (BlinkOnce ());
	}

	private IEnumerator BlinkOnce () 
	{
		//Enable light
		blinkingLight.enabled = true;
		//Wait for set amount of time
		yield return new WaitForSeconds (blinkTimer);
		//Disable light
		blinkingLight.enabled = false;
		//Restart timer
		StartCoroutine (BlinkTimer ());
	}
}