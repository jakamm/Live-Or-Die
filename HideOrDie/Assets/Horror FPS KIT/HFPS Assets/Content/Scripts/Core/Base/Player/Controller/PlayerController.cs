/*
 * PlayerMovement.cs - by ThunderWire Studio
 * ver. 3.0
*/

using System;
using UnityEngine;
using System.Collections;
using ThunderWire.CrossPlatform.Input;

/// <summary>
/// Basic Player Movement Script
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(HealthManager), typeof(FootstepsController))]
public class PlayerController : Singleton<PlayerController>
{
    public enum CharacterState { Stand, Crouch, Prone }
    public enum MovementState { Normal, Ladder }

    #region Public Variables
    private CrossPlatformInput crossPlatformInput;
    private HFPS_GameManager gameManager;
    private ScriptManager scriptManager;
    private ItemSwitcher itemSwitcher;
    private HealthManager healthManager;
    private FootstepsController footsteps;
    private Timekeeper timekeeper = new Timekeeper();

    [ReadOnly] public CharacterState characterState = CharacterState.Stand;
    [ReadOnly] public MovementState movementState = MovementState.Normal;

    [Header("Main")]
    public LayerMask surfaceCheckMask;
    public CharacterController characterController;
    public StabilizeKickback baseKickback;
    public StabilizeKickback weaponKickback;
    public Transform mouseLook;
    public ParticleSystem waterParticles;

    [Header("Movement Basic")]
    public float walkSpeed = 4;
    public float runSpeed = 8;
    public float crouchSpeed = 2;
    public float proneSpeed = 1;
    public float inWaterSpeed = 2;
    [Space(5)]
    public float climbSpeed = 1.5f;
    public float pushSpeed = 2;
    public float jumpHeight = 7;
    public float waterJumpHeight = 5;
    public float stateChangeSpeed = 3f;
    public float runTransitionSpeed = 5f;
    public bool enableSliding = false;
    public bool airControl = false;

    [Header("Controller Settings")]
    public float baseGravity = 24;
    public float inputSmoothing = 3f;
    [Tooltip("Modify the FW/BW -> Left/Right input value.")]
    public float inputModifyFactor = 0.7071f;
    public float slideAngleLimit = 45.0f;
    public float slideSpeed = 8.0f;
    public float fallDamageMultiplier = 5.0f;
    public float standFallTreshold = 8;
    public float crouchFallTreshold = 4;
    public float consoleToProneTime = 0.5f;

    [Header("AutoMove Settings")]
    public float globalAutoMove = 10f;
    public float climbUpAutoMove = 15f;
    public float climbDownAutoMove = 10f;
    public float climbFinishAutoMove = 10f;
    [Space(5)]
    public float globalAutoLook = 3f;
    public float climbUpAutoLook = 3f;
    public float climbDownAutoLook = 3f;

    [Header("Controller Adjustments")]
    public float normalHeight = 2.0f;
    public float crouchHeight = 1.4f;
    public float proneHeight = 0.6f;
    [Space(5)]
    public float camNormalHeight = 0.9f;
    public float camCrouchHeight = 0.2f;
    public float camProneHeight = -0.4f;
    [Space(5)]
    public Vector3 normalCenter = Vector3.zero;
    public Vector3 crouchCenter = new Vector3(0, -0.3f, 0);
    public Vector3 proneCenter = new Vector3(0, -0.7f, 0);

    [Header("Distance Settings")]
    public float groundCheckOffset;
    public float groundCheckRadius;

    [Header("HeadBob Animations")]
    public Animation cameraAnimations;
    public Animation armsAnimations;
    [Space(5)]
    public CameraHeadBob cameraHeadBob = new CameraHeadBob();
    public ArmsHeadBob armsHeadBob = new ArmsHeadBob();

    [Serializable]
    public class CameraHeadBob
    {
        public string cameraIdle = "CameraIdle";
        public string cameraWalk = "CameraWalk";
        public string cameraRun = "CameraRun";
        [Range(0, 5)] public float walkAnimSpeed = 1f;
        [Range(0, 5)] public float runAnimSpeed = 1f;
    }

    [Serializable]
    public class ArmsHeadBob
    {
        public string armsIdle = "ArmsIdle";
        public string armsBreath = "ArmsBreath";
        public string armsWalk = "ArmsWalk";
        public string armsRun = "ArmsRun";
        [Range(0, 5)] public float walkAnimSpeed = 1f;
        [Range(0, 5)] public float runAnimSpeed = 1f;
        [Range(0, 5)] public float breathAnimSpeed = 1f;
    }
    #endregion

