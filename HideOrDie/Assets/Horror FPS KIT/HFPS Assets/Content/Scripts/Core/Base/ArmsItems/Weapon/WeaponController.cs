/*
 * WeaponController.cs - by ThunderWire Studio
 * Version 2.0
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using ThunderWire.CrossPlatform.Input;

/// <summary>
/// Weapon Shooting Script
/// </summary>
/// 
public class WeaponController : SwitcherBehaviour, ISaveableArmsItem, IOnAnimatorState
{
    public enum WeaponType { Semi, Auto, Shotgun }
    public enum ReloadSound { None, Script, Animation }

    private ScriptManager scriptManager;
    private CrossPlatformInput crossPlatformInput;
    private HFPS_GameManager gameManager;
    private PlayerFunctions playerFunctions;
    private Inventory inventory;
    private Transform Player;

    private Animator weaponAnim;
    private Transform weaponRoot;

    public WeaponType weaponType = WeaponType.Semi;

    [Header("Inventory")]
    public int weaponID;
    public int bulletsID;

    [Header("Weapon Configuration")]
    public LayerMask layerMask;
    public int weaponDamage = 20;
    public float shootRange = 250.0f;
    public float hitforce = 20.0f;
    public float fireRate = 0.1f;
    public float recoil = 0.1f;

    [Header("NPC Sound Reaction")]
    public LayerMask soundReactionMask;
    public float soundReactionRadius = 20f;
    public bool enableSoundReaction;

    [Header("Aiming Configuration")]
    public Vector3 aimPosition;
    public float aimSpeed = 0.25f;
    public float zoomFOVSmooth = 10f;
    public float unzoomFOVSmooth = 5f;
    public int FOV = 40;

    [Header("Bulletmark Configuration")]
    public SurfaceID surfaceID = SurfaceID.Texture;
    public SurfaceDetailsScriptable surfaceDetails;
    public string FleshTag = "Flesh";
    public int defaultSurfaceID;
    [Space(5)]
    [ReadOnly] public int carryingBullets = 0;
    public int bulletsInMag = 0;
    public int bulletsPerMag = 0;
    public int bulletsPerShot = 1;
    public bool keepReloadMagBullets;

    [Header("Kickback")]
    public Transform kickGO;
    public float kickUp = 0.5f;
    public float kickSideways = 0.5f;

    [Header("Muzzle-Flash")]
    public Vector3 muzzleRotation;
    public Renderer muzzleFlash;
    public Light muzzleLight;

    [Header("Animations")]
    public string hideAnim = "Hide";
    public string fireAnim = "Fire";
    public string reloadAnim = "Reload";

    [Header("Audio")]
    public AudioSource aSource;
    public ReloadSound reloadSound = ReloadSound.Script;
    [Space(5)]
    public AudioClip soundDraw;
    [Range(0, 2)] public float volumeDraw = 1f;
    public AudioClip soundFire;
    [Range(0, 2)] public float volumeFire = 1f;
    public AudioClip soundEmpty;
    [Range(0, 2)] public float volumeEmpty = 1f;
    public AudioClip soundReload;
    [Range(0, 2)] public float volumeReload = 1f;

    #region Private Variables
    private RaycastHit hit;
    private Camera mainCamera;

    private bool fireControl;
    private bool reloadControl;
    private bool zoomControl;

    private float fireTime = 0;
    private float muzzleTime = 0;
    private float conflictTime = 0;

    private Vector3 hipPosition;
    private Vector3 distVector;

    private bool isHideAnim;
    private bool isSelected;
    private bool isBlocked;
    private bool isReloading;
    private bool isAiming;
    private bool canFire;
    private bool fireOnce;

    private bool muzzleShown;
    private bool wallHit;
    private bool uiShown;

    private string stateName = string.Empty;
    private float stateTime;

    private bool inputWait;
    private float waitTime;
    #endregion

    void Awake()
    {
        weaponRoot = transform.GetChild(0);
        weaponAnim = weaponRoot.GetComponent<Animator>();
        scriptManager = ScriptManager.Instance;
        crossPlatformInput = CrossPlatformInput.Instance;
        gameManager = HFPS_GameManager.Instance;
        inventory = Inventory.Instance;
        Player = PlayerController.Instance.transform;
        playerFunctions = scriptManager.GetScript<PlayerFunctions>();
        mainCamera = scriptManager.MainCamera;

        hipPosition = weaponAnim.transform.localPosition;
        distVector = hipPosition;

        fireTime = fireRate;

        muzzleFlash.enabled = false;
        muzzleLight.enabled = false;

        if (weaponType != WeaponType.Shotgun)
        {
            bulletsPerShot = 1;
        }
    }

    void Update()
    {
        if (!scriptManager.ScriptGlobalState)
        {
            waitTime = 0.5f;
            inputWait = true;
        }
        else if (inputWait && waitTime > 0)
        {
            waitTime -= Time.deltaTime;
        }
        else
        {
            inputWait = false;
        }

        if (crossPlatformInput.inputsLoaded && !inputWait)
        {
            fireControl = crossPlatformInput.GetInput<bool>("Fire");
            zoomControl = crossPlatformInput.GetInput<bool>("Zoom");

            if(crossPlatformInput.IsControlsSame("Reload", "Examine"))
            {
                if(!scriptManager.IsExamineRaycast && !scriptManager.IsGrabRaycast)
                {
                    if (conflictTime <= 0)
                    {
                        reloadControl = crossPlatformInput.GetInput<bool>("Reload");
                    }
                    else
                    {
                        conflictTime -= Time.deltaTime;
                    }
                }
                else
                {
                    conflictTime = 0.3f;
                }
            }
            else
            {
                reloadControl = crossPlatformInput.GetInput<bool>("Reload");
            }
        }

        if (inventory)
        {
            if (inventory.CheckItemInventory(weaponID) && weaponID != -1)
            {
                inventory.SetItemAmount(weaponID, bulletsInMag);
            }

            if (inventory.CheckItemInventory(bulletsID) && bulletsID != -1)
            {
                carryingBullets = inventory.GetItemAmount(bulletsID);
            }
            else
            {
                carryingBullets = 0;
            }
        }

        if (gameManager && uiShown)
        {
            gameManager.BulletsText.text = bulletsInMag.ToString();
            gameManager.MagazinesText.text = carryingBullets.ToString();

            if (inventory.isInventoryShown)
            {
                gameManager.AmmoUI.SetActive(false);
            }
            else
            {
                gameManager.AmmoUI.SetActive(true);
            }
        }

        if (!weaponRoot.gameObject.activeSelf) return;
        if (!scriptManager.ScriptEnabledGlobal) return;
        if (wallHit || isBlocked || !isSelected || isHideAnim) return;

        if (Cursor.lockState != CursorLockMode.None)
        {
            if (fireControl && !isReloading)
            {
                if (weaponType == WeaponType.Auto)
                {
                    if (canFire)
                    {
                        FireOneBullet();
                        fireTime = fireRate;
                    }
                }
                else if (!fireOnce && canFire)
                {
                    FireOneBullet();
                    fireTime = fireRate;
                    fireOnce = true;
                }
            }
            else if (fireOnce)
            {
                fireOnce = false;
            }

            if(fireTime > 0)
            {
                fireTime -= Time.deltaTime;
                canFire = false;
            }
            else
            {
                fireTime = 0;
                canFire = true;
            }

            if(reloadControl && !isReloading && carryingBullets > 0 && bulletsInMag < bulletsPerMag)
            {
                StartCoroutine(Reload());

                if(reloadSound == ReloadSound.Script)
                {
                    if(soundReload) PlaySound(soundReload, volumeReload);
                }

                isReloading = true;
            }

            if(zoomControl && !isReloading && playerFunctions.zoomEnabled)
            {
                if (!isAiming)
                {
                    distVector = aimPosition;
                    isAiming = true;
                }
                else
                {
                    gameManager.Crosshair.enabled = false;
                    gameManager.isWeaponZooming = true;
                }
            }
            else
            {
                if (isAiming)
                {
                    distVector = hipPosition;
                    isAiming = false;
                }
                else
                {
                    gameManager.Crosshair.enabled = true;
                    gameManager.isWeaponZooming = false;
                }
            }

            if (weaponAnim.transform.localPosition != distVector)
            {
                Vector3 lerp = Vector3.LerpUnclamped(weaponRoot.localPosition, distVector, aimSpeed * Time.deltaTime);
                weaponRoot.localPosition = lerp;
            }

            if (muzzleFlash)
            {
                if (muzzleShown)
                {
                    Vector3 muzzleRot = muzzleFlash.transform.localEulerAngles;
                    muzzleRot += muzzleRotation.normalized * (Random.value * 360);
                    muzzleFlash.transform.localEulerAngles = muzzleRot;

                    muzzleTime = 0f;
                    muzzleFlash.enabled = true;
                    muzzleLight.enabled = true;
                    muzzleShown = false;
                }
                else
                {
                    if (muzzleTime <= 0.01f)
                    {
                        muzzleTime += Time.deltaTime;
                    }
                    else
                    {
                        muzzleFlash.enabled = false;
                        muzzleLight.enabled = false;
                        muzzleShown = false;
                    }
                }
            }
        }
    }

    void FireOneBullet()
    {
        if (bulletsInMag <= 0)
        {
            if (soundEmpty) PlaySound(soundEmpty, volumeEmpty);
            bulletsInMag = 0;
            return;
        }

        weaponAnim.SetTrigger(fireAnim);

        for (int i = 0; i < bulletsPerShot; i++)
        {
            float width = Random.Range(-1f, 1f) * recoil;
            float height = Random.Range(-1f, 1f) * recoil;

            Vector3 spray = mainCamera.transform.forward + mainCamera.transform.right * width + mainCamera.transform.up * height;
            Ray aim = new Ray(mainCamera.transform.position, spray.normalized);

            if (Physics.Raycast(aim, out hit, shootRange, layerMask))
            {
                if (hit.rigidbody)
                {
                    hit.rigidbody.AddForceAtPosition(aim.direction * hitforce, hit.point);
                }

                ShowBulletMark(hit);
            }
        }

        muzzleShown = true;
        bulletsInMag--;
        if(soundFire) PlaySound(soundFire, volumeFire, true);
        kickGO.localRotation = Quaternion.Euler(kickGO.localRotation.eulerAngles - new Vector3(kickUp, Random.Range(-kickSideways, kickSideways), 0));
    }

    void ShowBulletMark(RaycastHit hit)
    {
        Quaternion hitRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        Vector3 hitPosition = hit.point;

        GameObject hitObject = hit.collider.gameObject;
        SurfaceDetails surface;
        Terrain terrain;

        if ((terrain = hitObject.GetComponent<Terrain>()) != null)
        {
            surface = surfaceDetails.GetTerrainSurfaceDetails(terrain, hit.point);
        }
        else
        {
            surface = surfaceDetails.GetSurfaceDetails(hitObject, surfaceID);
        }

        if(surface == null || hitObject.CompareTag("Water") && (surface = surfaceDetails.GetSurfaceDetails("Water")) == null)
        {
            surface = surfaceDetails.surfaceDetails[defaultSurfaceID];
        }

        if(surface.SurfaceBulletmark && surface.allowImpactMark)
        {
            float rScale = Random.Range(0.5f, 1.0f);

            if (surface.SurfaceTag != FleshTag)
            {
                GameObject bulletMark = Instantiate(surface.SurfaceBulletmark, hitPosition, hitRotation);
                bulletMark.transform.localPosition += .02f * hit.normal;
                bulletMark.transform.localScale = new Vector3(rScale, 1, rScale);
                bulletMark.transform.parent = hit.transform;
            }
            else
            {
                Instantiate(surface.SurfaceBulletmark, hitPosition, hitRotation);
            }

            hit.collider.SendMessage("ApplyDamage", weaponDamage, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void OnStateEnter(AnimatorStateInfo state, string name)
    {
        stateName = name;
        stateTime = state.length;
    }

    IEnumerator Reload()
    {
        int bulletsToFullMag = keepReloadMagBullets ? bulletsPerMag - bulletsInMag : bulletsPerMag;

        if (carryingBullets > 0 && bulletsInMag != bulletsPerMag)
        {
            weaponAnim.SetTrigger(reloadAnim);

            yield return new WaitUntil(() => stateName.Equals("Reload"));
            yield return new WaitForSeconds(stateTime);

            inventory.RemoveItemAmount(bulletsID, bulletsToFullMag);

            if (carryingBullets >= bulletsToFullMag)
            {
                bulletsInMag = bulletsPerMag;
            }
            else
            {
                bulletsInMag += carryingBullets;
            }
        }

        isReloading = false;
        canFire = false;

        stateName = string.Empty;
        stateTime = 0;
    }

    void PlaySound(AudioClip clip, float volume, bool reaction = false)
    {
        if (aSource)
        {
            aSource.clip = clip;
            aSource.volume = volume;
            aSource.Play();
        }

        if (reaction && enableSoundReaction)
        {
            Collider[] colliderHit = Physics.OverlapSphere(Player.position, soundReactionRadius, soundReactionMask);

            foreach (var hit in colliderHit)
            {
                INPCReaction m_reaction;

                if ((m_reaction = hit.GetComponentInChildren<INPCReaction>()) != null)
                {
                    m_reaction.SoundReaction(Player.position, false);
                }
            }
        }
    }

    void SetZoomFOV(bool useDefault)
    {
        if (!useDefault)
        {
            playerFunctions.ZoomFOV = FOV;
            playerFunctions.ZoomSmooth = zoomFOVSmooth;
            playerFunctions.UnzoomSmooth = unzoomFOVSmooth;
        }
        else
        {
            playerFunctions.ResetDefaults();
            playerFunctions.wallHit = false;
            gameManager.Crosshair.enabled = true;
            gameManager.isWeaponZooming = false;
        }
    }

    public override void OnSwitcherSelect()
    {
        if (isSelected || stateName.Equals("Draw")) return;

        StartCoroutine(SelectEvent());
    }

    public override void OnSwitcherDeselect()
    {
        if (!isSelected || isReloading || stateName.Equals("Hide")) return;

        isHideAnim = true;

        StartCoroutine(DeselectEvent());
        SetZoomFOV(true);
    }

    public override void OnSwitcherActivate()
    {
        weaponRoot.gameObject.SetActive(true);
        SetZoomFOV(false);

        if (gameManager)
        {
            gameManager.AmmoUI.SetActive(true);
            uiShown = true;
        }

        isReloading = false;
        isSelected = true;

        stateName = string.Empty;
        stateTime = 0;
    }

    public override void OnSwitcherDeactivate()
    {
        isSelected = false;
        SetZoomFOV(true);
        weaponRoot.localPosition = hipPosition;
        weaponRoot.gameObject.SetActive(false);

        if (gameManager)
        {
            gameManager.AmmoUI.SetActive(false);
            uiShown = false;
        }
    }

    public override void OnSwitcherWallHit(bool hit)
    {
        wallHit = hit;
        playerFunctions.wallHit = hit;
    }

    public override void OnSwitcherDisable(bool enabled)
    {
        isBlocked = enabled;
    }

    IEnumerator SelectEvent()
    {
        weaponRoot.gameObject.SetActive(true);

        yield return new WaitUntil(() => stateName.Equals("Draw"));

        SetZoomFOV(false);

        if (gameManager)
        {
            gameManager.AmmoUI.SetActive(true);
            uiShown = true;
        }

        if (soundDraw)
        {
            PlaySound(soundDraw, volumeDraw);
        }

        yield return new WaitForSeconds(stateTime);

        isReloading = false;
        isSelected = true;

        stateName = string.Empty;
        stateTime = 0;
    }

    IEnumerator DeselectEvent()
    {
        if (gameManager)
        {
            gameManager.AmmoUI.SetActive(false);
            uiShown = false;
        }

        weaponRoot.localPosition = hipPosition;

        weaponAnim.SetTrigger(hideAnim);

        yield return new WaitUntil(() => stateName.Equals("Hide"));
        yield return new WaitForSeconds(stateTime);

        isHideAnim = false;
        isSelected = false;
        stateName = string.Empty;
        stateTime = 0;

        weaponRoot.gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        if (enableSoundReaction)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, soundReactionRadius);
        }
    }

    public Dictionary<string, object> OnSave()
    {
        return new Dictionary<string, object>
        {
            {"bulletsInMag", bulletsInMag},
            {"uiShown", uiShown}
        };
    }

    public void OnLoad(JToken token)
    {
        bulletsInMag = (int)token["bulletsInMag"];

        if ((bool)token["uiShown"])
        {
            if (gameManager)
            {
                gameManager.AmmoUI.SetActive(true);
                uiShown = true;
            }
        }
    }
}
