using System;
using UnityEngine;

[Serializable]
public enum dragSoundType { Default, Once }

public class DragSound : MonoBehaviour {

    public dragSoundType soundType = dragSoundType.Default;
    public AudioClip dragSound;
    [Range(0, 1)] public float dragVolume;

    [SaveableField, HideInInspector]
    public bool isPlayed;

    private bool once;

    public void OnRigidbodyDrag()
    {
        if (!once)
        {
            if (soundType == dragSoundType.Default)
            {
                AudioSource.PlayClipAtPoint(dragSound, transform.position, dragVolume);
            }
            else if(!isPlayed)
            {
                AudioSource.PlayClipAtPoint(dragSound, transform.position, dragVolume);
                isPlayed = true;
            }

            once = true;
        }
    }

    public void OnRigidbodyRelease()
    {
        once = false;
    }
}
