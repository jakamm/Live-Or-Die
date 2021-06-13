/*
 * Inventory.cs - by ThunderWire Studio
 * ver. 1.6.1
 * 
 * The most complex script in whole asset :)
 * 
 * Bugs please report here: thunderwiregames@gmail.com
*/

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using ThunderWire.Helpers;
using ThunderWire.Utility;
using ThunderWire.CrossPlatform.Input;

/// <summary>
/// Main Inventory Script
/// </summary>
public class Inventory : Singleton<Inventory> {

    public const string ITEM_VALUE = "Value";
    public const string ITEM_PATH = "Path";
    public const string ITEM_TAG = "Tag";

    private HFPS_GameManager gameManager;
    private ScriptManager scriptManager;
    private HealthManager healthManager;
    private InventoryContainer currentContainer;
    private ObjectiveManager objectives;
    private UIFader fader;
    private Device currentInputDevice;

    [HideInInspector]
    public CrossPlatformInput input;

    [Tooltip("Database of all inventory items.")]
    public InventoryScriptable inventoryDatabase;

    [Header("Panels")]
    public GameObject ContainterPanel;
    public GameObject ItemInfoPanel;

    [Header("Contents")]
    public GameObject SlotsContent;
    public GameObject ContainterContent;

    [Space(7)]
    public Text ItemLabel;
    public Text ItemDescription;
    public Text ContainerNameText;
    public Text ContainerEmptyText;
    public Text ContainerCapacityText;
    public Image InventoryNotification;

    [Header("Cross-Platform")]
    public GameObject ButtonsInfoPC;
    public GameObject ButtonsInfoConsole;
    public string[] shortcutActions;

    [Header("Contex Menu")]
    public GameObject contexMenu;
    public InventoryContex contexUse;
    public InventoryContex contexCombine;
    public InventoryContex contexExamine;
    public InventoryContex contexDrop;
    public InventoryContex contexStore;
    public InventoryContex contexShortcut;
    public InventoryContex contexRemovable;

    [Header("Contex Coloring")]
    public Color NormalContex = Color.white;
    public Color SelectedContex = Color.white;

    [Header("Inventory Prefabs")]
    public GameObject inventorySlot;
    public GameObject inventoryItem;
    public GameObject containerItem;

    [Header("Slot Settings")]
    public Sprite slotWithItem;
    public Sprite slotSelected;
    public Sprite slotFrameEmpty;
    public Sprite slotFrameItem;

    [Header("Inventory Coloring")]
    public Color slotDisabled = Color.white;
    public Color itemDisabled = Color.white;

    [Header("Inventory Items")]
    public int slotAmount;
    public int slotsInRow;
    public int maxSlots = 16;

    [Header("Inventory Settings")]
    public bool takeBackOneByOne;
    public int itemDropStrength = 10;

    #region Hidden Variables
    [HideInInspector] public bool isInventoryShown;
    [HideInInspector] public bool isDragging;
    [HideInInspector] public bool isStoring;
    [HideInInspector] public bool isSelecting;
    [HideInInspector] public bool isContexVisible;
    [HideInInspector] public int selectedSlotID;
    [HideInInspector] public int selectedSwitcherID = -1;

    [HideInInspector]
    public ItemSwitcher itemSwitcher;

    [HideInInspector]
    public InventoryItemData itemToMove;

    [HideInInspector]
    public List<GameObject> Slots = new List<GameObject>();

    [HideInInspector]
    public List<ShortcutModel> Shortcuts = new List<ShortcutModel>();

    [HideInInspector]
    public List<ContainerItemData> FixedContainerData = new List<ContainerItemData>();

    [HideInInspector]
    public List<ContainerItem> ContainterItemsCache = new List<ContainerItem>();
    #endregion

    #region Private Variables
    private int selectedContex;
    private int selectedBind;
    private string bindControl;

    private bool fadeNotification;
    private bool isContainerFixed;
    private bool isNavDisabled;
    private bool isNavContainer;
    private bool isShortcutBind;
    private bool isBindPressed;

    private InventorySlot firstCandidate;
    private ContainerItem selectedCoItem;
    private MonoBehaviour selectedScript;

    private List<SlotGrid> SlotsGrid = new List<SlotGrid>();
    private List<Item> AllItems = new List<Item>();
    private List<InventoryItem> ItemsCache = new List<InventoryItem>();
    private List<InventoryContex> InventoryContexts = new List<InventoryContex>();
    #endregion

    void Awake()
    {
        input = CrossPlatformInput.Instance;
        input.OnInputsInitialized += OnInputsInitialized;
        gameManager = GetComponent<HFPS_GameManager>();
        objectives = GetComponent<ObjectiveManager>();
        scriptManager = ScriptManager.Instance;
        healthManager = PlayerController.Instance.GetComponent<HealthManager>();
        itemSwitcher = scriptManager.GetScript<ItemSwitcher>();
        fader = new UIFader();

        if (!inventoryDatabase) { Debug.LogError("Inventory Database was not set!"); return; }

        for (int i = 0; i < inventoryDatabase.ItemDatabase.Count; i++)
        {
            AllItems.Add(new Item(i, inventoryDatabase.ItemDatabase[i]));
        }

        int row = 0;
        int column = 0;

        for (int i = 0; i < slotAmount; i++)
        {
            GameObject slot = Instantiate(inventorySlot);
            InventorySlot sc = slot.GetComponent<InventorySlot>();
            slot.transform.SetParent(SlotsContent.transform);
            slot.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            SlotsGrid.Add(new SlotGrid(row, column, i));
            Slots.Add(slot);
            sc.inventory = this;
            sc.slotID = i;

            if (column >= slotsInRow - 1)
            {
                column = 0;
                row++;
            }
            else
            {
                column++;
            }
        }
    }

    void Start()
    {
        ItemLabel.text = string.Empty;
        ItemDescription.text = string.Empty;
        ShowContexMenu(false);
        ItemInfoPanel.SetActive(false);
        selectedContex = 0;
        selectedSlotID = -1;
    }

    void OnInputsInitialized(Device device)
    {
        currentInputDevice = device;

        if (ButtonsInfoPC && ButtonsInfoConsole)
        {
            if (device == Device.Keyboard)
            {
                ButtonsInfoPC.SetActive(true);
                ButtonsInfoConsole.SetActive(false);
            }
            else if (device == Device.Gamepad)
            {
                ButtonsInfoPC.SetActive(false);
                ButtonsInfoConsole.SetActive(true);
            }
        }
    }

    void Update()
    {
        if (itemSwitcher)
        {
            selectedSwitcherID = itemSwitcher.currentItem;
        }

        isInventoryShown = gameManager.TabButtonPanel.activeSelf;

        if (!isInventoryShown)
        {
            if (!isNavDisabled)
            {
                EventSystem.current.SetSelectedGameObject(null);
                ItemInfoPanel.SetActive(false);
                ShowContexMenu(false);
                ResetSlotProperties();
                StopAllCoroutines();

                foreach (var item in ContainterContent.GetComponentsInChildren<ContainerItem>())
                {
                    Destroy(item.gameObject);
                }

                if (currentContainer)
                {
                    currentContainer.isOpened = false;
                    currentContainer = null;
                }

                ContainterPanel.SetActive(false);
                objectives.ShowObjectives(true);

                foreach (var slot in Slots)
                {
                    slot.GetComponent<InventorySlot>().isSelectable = true;
                    slot.GetComponent<InventorySlot>().contexVisible = false;
                }

                if (ContainterItemsCache.Count > 0)
                {
                    foreach (var item in ContainterItemsCache)
                    {
                        item.Deselect();
                    }
                }

                ContainterItemsCache.Clear();
                InventoryNotification.gameObject.SetActive(false);

                if (itemToMove)
                {
                    itemToMove.isMoving = false;
                    itemToMove = null;
                }

                selectedContex = 0;
                selectedSlotID = -1;
                isSelecting = false;
                isStoring = false;
                isNavContainer = false;
                isShortcutBind = false;
                isContainerFixed = false;
                isContexVisible = false;
                fadeNotification = false;

                fader.fadeOut = true;
                selectedScript = null;
                isNavDisabled = true;
            }
        }
        else
        {
            isNavDisabled = false;
        }

        if (currentContainer != null || isContainerFixed)
        {
            if (isContainerFixed)
            {
                selectedCoItem = ContainterItemsCache.SingleOrDefault(item => item.IsSelected());
            }
            else if(currentContainer.IsSelecting())
            {
                selectedCoItem = currentContainer.GetSelectedItem();
            }
            else
            {
                selectedCoItem = null;
            }

            if (!isContainerFixed)
            {
                if (currentContainer.GetContainerCount() < 1)
                {
                    ContainerEmptyText.text = currentContainer.containerName.TitleCase() + " is Empty!";
                    ContainerEmptyText.gameObject.SetActive(true);
                }

                ContainerCapacityText.text = string.Format("Capacity {0}/{1}", currentContainer.GetContainerCount(), currentContainer.containerSpace);
            }
            else
            {
                if (FixedContainerData.Count < 1)
                {
                    ContainerEmptyText.gameObject.SetActive(true);
                }

                ContainerCapacityText.text = string.Format("Items Count: {0}", FixedContainerData.Count);
            }
        }

        if (isShortcutBind && selectedBind == selectedSlotID && selectedBind > -1 && input)
        {
            string pressed = input.SelectActionSpecific(shortcutActions);

            if (input && !string.IsNullOrEmpty(pressed) && !isBindPressed)
            {
                bindControl = pressed;
                isBindPressed = true;
            }
            else if (isBindPressed)
            {
                var control = input.ControlOf(bindControl);
                InventoryItemData itemData = GetSlotItemData(selectedSlotID);
                ShortcutBind(itemData.itemID, itemData.slotID, control.Control);
                bindControl = string.Empty;
                isBindPressed = false;
            }
        }
        else
        {
            if (Shortcuts.Count > 0 && !isDragging && !itemToMove)
            {
                for (int i = 0; i < Shortcuts.Count; i++)
                {
                    int slotID = Shortcuts[i].slot;

                    if (!HasSlotItem(slotID))
                    {
                        Shortcuts.RemoveAt(i);
                        break;
                    }

                    if (input.GetControlPressedOnce(this, Shortcuts[i].shortcut))
                    {
                        UseItem(Shortcuts[i].slot);
                    }
                }
            }

            //fader.fadeOut = true;
            isShortcutBind = false;
            selectedBind = -1;
        }

        if (!fader.fadeCompleted && fadeNotification)
        {
            Color colorN = InventoryNotification.color;
            Color colorT = InventoryNotification.transform.GetComponentInChildren<Text>().color;
            colorN.a = fader.GetFadeAlpha();
            colorT.a = fader.GetFadeAlpha();
            InventoryNotification.color = colorN;
            InventoryNotification.transform.GetComponentInChildren<Text>().color = colorT;
        }
        else
        {
            InventoryNotification.gameObject.SetActive(false);
            fadeNotification = false;
        }
    }

