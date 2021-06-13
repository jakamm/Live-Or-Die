/*
 * AdvancedMenuUI.cs - by ThunderWire Studio
 * Version 1.0
*/

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using ThunderWire.CrossPlatform.Input;
using CPCS = CrossPlatformControlScheme;

namespace ThunderWire.Game.Options
{
    [RequireComponent(typeof(PlayerInput), typeof(OptionsController))]
    public class AdvancedMenuUI : Singleton<AdvancedMenuUI>
    {
        public enum OptionTab { General, Graphic, Controls }

        #region Structures
        [Serializable]
        public class TabPanelContent
        {
            public string Name;
            public GameObject PanelObj;

            public GameObject[] PCContent;
            public GameObject[] ConsoleContent;

            public TabPanelEvents Events;
            public bool isOptions;
        }

        [Serializable]
        public struct TabObject
        {
            public GameObject Tab;
            public TabButton Button;
            public Selectable FirstOption;
            public Selectable FirstOptionAlt;
        }

        [Serializable]
        public struct GPStruct
        {
            public string Name;
            public CrossGamepadControl Control;
            public CPCS.OutputButtonType Output;
        }
        #endregion

        private CrossPlatformInput crossPlatformInput;
        private OptionsController optionsController;
        private HFPS_GameManager gameManager;
        private Device device = Device.Keyboard;

        public OptionTab currentTab = OptionTab.General;
        public bool isMainMenu;

        [Header("Cross-Platform")]
        public GameObject GamepadHelpersParent;

        [Header("Cross-Platform Panels")]
        public GameObject PausePanel;
        public TabPanelContent[] tabContents;

        [Header("Gamepad Controls")]
        public Image GamepadType;
        public GameObject LeftControlPrefab;
        public GameObject RightControlPrefab;
        public Transform LeftGPControls;
        public Transform RightGPControls;
        public Selectable GamepadScheme;

        [Header("PC Keybinding")]
        public Transform KeyboardParent;
        public GameObject SingleControl;
        public GameObject ReplacePrompt;
        public Button BackSettingsBTN;
        public Button ApplySettingsBTN;
        public Text ReplaceText;
        [Multiline]
        public string ReplaceFormat = "The control binded to a following action \"{0}\", will be removed!";

        [Header("First Select")]
        public bool manualSelect;
        public bool allowSelectOnPC;
        public Selectable FirstButton;
        public Selectable FirstAltButton;

        [Header("Tabs")]
        public TabObject GeneralTab;
        public TabObject GraphicTab;
        public TabObject ControlsTab;

        private TabObject SelectedTab;
        private string SelectedPanel;

        #region Private Variables
        [HideInInspector]
        public bool IsLocked = false;
        [HideInInspector]
        public bool OptionsShown = false;

        [HideInInspector]
        public Vector2 Navigation;

        [HideInInspector]
        public bool IsRebinding;

        private bool rebindUnsaved;
        private KeybindUI currentRebind;
        private KeybindUI sameKeybind;
        private KeyMouse pressedKey;

        private List<GPStruct> LeftGPStructures = new List<GPStruct>();
        private List<GPStruct> RightGPStructures = new List<GPStruct>();
        private List<GameObject> GPCache = new List<GameObject>();
        private List<KeybindUI> KeyboardControls = new List<KeybindUI>();
        #endregion

        void Awake()
        {
            crossPlatformInput = CrossPlatformInput.Instance;
            gameManager = GetComponent<HFPS_GameManager>();
            optionsController = GetComponent<OptionsController>();
            SelectedTab = GeneralTab;
            OptionsShown = false;
            IsLocked = false;
        }

        void Start()
        {
            crossPlatformInput.OnInputsInitialized += OnInputsInitialized;
            crossPlatformInput.OnKeyBindPressed += OnKeyBindPressed;
            crossPlatformInput.OnRebindCancelled += OnRebindCancel;
            crossPlatformInput.OnRebined += OnRebined;
        }

