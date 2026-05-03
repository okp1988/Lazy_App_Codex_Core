using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Lazy_App_Codex_Core
{
    public enum HotkeyAction
    {
        None = 0,
        Start = 1,
        Stop = 2,
        StartOrStop = 3
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
        private bool _startHotkeyEnabled = true;
        private bool _stopHotkeyEnabled = true;
        private int _startHotkeyModifiers = DEFAULT_HOTKEY_MODIFIERS;
        private int _startHotkeyKey = DEFAULT_HOTKEY_START;
        private int _stopHotkeyModifiers = DEFAULT_HOTKEY_MODIFIERS;
        private int _stopHotkeyKey = DEFAULT_HOTKEY_STOP;

        public string StartHotkeyText => _startHotkeyEnabled ? ToDisplayText(_startHotkeyModifiers, _startHotkeyKey) : "Disabled";
        public string StopHotkeyText => _stopHotkeyEnabled ? ToDisplayText(_stopHotkeyModifiers, _stopHotkeyKey) : "Disabled";

        public void Configure(string? startHotkey, string? stopHotkey)
        {
            _startHotkeyEnabled = TryParseHotkey(
                startHotkey,
                DEFAULT_HOTKEY_MODIFIERS,
                DEFAULT_HOTKEY_START,
                out _startHotkeyModifiers,
                out _startHotkeyKey);

            _stopHotkeyEnabled = TryParseHotkey(
                stopHotkey,
                DEFAULT_HOTKEY_MODIFIERS,
                DEFAULT_HOTKEY_STOP,
                out _stopHotkeyModifiers,
                out _stopHotkeyKey);
        }

        /// <summary>Registers global hotkeys for the current window handle.</summary>
        public bool Register(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                _registered = false;
                _secondaryRegistered = false;
                return false;
            }

            UnregisterAll(handle);

            bool primaryOk = true;
            if (_startHotkeyEnabled)
            {
                primaryOk = RegisterHotKey(handle, HOTKEY_ID_PRIMARY, _startHotkeyModifiers, _startHotkeyKey);
                if (!primaryOk)
                {
                    _registered = false;
                    return false;
                }
            }

            _secondaryRegistered = _stopHotkeyEnabled && (!_startHotkeyEnabled || !IsSameStartStop());
            bool secondaryOk = true;
            if (_secondaryRegistered)
            {
                secondaryOk = RegisterHotKey(handle, HOTKEY_ID_SECONDARY, _stopHotkeyModifiers, _stopHotkeyKey);
            }

            _registered = primaryOk && secondaryOk;
            if (!_startHotkeyEnabled && !_secondaryRegistered)
            {
                _registered = true;
            }

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
                if (_startHotkeyEnabled && _stopHotkeyEnabled && IsSameStartStop())
                {
                    return HotkeyAction.StartOrStop;
                }

                return HotkeyAction.Start;
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

        private static bool TryParseHotkey(string? hotkeySetting, int defaultModifiers, int defaultKey, out int modifiers, out int key)
        {
            modifiers = defaultModifiers;
            key = defaultKey;

            if (string.IsNullOrWhiteSpace(hotkeySetting))
            {
                return false;
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
                return true;
            }

            return true;
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
