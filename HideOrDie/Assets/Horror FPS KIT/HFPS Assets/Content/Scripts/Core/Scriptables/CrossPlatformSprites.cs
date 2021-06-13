using System;
using UnityEngine;
using ThunderWire.CrossPlatform.Input;

public class CrossPlatformSprites : ScriptableObject
{
    [Serializable]
    public class PS4Sprites
    {
        public Sprite PS4Gamepad;
        public Sprite DpadUp;
        public Sprite DpadDown;
        public Sprite DpadLeft;
        public Sprite DpadRight;
        public Sprite Triangle;
        public Sprite Circle;
        public Sprite Cross;
        public Sprite Square;
        public Sprite LeftStick;
        public Sprite RightStick;
        public Sprite L1;
        public Sprite R1;
        public Sprite L2;
        public Sprite R2;
        public Sprite Options;
        public Sprite Share;
        public Sprite Touchpad;
    }

    [Serializable]
    public class XboxOneSprites
    {
        public Sprite XboxGamepad;
        public Sprite DpadUp;
        public Sprite DpadDown;
        public Sprite DpadLeft;
        public Sprite DpadRight;
        public Sprite Y;
        public Sprite B;
        public Sprite A;
        public Sprite X;
        public Sprite LeftStick;
        public Sprite RightStick;
        public Sprite LB;
        public Sprite RB;
        public Sprite LT;
        public Sprite RT;
        public Sprite Menu;
        public Sprite ChangeView;
    }

    [Serializable]
    public class MouseSprites
    {
        public Sprite MouseLeft;
        public Sprite MouseMiddle;
        public Sprite MouseRight;
    }

    [Serializable]
    public class KeyboardSprites
    {
        public Sprite None;

        //Digits
        public Sprite Key0;
        public Sprite Key1;
        public Sprite Key2;
        public Sprite Key3;
        public Sprite Key4;
        public Sprite Key5;
        public Sprite Key6;
        public Sprite Key7;
        public Sprite Key8;
        public Sprite Key9;

        //Alphabet
        public Sprite A;
        public Sprite B;
        public Sprite C;
        public Sprite D;
        public Sprite E;
        public Sprite F;
        public Sprite G;
        public Sprite H;
        public Sprite I;
        public Sprite J;
        public Sprite K;
        public Sprite L;
        public Sprite M;
        public Sprite N;
        public Sprite O;
        public Sprite P;
        public Sprite Q;
        public Sprite R;
        public Sprite S;
        public Sprite T;
        public Sprite U;
        public Sprite V;
        public Sprite W;
        public Sprite X;
        public Sprite Y;
        public Sprite Z;

        //Arrows
        public Sprite Arrow_UP;
        public Sprite Arrow_DOWN;
        public Sprite Arrow_LEFT;
        public Sprite Arrow_RIGHT;

        //F-Keys
        public Sprite F1;
        public Sprite F2;
        public Sprite F3;
        public Sprite F4;
        public Sprite F5;
        public Sprite F6;
        public Sprite F7;
        public Sprite F8;
        public Sprite F9;
        public Sprite F10;
        public Sprite F11;
        public Sprite F12;

        //Others
        public Sprite Esc;
        public Sprite Tab;
        public Sprite CapsLock;
        public Sprite Shift;
        public Sprite Ctrl;
        public Sprite Alt;
        public Sprite Space;
        public Sprite Enter;
        public Sprite Backspace;
        public Sprite Insert;
        public Sprite Home;
        public Sprite PGUP;
        public Sprite Delete;
        public Sprite End;
        public Sprite PGDN;
        public Sprite Asterisk;
        public Sprite Semicolon;
        public Sprite Bracket_L;
        public Sprite Bracket_R;
        public Sprite Quote;
        public Sprite Comma;
        public Sprite Period;
        public Sprite Backslash;
        public Sprite EqualsKey;
        public Sprite Backquote;
        public Sprite Context;

