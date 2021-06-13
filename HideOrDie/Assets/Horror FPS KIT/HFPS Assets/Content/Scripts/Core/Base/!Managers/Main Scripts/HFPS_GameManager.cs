/*
 * HFPS_GameManager.cs - script written by ThunderWire Games
 * ver. 1.34
*/

using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.PostProcessing;
using ThunderWire.CrossPlatform.Input;
using ThunderWire.Game.Options;
using HFPS.Prefs;
using ThunderWire.Cutscenes;

public enum HideHelpType
{
    Interact, Help
}

/// <summary>
/// The main GameManager
/// </summary>
public class HFPS_GameManager : Singleton<HFPS_GameManager> {

    private PostProcessVolume postProcessing;
    private ColorGrading colorGrading;
    private SaveGameHandler saveHandler;
    private CrossPlatformInput crossPlatformInput;
    private AdvancedMenuUI menuUI;
    private CutsceneManager cutscene;
    private ScriptManager scriptManager;
    private Inventory inventory;

    [Header("Main")]
    public GameObject Player;
    public string m_sceneLoader = "SceneLoader";

    [HideInInspector]
    public Scene CurrentScene;

    [Header("Cursor")]
    public bool m_ShowCursor = false;

    [Header("Game Panels")]
    public GameObject PauseGamePanel;
    public GameObject MainGamePanel;
    public GameObject TabButtonPanel;
    public Selectable DeadFirstButton;

    [Header("Pause UI")]
    public bool reallyPause = false;

    [Header("Pause Effects")]
    public bool useGreyscale = true;
    public float greyscaleFadeSpeed = 5f;

    [Header("Paper UI")]
    public GameObject PaperTextUI;
    public Text PaperReadText;

    [Header("UI Percentagles")]
    public GameObject LightPercentagle;

    [Header("Valve UI")]
    public Slider ValveSlider;

    [Header("Notification UI")]
    public CanvasGroup SaveNotification;
    public GameObject NotificationsPanel;
    public GameObject NotificationPanel;
    public GameObject NotificationPrefab;
    public Sprite WarningSprite;
    public float saveFadeSpeed;
    public float saveShowTime = 3f;

    [Header("Hints UI")]
    public GameObject ExamineNotification;
    public GameObject HintNotification;

    public GameObject HintMessages;
    public GameObject HintTipsPanel;
    public GameObject HintTipPrefab;

    [Header("Crosshair/Cursor")]
    public Image Crosshair;
    public Image ConsoleCursor;
    public float ConsoleCursorSpeed;

    [Header("UI Amounts")]
    public Text HealthText;
    public GameObject AmmoUI;
    public Text BulletsText;
    public Text MagazinesText;

    [Header("Interact UI")]
    public GameObject InteractUI;
    public GameObject InteractInfoUI;
    public GameObject KeyboardButton1;
    public GameObject KeyboardButton2;

    [Header("Down Help Buttons")]
    public GameObject DownHelpUI;
    public GameObject HelpButton1;
    public GameObject HelpButton2;
    public GameObject HelpButton3;
    public GameObject HelpButton4;
    public Sprite DefaultSprite;

    #region Private Variables
    private List<IPauseEvent> PauseEvents = new List<IPauseEvent>();
    private List<GameObject> Notifications = new List<GameObject>();
    private List<GameObject> PickupMessages = new List<GameObject>();

    private Text HintText;
    private Text ExamineTxt;
    private Sprite defaultIcon;
    private Slider lightSlider;
    private Image lightBackground;

    [HideInInspector] public bool isPaused;
    [HideInInspector] public bool isHeld;
    [HideInInspector] public bool canGrab;
    [HideInInspector] public bool isGrabbed;
    [HideInInspector] public bool isExamining;
    [HideInInspector] public bool isLocked;
    [HideInInspector] public bool isWeaponZooming;
    [HideInInspector] public bool ConfigError;

    private CrossPlatformControl UseKey;
    private CrossPlatformControl GrabKey;
    private CrossPlatformControl ThrowKey;
    private CrossPlatformControl RotateKey;
    private CrossPlatformControl CursorKey;
    private bool PauseKey;
    private bool InventoryKey;

