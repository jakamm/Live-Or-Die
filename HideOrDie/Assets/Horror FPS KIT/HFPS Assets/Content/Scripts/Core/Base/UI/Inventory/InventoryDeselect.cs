using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDeselect : MonoBehaviour, IPointerClickHandler
{
    private Inventory inventory;

    void Awake()
    {
        inventory = transform.root.GetComponentInChildren<Inventory>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        inventory.ResetInventory();
    }
}
