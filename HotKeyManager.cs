using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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

        public void LoadFromConfig()
        {
            ParseHotkey(
                ConfigurationManager.AppSettings["HotkeyStartStopToggle"],
                DEFAULT_HOTKEY_MODIFIERS,
                DEFAULT_HOTKEY_START,
                out _startHotkeyModifiers,
                out _startHotkeyKey);

            ParseHotkey(
                ConfigurationManager.AppSettings["HotkeyStop"],
                DEFAULT_HOTKEY_MODIFIERS,
                DEFAULT_HOTKEY_STOP,
                out _stopHotkeyModifiers,
                out _stopHotkeyKey);
        }

        public bool RegisterIfActive(IntPtr handle, bool isMinimized)
        {
            if (isMinimized)
            {
                return _registered;
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

        public void UnregisterAll(IntPtr handle)
        {
            UnregisterHotKey(handle, HOTKEY_ID_PRIMARY);
            UnregisterHotKey(handle, HOTKEY_ID_SECONDARY);
            _registered = false;
            _secondaryRegistered = false;
        }

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

        private static void ParseHotkey(string hotkeySetting, int defaultModifiers, int defaultKey, out int modifiers, out int key)
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
                else
                {
                    Keys parsed;
                    if (Enum.TryParse(token, true, out parsed))
                    {
                        parsedKey = (int)parsed;
                        hasKey = true;
                    }
                }
            }

            if (hasKey)
            {
                modifiers = parsedModifiers;
                key = parsedKey;
            }
        }
    }
}