        void FixedUpdate()
        {
            if(EventSystem.current.currentSelectedGameObject == null && device == Device.Gamepad && string.IsNullOrEmpty(SelectedPanel))
            {
                if (!manualSelect)
                {
                    if (FirstButton && FirstButton.interactable)
                    {
                        FirstButton.Select();
                    }
                    else if (FirstAltButton && FirstAltButton.interactable)
                    {
                        FirstAltButton.Select();
                    }
                }
            }
        }

        #region Cross-Platform Events
        void OnInputsInitialized(Device device)
        {
            this.device = device;

            if (device == Device.Gamepad)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                if (!manualSelect)
                {
                    if (FirstButton && FirstButton.interactable)
                    {
                        FirstButton.Select();
                    }
                    else if (FirstAltButton && FirstAltButton.interactable)
                    {
                        FirstAltButton.Select();
                    }
                }

                StartCoroutine(InitializeGamepadControls());

                foreach (var panel in tabContents)
                {
                    foreach (var content in panel.PCContent)
                    {
                        content.SetActive(false);
                    }

                    foreach (var content in panel.ConsoleContent)
                    {
                        content.SetActive(true);
                    }
                }

                GamepadHelpersParent.SetActive(true);
            }
            else if(device == Device.Keyboard)
            {
                if (!manualSelect && allowSelectOnPC)
                {
                    if (FirstButton && FirstButton.interactable)
                    {
                        FirstButton.Select();
                    }
                    else if (FirstAltButton && FirstAltButton.interactable)
                    {
                        FirstAltButton.Select();
                    }
                }

                StartCoroutine(InitializeKeyboardControls());

                foreach (var panel in tabContents)
                {
                    foreach (var content in panel.PCContent)
                    {
                        content.SetActive(true);
                    }

                    foreach (var content in panel.ConsoleContent)
                    {
                        content.SetActive(false);
                    }
                }

                GamepadHelpersParent.SetActive(false);
            }
        }

        void OnKeyBindPressed(KeyMouse key)
        {
            pressedKey = key;

            if (!KeyboardControls.Any(x => x.KeyBind == key))
            {
                currentRebind.RebindKey(key);
                crossPlatformInput.AcceptRebind(key);
            }
            else
            {
                sameKeybind = KeyboardControls.Where(x => x.KeyBind == key).FirstOrDefault();
                ReplacePrompt.SetActive(true);
                ReplaceText.text = string.Format(ReplaceFormat, sameKeybind.ActionNameText.text);
            }
        }

        void OnRebined()
        {
            foreach (var control in KeyboardControls)
            {
                control.KeybindButton.interactable = true;

                if (control.GetComponent<Selectable>())
                {
                    control.GetComponent<Selectable>().interactable = true;
                }
            }

            ApplySettingsBTN.interactable = true;
            BackSettingsBTN.interactable = true;

            pressedKey = KeyMouse.None;
            currentRebind = null;
            sameKeybind = null;
            IsRebinding = false;
            rebindUnsaved = true;
            IsLocked = false;
        }

        public void OnRebindCancel()
        {
            ReplacePrompt.SetActive(false);
            currentRebind.ResetKey();
            OnRebined();
        }
        #endregion

