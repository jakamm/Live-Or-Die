using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using ThunderWire.CrossPlatform.Input;

public class CrossPlatformInputExample : MonoBehaviour
{
    private CrossPlatformInput input;

    [Header("Control Scheme Action")]
    public string ActionName;

    [Header("Control Raw")]
    public Key keyboardKey = Key.Space;
    public GamepadButton gamepadButton = GamepadButton.DpadUp;
    [ReadOnly] public string control;

    [Header("Settings")]
    public bool pressOnce;
    public bool useControlRaw;
    public bool useGamepad;

    void Awake()
    {
        input = CrossPlatformInput.Instance;
    }

    void Update()
    {
        if (input.inputsLoaded)
        {
            if (!useControlRaw)
            {
                if (input.ActionExist(ActionName))
                {
                    if (!pressOnce)
                    {
                        if (input.GetInput<bool>(ActionName))
                        {
                            Debug.Log($"Action {ActionName} is pressed!");
                        }
                    }
                    else
                    {
                        if (input.GetActionPressedOnce(this, ActionName))
                        {
                            Debug.Log($"Action {ActionName} is pressed once!");
                        }
                    }
                }
                else
                {
                    Debug.Log($"Action {ActionName} does not exist!");
                }
            }
            else
            {
                control = useGamepad ? gamepadButton.ToString() : keyboardKey.ToString();

                if (!pressOnce)
                {
                    if (input.GetControlDown(control))
                    {
                        Debug.Log($"Control {control} is pressed!");
                    }
                }
                else
                {
                    if (input.GetControlPressedOnce(this, control))
                    {
                        Debug.Log($"Control {control} is pressed once!");
                    }
                }
            }
        }
    }
}
