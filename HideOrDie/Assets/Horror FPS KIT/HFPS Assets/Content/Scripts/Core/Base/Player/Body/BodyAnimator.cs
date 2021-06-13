/*
 * BodyAnimator.cs - wirted by ThunderWire Games
 * ver. 2.1
*/

using System.Linq;
using UnityEngine;

public class BodyAnimator : MonoBehaviour
{
    private HFPS_GameManager gameManager;
    private PlayerController controller;
    private HealthManager health;
    private MouseLook mouseLook;
    private Animator anim;
    private Transform cam;

    [Header("Main")]
    public Transform MiddleSpine;
    [Layer] public int InvisibleLayer;

    public float TurnSmooth;
    public float AdjustSmooth;
    public float OverrideSmooth;
    public float BackOverrideSmooth;

    [Header("Angled Body Rotation")]
    public bool angledBody;
    public float rotateBodySpeed;
    public int dirAngleIncrease;
    public int deadzonePow;
    public int minAngleBody;
    public int maxAngleBody;
    public int minBodyMaxDeadzone;
    public int maxBodyMaxDeadzone;

    [Header("Speed")]
    public float animWalkSpeed = 1f;
    public float animCrouchSpeed = 1f;
    public float animRunSpeed = 1f;
    public float animWaterSpeed = 0.5f;

    [Header("Misc")]
    public float turnMouseTrigger = 0.5f;
    public float animStartVelocity = 0.2f;
    public float blockStopVelocity = 0.1f;
    public float turnLeftRightDelay = 0.2f;
    public bool velocityBasedAnim = true;
    public bool enableShadows = true;
    public bool visibleToCamera = true;
    public bool proneDisableBody;
    public bool showBodyGizmos;

    [Header("Body Death Settings")]
    public bool ragdollDeath;
    public Transform CameraRoot;
    public Transform NewDeathCamParent;
    [Layer] public int LimbVisibleLayer;
    public GameObject[] ActivateLimbs;
    public GameObject[] DeactivateLimbs;

    [Header("Body Adjustment")]
    [Space(10)]
    public Vector3 originalOffset;
    [Space(5)]
    public Vector3 runningOffset;
    [Space(5)]
    public Vector3 crouchOffset;
    [Space(5)]
    public Vector3 jumpOffset;
    [Space(5)]
    public Vector3 proneOffset;
    [Space(5)]
    public Vector3 turnOffset;
    [Space(10)]
    public Vector3 bodyAngle;
    [Space(5)]
    public Vector2 spineMaxRotation;

    private RagdollPart[] ragdollParts;

    private Vector2 movement;
    private Vector3 localBodyAngle;
    private Vector3 adjustedSpineEuler;

    private float spineAngle;
    private float mouseSpeed;
    private float yBodyRotation;
    private float yBodyAngle;
    private float angleSpeed;
    private float tempArmsWeight;
    private float inputAngle;
    private float movementInput;
    private float animationSpeed;
    private float inputMagnitude;

    private bool blockWalk = false;
    private bool bodyControl = false;
    private bool ladderReady = false;
    private bool death = false;

    void Awake()
    {
        gameManager = HFPS_GameManager.Instance;
        controller = PlayerController.Instance;
        health = controller.GetComponent<HealthManager>();
        mouseLook = ScriptManager.Instance.GetComponent<MouseLook>();
        cam = ScriptManager.Instance.MainCamera.transform;
        anim = GetComponentInChildren<Animator>();

        localBodyAngle = transform.localEulerAngles;
        localBodyAngle.y = 0;
        originalOffset = transform.localPosition;

        yBodyAngle = transform.root.eulerAngles.y;
        Vector3 current = transform.localEulerAngles; current.x = 0; current.z = 0;
        Vector3 angle = bodyAngle + current;
        yBodyRotation = yBodyAngle + angle.y;
        transform.localEulerAngles = angle;

        angleSpeed = rotateBodySpeed;
    }