        IEnumerator InitializeGamepadControls()
        {
            yield return new WaitUntil(() => crossPlatformInput.inputsLoaded);

            Platform platform = crossPlatformInput.GetCurrentPlatform();

            if (crossPlatformInput.deviceType == Device.Gamepad && device == Device.Gamepad)
            {
                if (platform == Platform.PS4)
                {
                    GamepadType.sprite = crossPlatformInput.crossPlatformSprites.PS4.PS4Gamepad;
                }
                else if (platform == Platform.XboxOne)
                {
                    GamepadType.sprite = crossPlatformInput.crossPlatformSprites.XboxOne.XboxGamepad;
                }
            }
            else
            {
                yield break;
            }

            var all = crossPlatformInput.GamepadAll();
            List<CPCS.GamepadControl> left_struct = new List<CPCS.GamepadControl>();
            List<CPCS.GamepadControl> right_struct = new List<CPCS.GamepadControl>();
            GamepadScheme.GetComponentInChildren<Text>().text = all.schemeName.ToUpper();

            foreach (var item in all.gamepadControls)
            {
                if (IsControlLeft(item.GamepadButton))
                {
                    left_struct.Add(item);
                }
                else
                {
                    right_struct.Add(item);
                }
            }

            left_struct = left_struct.OrderBy(x => (int)x.GamepadButton).ToList();
            right_struct = right_struct.OrderBy(x => (int)x.GamepadButton).ToList();

            foreach (var item in left_struct)
            {
                if (LeftGPStructures.Any(x => x.Control == item.GamepadButton && x.Output == item.Output))
                {
                    for (int i = 0; i < LeftGPStructures.Count; i++)
                    {
                        if (LeftGPStructures[i].Control == item.GamepadButton)
                        {
                            var structure = LeftGPStructures[i];
                            structure.Name += "/" + item.ActionName;
                            LeftGPStructures[i] = structure;
                            break;
                        }
                    }
                }
                else
                {
                    LeftGPStructures.Add(new GPStruct() { Name = item.ActionName, Control = item.GamepadButton, Output = item.Output });
                }
            }

            foreach (var item in right_struct)
            {
                if (RightGPStructures.Any(x => x.Control == item.GamepadButton && x.Output == item.Output))
                {
                    for (int i = 0; i < RightGPStructures.Count; i++)
                    {
                        if (RightGPStructures[i].Control == item.GamepadButton)
                        {
                            var structure = RightGPStructures[i];
                            structure.Name += "/" + item.ActionName;
                            RightGPStructures[i] = structure;
                            break;
                        }
                    }
                }
                else
                {
                    RightGPStructures.Add(new GPStruct() { Name = item.ActionName, Control = item.GamepadButton, Output = item.Output });
                }
            }

            foreach (var lt in LeftGPStructures)
            {
                GameObject obj = Instantiate(LeftControlPrefab, LeftGPControls);
                obj.transform.GetChild(0).GetComponent<Image>().sprite = crossPlatformInput.crossPlatformSprites.GetSprite(lt.Control, platform);
                obj.transform.GetChild(1).GetComponent<Text>().text = lt.Name;
                GPCache.Add(obj);
            }

            foreach (var rt in RightGPStructures)
            {
                GameObject obj = Instantiate(RightControlPrefab, RightGPControls);
                obj.transform.GetChild(0).GetComponent<Image>().sprite = crossPlatformInput.crossPlatformSprites.GetSprite(rt.Control, platform);
                obj.transform.GetChild(1).GetComponent<Text>().text = rt.Name;
                GPCache.Add(obj);
            }
        }

        IEnumerator InitializeKeyboardControls()
        {
            yield return new WaitUntil(() => crossPlatformInput.inputsLoaded);

            foreach (var control in crossPlatformInput.KeyboardAll())
            {
                if (control.Output == CPCS.OutputButtonType.Bool)
                {
                    GameObject obj = Instantiate(SingleControl, KeyboardParent);
                    KeybindUI keybind = obj.GetComponentInChildren<KeybindUI>();
                    keybind.Initialize(control.ButtonBinding.KeyMouseKey, control.ActionName);
                    keybind.KeybindButton.onClick.AddListener(delegate { RebindButton(keybind); });
                    KeyboardControls.Add(keybind);
                }
                else
                {
                    GameObject obj1 = Instantiate(SingleControl, KeyboardParent);
                    KeybindUI keybind1 = obj1.GetComponentInChildren<KeybindUI>();
                    GameObject obj2 = Instantiate(SingleControl, KeyboardParent);
                    KeybindUI keybind2 = obj2.GetComponentInChildren<KeybindUI>();
                    GameObject obj3 = Instantiate(SingleControl, KeyboardParent);
                    KeybindUI keybind3 = obj3.GetComponentInChildren<KeybindUI>();
                    GameObject obj4 = Instantiate(SingleControl, KeyboardParent);
                    KeybindUI keybind4 = obj4.GetComponentInChildren<KeybindUI>();

                    keybind1.Initialize(control.VectorBinding.UpKey, control.ActionName, control.ActionName + " Up");
                    keybind1.KeybindButton.onClick.AddListener(delegate { RebindButton(keybind1); });
                    KeyboardControls.Add(keybind1);

                    keybind2.Initialize(control.VectorBinding.DownKey, control.ActionName, control.ActionName + " Down");
                    keybind2.KeybindButton.onClick.AddListener(delegate { RebindButton(keybind2); });
                    KeyboardControls.Add(keybind2);

                    keybind3.Initialize(control.VectorBinding.LeftKey, control.ActionName, control.ActionName + " Left");
                    keybind3.KeybindButton.onClick.AddListener(delegate { RebindButton(keybind3); });
                    KeyboardControls.Add(keybind3);

                    keybind4.Initialize(control.VectorBinding.RightKey, control.ActionName, control.ActionName + " Right");
                    keybind4.KeybindButton.onClick.AddListener(delegate { RebindButton(keybind4); });
                    KeyboardControls.Add(keybind4);
                }
            }
        }

