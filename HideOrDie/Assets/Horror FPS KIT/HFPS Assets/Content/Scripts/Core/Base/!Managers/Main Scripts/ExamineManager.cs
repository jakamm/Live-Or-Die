/*
 * ExamineManager.cs - by ThunderWire Studio
 * ver. 2.1
*/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ThunderWire.CrossPlatform.Input;
using ThunderWire.Utility;

public class ExamineManager : Singleton<ExamineManager>
{
    #region Structures
    public class RigidbodyExamine
    {
        public class RigidbodyParams
        {
            public bool isKinematic;
            public bool useGravity;
        }

        public RigidbodyExamine(GameObject obj, RigidbodyParams rbp)
        {
            rbObject = obj;
            rbParameters = rbp;
        }

        public GameObject rbObject;
        public RigidbodyParams rbParameters;
    }
    #endregion

    private CrossPlatformInput crossPlatformInput;
    private HFPS_GameManager gameManager;
    private Inventory inventory;
    private InteractManager interact;
    private PlayerFunctions pfunc;
    private FloatingIconManager floatingItem;
    private DelayEffect delay;
    private ScriptManager scriptManager;
    private ItemSwitcher itemSwitcher;

    private GameObject paperUI;
    private Text paperText;

    private List<RigidbodyExamine> ExamineRBs = new List<RigidbodyExamine>();

    public delegate void ActionDelegate(ExamineManager examine);
    public event ActionDelegate onDropObject;

    [HideInInspector]
    public bool isExamining;

    [Header("Raycast")]
    public LayerMask CullLayers;
    [Layer] public int InteractLayer;
    [Layer] public int ExamineLayer;
    [Layer] public int SecondExamineLayer;

    [Header("Examine Layering")]
    public LayerMask MainCameraMask;
    public LayerMask ArmsCameraMask;

    [Header("Second Examine Layering")]
    public LayerMask SecMainCameraMask;
    public LayerMask SecArmsCameraMask;

    [Header("Adjustments")]
    public float pickupSpeed = 10;
    public float pickupRotateSpeed = 10;
    [Space(5)]
    public float putBackTime = 10;
    public float putBackRotateTime = 10;
    [Space(5)]
    public float rotationDeadzone = 0.1f;
    public float rotateSpeed = 10f;
    public float rotateSmoothing = 1f;
    public float timeToExamine = 1f;
    public float spamWaitTime = 0.5f;

    [Header("Lighting")]
    public Light examineLight;

    [Header("Sounds")]
    public AudioClip examinedSound;
    public float examinedVolume = 1f;

    #region Private Variables
    private bool isReading;
    private bool isPaper;
    private bool antiSpam;
    private bool isObjectHeld;
    private bool tryExamine;
    private bool otherHeld;
    private bool isInspect;
    private bool cursorShown;
    private float distance;
    private float pickupRange = 3f;

    private Quaternion faceRotationFirst;
    private Quaternion faceRotationSecond;

    private bool firstFaceBreak = false;
    private bool secondFaceBreak = false;

    private bool faceToCameraFirst = false;
    private bool faceToCameraSecond = false;
    private bool faceToCameraInspect = false;

    private Camera ArmsCam;
    private Camera PlayerCam;
    private Ray PlayerAim;

    private LayerMask DefaultMainCamMask;
    private LayerMask DefaultArmsCamMask;

    private GameObject objectRaycast;
    private GameObject objectHeld;

    private InteractiveItem firstExamine;
    private InteractiveItem secondExamine;
    private InteractiveItem priorityObject;

    private Transform oldSecondObjT;
    private Vector3 oldSecondObjPos;
    private Quaternion oldSecondRot;

    private Vector3 objectPosition;
    private Quaternion objectRotation;

    private Vector2 rotationVelocity;
    private Vector2 smoothRotation;

    private Device crossPlatformDevice;
    private CrossPlatformControl useControl;
    private Vector2 rotateValue;
    private Vector2 movementVector;

