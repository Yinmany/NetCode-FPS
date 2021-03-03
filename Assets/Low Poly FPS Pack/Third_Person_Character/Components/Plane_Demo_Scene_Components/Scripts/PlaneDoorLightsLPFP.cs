using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneDoorLightsLPFP : MonoBehaviour {

	[Header("Plane Lights Object")]
	public GameObject planeDoorLights;

	[Header("Green Light Material")]
	public Material greenEmission;

	[Header("Light Components")]
	public Light redLight;
	public Light greenLight;

	[Header("Timer")]
	public float openDoorTimer;

	private void Start () 
	{
		//Start timer
		StartCoroutine (DoorLightsTimer ());
		//Enable red light, disable green
		redLight.enabled = true;
		greenLight.enabled = false;
	}

	private IEnumerator DoorLightsTimer () 
	{	
		//Wait for set amount of time
		yield return new WaitForSeconds (openDoorTimer);
		//Set light to green material
		planeDoorLights.GetComponent<MeshRenderer> ().material = greenEmission;
		//Enable green light, disable red light
		redLight.enabled = false;
		greenLight.enabled = true;
	}
}