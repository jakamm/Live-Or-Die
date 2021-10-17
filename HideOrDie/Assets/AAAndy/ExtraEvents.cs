using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtraEvents : MonoBehaviour
{
    ExtraScene ExtraScene;
    // Start is called before the first frame update
    void Start()
    {
        ExtraScene = FindObjectOfType<ExtraScene>();
    }
    public void FinishedOn()
    {
        ExtraScene.OnExtraOnEnd?.Invoke();
    }

    public void FinishedOff()
    {
        ExtraScene.OnExtraOffEnd?.Invoke();
    }
}
