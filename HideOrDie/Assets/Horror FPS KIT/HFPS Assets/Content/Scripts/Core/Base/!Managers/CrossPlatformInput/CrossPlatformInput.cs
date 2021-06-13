/*
 * CrossPlatformInput.cs - by ThunderWire Studio
 * Requires (* Unity Input System Package)
 * Ver. 1.0 Beta (May occur bugs sometimes)
 * 
 * Bugs please report here: thunderwiregames@gmail.com
*/

using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;
using UnityEngine;
using ThunderWire.Utility;
using CPCS = CrossPlatformControlScheme;

namespace ThunderWire.CrossPlatform.Input {
    public enum ControlDevice { Keyboard, Mouse, Gamepad };
    public enum Device { Null, Keyboard, Gamepad };
    public enum Platform { None, PC, PS4, XboxOne }

    /// <summary>
    /// Main Cross-Platform Input Controller
    /// </summary>
    public class CrossPlatformInput : Singleton<CrossPlatformInput>
    {
        public const string KB_INPUTS_FILENAME = "KeyboardInputs.xml";
        public const string GP_INPUTS_FILENAME = "GamepadInputs.xml";
        const string KEYBOARD_PREFIX = "kb";
        const string MOUSE_PREFIX = "mb";
        const string GAMEPAD_PREFIX = "gp";

        XmlDocument inputsXML = new XmlDocument();

        public Device deviceType = Device.Null;
        public CPCS.SchemeType schemeType = CPCS.SchemeType.Keyboard;
        [ReadOnly] public string activeGamepadSchemeName = "Default";

        [Header("Main")]
        public CPCS controlScheme;
        public CrossPlatformSprites crossPlatformSprites;

        [Header("Cancelation")]
        public Key RebindCancelKey = Key.Escape;

        [Header("Other")]
        public bool debugMode = false;

        [HideInInspector]
        public bool inputsLoaded = false;

        [HideInInspector]
        public bool inputSuspended = false;

        private InputDevice inputDevice;
        private string folder_path;
        private string full_filepath;

        private List<CPCS.KeyboardControl> keyboardScheme = new List<CPCS.KeyboardControl>();
        private CPCS.NestedGamepadControls gamepadScheme = new CPCS.NestedGamepadControls();

        private List<ControlInstance> pressedActions = new List<ControlInstance>();
        private List<ControlInstance> pressedControls = new List<ControlInstance>();
        private string[] exceptSuspend;

        private bool rebindActive = false;
        private bool unsavedChanges = false;
        private bool error = false;

        private int activeGamepadScheme = 0;
        private int cycleSchemeID = 0;
        private RebindControl rebindControl;

        public event Action<KeyMouse> OnKeyBindPressed;
        public event Action<Device> OnInputsInitialized;
        public event Action<bool> OnRebindStatusChange;
        public event Action OnRebined;
        public event Action OnRebindCancelled;

        async void Start()
        {
            if (controlScheme != null)
            {
                deviceType = await Task.Run(() => InitializeInputDevice());
                inputsLoaded = await InitializeInputs();

                OnInputsInitialized?.Invoke(deviceType);

                if (inputsLoaded)
                {
                    if (debugMode)
                    {
                        Debug.Log("[Init] Inputs was successfully loaded.");
                        Debug.Log("[Init] Initialized Device: " + deviceType.ToString());
                    }
                }
                else
                {
                    if (debugMode)
                    {
                        Debug.LogError("[Init] Could not successfully initialize Inputs!");
                    }

                    if (deviceType == Device.Keyboard)
                    {
                        keyboardScheme = controlScheme.keyboardScheme;
                    }
                    else
                    {
                        activeGamepadScheme = controlScheme.activeGamepadScheme;
                        if (controlScheme.gamepadScheme.Count > 0)
                        {
                            gamepadScheme = controlScheme.gamepadScheme[activeGamepadScheme];
                        }
                    }

                    inputsLoaded = true;

                    Debug.Log($"[Init] Default {controlScheme.schemeType.ToString()} scheme was loaded.");
                }
            }
            else
            {
                Debug.LogError("[Cross-Platform Input] Control Scheme is not assigned!");
            }
        }