    #region PlayerInput Callbacks
    InventorySlot GetCloseSlot(int selected, bool nextLine)
    {
        InventorySlot slot = null;
        int lineSlot = 0;

        if (nextLine)
        {
            if ((selected + slotsInRow) < slotAmount)
            {
                lineSlot = selected + slotsInRow;
            }
            else
            {
                return null;
            }
        }
        else
        {
            if ((selected - slotsInRow) >= 0)
            {
                lineSlot = selected - slotsInRow;
            }
            else
            {
                return null;
            }
        }

        if (GetSlot(lineSlot).itemData != null)
        {
            return GetSlot(lineSlot);
        }

        SlotGrid lb = SlotsGrid.Where(x => x.slotID == lineSlot).FirstOrDefault();
        SlotGrid[] lineSlots = SlotsGrid.Where(x => x.row == lb.row).ToArray();
        int leftSteps = 0, rightSteps = 0;
        int leftCloseID = -1, rightCloseID = -1;

        for (int i = lb.column; i >= 0; i--)
        {
            InventorySlot cl = GetSlot(lineSlots[i].slotID);
            if (cl.isSelectable && cl.itemData)
            {
                leftCloseID = lineSlots[i].slotID;
                break;
            }

            leftSteps++;
        }

        for (int i = lb.column; i < lineSlots.Length; i++)
        {
            InventorySlot cl = GetSlot(lineSlots[i].slotID);
            if (cl.isSelectable && cl.itemData)
            {
                rightCloseID = lineSlots[i].slotID;
                break;
            }

            rightSteps++;
        }

        if (leftCloseID >= 0 && rightCloseID >= 0 && leftSteps < rightSteps)
        {
            slot = GetSlot(leftCloseID);
        }
        else if (leftCloseID >= 0 && rightCloseID >= 0 && leftSteps > rightSteps)
        {
            slot = GetSlot(rightCloseID);
        }
        else
        {
            if (rightSteps == leftSteps && leftCloseID >= 0 && rightCloseID >= 0)
            {
                slot = GetSlot(leftCloseID >= 0 ? leftCloseID : rightCloseID);
            }
            else
            {
                if (leftCloseID >= 0)
                {
                    slot = GetSlot(leftCloseID);
                }
                else if (rightCloseID >= 0)
                {
                    slot = GetSlot(rightCloseID);
                }
            }
        }

        return slot;
    }