    private bool cursorKey;
    private bool rotateKey;
    private bool selectKey;
    private bool examineKey;
    private bool read_takeKey;
    #endregion

    void Awake()
    {
        crossPlatformInput = CrossPlatformInput.Instance;
        scriptManager = ScriptManager.Instance;
        itemSwitcher = scriptManager.GetScript<ItemSwitcher>();
    }

    void Start()
    {
        if (GetComponent<InteractManager>() && GetComponent<PlayerFunctions>())
        {
            gameManager = HFPS_GameManager.Instance;
            inventory = Inventory.Instance;
            floatingItem = FloatingIconManager.Instance;
            interact = GetComponent<InteractManager>();
            pfunc = GetComponent<PlayerFunctions>();
            paperUI = gameManager.PaperTextUI;
            paperText = gameManager.PaperReadText;
        }
        else
        {
            Debug.LogError("Missing one or more scripts in " + gameObject.name);
            return;
        }

        if (examineLight)
        {
            examineLight.enabled = false;
        }

        delay = transform.root.gameObject.GetComponentInChildren<DelayEffect>();
        PlayerCam = scriptManager.MainCamera;
        ArmsCam = scriptManager.ArmsCamera;
        DefaultMainCamMask = PlayerCam.cullingMask;
        DefaultArmsCamMask = ArmsCam.cullingMask;
        pickupRange = interact.RaycastRange;
    }

