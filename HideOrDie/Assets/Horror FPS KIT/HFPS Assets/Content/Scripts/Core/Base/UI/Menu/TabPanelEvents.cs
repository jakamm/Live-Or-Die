using UnityEngine;
using UnityEngine.Events;

public class TabPanelEvents : MonoBehaviour
{
    public UnityEvent OnCancel;
    public UnityEvent OnApply;

    public void Cancel()
    {
        OnCancel?.Invoke();
    }

    public void Apply()
    {
        OnApply?.Invoke();
    }
}