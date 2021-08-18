using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapWire : MonoBehaviour
{

    public int damageAmount;
    public bool canDestroy = false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Contains("Player"))
        {
            other.gameObject.GetComponent<HealthManager>().ApplyDamage(damageAmount);
            if (canDestroy)
                Destroy(this.gameObject);
        }
    }

}
