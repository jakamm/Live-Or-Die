using UnityEngine;

public class DynamicKey : MonoBehaviour, IItemEvent {
    public DynamicObject dynamicObject;

    public void DoEvent()
    {
        UseObject();
    }

    public void UseObject()
    {
        dynamicObject.hasKey = true;
    }
}
