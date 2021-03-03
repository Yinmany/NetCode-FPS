using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropellerRotateLPFP : MonoBehaviour {

	[Tooltip("How fast the propellers rotate on the Z axis")]
	public float rotationSpeed = 2500.0f;

	private void Update () 
	{
		transform.Rotate (0, 0, rotationSpeed * Time.deltaTime);
	}
}