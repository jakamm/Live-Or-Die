using UnityEngine;
using ThunderWire.CrossPlatform.Input;

public class DynamicObjectController : MonoBehaviour
{
    private CrossPlatformInput crossPlatformInput;
    private HFPS_GameManager gameManager;
    private DynamicObject dynamicObj;
    private DelayEffect delay;
    private Camera mainCamera;

    [Header("Raycast")]
    public LayerMask CullLayers;
    [Layer] public int InteractLayer;
    public float MaxHoldDistance;

    [Header("Dynamic Settings")]
    public float doorMoveSpeed;
    public float drawerMoveSpeed;
    public float leverMoveSpeed;
    public float valveSliderSpeed;

    [Header("Smoothing")]
    public float doorDragSmoothing = 10f;
    public float drawerDragSmoothing = 10f;
    public float levelDragSmoothing = 10f;

    private bool UseKey;
    private GameObject raycastObject;

    private Vector2 mouseInput;

    private bool isOutOfDistance;
    private bool isOtherHolding;
    private bool isHolding;
    private bool isDynamic;
    private bool firstPass;
    private bool alrLock;

    private float RayLength;
    private float mouseSmooth;

    void Awake()
    {
        crossPlatformInput = CrossPlatformInput.Instance;
        mainCamera = ScriptManager.Instance.MainCamera;
        gameManager = HFPS_GameManager.Instance;
        delay = transform.root.GetComponentInChildren<DelayEffect>(true);
        RayLength = GetComponent<InteractManager>().RaycastRange;
    }