    private bool greyscale;
    private bool greyscaleIn = false;
    private bool greyscaleOut = false;

    private bool playerLocked;
    private int oldBlurLevel;

    private bool uiInteractive = true;
    private bool isOverlapping;
    private bool antiSpam;

    private bool isPressedPause;
    private bool isPressedInv;
    private bool isGamepad;
    #endregion

    void Awake()
    {
        scriptManager = ScriptManager.Instance;
        crossPlatformInput = GetComponent<CrossPlatformInput>();
        menuUI = GetComponent<AdvancedMenuUI>();
        saveHandler = GetComponent<SaveGameHandler>();
        inventory = GetComponent<Inventory>();
        cutscene = GetComponent<CutsceneManager>();

        if (scriptManager.ArmsCamera.GetComponent<PostProcessVolume>())
        {
            postProcessing = scriptManager.ArmsCamera.GetComponent<PostProcessVolume>();

            if (postProcessing.profile.HasSettings<ColorGrading>())
            {
                colorGrading = postProcessing.profile.GetSetting<ColorGrading>();
            }
            else if (useGreyscale)
            {
                Debug.LogError($"[PostProcessing] Please add ColorGrading Effect to a {postProcessing.profile.name} profile in order to use Greyscale.");
            }
        }
        else
        {
            Debug.LogError($"[PostProcessing] There is no PostProcessVolume script added to a {ScriptManager.Instance.ArmsCamera.gameObject.name}!");
        }

        lightSlider = LightPercentagle.GetComponentInChildren<Slider>();
        lightBackground = lightSlider.transform.GetChild(0).GetComponent<Image>();
        defaultIcon = LightPercentagle.transform.GetChild(0).GetComponent<Image>().sprite;

        CurrentScene = SceneManager.GetActiveScene();
        uiInteractive = true;

        crossPlatformInput.OnInputsInitialized += OnInputsInitialized;
    }

