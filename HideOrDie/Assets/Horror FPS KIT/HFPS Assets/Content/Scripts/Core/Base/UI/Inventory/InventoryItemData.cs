/*
 * InventoryItemData.cs - script by ThunderWire Games
 * ver. 1.3
*/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using ThunderWire.Helpers;

public class InventoryItemData : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler 
{
    private Inventory inventory;

    public Item item;
    public CustomItemData customData = new CustomItemData();

    [ReadOnly(true)]
    public int itemID;
    [ReadOnly(true)]
    public int slotID;

    public string itemTitle;
    public int itemAmount;
    public string shortcut;

    [HideInInspector]
    public Image ShortcutImg;

    [HideInInspector]
    public Text textAmount;

    [HideInInspector]
	public bool isDisabled;

    [HideInInspector]
    public bool isMoving;

    private Vector2 offset;
    private bool itemDrag;

    public void InitializeData()
    {
        textAmount = transform.parent.GetChild(0).GetChild(0).GetComponent<Text>();
        itemTitle = item.Title;
        itemID = item.ID;
    }

    void Awake()
    {
        inventory = Inventory.Instance;
        ShortcutImg = transform.GetChild(0).GetComponent<Image>();
    }

    void Start()
	{
        inventory = Inventory.Instance;
        transform.position = transform.parent.position;
    }

	void Update()
	{
        if (ShortcutImg && !string.IsNullOrEmpty(shortcut) && !itemDrag)
        {
            ShortcutImg.enabled = true;
        }
        else
        {
            ShortcutImg.enabled = false;
        }

        if (itemDrag) return;

        if ((textAmount = transform.parent.GetChild(0).GetChild(0).GetComponent<Text>()) != null)
        {
            if (item.itemType == ItemType.Bullets || item.itemType == ItemType.Weapon)
            {
                textAmount.text = itemAmount.ToString();
            }
            else
            {
                if (itemAmount > 1)
                {
                    textAmount.text = itemAmount.ToString();
                }
                else if (itemAmount == 1)
                {
                    textAmount.text = string.Empty;
                }
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
		if (item != null && !isDisabled)
        {
            itemDrag = true;
            offset = eventData.position - new Vector2(transform.position.x, transform.position.y);
            transform.SetParent(transform.parent.parent.parent);
            transform.position = eventData.position - offset;
			GetComponent<CanvasGroup> ().blocksRaycasts = false;
            inventory.ResetInventory();
            inventory.isDragging = true;

            if(textAmount) textAmount.text = string.Empty;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
		if (item != null && !isDisabled)
        {
            transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDisabled)
        {
            transform.SetParent(inventory.Slots[slotID].transform);
            transform.position = inventory.Slots[slotID].transform.position;
            GetComponent<CanvasGroup>().blocksRaycasts = true;
            inventory.ResetInventory();
            inventory.isDragging = false;
            itemDrag = false;
        }
    }
}

public class CustomItemData
{
    public Dictionary<string, string> dataDictionary;
    public bool canUse;
    public bool canCombine;

    public CustomItemData() 
    {
        dataDictionary = new Dictionary<string, string>();
        canUse = true;
        canCombine = true;
    }

    public CustomItemData(Dictionary<string, string> customData)
    {
        dataDictionary = customData;
        canUse = true;
        canCombine = true;
    }

    public bool Exist(string key)
    {
        return dataDictionary.ContainsKey(key);
    }

    public string Get(string key)
    {
        if (Exist(key))
        {
            return dataDictionary[key];
        }

        return default;
    }

    public T Get<T>(string key)
    {
        if (Exist(key)){
            return Parser.Convert<T>(dataDictionary[key]);
        }

        return default;
    }
}
