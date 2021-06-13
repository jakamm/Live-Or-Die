/*
 * InteractManager.cs - by ThunderWire Studio
 * ver. 2.0
*/

using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ThunderWire.Utility;
using ThunderWire.CrossPlatform.Input;

/// <summary>
/// Main Interact Manager
/// </summary>
public class InteractManager : MonoBehaviour {

    private CrossPlatformInput crossPlatformInput;
    private HFPS_GameManager gameManager;
	private ItemSwitcher itemSelector;
	private Inventory inventory;

    private Camera mainCamera;
    private DynamicObject dynamicObj;
    private InteractiveItem interactItem;
    private UIObjectInfo objectInfo;
    private DraggableObject dragRigidbody;

    [Header("Raycast")]
	public float RaycastRange = 3;
	public LayerMask cullLayers;
    public LayerMask interactLayers;
	
	[Header("Crosshair Textures")]
	public Sprite defaultCrosshair;
	public Sprite interactCrosshair;
	private Sprite default_interactCrosshair;
	
	[Header("Crosshair")]
	private Image CrosshairUI;
	public int crosshairSize = 5;
	public int interactSize = 10;

    [Header("Texts")]
    [Tooltip("Make sure you have included \"{0}\" to string")]
    public string PickupHintFormat = "You have taken a {0}";
    public string TakeText = "Take";
    public string UseText = "Use";
    public string UnlockText = "Unlock";
    public string GrabText = "Grab";
    public string DragText = "Drag";
    public string ExamineText = "Examine";
    public string RemoveText = "Remove";

    #region Private Variables
    [HideInInspector] public bool isHeld = false;
    [HideInInspector] public bool inUse;
    [HideInInspector] public Ray playerAim;
    [HideInInspector] public GameObject RaycastObject;
    private GameObject LastRaycastObject;

    private int default_interactSize;
    private int default_crosshairSize;

    private CrossPlatformControl UseKey;
    private CrossPlatformControl PickupKey;
    private bool UsePressed;

	private bool isPressed;
    private bool isDraggable;
    private bool isCorrectLayer;
    #endregion

    void Awake()
    {
        inventory = Inventory.Instance;
        crossPlatformInput = CrossPlatformInput.Instance;
        gameManager = HFPS_GameManager.Instance;
        mainCamera = ScriptManager.Instance.MainCamera;
        itemSelector = ScriptManager.Instance.GetScript<ItemSwitcher>();

        CrosshairUI = gameManager.Crosshair;
        default_interactCrosshair = interactCrosshair;
        default_crosshairSize = crosshairSize;
        default_interactSize = interactSize;
        RaycastObject = null;
        dynamicObj = null;
    }

