using UnityEngine;
using UnityEngine.Events;

public class ItemEvent : MonoBehaviour, IItemEvent
{
    public UnityEvent InteractEvent;

    [SaveableField, HideInInspector]
    public bool eventExecuted;

    public void DoEvent()
    {
        if (!eventExecuted)
        {
            InteractEvent?.Invoke();
        }
    }
}
