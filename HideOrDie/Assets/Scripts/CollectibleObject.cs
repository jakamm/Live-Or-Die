using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleObject : MonoBehaviour
{
    public enum ObjectType { JITB, Clip, Letter, _MAX }
    public ObjectType type;
    [Tooltip("Check the box if object will not show up after interacted with")]
    public bool HideIfDestroyed;
    public int ID;
    public CollectibleManager cm;
    // Start is called before the first frame update
    void Start()
    {
        cm = FindObjectOfType<CollectibleManager>();
        if (cm.GetIfDestroyed(type, ID) && HideIfDestroyed)
            gameObject.SetActive(false);
    }

    public void OnItemInteracted()
    {
        Debug.Log("Called");
        //cm.DestroyingNewBox(type, ID);
        cm.Interacted(type, ID);
    }

    public static int GetObjectTypeMax() => (int)ObjectType._MAX;
}