    void Update()
    {
        if (crossPlatformInput.inputsLoaded)
        {
            UseKey = crossPlatformInput.ControlOf("Use");
            PickupKey = crossPlatformInput.ControlOf("Examine");
            UsePressed = crossPlatformInput.GetActionPressedOnce(this, "Use");
        }

        if (UsePressed && RaycastObject && !isPressed && !isHeld && !inUse && !gameManager.isWeaponZooming)
        {
            Interact(RaycastObject);
            isPressed = true;
        }

        if (!UsePressed && isPressed)
        {
            isPressed = false;
        }

        Ray playerAim = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(playerAim, out RaycastHit hit, RaycastRange, cullLayers))
        {
            if (interactLayers.CompareLayer(hit.collider.gameObject.layer) && !gameManager.isWeaponZooming)
            {
                if (hit.collider.gameObject != RaycastObject)
                {
                    gameManager.HideSprites(HideHelpType.Interact);
                }

                RaycastObject = hit.collider.gameObject;
                isDraggable = (dragRigidbody = RaycastObject.GetComponent<DraggableObject>()) != null;
                isCorrectLayer = true;

                if (RaycastObject.GetComponent<InteractiveItem>())
                {
                    interactItem = RaycastObject.GetComponent<InteractiveItem>();
                }
                else
                {
                    interactItem = null;
                }

                if (RaycastObject.GetComponent<DynamicObject>())
                {
                    dynamicObj = RaycastObject.GetComponent<DynamicObject>();
                }
                else
                {
                    dynamicObj = null;
                }

                if (RaycastObject.GetComponent<UIObjectInfo>())
                {
                    objectInfo = RaycastObject.GetComponent<UIObjectInfo>();
                }
                else
                {
                    objectInfo = null;
                }

                if (RaycastObject.GetComponent<CrosshairReticle>())
                {
                    CrosshairReticle ChangeReticle = RaycastObject.GetComponent<CrosshairReticle>();
                    if (dynamicObj)
                    {
                        if (dynamicObj.useType != Type_Use.Locked)
                        {
                            interactCrosshair = ChangeReticle.interactSprite;
                            interactSize = ChangeReticle.size;
                        }
                    }
                    else
                    {
                        interactCrosshair = ChangeReticle.interactSprite;
                        interactSize = ChangeReticle.size;
                    }
                }

                if (LastRaycastObject)
                {
                    if (!(LastRaycastObject == RaycastObject))
                    {
                        ResetCrosshair();
                    }
                }
                LastRaycastObject = RaycastObject;

                if (objectInfo && !string.IsNullOrEmpty(objectInfo.objectTitle))
                {
                    gameManager.ShowInteractInfo(objectInfo.objectTitle);
                }

                if (!inUse)
                {
                    if (dynamicObj)
                    {
                        if (dynamicObj.useType == Type_Use.Locked)
                        {
                            if (dynamicObj.CheckHasKey())
                            {
                                gameManager.ShowInteractSprite(1, UnlockText, UseKey);
                            }
                            else
                            {
                                if (dynamicObj.interactType == Type_Interact.Mouse)
                                {
                                    gameManager.ShowInteractSprite(1, DragText, UseKey);
                                }
                                else
                                {
                                    gameManager.ShowInteractSprite(1, UseText, UseKey);
                                }
                            }
                        }
                        else
                        {
                            if (dynamicObj.interactType == Type_Interact.Mouse)
                            {
                                gameManager.ShowInteractSprite(1, DragText, UseKey);
                            }
                            else
                            {
                                gameManager.ShowInteractSprite(1, UseText, UseKey);
                            }
                        }
                    }
                    else
                    {
                        if (interactItem)
                        {
                            if (interactItem.showItemName && !string.IsNullOrEmpty(interactItem.examineName))
                            {
                                gameManager.ShowInteractInfo(interactItem.examineName);
                            }

                            if (!dragRigidbody || dragRigidbody && !dragRigidbody.dragAndUse)
                            {
                                if (interactItem.ItemType == InteractiveItem.Type.OnlyExamine)
                                {
                                    gameManager.ShowInteractSprite(1, ExamineText, PickupKey);
                                }
                                else if (interactItem.ItemType == InteractiveItem.Type.InteractObject)
                                {
                                    if (interactItem.examineType != InteractiveItem.ExamineType.None)
                                    {
                                        gameManager.ShowInteractSprite(1, UseText, UseKey);
                                        gameManager.ShowInteractSprite(2, ExamineText, PickupKey);
                                    }
                                    else
                                    {
                                        gameManager.ShowInteractSprite(1, UseText, UseKey);
                                    }
                                }
                                else if (interactItem.examineType != InteractiveItem.ExamineType.None && interactItem.ItemType != InteractiveItem.Type.InteractObject)
                                {
                                    gameManager.ShowInteractSprite(1, TakeText, UseKey);
                                    gameManager.ShowInteractSprite(2, ExamineText, PickupKey);
                                }
                                else if (interactItem.examineType == InteractiveItem.ExamineType.Paper)
                                {
                                    gameManager.ShowInteractSprite(1, ExamineText, PickupKey);
                                }
                                else
                                {
                                    gameManager.ShowInteractSprite(1, TakeText, UseKey);
                                }
                            }
                            else if(dragRigidbody && dragRigidbody.dragAndUse)
                            {
                                if (interactItem.ItemType != InteractiveItem.Type.OnlyExamine)
                                {
                                    gameManager.ShowInteractSprite(1, TakeText, UseKey);
                                    gameManager.ShowInteractSprite(2, GrabText, PickupKey);
                                }
                            }
                        }
                        else if (RaycastObject.GetComponent<DynamicObjectPlank>())
                        {
                            gameManager.ShowInteractSprite(1, RemoveText, UseKey);
                        }
                        else if (dragRigidbody && !dragRigidbody.dragAndUse)
                        {
                            gameManager.ShowInteractSprite(1, GrabText, PickupKey);
                        }
                        else if (objectInfo)
                        {
                            gameManager.ShowInteractSprite(1, objectInfo.useText, UseKey);
                        }
                        else
                        {
                            gameManager.ShowInteractSprite(1, UseText, UseKey);
                        }
                    }
                }

                CrosshairChange(true);
            }
            else if (RaycastObject)
            {
                isCorrectLayer = false;
            }
        }
        else if (RaycastObject)
        {
            isCorrectLayer = false;
        }

