using System.Collections;
using UnityEngine;
using Newtonsoft.Json.Linq;
using ThunderWire.Utility;
using ThunderWire.CrossPlatform.Input;
using ThunderWire.Game.Options;

public class MouseLook : MonoBehaviour, IJsonListener
{
    private JsonHandler jsonHandler;
    private CrossPlatformInput crossPlatformInput;
    private Timekeeper timekeeper = new Timekeeper();
    private Camera mainCamera;
    private GameObject player;

    public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
    public RotationAxes axes = RotationAxes.MouseXAndY;

    public bool isLocalCamera;
    public bool smoothLook;
    public float smoothTime = 5f;

    public float sensitivityX = 15F;
    public float sensitivityY = 15F;

    public float minimumX = -60F;
    public float maximumX = 60F;

    public float minimumY = -60F;
    public float maximumY = 60F;

    public float offsetY = 0F;
    public float offsetX = 0F;

    public float rotationX = 0F;
    public float rotationY = 0F;

    [Header("Load Prefixes")]
    public string mousePrefix;
    public string stickVPrefix;
    public string stickHPrefix;
    public string invertlookPrefix;

    private float deltaInputX;
    private float deltaInputY;
    private bool invertLook;
    private bool lockLook;

    private Vector2 lerpRotation;
    private float lerpSpeed;
    private bool doLerpLook;

    Vector2 clampRange;
    Quaternion originalRotation;

    [HideInInspector]
    public bool bodyClamp = false;

    [HideInInspector]
    public Quaternion playerOriginalRotation;

    void Awake()
    {
        jsonHandler = HFPS_GameManager.Instance.GetComponent<JsonHandler>();
        crossPlatformInput = CrossPlatformInput.Instance;
        player = transform.root.gameObject;
    }

