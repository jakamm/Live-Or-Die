using UnityEngine;
using UnityEngine.Events;

public class OnDestroyEvent : MonoBehaviour
{
    public UnityEvent m_OnDestroy;

    private void OnDestroy()
    {
        m_OnDestroy?.Invoke();
    }
}