    void Update()
    {
        if (crossPlatformInput.inputsLoaded)
        {
            crossPlatformDevice = crossPlatformInput.deviceType;

            Vector2 rotation = Vector2.zero;

            useControl = crossPlatformInput.ControlOf("Use");
            rotateKey = crossPlatformInput.GetInput<bool>("Fire");
            selectKey = crossPlatformInput.GetActionPressedOnce(this, "Fire");

            if (objectRaycast || firstExamine)
            {
                read_takeKey = crossPlatformInput.GetActionPressedOnce(this, "Use");
                examineKey = crossPlatformInput.GetActionPressedOnce(this, "Examine");
                cursorKey = crossPlatformInput.GetActionPressedOnce(this, "Zoom");
            }

            if (crossPlatformDevice == Device.Keyboard)
            {
                rotation = crossPlatformInput.GetMouseDelta();
            }
            else if (crossPlatformDevice == Device.Gamepad)
            {
                rotation = crossPlatformInput.GetInput<Vector2>("Look");
                movementVector = crossPlatformInput.GetInput<Vector2>("Movement");
            }

            if (Mathf.Abs(rotation.x) > rotationDeadzone)
            {
                rotateValue.x = -(rotation.x * rotateSpeed);
            }
            else
            {
                rotateValue.x = 0;
            }

            if (Mathf.Abs(rotation.y) > rotationDeadzone)
            {
                rotateValue.y = (rotation.y * rotateSpeed);
            }
            else
            {
                rotateValue.y = 0;
            }

            smoothRotation = Vector2.SmoothDamp(smoothRotation, rotateValue, ref rotationVelocity, Time.deltaTime * rotateSmoothing);
        }

        //Prevent Interact Dynamic Object when player is holding other object
        otherHeld = GetComponent<DragRigidbody>().CheckHold();

        if (gameManager.isPaused) return;

        if (objectRaycast && !antiSpam && firstExamine && firstExamine.examineType != InteractiveItem.ExamineType.None && !gameManager.isWeaponZooming)
        {
            if (examineKey && !otherHeld)
            {
                isExamining = !isExamining;
            }
        }

        if (isExamining)
        {
            if (!isObjectHeld)
            {
                FirstPhase();
                tryExamine = true;
            }
            else
            {
                HoldObject();
            }
        }
        else if (isObjectHeld)
        {
            if (!secondExamine)
            {
                DropObject();
            }
            else
            {
                SecondExaminedObject(false);
                isExamining = true;
            }
        }

        PlayerAim = PlayerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (!isInspect)
        {
            if (Physics.Raycast(PlayerAim, out RaycastHit hit, pickupRange, CullLayers))
            {
                if (hit.collider.gameObject.layer == InteractLayer)
                {
                    if (hit.collider.gameObject.GetComponent<InteractiveItem>())
                    {
                        objectRaycast = hit.collider.gameObject;
                        firstExamine = objectRaycast.GetComponent<InteractiveItem>();
                    }
                    else
                    {
                        if (!tryExamine)
                        {
                            objectRaycast = null;
                            firstExamine = null;
                        }
                    }
                }
                else
                {
                    if (!tryExamine)
                    {
                        objectRaycast = null;
                        firstExamine = null;
                    }
                }

                scriptManager.IsExamineRaycast = objectRaycast != null;
            }
            else
            {
                if (!tryExamine)
                {
                    objectRaycast = null;
                    firstExamine = null;
                    scriptManager.IsExamineRaycast = false;
                }
            }
        }

        if (priorityObject && isObjectHeld)
        {
            if (rotateKey && !isReading && !cursorShown && priorityObject.examineRotate != InteractiveItem.ExamineRotate.None)
            {
                firstFaceBreak = true;

                if (secondExamine)
                {
                    secondFaceBreak = false;
                }

                if (priorityObject.examineRotate == InteractiveItem.ExamineRotate.Both)
                {
                    priorityObject.transform.Rotate(PlayerCam.transform.up, smoothRotation.x, Space.World);
                    priorityObject.transform.Rotate(PlayerCam.transform.right, smoothRotation.y, Space.World);
                }
                else if (priorityObject.examineRotate == InteractiveItem.ExamineRotate.Horizontal)
                {
                    priorityObject.transform.Rotate(PlayerCam.transform.up, smoothRotation.x, Space.World);
                }
                else if (priorityObject.examineRotate == InteractiveItem.ExamineRotate.Vertical)
                {
                    priorityObject.transform.Rotate(PlayerCam.transform.right, smoothRotation.y, Space.World);
                }
            }

            if (isPaper)
            {
                if (read_takeKey)
                {
                    isReading = !isReading;
                }

                if (isReading)
                {
                    paperText.text = priorityObject.paperMessage;
                    paperText.fontSize = priorityObject.paperMessageSize;
                    paperUI.SetActive(true);
                }
                else
                {
                    paperUI.SetActive(false);
                }
            }
            else if (priorityObject.ItemType != InteractiveItem.Type.OnlyExamine && priorityObject.ItemType != InteractiveItem.Type.InteractObject && !isInspect)
            {
                if (read_takeKey && inventory.CheckInventorySpace())
                {
                    TakeObject(secondExamine, priorityObject.gameObject);
                }
            }

            if (priorityObject.enableCursor)
            {
                if (cursorKey)
                {
                    cursorShown = !cursorShown;

                    if (crossPlatformDevice == Device.Keyboard)
                    {
                        gameManager.ShowCursor(cursorShown);
                    }
                    else if (crossPlatformDevice == Device.Gamepad)
                    {
                        gameManager.ShowConsoleCursor(cursorShown);
                    }
                }
            }
            else
            {
                cursorShown = false;

                if (crossPlatformDevice == Device.Keyboard)
                {
                    gameManager.ShowCursor(false);
                }
                else if (crossPlatformDevice == Device.Gamepad)
                {
                    gameManager.ShowConsoleCursor(false);
                }
            }

            if (cursorShown)
            {
                Vector3 consoleCursorPos = gameManager.ConsoleCursor.transform.position;

                if (crossPlatformDevice == Device.Gamepad)
                {
                    gameManager.MoveConsoleCursor(movementVector);
                }

                if (selectKey)
                {
                    Vector3 mousePosition = crossPlatformInput.GetMousePosition();
                    Ray ray = PlayerCam.ScreenPointToRay(crossPlatformDevice == Device.Keyboard ? mousePosition : consoleCursorPos);

                    if (Physics.Raycast(ray, out RaycastHit rayHit, 5, CullLayers))
                    {
                        if (rayHit.collider.GetComponent<InteractiveItem>() && rayHit.collider.GetComponent<InteractiveItem>().examineCollect)
                        {
                            interact.Interact(rayHit.collider.gameObject);
                        }
                        else if (rayHit.collider.GetComponent<ExamineObjectAnimation>() && rayHit.collider.GetComponent<ExamineObjectAnimation>().isEnabled)
                        {
                            rayHit.collider.GetComponent<ExamineObjectAnimation>().PlayAnimation();
                        }
                        else if (rayHit.collider.GetComponent<InteractiveItem>())
                        {
                            if (rayHit.collider.GetComponent<InteractiveItem>() != firstExamine && !secondExamine)
                            {
                                secondExamine = rayHit.collider.GetComponent<InteractiveItem>();
                                SecondExaminedObject(true);
                            }
                        }
                        else
                        {
                            rayHit.collider.gameObject.SendMessage("Interact", SendMessageOptions.DontRequireReceiver);
                        }
                    }
                }
            }
        }
    }

