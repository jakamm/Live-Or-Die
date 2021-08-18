using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerChanger : MonoBehaviour
{
    public int newLayer;
    private int defaultLayer;

    // Start is called before the first frame update
    void Start()
    {
        defaultLayer = this.gameObject.layer;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Contains("Player"))
        {
            this.gameObject.layer = newLayer;
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.tag.Contains("Player"))
        {
            this.gameObject.layer =  defaultLayer;
        }
    }
}