        async Task<bool> InitializeInputs()
        {
            if (deviceType == Device.Keyboard)
            {
                folder_path = Tools.GetFolderPath(FilePath.GameDataPath);
                full_filepath = folder_path + KB_INPUTS_FILENAME;
            }
            else if (deviceType == Device.Gamepad)
            {
                folder_path = Tools.GetFolderPath(FilePath.GameDataPath);
                full_filepath = folder_path + GP_INPUTS_FILENAME;
            }
            else
            {
                error = true;
                return false;
            }

            if (!Directory.Exists(folder_path) || !File.Exists(full_filepath))
            {
                activeGamepadScheme = controlScheme.activeGamepadScheme;
                cycleSchemeID = activeGamepadScheme;

                error = !await UpdateLoadedInputXML(true);

                if (!error)
                {
                    StringWriter sw = new StringWriter();
                    XmlTextWriter xw = new XmlTextWriter(sw)
                    {
                        Formatting = Formatting.Indented
                    };

                    inputsXML.WriteTo(xw);

                    await WriteXmlInputs(sw.ToString());
                }
                else
                {
                    Debug.LogError("[Fatal Error] Controls Scheme is not set properly!");
                }
            }
            else
            {
                string xml = string.Empty;

                using (StreamReader sr = new StreamReader(full_filepath))
                {
                    xml = await sr.ReadToEndAsync();
                }

                try
                {
                    inputsXML.LoadXml(xml);
                    string scheme = inputsXML.DocumentElement.Attributes["scheme"].Value;
                    schemeType = (CPCS.SchemeType)Enum.Parse(typeof(CPCS.SchemeType), scheme);

                    if (schemeType == CPCS.SchemeType.Gamepad)
                    {
                        activeGamepadScheme = int.Parse(inputsXML.DocumentElement.Attributes["index"].Value);
                        cycleSchemeID = activeGamepadScheme;
                        string sname = controlScheme.gamepadScheme[activeGamepadScheme].schemeName;
                        gamepadScheme.schemeName = sname;
                        activeGamepadSchemeName = sname;
                    }
                }
                catch
                {
                    error = true;
                    return false;
                }

                if ((deviceType == Device.Keyboard && schemeType == CPCS.SchemeType.Keyboard) || (deviceType == Device.Gamepad && schemeType == CPCS.SchemeType.Gamepad))
                {
                    List<Task<object>> tasks = new List<Task<object>>();

                    foreach (XmlNode node in inputsXML.DocumentElement.ChildNodes)
                    {
                        tasks.Add(Task.Run(() => InitializeXMLNode(node, schemeType)));
                    }

                    var results = await Task.WhenAll(tasks);
                    int controlID = 0;

                    if (!error)
                    {
                        foreach (var item in results)
                        {
                            if (schemeType == CPCS.SchemeType.Keyboard && item is CPCS.KeyboardControl kb_action)
                            {
                                keyboardScheme.Add(kb_action);
                            }
                            else if (item is CPCS.GamepadControl gp_action)
                            {
                                float scheme_scaleFactor = controlScheme.gamepadScheme[activeGamepadScheme].gamepadControls[controlID].ScaleFactor;
                                var action = gp_action;
                                action.ScaleFactor = scheme_scaleFactor;
                                controlID++;

                                gamepadScheme.gamepadControls.Add(action);
                            }
                            else
                            {
                                Debug.LogError("[Control Initialization] Unsupported control type " + item.GetType().Name);
                            }
                        }
                    }
                }
                else
                {
                    if (deviceType == Device.Keyboard)
                    {
                        keyboardScheme = controlScheme.keyboardScheme;
                    }
                    else
                    {
                        activeGamepadScheme = controlScheme.activeGamepadScheme;
                        gamepadScheme = controlScheme.gamepadScheme[activeGamepadScheme];
                    }
                }
            }

            if (error)
            {
                return false;
            }

            return true;
        }