    void OnInputsInitialized(Device obj)
    {
        isGamepad = obj == Device.Gamepad;

        if (!isGamepad)
        {
            if (m_ShowCursor)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    void Start()
    {
        SetupUI();
        Unpause();

        if (useGreyscale && colorGrading)
        {
            colorGrading.enabled.Override(true);
            colorGrading.saturation.Override(0);
        }

        if (reallyPause)
        {
            foreach (var Instance in FindObjectsOfType<MonoBehaviour>().Where(x => typeof(IPauseEvent).IsAssignableFrom(x.GetType())).Cast<IPauseEvent>())
            {
                PauseEvents.Add(Instance);
            }
        }
    }

    void SetupUI()
    {
        TabButtonPanel.SetActive(false);
        if (SaveNotification)
        {
            SaveNotification.alpha = 0f;
            SaveNotification.gameObject.SetActive(false);
        }

        HideSprites(HideHelpType.Interact);
        HideSprites(HideHelpType.Help);

        HintNotification.SetActive(false);
        ExamineNotification.SetActive(false);

        Color HintColor = HintNotification.GetComponent<Image>().color;
        HintNotification.GetComponent<Image>().color = new Color(HintColor.r, HintColor.g, HintColor.b, 0);
        Color ExmColor = ExamineNotification.GetComponent<Image>().color;
        ExamineNotification.GetComponent<Image>().color = new Color(ExmColor.r, ExmColor.g, ExmColor.b, 0);

        HintText = HintNotification.transform.GetChild(0).GetComponent<Text>();
        ExamineTxt = ExamineNotification.transform.GetChild(0).GetComponent<Text>();
    }

    void Update()
    {
        transform.SetSiblingIndex(0);

        if (crossPlatformInput.inputsLoaded)
        {
            PauseKey = crossPlatformInput.GetActionPressedOnce(this, "Pause");
            InventoryKey = crossPlatformInput.GetActionPressedOnce(this, "Inventory");

            UseKey = crossPlatformInput.ControlOf("Use");
            GrabKey = crossPlatformInput.ControlOf("Examine");
            ThrowKey = crossPlatformInput.ControlOf("Zoom");
            RotateKey = crossPlatformInput.ControlOf("Fire");
            CursorKey = crossPlatformInput.ControlOf("Zoom");

            if (!uiInteractive)
            {
                crossPlatformInput.SuspendInput(true);
            }
            else
            {
                if (isPaused || inventory.isInventoryShown)
                {
                    string[] except = inventory.shortcutActions;
                    except = except.Append("Inventory").Append("Pause").ToArray();
                    crossPlatformInput.SuspendInput(true, except);
                }
                else
                {
                    crossPlatformInput.SuspendInput(false);
                }
            }
        }

        if (!uiInteractive) return;

        if (PauseKey && !isPressedPause && !antiSpam && !menuUI.IsLocked && !cutscene.cutsceneRunning)
        {
            PauseGamePanel.SetActive(!PauseGamePanel.activeSelf);
            MainGamePanel.SetActive(!MainGamePanel.activeSelf);

            StartCoroutine(AntiPauseSpam());

            if (useGreyscale)
            {
                if (!greyscaleIn)
                {
                    GreyscaleScreen(true);
                }
                else if(!greyscaleOut)
                {
                    GreyscaleScreen(false);
                }
            }

            if (!isPaused)
            {
                menuUI.OnShowMenu(true);
            }
            else
            {
                menuUI.ResetPanels();
            }

            isPaused = !isPaused;
            isPressedPause = true;
        }
        else if (!PauseKey && isPressedPause)
        {
            crossPlatformInput.SuspendInput(false);
            isPressedPause = false;
        }

        if (PauseGamePanel.activeSelf && isPaused && isPressedPause)
        {
            Crosshair.enabled = false;
            LockPlayerControls(false, false, true, 3, true);
            scriptManager.GetScript<PlayerFunctions>().enabled = false;
            GetComponent<FloatingIconManager>().SetAllIconsVisible(false);
            if (reallyPause)
            {
                foreach (var PauseEvent in PauseEvents)
                {
                    PauseEvent.OnPauseEvent(true);
                }

                Time.timeScale = 0;
            }
        }
        else if (isPressedPause)
        {
            Crosshair.enabled = true;
            LockPlayerControls(true, true, false, 3, false);
            scriptManager.GetScript<PlayerFunctions>().enabled = true;
            GetComponent<FloatingIconManager>().SetAllIconsVisible(true);
            if (TabButtonPanel.activeSelf)
            {
                TabButtonPanel.SetActive(false);
            }
            if (reallyPause)
            {
                foreach (var PauseEvent in PauseEvents)
                {
                    PauseEvent.OnPauseEvent(false);
                }

                Time.timeScale = 1;
            }
        }

        if (InventoryKey && !isPressedInv && !isPaused && !isOverlapping && !cutscene.cutsceneRunning)
        {
            TabButtonPanel.SetActive(!TabButtonPanel.activeSelf);
            isPressedInv = true;
        }
        else if (!InventoryKey && isPressedInv)
        {
            crossPlatformInput.SuspendInput(false);
            isPressedInv = false;
        }

        NotificationsPanel.SetActive(!TabButtonPanel.activeSelf);

        if (TabButtonPanel.activeSelf && isPressedInv)
        {
            Crosshair.enabled = false;
            GetComponent<FloatingIconManager>().SetAllIconsVisible(false);
            LockPlayerControls(false, false, true, 3, true);
            HideSprites(HideHelpType.Interact);
            HideSprites(HideHelpType.Help);
        }
        else if (isPressedInv)
        {
            Crosshair.enabled = true;
            LockPlayerControls(true, true, false, 3, false);
            GetComponent<FloatingIconManager>().SetAllIconsVisible(true);
        }

        LockScript<ExamineManager>(!TabButtonPanel.activeSelf);

        if (Notifications.Count > 3)
        {
            Destroy(Notifications[0]);
        }

        Notifications.RemoveAll(x => x == null);

        if (greyscale && colorGrading)
        {
            if (greyscaleIn)
            {
                if (colorGrading.saturation.value > -99)
                {
                    colorGrading.saturation.value -= Time.unscaledDeltaTime * (greyscaleFadeSpeed * 20);
                }
                else if (colorGrading.saturation <= -99)
                {
                    colorGrading.saturation.Override(-100);
                }
            }

            if (greyscaleOut)
            {
                if (colorGrading.saturation.value < -1)
                {
                    colorGrading.saturation.value += Time.unscaledDeltaTime * (greyscaleFadeSpeed * 20);
                }
                else if (colorGrading.saturation >= -1)
                {
                    colorGrading.saturation.Override(0);
                }
            }
        }
    }

    void OnDisable()
    {
        colorGrading.saturation.Override(0);
    }

    IEnumerator AntiPauseSpam()
    {
        antiSpam = true;
        yield return new WaitForSecondsRealtime(0.5f);
        antiSpam = false;
    }

    public void ShowInventory(bool show)
    {
        if (show)
        {
            isPressedInv = true;
            TabButtonPanel.SetActive(true);
            Crosshair.enabled = false;
            GetComponent<FloatingIconManager>().SetAllIconsVisible(false);
            LockPlayerControls(false, false, true, 3, true);
            HideSprites(HideHelpType.Interact);
            HideSprites(HideHelpType.Help);
        }
        else
        {
            isPressedInv = false;
            TabButtonPanel.SetActive(false);
            Crosshair.enabled = true;
            LockPlayerControls(true, true, false, 3, false);
            GetComponent<FloatingIconManager>().SetAllIconsVisible(true);
        }
    }

    public void GreyscaleScreen(bool Greyscale)
    {
        greyscale = true;

        switch (Greyscale)
        {
            case true:
                greyscaleIn = true;
                greyscaleOut = false;
                break;
            case false:
                greyscaleIn = false;
                greyscaleOut = true;
                break;
        }
    }

    public void Unpause()
    {
        GetComponent<FloatingIconManager>().SetAllIconsVisible(true);

        if (TabButtonPanel.activeSelf)
        {
            TabButtonPanel.SetActive(false);
        }

        if (useGreyscale)
        {
            GreyscaleScreen(false);
        }

        Crosshair.enabled = true;
        LockPlayerControls(true, true, false, 3, false);
        PauseGamePanel.SetActive(false);
        MainGamePanel.SetActive(true);
        isPaused = false;

        if (reallyPause)
        {
            foreach (var PauseEvent in PauseEvents)
            {
                PauseEvent.OnPauseEvent(false);
            }

            Time.timeScale = 1;
        }
    }

    /// <summary>
    /// Lock some Player Controls
    /// </summary>
    /// <param name="Controller">Player Controller Enabled State</param>
    /// <param name="Interact">Interact Enabled State</param>
    /// <param name="CursorVisible">Show, Hide Cursor?</param>
    /// <param name="BlurLevel">0 - Null, 1 - MainCam Blur, 2 - ArmsCam Blur, 3 - Both Blur</param>
    /// <param name="BlurEnable">Enable/Disable Blur?</param>
    /// <param name="ResetBlur">Reset Blur?</param>
    /// <param name="ForceLockLevel">0 - Null, 1 = Enable, 2 - Disable</param>
    public void LockPlayerControls(bool Controller, bool Interact, bool CursorVisible, int BlurLevel = 0, bool BlurEnable = false, bool ResetBlur = false, int ForceLockLevel = 0)
    {
        if (ForceLockLevel == 2)
        {
            playerLocked = false;
        }

        if (!playerLocked)
        {
            //Controller Lock
            Player.GetComponent<PlayerController>().isControllable = Controller;
            scriptManager.GetScript<PlayerFunctions>().enabled = Controller;
            scriptManager.ScriptGlobalState = Controller;
            LockScript<MouseLook>(Controller);

            //Interact Lock
            scriptManager.GetScript<InteractManager>().inUse = !Interact;
        }

        //Show Cursor
        ShowCursor(CursorVisible && !isGamepad);

        //Blur Levels
        if (BlurLevel > 0)
        {
            if (BlurEnable)
            {
                SetBlur(true, BlurLevel, ResetBlur);
            }
            else
            {
                if (playerLocked)
                {
                    SetBlur(true, oldBlurLevel, true);
                }
                else
                {
                    SetBlur(false, BlurLevel);
                }
            }
        }

        if (ForceLockLevel == 1)
        {
            playerLocked = true;
            oldBlurLevel = BlurLevel;
        }
    }

    private void SetBlur(bool Enable, int BlurLevel, bool Reset = false)
    {
        PostProcessVolume mainPostProcess = scriptManager.MainCamera.GetComponent<PostProcessVolume>();
        PostProcessVolume armsPostProcess = scriptManager.ArmsCamera.GetComponent<PostProcessVolume>();

        if (!mainPostProcess.profile.HasSettings<Blur>())
        {
            Debug.LogError($"[PostProcessing] {mainPostProcess.gameObject.name} does not have Blur PostProcessing Script.");
            return;
        }

        if (!armsPostProcess.profile.HasSettings<Blur>())
        {
            Debug.LogError($"[PostProcessing] {armsPostProcess.gameObject.name} does not have Blur PostProcessing script.");
            return;
        }

        if (Reset)
        {
            mainPostProcess.profile.GetSetting<Blur>().enabled.Override(false);
            armsPostProcess.profile.GetSetting<Blur>().enabled.Override(false);
        }

        if (BlurLevel == 1) { mainPostProcess.profile.GetSetting<Blur>().enabled.Override(Enable); }
        if (BlurLevel == 2) { armsPostProcess.profile.GetSetting<Blur>().enabled.Override(Enable); }
        if (BlurLevel == 3)
        {
            mainPostProcess.profile.GetSetting<Blur>().enabled.Override(Enable);
            armsPostProcess.profile.GetSetting<Blur>().enabled.Override(Enable);
        }
    }

    public void LockScript<T> (bool state) where T : MonoBehaviour
    {
        if (scriptManager.GetScript<T>())
        {
            scriptManager.GetScript<T>().enabled = state;
        }
        else
        {
            if (scriptManager.gameObject.GetComponent<T>())
            {
                scriptManager.gameObject.GetComponent<T>().enabled = state;
            }
            else
            {
                Debug.LogError("Script " + typeof(T).Name + " cannot be locked");
            }
        }
    }

    public bool IsEnabled<T>() where T : MonoBehaviour
    {
        if (scriptManager.GetScript<T>())
        {
            return scriptManager.GetScript<T>().enabled;
        }
        else
        {
            if (scriptManager.gameObject.GetComponent<T>())
            {
                return scriptManager.gameObject.GetComponent<T>().enabled;
            }
        }

        return false;
    }

    public void UIPreventOverlap(bool State)
    {
        isOverlapping = State;
    }

    public void ShowCursor(bool state)
    {
        switch (state) {
            case true:
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                break;
            case false:
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                break;
        }
    }

    public void ShowConsoleCursor(bool state)
    {
        if (ConsoleCursor)
        {
            ConsoleCursor.gameObject.SetActive(state);
            ConsoleCursor.GetComponent<RectTransform>().localPosition = Vector3.zero;
        }
    }

    public void MoveConsoleCursor(Vector2 movement)
    {
        if (ConsoleCursor && ConsoleCursor.gameObject.activeSelf)
        {
            Vector3 consoleCursorPos = ConsoleCursor.transform.position;
            consoleCursorPos.x += movement.x * ConsoleCursorSpeed;
            consoleCursorPos.y += movement.y * ConsoleCursorSpeed;
            ConsoleCursor.transform.position = consoleCursorPos;
        }
    }

    public void AddPickupMessage(string itemName)
    {
        GameObject Message = Instantiate(NotificationPrefab, NotificationPanel.transform);
        Notifications.Add(Message);
        Message.GetComponent<Notification>().SetMessage(string.Format("Picked up {0}", itemName));
    }

    public void AddMessage(string message)
    {
        GameObject Message = Instantiate(NotificationPrefab.gameObject, NotificationPanel.transform);
        Notifications.Add(Message);
        Message.GetComponent<Notification>().SetMessage(message);
    }

    public void AddSingleMessage(string message, string id, bool isWarning = false)
    {
        if (Notifications.Count == 0 || Notifications.Count(x => x.GetComponent<Notification>().id == id) == 0)
        {
            GameObject Message = Instantiate(NotificationPrefab.gameObject, NotificationPanel.transform);
            Notifications.Add(Message);

            Message.GetComponent<Notification>().id = id;

            if (!isWarning)
            {
                Message.GetComponent<Notification>().SetMessage(message);
            }
            else
            {
                Message.GetComponent<Notification>().SetMessage(message, 3f, WarningSprite);
            }
        }
    }

    public void WarningMessage(string warning)
    {
        GameObject Message = Instantiate(NotificationPrefab, NotificationPanel.transform);
        Notifications.Add(Message);
        Message.GetComponent<Notification>().SetMessage(warning, 3f, WarningSprite);
    }

    public void ShowExamineText(string text)
    {
        ExamineTxt.text = text;
        ExamineNotification.SetActive(true);
        UIFade uIFade = UIFade.CreateInstance(ExamineNotification, "[UIFader] ExamineNotification");
        uIFade.ResetGraphicsColor();
        uIFade.ImageTextAlpha(0.8f, 1f);
        uIFade.fadeOut = false;
        uIFade.FadeInOut(fadeOutSpeed: 3f, fadeOutAfter: UIFade.FadeOutAfter.Bool);
        isExamining = false;
    }

    public void ShowHint(string hint, float time = 3f, InteractiveItem.MessageTip[] messageTips = null)
    {
        HintText.text = hint;

        if(PickupMessages.Count > 0)
        {
            foreach (var item in PickupMessages)
            {
                Destroy(item);
            }
        }

        PickupMessages.Clear();

        if (messageTips != null && messageTips.Length > 0)
        {
            foreach (var item in messageTips)
            {
                GameObject obj = Instantiate(HintTipPrefab, HintTipsPanel.transform);
                CrossPlatformControl? input = crossPlatformInput.ControlOf(item.InputString);
                PickupMessages.Add(obj);

                if (input.HasValue)
                {
                    SetKey(obj.transform, input.Value, item.KeyMessage);
                }
            }

            HintMessages.SetActive(true);
        }
        else
        {
            HintMessages.SetActive(false);
        }

        UIFade uIFade = UIFade.CreateInstance(HintNotification, "[UIFader] HintNotification");
        uIFade.ResetGraphicsColor();
        uIFade.ImageTextAlpha(0.8f, 1f);
        uIFade.FadeInOut(fadeOutTime: time, fadeOutAfter: UIFade.FadeOutAfter.Time);
        uIFade.onFadeOutEvent += delegate {
            foreach (var item in PickupMessages)
            {
                Destroy(item);
            }
        };
        isExamining = false;

        foreach (GameObject mtip in PickupMessages)
        {
            HorizontalLayoutGroup horizontalLayoutGroup = mtip.GetComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.enabled = false;
            horizontalLayoutGroup.enabled = true;
        }
    }

    public void HideExamine()
    {
        UIFade fade = UIFade.FindUIFader(ExamineNotification);

        if(fade != null)
        {
            fade.fadeOut = true;
        }
    }

    public void ShowLightPercentagle(float start = 0f, bool fadeIn = true, Sprite icon = null)
    {
        UIFade fade = UIFade.CreateInstance(LightPercentagle, "[UIFader] LightPercentagle");
        lightSlider.value = PercentToValue(start);

        if (icon != null)
        {
            LightPercentagle.transform.GetChild(0).GetComponent<Image>().sprite = icon;
        }
        else
        {
            LightPercentagle.transform.GetChild(0).GetComponent<Image>().sprite = defaultIcon;
        }

        if (fadeIn)
        {
            fade.ResetGraphicsColor();
            fade.SetFadeValues(new UIFade.FadeValue[] { new UIFade.FadeValue(lightBackground.gameObject, 0.7f) });
            fade.FadeInOut(1, 1.5f, 3f, fadeOutAfter: UIFade.FadeOutAfter.Bool);
        }
        else
        {
            fade.fadeOut = true;
        }
    }

    public void UpdateLightPercent(float value)
    {
        lightSlider.value = Mathf.MoveTowards(lightSlider.value, PercentToValue(value), Time.deltaTime * 1);
    }

    float PercentToValue(float value)
    {
        if(value > 1)
        {
            return value / 100;
        }
        else
        {
            return value;
        }
    }

    public void ShowSaveNotification()
    {
        if (SaveNotification)
        {
            StartCoroutine(ShowSave());
        }
    }

    IEnumerator ShowSave()
    {
        float speed = 3f;

        SaveNotification.gameObject.SetActive(true);

        while (SaveNotification.alpha <= 0.9f)
        {
            SaveNotification.alpha += Time.fixedDeltaTime * speed;
            yield return null;
        }

        SaveNotification.alpha = 1f;
        yield return new WaitForSecondsRealtime(saveShowTime);

        while (SaveNotification.alpha >= 0.1f)
        {
            SaveNotification.alpha -= Time.fixedDeltaTime * speed;
            yield return null;
        }

        SaveNotification.alpha = 0f;
        SaveNotification.gameObject.SetActive(false);
    }

    public bool CheckController()
	{
		return Player.GetComponent<PlayerController> ().isControllable;
	}

    private void SetKey(Transform ControlObj, CrossPlatformControl Control, string ControlName = "")
    {
        if (!string.IsNullOrEmpty(ControlName))
        {
            ControlObj.GetChild(1).GetComponent<Text>().text = ControlName;
        }

        if (!string.IsNullOrEmpty(Control.Control))
        {
            if (Control.DeviceType == ControlDevice.Keyboard)
            {
                Sprite button = crossPlatformInput.crossPlatformSprites.GetSprite(Control.Control, crossPlatformInput.GetCurrentPlatform());

                if (button != null)
                {
                    ControlObj.GetChild(0).GetComponent<Image>().sprite = button;
                }
                else
                {
                    Debug.LogError("[Control Sprite] The specified control key was not found!");
                }
            }
            else if (Control.DeviceType == ControlDevice.Mouse)
            {
                Sprite mouse = crossPlatformInput.crossPlatformSprites.GetMouseSprite(Control.Control);
                ControlObj.GetChild(0).GetComponent<Image>().sprite = mouse;
            }
            else if (Control.DeviceType == ControlDevice.Gamepad)
            {
                Sprite button = crossPlatformInput.crossPlatformSprites.GetSprite(Control.Control, crossPlatformInput.GetCurrentPlatform());
                ControlObj.GetChild(0).GetComponent<Image>().sprite = button;
            }

            ControlObj.gameObject.SetActive(true);
        }
        else
        {
            ControlObj.gameObject.SetActive(false);
        }
    }

    public void ShowInteractSprite(int Row, string KeyName, CrossPlatformControl control)
    {
        if (isHeld) return;
        InteractUI.SetActive(true);

        switch (Row)
        {
            case 1:
                SetKey(KeyboardButton1.transform, control, KeyName);
                break;
            case 2:
                SetKey(KeyboardButton2.transform, control, KeyName);
                break;
        }
    }

    public void ShowInteractInfo(string info)
    {
        InteractInfoUI.SetActive(true);
        InteractInfoUI.GetComponent<Text>().text = info;
    }

    /// <summary>
    /// Show Down Help Buttons
    /// </summary>
    public void ShowHelpButtons(HelpButton help1, HelpButton help2, HelpButton help3, HelpButton help4)
    {
        if (help1 != null) { SetKey(HelpButton1.transform, help1.Control, help1.Name); } else { HelpButton1.SetActive(false); }
        if (help2 != null) { SetKey(HelpButton2.transform, help2.Control, help2.Name); } else { HelpButton2.SetActive(false); }
        if (help3 != null) { SetKey(HelpButton3.transform, help3.Control, help3.Name); } else { HelpButton3.SetActive(false); }
        if (help4 != null) { SetKey(HelpButton4.transform, help4.Control, help4.Name); } else { HelpButton4.SetActive(false); }

        if (help1 != null || help2 != null || help3 != null || help4 != null)
        {
            DownHelpUI.SetActive(true);
        }
    }

    /// <summary>
    /// Show Examine UI Buttons
    /// </summary>
    /// <param name="btn1">Put Away</param>
    /// <param name="btn2">Use</param>
    /// <param name="btn3">Rotate</param>
    /// <param name="btn4">Show Cursor</param>
    public void ShowExamineSprites(bool btn1 = true, bool btn2 = true, bool btn3 = true, bool btn4 = true, string PutAwayText = "Put Away", string UseText = "Take")
    {
        if (btn1) { SetKey(HelpButton1.transform, GrabKey, PutAwayText); } else { HelpButton1.SetActive(false); }
        if (btn2) { SetKey(HelpButton2.transform, UseKey, UseText); } else { HelpButton2.SetActive(false); }
        if (btn3) { SetKey(HelpButton3.transform, RotateKey, "Rotate"); } else { HelpButton3.SetActive(false); }
        if (btn4) { SetKey(HelpButton4.transform, CursorKey, "Show Cursor"); } else { HelpButton4.SetActive(false); }
        DownHelpUI.SetActive(true);
    }

    public void ShowPaperExamineSprites(CrossPlatformControl ExamineKey, bool rotate, string ExamineText = "Examine")
    {
        SetKey(HelpButton1.transform, GrabKey, "Put Away");
        SetKey(HelpButton2.transform, ExamineKey, ExamineText);

        if (rotate)
        {
            SetKey(HelpButton3.transform, RotateKey, "Rotate");
        }
        else
        {
            HelpButton3.SetActive(false);
        }

        HelpButton4.SetActive(false);
        DownHelpUI.SetActive(true);
    }

    public void ShowGrabSprites()
    {
        SetKey(HelpButton1.transform, GrabKey, "Put Away");
        SetKey(HelpButton2.transform, RotateKey, "Rotate");
        SetKey(HelpButton3.transform, ThrowKey, "Throw");
        HelpButton4.SetActive(false);
        DownHelpUI.SetActive(true);
    }

    public Sprite GetKeySprite(string Key)
    {
        return Resources.Load<Sprite>(Key);
    }

    public void HideSprites(HideHelpType type)
	{
		switch (type) {
            case HideHelpType.Interact:
                KeyboardButton1.SetActive(false);
                KeyboardButton2.SetActive(false);
                InteractInfoUI.SetActive(false);
                InteractUI.SetActive(false);
                break;
            case HideHelpType.Help:
                DownHelpUI.SetActive(false);
                break;
		}
	}

    public void ShowDeadPanel()
    {
        LockPlayerControls(false, false, true);
        scriptManager.GetScript<ItemSwitcher>().DisableItems();
        scriptManager.GetScript<ItemSwitcher>().enabled = false;

        GetComponent<AdvancedMenuUI>().ShowTabPanel("Dead"); //Show Dead UI and Buttons
        GetComponent<AdvancedMenuUI>().SelectFirstOption(DeadFirstButton, null);

        PauseGamePanel.SetActive(false);
        MainGamePanel.SetActive(false);

        uiInteractive = false;
    }

    public void ChangeScene(string SceneName)
    {
        SceneManager.LoadScene(SceneName);
    }

    public void LoadNextScene(string scene)
    {
        if (saveHandler)
        {
            if (saveHandler.crossSceneSaving)
            {
                saveHandler.SaveNextSceneData(scene);

                if (!isPaused)
                {
                    LockPlayerControls(false, false, false);
                }

                if (saveHandler.fadeControl)
                {
                    saveHandler.fadeControl.FadeIn();
                }

                StartCoroutine(LoadScene(scene, 2));
            }
        }
    }

    public void Retry()
    {
        if (saveHandler.fadeControl)
        {
            saveHandler.fadeControl.FadeIn();
        }

        StartCoroutine(LoadScene(SceneManager.GetActiveScene().name, 1));
    }

    private IEnumerator LoadScene(string scene, int loadstate)
    {
        yield return new WaitUntil(() => !saveHandler.fadeControl.IsFadedIn);

        Prefs.Game_SaveName(saveHandler.lastSave);
        Prefs.Game_LoadState(loadstate);
        Prefs.Game_LevelName(scene);

        SceneManager.LoadScene(m_sceneLoader);
    }
}

public class HelpButton
{
    public string Name;
    public CrossPlatformControl Control;

    public HelpButton(string name, CrossPlatformControl control)
    {
        Name = name;
        Control = control;
    }
}