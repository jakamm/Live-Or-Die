/*
 * InventoryContainer.cs - script by ThunderWire Games
 * ver. 1.1
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class InventoryContainer : MonoBehaviour, ISaveable
{
    [Serializable]
    public class ItemKeyValue
    {
        public int ItemID;
        public int Amount;
    }

    private Inventory inventory;

    private ContainerItem selectedItem;

    private List<ContainerItemData> ContainterItemsData = new List<ContainerItemData>();

    public List<ItemKeyValue> StartingItems = new List<ItemKeyValue>();

    [Header("Settings")]
    public string containerName;
    public int containerSpace;

    [Header("Sounds")]
    public AudioClip OpenSound;
    [Range(0,1)]
    public float Volume = 1f;

    [HideInInspector] public bool isOpened;

    public bool IsSelecting()
    {
        return selectedItem;
    }

    public ContainerItem GetSelectedItem()
    {
        return selectedItem;
    }

    void Awake()
    {
        inventory = Inventory.Instance;
    }

    void Start()
    {
        if(StartingItems.Count > 0)
        {
            foreach (var item in StartingItems)
            {
                ContainterItemsData.Add(new ContainerItemData(inventory.GetItem(item.ItemID), item.Amount));
            }
        }
    }

    void Update()
    {
        if (!isOpened) return;

        if (inventory.ContainterItemsCache.Count > 0 && inventory.ContainterItemsCache.Any(item => item.IsSelected()))
        {
            selectedItem = inventory.ContainterItemsCache.SingleOrDefault(item => item.IsSelected());
        }
        else
        {
            selectedItem = null;
        }
    }

    public void UseObject()
    {
        if (OpenSound) { AudioSource.PlayClipAtPoint(OpenSound, transform.position, Volume); }

        inventory.ContainterItemsCache.Clear();
        inventory.ShowInventoryContainer(this, ContainterItemsData.ToArray(), containerName);
        isOpened = true;
    }

    public void StoreItem(Item item, int amount, CustomItemData customData = null)
    {
        GameObject coItem = Instantiate(inventory.containerItem, inventory.ContainterContent.transform);
        ContainerItemData itemData = new ContainerItemData(item, amount, customData);
        ContainerItem containerItem = coItem.GetComponent<ContainerItem>();
        containerItem.inventoryContainer = this;
        containerItem.item = item;
        containerItem.amount = amount;
        containerItem.customData = customData;
        coItem.name = "CoItem_" + item.Title.Replace(" ", "");
        ContainterItemsData.Add(itemData);
        inventory.ContainterItemsCache.Add(coItem.GetComponent<ContainerItem>());
    }

    /// <summary>
    /// Take Back last selected Item
    /// </summary>
    public void TakeBack(bool all, ContainerItem item)
    {
        if (inventory.CheckInventorySpace())
        {
            GameObject destroyObj = item.gameObject;

            if (all)
            {
                inventory.AddItem(item.item.ID, item.amount, item.customData);
                RemoveItem(item, true);
                Destroy(destroyObj);
            }
            else
            {
                if (item.item.itemType != ItemType.Weapon && item.item.itemType != ItemType.Bullets)
                {
                    if (item.amount == 1)
                    {
                        Destroy(destroyObj);
                    }

                    inventory.AddItem(item.item.ID, 1, item.customData);
                    RemoveItem(item, false);
                }
                else
                {
                    inventory.AddItem(item.item.ID, item.amount, item.customData);
                    RemoveItem(item, true);
                    Destroy(destroyObj);
                }
            }
        }
        else
        {
            inventory.ResetInventory();
            inventory.ShowNotification("No Space in Inventory!");
        }
    }

    public void AddItemAmount(Item item, int amount)
    {
        ContainerItemData itemData = ContainterItemsData.SingleOrDefault(citem => citem.item.ID == item.ID);
        itemData.amount += amount;
        inventory.GetContainerItem(item.ID).amount = itemData.amount;
    }

    private void RemoveItem(ContainerItem containerItem, bool all)
    {
        int itemIndex = inventory.ContainterItemsCache.IndexOf(containerItem);

        if (all)
        {
            ContainterItemsData.RemoveAt(itemIndex);
            inventory.ContainterItemsCache.RemoveAt(itemIndex);
        }
        else
        {
            if (ContainterItemsData[itemIndex].amount > 1)
            {
                ContainterItemsData[itemIndex].amount--;
                inventory.ContainterItemsCache[itemIndex].amount--;
            }
            else
            {
                ContainterItemsData.RemoveAt(itemIndex);
                inventory.ContainterItemsCache.RemoveAt(itemIndex);
            }
        }
    }

    public int GetContainerCount()
    {
        return ContainterItemsData.Count;
    }

    public bool ContainsItemID(int id)
    {
        return ContainterItemsData.Any(citem => citem.item.ID == id);
    }

    public Dictionary<string, object> OnSave()
    {
        if (ContainterItemsData.Count > 0)
        {
            Dictionary<string, object> containerData = new Dictionary<string, object>();

            foreach (var item in ContainterItemsData)
            {
                containerData.Add(item.item.ID.ToString(), new Dictionary<string, object> { { "item_amount", item.amount }, { "item_custom", item.customData } });
            }

            return containerData;
        }
        else
        {
            return null;
        }
    }

    public void OnLoad(JToken token)
    {
        if (token != null && token.HasValues)
        {
            foreach (var item in token.ToObject<Dictionary<int, JToken>>())
            {
                ContainterItemsData.Add(new ContainerItemData(inventory.GetItem(item.Key), (int)item.Value["item_amount"], item.Value["item_custom"].ToObject<CustomItemData>()));
            }
        }
    }
}

public class ContainerItemData
{
    public Item item;
    public int amount;
    public CustomItemData customData;

    public ContainerItemData(Item item, int amount, CustomItemData customData = null)
    {
        this.item = item;
        this.amount = amount;
        this.customData = customData;
    }
}