        //Numpad
        public Sprite NumpadSlash;
        public Sprite NumpadMinus;
        public Sprite NumpadPlus;
        public Sprite NumpadEnter;
        public Sprite NumpadPeriod;
    }

    public PS4Sprites PS4;
    public XboxOneSprites XboxOne;
    public MouseSprites Mouse;
    public KeyboardSprites Keyboard;

    /// <summary>
    /// Get Control Sprite for Specific Platform
    /// </summary>
    public Sprite GetSprite(string control, Platform platform)
    {
        if (platform != Platform.PC)
        {
            if (Enum.TryParse(control, out CrossGamepadControl c))
            {
                return GetSprite(c, platform);
            }
        }
        else if (platform == Platform.PC && Enum.TryParse(control, out KeyMouse k))
        {
            return GetKeyboardSprite(k);
        }

        return default;
    }

    /// <summary>
    /// Get Control Sprite for Specific Console Platform
    /// </summary>
    public Sprite GetSprite(CrossGamepadControl control, Platform platform)
    {
        if(platform == Platform.PS4)
        {
            switch (control)
            {
                case CrossGamepadControl.DpadUp:
                    return PS4.DpadUp;
                case CrossGamepadControl.DpadDown:
                    return PS4.DpadDown;
                case CrossGamepadControl.DpadLeft:
                    return PS4.DpadLeft;
                case CrossGamepadControl.DpadRight:
                    return PS4.DpadRight;
                case CrossGamepadControl.FaceUp:
                    return PS4.Triangle;
                case CrossGamepadControl.FaceRight:
                    return PS4.Circle;
                case CrossGamepadControl.FaceDown:
                    return PS4.Cross;
                case CrossGamepadControl.FaceLeft:
                    return PS4.Square;
                case CrossGamepadControl.LeftStick:
                    return PS4.LeftStick;
                case CrossGamepadControl.RightStick:
                    return PS4.RightStick;
                case CrossGamepadControl.LeftShoulder:
                    return PS4.L1;
                case CrossGamepadControl.RightShoulder:
                    return PS4.R1;
                case CrossGamepadControl.LeftTrigger:
                    return PS4.L2;
                case CrossGamepadControl.RightTrigger:
                    return PS4.R2;
                case CrossGamepadControl.Start:
                    return PS4.Options;
                case CrossGamepadControl.Select:
                    return PS4.Share;
                case CrossGamepadControl.PS4TouchpadButton:
                    return PS4.Touchpad;
            }
        }
        else if (platform == Platform.XboxOne)
        {
            switch (control)
            {
                case CrossGamepadControl.DpadUp:
                    return XboxOne.DpadUp;
                case CrossGamepadControl.DpadDown:
                    return XboxOne.DpadDown;
                case CrossGamepadControl.DpadLeft:
                    return XboxOne.DpadLeft;
                case CrossGamepadControl.DpadRight:
                    return XboxOne.DpadRight;
                case CrossGamepadControl.FaceUp:
                    return XboxOne.Y;
                case CrossGamepadControl.FaceRight:
                    return XboxOne.B;
                case CrossGamepadControl.FaceDown:
                    return XboxOne.A;
                case CrossGamepadControl.FaceLeft:
                    return XboxOne.X;
                case CrossGamepadControl.LeftStick:
                    return XboxOne.LeftStick;
                case CrossGamepadControl.RightStick:
                    return XboxOne.RightStick;
                case CrossGamepadControl.LeftShoulder:
                    return XboxOne.LB;
                case CrossGamepadControl.RightShoulder:
                    return XboxOne.RB;
                case CrossGamepadControl.LeftTrigger:
                    return XboxOne.LT;
                case CrossGamepadControl.RightTrigger:
                    return XboxOne.RT;
                case CrossGamepadControl.Start:
                    return XboxOne.Menu;
                case CrossGamepadControl.Select:
                    return XboxOne.ChangeView;
                case CrossGamepadControl.PS4TouchpadButton:
                    return PS4.Touchpad;
            }
        }

        return default;
    }