    void Start()
    {
        anim.SetBool("Idle", true);

        if (!enableShadows)
        {
            foreach (SkinnedMeshRenderer renderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        if (!visibleToCamera)
        {
            foreach (SkinnedMeshRenderer renderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.gameObject.layer = InvisibleLayer;
            }
        }

        mouseLook.SetClampRange(minBodyMaxDeadzone, maxBodyMaxDeadzone);

        ragdollParts = (from obj in anim.GetComponentsInChildren<Rigidbody>()
                        let col = obj.GetComponent<Collider>()
                        select new RagdollPart(col, obj)).ToArray();

        Ragdoll(false);
    }

    float InputToAngle(Vector2 input)
    {
        float raw = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;

        if (raw < (-90 - dirAngleIncrease))
        {
            raw += 180 + dirAngleIncrease;
        }
        else if (raw > (90 + dirAngleIncrease))
        {
            raw -= 180 + dirAngleIncrease;
        }

        return raw;
    }

    float InputMagnitude(Vector2 input)
    {
        float mag = Mathf.Clamp01(new Vector2(input.x, input.y).magnitude);
        return input.y > 0.01f ? mag : input.y < -0.01f ? mag * -1 : mag;
    }

    float InputMagnitudeClamped(Vector2 input)
    {
        return Mathf.Clamp01(new Vector2(input.x, input.y).magnitude);
    }

    void Update()
    {
        if (mouseLook)
        {
            mouseSpeed = mouseLook.GetInputDelta().x;
        }

        ladderReady = controller.ladderReady;
        movement = controller.GetMovementValue();
        inputAngle = InputToAngle(movement);
        movementInput = InputMagnitude(movement);
        inputMagnitude = InputMagnitudeClamped(movement);

        anim.SetBool("Crouch", controller.characterState != PlayerController.CharacterState.Stand);
        anim.SetBool("ClimbLadder", ladderReady);

        int state = (int)controller.characterState;

        if (controller.isControllable && !ladderReady)
        {
            /* POSITIONING */
            if (controller.isRunning && state == 0 && movement.y > 0.1f)
            {
                if (controller.velMagnitude >= animStartVelocity)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, runningOffset, Time.deltaTime * AdjustSmooth);
                }
            }
            else if (!controller.IsGrounded())
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, jumpOffset, Time.deltaTime * AdjustSmooth);
            }
            else if (state == 1)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, crouchOffset, Time.deltaTime * AdjustSmooth);
            }
            else if (state == 2)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, proneOffset, Time.deltaTime * AdjustSmooth);
            }
            else if (movement.x > 0.1f || movement.x < -0.1f)
            {
                if (controller.velMagnitude >= animStartVelocity)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, turnOffset, Time.deltaTime * AdjustSmooth);
                }
            }
            else
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, originalOffset, Time.deltaTime * AdjustSmooth);
            }

            if (controller.velMagnitude >= animStartVelocity)
            {
                blockWalk = false;

                /* MOVEMENT ANIMATIONS */
                movementInput = InputMagnitude(movement);
                anim.SetFloat("Movement", movementInput);

                /* ROTATIONS */
                localBodyAngle.y = inputAngle;
            }
            else if (controller.velMagnitude <= blockStopVelocity && Mathf.Abs(movement.x) < 0.1f)
            {
                localBodyAngle.y = 0;
                anim.SetBool("Run", false);
                anim.SetFloat("Movement", 0f);
                blockWalk = true;
            }

            if (!controller.IsGrounded())
            {
                localBodyAngle.y = 0;
                anim.SetBool("Jump", true);
                anim.SetBool("Idle", false);
                tempArmsWeight = Mathf.Lerp(tempArmsWeight, 1, Time.deltaTime * OverrideSmooth);
                bodyControl = false;
            }
            else
            {
                if (inputMagnitude < 0.1f || blockWalk)
                {
                    anim.SetBool("Idle", true);
                }
                else
                {
                    anim.SetBool("Idle", false);
                }

                bodyControl = inputMagnitude < 0.1f;

                anim.SetBool("Jump", false);
                anim.SetBool("Run", controller.isRunning && !blockWalk);
                tempArmsWeight = Mathf.Lerp(tempArmsWeight, 0, Time.deltaTime * BackOverrideSmooth);
            }

            if (!angledBody)
            {
                if (gameManager.IsEnabled<MouseLook>())
                {
                    if (mouseSpeed > turnMouseTrigger)
                    {
                        anim.SetBool("TurningRight", true);
                        anim.SetBool("TurningLeft", false);
                    }
                    else if (mouseSpeed < -turnMouseTrigger)
                    {
                        anim.SetBool("TurningRight", false);
                        anim.SetBool("TurningLeft", true);
                    }
                    else
                    {
                        anim.SetBool("TurningRight", false);
                        anim.SetBool("TurningLeft", false);
                    }
                }
                else
                {
                    anim.SetBool("TurningRight", false);
                    anim.SetBool("TurningLeft", false);
                }
            }
            else
            {
                if (Mathf.Abs(mouseSpeed) < 0.1f)
                {
                    anim.SetBool("TurningRight", false);
                    anim.SetBool("TurningLeft", false);
                }
            }
        }
        else
        {
            if (!ladderReady)
            {
                if (controller.IsGrounded())
                {
                    tempArmsWeight = Mathf.Lerp(tempArmsWeight, 0, Time.deltaTime * BackOverrideSmooth);
                    anim.SetBool("TurningRight", false);
                    anim.SetBool("TurningLeft", false);
                    anim.SetBool("Jump", false);
                    anim.SetBool("Run", false);
                    anim.SetBool("Idle", true);
                }
                else
                {
                    anim.SetBool("Jump", true);
                }
            }
            else
            {
                tempArmsWeight = 0;
                anim.SetBool("TurningRight", false);
                anim.SetBool("TurningLeft", false);
                anim.SetBool("Idle", false);
                anim.SetBool("Jump", false);
                anim.SetFloat("ClimbSpeed", movementInput);
            }

            if (state == 0)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, originalOffset, Time.deltaTime * AdjustSmooth);
            }
            else if (state == 1)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, crouchOffset, Time.deltaTime * AdjustSmooth);
            }
            else
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, proneOffset, Time.deltaTime * AdjustSmooth);
            }

            localBodyAngle.y = 0;
        }

        if (proneDisableBody)
        {
            if (transform.localPosition.y <= (proneOffset.y + 0.1) && transform.localPosition.z <= (proneOffset.z + 0.1))
            {
                foreach (SkinnedMeshRenderer smr in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    smr.enabled = false;
                }
            }
            else
            {
                foreach (SkinnedMeshRenderer smr in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    smr.enabled = true;
                }
            }
        }
        else
        {
            foreach (SkinnedMeshRenderer smr in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.enabled = true;
            }
        }

        if (!controller.isInWater && !controller.isRunning)
        {
            if (controller.characterState == PlayerController.CharacterState.Stand)
            {
                animationSpeed = animWalkSpeed;
            }
            else if (controller.characterState == PlayerController.CharacterState.Crouch)
            {
                animationSpeed = animCrouchSpeed;
            }
        }
        else if (!controller.isInWater && controller.isRunning)
        {
            animationSpeed = animRunSpeed;
        }
        else if (controller.isInWater)
        {
            animationSpeed = animWaterSpeed;
        }

        float movementVelocity = Mathf.Clamp(controller.velMagnitude, 0, animationSpeed);
        anim.SetFloat("AnimationSpeed", velocityBasedAnim ? movementVelocity : animationSpeed);
        anim.SetLayerWeight(anim.GetLayerIndex("Arms Layer"), tempArmsWeight);

        if (health.isDead && !death)
        {
            if (ragdollDeath && CameraRoot && NewDeathCamParent)
            {
                CameraRoot.SetParent(NewDeathCamParent);
                CameraRoot.localPosition = Vector3.zero;
            }

            if(ActivateLimbs.Length > 0)
            {
                foreach (var limb in ActivateLimbs)
                {
                    limb.layer = LimbVisibleLayer;
                }
            }

            if (DeactivateLimbs.Length > 0)
            {
                foreach (var limb in DeactivateLimbs)
                {
                    limb.gameObject.SetActive(false);
                }
            }

            anim.enabled = false;
            Ragdoll(ragdollDeath);
            death = true;
        }

        if (!death)
        {
            if (angledBody)
            {
                if (bodyControl && !ladderReady)
                {
                    if ((spineAngle <= minBodyMaxDeadzone) || (spineAngle >= maxBodyMaxDeadzone))
                    {
                        yBodyAngle = transform.root.eulerAngles.y - (spineAngle > 0 ? spineAngle - 10 : spineAngle + 10);
                        angleSpeed *= deadzonePow;
                        TurnAnimation(spineAngle);
                    }
                    else if ((spineAngle <= minAngleBody) || (spineAngle >= maxAngleBody))
                    {
                        yBodyAngle = transform.root.eulerAngles.y;
                        angleSpeed = rotateBodySpeed;
                        TurnAnimation(spineAngle);
                    }

                    yBodyRotation = Mathf.LerpAngle(yBodyRotation, yBodyAngle, Time.deltaTime * angleSpeed);
                }
                else
                {
                    Vector3 current = transform.localEulerAngles; current.x = 0; current.z = 0;
                    Vector3 angle = bodyAngle + current;
                    angle.y = Mathf.LerpAngle(angle.y, localBodyAngle.y, Time.deltaTime * TurnSmooth);
                    transform.localEulerAngles = angle;

                    yBodyAngle = transform.root.eulerAngles.y;
                    yBodyRotation = yBodyAngle + angle.y;

                    mouseLook.SetClampRange(minBodyMaxDeadzone, maxBodyMaxDeadzone);
                }
            }
            else
            {
                Vector3 current = transform.localEulerAngles; current.x = 0; current.z = 0;
                Vector3 angle = bodyAngle + current;
                angle.y = Mathf.LerpAngle(angle.y, localBodyAngle.y, Time.deltaTime * TurnSmooth);
                transform.localEulerAngles = angle;
            }

            Vector3 relative = transform.InverseTransformPoint(cam.position);
            spineAngle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
            spineAngle = Mathf.Clamp(spineAngle, spineMaxRotation.x, spineMaxRotation.y);
            adjustedSpineEuler = new Vector3(MiddleSpine.localEulerAngles.x, spineAngle, MiddleSpine.localEulerAngles.z);
        }
    }

    void Ragdoll(bool state)
    {
        if (ragdollParts.Length > 0)
        {
            controller.characterController.enabled = !state;

            foreach (var part in ragdollParts)
            {
                if (state)
                {
                    part.rigidbody.isKinematic = false;
                    part.rigidbody.useGravity = true;
                    part.collider.enabled = true;
                }
                else
                {
                    part.rigidbody.isKinematic = true;
                    part.rigidbody.useGravity = false;
                    part.collider.enabled = false;
                }
            }
        }
        else
        {
            Debug.LogError("[Player Body] Cannot activate body ragdoll. Ragdoll Parts was not located!");
        }
    }

    void TurnAnimation(float angle)
    {
        if (angle > 0)
        {
            anim.SetBool("TurningRight", true);
            anim.SetBool("TurningLeft", false);
        }
        else
        {
            anim.SetBool("TurningRight", false);
            anim.SetBool("TurningLeft", true);
        }
    }

    void LateUpdate()
    {
        if (death) return;

        MiddleSpine.localRotation = Quaternion.Euler(adjustedSpineEuler);
        anim.transform.localPosition = Vector3.zero;

        if (bodyControl && angledBody)
        {
            transform.rotation = Quaternion.Euler(new Vector3(transform.eulerAngles.x, yBodyRotation, transform.eulerAngles.z));
        }
    }

    private void OnDrawGizmos()
    {
        if (showBodyGizmos)
        {
            Vector3 angleLeft = Quaternion.AngleAxis(minAngleBody, Vector3.up) * transform.forward;
            Vector3 angleRight = Quaternion.AngleAxis(maxAngleBody, Vector3.up) * transform.forward;

            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, angleLeft * 2);
            Gizmos.DrawRay(transform.position, angleRight * 2);

            Vector3 angleLeftMax = Quaternion.AngleAxis(minBodyMaxDeadzone, Vector3.up) * transform.forward;
            Vector3 angleRightMax = Quaternion.AngleAxis(maxBodyMaxDeadzone, Vector3.up) * transform.forward;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, angleLeftMax * 2);
            Gizmos.DrawRay(transform.position, angleRightMax * 2);

            Vector3 spineAngleGizmo = Quaternion.AngleAxis(spineAngle, Vector3.up) * transform.forward;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, spineAngleGizmo * 1.5f);
        }
    }

    public struct RagdollPart
    {
        public Collider collider;
        public Rigidbody rigidbody;

        public RagdollPart(Collider col, Rigidbody rb)
        {
            collider = col;
            rigidbody = rb;
        }
    }
}