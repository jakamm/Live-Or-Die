using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapManager : MonoBehaviour
{

    public Animation[] animations;

    public void DisableTrap()
    {
        for(int i = 0; i < animations.Length; i++)
        {
            animations[i].Stop();
        }
        Debug.Log("off");

    }

    public void EnableTrap()
    {

        for (int i = 0; i < animations.Length; i++)
        {
            animations[i].Play();
        }
        Debug.Log("on");
    }
}
