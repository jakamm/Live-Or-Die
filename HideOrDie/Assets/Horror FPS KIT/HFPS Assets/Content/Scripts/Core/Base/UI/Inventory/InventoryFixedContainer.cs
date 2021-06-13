/*
 * InventoryFixedContainer.cs - script by ThunderWire Games
 * ver. 1.0
*/

using UnityEngine;

public class InventoryFixedContainer : MonoBehaviour
{
    public string ContainerName;

    public void UseObject()
    {
        Inventory.Instance.ShowFixedInventoryContainer(ContainerName);
    }
}
