using UnityEngine;
using System.Collections;

public class PlaySoundScript : StateMachineBehaviour {

	[Header("Audo Clips")]
	public AudioClip soundClip;

	override public void OnStateEnter
		(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		//Play sound clip at camera
		animator.gameObject.GetComponent<AudioSource> ().clip = soundClip;
		animator.gameObject.GetComponent<AudioSource> ().Play ();
	}
}