    void Start()
    {
        if (!isLocalCamera)
        {
            mainCamera = Tools.MainCamera();
        }
        else
        {
            mainCamera = GetComponent<Camera>();
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // Make the rigid body not change rotation
        if (GetComponent<Rigidbody>())
        {
            GetComponent<Rigidbody>().freezeRotation = true;
        }

        originalRotation = transform.localRotation;
        playerOriginalRotation = player.transform.localRotation;
    }

    public void OnJsonChanged()
    {
        if (jsonHandler)
        {
            JObject root = jsonHandler.Json();

            if (root[OptionsController.OPTIONS_PC_PREFIX] != null)
            {
                if (root[OptionsController.OPTIONS_PC_PREFIX]["general"][mousePrefix] != null)
                {
                    float sensitivity = (float)root[OptionsController.OPTIONS_PC_PREFIX]["general"][mousePrefix];

                    if (sensitivity > 0)
                    {
                        sensitivityX = sensitivity;
                        sensitivityY = sensitivity;
                    }
                }
            }
            else if (root[OptionsController.OPTIONS_CONSOLE_PREFIX] != null)
            {
                if (root[OptionsController.OPTIONS_CONSOLE_PREFIX]["general"][stickVPrefix] != null)
                {
                    sensitivityX = (float)root[OptionsController.OPTIONS_CONSOLE_PREFIX]["general"][stickVPrefix];
                }

                if (root[OptionsController.OPTIONS_CONSOLE_PREFIX]["general"][stickHPrefix] != null)
                {
                    sensitivityY = (float)root[OptionsController.OPTIONS_CONSOLE_PREFIX]["general"][stickHPrefix];
                }

                if (root[OptionsController.OPTIONS_CONSOLE_PREFIX]["general"][invertlookPrefix] != null)
                {
                    invertLook = (bool)root[OptionsController.OPTIONS_CONSOLE_PREFIX]["general"][invertlookPrefix];
                }
            }
        }
    }

    void Update()
    {
        timekeeper.UpdateTime();

        if (!lockLook)
        {
            doLerpLook = false;

            if (crossPlatformInput.deviceType == Device.Gamepad)
            {
                Vector2 look;
                if ((look = crossPlatformInput.GetInput<Vector2>("Look")) != null)
                {
                    deltaInputX = look.x;

                    if (!invertLook)
                    {
                        deltaInputY = look.y;
                    }
                    else
                    {
                        deltaInputY = look.y * -1;
                    }
                }
            }
            else
            {
                Vector2 mouse = crossPlatformInput.GetMouseDelta();
                deltaInputX = mouse.x;

                if (!invertLook)
                {
                    deltaInputY = mouse.y;
                }
                else
                {
                    deltaInputY = mouse.y * -1;
                }
            }
        }
        else if (doLerpLook)
        {
            rotationX = Mathf.LerpAngle(rotationX, lerpRotation.x, timekeeper.deltaTime * (lerpSpeed * 1.5f));
            rotationY = Mathf.LerpAngle(rotationY, lerpRotation.y, timekeeper.deltaTime * lerpSpeed);
        }

        if (Cursor.lockState == CursorLockMode.None)
            return;

        if (axes == RotationAxes.MouseXAndY)
        {
            // Read the mouse input axis
            rotationX += (deltaInputX * sensitivityX / 30 * mainCamera.fieldOfView + offsetX);
            rotationY += (deltaInputY * sensitivityY / 30 * mainCamera.fieldOfView + offsetY);

            rotationX = ClampAngle(rotationX, minimumX, maximumX);

            if (bodyClamp) {
                rotationX = ClampBodyAngle(rotationX);
            }

            rotationY = ClampAngle(rotationY, minimumY, maximumY);

            Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
            Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, Vector3.left);

            Quaternion playerRotation = playerOriginalRotation * xQuaternion;
            Quaternion lookRotation = originalRotation * yQuaternion;

            if (smoothLook)
            {
                player.transform.localRotation = Quaternion.Slerp(player.transform.localRotation, playerRotation, smoothTime * timekeeper.deltaTime);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, lookRotation, smoothTime * timekeeper.deltaTime);
            }
            else
            {
                player.transform.localRotation = playerRotation;
                transform.localRotation = lookRotation;
            }
        }
        else if (axes == RotationAxes.MouseX)
        {
            rotationX += (deltaInputX * sensitivityX / 60 * mainCamera.fieldOfView + offsetX);
            rotationX = ClampAngle(rotationX, minimumX, maximumX);

            Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
            transform.localRotation = originalRotation * xQuaternion;
        }
        else
        {
            rotationY += (deltaInputY * sensitivityY / 60 * mainCamera.fieldOfView + offsetY);
            rotationY = ClampAngle(rotationY, minimumY, maximumY);

            Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, Vector3.left);
            transform.localRotation = originalRotation * yQuaternion;
        }

        offsetY = 0F;
        offsetX = 0F;
    }

    public void LockLook(bool state)
    {
        lockLook = state;
    }

    public void LerpLook(Vector2 rotation, float speed, bool lockLook)
    {
        this.lockLook = lockLook;
        lerpRotation = rotation;
        lerpSpeed = speed;

        deltaInputX = 0;
        deltaInputY = 0;

        StartCoroutine(LerpLookWait());
    }

    IEnumerator LerpLookWait()
    {
        yield return new WaitForEndOfFrame();
        doLerpLook = true;
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        float newAngle = Tools.FixAngle(angle);
        return Mathf.Clamp(newAngle, min, max);
    }

    public void SetClampRange(float min, float max)
    {
        float y = rotationX;

        float angle1 = Tools.FixAngle(y + min);
        float angle2 = Tools.FixAngle(y + max);

        clampRange = new Vector2(angle1, angle2);
    }

    public float ClampBodyAngle(float angle)
    {
        return Mathf.Clamp(angle, clampRange.x, clampRange.y);
    }

    public Vector2 GetRotation()
    {
        return new Vector2(rotationX, rotationY);
    }

    public Vector2 GetInputDelta()
    {
        return new Vector2(deltaInputX, deltaInputY);
    }

    public void SetRotation(Vector2 rotation)
    {
        rotationX = rotation.x;
        rotationY = rotation.y;
    }
}