        Task<bool> UpdateLoadedInputXML(bool initialLoad = false)
        {
            inputsXML = new XmlDocument();
            XmlNode rootNode = inputsXML.CreateElement("UserInputs");
            XmlAttribute attr_scheme = inputsXML.CreateAttribute("scheme");
            attr_scheme.Value = deviceType == Device.Keyboard ? CPCS.SchemeType.Keyboard.ToString() : CPCS.SchemeType.Gamepad.ToString();
            rootNode.Attributes.Append(attr_scheme);
            inputsXML.AppendChild(rootNode);

            if (deviceType == Device.Keyboard)
            {
                if (initialLoad)
                {
                    if (controlScheme.keyboardScheme.Count > 0)
                    {
                        keyboardScheme = controlScheme.keyboardScheme;
                    }
                    else
                    {
                        return Task.FromResult(false);
                    }
                }

                schemeType = CPCS.SchemeType.Keyboard;

                foreach (var scheme in keyboardScheme)
                {
                    if (string.IsNullOrEmpty(scheme.ActionName))
                    {
                        return Task.FromResult(false);
                    }

                    XmlNode action = inputsXML.CreateElement("Action");
                    XmlNode binding = inputsXML.CreateElement("Binding");
                    XmlAttribute attr_name = inputsXML.CreateAttribute("name");
                    XmlAttribute attr_type = inputsXML.CreateAttribute("type");

                    attr_name.Value = scheme.ActionName;
                    attr_type.Value = ((int)scheme.Output).ToString();

                    action.Attributes.Append(attr_name);
                    action.Attributes.Append(attr_type);

                    if (scheme.Output == CPCS.OutputButtonType.Bool)
                    {
                        XmlAttribute attr_input = inputsXML.CreateAttribute("key");

                        if (!IsMouseControl(scheme.ButtonBinding.KeyMouseKey))
                        {
                            attr_input.Value = KEYBOARD_PREFIX + "." + scheme.ButtonBinding.KeyMouseKey.ToString();
                        }
                        else
                        {
                            attr_input.Value = MOUSE_PREFIX + "." + scheme.ButtonBinding.KeyMouseKey.ToString();
                        }

                        binding.Attributes.Append(attr_input);
                    }
                    else
                    {
                        XmlNode bindingUp = inputsXML.CreateElement("Up");
                        XmlAttribute up_input = inputsXML.CreateAttribute("key");
                        up_input.Value = KEYBOARD_PREFIX + "." + scheme.VectorBinding.UpKey.ToString();
                        bindingUp.Attributes.Append(up_input);

                        XmlNode bindingDown = inputsXML.CreateElement("Down");
                        XmlAttribute down_input = inputsXML.CreateAttribute("key");
                        down_input.Value = KEYBOARD_PREFIX + "." + scheme.VectorBinding.DownKey.ToString();
                        bindingDown.Attributes.Append(down_input);

                        XmlNode bindingLeft = inputsXML.CreateElement("Left");
                        XmlAttribute left_input = inputsXML.CreateAttribute("key");
                        left_input.Value = KEYBOARD_PREFIX + "." + scheme.VectorBinding.LeftKey.ToString();
                        bindingLeft.Attributes.Append(left_input);

                        XmlNode bindingRight = inputsXML.CreateElement("Right");
                        XmlAttribute right_input = inputsXML.CreateAttribute("key");
                        right_input.Value = KEYBOARD_PREFIX + "." + scheme.VectorBinding.RightKey.ToString();
                        bindingRight.Attributes.Append(right_input);

                        binding.AppendChild(bindingUp);
                        binding.AppendChild(bindingDown);
                        binding.AppendChild(bindingLeft);
                        binding.AppendChild(bindingRight);
                    }

                    action.AppendChild(binding);
                    rootNode.AppendChild(action);
                }

                return Task.FromResult(true);
            }
            else if (deviceType == Device.Gamepad)
            {
                if (controlScheme.gamepadScheme.Count < 0 || controlScheme.gamepadScheme.Count < activeGamepadScheme) return Task.FromResult(false);
                if (controlScheme.gamepadScheme[activeGamepadScheme].gamepadControls.Count < 0) return Task.FromResult(false);

                gamepadScheme = controlScheme.gamepadScheme[activeGamepadScheme];
                string sname = controlScheme.gamepadScheme[activeGamepadScheme].schemeName;
                gamepadScheme.schemeName = sname;
                activeGamepadSchemeName = sname;
                schemeType = CPCS.SchemeType.Gamepad;

                XmlAttribute attr_scheme_id = inputsXML.CreateAttribute("index");
                attr_scheme_id.Value = activeGamepadScheme.ToString();
                rootNode.Attributes.Append(attr_scheme_id);

                foreach (var scheme in gamepadScheme.gamepadControls)
                {
                    if (string.IsNullOrEmpty(scheme.ActionName))
                    {
                        return Task.FromResult(false);
                    }

                    XmlNode action = inputsXML.CreateElement("Action");
                    XmlNode binding = inputsXML.CreateElement("Binding");
                    XmlAttribute attr_name = inputsXML.CreateAttribute("name");
                    XmlAttribute attr_type = inputsXML.CreateAttribute("type");
                    XmlAttribute attr_input = inputsXML.CreateAttribute("control");

                    attr_name.Value = scheme.ActionName;
                    attr_type.Value = ((int)scheme.Output).ToString();
                    attr_input.Value = GAMEPAD_PREFIX + "." + scheme.GamepadButton.ToString();

                    action.Attributes.Append(attr_name);
                    action.Attributes.Append(attr_type);
                    binding.Attributes.Append(attr_input);

                    action.AppendChild(binding);
                    rootNode.AppendChild(action);
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        object InitializeXMLNode(XmlNode action, CPCS.SchemeType scheme)
        {
            try
            {
                string name = action.Attributes["name"].Value;
                var type = (CPCS.OutputButtonType)Enum.Parse(typeof(CPCS.OutputButtonType), action.Attributes["type"].Value);

                if (scheme == CPCS.SchemeType.Keyboard)
                {
                    if (type == CPCS.OutputButtonType.Bool)
                    {
                        XmlNode binding = action.FirstChild;
                        string[] keySplit = binding.Attributes["key"].Value.Split('.');

                        if (keySplit[0].Equals(KEYBOARD_PREFIX) || keySplit[0].Equals(MOUSE_PREFIX))
                        {
                            if (Enum.TryParse(keySplit[1], out KeyMouse key))
                            {
                                return new CPCS.KeyboardControl()
                                {
                                    ActionName = name,
                                    Output = type,
                                    ButtonBinding = new CPCS.OneButtonBinding(key)
                                };
                            }
                            else
                            {
                                Debug.LogError("[Init] Could not parse " + key);
                                error = true;
                            }
                        }
                        else
                        {
                            Debug.LogError("[Init] Key prefix has wrong format!");
                            error = true;
                        }
                    }
                    else
                    {
                        XmlNode binding = action.FirstChild;
                        KeyMouse[] keys = new KeyMouse[4];

                        foreach (XmlNode vector in binding.ChildNodes)
                        {
                            string[] keySplit = vector.Attributes["key"].Value.Split('.');

                            if (keySplit[0].Equals(KEYBOARD_PREFIX) || keySplit[0].Equals(MOUSE_PREFIX))
                            {
                                if (Enum.TryParse(keySplit[1], out KeyMouse key))
                                {
                                    if (vector.Name.Equals("Up"))
                                    {
                                        keys[0] = key;
                                    }
                                    else if (vector.Name.Equals("Down"))
                                    {
                                        keys[1] = key;
                                    }
                                    else if (vector.Name.Equals("Left"))
                                    {
                                        keys[2] = key;
                                    }
                                    else if (vector.Name.Equals("Right"))
                                    {
                                        keys[3] = key;
                                    }
                                }
                                else
                                {
                                    Debug.LogError("[Init] Could not parse " + key);
                                    error = true;
                                    break;
                                }
                            }
                            else
                            {
                                Debug.LogError("[Init] Key prefix has wrong format!");
                                error = true;
                                break;
                            }
                        }

                        if (keys.Any(x => x != KeyMouse.None))
                        {
                            return new CPCS.KeyboardControl()
                            {
                                ActionName = name,
                                Output = type,
                                VectorBinding = new CPCS.Vector2Binding(keys[0], keys[1], keys[2], keys[3])
                            };
                        }
                    }
                }
                else
                {
                    XmlNode binding = action.FirstChild;
                    string[] keySplit = binding.Attributes["control"].Value.Split('.');

                    if (keySplit[0].Equals(GAMEPAD_PREFIX))
                    {
                        if (Enum.TryParse(keySplit[1], out CrossGamepadControl control))
                        {
                            return new CPCS.GamepadControl()
                            {
                                ActionName = name,
                                Output = type,
                                GamepadButton = control
                            };
                        }
                        else
                        {
                            Debug.LogError("[Init] Could not parse " + control);
                            error = true;
                        }
                    }
                    else
                    {
                        Debug.LogError("[Init] Gamepad Button prefix has wrong format!");
                        error = true;
                    }
                }
            }
            catch
            {
                error = true;
            }

            return null;
        }

        Device InitializeInputDevice()
        {          
            if (Gamepad.current != null)
            {
                inputDevice = Gamepad.current;
                return Device.Gamepad;
            }
            else if (Keyboard.current != null)
            {
                inputDevice = Keyboard.current;
                return Device.Keyboard;
            }
            else
            {
                Debug.LogError("[Device Init] Input Device is not connected!");
                error = true;
            }

            return Device.Null;
        }

        async Task WriteXmlInputs(string xml)
        {
            if (!Directory.Exists(folder_path))
            {
                Directory.CreateDirectory(folder_path);
            }

            using (StreamWriter sw = new StreamWriter(full_filepath))
            {
                await sw.WriteAsync(xml);
            }

            if (debugMode)
            {
                Debug.Log("[XML] Inputs was successfully writed!");
            }
        }

        /// <summary>
        /// Suspend Input.
        /// </summary>
        public void SuspendInput(bool suspend, params string[] except)
        {
            if (suspend)
            {
                inputSuspended = true;
                exceptSuspend = except;
            }
            else if (!AnyControlPressed())
            {
                inputSuspended = false;
                exceptSuspend = null;
            }
        }

        /// <summary>
        /// Get Input result with scpecific Action Name.
        /// </summary>
        public T GetInput<T>(string ActionName) where T : struct
        {
            if (!inputsLoaded) return default;

            if (!typeof(T).Equals(typeof(bool)) && !typeof(T).Equals(typeof(Vector2)))
            {
                Debug.LogError("[Input] Cannot convert input to a type " + typeof(T).Name);
                return default;
            }

            if (inputSuspended)
            {
                if (!exceptSuspend.Any(x => x.Equals(ActionName)))
                {
                    return default;
                }
            }

            if (deviceType == Device.Keyboard)
            {
                if (keyboardScheme.Count > 0)
                {
                    if (!keyboardScheme.Any(x => x.ActionName == ActionName))
                    {
                        Debug.LogError("[Input] \"" + ActionName + "\" Action does not exist!");
                        return default;
                    }

                    if (inputDevice is Keyboard && Keyboard.current != null)
                    {
                        foreach (var scheme in keyboardScheme)
                        {
                            if (scheme.ActionName.Equals(ActionName))
                            {
                                if (scheme.Output == CPCS.OutputButtonType.Bool)
                                {
                                    if (!IsMouseControl(scheme.ButtonBinding.KeyMouseKey))
                                    {
                                        Key key = (Key)scheme.ButtonBinding.KeyMouseKey;
                                        return key != Key.None ? (T)(object)(inputDevice as Keyboard)[key].isPressed : (T)(object)false;
                                    }
                                    else
                                    {
                                        return (T)(object)GetMouseInput(scheme.ButtonBinding.KeyMouseKey);
                                    }
                                }
                                else
                                {
                                    Vector2 input = Vector2.zero;
                                    ControlDirection directions = new ControlDirection();

                                    if (!IsMouseControl(scheme.VectorBinding.UpKey))
                                    {
                                        Key key = (Key)scheme.VectorBinding.UpKey;
                                        directions.Up = key != Key.None ? (inputDevice as Keyboard)[key].isPressed : false;
                                    }
                                    else
                                    {
                                        directions.Up = GetMouseInput(scheme.VectorBinding.UpKey);
                                    }

                                    if (!IsMouseControl(scheme.VectorBinding.DownKey))
                                    {
                                        Key key = (Key)scheme.VectorBinding.DownKey;
                                        directions.Down = key != Key.None ? (inputDevice as Keyboard)[key].isPressed : false;
                                    }
                                    else
                                    {
                                        directions.Down = GetMouseInput(scheme.VectorBinding.DownKey);
                                    }

                                    if (!IsMouseControl(scheme.VectorBinding.LeftKey))
                                    {
                                        Key key = (Key)scheme.VectorBinding.LeftKey;
                                        directions.Left = key != Key.None ? (inputDevice as Keyboard)[key].isPressed : false;
                                    }
                                    else
                                    {
                                        directions.Left = GetMouseInput(scheme.VectorBinding.LeftKey);
                                    }

                                    if (!IsMouseControl(scheme.VectorBinding.RightKey))
                                    {
                                        Key key = (Key)scheme.VectorBinding.RightKey;
                                        directions.Right = key != Key.None ? (inputDevice as Keyboard)[key].isPressed : false;
                                    }
                                    else
                                    {
                                        directions.Right = GetMouseInput(scheme.VectorBinding.RightKey);
                                    }

                                    input.y = directions.Up ? 1 : directions.Down ? -1 : 0;
                                    input.x = directions.Right ? 1 : directions.Left ? -1 : 0;

                                    return (T)(object)input;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (gamepadScheme.gamepadControls.Count > 0)
                {
                    if (!gamepadScheme.gamepadControls.Any(x => x.ActionName == ActionName))
                    {
                        Debug.LogError("[Input] \"" + ActionName + "\" Action does not exist!");
                        return default;
                    }

                    if (inputDevice is Gamepad && Gamepad.current != null)
                    {
                        foreach (var control in gamepadScheme.gamepadControls)
                        {
                            if (control.ActionName.Equals(ActionName))
                            {
                                if (control.Output == CPCS.OutputButtonType.Bool)
                                {
                                    if (control.GamepadButton != CrossGamepadControl.PS4TouchpadButton)
                                    {
                                        return (T)(object)(inputDevice as Gamepad)[ToDefaultButton(control.GamepadButton)].isPressed;
                                    }
                                    else if (DualShockGamepad.current != null)
                                    {
                                        return (T)(object)DualShockGamepad.current.touchpadButton.isPressed;
                                    }
                                    else
                                    {
                                        Debug.LogError("[Device Init] DualShock Gamepad is not connected!");
                                    }
                                }
                                else if (control.GamepadButton == CrossGamepadControl.LeftStick || control.GamepadButton == CrossGamepadControl.RightStick)
                                {
                                    if (control.GamepadButton == CrossGamepadControl.LeftStick)
                                    {
                                        Vector2 value = (inputDevice as Gamepad).leftStick.ReadValue();

                                        if(Math.Abs(control.ScaleFactor) > 0)
                                        {
                                            value *= control.ScaleFactor;
                                        }

                                        return (T)(object)value;
                                    }
                                    else if (control.GamepadButton == CrossGamepadControl.RightStick)
                                    {
                                        Vector2 value = (inputDevice as Gamepad).rightStick.ReadValue();

                                        if (Math.Abs(control.ScaleFactor) > 0)
                                        {
                                            value *= control.ScaleFactor;
                                        }

                                        return (T)(object)value;
                                    }
                                }
                                else
                                {
                                    Debug.LogError("[Input] Action \"" + ActionName + "\" has wrong Output Type for button " + control.GamepadButton.ToString());
                                }
                            }
                        }
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Get action pressed status once.
        /// </summary>
        public bool GetActionPressedOnce(MonoBehaviour Sender, string ActionName)
        {
            if (GetInput<bool>(ActionName))
            {
                if (!pressedActions.Any(x => x.Sender.Equals(Sender) && x.Control.Equals(ActionName)))
                {
                    pressedActions.Add(new ControlInstance(Sender, ActionName));
                    return true;
                }
            }
            else if (pressedActions.Any(x => x.Sender.Equals(Sender) && x.Control.Equals(ActionName)))
            {
                for (int i = 0; i < pressedActions.Count; i++)
                {
                    ControlInstance control = pressedActions[i];

                    if (control.Sender.Equals(Sender) && control.Control.Equals(ActionName))
                    {
                        pressedActions.RemoveAt(i);
                        break;
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Get Control Pressed Status.
        /// </summary>
        public bool GetControlDown(string control)
        {
            if (!inputSuspended)
            {
                if (Enum.TryParse(control, out Key key) && deviceType == Device.Keyboard)
                {
                    if (inputDevice is Keyboard keyboard)
                    {
                        return keyboard[key].isPressed;
                    }
                }
                else if (Enum.TryParse(control, out GamepadButton gb) && deviceType == Device.Gamepad)
                {
                    if (inputDevice is Gamepad gamepad)
                    {
                        return gamepad[gb].isPressed;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get control pressed status once.
        /// </summary>
        public bool GetControlPressedOnce(MonoBehaviour Sender, string Control)
        {
            if (GetControlDown(Control))
            {
                if (!pressedControls.Any(x => x.Sender.Equals(Sender) && x.Control.Equals(Control)))
                {
                    pressedControls.Add(new ControlInstance(Sender, Control));
                    return true;
                }
            }
            else if (pressedControls.Any(x => x.Sender.Equals(Sender) && x.Control.Equals(Control)))
            {
                for (int i = 0; i < pressedControls.Count; i++)
                {
                    ControlInstance control = pressedControls[i];

                    if (control.Sender.Equals(Sender) && control.Control.Equals(Control))
                    {
                        pressedControls.RemoveAt(i);
                        break;
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Select pressed Specific Action.
        /// </summary>
        public string SelectActionSpecific(params string[] specificActions)
        {
            return specificActions.FirstOrDefault(x => GetInput<bool>(x));
        }

        /// <summary>
        /// Check if controls scheme contains a specific key.
        /// </summary>
        public bool IsControlExist(KeyMouse key)
        {
            if (!inputsLoaded) return default;

            if (deviceType == Device.Keyboard)
            {
                if (keyboardScheme.Count > 0)
                {
                    if (keyboardScheme.Any(x => x.ButtonBinding.KeyMouseKey == key))
                    {
                        return true;
                    }
                    else if (keyboardScheme.Any(x => x.VectorBinding.Equals(key)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if action contains a specific key.
        /// </summary>
        public bool IsControlExist(string ActionName, KeyMouse key)
        {
            if (!inputsLoaded) return default;

            if (deviceType == Device.Keyboard)
            {
                if (keyboardScheme.Count > 0)
                {
                    if (keyboardScheme.Any(x => x.ActionName.Equals(ActionName) && x.ButtonBinding.KeyMouseKey == key))
                    {
                        return true;
                    }
                    else if (keyboardScheme.Any(x => x.ActionName.Equals(ActionName) && x.VectorBinding.Equals(key)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if controls scheme contains a specific action.
        /// </summary>
        public bool ActionExist(string ActionName)
        {
            if (!inputsLoaded) return default;

            if (deviceType == Device.Keyboard)
            {
                if (keyboardScheme.Count > 0)
                {
                    if (keyboardScheme.Any(x => x.ActionName.Equals(ActionName)))
                    {
                        return true;
                    }
                }
            }
            else if (deviceType == Device.Gamepad)
            {
                if (gamepadScheme.gamepadControls.Count > 0)
                {
                    if (gamepadScheme.gamepadControls.Any(x => x.ActionName.Equals(ActionName)))
                    {
                        return true;
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Get Mouse Axis value.
        /// </summary>
        public Vector2 GetMouseDelta()
        {
            if (Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                delta *= 0.5f;
                delta *= 0.1f;
                return delta;
            }
            else
            {
                Debug.LogError("[Mouse Init] Mouse is not connected!");
            }

            return default;
        }

        /// <summary>
        /// Get Mouse ScrollWheel value.
        /// </summary>
        public Vector2 GetMouseScroll()
        {
            if (Mouse.current != null)
            {
                Vector2 scroll = Mouse.current.scroll.ReadValue() * 0.001f;
                return scroll;
            }
            else
            {
                Debug.LogError("[Mouse Init] Mouse is not connected!");
            }

            return default;
        }

        public Vector2 GetMousePosition()
        {
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
            else
            {
                Debug.LogError("[Mouse Init] Mouse is not connected!");
            }

            return default;
        }

        /// <summary>
        /// Start keyboard action control rebind process.
        /// </summary>
        public void StartKeyRebind(string ActionName, KeyMouse ToRebind)
        {
            if (deviceType == Device.Keyboard && inputDevice is Keyboard && Keyboard.current != null)
            {
                if (!keyboardScheme.Any(x => x.ActionName == ActionName))
                {
                    Debug.LogError("[Keyboard Rebind] \"" + ActionName + "\" Action does not exist!");
                    return;
                }

                if (IsControlExist(ActionName, ToRebind))
                {
                    CPCS.KeyboardControl action = keyboardScheme.Where(x => x.ActionName.Equals(ActionName)).FirstOrDefault();

                    rebindControl = new RebindControl()
                    {
                        output = action.Output,
                        action = action.ActionName,
                        control = ToRebind
                    };

                    StartCoroutine(ControlUpdate());
                    OnRebindStatusChange?.Invoke(true);
                    rebindActive = true;
                }

                if (!rebindActive)
                {
                    Debug.LogError("[Control Rebind] Could not parse control \"" + ToRebind.ToString() + "\" by " + schemeType.ToString() + " scheme!");
                }
            }
            else
            {
                Debug.LogError($"[Control Rebind] Cannot perform interactive rebind while, Keyboard is disconnected!");
                return;
            }

            if (!rebindActive)
            {
                Debug.LogError("[Control Rebind] Given Action or Control does not belongs to any stored Action!");
            }
            else
            {
                Debug.Log("[Control Rebind] Press button to rebind.");
            }
        }

        IEnumerator ControlUpdate()
        {
            KeyMouse key = KeyMouse.None;
            yield return new WaitUntil(() => !AnyControlPressed());
            yield return new WaitUntil(() => (key = GetPressedKey()) != KeyMouse.None);

            if ((int)key == (int)RebindCancelKey || key == rebindControl.control)
            {
                yield return new WaitUntil(() => !AnyControlPressed());
                OnRebindStatusChange?.Invoke(false);
                rebindActive = false;
                OnRebindCancelled?.Invoke();

                if (debugMode)
                {
                    Debug.Log("[Control Rebind] Cancelled!");
                }

                yield break;
            }

            if (debugMode)
            {
                Debug.Log("[Control Rebind] Key " + key.ToString() + " pressed, waiting to accept.");
            }

            OnKeyBindPressed?.Invoke(key);
        }

        /// <summary>
        /// Function to accept rebind key.
        /// </summary>
        public void AcceptRebind(KeyMouse key)
        {
            foreach (var control in keyboardScheme)
            {
                if (control.Equals(key))
                {
                    if (control.Output == CPCS.OutputButtonType.Bool)
                    {
                        control.ButtonBinding.KeyMouseKey = KeyMouse.None;
                    }
                    else
                    {
                        control.VectorBinding.Replace(key, KeyMouse.None);
                    }
                }

                if (control.ActionName == rebindControl.action && control.Equals(rebindControl.control))
                {
                    if (control.Output == CPCS.OutputButtonType.Bool)
                    {
                        control.ButtonBinding.KeyMouseKey = key;
                    }
                    else
                    {
                        control.VectorBinding.Replace(rebindControl.control, key);
                    }

                    break;
                }
            }

            unsavedChanges = true;

            if (debugMode)
            {
                Debug.Log($"[Control Rebind] New Control Registered: {key.ToString()} for {rebindControl.action}");
            }

            OnRebined?.Invoke();
        }


        /// <summary>
        /// Function to cancel rebind action manually.
        /// </summary>
        public void CancelRebindManually()
        {
            OnRebindStatusChange?.Invoke(false);
            rebindActive = false;

            if (debugMode)
            {
                Debug.Log("[Control Rebind] Cancelled!");
            }
        }

        /// <summary>
        /// Serialize unsaved controls changes.
        /// </summary>
        public async Task SerializeNewControls()
        {
            if (unsavedChanges)
            {
                if (deviceType == Device.Gamepad)
                {
                    activeGamepadScheme = cycleSchemeID;
                }

                inputsLoaded = false;

                if (await Task.Run(() => UpdateLoadedInputXML()))
                {
                    StringWriter sw = new StringWriter();
                    XmlTextWriter xw = new XmlTextWriter(sw)
                    {
                        Formatting = Formatting.Indented
                    };

                    inputsXML.WriteTo(xw);

                    await WriteXmlInputs(sw.ToString());

                    inputsLoaded = true;
                    unsavedChanges = false;
                }
                else if (debugMode)
                {
                    Debug.LogError("[Serialization] Unable to serialize.");
                }
            }
            else if (debugMode)
            {
                Debug.Log("[Serialization] There are no unsaved changes.");
            }
        }

        /// <summary>
        /// Cycle between Gamepad Control Schemes.
        /// </summary>
        public void CycleGamepadScheme(bool increase)
        {
            if (increase)
            {
                cycleSchemeID = cycleSchemeID < controlScheme.gamepadScheme.Count - 1 ? cycleSchemeID + 1 : 0;
            }
            else
            {
                cycleSchemeID = cycleSchemeID > 0 ? cycleSchemeID - 1 : controlScheme.gamepadScheme.Count - 1;
            }

            if (cycleSchemeID != activeGamepadScheme)
            {
                unsavedChanges = true;
            }
            else
            {
                unsavedChanges = false;
            }
        }

        /// <summary>
        /// Get Action Details.
        /// </summary>
        public CrossPlatformControl ControlOf(string ActionName)
        {
            if (deviceType == Device.Keyboard)
            {
                if (keyboardScheme.Any(x => x.ActionName == ActionName))
                {
                    CPCS.KeyboardControl control = keyboardScheme.FirstOrDefault(x => x.ActionName == ActionName);

                    if (control.Output == CPCS.OutputButtonType.Bool)
                    {
                        return new CrossPlatformControl()
                        {
                            DeviceType = IsMouseControl(control.ButtonBinding.KeyMouseKey) ? ControlDevice.Mouse : ControlDevice.Keyboard,
                            Control = control.ButtonBinding.KeyMouseKey.ToString()
                        };
                    }
                    else
                    {
                        Debug.LogError("Vector2 Keyboard Control is not supported.");
                    }
                }
                else
                {
                    Debug.LogError($"[Control Of] Could not get action \"{ActionName}\"!");
                }
            }
            else if (deviceType == Device.Gamepad)
            {
                if (gamepadScheme.gamepadControls.Any(x => x.ActionName == ActionName))
                {
                    CPCS.GamepadControl control = gamepadScheme.gamepadControls.FirstOrDefault(x => x.ActionName == ActionName);

                    return new CrossPlatformControl()
                    {
                        DeviceType = ControlDevice.Gamepad,
                        Control = control.GamepadButton.ToString()
                    };
                }
                else
                {
                    Debug.LogError($"[Control Of] Could not get action \"{ActionName}\"!");
                }
            }

            return default;
        }

        /// <summary>
        /// Get Action Keyboard Control
        /// </summary>
        public CPCS.KeyboardControl KBControlOf(string ActionName)
        {
            if (!inputsLoaded) return default;

            if (deviceType == Device.Keyboard)
            {
                if (keyboardScheme.Any(x => x.ActionName == ActionName))
                {
                    return keyboardScheme.FirstOrDefault(x => x.ActionName == ActionName);
                }
                else
                {
                    Debug.LogError($"[Keyboard Control] Could not get action \"{ActionName}\"!");
                }
            }

            return default;
        }

        /// <summary>
        /// Get all control structures from keyboard scheme.
        /// </summary>
        public List<CPCS.KeyboardControl> KeyboardAll()
        {
            if (deviceType == Device.Keyboard)
            {
                return keyboardScheme;
            }

            return default;
        }

        /// <summary>
        /// Get all control structures from gamepad scheme.
        /// </summary>
        public CPCS.NestedGamepadControls GamepadAll()
        {
            if (deviceType == Device.Gamepad)
            {
                return controlScheme.gamepadScheme[cycleSchemeID];
            }

            return default;
        }

        /// <summary>
        /// Get all pressed actions.
        /// </summary>
        public ControlInstance[] GetPressedActions()
        {
            return pressedActions.ToArray();
        }

        /// <summary>
        /// Check if action is still pressed after <see cref="GetActionPressedOnce(MonoBehaviour, string)"/> is called.
        /// </summary>
        public bool IsActionPressed(string ActionName)
        {
            return pressedActions.Any(x => x.Control.Equals(ActionName));
        }

        /// <summary>
        /// Get all pressed controls.
        /// </summary>
        public ControlInstance[] GetPressedControls()
        {
            return pressedControls.ToArray();
        }

        /// <summary>
        /// Check if control is still pressed after <see cref="GetControlPressedOnce(MonoBehaviour, string)"/> is called.
        /// </summary>
        public bool IsControlPressed(string Control)
        {
            return pressedControls.Any(x => x.Control.Equals(Control));
        }

        /// <summary>
        /// Get current platform.
        /// </summary>
        public Platform GetCurrentPlatform()
        {
            if (deviceType == Device.Keyboard)
            {
                return Platform.PC;
            }
            else if (deviceType == Device.Gamepad)
            {
                if (DualShockGamepad.current != null)
                {
                    return Platform.PS4;
                }
                else if (Gamepad.current != null)
                {
                    XInputController xInput = (XInputController)Gamepad.current;
                    if (xInput.subType == XInputController.DeviceSubType.Gamepad)
                    {
                        return Platform.XboxOne;
                    }
                }
            }

            return Platform.None;
        }

        /// <summary>
        /// Detect if any current device control is pressed.
        /// </summary>
        public bool AnyControlPressed()
        {
            if (deviceType == Device.Keyboard)
            {
                Mouse m = Mouse.current;
                bool m_pressed = m != null && (m.leftButton.isPressed || m.middleButton.isPressed || m.rightButton.isPressed);
                return (inputDevice as Keyboard).allControls.Any(x => x.IsActuated()) || m_pressed;
            }
            if (deviceType == Device.Gamepad)
            {
                GamepadButton[] gamepadButtons = (GamepadButton[])Enum.GetValues(typeof(GamepadButton));
                bool result = false;

                foreach (var button in gamepadButtons)
                {
                    if ((inputDevice as Gamepad)[button].isPressed)
                    {
                        result = true;
                        break;
                    }
                }

                return result;
            }

            return false;
        }

        /// <summary>
        /// Check if two Actions has same Controls.
        /// </summary>
        public bool IsControlsSame(string LeftAction, string RightAction)
        {
            if (ActionExist(LeftAction) && ActionExist(RightAction))
            {
                CrossPlatformControl leftStruct = ControlOf(LeftAction);
                CrossPlatformControl rightStruct = ControlOf(RightAction);
                return leftStruct.Control == rightStruct.Control;
            }
            else
            {
                Debug.LogError("[Comparsion] Any of given actions for comparsion does not exist!");
            }

            return default;
        }

        GamepadButton ToDefaultButton(CrossGamepadControl control)
        {
            switch (control)
            {
                case CrossGamepadControl.LeftTrigger:
                    return GamepadButton.LeftTrigger;
                case CrossGamepadControl.LeftShoulder:
                    return GamepadButton.LeftShoulder;
                case CrossGamepadControl.Select:
                    return GamepadButton.Select;
                case CrossGamepadControl.DpadUp:
                    return GamepadButton.DpadUp;
                case CrossGamepadControl.DpadRight:
                    return GamepadButton.DpadRight;
                case CrossGamepadControl.DpadDown:
                    return GamepadButton.DpadDown;
                case CrossGamepadControl.DpadLeft:
                    return GamepadButton.DpadLeft;
                case CrossGamepadControl.LeftStick:
                    return GamepadButton.LeftStick;
                case CrossGamepadControl.RightTrigger:
                    return GamepadButton.RightTrigger;
                case CrossGamepadControl.RightShoulder:
                    return GamepadButton.RightShoulder;
                case CrossGamepadControl.Start:
                    return GamepadButton.Start;
                case CrossGamepadControl.FaceUp:
                    return GamepadButton.North;
                case CrossGamepadControl.FaceRight:
                    return GamepadButton.East;
                case CrossGamepadControl.FaceDown:
                    return GamepadButton.South;
                case CrossGamepadControl.FaceLeft:
                    return GamepadButton.West;
                case CrossGamepadControl.RightStick:
                    return GamepadButton.RightStick;
            }

            return default;
        }

        bool GetMouseInput(KeyMouse control)
        {
            Mouse mouse = Mouse.current;

            if (mouse != null) {
                switch (control)
                {
                    case KeyMouse.MouseLeft:
                        return mouse.leftButton.isPressed;
                    case KeyMouse.MouseMiddle:
                        return mouse.middleButton.isPressed;
                    case KeyMouse.MouseRight:
                        return mouse.rightButton.isPressed;
                }
            }

            return default;
        }

        KeyMouse GetPressedKey()
        {
            Mouse mouse = Mouse.current;

            if (inputDevice is Keyboard && (inputDevice as Keyboard).anyKey.isPressed)
            {
                return (KeyMouse)(int)(inputDevice as Keyboard).allKeys.Where(x => x.isPressed).SingleOrDefault().keyCode;
            }
            else if (mouse != null)
            {
                if (mouse.leftButton.isPressed)
                {
                    return KeyMouse.MouseLeft;
                }
                else if (mouse.middleButton.isPressed)
                {
                    return KeyMouse.MouseMiddle;
                }
                else if (mouse.rightButton.isPressed)
                {
                    return KeyMouse.MouseRight;
                }
            }

            return KeyMouse.None;
        }

        bool IsMouseControl(KeyMouse key)
        {
            return key == KeyMouse.MouseLeft || key == KeyMouse.MouseMiddle || key == KeyMouse.MouseRight;
        }
    }

    [Serializable]
    public struct ControlInstance
    {
        public MonoBehaviour Sender;
        public string Control;

        public ControlInstance(MonoBehaviour sender, string control)
        {
            Sender = sender;
            Control = control;
        }
    }

    [Serializable]
    public struct CrossPlatformControl
    {
        public ControlDevice DeviceType;
        public string Control;
    }

    [Serializable]
    public struct RebindControl
    {
        public CPCS.OutputButtonType output;
        public string action;
        public KeyMouse control;
    }

    public struct ControlDirection
    {
        public bool Up;
        public bool Down;
        public bool Left;
        public bool Right;
    }
}