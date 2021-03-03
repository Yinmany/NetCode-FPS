using UnityEngine;
using System.Collections;

public class LightMovementScript : MonoBehaviour {

	Vector3 StartPos;
	Vector3 randomPos;

	//Min and max light intensity values
	public float minIntensity = 0.25f;
	public float maxIntensity = 0.5f;

	float random;
	float TimeSinceRandomRefresh = 9999.0f;

	private void Start ()	{
		//Start at lights position
		StartPos = transform.position;
		random = Random.Range(0.0f, 25000.0f);
	}
		
	private void Update ()	{
		setRandomPos(0.1f);
		RandomLerpPos(0.2f);

		float noise = Mathf.PerlinNoise(random, Time.time);
		GetComponent<Light>().intensity = Mathf.Lerp
			(minIntensity, maxIntensity, noise);
	}
		
	private void RandomLerpPos(float speed)	{
		Vector3 newPos = Vector3.Lerp
			(transform.position, randomPos, Time.deltaTime * speed);
		transform.position = newPos;
	}

	private void setRandomPos(float interval)	{
		if(TimeSinceRandomRefresh > interval)
		{
			randomPos = Random.insideUnitSphere;
			randomPos += StartPos;

			TimeSinceRandomRefresh = 0.0f;
		}
		else
		{
			TimeSinceRandomRefresh += Time.deltaTime;
		}
	}
}