        bool IsControlLeft(CrossGamepadControl control)
        {
            switch (control)
            {
                case CrossGamepadControl.LeftTrigger:
                case CrossGamepadControl.LeftShoulder:
                case CrossGamepadControl.Select:
                case CrossGamepadControl.DpadUp:
                case CrossGamepadControl.DpadDown:
                case CrossGamepadControl.DpadLeft:
                case CrossGamepadControl.DpadRight:
                case CrossGamepadControl.LeftStick:
                    return true;
            }

            return false;
        }

        void ClearGPControls()
        {
            foreach (var item in GPCache)
            {
                Destroy(item);
            }

            LeftGPStructures.Clear();
            RightGPStructures.Clear();
        }

        public void ShowTabPanel(string name)
        {
            foreach (var panel in tabContents)
            {
                if (panel.Name.Equals(name))
                {
                    SelectedPanel = panel.Name;
                    panel.PanelObj.SetActive(true);
                    OptionsShown = panel.isOptions;
                    break;
                }
            }
        }

        public async void ApplyOptions()
        {
            optionsController.ApplyOptions(device == Device.Keyboard ? true : false);

            if (rebindUnsaved)
            {
                await crossPlatformInput.SerializeNewControls();
                rebindUnsaved = false;
            }
        }

        public void RebindButton(KeybindUI keybind)
        {
            crossPlatformInput.StartKeyRebind(keybind.ActionName, keybind.KeyBind);
            currentRebind = keybind;
            keybind.KeyText("Press Key");

            foreach (var control in KeyboardControls)
            {
                control.KeybindButton.interactable = false;

                if (control.GetComponent<Selectable>())
                {
                    control.GetComponent<Selectable>().interactable = false;
                }
            }


            ApplySettingsBTN.interactable = false;
            BackSettingsBTN.interactable = false;

            IsRebinding = true;
            IsLocked = true;
        }

        public void ForceRebind()
        {
            ReplacePrompt.SetActive(false);
            currentRebind.RebindKey(pressedKey);
            sameKeybind.RebindKey(KeyMouse.None);
            crossPlatformInput.AcceptRebind(pressedKey);
        }

        public void SelectTab(int tab)
        {
            SelectedTab.Button.Unhold();
            SelectedTab.Tab.SetActive(false);

            if (tab == 0)
            {
                GeneralTab.Button.Select();
                GeneralTab.Tab.SetActive(true);
                GraphicTab.Tab.SetActive(false);
                ControlsTab.Tab.SetActive(false);
                SelectFirstOption(GeneralTab.FirstOption, GeneralTab.FirstOptionAlt, false, true);
                SelectedTab = GeneralTab;
            }
            else if (tab == 1)
            {
                GraphicTab.Button.Select();
                GeneralTab.Tab.SetActive(false);
                GraphicTab.Tab.SetActive(true);
                ControlsTab.Tab.SetActive(false);
                SelectFirstOption(GraphicTab.FirstOption, GraphicTab.FirstOptionAlt, false, true);
                SelectedTab = GraphicTab;
            }
            else if (tab == 2)
            {
                ControlsTab.Button.Select();
                GeneralTab.Tab.SetActive(false);
                GraphicTab.Tab.SetActive(false);
                ControlsTab.Tab.SetActive(true);
                SelectFirstOption(ControlsTab.FirstOption, ControlsTab.FirstOptionAlt, false, true);
                SelectedTab = ControlsTab;
            }

            currentTab = (OptionTab)tab;
        }

