// UnityWebBrowser (UWB)
// Copyright (c) 2021-2022 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

using System;
using UnityEngine;
using VoltstroStudios.UnityWebBrowser.Shared;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VoltstroStudios.UnityWebBrowser.Input
{
    /// <summary>
    ///     Helper class for inputs
    /// </summary>
    public static class WebBrowserInputHelper
    {
        /// <summary>
        ///     Converts a <see cref="KeyCode" /> to <see cref="WindowsKey" />
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static WindowsKey UnityKeyCodeToWindowKey(this KeyCode key)
        {
            switch (key)
            {
                case KeyCode.None:
                    return WindowsKey.None;
                case KeyCode.Backspace:
                    return WindowsKey.Back;
                case KeyCode.Delete:
                    return WindowsKey.Delete;
                case KeyCode.Tab:
                    return WindowsKey.Tab;
                case KeyCode.Clear:
                    return WindowsKey.Clear;
                case KeyCode.Return:
                    return WindowsKey.Return;
                case KeyCode.Pause:
                    return WindowsKey.Pause;
                case KeyCode.Escape:
                    return WindowsKey.Escape;
                case KeyCode.Space:
                    return WindowsKey.Space;
                case KeyCode.KeypadPeriod:
                    return WindowsKey.OemPeriod;
                case KeyCode.KeypadDivide:
                    return WindowsKey.Divide;
                case KeyCode.KeypadMultiply:
                    return WindowsKey.Multiply;
                case KeyCode.KeypadMinus:
                    return WindowsKey.OemMinus;
                case KeyCode.KeypadPlus:
                    return WindowsKey.Oemplus;
                case KeyCode.KeypadEnter:
                    return WindowsKey.Enter;
                case KeyCode.UpArrow:
                    return WindowsKey.Up;
                case KeyCode.DownArrow:
                    return WindowsKey.Down;
                case KeyCode.RightArrow:
                    return WindowsKey.Right;
                case KeyCode.LeftArrow:
                    return WindowsKey.Left;
                case KeyCode.Insert:
                    return WindowsKey.Insert;
                case KeyCode.Home:
                    return WindowsKey.Home;
                case KeyCode.End:
                    return WindowsKey.End;
                case KeyCode.PageUp:
                    return WindowsKey.PageUp;
                case KeyCode.PageDown:
                    return WindowsKey.PageDown;
                case KeyCode.F1:
                    return WindowsKey.F1;
                case KeyCode.F2:
                    return WindowsKey.F2;
                case KeyCode.F3:
                    return WindowsKey.F3;
                case KeyCode.F4:
                    return WindowsKey.F4;
                case KeyCode.F5:
                    return WindowsKey.F5;
                case KeyCode.F6:
                    return WindowsKey.F6;
                case KeyCode.F7:
                    return WindowsKey.F7;
                case KeyCode.F8:
                    return WindowsKey.F8;
                case KeyCode.F9:
                    return WindowsKey.F9;
                case KeyCode.F10:
                    return WindowsKey.F10;
                case KeyCode.F11:
                    return WindowsKey.F11;
                case KeyCode.F12:
                    return WindowsKey.F12;
                case KeyCode.F13:
                    return WindowsKey.F13;
                case KeyCode.F14:
                    return WindowsKey.F14;
                case KeyCode.F15:
                    return WindowsKey.F15;
                case KeyCode.DoubleQuote:
                    return WindowsKey.OemQuotes;
                case KeyCode.Quote:
                    return WindowsKey.OemQuotes;
                case KeyCode.LeftParen:
                    return WindowsKey.OemOpenBrackets;
                case KeyCode.RightParen:
                    return WindowsKey.OemCloseBrackets;
                case KeyCode.Plus:
                    return WindowsKey.Oemplus;
                case KeyCode.Comma:
                    return WindowsKey.Oemcomma;
                case KeyCode.Minus:
                    return WindowsKey.OemMinus;
                case KeyCode.Period:
                    return WindowsKey.OemPeriod;
                case KeyCode.Semicolon:
                    return WindowsKey.OemSemicolon;
                case KeyCode.RightShift:
                    return WindowsKey.RShiftKey;
                case KeyCode.LeftShift:
                    return WindowsKey.LShiftKey;
                case KeyCode.RightControl:
                    return WindowsKey.RControlKey;
                case KeyCode.LeftControl:
                    return WindowsKey.LControlKey;
                case KeyCode.RightAlt:
                    return WindowsKey.Alt;
                case KeyCode.LeftAlt:
                    return WindowsKey.Alt;
                case KeyCode.Print:
                    return WindowsKey.Print;
                case KeyCode.Backslash:
                    return WindowsKey.OemBackslash;
                case KeyCode.Slash:
                    return WindowsKey.Oem2;
                case KeyCode.Tilde:
                    return WindowsKey.Oem3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(key), key, null);
            }
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        ///     Converts a <see cref="Key" /> to <see cref="WindowsKey" />
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static WindowsKey UnityKeyToWindowKey(this Key key)
        {
            switch (key)
            {
                case Key.None:
                    return WindowsKey.None;
                case Key.Space:
                    return WindowsKey.Space;
                case Key.Enter:
                    return WindowsKey.Enter;
                case Key.Tab:
                    return WindowsKey.Tab;
                case Key.Quote:
                    return WindowsKey.OemQuotes;
                case Key.Semicolon:
                    return WindowsKey.OemSemicolon;
                case Key.Comma:
                    return WindowsKey.Oemcomma;
                case Key.Period:
                    return WindowsKey.OemPeriod;
                case Key.LeftBracket:
                    return WindowsKey.OemOpenBrackets;
                case Key.RightBracket:
                    return WindowsKey.OemCloseBrackets;
                case Key.Minus:
                    return WindowsKey.OemMinus;
                case Key.LeftShift:
                    return WindowsKey.LShiftKey;
                case Key.RightShift:
                    return WindowsKey.RShiftKey;
                case Key.RightAlt:
                case Key.LeftAlt:
                    return WindowsKey.Alt;
                case Key.LeftCtrl:
                    return WindowsKey.LControlKey;
                case Key.RightCtrl:
                    return WindowsKey.RControlKey;
                case Key.Escape:
                    return WindowsKey.Escape;
                case Key.LeftArrow:
                    return WindowsKey.Left;
                case Key.RightArrow:
                    return WindowsKey.Right;
                case Key.UpArrow:
                    return WindowsKey.Up;
                case Key.DownArrow:
                    return WindowsKey.Down;
                case Key.Backspace:
                    return WindowsKey.Back;
                case Key.PageDown:
                    return WindowsKey.PageDown;
                case Key.PageUp:
                    return WindowsKey.PageUp;
                case Key.Home:
                    return WindowsKey.Home;
                case Key.End:
                    return WindowsKey.End;
                case Key.Insert:
                    return WindowsKey.Insert;
                case Key.Delete:
                    return WindowsKey.Delete;
                case Key.PrintScreen:
                    return WindowsKey.PrintScreen;
                case Key.Pause:
                    return WindowsKey.Pause;
                case Key.NumpadEnter:
                    return WindowsKey.Enter;
                case Key.NumpadDivide:
                    return WindowsKey.Divide;
                case Key.NumpadMultiply:
                    return WindowsKey.Multiply;
                case Key.NumpadPlus:
                    return WindowsKey.Play;
                case Key.NumpadMinus:
                    return WindowsKey.OemMinus;
                case Key.NumpadPeriod:
                    return WindowsKey.OemPeriod;
                case Key.Numpad0:
                    return WindowsKey.NumPad0;
                case Key.Numpad1:
                    return WindowsKey.NumPad1;
                case Key.Numpad2:
                    return WindowsKey.NumPad2;
                case Key.Numpad3:
                    return WindowsKey.NumPad3;
                case Key.Numpad4:
                    return WindowsKey.NumPad4;
                case Key.Numpad5:
                    return WindowsKey.NumPad5;
                case Key.Numpad6:
                    return WindowsKey.NumPad6;
                case Key.Numpad7:
                    return WindowsKey.NumPad7;
                case Key.Numpad8:
                    return WindowsKey.NumPad8;
                case Key.Numpad9:
                    return WindowsKey.NumPad9;
                case Key.F1:
                    return WindowsKey.F1;
                case Key.F2:
                    return WindowsKey.F2;
                case Key.F3:
                    return WindowsKey.F3;
                case Key.F4:
                    return WindowsKey.F4;
                case Key.F5:
                    return WindowsKey.F5;
                case Key.F6:
                    return WindowsKey.F6;
                case Key.F7:
                    return WindowsKey.F7;
                case Key.F8:
                    return WindowsKey.F8;
                case Key.F9:
                    return WindowsKey.F9;
                case Key.F10:
                    return WindowsKey.F10;
                case Key.F11:
                    return WindowsKey.F11;
                case Key.F12:
                    return WindowsKey.F12;
                default:
                    throw new ArgumentOutOfRangeException(nameof(key), key, null);
            }
        }

#endif
    }
}