    void SecondExaminedObject(bool isExamined)
    {
        if (secondExamine)
        {
            if (isExamined)
            {
                priorityObject = secondExamine;

                PlayerCam.cullingMask = SecMainCameraMask;
                ArmsCam.cullingMask = SecArmsCameraMask;

                ShowExamineUI(true);

                secondExamine.floatingIconEnabled = false;

                oldSecondObjT = secondExamine.transform.parent;
                oldSecondObjPos = secondExamine.transform.position;
                oldSecondRot = secondExamine.transform.rotation;

                secondExamine.transform.parent = null;

                if (secondExamine.faceToCamera)
                {
                    Vector3 rotation = secondExamine.faceRotation;
                    faceRotationSecond = Quaternion.LookRotation(PlayerCam.transform.forward, PlayerCam.transform.up) * Quaternion.Euler(rotation);
                    faceToCameraSecond = true;
                }

                foreach (MeshFilter mesh in objectHeld.GetComponentsInChildren<MeshFilter>())
                {
                    mesh.gameObject.layer = SecondExamineLayer;
                }

                if (secondExamine.examineType == InteractiveItem.ExamineType.AdvancedObject)
                {
                    if (secondExamine.CollidersDisable.Length > 0)
                    {
                        foreach (var col in secondExamine.CollidersDisable)
                        {
                            col.enabled = false;
                        }
                    }

                    if (secondExamine.CollidersEnable.Length > 0)
                    {
                        foreach (var col in secondExamine.CollidersEnable)
                        {
                            col.enabled = true;
                        }
                    }
                }
            }
            else
            {
                priorityObject = firstExamine;

                PlayerCam.cullingMask = MainCameraMask;
                ArmsCam.cullingMask = ArmsCameraMask;

                ShowExamineUI(false);

                secondExamine.transform.SetParent(oldSecondObjT);
                secondExamine.transform.position = oldSecondObjPos;
                secondExamine.transform.rotation = oldSecondRot;

                secondExamine.floatingIconEnabled = true;

                if (secondExamine.examineType == InteractiveItem.ExamineType.AdvancedObject)
                {
                    if (secondExamine.CollidersDisable.Length > 0)
                    {
                        foreach (var col in secondExamine.CollidersDisable)
                        {
                            col.enabled = true;
                        }
                    }

                    if (secondExamine.CollidersEnable.Length > 0)
                    {
                        foreach (var col in secondExamine.CollidersEnable)
                        {
                            col.enabled = false;
                        }
                    }
                }

                secondExamine = null;
                secondFaceBreak = false;
                faceToCameraSecond = false;

                foreach (MeshFilter mesh in objectHeld.GetComponentsInChildren<MeshFilter>())
                {
                    mesh.gameObject.layer = ExamineLayer;
                }
            }
        }
        else
        {
            priorityObject = firstExamine;
            ShowExamineUI(false);
            foreach (MeshFilter mesh in objectHeld.GetComponentsInChildren<MeshFilter>())
            {
                mesh.gameObject.layer = ExamineLayer;
            }
        }
    }