        public void OnShowMenu(bool show)
        {
            if (show)
            {
                if (!manualSelect && allowSelectOnPC || device == Device.Gamepad)
                {
                    SelectFirstOption(FirstButton, FirstAltButton, !isMainMenu, false);
                }
            }
            else
            {
                SelectedPanel = null;
            }
        }

        public void ResetPanels()
        {
            SelectTab(0);

            foreach (var obj in tabContents)
            {
                obj.PanelObj.SetActive(false);
            }

            if (PausePanel)
            {
                PausePanel.SetActive(true);
            }

            SelectedPanel = string.Empty;
            OptionsShown = false;
        }

        public void DeselectPanel()
        {
            SelectedPanel = string.Empty;
            OptionsShown = false;
        }

        public void SelectFirstOption(Selectable first, Selectable alt, bool sendMessage = false, bool activeCheck = true)
        {
            EventSystem.current.SetSelectedGameObject(null);

            if (first != null && (first.gameObject.activeInHierarchy || !activeCheck) && first.interactable)
            {
                first.Select();

                if (sendMessage)
                {
                    first.gameObject.SendMessage("Select", SendMessageOptions.DontRequireReceiver);
                }
            }
            else if (alt != null && (alt.gameObject.activeInHierarchy || !activeCheck) && alt.interactable)
            {
                alt.Select();

                if (sendMessage)
                {
                    alt.gameObject.SendMessage("Select", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        #region Input Callbacks
        public void OnNavigate(InputValue value)
        {
            Navigation = value.Get<Vector2>();

            if(device == Device.Gamepad && currentTab == OptionTab.Controls && GamepadScheme && !IsLocked && OptionsShown)
            {
                if(Navigation.x > 0.1)
                {
                    crossPlatformInput.CycleGamepadScheme(true);
                    ClearGPControls();
                    StartCoroutine(InitializeGamepadControls());
                    rebindUnsaved = true;
                }
                else if(Navigation.x < -0.1)
                {
                    crossPlatformInput.CycleGamepadScheme(false);
                    ClearGPControls();
                    StartCoroutine(InitializeGamepadControls());
                    rebindUnsaved = true;
                }
            }
        }

        public void OnNavigateTab(InputValue value)
        {
            int navigate = (int)value.Get<float>();

            if (!IsLocked && OptionsShown)
            {
                if (navigate < 0)
                {
                    int tab = currentTab > 0 ? (int)currentTab - 1 : Enum.GetValues(typeof(OptionTab)).Length - 1;
                    SelectTab(tab);
                }
                else if (navigate > 0)
                {
                    int tab = currentTab < (OptionTab)Enum.GetValues(typeof(OptionTab)).Length - 1 ? (int)currentTab + 1 : 0;
                    SelectTab(tab);
                }
            }
        }

        public void OnApply()
        {
            if (IsRebinding) return;

            if (!string.IsNullOrEmpty(SelectedPanel))
            {
                foreach (var panel in tabContents)
                {
                    if (panel.Name.Equals(SelectedPanel) && panel.Events)
                    {
                        panel.Events.OnApply?.Invoke();
                        break;
                    }
                }
            }
        }

        public void OnCancel()
        {
            if (IsRebinding) return;

            if (!string.IsNullOrEmpty(SelectedPanel))
            {
                foreach (var panel in tabContents)
                {
                    if (panel.Name.Equals(SelectedPanel) && panel.Events)
                    {
                        panel.Events.OnCancel?.Invoke();
                        break;
                    }
                }
            }
            else if(gameManager && gameManager.isPaused)
            {
                gameManager.Unpause();
            }
        }
        #endregion
    }
}