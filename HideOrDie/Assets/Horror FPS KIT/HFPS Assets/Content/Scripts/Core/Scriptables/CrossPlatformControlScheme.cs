using System;
using System.Collections.Generic;
using UnityEngine;
using Malee;

public class CrossPlatformControlScheme : ScriptableObject
{
    public enum SchemeType { Keyboard, Gamepad }
    public enum OutputButtonType { Bool, Vector2 }

    public SchemeType schemeType = SchemeType.Keyboard;

    #region Control Classes
    [Serializable]
    public class KeyboardControl : IEquatable<KeyMouse>
    {
        public string ActionName;
        public OutputButtonType Output;
        public OneButtonBinding ButtonBinding;
        public Vector2Binding VectorBinding;

        public bool Equals(KeyMouse other)
        {
            if (ButtonBinding.KeyMouseKey == other || VectorBinding.Equals(other))
            {
                return true;
            }

            return false;
        }
    }

    [Serializable]
    public struct OneButtonBinding
    {
        public KeyMouse KeyMouseKey;

        public OneButtonBinding(KeyMouse key)
        {
            KeyMouseKey = key;
        }
    }

    [Serializable]
    public struct Vector2Binding : IEquatable<KeyMouse>
    {
        public KeyMouse UpKey;
        public KeyMouse DownKey;
        public KeyMouse LeftKey;
        public KeyMouse RightKey;

        public Vector2Binding(KeyMouse up, KeyMouse down, KeyMouse left, KeyMouse right)
        {
            UpKey = up;
            DownKey = down;
            LeftKey = left;
            RightKey = right;
        }

        public bool Equals(KeyMouse other)
        {
            return UpKey == other || DownKey == other || LeftKey == other || RightKey == other;
        }

        public bool Replace(KeyMouse original, KeyMouse key)
        {
            if (UpKey == original)
            {
                UpKey = key;
                return true;
            }
            else if (DownKey == original)
            {
                DownKey = key;
                return true;
            }
            else if (LeftKey == original)
            {
                LeftKey = key;
                return true;
            }
            else if (RightKey == original)
            {
                RightKey = key;
                return true;
            }

            return false;
        }
    }

    [Serializable]
    public struct GamepadControl
    {
        public string ActionName;
        public OutputButtonType Output;
        public CrossGamepadControl GamepadButton;
        public float ScaleFactor;
    }

    [Serializable]
    public class ReorderableGamepadControls : ReorderableArray<GamepadControl>
    {
    }

    [Serializable]
    public class NestedGamepadControls
    {
        public string schemeName;
        [Reorderable]
        public ReorderableGamepadControls gamepadControls = new ReorderableGamepadControls();
    }
    #endregion

    #region Scheme Declaration
    public List<KeyboardControl> keyboardScheme = new List<KeyboardControl>();
    public List<NestedGamepadControls> gamepadScheme = new List<NestedGamepadControls>();
    public int activeGamepadScheme = 0;
    #endregion
}