        if (!isCorrectLayer)
        {
            ResetCrosshair();
            CrosshairChange(false);
            gameManager.HideSprites(HideHelpType.Interact);
            interactItem = null;
            RaycastObject = null;
            dynamicObj = null;
        }

        if (!RaycastObject)
        {
            gameManager.HideSprites(HideHelpType.Interact);
            CrosshairChange(false);
            dynamicObj = null;
        }
    }

    void CrosshairChange(bool useTexture)
    {
        if(useTexture && CrosshairUI.sprite != interactCrosshair)
        {
            CrosshairUI.sprite = interactCrosshair;
            CrosshairUI.GetComponent<RectTransform>().sizeDelta = new Vector2(interactSize, interactSize);
        }
        else if(!useTexture && CrosshairUI.sprite != defaultCrosshair)
        {
            CrosshairUI.sprite = defaultCrosshair;
            CrosshairUI.GetComponent<RectTransform>().sizeDelta = new Vector2(crosshairSize, crosshairSize);
        }

        CrosshairUI.DisableSpriteOptimizations();
    }

	private void ResetCrosshair(){
		crosshairSize = default_crosshairSize;
		interactSize = default_interactSize;
		interactCrosshair = default_interactCrosshair;
	}

	public void CrosshairVisible(bool state)
	{
		switch (state) 
		{
		case true:
			CrosshairUI.enabled = true;
			break;
		case false:
			CrosshairUI.enabled = false;
			break;
		}
	}

	public bool GetInteractBool()
	{
		if (RaycastObject) {
			return true;
		} else {
			return false;
		}
	}

    public void Interact(GameObject InteractObject)
    {
        InteractiveItem interactiveItem = interactItem;

        if(!interactItem && !interactiveItem && InteractObject.GetComponent<InteractiveItem>())
        {
            interactiveItem = InteractObject.GetComponent<InteractiveItem>();
        }

        if (interactiveItem && interactiveItem.ItemType == InteractiveItem.Type.OnlyExamine) return;

        if (InteractObject.GetComponent<Message>())
        {
            Message message = InteractObject.GetComponent<Message>();

            if (message.messageType == Message.MessageType.Hint)
            {
                char[] messageChars = message.message.ToCharArray();

                if (messageChars.Contains('{') && messageChars.Contains('}'))
                {
                    //string key = inputManager.GetInput(message.message.GetBetween('{', '}')).ToString();
                    //message.message = message.message.ReplacePart('{', '}', key);
                }

                gameManager.ShowHint(message.message, message.messageTime);
            }
            else if(message.messageType == Message.MessageType.PickupHint)
            {
                gameManager.ShowHint(string.Format(PickupHintFormat, message.message), message.messageTime);
            }
            else if (message.messageType == Message.MessageType.Message)
            {
                gameManager.AddMessage(message.message);
            }
            else if(message.messageType == Message.MessageType.ItemName)
            {
                gameManager.AddPickupMessage(message.message);
            }
        }

        if (interactiveItem)
        {
            Item item = new Item();
            bool showMessage = true;
            string autoShortcut = string.Empty;

            if (interactiveItem.ItemType == InteractiveItem.Type.InventoryItem)
            {
                item = inventory.GetItem(interactiveItem.inventoryID);
            }

            if (interactiveItem.ItemType == InteractiveItem.Type.GenericItem)
            {
                InteractEvent(InteractObject);
            }
            else if (interactiveItem.ItemType == InteractiveItem.Type.BackpackExpand)
            {
                if ((inventory.slotAmount + interactiveItem.backpackExpandAmount) > inventory.maxSlots)
                {
                    gameManager.WarningMessage("Cannot carry more backpacks");
                    return;
                }

                inventory.ExpandSlots(interactiveItem.backpackExpandAmount);
                InteractEvent(InteractObject);
            }
            else if (interactiveItem.ItemType == InteractiveItem.Type.InventoryItem)
            {
                if (inventory.CheckInventorySpace() || inventory.CheckItemInventoryStack(interactiveItem.inventoryID))
                {
                    if (inventory.GetItemAmount(item.ID) < item.maxItemCount || item.maxItemCount == 0)
                    {
                        autoShortcut = inventory.AddItem(interactiveItem.inventoryID, interactiveItem.pickupAmount, interactiveItem.customData, interactiveItem.autoShortcut);
                        InteractEvent(InteractObject);
                    }
                    else if (inventory.GetItemAmount(item.ID) >= item.maxItemCount)
                    {
                        gameManager.AddSingleMessage("You cannot carry more " + item.Title, "MaxItemCount");
                        showMessage = false;
                    }
                }
                else
                {
                    gameManager.AddSingleMessage("No Inventory Space!", "NoSpace");
                    showMessage = false;
                }
            }
            else if (interactiveItem.ItemType == InteractiveItem.Type.ArmsItem)
            {
                if (inventory.CheckInventorySpace() || inventory.CheckItemInventoryStack(interactiveItem.inventoryID))
                {
                    if (inventory.GetItemAmount(item.ID) < item.maxItemCount || item.maxItemCount == 0)
                    {
                        autoShortcut = inventory.AddItem(interactiveItem.inventoryID, interactiveItem.pickupAmount, null, interactiveItem.autoShortcut);

                        if (interactiveItem.pickupSwitch)
                        {
                            itemSelector.SelectSwitcherItem(interactiveItem.weaponID);
                        }

                        if (item.itemType == ItemType.Weapon)
                        {
                            itemSelector.weaponItem = interactiveItem.weaponID;
                        }

                        InteractEvent(InteractObject);
                    }
                    else if (inventory.GetItemAmount(item.ID) >= item.maxItemCount)
                    {
                        gameManager.AddSingleMessage("You cannot carry more " + item.Title, "MaxItemCount");
                        showMessage = false;
                    }
                }
                else
                {
                    gameManager.AddSingleMessage("No Inventory Space!", "NoSpace");
                    showMessage = false;
                }
            }
            else if(interactItem.ItemType == InteractiveItem.Type.InteractObject)
            {
                InteractEvent(InteractObject);
            }

            if (showMessage)
            {
                if (interactiveItem.messageType == InteractiveItem.MessageType.PickupHint)
                {
                    if (!string.IsNullOrEmpty(autoShortcut) && interactiveItem.MessageTips.Any(x => x.InputString.Equals("?")))
                    {
                        foreach (var tip in interactiveItem.MessageTips)
                        {
                            if (tip.InputString.Equals("?"))
                            {
                                tip.InputString = autoShortcut;
                                break;
                            }
                        }
                    }

                    gameManager.ShowHint(string.Format(PickupHintFormat, interactiveItem.itemMessage), interactiveItem.messageShowTime, interactiveItem.MessageTips);
                }
                else if (interactiveItem.messageType == InteractiveItem.MessageType.Message)
                {
                    gameManager.AddMessage(interactiveItem.itemMessage);
                }
                else if (interactiveItem.messageType == InteractiveItem.MessageType.ItemName)
                {
                    gameManager.AddPickupMessage(interactiveItem.itemMessage);
                }
            }
        }
        else
        {
            InteractEvent(InteractObject);
        }
    }

	void InteractEvent(GameObject InteractObject)
	{
        gameManager.HideSprites (HideHelpType.Interact);
        InteractObject.SendMessage ("UseObject", SendMessageOptions.DontRequireReceiver);
	}
}
