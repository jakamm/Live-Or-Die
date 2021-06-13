using UnityEngine;
using UnityEngine.EventSystems;

public class UIRaycastEvent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool pointerEnter;

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerEnter = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerEnter = false;
    }
}