    void OnNavigateAlt(InputValue value)
    {
        Vector2 move = value.Get<Vector2>();

        if (!isNavDisabled)
        {
            if (!isNavContainer)
            {
                if (selectedSlotID < 0 && move.magnitude > 0)
                {
                    if ((currentContainer != null || isContainerFixed) && move.x < 0 && ContainterItemsCache.Count > 0)
                    {
                        isNavContainer = true;
                        ContainterItemsCache[0].Select();
                    }
                    else
                    {
                        if (AnyInventroy())
                        {
                            InventorySlot slot = GetSlotWitItem();
                            if (slot != null) slot.Select();
                        }
                        else if (ContainterItemsCache.Count > 0)
                        {
                            isNavContainer = true;
                            ContainterItemsCache[0].Select();
                        }
                    }
                }
                else
                {
                    if (!isContexVisible)
                    {
                        if (itemToMove)
                        {
                            int curr = itemToMove.slotID;

                            if (move.x > 0 && move.y == 0)
                            {
                                if (curr < slotAmount - 1)
                                {
                                    Slots[curr + 1].GetComponent<InventorySlot>().PutItem(itemToMove.gameObject);
                                }
                            }
                            else if (move.x < 0 && move.y == 0)
                            {
                                if (curr > 0)
                                {
                                    Slots[curr - 1].GetComponent<InventorySlot>().PutItem(itemToMove.gameObject);
                                }
                            }
                            else if (move.y > 0 && move.x == 0)
                            {
                                if ((curr - slotsInRow) >= 0)
                                {
                                    Slots[curr - slotsInRow].GetComponent<InventorySlot>().PutItem(itemToMove.gameObject);
                                }
                            }
                            else if (move.y < 0 && move.x == 0)
                            {
                                if ((curr + slotsInRow) <= slotAmount - 1)
                                {
                                    Slots[curr + slotsInRow].GetComponent<InventorySlot>().PutItem(itemToMove.gameObject);
                                }
                            }
                        }
                        else if (selectedSlotID >= 0)
                        {
                            if (move.x > 0 && move.y == 0)
                            {
                                if (selectedSlotID < slotAmount - 1)
                                {
                                    for (int i = selectedSlotID + 1; i < slotAmount; i++)
                                    {
                                        InventorySlot close = GetSlot(i);
                                        if (close.isSelectable && close.itemData)
                                        {
                                            close.Select();
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (move.x < 0 && move.y == 0)
                            {
                                //Select slot item or container item if container view is visible
                                if (currentContainer == null && !isContainerFixed)
                                {
                                    //Select slot item
                                    if (selectedSlotID > 0)
                                    {
                                        for (int i = selectedSlotID - 1; i >= 0; i--)
                                        {
                                            InventorySlot close = GetSlot(i);
                                            if (close.isSelectable && close.itemData)
                                            {
                                                close.Select();
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //Select slot item or contaier item if selected slot id equals zero or there are no next slot items
                                    if (selectedSlotID >= 1)
                                    {
                                        for (int i = selectedSlotID - 1; i >= 0; i--)
                                        {
                                            InventorySlot next = GetSlot(i);

                                            if(next.isSelectable && next.itemData)
                                            {
                                                next.Select();
                                                break;
                                            }
                                            else if(i <= 0 && ContainterItemsCache.Count > 0)
                                            {
                                                isNavContainer = true;
                                                ResetInventory();
                                                ContainterItemsCache[0].Select();
                                            }
                                        }
                                    }
                                    else if(ContainterItemsCache.Count > 0)
                                    {
                                        isNavContainer = true;
                                        ResetInventory();
                                        ContainterItemsCache[0].Select();
                                    }
                                }
                            }
                            else if (move.y > 0 && move.x == 0)
                            {
                                InventorySlot slot = GetCloseSlot(selectedSlotID, false);
                                if (slot != null) slot.Select();
                            }
                            else if (move.y < 0 && move.x == 0)
                            {
                                InventorySlot slot = GetCloseSlot(selectedSlotID, true);
                                if (slot != null) slot.Select();
                            }
                        }
                    }
                    else
                    {
                        if (move.y > 0 && move.x == 0)
                        {
                            InventoryContexts[selectedContex].Deselect();
                            selectedContex = selectedContex > 0 ? selectedContex - 1 : 0;
                            InventoryContexts[selectedContex].Select();
                        }
                        else if (move.y < 0 && move.x == 0)
                        {
                            InventoryContexts[selectedContex].Deselect();
                            selectedContex = selectedContex < InventoryContexts.Count - 1 ? selectedContex + 1 : InventoryContexts.Count - 1;
                            InventoryContexts[selectedContex].Select();
                        }
                    }
                }
            }
            else
            {
                if(move.x > 0 && move.y == 0 && AnyInventroy())
                {
                    if (selectedCoItem != null) selectedCoItem.Deselect();
                    InventorySlot slot = GetSlotWitItem();
                    if (slot != null) slot.Select();
                    isNavContainer = false;
                }
                else if(move.y < 0 && selectedCoItem != null)
                {
                    int id = ContainterItemsCache.IndexOf(selectedCoItem);
                    selectedCoItem.Deselect();
                    ContainterItemsCache[id < ContainterItemsCache.Count - 1 ? id + 1 : 0].Select();
                }
                else if(move.y > 0 && selectedCoItem != null)
                {
                    int id = ContainterItemsCache.IndexOf(selectedCoItem);
                    selectedCoItem.Deselect();
                    ContainterItemsCache[id > 0 ? id - 1 : ContainterItemsCache.Count - 1].Select();
                }
            }
        }
    }

    void OnSubmit()
    {
        if (!isNavDisabled)
        {
            if (selectedCoItem != null)
            {
                TakeBackToInventory(selectedCoItem);
            }
            else
            {
                if (itemToMove)
                {
                    foreach (var sobj in Slots)
                    {
                        sobj.GetComponent<InventorySlot>().isSelectable = true;
                        sobj.GetComponent<InventorySlot>().itemIsMoving = false;
                    }

                    fader.fadeOut = true;
                    itemToMove.isMoving = false;
                    itemToMove = null;
                }
                else if (selectedSlotID >= 0)
                {
                    InventorySlot slot = GetSlot(selectedSlotID);

                    if (!slot.contexVisible && !slot.itemIsMoving && slot.isSelectable && slot.isSelected)
                    {
                        if (!slot.isCombineCandidate && !slot.isItemSelect)
                        {
                            slot.ShowContext();
                        }
                        else
                        {
                            slot.CombineSelect();
                        }
                    }
                    else if (isContexVisible)
                    {
                        InventoryContexts[selectedContex].Click();
                    }
                }
            }
        }
    }

    void OnInventoryMove()
    {
        if (!isNavDisabled && selectedSlotID >= 0 && !itemToMove && selectedScript == null)
        {
            InventorySlot slot = GetSlot(selectedSlotID);

            if (!slot.itemIsMoving && slot.isSelected && slot.isSelectable && !slot.contexVisible)
            {
                SetSlotsState(false, slot.gameObject);
                itemToMove = slot.itemData;
                slot.itemData.isMoving = true;
                ShowNotificationFixed("Move selected item where do you want.");

                foreach (var sobj in Slots)
                {
                    sobj.GetComponent<InventorySlot>().itemIsMoving = true;
                }
            }
        }
    }

    void OnCancel()
    {
        if (!isNavDisabled)
        {
            if (itemToMove == null && !isContexVisible && !isShortcutBind)
            {
                gameManager.ShowInventory(false);
                isContainerFixed = false;
            }

            EventSystem.current.SetSelectedGameObject(null);
            SetSlotsState(true);
            ShowContexMenu(false);
            fader.fadeOut = true;
            isContexVisible = false;
            isBindPressed = false;
            isShortcutBind = false;

            itemToMove = null;
            bindControl = string.Empty;
            selectedBind = -1;

            foreach (var sobj in Slots)
            {
                sobj.GetComponent<InventorySlot>().contexVisible = false;
                sobj.GetComponent<InventorySlot>().isSelectable = true;
                sobj.GetComponent<InventorySlot>().itemIsMoving = false;
            }
        }
    }
    #endregion

    /// <summary>
    /// Deselect current selected Item
    /// </summary>
    public void DeselectContainerItem()
    {
        if (currentContainer != null && currentContainer.IsSelecting())
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// Callback to Take Back Item from Inventory Container
    /// </summary>
    public void TakeBackToInventory(ContainerItem coitem)
    {
        if (coitem == null) return;

        if (!isContainerFixed)
        {
            currentContainer.TakeBack(!takeBackOneByOne, coitem);
            isNavContainer = false;
        }
        else
        {
            if (CheckInventorySpace())
            {
                GameObject destroyObj = coitem.gameObject;
                Item containerItem = coitem.item;

                if (!takeBackOneByOne)
                {
                    AddItem(containerItem.ID, coitem.amount, coitem.customData);

                    if (containerItem.isStackable)
                    {
                        FixedContainerData.RemoveAll(x => x.item.ID == coitem.item.ID);
                        ContainterItemsCache.RemoveAll(x => x.item.ID == coitem.item.ID);
                        Destroy(destroyObj);
                    }
                    else
                    {
                        int itemIndex = ContainterItemsCache.IndexOf(coitem);
                        FixedContainerData.RemoveAt(itemIndex);
                        ContainterItemsCache.RemoveAt(itemIndex);
                        Destroy(destroyObj);
                    }
                }
                else
                {
                    int itemIndex = ContainterItemsCache.IndexOf(coitem);

                    if (containerItem.itemType != ItemType.Weapon && containerItem.itemType != ItemType.Bullets)
                    {
                        AddItem(containerItem.ID, 1, coitem.customData);

                        if (coitem.amount == 1)
                        {
                            FixedContainerData.RemoveAt(itemIndex);
                            ContainterItemsCache.RemoveAt(itemIndex);
                            Destroy(destroyObj);
                        }
                        else
                        {
                            FixedContainerData[itemIndex].amount--;
                            coitem.amount--;
                        }
                    }
                    else
                    {
                        AddItem(containerItem.ID, coitem.amount, coitem.customData);
                        FixedContainerData.RemoveAt(itemIndex);
                        ContainterItemsCache.RemoveAt(itemIndex);
                        Destroy(destroyObj);
                    }
                }

                isNavContainer = false;
            }
            else
            {
                ResetInventory();
                ShowNotification("No Space in Inventory!");
            }
        }
    }

    /// <summary>
    /// Function to show normal Inventory Container
    /// </summary>
    public void ShowInventoryContainer(InventoryContainer container, ContainerItemData[] containerItems, string name = "CONTAINER")
    {
        if (!string.IsNullOrEmpty(name))
        {
            ContainerNameText.text = name.ToUpper();
        }
        else
        {
            ContainerNameText.text = "CONTAINER";
        }

        if (containerItems.Length > 0)
        {
            ContainerEmptyText.gameObject.SetActive(false);

            foreach (var citem in containerItems)
            {
                GameObject coItem = Instantiate(containerItem, ContainterContent.transform);
                ContainerItem item = coItem.GetComponent<ContainerItem>();
                item.item = citem.item;
                item.amount = citem.amount;
                item.customData = citem.customData;
                coItem.name = "CoItem_" + citem.item.Title.Replace(" ", "");
                ContainterItemsCache.Add(coItem.GetComponent<ContainerItem>());
            }
        }
        else
        {
            ContainerEmptyText.text = name.TitleCase() + " is Empty!";
            ContainerEmptyText.gameObject.SetActive(true);
        }

        isContainerFixed = false;
        currentContainer = container;
        objectives.ShowObjectives(false);
        ContainterPanel.SetActive(true);
        gameManager.ShowInventory(true);
        isStoring = true;
    }

    public Dictionary<int, Dictionary<string, object>> GetFixedContainerData()
    {
        return FixedContainerData.ToDictionary(x => x.item.ID, y => new Dictionary<string, object> { { "item_amount", y.amount }, { "item_custom", y.customData } });
    }

    /// <summary>
    /// Function to show Fixed Container
    /// </summary>
    public void ShowFixedInventoryContainer(string name = "CONTAINER")
    {
        if (!string.IsNullOrEmpty(name))
        {
            ContainerNameText.text = name.ToUpper();
        }
        else
        {
            ContainerNameText.text = "CONTAINER";
        }

        if (FixedContainerData.Count > 0)
        {
            ContainerEmptyText.gameObject.SetActive(false);

            foreach (var citem in FixedContainerData)
            {
                GameObject coItem = Instantiate(containerItem, ContainterContent.transform);
                ContainerItem item = coItem.GetComponent<ContainerItem>();
                item.item = citem.item;
                item.amount = citem.amount;
                item.customData = citem.customData;
                coItem.name = "CoItem_" + citem.item.Title.Replace(" ", "");
                ContainterItemsCache.Add(coItem.GetComponent<ContainerItem>());
            }
        }
        else
        {
            ContainerEmptyText.text = name.TitleCase() + " is Empty!";
            ContainerEmptyText.gameObject.SetActive(true);
        }

        isContainerFixed = true;
        objectives.ShowObjectives(false);
        ContainterPanel.SetActive(true);
        gameManager.ShowInventory(true);
        isStoring = true;
    }

    /// <summary>
    /// Callback for UI Store Button
    /// </summary>
    public void StoreSelectedItem()
    {
        if (selectedSlotID < 0) return;

        InventoryItemData itemData = GetSlotItemData(selectedSlotID);

        if (!isContainerFixed)
        {
            if (selectedSlotID != -1 && currentContainer != null)
            {
                if (currentContainer.ContainsItemID(itemData.item.ID) && itemData.item.isStackable)
                {
                    currentContainer.AddItemAmount(itemData.item, itemData.itemAmount);
                    RemoveSelectedItem(true);
                }
                else
                {
                    if (currentContainer.GetContainerCount() < currentContainer.containerSpace)
                    {
                        ContainerEmptyText.gameObject.SetActive(false);
                        currentContainer.StoreItem(itemData.item, itemData.itemAmount, itemData.customData);

                        if (itemSwitcher.currentItem == itemData.item.useSwitcherID)
                        {
                            itemSwitcher.DeselectItems();
                        }

                        RemoveSelectedItem(true);
                    }
                    else
                    {
                        ShowNotification("No Space in Container!");
                        ResetInventory();
                    }
                }
            }
        }
        else
        {
            if (selectedSlotID != -1)
            {
                if (FixedContainerData.Any(item => item.item.ID == itemData.item.ID) && itemData.item.isStackable)
                {
                    foreach (var item in FixedContainerData)
                    {
                        if(item.item.ID == itemData.item.ID)
                        {
                            item.amount += itemData.itemAmount;
                            GetContainerItem(itemData.item.ID).amount = item.amount;
                        }
                    }
                }
                else
                {
                    ContainerEmptyText.gameObject.SetActive(false);
                    StoreFixedContainerItem(itemData.item, itemData.itemAmount, itemData.customData);

                    if (itemSwitcher.currentItem == itemData.item.useSwitcherID)
                    {
                        itemSwitcher.DeselectItems();
                    }
                }

                RemoveSelectedItem(true);
            }
        }
    }

    void StoreFixedContainerItem(Item item, int amount, CustomItemData custom)
    {
        GameObject coItem = Instantiate(containerItem, ContainterContent.transform);
        ContainerItem citem = coItem.GetComponent<ContainerItem>();
        citem.inventoryContainer = null;
        citem.item = item;
        citem.amount = amount;
        citem.customData = custom;
        coItem.name = "CoItem_" + item.Title.Replace(" ", "");
        FixedContainerData.Add(new ContainerItemData(item, amount, custom));
        ContainterItemsCache.Add(coItem.GetComponent<ContainerItem>());
    }

    /// <summary>
    /// Get UI ContainerItem Object
    /// </summary>
    public ContainerItem GetContainerItem(int id)
    {
        foreach (var item in ContainterItemsCache)
        {
            if(item.item.ID == id)
            {
                return item;
            }
        }

        Debug.LogError($"Item with ID ({id}) does not found!");
        return null;
    }

    /// <summary>
    /// Start Shortcut Bind process
    /// </summary>
    public void BindShortcutItem()
    {
        if (shortcutActions.Length > 0)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                Slots[i].GetComponent<InventorySlot>().isSelected = false;
                Slots[i].GetComponent<InventorySlot>().contexVisible = false;
            }

            ShowContexMenu(false);

            if (currentInputDevice == Device.Keyboard)
            {
                ShowNotificationFixed("Select 1, 2, 3, 4 to bind item shortcut.");
            }
            else if (currentInputDevice == Device.Gamepad)
            {
                ShowNotificationFixed("Select Dpad Button to bind item shortcut.");
            }

            selectedBind = selectedSlotID;
            isShortcutBind = true;
        }
        else
        {
            Debug.LogError("[Shortcut Bind] ShortcutActions are Empty!");
        }
    }

    /// <summary>
    /// Bind new or Exchange Inventory Shortcut
    /// </summary>
    public void ShortcutBind(int itemID, int slotID, string control)
    {
        Item item = GetItem(itemID);

        if (Shortcuts.Count > 0) {
            if (Shortcuts.All(s => s.slot != slotID && !s.shortcut.Equals(control)))
            {
                //Shortcut does not exist
                Shortcuts.Add(new ShortcutModel(item, slotID, control));
                GetSlotItemData(slotID).shortcut = control;
            }
            else
            {
                //Shortcut already exist
                for (int i = 0; i < Shortcuts.Count; i++)
                {
                    if (Shortcuts.Any(s => s.slot == slotID))
                    {
                        if (Shortcuts[i].slot == slotID)
                        {
                            //Change shortcut key
                            if (Shortcuts.Any(s => s.shortcut.Equals(control)))
                            {
                                //Find equal shortcut with key and exchange it
                                foreach (var equal in Shortcuts)
                                {
                                    if (equal.shortcut.Equals(control))
                                    {
                                        equal.shortcut = Shortcuts[i].shortcut;
                                        GetSlotItemData(equal.slot).shortcut = Shortcuts[i].shortcut;
                                    }
                                }
                            }

                            //Change actual shortcut key
                            Shortcuts[i].shortcut = control;
                            GetSlotItemData(Shortcuts[i].slot).shortcut = Shortcuts[i].shortcut;
                            break;
                        }
                    }
                    else if (Shortcuts[i].shortcut.Equals(control))
                    {
                        //Change shortcut item
                        GetSlotItemData(Shortcuts[i].slot).shortcut = string.Empty;
                        GetSlotItemData(slotID).shortcut = control;
                        Shortcuts[i].slot = slotID;
                        Shortcuts[i].item = item;
                        break;
                    }
                }
            }
        }
        else
        {
            Shortcuts.Add(new ShortcutModel(item, slotID, control));
            GetSlotItemData(slotID).shortcut = control;
        }

        isShortcutBind = false;
        fader.fadeOut = true;
        ResetInventory();
    }

    /// <summary>
    /// Update Shortcut slot with binded Control
    /// </summary>
    public void UpdateShortcut(string control, int newSlotID)
    {
        foreach (var shortcut in Shortcuts)
        {
            if(shortcut.shortcut.Equals(control))
            {
                shortcut.slot = newSlotID;
            }
        }
    }

    /// <summary>
    /// Automatically Bind Shortcut
    /// </summary>
    /// <returns>Shortcut Action</returns>
    public string AutoBindShortcut(int slotID, int itemID)
    {
        Item item = GetItem(itemID);

        if (HasSlotItem(slotID, itemID) && item.canBindShortcut && item.isUsable)
        {
            if (shortcutActions.Length > 0)
            {
                Dictionary<string, string> avaiableShortcuts = new Dictionary<string, string>();

                foreach (var shortcut in shortcutActions)
                {
                    var control = input.ControlOf(shortcut);

                    if (!string.IsNullOrEmpty(control.Control))
                    {
                        if (!Shortcuts.Select(x => x.shortcut).Any(x => x.Equals(control.Control)))
                        {
                            avaiableShortcuts.Add(shortcut, control.Control);
                        }
                    }
                    else
                    {
                        Debug.LogError($"[AutoBind] Shortcut ({shortcut}) does not exist in control scheme!");
                        break;
                    }
                }

                if (avaiableShortcuts.Count > 0)
                {
                    var newShortcut = avaiableShortcuts.FirstOrDefault();

                    ShortcutBind(itemID, slotID, newShortcut.Value);
                    return newShortcut.Key;
                }
            }
            else
            {
                Debug.LogError("[AutoBind] ShortcutActions are Empty!");
            }
        }
        else
        {
            Debug.LogError("[AutoBind] Cannot bind shortcut!");
        }

        return string.Empty;
    }

    /// <summary>
    /// Function to Open Inventory with Highlighted Items with Select Option.
    /// </summary>
    public void OnInventorySelect(int[] highlight, string[] tags, MonoBehaviour script, string selectText = "", string nullText = "")
    {
        if(highlight.Length > 0 && ItemsCache.Any(x => highlight.Any(y => x.item.ID.Equals(y))))
        {
            selectedScript = script;

            gameManager.ShowInventory(true);

            if(selectText != string.Empty)
            {
                ShowNotificationFixed(selectText);
            }

            for (int i = 0; i < Slots.Count; i++)
            {
                InventorySlot slot = Slots[i].GetComponent<InventorySlot>();

                slot.isSelectable = false;
                slot.isItemSelect = true;

                if (slot.slotItem != null)
                {
                    if (highlight.Any(x => slot.slotItem.ID.Equals(x)))
                    {
                        if (tags.Length > 0)
                        {
                            if (tags.Any(x => slot.itemData.customData.dataDictionary.ContainsValue(x)))
                            {
                                slot.isSelectable = true;
                            }
                        }
                        else
                        {
                            slot.isSelectable = true;
                        }
                    }
                    else
                    {
                        slot.GetComponent<Image>().color = slotDisabled;
                    }
                }
            }

            ShowContexMenu(false);
        }
        else
        {
            if(nullText != string.Empty)
            {
                gameManager.AddSingleMessage(nullText, "NoItems");
            }
        }
    }

    /// <summary>
    /// Get Item from Database
    /// </summary>
    public Item GetItem(int ID)
    {
        return inventoryDatabase.ItemDatabase.Where(item => item.ID == ID).Select(item => new Item(item.ID, item)).SingleOrDefault();
    }

    /// <summary>
    /// Function to add new Item to Inventory Specific Slot.
    /// </summary>
    /// <returns>Auto Shortcut Input</returns>
    public string AddItemToSlot(int slotID, int itemID, int amount = 1, CustomItemData customData = null, bool autoShortcut = false)
    {
        Item itemToAdd = GetItem(itemID);

        if (CheckInventorySpace())
        {
            if (itemToAdd.isStackable && HasSlotItem(slotID, itemID))
            {
                InventoryItemData itemData = GetItemData(itemToAdd.ID, slotID);
                itemData.itemAmount += amount;
            }
            else
            {
                for (int i = 0; i < Slots.Count; i++)
                {
                    if (i == slotID)
                    {
                        GameObject item = Instantiate(inventoryItem, Slots[i].transform);
                        InventoryItemData itemData = item.GetComponent<InventoryItemData>();
                        itemData.item = itemToAdd;
                        itemData.itemAmount = amount;
                        itemData.slotID = i;
                        itemData.InitializeData();
                        if (customData != null) { itemData.customData = customData; }
                        Slots[i].GetComponent<InventorySlot>().slotItem = itemToAdd;
                        Slots[i].GetComponent<InventorySlot>().itemData = itemData;
                        Slots[i].GetComponent<Image>().sprite = slotWithItem;
                        Slots[i].GetComponent<Image>().enabled = true;
                        item.GetComponent<Image>().sprite = itemToAdd.itemSprite;
                        item.GetComponent<RectTransform>().position = Vector2.zero;
                        item.name = itemToAdd.Title;
                        ItemsCache.Add(new InventoryItem(itemToAdd, customData));
                        break;
                    }
                }

                if (autoShortcut)
                {
                    return AutoBindShortcut(slotID, itemID);
                }
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Function to add new Item to Inventory.
    /// </summary>
    /// <returns>Auto Shortcut Input</returns>
    public string AddItem(int itemID, int amount, CustomItemData customData = null, bool autoShortcut = false)
    {
        Item itemToAdd = GetItem(itemID);

        if (CheckInventorySpace() || CheckItemInventory(itemID))
        {
            if (itemToAdd.isStackable && CheckItemInventory(itemToAdd.ID) && GetItemData(itemToAdd.ID) != null)
            {
                InventoryItemData itemData = GetItemData(itemToAdd.ID);
                itemData.itemAmount += amount;
            }
            else
            {
                int slot = -1;

                for (int i = 0; i < Slots.Count; i++)
                {
                    if (Slots[i].transform.childCount == 1)
                    {
                        GameObject item = Instantiate(inventoryItem, Slots[i].transform);
                        InventoryItemData itemData = item.GetComponent<InventoryItemData>();
                        itemData.item = itemToAdd;
                        itemData.itemAmount = amount;
                        itemData.slotID = i;
                        itemData.InitializeData();
                        if (customData != null) { itemData.customData = customData; }
                        Slots[i].GetComponent<InventorySlot>().slotItem = itemToAdd;
                        Slots[i].GetComponent<InventorySlot>().itemData = itemData;
                        Slots[i].GetComponent<Image>().sprite = slotWithItem;
                        Slots[i].GetComponent<Image>().enabled = true;
                        item.GetComponent<Image>().sprite = itemToAdd.itemSprite;
                        item.GetComponent<RectTransform>().position = Vector2.zero;
                        item.name = itemToAdd.Title;
                        ItemsCache.Add(new InventoryItem(itemToAdd, customData));
                        slot = i;
                        break;
                    }
                }

                if (autoShortcut && slot >= 0)
                {
                    return AutoBindShortcut(slot, itemID);
                }
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Remove Item from Slot
    /// </summary>
    public void RemoveSlotItem(int slotID, bool all = false)
    {
        if (slotID >= 0)
        {
            InventoryItemData data = GetSlotItemData(slotID);

            if (data != null)
            {
                Item itemToRemove = data.item;

                if (itemToRemove.isStackable && HasSlotItem(slotID, itemToRemove.ID) && !all)
                {
                    data.itemAmount--;
                    data.textAmount.text = data.itemAmount.ToString();

                    if (data.itemAmount <= 0)
                    {
                        Destroy(Slots[slotID].transform.GetChild(1).gameObject);
                        RemoveFromCache(itemToRemove, data.customData);
                        ResetInventory();
                    }

                    if (data.itemAmount == 1)
                    {
                        data.textAmount.text = "";
                    }
                }
                else
                {
                    Destroy(Slots[slotID].transform.GetChild(1).gameObject);
                    RemoveFromCache(itemToRemove, data.customData);
                    ResetInventory();
                }
            }
        }
    }

    /// <summary>
    /// Remove one or all Item stacks by Item ID
    /// </summary>
    public void RemoveItem(int ID, bool all = false, bool lastItem = false)
    {
        Item itemToRemove = GetItem(ID);
        int slotID;

        if (lastItem)
        {
            slotID = GetItemSlotID(itemToRemove.ID, true);
        }
        else
        {
            slotID = GetItemSlotID(itemToRemove.ID);
        }

        if (slotID >= 0)
        {
            if (itemToRemove.isStackable && CheckItemInventory(itemToRemove.ID) && !all)
            {
                InventoryItemData data = Slots[slotID].GetComponentInChildren<InventoryItemData>();
                data.itemAmount--;
                data.textAmount.text = data.itemAmount.ToString();

                if (data.itemAmount <= 0)
                {
                    Destroy(Slots[slotID].transform.GetChild(1).gameObject);
                    RemoveFromCache(itemToRemove);
                    ResetInventory();
                }

                if (data.itemAmount == 1)
                {
                    data.textAmount.text = "";
                }
            }
            else
            {
                Destroy(Slots[slotID].transform.GetChild(1).gameObject);
                RemoveFromCache(itemToRemove);
                ResetInventory();
            }
        }
    }

    /// <summary>
    /// Remove one or all Item stacks from selected Slot
    /// </summary>
    public void RemoveSelectedItem(bool all = false)
    {
        if (selectedSlotID >= 0)
        {
            int slot = selectedSlotID;
            Item item = GetSlotItem(slot);

            if (item.isStackable && CheckItemInventory(item.ID) && !all)
            {
                InventoryItemData data = Slots[slot].GetComponentInChildren<InventoryItemData>();
                data.itemAmount--;
                data.textAmount.text = data.itemAmount.ToString();

                if (data.itemAmount <= 0)
                {
                    Destroy(Slots[slot].transform.GetChild(1).gameObject);
                    RemoveFromCache(item);
                    ResetInventory();
                }

                if (data.itemAmount == 1)
                {
                    data.textAmount.text = "";
                }
            }
            else
            {
                Destroy(Slots[slot].transform.GetChild(1).gameObject);
                RemoveFromCache(item);
                ResetInventory();
            }
        }
    }

    /// <summary>
    /// Remove specific item amount by ItemID
    /// </summary>
    public void RemoveItemAmount(int ID, int Amount)
    {
        if (CheckItemInventory(ID))
        {
            InventoryItemData data = Slots[GetItemSlotID(ID)].GetComponentInChildren<InventoryItemData>();

            if (data.itemAmount > Amount)
            {
                data.itemAmount = data.itemAmount - Amount;
                data.transform.parent.GetChild(0).GetChild(0).GetComponent<Text>().text = data.itemAmount.ToString();
            }
            else
            {
                RemoveItem(ID, true, true);
            }
        }
    }

    /// <summary>
    /// Remove selected slot Item Amount
    /// </summary>
    public void RemoveSelectedItemAmount(int Amount)
    {
        if (selectedSlotID >= 0)
        {
            int slot = selectedSlotID;
            Item item = GetSlotItem(slot);

            if (CheckItemInventory(item.ID))
            {
                InventoryItemData data = Slots[slot].GetComponentInChildren<InventoryItemData>();

                if (data.itemAmount > Amount)
                {
                    data.itemAmount -= Amount;
                    data.transform.parent.GetChild(0).GetChild(0).GetComponent<Text>().text = data.itemAmount.ToString();
                }
                else
                {
                    Destroy(Slots[slot].transform.GetChild(1).gameObject);
                    RemoveFromCache(item);
                    ResetInventory();
                }
            }
        }
    }

    /// <summary>
    /// Remove Item from current Items Cache
    /// </summary>
    private void RemoveFromCache(Item item, CustomItemData customData = null, bool all = false)
    {
        if (all)
        {
            if (customData != null && customData.dataDictionary.ContainsKey(ITEM_TAG))
            {
                ItemsCache.RemoveAll(i => i.item.ID.Equals(item.ID) && i.customData.dataDictionary.Any(x => customData.dataDictionary.Any(y => x.Value == y.Value)));
            }
            else
            {
                ItemsCache.RemoveAll(x => x.item.ID.Equals(item.ID));
            }
        }
        else
        {
            int index = -1;

            if (customData != null && customData.dataDictionary.ContainsKey(ITEM_TAG))
            {
                index = ItemsCache.FindIndex(i => i.item.ID.Equals(item.ID) && i.customData.dataDictionary.Any(x => customData.dataDictionary.Any(y => x.Value == y.Value)));
            }
            else
            {
                index = ItemsCache.FindIndex(x => x.item.ID.Equals(item.ID));
            }

            if(index != -1)
            {
                ItemsCache.RemoveAt(index);
            }
        }
    }

    /// <summary>
    /// Use Selected Item
    /// </summary>
    public void UseSelectedItem()
    {
        UseItem();
    }

    /// <summary>
    /// Use Selected Item
    /// </summary>
    /// <param name="slotItem">Slot with item</param>
    public void UseItem(int slotItem = -1)
    {
        Item usableItem = null;

        if (slotItem >= 0)
        {
            if (HasSlotItem(slotItem))
            {
                usableItem = GetSlotItem(slotItem);
            }
        }
        else if (usableItem == null && selectedSlotID >= 0)
        {
            usableItem = GetSlotItem(selectedSlotID);
            slotItem = selectedSlotID;
        }

        if (usableItem == null)
        {
            Debug.LogError("[Inventory Use] Cannot use a null Item!");
            return;
        }

        if (GetItemAmount(usableItem.ID) < 2 || usableItem.useItemSwitcher)
        {
            if(selectedSlotID >= 0)
            {
                GetSlot(selectedSlotID).Select();
            }

            if (usableItem.useItemSwitcher)
            {
                ResetInventory();
            }
        }

        if (usableItem.doActionUse)
        {
            TriggerItemAction(slotItem, usableItem.useSwitcherID);
        }

        if (usableItem.itemType == ItemType.Heal)
        {
            healthManager.ApplyHeal(usableItem.healAmount);

            if (!healthManager.isMaximum)
            {
                if (usableItem.useSound)
                {
                    Tools.PlayOneShot2D(Tools.MainCamera().transform.position, usableItem.useSound, usableItem.soundVolume);
                }

                if (slotItem >= 0)
                {
                    if (usableItem.doActionUse && !usableItem.customActions.actionRemove)
                    {
                        RemoveSlotItem(slotItem);
                    }
                    else
                    {
                        RemoveSlotItem(slotItem);
                    }
                }
                else
                {
                    Debug.LogError("[Inventory] slotItem parameter cannot be (-1)!");
                }
            }
        }

        if (usableItem.itemType == ItemType.Weapon || usableItem.useItemSwitcher)
        {
            itemSwitcher.SelectSwitcherItem(usableItem.useSwitcherID);
            itemSwitcher.weaponItem = usableItem.useSwitcherID;
        }

        ShowContexMenu(false);
        ResetSlotProperties(true);
    }

    /// <summary>
    /// Drop selected slot Item to ground
    /// </summary>
    public void DropItemGround()
    {
        InteractiveItem interactiveItem = null;
        GameObject worldItem = null;
        InventoryItemData itemData = GetSlotItemData(selectedSlotID);
        Item item = itemData.item;

        SaveGameHandler.SaveableType saveableType = SaveGameHandler.SaveableType.None;

        Transform dropPos = PlayerController.Instance.GetComponentInChildren<PlayerFunctions>().inventoryDropPos;
        GameObject dropObject = GetDropObject(item);

        if (item.itemType == ItemType.Weapon || item.useItemSwitcher)
        {
            if (itemSwitcher.currentItem == item.useSwitcherID)
            {
                itemSwitcher.DisableItems();
            }
        }
      
        if (itemData.customData.Exist("object_scene"))
        {
            if (itemData.customData.Get("object_scene").Equals(gameManager.CurrentScene.name) && itemData.customData.Exist("object_path"))
            {
                string objPath = itemData.customData.Get("object_path");

                if (worldItem = GameObject.Find(objPath))
                {
                    saveableType = SaveGameHandler.Instance.GetSaveableType(worldItem);
                }
            }
        }

        if (GetItemAmount(item.ID) >= 2 && item.itemType != ItemType.Weapon)
        {
            if (worldItem)
            {
                if (saveableType != SaveGameHandler.SaveableType.Constant)
                {
                    SaveGameHandler.Instance.RemoveSaveableObject(worldItem, true);
                }

                worldItem = SaveGameHandler.Instance.InstantiateSaveable(item.packDropObject, dropPos.position, dropPos.eulerAngles, "ISAVE_" + dropObject.name);
            }
            else
            {
                worldItem = SaveGameHandler.Instance.InstantiateSaveable(item.packDropObject, dropPos.position, dropPos.eulerAngles, "ISAVE_" + dropObject.name);
            }

            if (worldItem && worldItem.GetComponent<InteractiveItem>())
            {
                interactiveItem = worldItem.GetComponent<InteractiveItem>();
                interactiveItem.EnableObject();
            }

            if (interactiveItem)
            {
                if (string.IsNullOrEmpty(interactiveItem.examineName))
                {
                    interactiveItem.examineName = "Sack of " + item.Title;
                }

                if (interactiveItem.messageType != InteractiveItem.MessageType.None && string.IsNullOrEmpty(interactiveItem.itemMessage))
                {
                    interactiveItem.itemMessage = "Sack of " + item.Title;
                }

                interactiveItem.ItemType = InteractiveItem.Type.InventoryItem;
                interactiveItem.disableType = InteractiveItem.DisableType.Destroy;
                interactiveItem.inventoryID = item.ID;
            }
            else
            {
                Debug.LogError($"[Inventory Drop] {worldItem.name} does not have InteractiveItem script");
                return;
            }
        }
        else if(GetItemAmount(item.ID) == 1 || item.itemType == ItemType.Weapon)
        {
            if (saveableType != SaveGameHandler.SaveableType.Constant)
            {
                if (!worldItem)
                {
                    worldItem = SaveGameHandler.Instance.InstantiateSaveable(item.dropObject, dropPos.position, dropPos.eulerAngles, "ISAVE_" + dropObject.name);
                }
                else
                {
                    worldItem.transform.SetPositionAndRotation(dropPos.position, dropPos.rotation);
                }
            }
            else if(saveableType == SaveGameHandler.SaveableType.Constant)
            {
                worldItem.transform.SetPositionAndRotation(dropPos.position, dropPos.rotation);
            }

            if (worldItem && worldItem.GetComponent<InteractiveItem>())
            {
                interactiveItem = worldItem.GetComponent<InteractiveItem>();
                interactiveItem.EnableObject();
            }

            if (interactiveItem)
            {
                if (saveableType != SaveGameHandler.SaveableType.Constant)
                {
                    interactiveItem.disableType = InteractiveItem.DisableType.Destroy;
                }
            }
            else
            {
                Debug.LogError($"[Inventory Drop] {worldItem.name} does not have InteractiveItem script!");
                return;
            }

            if (itemData.customData.Exist(ITEM_PATH))
            {
                Texture tex = Resources.Load<Texture2D>(itemData.customData.Get(ITEM_PATH));
                worldItem.GetComponentInChildren<MeshRenderer>().material.SetTexture("_MainTex", tex);
            }
        }

        if (interactiveItem && interactiveItem.customData != null && saveableType != SaveGameHandler.SaveableType.Constant)
        {
            interactiveItem.customData = itemData.customData;
        }

        Physics.IgnoreCollision(worldItem.GetComponent<Collider>(), Tools.MainCamera().transform.root.GetComponent<Collider>());

        if (worldItem && worldItem.GetComponent<Rigidbody>())
        {
            worldItem.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            worldItem.GetComponent<Rigidbody>().AddForce(Tools.MainCamera().transform.forward * (itemDropStrength * 10));
        }
        else
        {
            Debug.LogError($"[Inventory Drop] {worldItem.name} does not have Rigidbody!");
        }

        if (GetItemAmount(item.ID) < 2 || item.useItemSwitcher || item.itemType == ItemType.Bullets)
        {
            ShowContexMenu(false);
        }

        if (GetItemAmount(item.ID) > 1)
        {
            worldItem.GetComponent<InteractiveItem>().pickupAmount = GetItemAmount(item.ID);
            RemoveSelectedItem(true);
        }
        else
        {
            RemoveSelectedItem(true);
        }
    }

    /// <summary>
    /// Callback for CombineItem UI Button
    /// </summary>
    public void CombineItem()
    {
        firstCandidate = GetSlot(selectedSlotID);

        for (int i = 0; i < Slots.Count; i++)
        {
            Slots[i].GetComponent<InventorySlot>().isItemSelect = true;

            if (IsCombineSlot(i))
            {
                Slots[i].GetComponent<InventorySlot>().isSelectable = true;
                Slots[i].GetComponent<InventorySlot>().isCombineCandidate = true;
            }
            else
            {
                Slots[i].GetComponent<InventorySlot>().isSelectable = false;
                Slots[i].GetComponent<InventorySlot>().isCombineCandidate = false;
            }
        }

        ShowContexMenu(false);
    }

    /// <summary>
    /// Check if slot has item which is combinable.
    /// </summary>
    bool IsCombineSlot(int slotID)
    {
        InventoryItemData itemData;

        if ((itemData = GetSlotItemData(selectedSlotID)) != null)
        {
            InventoryScriptable.ItemMapper.CombineSettings[] combineSettings = itemData.item.combineSettings;

            foreach (var id in combineSettings)
            {
                InventoryItemData slotData;

                if ((slotData = GetSlotItemData(slotID)) != null)
                {
                    if (slotData.item.ID == id.combineWithID && slotData.customData.canCombine)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Check if item has partner item to combine.
    /// </summary>
    public bool HasCombinePartner(Item Item)
    {
        InventoryScriptable.ItemMapper.CombineSettings[] combineSettings = Item.combineSettings;
        return ItemsCache.Any(item => combineSettings.Any(item2 => item.item.ID == item2.combineWithID) && CanAnyCombine(item.item));
    }

    bool CanAnyCombine(Item item)
    {
        var itemDatas = Slots.Where(x => x.GetComponentInChildren<InventoryItemData>()).Select(x => x.GetComponentInChildren<InventoryItemData>()).ToArray();
        return itemDatas.Any(x => x.itemID == item.ID && x.customData.canCombine);
    }

    /// <summary>
    /// Function to combine selected Item with second Item.
    /// </summary>
    public void CombineWith(Item SecondItem, int slotID)
    {
        if (selectedScript != null)
        {
            if(selectedScript is IItemSelect)
            {
                InventoryItemData data = GetSlotItemData(slotID);
                RemoveSlotItem(slotID);
                (selectedScript as IItemSelect).OnItemSelect(SecondItem.ID, data.customData);
                selectedScript = null;
                gameManager.ShowInventory(false);
            }
        }
        else if(firstCandidate != null)
        {
            int firstItemSlot = firstCandidate.slotID;
            int secondItemSlot = slotID;

            if (firstItemSlot != secondItemSlot)
            {
                Item SelectedItem = firstCandidate.itemData.item;
                InventoryScriptable.ItemMapper.CombineSettings[] selectedCombineSettings = SelectedItem.combineSettings;

                int CombinedItemID = -1;
                int CombineSwitcherID = -1;

                foreach (var item in selectedCombineSettings)
                {
                    if (item.combineWithID == SecondItem.ID)
                    {
                        CombinedItemID = item.resultCombineID;
                        CombineSwitcherID = item.combineSwitcherID;
                    }
                }

                for (int i = 0; i < Slots.Count; i++)
                {
                    Slots[i].GetComponent<InventorySlot>().isSelectable = true;
                }

                if (SelectedItem.combineSound)
                {
                    Tools.PlayOneShot2D(Tools.MainCamera().transform.position, SelectedItem.combineSound, SelectedItem.soundVolume);
                }
                else
                {
                    if (SecondItem.combineSound)
                    {
                        Tools.PlayOneShot2D(Tools.MainCamera().transform.position, SecondItem.combineSound, SecondItem.soundVolume);
                    }
                }

                if (SelectedItem.doActionCombine)
                {
                    TriggerItemAction(firstItemSlot, CombineSwitcherID);
                }
                if (SecondItem.doActionCombine)
                {
                    TriggerItemAction(secondItemSlot, CombineSwitcherID);
                }

                if (SelectedItem.itemType == ItemType.ItemPart && SelectedItem.isCombinable)
                {
                    int switcherID = GetItem(SelectedItem.combineSettings[0].combineWithID).useSwitcherID;
                    GameObject MainObject = itemSwitcher.ItemList[switcherID];

                    MonoBehaviour script = MainObject.GetComponents<MonoBehaviour>().SingleOrDefault(sc => sc.GetType().GetField("CanReload") != null);
                    FieldInfo info = script.GetType().GetField("CanReload");

                    if (info != null)
                    {
                        bool canReload = Parser.Convert<bool>(script.GetType().InvokeMember("CanReload", BindingFlags.GetField, null, script, null).ToString());

                        if (canReload)
                        {
                            MainObject.SendMessage("Reload", SendMessageOptions.DontRequireReceiver);
                            RemoveSlotItem(firstItemSlot);
                        }
                        else
                        {
                            gameManager.AddMessage("Cannot reload yet!");
                            ResetInventory();
                        }
                    }
                    else
                    {
                        Debug.Log(MainObject.name + " object does not have script with CanReload property!");
                    }
                }
                else if (SelectedItem.isCombinable)
                {
                    if (SelectedItem.combineGetSwItem && CombineSwitcherID != -1)
                    {
                        if (CombineSwitcherID != -1)
                        {
                            itemSwitcher.SelectSwitcherItem(CombineSwitcherID);
                        }
                    }

                    if (SelectedItem.combineGetItem && CombinedItemID != -1)
                    {
                        int a_count = GetSlotItemData(firstItemSlot).itemAmount;
                        int b_count = GetSlotItemData(secondItemSlot).itemAmount;

                        if (!CheckInventorySpace())
                        {
                            if (a_count > 1 && b_count > 1)
                            {
                                gameManager.AddSingleMessage("No Inventory Space!", "inv_space", true);
                                return;
                            }
                        }

                        if (a_count < 2 && b_count >= 2)
                        {
                            if (!SelectedItem.combineNoRemove)
                            {
                                StartCoroutine(WaitForRemoveAddItem(secondItemSlot, CombinedItemID));
                            }
                            else
                            {
                                AddItem(CombinedItemID, 1);
                            }
                        }
                        if (a_count >= 2 && b_count < 2)
                        {
                            if (!SecondItem.combineNoRemove)
                            {
                                StartCoroutine(WaitForRemoveAddItem(secondItemSlot, CombinedItemID));
                            }
                            else
                            {
                                AddItem(CombinedItemID, 1);
                            }
                        }
                        if (a_count < 2 && b_count < 2)
                        {
                            if (!SelectedItem.combineNoRemove)
                            {
                                StartCoroutine(WaitForRemoveAddItem(secondItemSlot, CombinedItemID));
                            }
                            else
                            {
                                AddItem(CombinedItemID, 1);
                            }
                        }
                        if (a_count >= 2 && b_count >= 2)
                        {
                            AddItem(CombinedItemID, 1);
                        }
                    }

                    if (!SelectedItem.combineNoRemove && !SelectedItem.customActions.actionRemove)
                    {
                        RemoveSlotItem(firstItemSlot);
                    }
                    if (!SecondItem.combineNoRemove && !SecondItem.customActions.actionRemove)
                    {
                        RemoveSlotItem(secondItemSlot);
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Selected Slot ID cannot be null!");
        }

        ResetSlotProperties();
        selectedSlotID = -1;
        firstCandidate = null;
    }

    /// <summary>
    /// Function to Trigger Item Actions
    /// </summary>
    void TriggerItemAction(int itemSlot, int switcherID = -1)
    {
        Item SelectedItem = GetSlotItem(itemSlot);

        bool trigger = false;

        if (SelectedItem.useActionType != ItemAction.None)
        {
            if (SelectedItem.useActionType == ItemAction.Increase)
            {
                InventoryItemData itemData = GetSlotItemData(itemSlot);

                if (itemData.customData.dataDictionary.ContainsKey(ITEM_VALUE))
                {
                    int num = int.Parse(itemData.customData.dataDictionary[ITEM_VALUE]);
                    num++;
                    itemData.customData.dataDictionary[ITEM_VALUE] = num.ToString();

                    if (num >= SelectedItem.customActions.triggerValue)
                    {
                        trigger = true;
                    }
                }
            }
            else if (SelectedItem.useActionType == ItemAction.Decrease)
            {
                InventoryItemData itemData = GetSlotItemData(itemSlot);

                if (itemData.customData.dataDictionary.ContainsKey(ITEM_VALUE))
                {
                    int num = int.Parse(itemData.customData.dataDictionary[ITEM_VALUE]);
                    num--;
                    itemData.customData.dataDictionary[ITEM_VALUE] = num.ToString();

                    if (num <= SelectedItem.customActions.triggerValue)
                    {
                        trigger = true;
                    }
                }
            }
            else if (SelectedItem.useActionType == ItemAction.ItemValue)
            {
                IItemValueProvider itemValue = itemSwitcher.ItemList[switcherID].GetComponent<IItemValueProvider>();
                if (itemValue != null)
                {
                    InventoryItemData itemData = GetSlotItemData(itemSlot);

                    if (GetSlotItem(itemSlot).Description.RegexMatch('{', '}', "value"))
                    {
                        if (itemData.customData.dataDictionary.ContainsKey(ITEM_VALUE))
                        {
                            itemValue.OnSetValue(GetSlotItemData(itemSlot).customData.dataDictionary[ITEM_VALUE].ToString());
                        }
                    }
                }
            }
        }

        if (trigger)
        {
            if (SelectedItem.customActions.actionRemove)
            {
                RemoveSlotItem(itemSlot);
            }
            if (SelectedItem.customActions.actionAddItem)
            {
                AddItem(SelectedItem.customActions.triggerAddItem, 1, new CustomItemData(new Dictionary<string, string>() { { ITEM_VALUE, SelectedItem.customActions.addItemValue.ToString() } }));
            }
            if (SelectedItem.customActions.actionRestrictCombine)
            {
                GetSlotItemData(itemSlot).customData.canCombine = false;
            }
            if (SelectedItem.customActions.actionRestrictUse)
            {
                GetSlotItemData(itemSlot).customData.canUse = false;
            }
        }
    }

    /// <summary>
    /// Wait until old Item will be removed, then add new Item
    /// </summary>
    IEnumerator WaitForRemoveAddItem(int oldItemSlot, int newItem)
    {
        int oldItemCount = GetSlotItemData(oldItemSlot).itemAmount;

        if (oldItemCount < 2)
        {
            yield return new WaitUntil(() => !HasSlotItem(oldItemSlot));
            AddItemToSlot(oldItemSlot, newItem);
        }
        else
        {
            AddItem(newItem, 1);
        }
    }

    /// <summary>
    /// Callback for Examine Item Button
    /// </summary>
    public void ExamineItem()
    {
        InventoryItemData itemData = GetSlotItemData(selectedSlotID);
        Item item = itemData.item;
        gameManager.TabButtonPanel.SetActive(false);
        gameManager.ShowCursor(false);
        gameManager.ShowConsoleCursor(false);

        if (item.dropObject && item.dropObject.GetComponent<InteractiveItem>())
        {
            GameObject examine = Instantiate(GetDropObject(item));

            if (itemData.customData.dataDictionary.ContainsKey(ITEM_PATH))
            {
                Texture tex = Resources.Load<Texture2D>(itemData.customData.dataDictionary[ITEM_PATH].ToString());
                examine.GetComponentInChildren<MeshRenderer>().material.SetTexture("_MainTex", tex);
            }

            scriptManager.gameObject.GetComponent<ExamineManager>().ExamineObject(examine, item.inspectRotation);
        }
    }

    /// <summary>
    /// Get Item Drop Object by Item
    /// </summary>
    public GameObject GetDropObject(Item item)
    {
        try
        {
            return AllItems.Where(x => x.ID == item.ID).Select(x => x.dropObject).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Function to set specific item amount
    /// </summary>
    public void SetItemAmount(int ID, int Amount)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].transform.childCount > 1)
            {
                if (Slots[i].GetComponentInChildren<InventoryItemData>().item.ID == ID)
                {
                    Slots[i].GetComponentInChildren<InventoryItemData>().itemAmount = Amount;
                }
            }
        }
    }

    /// <summary>
    /// Function to expand item slots
    /// </summary>
    public void ExpandSlots(int SlotsAmount)
    {
        int extendedSlots = slotAmount + SlotsAmount;

        for (int i = slotAmount; i < extendedSlots; i++)
        {
            GameObject slot = Instantiate(inventorySlot);
            Slots.Add(slot);
            slot.GetComponent<InventorySlot>().inventory = this;
            slot.GetComponent<InventorySlot>().slotID = i;
            slot.transform.SetParent(SlotsContent.transform);
            slot.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
        }

        slotAmount = extendedSlots;
    }

    /// <summary>
    /// Function to set slots button enabled state
    /// </summary>
    public void SetSlotsState(bool state, params GameObject[] except)
    {
        foreach (var slot in Slots)
        {
            if (!state && except.Length > 0 && !except.Any(x => x == slot))
            {
                slot.GetComponent<InventorySlot>().isSelectable = state;
            }
            else if(state)
            {
                slot.GetComponent<InventorySlot>().isSelectable = state;
            }
        }
    }

    /// <summary>
    /// Check if there is space in Inevntory
    /// </summary>
    public bool CheckInventorySpace()
    {
        return Slots.Any(x => x.transform.childCount < 2);
    }

    /// <summary>
    /// Check if any Item is in Inventory
    /// </summary>
    public bool AnyInventroy()
    {
        return Slots.Any(x => x.transform.childCount > 1);
    }

    /// <summary>
    /// Check if Item is in Inventory by ID
    /// </summary>
    public bool CheckItemInventory(int ID)
    {
        return ItemsCache.Any(x => x.item.ID == ID);
    }

    /// <summary>
    /// Check if Item is in Inventory and is Stackable by Item ID
    /// </summary>
    public bool CheckItemInventoryStack(int ID)
    {
        return ItemsCache.Any(x => x.item.ID == ID && x.item.isStackable);
    }

    /// <summary>
    /// Check if Switcher Item is in Inventory
    /// </summary>
    public bool CheckSWIDInventory(int SwitcherID)
    {
        return ItemsCache.Any(x => x.item.useSwitcherID == SwitcherID);
    }

    /// <summary>
    /// Check if slot has specific item
    /// </summary>
    bool HasSlotItem(int slotID, int itemID)
    {
        if (Slots[slotID].GetComponentInChildren<InventoryItemData>())
        {
            return Slots[slotID].GetComponentInChildren<InventoryItemData>().item.ID == itemID;
        }

        return false;
    }

    /// <summary>
    /// Check if slot has any item
    /// </summary>
    bool HasSlotItem(int slotID)
    {
        if (Slots[slotID].GetComponentInChildren<InventoryItemData>())
        {
            return Slots[slotID].GetComponentInChildren<InventoryItemData>().item.ID != -1;
        }

        return false;
    }

    /// <summary>
    /// Get specific InventoryItemData
    /// </summary>
    InventoryItemData GetItemData(int itemID, int slotID = -1)
    {
        if (slotID != -1)
        {
            if (HasSlotItem(slotID, itemID))
            {
                return Slots[slotID].GetComponentInChildren<InventoryItemData>();
            }
        }
        else
        {
            foreach (var slot in Slots)
            {
                if (slot.GetComponentInChildren<InventoryItemData>() && slot.GetComponentInChildren<InventoryItemData>().item.ID == itemID)
                {
                    return slot.GetComponentInChildren<InventoryItemData>();
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Get InventoryItemData from slot
    /// </summary>
    InventoryItemData GetSlotItemData(int slotID)
    {
        if (Slots[slotID].GetComponentInChildren<InventoryItemData>())
        {
            return Slots[slotID].GetComponentInChildren<InventoryItemData>();
        }

        return null;
    }

    /// <summary>
    /// Get next slot with Item.
    /// </summary>
    InventorySlot GetSlotWitItem()
    {
        foreach (var slot in Slots)
        {
            if (slot.GetComponentInChildren<InventoryItemData>())
            {
                return slot.GetComponent<InventorySlot>();
            }
        }

        return null;
    }

    /// <summary>
    /// Get InventorySlot by Slot ID
    /// </summary>
    public InventorySlot GetSlot(int slotID)
    {
        return Slots[slotID].GetComponent<InventorySlot>();
    }

    /// <summary>
    /// Get Slot ID by Inventory ID
    /// </summary>
    /// <param name="reverse">Check slots from last to first?</param>
    int GetItemSlotID(int itemID, bool reverse = false)
    {
        if (!reverse)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].GetComponentInChildren<InventoryItemData>() && Slots[i].GetComponentInChildren<InventoryItemData>().item.ID == itemID)
                {
                    return i;
                }
            }
        }
        else
        {
            for (int i = Slots.Count - 1; i > 0; i--)
            {
                if (Slots[i].GetComponentInChildren<InventoryItemData>() && Slots[i].GetComponentInChildren<InventoryItemData>().item.ID == itemID)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Get Item from Slot ID
    /// </summary>
    Item GetSlotItem(int slotID)
    {
        if (Slots[slotID].GetComponentInChildren<InventoryItemData>())
        {
            return Slots[slotID].GetComponentInChildren<InventoryItemData>().item;
        }

        return null;
    }

    /// <summary>
    /// Get Item Amount by Item ID
    /// </summary>
    public int GetItemAmount(int itemID)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].GetComponentInChildren<InventoryItemData>() && Slots[i].GetComponentInChildren<InventoryItemData>().item.ID == itemID)
            {
                return Slots[i].GetComponentInChildren<InventoryItemData>().itemAmount;
            }
        }

        return -1;
    }

    /// <summary>
    /// Reset all slot properties
    /// </summary>
    public void ResetSlotProperties(bool exceptSelected = false)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            InventorySlot slot = GetSlot(i);

            slot.isSelectable = true;
            slot.isSelected = exceptSelected ? slot.isSelected : false;
            slot.contexVisible = false;
            slot.itemIsMoving = false;
            slot.isItemSelect = false;
            slot.isCombineCandidate = false;
        }
    }

    /// <summary>
    /// Deselect specific slot
    /// </summary>
	public void Deselect(int slotID){
        Slots[slotID].GetComponent<Image>().color = Color.white;

        ItemLabel.text = string.Empty;
        ItemDescription.text = string.Empty;
        ShowContexMenu(false);
        ResetSlotProperties();

        selectedSlotID = -1;
	}

    /// <summary>
    /// Reset Inventory
    /// </summary>
    public void ResetInventory()
    {
        if (selectedScript != null || isShortcutBind) return;

        EventSystem.current.SetSelectedGameObject(null);
        ResetSlotProperties();
        SetSlotsState(true);

        if (selectedSlotID >= 0)
        {
            if (itemToMove)
            {
                itemToMove.isMoving = false;
                itemToMove = null;
            }

            GetSlot(selectedSlotID).isSelected = false;
            ShowContexMenu(false);
            ItemLabel.text = string.Empty;
            ItemDescription.text = string.Empty;
            selectedSlotID = -1;
            isContexVisible = false;
        }

        isSelecting = false;
        isShortcutBind = false;
    }

    /// <summary>
    /// Show selected Item Contex Menu
    /// </summary>
    public void ShowContexMenu(bool show, Item item = null, int slot = -1, bool ctx_use = true, bool ctx_combine = true, bool ctx_examine = true, bool ctx_drop = true, bool ctx_shortcut = false, bool ctx_store = false, bool ctx_remove = false)
    {
        InventoryItemData itemData = null;

        if (show && item != null && slot > -1)
        {
            Vector3[] corners = new Vector3[4];
            Slots[slot].GetComponent<RectTransform>().GetWorldCorners(corners);
            int[] cornerSlots = Enumerable.Range(0, maxSlots + 1).Where(x => x % slotsInRow == 0).ToArray();
            int n_slot = slot + 1;

            if (slot > -1)
            {
                itemData = GetSlotItemData(slot);
            }

            if (!cornerSlots.Contains(n_slot))
            {
                contexMenu.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                contexMenu.transform.position = corners[2];
            }
            else
            {
                contexMenu.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
                contexMenu.transform.position = corners[1];
            }

            bool use = item.isUsable && ctx_use && itemData.customData.canUse;
            bool combine = item.isCombinable && ctx_combine && itemData.customData.canCombine;
            bool examine = item.canInspect && ctx_examine;
            bool drop = item.isDroppable && ctx_drop;
            bool shortcut = item.canBindShortcut && ctx_shortcut;
            bool remove = item.isRemovable && ctx_remove;

            contexUse.gameObject.SetActive(use);
            contexCombine.gameObject.SetActive(combine);
            contexExamine.gameObject.SetActive(examine);
            contexDrop.gameObject.SetActive(drop);
            contexShortcut.gameObject.SetActive(shortcut);
            contexStore.gameObject.SetActive(ctx_store);
            contexRemovable.gameObject.SetActive(remove);

            for (int i = 0; i < contexMenu.transform.childCount; i++)
            {
                if (contexMenu.transform.GetChild(i).gameObject.activeSelf)
                {
                    InventoryContexts.Add(contexMenu.transform.GetChild(i).GetComponent<InventoryContex>());
                }
            }

            if (use || combine || examine || drop || ctx_store || remove)
            {
                contexMenu.SetActive(true);
            }
            else
            {
                contexMenu.SetActive(false);
                InventoryContexts.Clear();
                selectedContex = 0;
                isContexVisible = false;
            }
        }
        else
        {
            contexMenu.SetActive(false);
            contexUse.gameObject.SetActive(false);
            contexCombine.gameObject.SetActive(false);
            contexExamine.gameObject.SetActive(false);
            contexDrop.gameObject.SetActive(false);
            contexShortcut.gameObject.SetActive(false);
            contexStore.gameObject.SetActive(false);
            contexRemovable.gameObject.SetActive(false);
            InventoryContexts.Clear();
            selectedContex = 0;
            isContexVisible = false;
        }
    }

    /// <summary>
    /// Show timed UI Notification
    /// </summary>
    public void ShowNotification(string text)
    {
        InventoryNotification.transform.GetComponentInChildren<Text>().text = text;
        InventoryNotification.gameObject.SetActive(true);
        fadeNotification = true;
        StartCoroutine(fader.StartFadeIO(InventoryNotification.color.a, 1.2f, 0.8f, 3, 4, UIFader.FadeOutAfter.Time));
    }

    /// <summary>
    /// Show fixed UI Notification (Bool Fade Out)
    /// </summary>
    public void ShowNotificationFixed(string text)
    {
        InventoryNotification.transform.GetComponentInChildren<Text>().text = text;
        InventoryNotification.gameObject.SetActive(true);
        fadeNotification = true;
        fader.fadeOut = false;
        StartCoroutine(fader.StartFadeIO(InventoryNotification.color.a, 1.2f, 0.8f, 3, 3, UIFader.FadeOutAfter.Bool));
    }

    [Serializable]
    public class ShortcutModel
    {
        public Item item;
        public int slot;
        public string shortcut;

        public ShortcutModel(Item item, int slot, string control)
        {
            this.item = item;
            this.slot = slot;
            shortcut = control;
        }
    }

    [Serializable]
    public struct SlotGrid
    {
        public int row;
        public int column;
        public int slotID;

        public SlotGrid(int row, int column, int id)
        {
            this.row = row;
            this.column = column;
            slotID = id;
        }
    }
}

public class InventoryItem
{
    public Item item;
    public CustomItemData customData;

    public InventoryItem(Item item, CustomItemData data)
    {
        this.item = item;
        customData = data;
    }
}

public class Item
{
    //Main
    public int ID { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public ItemType itemType { get; set; }
    public ItemAction useActionType { get; set; }
    public Sprite itemSprite { get; set; }
    public GameObject dropObject { get; set; }
    public GameObject packDropObject { get; set; }

    //Toggles
    public bool isStackable { get; set; }
    public bool isUsable { get; set; }
    public bool isCombinable { get; set; }
    public bool isDroppable { get; set; }
    public bool isRemovable { get; set; }
    public bool canInspect { get; set; }
    public bool canBindShortcut { get; set; }
    public bool combineGetItem { get; set; }
    public bool combineNoRemove { get; set; }
    public bool combineGetSwItem { get; set; }
    public bool useItemSwitcher { get; set; }
    public bool showContainerDesc { get; set; }
    public bool doActionUse { get; set; }
    public bool doActionCombine { get; set; }

    //Sounds
    public AudioClip useSound { get; set; }
    public AudioClip combineSound { get; set; }
    public float soundVolume { get; set; }

    //Settings
    public int maxItemCount { get; set; }
    public int useSwitcherID { get; set; }
    public int healAmount { get; set; }
    public Vector3 inspectRotation { get; set; }

    //Use Action Settings
    public InventoryScriptable.ItemMapper.CustomActionSettings customActions { get; set; }

    //Combine Settings
    public InventoryScriptable.ItemMapper.CombineSettings[] combineSettings { get; set; }

    public Item()
    {
        ID = 0;
    }

    public Item(int itemId, InventoryScriptable.ItemMapper mapper)
    {
        ID = itemId;
        Title = mapper.Title;
        Description = mapper.Description;
        itemType = mapper.itemType;
        useActionType = mapper.useActionType;
        itemSprite = mapper.itemSprite;
        dropObject = mapper.dropObject;
        packDropObject = mapper.packDropObject;

        isStackable = mapper.itemToggles.isStackable;
        isUsable = mapper.itemToggles.isUsable;
        isCombinable = mapper.itemToggles.isCombinable;
        isDroppable = mapper.itemToggles.isDroppable;
        isRemovable = mapper.itemToggles.isRemovable;
        canInspect = mapper.itemToggles.canInspect;
        canBindShortcut = mapper.itemToggles.canBindShortcut;
        combineGetItem = mapper.itemToggles.CombineGetItem;
        combineNoRemove = mapper.itemToggles.CombineNoRemove;
        combineGetSwItem = mapper.itemToggles.CombineGetSwItem;
        useItemSwitcher = mapper.itemToggles.UseItemSwitcher;
        showContainerDesc = mapper.itemToggles.ShowContainerDesc;
        doActionUse = mapper.itemToggles.doActionUse;
        doActionCombine = mapper.itemToggles.doActionCombine;

        useSound = mapper.itemSounds.useSound;
        combineSound = mapper.itemSounds.combineSound;
        soundVolume = mapper.itemSounds.soundVolume;

        maxItemCount = mapper.itemSettings.maxItemCount;
        useSwitcherID = mapper.itemSettings.useSwitcherID;
        healAmount = mapper.itemSettings.healAmount;
        inspectRotation = mapper.itemSettings.inspectRotation;

        customActions = mapper.useActionSettings;
        combineSettings = mapper.combineSettings;
    }
}