    void ShowExamineUI(bool SecondItem = false)
    {
        if (!SecondItem)
        {
            if (priorityObject.ItemType != InteractiveItem.Type.OnlyExamine)
            {
                if (!isInspect)
                {
                    if (priorityObject.ItemType == InteractiveItem.Type.InteractObject)
                    {
                        gameManager.ShowExamineSprites(btn2: false, btn3: priorityObject.examineRotate != InteractiveItem.ExamineRotate.None, btn4: priorityObject.enableCursor);
                    }
                    else
                    {
                        gameManager.ShowExamineSprites(btn2: inventory.CheckInventorySpace(), btn3: priorityObject.examineRotate != InteractiveItem.ExamineRotate.None, btn4: priorityObject.enableCursor);
                    }
                }
                else
                {
                    gameManager.ShowExamineSprites(PutAwayText: "Put Away", btn2: false, btn3: priorityObject.examineRotate != InteractiveItem.ExamineRotate.None, btn4: priorityObject.enableCursor);
                }
            }
            else
            {
                gameManager.ShowExamineSprites(btn2: false, btn3: priorityObject.examineRotate != InteractiveItem.ExamineRotate.None, btn4: priorityObject.enableCursor);
            }
        }
        else
        {
            InteractiveItem secondItem = secondExamine.GetComponent<InteractiveItem>();
            gameManager.ShowExamineSprites(PutAwayText: "Put Away", btn2: secondItem.ItemType != InteractiveItem.Type.OnlyExamine, btn3: priorityObject.examineRotate != InteractiveItem.ExamineRotate.None, btn4: priorityObject.enableCursor);
        }
    }

