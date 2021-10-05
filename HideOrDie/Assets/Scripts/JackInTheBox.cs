using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JackInTheBox : MonoBehaviour
{
    public int ID;
    public CollectibleManager cm;
    // Start is called before the first frame update
    void Start()
    {
        cm = FindObjectOfType<CollectibleManager>();
        if (cm.GetIfDestroyed(ID))
            gameObject.SetActive(false);
    }

    public void OnBoxDestroyed()
    {
        cm.DestroyingNewBox(ID);
    }
}
