using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Lazy_App_Codex_Core
{
    public enum HotkeyAction
    {
        None = 0,
        Toggle = 1,
        Stop = 2
    }

    public class HotkeyManager
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_ALT = 0x0001;
        private const int HOTKEY_ID_CTRL = 0x0002;
        private const int HOTKEY_ID_PRIMARY = 1001;
        private const int HOTKEY_ID_SECONDARY = 1002;
        private const int HOTKEY_ID_SHIFT = 0x0004;
        private const int HOTKEY_ID_WIN = 0x0008;
        private const int DEFAULT_HOTKEY_MODIFIERS = HOTKEY_ID_CTRL | HOTKEY_ID_ALT;
        private const int DEFAULT_HOTKEY_START = (int)Keys.S;
        private const int DEFAULT_HOTKEY_STOP = (int)Keys.D;
        public const int WM_HOTKEY = 0x0312;

        private bool _registered;
        private bool _secondaryRegistered;
        private int _startHotkeyModifiers = DEFAULT_HOTKEY_MODIFIERS;
        private int _startHotkeyKey = DEFAULT_HOTKEY_START;
        private int _stopHotkeyModifiers = DEFAULT_HOTKEY_MODIFIERS;
        private int _stopHotkeyKey = DEFAULT_HOTKEY_STOP;

        public string ToggleHotkeyText => ToDisplayText(_startHotkeyModifiers, _startHotkeyKey);
        public string StopHotkeyText => ToDisplayText(_stopHotkeyModifiers, _stopHotkeyKey);

        public void Configure(string? startStopHotkey, string? stopHotkey)
        {
            ParseHotkey(
                startStopHotkey,
                DEFAULT_HOTKEY_MODIFIERS,
                DEFAULT_HOTKEY_START,
                out _startHotkeyModifiers,
                out _startHotkeyKey);

            ParseHotkey(
                stopHotkey,
                DEFAULT_HOTKEY_MODIFIERS,
                DEFAULT_HOTKEY_STOP,
                out _stopHotkeyModifiers,
                out _stopHotkeyKey);
        }

        /// <summary>Registers global hotkeys for the current window when active.</summary>
        public bool RegisterIfActive(IntPtr handle, bool isMinimized)
        {
            if (handle == IntPtr.Zero)
            {
                _registered = false;
                _secondaryRegistered = false;
                return false;
            }

            if (isMinimized)
            {
                if (_registered || _secondaryRegistered)
                {
                    UnregisterAll(handle);
                }

                return false;
            }

            UnregisterAll(handle);

            bool primaryOk = RegisterHotKey(handle, HOTKEY_ID_PRIMARY, _startHotkeyModifiers, _startHotkeyKey);
            if (!primaryOk)
            {
                _registered = false;
                return false;
            }

            _secondaryRegistered = !IsSameStartStop();
            bool secondaryOk = true;
            if (_secondaryRegistered)
            {
                secondaryOk = RegisterHotKey(handle, HOTKEY_ID_SECONDARY, _stopHotkeyModifiers, _stopHotkeyKey);
            }

            _registered = primaryOk && secondaryOk;
            if (!_registered)
            {
                UnregisterAll(handle);
            }

            return _registered;
        }

        /// <summary>Unregisters all hotkeys owned by this window handle.</summary>
        public void UnregisterAll(IntPtr handle)
        {
            UnregisterHotKey(handle, HOTKEY_ID_PRIMARY);
            UnregisterHotKey(handle, HOTKEY_ID_SECONDARY);
            _registered = false;
            _secondaryRegistered = false;
        }

        /// <summary>Handles WM_HOTKEY window messages and maps to app actions.</summary>
        public HotkeyAction HandleMessage(Message m)
        {
            if (m.Msg != WM_HOTKEY)
            {
                return HotkeyAction.None;
            }

            int id = m.WParam.ToInt32();
            if (id == HOTKEY_ID_PRIMARY)
            {
                return HotkeyAction.Toggle;
            }

            if (id == HOTKEY_ID_SECONDARY)
            {
                return HotkeyAction.Stop;
            }

            return HotkeyAction.None;
        }

        private bool IsSameStartStop()
        {
            return _startHotkeyModifiers == _stopHotkeyModifiers && _startHotkeyKey == _stopHotkeyKey;
        }

        private static void ParseHotkey(string? hotkeySetting, int defaultModifiers, int defaultKey, out int modifiers, out int key)
        {
            modifiers = defaultModifiers;
            key = defaultKey;

            if (string.IsNullOrWhiteSpace(hotkeySetting))
            {
                return;
            }

            int parsedModifiers = 0;
            int parsedKey = defaultKey;
            bool hasKey = false;
            string[] tokens = hotkeySetting.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string rawToken in tokens)
            {
                string token = rawToken.Trim();
                if (token.Equals("CTRL", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("CONTROL", StringComparison.OrdinalIgnoreCase))
                {
                    parsedModifiers |= HOTKEY_ID_CTRL;
                }
                else if (token.Equals("ALT", StringComparison.OrdinalIgnoreCase))
                {
                    parsedModifiers |= HOTKEY_ID_ALT;
                }
                else if (token.Equals("SHIFT", StringComparison.OrdinalIgnoreCase))
                {
                    parsedModifiers |= HOTKEY_ID_SHIFT;
                }
                else if (token.Equals("WIN", StringComparison.OrdinalIgnoreCase) ||
                         token.Equals("WINDOWS", StringComparison.OrdinalIgnoreCase))
                {
                    parsedModifiers |= HOTKEY_ID_WIN;
                }
                else if (Enum.TryParse(token, true, out Keys parsed))
                {
                    parsedKey = (int)parsed;
                    hasKey = true;
                }
            }

            if (hasKey)
            {
                modifiers = parsedModifiers;
                key = parsedKey;
            }
        }

        private static string ToDisplayText(int modifiers, int key)
        {
            var parts = new List<string>();
            if ((modifiers & HOTKEY_ID_CTRL) != 0) parts.Add("Ctrl");
            if ((modifiers & HOTKEY_ID_ALT) != 0) parts.Add("Alt");
            if ((modifiers & HOTKEY_ID_SHIFT) != 0) parts.Add("Shift");
            if ((modifiers & HOTKEY_ID_WIN) != 0) parts.Add("Win");
            parts.Add(((Keys)key).ToString());
            return string.Join("+", parts);
        }
    }
}