    void FirstPhase()
    {
        StartCoroutine(AntiSpam());

        distance = 0;

        priorityObject = firstExamine;
        objectHeld = objectRaycast.gameObject;
        objectPosition = objectHeld.transform.position;
        objectRotation = objectHeld.transform.rotation;

        isPaper = firstExamine.examineType == InteractiveItem.ExamineType.Paper;

        if (isPaper)
        {
            CollectibleObject co = objectHeld.GetComponent<CollectibleObject>();
            if (co && co.type == CollectibleObject.ObjectType.Letter)
            {
                co.OnItemInteracted();
            }
        }

        if (examineLight)
        {
            examineLight.enabled = true;
        }

        if (isInspect) { firstExamine.isExamined = true; }

        if (!isPaper)
        {
            if (!string.IsNullOrEmpty(firstExamine.examineName))
            {
                if (!firstExamine.isExamined)
                {
                    ShowExamineText(firstExamine.examineName);
                }
                else
                {
                    gameManager.isExamining = true;
                    gameManager.ShowExamineText(firstExamine.examineName);
                }
            }

            ShowExamineUI();
        }
        else
        {
            gameManager.ShowPaperExamineSprites(useControl, firstExamine.examineRotate != InteractiveItem.ExamineRotate.None, "Read");
        }

        if (firstExamine.examineSound)
        {
            Tools.PlayOneShot2D(transform.position, firstExamine.examineSound, firstExamine.examineVolume);
        }

        foreach (MeshFilter mesh in objectHeld.GetComponentsInChildren<MeshFilter>())
        {
            mesh.gameObject.layer = ExamineLayer;
        }

        foreach (MeshRenderer renderer in objectHeld.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        foreach (Collider col in objectHeld.GetComponentsInChildren<Collider>())
        {
            if (col.GetType() != typeof(MeshCollider))
            {
                col.isTrigger = true;
            }
        }

        if (firstExamine.examineType == InteractiveItem.ExamineType.AdvancedObject)
        {
            if (firstExamine.CollidersDisable.Length > 0)
            {
                foreach (var col in objectHeld.GetComponent<InteractiveItem>().CollidersDisable)
                {
                    col.enabled = false;
                }
            }

            if (firstExamine.CollidersEnable.Length > 0)
            {
                foreach (var col in objectHeld.GetComponent<InteractiveItem>().CollidersEnable)
                {
                    col.enabled = true;
                }
            }
        }

        foreach (Rigidbody rb in objectHeld.GetComponentsInChildren<Rigidbody>())
        {
            ExamineRBs.Add(new RigidbodyExamine(rb.gameObject, new RigidbodyExamine.RigidbodyParams() { isKinematic = rb.isKinematic, useGravity = rb.useGravity }));
        }

        foreach (var col in objectHeld.GetComponentsInChildren<Collider>())
        {
            Physics.IgnoreCollision(col, PlayerCam.transform.root.gameObject.GetComponent<CharacterController>(), true);
        }

        if (firstExamine.faceToCamera)
        {
            Vector3 rotation = objectHeld.GetComponent<InteractiveItem>().faceRotation;
            faceRotationFirst = Quaternion.LookRotation(PlayerCam.transform.forward, PlayerCam.transform.up) * Quaternion.Euler(rotation);
            faceToCameraFirst = true;
        }

        PlayerCam.cullingMask = MainCameraMask;
        ArmsCam.cullingMask = ArmsCameraMask;

        SetFloatingIconsVisible(false);

        delay.isEnabled = false;
        gameManager.UIPreventOverlap(true);
        gameManager.HideSprites(HideHelpType.Interact);
        gameManager.LockPlayerControls(false, false, false, 1, true, true, 1);
        GetComponent<ScriptManager>().ScriptEnabledGlobal = false;
        distance = firstExamine.examineDistance;
        itemSwitcher.FreeHands(true);

        isObjectHeld = true;
    }

    void SetFloatingIconsVisible(bool visible)
    {
        GameObject SecondItem = null;

        if (!firstExamine) return;

        if (firstExamine.examineType == InteractiveItem.ExamineType.AdvancedObject)
        {
            if (objectHeld.GetComponentsInChildren<Transform>().Count(obj => floatingItem.ContainsFloatingIcon(obj.gameObject)) > 0)
            {
                foreach (var item in objectHeld.GetComponentsInChildren<Transform>().Where(obj => floatingItem.ContainsFloatingIcon(obj.gameObject)).ToArray())
                {
                    if (item.GetComponent<InteractiveItem>())
                    {
                        item.GetComponent<InteractiveItem>().floatingIconEnabled = !visible;
                        SecondItem = item.gameObject;
                    }
                }
            }
        }

        foreach (var item in floatingItem.FloatingIconCache)
        {
            if (item.FollowObject != SecondItem)
            {
                if (item.FollowObject.GetComponent<InteractiveItem>())
                {
                    item.FollowObject.GetComponent<InteractiveItem>().floatingIconEnabled = visible;
                }
            }
        }
    }

    void HoldObject()
    {
        interact.CrosshairVisible(false);
        pfunc.enabled = false;

        Vector3 nextPos = PlayerCam.transform.position + PlayerAim.direction * distance;

        if (secondExamine)
        {
            Vector3 second_nextPos = PlayerCam.transform.position + PlayerAim.direction * secondExamine.GetComponent<InteractiveItem>().examineDistance;
            secondExamine.transform.position = Vector3.Lerp(secondExamine.transform.position, second_nextPos, Time.deltaTime * pickupSpeed);

            if (!secondFaceBreak && faceToCameraSecond)
            {
                secondExamine.transform.rotation = Quaternion.Lerp(secondExamine.transform.rotation, faceRotationSecond, Time.deltaTime * pickupRotateSpeed);
            }
        }

        if (objectHeld)
        {
            if (objectHeld.GetComponent<Rigidbody>())
            {
                objectHeld.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                objectHeld.GetComponent<Rigidbody>().isKinematic = true;
                objectHeld.GetComponent<Rigidbody>().useGravity = false;
            }

            objectHeld.transform.position = Vector3.Lerp(objectHeld.transform.position, nextPos, Time.deltaTime * pickupSpeed);

            if (!firstFaceBreak && faceToCameraFirst)
            {
                objectHeld.transform.rotation = Quaternion.Lerp(objectHeld.transform.rotation, faceRotationFirst, Time.deltaTime * pickupRotateSpeed);
            }
        }
    }

    public void ExamineObject(GameObject @object, Vector3 rotation)
    {
        Debug.Log("Here");
        if (@object.GetComponent<CollectibleObject>()) @object.GetComponent<CollectibleObject>().OnItemInteracted();
        InteractiveItem item = @object.GetComponent<InteractiveItem>();
        firstExamine = item;
        priorityObject = item;

        float inspectDist = item.examineDistance;
        Vector3 nextPos = PlayerCam.transform.position + PlayerAim.direction * inspectDist;

        if (!faceToCameraInspect)
        {
            Quaternion faceRotation = Quaternion.LookRotation(PlayerCam.transform.forward, PlayerCam.transform.up) * Quaternion.Euler(rotation);
            @object.transform.rotation = faceRotation;
            faceToCameraInspect = true;
        }

        @object.name = "Inspect: " + item.examineName;
        if (@object.GetComponent<Rigidbody>())
        {
            @object.GetComponent<Rigidbody>().isKinematic = false;
            @object.GetComponent<Rigidbody>().useGravity = false;
        }
        @object.transform.position = nextPos;

        if (@object.GetComponent<SaveObject>())
        {
            Destroy(@object.GetComponent<SaveObject>());
        }

        foreach (var col in @object.GetComponentsInChildren<Collider>())
        {
            Physics.IgnoreCollision(col, PlayerCam.transform.root.gameObject.GetComponent<CharacterController>(), true);
        }

        objectRaycast = @object;
        isExamining = true; //Examine Object
        isInspect = true;
    }

    public void CancelExamine()
    {
        isExamining = false;
    }

    void DropObject()
    {
        SetFloatingIconsVisible(true);
        SecondExaminedObject(false);
        distance = 0;

        if (examineLight)
        {
            examineLight.enabled = false;
        }

        foreach (MeshFilter mesh in objectHeld.GetComponentsInChildren<MeshFilter>())
        {
            mesh.gameObject.layer = InteractLayer;
        }

        foreach (MeshRenderer renderer in objectHeld.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        foreach (Collider col in objectHeld.GetComponentsInChildren<Collider>())
        {
            if (col.GetType() != typeof(MeshCollider))
            {
                col.isTrigger = false;
            }
        }

        if (firstExamine.examineType == InteractiveItem.ExamineType.AdvancedObject)
        {
            if (firstExamine.CollidersDisable.Length > 0)
            {
                foreach (var col in objectHeld.GetComponent<InteractiveItem>().CollidersDisable)
                {
                    col.enabled = true;
                }
            }

            if (firstExamine.CollidersEnable.Length > 0)
            {
                foreach (var col in objectHeld.GetComponent<InteractiveItem>().CollidersEnable)
                {
                    col.enabled = false;
                }
            }
        }

        if (!isInspect)
        {
            ObjectPutter putter = objectHeld.AddComponent<ObjectPutter>();
            putter.Put(objectPosition, objectRotation, putBackTime, putBackRotateTime, ExamineRBs.ToArray());
        }
        else
        {
            Destroy(objectHeld);
            isInspect = false;
        }

        if (!isPaper)
        {
            if (objectHeld.GetComponent<Collider>().GetType() != typeof(MeshCollider))
            {
                objectHeld.GetComponent<Collider>().isTrigger = false;
            }
        }

        StopAllCoroutines();
        GetComponent<ScriptManager>().ScriptEnabledGlobal = true;
        gameManager.UIPreventOverlap(false);
        gameManager.HideExamine();
        gameManager.HideSprites(HideHelpType.Help);
        gameManager.ShowConsoleCursor(false);
        scriptManager.IsExamineRaycast = false;
        floatingItem.SetAllIconsVisible(true);
        delay.isEnabled = true;
        PlayerCam.cullingMask = DefaultMainCamMask;
        ArmsCam.cullingMask = DefaultArmsCamMask;

        paperUI.SetActive(false);
        pfunc.enabled = true;
        isObjectHeld = false;
        isExamining = false;
        isReading = false;
        tryExamine = false;
        cursorShown = false;

        firstFaceBreak = false;
        secondFaceBreak = false;
        faceToCameraFirst = false;
        faceToCameraSecond = false;
        faceToCameraInspect = false;

        firstExamine = null;
        priorityObject = null;
        objectRaycast = null;
        objectHeld = null;
        ExamineRBs.Clear();

        onDropObject?.Invoke(this);
        itemSwitcher.FreeHands(false);

        StartCoroutine(AntiSpam());
        StartCoroutine(UnlockPlayer());
    }

    void TakeObject(bool takeSecond, GameObject take)
    {
        SecondExaminedObject(false);

        if (!takeSecond)
        {
            StopAllCoroutines();
            GetComponent<ScriptManager>().ScriptEnabledGlobal = true;
            gameManager.UIPreventOverlap(false);
            gameManager.HideExamine();
            gameManager.HideSprites(HideHelpType.Help);
            gameManager.ShowConsoleCursor(false);
            floatingItem.SetAllIconsVisible(true);
            delay.isEnabled = true;
            PlayerCam.cullingMask = DefaultMainCamMask;
            ArmsCam.cullingMask = DefaultArmsCamMask;
            paperUI.SetActive(false);
            pfunc.enabled = true;
            firstExamine = null;
            isObjectHeld = false;
            isExamining = false;
            isReading = false;
            tryExamine = false;
            cursorShown = false;

            firstFaceBreak = false;
            faceToCameraFirst = false;
            faceToCameraInspect = false;

            objectRaycast = null;
            objectHeld = null;
            ExamineRBs.Clear();
            itemSwitcher.FreeHands(false);

            if (examineLight)
            {
                examineLight.enabled = false;
            }

            StartCoroutine(AntiSpam());
            StartCoroutine(UnlockPlayer());
        }

        interact.Interact(take);
    }

    void ShowExamineText(string ExamineName)
    {
        gameManager.isExamining = true;
        StopCoroutine(DoExamine());
        StartCoroutine(DoExamine(ExamineName));
    }

    IEnumerator DoExamine(string ExamineName = "")
    {
        InteractiveItem[] examineItems = FindObjectsOfType<InteractiveItem>().Where(i => i.examineName == ExamineName).ToArray();
        yield return new WaitForSeconds(timeToExamine);

        gameManager.ShowExamineText(ExamineName);

        if (examinedSound)
        {
            Tools.PlayOneShot2D(transform.position, examinedSound, examinedVolume);
        }

        foreach (var inst in examineItems)
        {
            inst.isExamined = true;
        }
    }

    IEnumerator UnlockPlayer()
    {
        yield return new WaitForFixedUpdate();
        gameManager.LockPlayerControls(true, true, false, 1, false, false, 2);
        interact.CrosshairVisible(true);
    }

    IEnumerator AntiSpam()
    {
        antiSpam = true;
        yield return new WaitForSeconds(spamWaitTime);
        antiSpam = false;
    }
}