    void Update()
    {
        if (crossPlatformInput.inputsLoaded)
        {
            UseKey = crossPlatformInput.GetInput<bool>("Use");

            if (crossPlatformInput.deviceType == Device.Keyboard)
            {
                mouseInput = crossPlatformInput.GetMouseDelta();
            }
            else if (crossPlatformInput.deviceType == Device.Gamepad)
            {
                mouseInput = crossPlatformInput.GetInput<Vector2>("Movement");
            }
        }

        //Prevent Interact Dynamic Object when player is holding other object
        isOtherHolding = GetComponent<DragRigidbody>().CheckHold();

        if (raycastObject && !isOtherHolding && !gameManager.isWeaponZooming && isDynamic && !isOutOfDistance)
        {
            if (UseKey)
            {
                if (!firstPass)
                {
                    FirstPass();
                }
                else
                {
                    UseDynamicObject(dynamicObj.dynamicType);
                    isHolding = true;
                }
            }
            else if (isHolding)
            {
                ReleaseObject();
                isHolding = false;
            }
        }

        if (isHolding)
        {
            gameManager.LockScript<MouseLook>(false);
            delay.isEnabled = false;
            alrLock = false;

            if (raycastObject && raycastObject.GetComponent<DynamicObject>())
            {
                raycastObject.GetComponent<DynamicObject>().isHolding = true;
            }

            isOutOfDistance = Vector3.Distance(raycastObject.transform.position, transform.root.position) >= MaxHoldDistance;
        }
        else
        {
            if (!alrLock)
            {
                gameManager.LockScript<MouseLook>(true);
                alrLock = true;
            }

            if (raycastObject && raycastObject.GetComponent<DynamicObject>())
            {
                raycastObject.GetComponent<DynamicObject>().isHolding = false;
            }
        }

        if (isOutOfDistance)
        {
            ReleaseObject();
        }

        Ray playerAim = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(playerAim, out RaycastHit hit, RayLength, CullLayers) && !isHolding)
        {
            if (IsDynamicObject(hit))
            {
                raycastObject = hit.collider.gameObject;
                dynamicObj = raycastObject.GetComponent<DynamicObject>();
                isDynamic = true;
            }
            else if(raycastObject && !isHolding)
            {
                raycastObject = null;
                isDynamic = false;
            }
        }
        else if (raycastObject && !isHolding)
        {
            raycastObject = null;
        }
    }

    private bool IsDynamicObject(RaycastHit hit)
    {
        GameObject raycastObj = hit.collider.gameObject;
        return hit.collider.gameObject.layer == InteractLayer && raycastObj.GetComponent<DynamicObject>();
    }

    private void ReleaseObject()
    {
        StopAllCoroutines();
        dynamicObj.mouseInput = 0;
        isHolding = false;
        delay.isEnabled = true;
        isOutOfDistance = false;
        firstPass = false;
        dynamicObj = null;
        raycastObject = null;
        gameManager.ValveSlider.gameObject.SetActive(false);
        gameManager.ValveSlider.value = 0f;
        gameManager.LockPlayerControls(true, true, false);
    }

    void FirstPass()
    {
        if (dynamicObj.dynamicType == Type_Dynamic.Valve)
        {
            ValvePass();
        }

        if (crossPlatformInput.deviceType == Device.Gamepad)
        {
            gameManager.LockPlayerControls(false, true, false);
        }

        firstPass = true;
    }

    private void UseDynamicObject(Type_Dynamic DynamicType)
    {
        switch (DynamicType)
        {
            case Type_Dynamic.Door:             IsDoor();   break;
            case Type_Dynamic.Drawer:           IsDrawer(); break;
            case Type_Dynamic.Lever:            IsLever();  break;
            case Type_Dynamic.Valve:            IsValve();  break;
            case Type_Dynamic.MovableInteract:  IsDrawer(); break;
        }
    }

    private void IsDoor()
    {
        if (!GetDynamicUseType()) return;

        if (dynamicObj.interactType == Type_Interact.Mouse)
        {
            HingeJoint joint = raycastObject.GetComponent<HingeJoint>();
            JointMotor motor = joint.motor;
            float mouseForce = mouseInput.x;
            float mouseVelocity = 0;

            mouseSmooth = Mathf.SmoothDamp(mouseSmooth, mouseForce, ref mouseVelocity, Time.deltaTime * doorDragSmoothing);

            motor.targetVelocity = mouseSmooth * (doorMoveSpeed * 10);
            motor.force = doorMoveSpeed * 10;
            joint.motor = motor;
            joint.useMotor = true;
        }
    }

    private void IsDrawer()
    {
        if (!GetDynamicUseType()) return;

        if (dynamicObj.interactType == Type_Interact.Mouse)
        {
            if (dynamicObj.reverseMove)
            {
                mouseInput.y = -mouseInput.y;
            }

            float mouseForce = mouseInput.y * drawerMoveSpeed;

            dynamicObj.mouseSmoothing = drawerDragSmoothing;
            dynamicObj.mouseInput = mouseForce;
        }
    }

    private void IsLever()
    {
        if (dynamicObj.interactType == Type_Interact.Mouse)
        {
            HingeJoint joint = raycastObject.GetComponent<HingeJoint>();
            JointMotor motor = joint.motor;

            float mouseForce = mouseInput.y;
            float mouseVelocity = 0;
            mouseSmooth = Mathf.SmoothDamp(mouseSmooth, mouseForce, ref mouseVelocity, Time.deltaTime * levelDragSmoothing);

            motor.targetVelocity = mouseSmooth * (leverMoveSpeed * 10);
            motor.force = leverMoveSpeed * 10;
            joint.motor = motor;
            joint.useMotor = true;
        }
    }

    private void IsValve()
    {
        if (dynamicObj.rotateValue < 1f)
        {
            float z = dynamicObj.valveTurnSpeed;
            Type_Axis axis = dynamicObj.turnAxis;
            Vector3 rotation = new Vector3(axis == Type_Axis.AxisX ? -z : 0, axis == Type_Axis.AxisY ? -z : 0, axis == Type_Axis.AxisZ ? -z : 0);
            raycastObject.transform.Rotate(rotation);
            dynamicObj.rotateValue = Mathf.MoveTowards(dynamicObj.rotateValue, 1f, Time.deltaTime * valveSliderSpeed);
            gameManager.ValveSlider.value = dynamicObj.rotateValue;
        }
    }

    void ValvePass()
    {
        if (dynamicObj.rotateValue < 1f)
        {
            gameManager.ValveSlider.value = dynamicObj.rotateValue;
            gameManager.ValveSlider.gameObject.SetActive(true);
        }
    }

    bool GetDynamicUseType()
    {
        if (dynamicObj && !dynamicObj.hasKey)
        {
            if (dynamicObj.useType == Type_Use.Locked || dynamicObj.useType == Type_Use.Jammed)
            {
                if (!string.IsNullOrEmpty(dynamicObj.customText))
                {
                    gameManager.ShowHint(dynamicObj.customText);
                }

                return false;
            }
        }

        return true;
    }
}
