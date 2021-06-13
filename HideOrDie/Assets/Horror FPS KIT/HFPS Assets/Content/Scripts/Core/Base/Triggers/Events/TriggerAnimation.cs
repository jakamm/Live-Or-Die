using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderWire.Utility;

public class TriggerAnimation : MonoBehaviour {

	public GameObject AnimationObject;
	public AudioClip AnimationSound;
	public float Volume = 0.5f;
	public bool is2D;

    [SaveableField, HideInInspector]
	public bool isPlayed;

	void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player" && !isPlayed)
		{
			AnimationObject.GetComponent<Animation>().Play();

			if (AnimationSound)
			{
				if (!is2D)
				{
					AudioSource.PlayClipAtPoint(AnimationSound, transform.position, Volume);
				}
				else
				{
					Tools.PlayOneShot2D(transform.position, AnimationSound, Volume);
				}
			}

			isPlayed = true;
		}
	}
}