    /// <summary>
    /// Get Control Sprite for Mouse
    /// </summary>
    public Sprite GetMouseSprite(KeyMouse control)
    {
        switch (control)
        {
            case KeyMouse.MouseLeft:
                return Mouse.MouseLeft;
            case KeyMouse.MouseMiddle:
                return Mouse.MouseMiddle;
            case KeyMouse.MouseRight:
                return Mouse.MouseRight;
            default:
                break;
        }

        return default;
    }

    /// <summary>
    /// Get Control Sprite for Keyboard
    /// </summary>
    public Sprite GetKeyboardSprite(KeyMouse control)
    {
        switch (control)
        {
            case KeyMouse.Digit0:
                return Keyboard.Key0;
            case KeyMouse.Digit1:
                return Keyboard.Key1;
            case KeyMouse.Digit2:
                return Keyboard.Key2;
            case KeyMouse.Digit3:
                return Keyboard.Key3;
            case KeyMouse.Digit4:
                return Keyboard.Key4;
            case KeyMouse.Digit5:
                return Keyboard.Key5;
            case KeyMouse.Digit6:
                return Keyboard.Key6;
            case KeyMouse.Digit7:
                return Keyboard.Key7;
            case KeyMouse.Digit8:
                return Keyboard.Key8;
            case KeyMouse.Digit9:
                return Keyboard.Key9;
            case KeyMouse.Numpad0:
                return Keyboard.Key0;
            case KeyMouse.Numpad1:
                return Keyboard.Key1;
            case KeyMouse.Numpad2:
                return Keyboard.Key2;
            case KeyMouse.Numpad3:
                return Keyboard.Key3;
            case KeyMouse.Numpad4:
                return Keyboard.Key4;
            case KeyMouse.Numpad5:
                return Keyboard.Key5;
            case KeyMouse.Numpad6:
                return Keyboard.Key6;
            case KeyMouse.Numpad7:
                return Keyboard.Key7;
            case KeyMouse.Numpad8:
                return Keyboard.Key8;
            case KeyMouse.Numpad9:
                return Keyboard.Key9;
            case KeyMouse.A:
                return Keyboard.A;
            case KeyMouse.B:
                return Keyboard.B;
            case KeyMouse.C:
                return Keyboard.C;
            case KeyMouse.D:
                return Keyboard.D;
            case KeyMouse.E:
                return Keyboard.E;
            case KeyMouse.F:
                return Keyboard.F;
            case KeyMouse.G:
                return Keyboard.G;
            case KeyMouse.H:
                return Keyboard.H;
            case KeyMouse.I:
                return Keyboard.I;
            case KeyMouse.J:
                return Keyboard.J;
            case KeyMouse.K:
                return Keyboard.K;
            case KeyMouse.L:
                return Keyboard.L;
            case KeyMouse.M:
                return Keyboard.M;
            case KeyMouse.N:
                return Keyboard.N;
            case KeyMouse.O:
                return Keyboard.O;
            case KeyMouse.P:
                return Keyboard.P;
            case KeyMouse.Q:
                return Keyboard.Q;
            case KeyMouse.R:
                return Keyboard.R;
            case KeyMouse.S:
                return Keyboard.S;
            case KeyMouse.T:
                return Keyboard.T;
            case KeyMouse.U:
                return Keyboard.U;
            case KeyMouse.V:
                return Keyboard.V;
            case KeyMouse.W:
                return Keyboard.W;
            case KeyMouse.X:
                return Keyboard.X;
            case KeyMouse.Y:
                return Keyboard.Y;
            case KeyMouse.Z:
                return Keyboard.Z;
            case KeyMouse.LeftArrow:
                return Keyboard.Arrow_LEFT;
            case KeyMouse.RightArrow:
                return Keyboard.Arrow_RIGHT;
            case KeyMouse.UpArrow:
                return Keyboard.Arrow_UP;
            case KeyMouse.DownArrow:
                return Keyboard.Arrow_DOWN;
            case KeyMouse.F1:
                return Keyboard.F1;
            case KeyMouse.F2:
                return Keyboard.F2;
            case KeyMouse.F3:
                return Keyboard.F3;
            case KeyMouse.F4:
                return Keyboard.F4;
            case KeyMouse.F5:
                return Keyboard.F5;
            case KeyMouse.F6:
                return Keyboard.F6;
            case KeyMouse.F7:
                return Keyboard.F7;
            case KeyMouse.F8:
                return Keyboard.F8;
            case KeyMouse.F9:
                return Keyboard.F9;
            case KeyMouse.F10:
                return Keyboard.F10;
            case KeyMouse.F11:
                return Keyboard.F11;
            case KeyMouse.F12:
                return Keyboard.F12;
            case KeyMouse.Escape:
                return Keyboard.Esc;
            case KeyMouse.Tab:
                return Keyboard.Tab;
            case KeyMouse.CapsLock:
                return Keyboard.CapsLock;
            case KeyMouse.LeftShift:
                return Keyboard.Shift;
            case KeyMouse.RightShift:
                return Keyboard.Shift;
            case KeyMouse.LeftCtrl:
                return Keyboard.Ctrl;
            case KeyMouse.RightCtrl:
                return Keyboard.Ctrl;
            case KeyMouse.LeftAlt:
                return Keyboard.Alt;
            case KeyMouse.RightAlt:
                return Keyboard.Alt;
            case KeyMouse.Space:
                return Keyboard.Space;
            case KeyMouse.Enter:
                return Keyboard.Enter;
            case KeyMouse.Backspace:
                return Keyboard.Backspace;
            case KeyMouse.Insert:
                return Keyboard.Insert;
            case KeyMouse.Home:
                return Keyboard.Home;
            case KeyMouse.PageUp:
                return Keyboard.PGUP;
            case KeyMouse.Delete:
                return Keyboard.Delete;
            case KeyMouse.End:
                return Keyboard.End;
            case KeyMouse.PageDown:
                return Keyboard.PGDN;
            case KeyMouse.Minus:
            case KeyMouse.NumpadMinus:
                return Keyboard.NumpadMinus;
            case KeyMouse.Semicolon:
                return Keyboard.Semicolon;
            case KeyMouse.Quote:
                return Keyboard.Quote;
            case KeyMouse.Slash:
                return Keyboard.NumpadSlash;
            case KeyMouse.LeftBracket:
                return Keyboard.Bracket_L;
            case KeyMouse.RightBracket:
                return Keyboard.Bracket_R;
            case KeyMouse.Comma:
                return Keyboard.Comma;
            case KeyMouse.Period:
                return Keyboard.Period;
            case KeyMouse.Backslash:
                return Keyboard.Backslash;
            case KeyMouse.Equals:
            case KeyMouse.NumpadEquals:
                return Keyboard.EqualsKey;
            case KeyMouse.NumpadDivide:
                return Keyboard.NumpadSlash;
            case KeyMouse.NumpadMultiply:
                return Keyboard.Asterisk;
            case KeyMouse.NumpadPlus:
                return Keyboard.NumpadPlus;
            case KeyMouse.NumpadEnter:
                return Keyboard.NumpadEnter;
            case KeyMouse.NumpadPeriod:
                return Keyboard.NumpadPeriod;
            case KeyMouse.Backquote:
                return Keyboard.Backquote;
            case KeyMouse.ContextMenu:
                return Keyboard.Context;
            default:
                return Keyboard.None;
        }
    }

    /// <summary>
    /// Get Control Sprite for Mouse
    /// </summary>
    public Sprite GetMouseSprite(string control)
    {
        if(Enum.TryParse(control, out KeyMouse mouse))
        {
            return GetMouseSprite(mouse);
        }

        return default;
    }
}
