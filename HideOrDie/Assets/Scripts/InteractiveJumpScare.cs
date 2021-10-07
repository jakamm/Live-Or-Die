/*
 * InteractiveItem.cs - by ThunderWire Studio
 * ver. 1.0
*/

using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ThunderWire.Utility;
using ThunderWire.Camera.Shaker;

/// <summary>
/// Script for defining Interactive Items
/// </summary>
/// 
public class InteractiveJumpScare : MonoBehaviour
//public class InteractiveItem : MonoBehaviour, ISaveable
{

    #region Structures
    [System.Serializable]
    public class MessageTip
    {
        public string InputString;
        public string KeyMessage;
    }
    #endregion

    public enum Type { OnlyExamine, GenericItem, InventoryItem, ArmsItem, BackpackExpand, InteractObject }
    public enum ExamineType { None, Object, AdvancedObject, Paper }
    public enum ExamineRotate { None, Horizontal, Vertical, Both }
    public enum MessageType { None, PickupHint, Message, ItemName }
    public enum DisableType { DisableRenderer, DisableObject, Destroy, None }

    private AudioSource audioSource;

    public Type ItemType = Type.GenericItem;
    public ExamineType examineType = ExamineType.None;
    public ExamineRotate examineRotate = ExamineRotate.Both;
    public MessageType messageType = MessageType.None;
    public DisableType disableType = DisableType.DisableRenderer;

    public MessageTip[] MessageTips;

    //Inventory
    public int weaponID;
    public int inventoryID;
    public int backpackExpandAmount;

    //Texts
    public string examineName;
    public string itemMessage;
    [Multiline]
    public string paperMessage;

    //Others
    public int pickupAmount = 1;
    public float messageShowTime = 3f;
    public float examineDistance = 0.5f;
    public int paperMessageSize = 15;

    public bool pickupSwitch;
    public bool examineCollect;
    public bool enableCursor;
    public bool showItemName;
    public bool autoShortcut;
    public bool floatingIconEnabled = true;
    public bool faceToCamera = false;

    public Vector3 faceRotation;
    public List<ItemHashtable> itemHashtables = new List<ItemHashtable>();

    [Tooltip("Colliders which will be disabled when object will be examined.")]
    public Collider[] CollidersDisable;
    [Tooltip("Colliders which will be enabled when object will be examined.")]
    public Collider[] CollidersEnable;

    //Sounds
    public AudioClip pickupSound;
    public float pickupVolume = 1f;
    public AudioClip examineSound;
    public float examineVolume = 1f;

    //Public Hidden
    public bool isExamined;
    public Vector3 lastFloorPosition;
    public CustomItemData customData;

    private string objectParentPath;

    private JumpscareEffects effects;

    [Header("Jumpscare Setup")]
    public Animation AnimationObject;
    public AudioClip JumpscareSound;
    public AudioClip ScaredBreath;
    [Range(0, 5)] public float scareVolume = 0.5f;
    [Tooltip("Value sets how long will be player scared.")]
    public float scaredBreathTime = 33f;
    public bool enableEffects = true;

    [Header("Scare Effects")]
    public float chromaticAberrationAmount = 0.8f;
    public float vignetteAmount = 0.3f;
    public float effectsTime = 5f;

    [Header("Scare Shake")]
    public bool shakeByPreset = false;
    public float magnitude = 3f;
    public float roughness = 3f;
    public float startTime = 0.1f;
    public float durationTime = 3f;

    [Header("Scare Position Influence")]
    public Vector3 PositionInfluence = new Vector3(0.15f, 0.15f, 0f);
    public Vector3 RotationInfluence = Vector3.one;

    [SaveableField, HideInInspector]
    public bool isPlayed;

    void Awake()
    {
        CreateCustomData(itemHashtables);
    }

    void Start()
    {
        audioSource = ScriptManager.Instance.SoundEffects;
        effects = ScriptManager.Instance.gameObject.GetComponent<JumpscareEffects>();
    }

    public void IsPlayed(bool state)
    {
        isPlayed = state;
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.isTrigger && !collision.collider.CompareTag("Player"))
        {
            lastFloorPosition = transform.position;
        }
    }

    public void CreateCustomData(List<ItemHashtable> hashtables)
    {
        Dictionary<string, string> data = new Dictionary<string, string>();

        if (hashtables.Count > 0)
        {
            foreach (var item in hashtables)
            {
                data.Add(item.Key, item.Value);
            }
        }

        if (ItemType == Type.InventoryItem || ItemType == Type.ArmsItem)
        {
            objectParentPath = gameObject.GameObjectPath();
            data.Add("object_path", objectParentPath);
            data.Add("object_scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        customData = new CustomItemData(data);
    }

    void FixedUpdate()
    {
        if (ItemType == Type.InventoryItem || ItemType == Type.ArmsItem)
        {
            if (objectParentPath != gameObject.GameObjectPath())
            {
                objectParentPath = gameObject.GameObjectPath();
                customData.dataDictionary["object_path"] = objectParentPath;
            }
        }
    }

    public void UseObject()
    {
        if (ItemType == Type.OnlyExamine) return;

        if (pickupSound)
        {
            audioSource.clip = pickupSound;
            audioSource.volume = pickupVolume;
            audioSource.Play();
        }

        AnimationObject.Play();

        if (JumpscareSound)
        {
            Tools.PlayOneShot2D(transform.position, JumpscareSound, scareVolume);
        }

        if (enableEffects)
        {
            if (shakeByPreset)
            {
                CameraShakeInstance shakeInstance = CameraShakePresets.Scare;
                effects.Scare(shakeInstance, chromaticAberrationAmount, vignetteAmount, scaredBreathTime, effectsTime, ScaredBreath);
            }
            else
            {
                CameraShakeInstance shakeInstance = new CameraShakeInstance(magnitude, roughness, startTime, durationTime);
                shakeInstance.PositionInfluence = PositionInfluence;
                shakeInstance.RotationInfluence = RotationInfluence;
                effects.Scare(shakeInstance, chromaticAberrationAmount, vignetteAmount, scaredBreathTime, effectsTime, ScaredBreath);
            }
        }

        isPlayed = true;
    

        if (GetComponent<ItemEvent>())
        {
            GetComponent<ItemEvent>().DoEvent();
        }

        if (GetComponent<TriggerObjective>())
        {
            GetComponent<TriggerObjective>().OnTrigger();
        }

        SaveGameHandler.Instance.RemoveSaveableObject(gameObject, false, false);

        if (disableType == DisableType.DisableRenderer)
        {
            DisableObject(false);
        }
        else if (disableType == DisableType.DisableObject)
        {
            gameObject.SetActive(false);
        }
        else if (disableType == DisableType.Destroy)
        {
            FloatingIconManager.Instance.DestroySafely(gameObject);
        }
    }

    public void DisableObject(bool state)
    {
        if (state == false)
        {
            if (GetComponent<Rigidbody>())
            {
                GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                GetComponent<Rigidbody>().useGravity = false;
                GetComponent<Rigidbody>().isKinematic = true;
            }

            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<Collider>().enabled = false;

            if (transform.childCount > 0)
            {
                foreach (Transform child in transform.transform)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    public void EnableObject()
    {
        if (GetComponent<Rigidbody>())
        {
            GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
            GetComponent<Rigidbody>().useGravity = true;
            GetComponent<Rigidbody>().isKinematic = false;
        }

        GetComponent<MeshRenderer>().enabled = true;
        GetComponent<Collider>().enabled = true;

        if (ItemType == Type.InventoryItem)
        {
            if (transform.childCount > 0)
            {
                foreach (Transform child in transform.transform)
                {
                    child.gameObject.SetActive(true);
                }
            }
        }
    }

    public Dictionary<string, object> OnSave()
    {
        if (GetComponent<MeshRenderer>())
        {
            bool disableState = true;

            if (disableType == DisableType.DisableRenderer)
            {
                disableState = GetComponent<MeshRenderer>().enabled;
            }
            else if (disableType == DisableType.DisableObject)
            {
                disableState = gameObject.activeSelf;
            }

            return new Dictionary<string, object>()
            {
                { "position", transform.position },
                { "rotation", transform.eulerAngles },
                { "inv_id", inventoryID },
                { "inv_amount", pickupAmount },
                { "weapon_id", weaponID },
                { "examined", isExamined },
                { "customData", customData },
                { "stateDisable", disableState }
            };
        }

        return null;
    }

    public void OnLoad(JToken token)
    {
        transform.position = token["position"].ToObject<Vector3>();
        transform.eulerAngles = token["rotation"].ToObject<Vector3>();
        inventoryID = (int)token["inv_id"];
        pickupAmount = (int)token["inv_amount"];
        weaponID = (int)token["weapon_id"];
        isExamined = (bool)token["examined"];

        customData = token["customData"].ToObject<CustomItemData>();

        if (disableType == DisableType.DisableRenderer)
        {
            DisableObject(token["stateDisable"].ToObject<bool>());
        }
        else if (disableType == DisableType.DisableObject)
        {
            gameObject.SetActive(token["stateDisable"].ToObject<bool>());
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && !isPlayed)
        {
            AnimationObject.Play();

            if (JumpscareSound)
            {
                Tools.PlayOneShot2D(transform.position, JumpscareSound, scareVolume);
            }

            if (enableEffects)
            {
                if (shakeByPreset)
                {
                    CameraShakeInstance shakeInstance = CameraShakePresets.Scare;
                    effects.Scare(shakeInstance, chromaticAberrationAmount, vignetteAmount, scaredBreathTime, effectsTime, ScaredBreath);
                }
                else
                {
                    CameraShakeInstance shakeInstance = new CameraShakeInstance(magnitude, roughness, startTime, durationTime);
                    shakeInstance.PositionInfluence = PositionInfluence;
                    shakeInstance.RotationInfluence = RotationInfluence;
                    effects.Scare(shakeInstance, chromaticAberrationAmount, vignetteAmount, scaredBreathTime, effectsTime, ScaredBreath);
                }
            }

            isPlayed = true;
        }
    }
}
/*
[System.Serializable]
public class ItemHashtable
{
    public string Key;
    public string Value;

    public ItemHashtable(string key, string value)
    {
        Key = key;
        Value = value;
    }
}
*/