    #region Input
    private CrossPlatformControl JumpControl;

    private bool JumpPressed;
    private bool RunPressed;
    private bool CrouchPressed;
    private bool PronePressed;
    private bool ZoomPressed;

    private float inputX;
    private float inputY;
    private Vector2 inputMovement;

    private bool proneTimeStart;
    private float proneTime;
    private bool inProne;
    #endregion

    #region Hidden Variables
    [HideInInspector] public bool ladderReady;
    [HideInInspector] public bool isControllable;
    [HideInInspector] public bool isRunning;
    [HideInInspector] public bool isInWater;
    [HideInInspector] public bool shakeCamera;
    [HideInInspector] public float velMagnitude;
    [HideInInspector] public float movementSpeed;
    #endregion

    #region Private Variables
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 climbDirection = Vector3.up;
    private Vector3 currPosition;
    private Vector3 lastPosition;

    private float antiBumpFactor = .75f;
    private float spamWaitTime = 0.5f;

    private float slideRayDistance;
    private float fallDamageThreshold;
    private float fallDistance;
    private float highestPoint;

    private bool antiSpam;
    private bool isGrounded;
    private bool isSliding;
    private bool isFalling;
    private bool isfoamRemoved;

    private ParticleSystem foamParticles;
    #endregion

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        footsteps = GetComponent<FootstepsController>();
        healthManager = GetComponent<HealthManager>();
        crossPlatformInput = CrossPlatformInput.Instance;
        gameManager = HFPS_GameManager.Instance;
        scriptManager = ScriptManager.Instance;
        itemSwitcher = scriptManager.GetScript<ItemSwitcher>();
    }

    void Start()
    {
        slideRayDistance = characterController.height / 2 + 1.1f;
        slideAngleLimit = characterController.slopeLimit - 0.2f;

        cameraAnimations.wrapMode = WrapMode.Loop;
        armsAnimations.wrapMode = WrapMode.Loop;
        armsAnimations.Stop();

        cameraAnimations[cameraHeadBob.cameraWalk].speed = cameraHeadBob.walkAnimSpeed;
        cameraAnimations[cameraHeadBob.cameraRun].speed = cameraHeadBob.runAnimSpeed;

        armsAnimations[armsHeadBob.armsWalk].speed = armsHeadBob.walkAnimSpeed;
        armsAnimations[armsHeadBob.armsRun].speed = armsHeadBob.runAnimSpeed;
        armsAnimations[armsHeadBob.armsBreath].speed = armsHeadBob.breathAnimSpeed;
    }

    void Update()
    {
        velMagnitude = characterController.velocity.magnitude;

        //Break update when player is dead and ragdoll is activated
        if (healthManager.isDead && cameraAnimations.transform.childCount < 1)
        {
            cameraAnimations.gameObject.SetActive(false);
            return;
        }

        if (crossPlatformInput.inputsLoaded)
        {
            JumpControl = crossPlatformInput.ControlOf("Jump");
            JumpPressed = crossPlatformInput.GetActionPressedOnce(this, "Jump");
            ZoomPressed = crossPlatformInput.GetInput<bool>("Zoom");

            if (crossPlatformInput.deviceType == Device.Gamepad)
            {
                if (crossPlatformInput.GetActionPressedOnce(this, "Run"))
                {
                    RunPressed = !RunPressed;
                }
            }
            else
            {
                RunPressed = crossPlatformInput.GetInput<bool>("Run");
            }

            if (!crossPlatformInput.IsControlsSame("Crouch", "Prone"))
            {
                CrouchPressed = crossPlatformInput.GetActionPressedOnce(this, "Crouch");
                PronePressed = crossPlatformInput.GetActionPressedOnce(this, "Prone");
            }
            else
            {
                bool prone = crossPlatformInput.GetInput<bool>("Prone");

                if (prone && !inProne)
                {
                    proneTimeStart = true;
                    proneTime += Time.deltaTime;

                    if (proneTime >= consoleToProneTime)
                    {
                        PronePressed = true;
                        inProne = true;
                    }
                }
                else if (proneTimeStart && proneTime < consoleToProneTime)
                {
                    CrouchPressed = true;
                    proneTimeStart = false;
                    proneTime = 0;
                }
                else
                {
                    CrouchPressed = false;
                    PronePressed = false;
                    proneTime = 0;

                    if (!prone && inProne)
                    {
                        inProne = false;
                    }
                }
            }

            if (isControllable)
            {
                GetInput();

                if (crossPlatformInput.deviceType == Device.Gamepad && inputY < 0.7f)
                {
                    RunPressed = false;
                }
            }
            else
            {
                RunPressed = false;
                inputX = 0f;
                inputY = 0f;
                inputMovement = Vector2.zero;
            }
        }

        if (movementState == MovementState.Ladder)
        {
            isRunning = false;
            highestPoint = transform.position.y;
            armsAnimations.CrossFade(armsHeadBob.armsIdle);
            cameraAnimations.CrossFade(cameraHeadBob.cameraIdle);

            Vector3 verticalMove;
            verticalMove = climbDirection.normalized;

            if (inputY >= 0.1)
            {
                verticalMove *= 1;
            }
            else if (inputY <= -0.1)
            {
                verticalMove *= -1;
            }
            else
            {
                verticalMove *= 0;
            }

            if (characterController.enabled)
            {
                //Apply ladder movement physics
                characterController.Move(verticalMove * climbSpeed * Time.deltaTime);
            }

            if (JumpPressed)
            {
                LadderExit();
            }
        }
        else
        {
            if (isGrounded)
            {
                //Detect sliding surface
                if (Physics.Raycast(transform.position, -Vector3.up, out RaycastHit hit, slideRayDistance, surfaceCheckMask) && enableSliding)
                {
                    float hitangle = Vector3.Angle(hit.normal, Vector3.up);

                    if (hitangle > slideAngleLimit)
                    {
                        isSliding = true;
                    }
                    else
                    {
                        isSliding = false;
                    }
                }

                //Change player affect type to running when they are not in water
                if (characterState == CharacterState.Stand && !isInWater)
                {
                    isRunning = isControllable && RunPressed && inputY > 0.5f && !ZoomPressed;
                }
                else
                {
                    isRunning = false;
                }

                //Apply fall damage and play footstep land sounds
                if (isFalling)
                {
                    fallDistance = highestPoint - currPosition.y;

                    if (fallDistance > fallDamageThreshold)
                    {
                        ApplyFallingDamage(fallDistance);
                    }

                    if (fallDistance < fallDamageThreshold && fallDistance > 0.1f)
                    {
                        footsteps.OnJump();
                        StartCoroutine(ApplyKickback(new Vector3(7, UnityEngine.Random.Range(-1.0f, 1.0f), 0), 0.15f));
                    }

                    isFalling = false;
                }

                if (isSliding)
                {
                    //Apply sliding physics
                    //If you are looking for a movement lag bug, it is here, just disable sliding
                    Vector3 hitNormal = hit.normal;
                    moveDirection = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
                    Vector3.OrthoNormalize(ref hitNormal, ref moveDirection);
                    moveDirection *= slideSpeed;
                    isSliding = false;
                }
                else
                {
                    //Assign movement speed
                    if (characterState == CharacterState.Stand)
                    {
                        if (!ZoomPressed)
                        {
                            if (!isInWater && !isRunning)
                            {
                                movementSpeed = walkSpeed;
                            }
                            else if (!isInWater && isRunning)
                            {
                                movementSpeed = Mathf.MoveTowards(movementSpeed, runSpeed, Time.deltaTime * runTransitionSpeed);
                            }
                            else if(isInWater)
                            {
                                movementSpeed = inWaterSpeed;
                            }
                        }
                        else
                        {
                            movementSpeed = crouchSpeed;
                        }
                    }
                    else if (characterState == CharacterState.Crouch)
                    {
                        movementSpeed = crouchSpeed;
                    }
                    else if (characterState == CharacterState.Prone)
                    {
                        movementSpeed = proneSpeed;
                    }

                    //Apply normal movement physics
                    moveDirection = new Vector3(inputMovement.x, -antiBumpFactor, inputMovement.y);
                    moveDirection = transform.TransformDirection(moveDirection);
                    moveDirection *= movementSpeed;

                    //Jump player
                    if (isControllable && JumpPressed && movementState != MovementState.Ladder)
                    {
                        if (characterState == CharacterState.Stand)
                        {
                            if (!isInWater)
                            {
                                moveDirection.y = jumpHeight;
                            }
                            else
                            {
                                moveDirection.y = waterJumpHeight;
                            }
                        }
                        else
                        {
                            if (CheckDistance() > 1.6f)
                            {
                                characterState = CharacterState.Stand;
                                StartCoroutine(AntiSpam());
                            }
                        }
                    }
                }

                //Play camera head bob animations
                if (!shakeCamera)
                {
                    if (!isRunning && velMagnitude > crouchSpeed)
                    {
                        armsAnimations.CrossFade(armsHeadBob.armsWalk);
                        cameraAnimations.CrossFade(cameraHeadBob.cameraWalk);
                    }
                    else if (isRunning && velMagnitude > walkSpeed)
                    {
                        armsAnimations.CrossFade(armsHeadBob.armsRun);
                        cameraAnimations.CrossFade(cameraHeadBob.cameraRun);
                    }
                    else if (velMagnitude < crouchSpeed)
                    {
                        armsAnimations.CrossFade(armsHeadBob.armsBreath);
                        cameraAnimations.CrossFade(cameraHeadBob.cameraIdle);
                    }
                }
            }
            else
            {
                currPosition = transform.position;

                if (!isFalling)
                {
                    highestPoint = transform.position.y;
                }

                if (currPosition.y > lastPosition.y)
                {
                    highestPoint = transform.position.y;
                }

                if (airControl)
                {
                    moveDirection.x = inputX * movementSpeed;
                    moveDirection.z = inputY * movementSpeed;
                    moveDirection = transform.TransformDirection(moveDirection);
                }

                if (!shakeCamera)
                {
                    armsAnimations.CrossFade(armsHeadBob.armsIdle);
                    cameraAnimations.CrossFade(cameraHeadBob.cameraIdle);
                }

                isFalling = true;
            }

            if (!isInWater && isControllable && !antiSpam)
            {
                //Crouch Player
                if (CrouchPressed)
                {
                    if (characterState != CharacterState.Crouch)
                    {
                        if (CheckDistance() > 1.6f)
                        {
                            characterState = CharacterState.Crouch;
                        }
                    }
                    else if (characterState != CharacterState.Stand)
                    {
                        if (CheckDistance() > 1.6f)
                        {
                            characterState = CharacterState.Stand;
                        }
                    }

                    StartCoroutine(AntiSpam());
                }

                //Prone Player
                if (PronePressed)
                {
                    if (characterState != CharacterState.Prone)
                    {
                        characterState = CharacterState.Prone;
                    }
                    else if (characterState == CharacterState.Prone)
                    {
                        if (CheckDistance() > 1.6f)
                        {
                            characterState = CharacterState.Stand;
                        }
                    }

                    StartCoroutine(AntiSpam());
                }
            }
        }

        //Play foam particles when player is in water
        if (foamParticles && !isfoamRemoved)
        {
            if (isInWater && !ladderReady)
            {
                if (velMagnitude > 0.01f)
                {
                    if (foamParticles.isStopped) foamParticles.Play(true);
                }
                else
                {
                    if (foamParticles.isPlaying) foamParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
            else
            {
                foamParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                StartCoroutine(RemoveFoam());
                isfoamRemoved = true;
            }
        }

        if (characterState == CharacterState.Stand)
        {
            //Stand Position
            characterController.height = normalHeight;
            characterController.center = normalCenter;
            fallDamageThreshold = standFallTreshold;

            if (mouseLook.localPosition.y < camNormalHeight)
            {
                float smooth = Mathf.Lerp(mouseLook.localPosition.y, camNormalHeight, Time.deltaTime * stateChangeSpeed);
                mouseLook.localPosition = new Vector3(mouseLook.localPosition.x, smooth, mouseLook.localPosition.z);
            }
            else
            {
                mouseLook.localPosition = new Vector3(mouseLook.localPosition.x, camNormalHeight, mouseLook.localPosition.z);
            }
        }
        else if (characterState == CharacterState.Crouch)
        {
            //Crouch Position
            characterController.height = crouchHeight;
            characterController.center = crouchCenter;
            fallDamageThreshold = crouchFallTreshold;

            if (mouseLook.localPosition.y < camCrouchHeight || mouseLook.localPosition.y > camCrouchHeight)
            {
                float smooth = Mathf.Lerp(mouseLook.localPosition.y, camCrouchHeight, Time.deltaTime * stateChangeSpeed);
                mouseLook.localPosition = new Vector3(mouseLook.localPosition.x, smooth, mouseLook.localPosition.z);
            }
            else
            {
                mouseLook.localPosition = new Vector3(mouseLook.localPosition.x, camCrouchHeight, mouseLook.localPosition.z);
            }
        }
        else if (characterState == CharacterState.Prone)
        {
            //Prone Position
            characterController.height = proneHeight;
            characterController.center = proneCenter;
            fallDamageThreshold = crouchFallTreshold;

            if (mouseLook.localPosition.y > camProneHeight)
            {
                float smooth = Mathf.Lerp(mouseLook.localPosition.y, camProneHeight, Time.deltaTime * stateChangeSpeed);
                mouseLook.localPosition = new Vector3(mouseLook.localPosition.x, smooth, mouseLook.localPosition.z);
            }
            else
            {
                mouseLook.localPosition = new Vector3(mouseLook.localPosition.x, camProneHeight, mouseLook.localPosition.z);
            }
        }

        if (movementState != MovementState.Ladder && characterController.enabled)
        {
            //Apply movement physics and gravity
            moveDirection.y -= baseGravity * Time.deltaTime;
            isGrounded = (characterController.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;
        }
    }

    void LateUpdate()
    {
        lastPosition = currPosition;
    }

    void GetInput()
    {
        Vector2 movement;

        if ((movement = crossPlatformInput.GetInput<Vector2>("Movement")) != null)
        {
            if (crossPlatformInput.deviceType == Device.Gamepad)
            {
                inputX = movement.x;
                inputY = movement.y;
                inputMovement = movement;
            }
            else
            {
                inputY = Mathf.MoveTowards(inputY, movement.y, Time.deltaTime * inputSmoothing);
                inputX = Mathf.MoveTowards(inputX, movement.x, Time.deltaTime * inputSmoothing);

                float inputModifer = Mathf.Abs(inputX) > 0 && Mathf.Abs(inputY) > 0 ? inputModifyFactor : 1;
                inputMovement.y = inputY * inputModifer;
                inputMovement.x = inputX * inputModifer;
            }
        }
    }

    float CheckDistance()
    {
        Vector3 pos = transform.position + characterController.center - new Vector3(0, characterController.height / 2, 0);

        if (Physics.SphereCast(pos, characterController.radius, transform.up, out RaycastHit hit, 10, surfaceCheckMask))
        {
            Debug.DrawLine(pos, hit.point, Color.yellow, 2.0f);
            return hit.distance;
        }
        else
        {
            Debug.DrawLine(pos, hit.point, Color.yellow, 2.0f);
            return 3;
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
            
        //dont move the rigidbody if the character is on top of it
        if (characterController.collisionFlags == CollisionFlags.Below)
        {
            return;
        }

        if (body == null || body.isKinematic)
        {
            return;
        }

        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        body.velocity = pushDir * pushSpeed;
    }

    void ApplyFallingDamage(float fallDistance)
    {
        healthManager.ApplyDamage(fallDistance * fallDamageMultiplier);
        if (characterState != CharacterState.Prone) footsteps.OnJump();
        StartCoroutine(ApplyKickback(new Vector3(12, UnityEngine.Random.Range(-2.0f, 2.0f), 0), 0.1f));
    }

    public void SetPlayerState(CharacterState state)
    {
        if (state == CharacterState.Crouch)
        {
            //Crouch Position
            characterController.height = crouchHeight;
            characterController.center = crouchCenter;
            fallDamageThreshold = crouchFallTreshold;
            mouseLook.localPosition = new Vector3(mouseLook.localPosition.x, camCrouchHeight, mouseLook.localPosition.z);
        }
        else if (state == CharacterState.Prone)
        {
            //Prone Position
            characterController.height = proneHeight;
            characterController.center = proneCenter;
            fallDamageThreshold = crouchFallTreshold;
            mouseLook.localPosition = new Vector3(mouseLook.localPosition.x, camProneHeight, mouseLook.localPosition.z);
        }

        characterState = state;
    }

    /// <summary>
    /// Check if player is on the ground
    /// </summary>
    public bool IsGrounded()
    {
        Vector3 pos = transform.position + characterController.center - new Vector3(0, (characterController.height / 2f) + groundCheckOffset, 0);

        if (Physics.OverlapSphere(pos, groundCheckRadius, surfaceCheckMask).Length > 0 || isGrounded)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get Movement Input Values
    /// </summary>
    public Vector2 GetMovementValue()
    {
        return new Vector2(inputX, inputY);
    }

    /// <summary>
    /// Spawn player water foam particles
    /// </summary>
    public void PlayerInWater(float top)
    {
        Vector3 foamPos = transform.position;
        foamPos.y = top;

        if (foamParticles == null)
        {
            foamParticles = Instantiate(waterParticles, foamPos, transform.rotation) as ParticleSystem;
        }

        if (foamParticles)
        {
            foamParticles.transform.position = foamPos;
        }
    }

    /// <summary>
    /// Use ladder function
    /// </summary>
    public void UseLadder(Transform center, Vector2 look, bool climbUp)
    {
        ladderReady = false;
        characterState = CharacterState.Stand;

        moveDirection = Vector3.zero;
        inputX = 0f;
        inputY = 0f;

        if (climbUp)
        {
            Vector3 destination = center.position;
            destination.y = transform.position.y;
            StartCoroutine(MovePlayer(destination, climbUpAutoMove, true));
            scriptManager.GetComponent<MouseLook>().LerpLook(look, climbUpAutoLook, true);
        }
        else
        {
            StartCoroutine(MovePlayer(center.position, climbDownAutoMove, true));
            scriptManager.GetComponent<MouseLook>().LerpLook(look, climbDownAutoLook, true);
        }

        gameManager.ShowHelpButtons(new HelpButton("Exit Ladder", JumpControl), null, null, null);
        itemSwitcher.FreeHands(true);
        movementState = MovementState.Ladder;
    }

    /// <summary>
    /// Exit ladder movement
    /// </summary>
    public void LadderExit()
    {
        if (ladderReady)
        {
            movementState = MovementState.Normal;
            ladderReady = false;
            scriptManager.GetComponent<MouseLook>().LockLook(false);
            gameManager.HideSprites(HideHelpType.Help);
            itemSwitcher.FreeHands(false);
        }
    }

    /// <summary>
    /// Lerp player from ladder to position
    /// </summary>
    public void LerpPlayerLadder(Vector3 destination)
    {
        if (ladderReady)
        {
            ladderReady = false;
            scriptManager.GetComponent<MouseLook>().LockLook(false);
            gameManager.HideSprites(HideHelpType.Help);
            itemSwitcher.FreeHands(false);
            StartCoroutine(MovePlayer(destination, climbFinishAutoMove, false));
        }
    }

    /// <summary>
    /// Lerp player to position
    /// </summary>
    public void LerpPlayer(Vector3 destination, Vector2 look, bool lerpLook = true)
    {
        characterState = CharacterState.Stand;
        moveDirection = Vector3.zero;
        ladderReady = false;
        isControllable = false;
        inputX = 0f;
        inputY = 0f;

        StartCoroutine(MovePlayer(destination, globalAutoMove, false, true));

        if (lerpLook)
        {
            scriptManager.GetComponent<MouseLook>().LerpLook(look, globalAutoLook, true);
        }
    }

    IEnumerator MovePlayer(Vector3 pos, float speed, bool ladder, bool unlockLook = false)
    {
        characterController.enabled = false;

        while (Vector3.Distance(transform.position, pos) > 0.05f)
        {
            transform.position = Vector3.Lerp(transform.position, pos, timekeeper.deltaTime * speed);
            yield return null;
        }

        characterController.enabled = true;
        isControllable = true;
        ladderReady = ladder;
        movementState = ladder ? MovementState.Ladder : MovementState.Normal;

        if (unlockLook)
        {
            scriptManager.GetComponent<MouseLook>().LockLook(false);
        }
    }

    IEnumerator RemoveFoam()
    {
        yield return new WaitForSeconds(2);
        Destroy(foamParticles.gameObject);
        isfoamRemoved = false;
    }

    public IEnumerator ApplyKickback(Vector3 offset, float time)
    {
        Quaternion s = baseKickback.transform.localRotation;
        Quaternion sw = weaponKickback.transform.localRotation;
        Quaternion e = baseKickback.transform.localRotation * Quaternion.Euler(offset);
        float r = 1.0f / time;
        float t = 0.0f;

        while (t < 1.0f)
        {
            t += Time.deltaTime * r;
            baseKickback.transform.localRotation = Quaternion.Slerp(s, e, t);
            weaponKickback.transform.localRotation = Quaternion.Slerp(sw, e, t);

            yield return null;
        }
    }

    IEnumerator AntiSpam()
    {
        antiSpam = true;
        yield return new WaitForSeconds(spamWaitTime);
        antiSpam = false;
    }

    void OnDrawGizmos()
    {
        if (!characterController) return;

        Vector3 pos = transform.position + characterController.center - new Vector3(0, (characterController.height / 2f) + groundCheckOffset, 0);
        Gizmos.DrawWireSphere(pos, groundCheckRadius);
    }
}
