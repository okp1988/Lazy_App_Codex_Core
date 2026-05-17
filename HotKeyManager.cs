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

    public enum HotkeyRegistrationProfile
    {
        None = 0,
        Primary = 1,
        Backup = 2
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
        private bool _backupStartHotkeyEnabled;
        private bool _backupStopHotkeyEnabled;
        private int _startHotkeyModifiers = DEFAULT_HOTKEY_MODIFIERS;
        private int _startHotkeyKey = DEFAULT_HOTKEY_START;
        private int _stopHotkeyModifiers = DEFAULT_HOTKEY_MODIFIERS;
        private int _stopHotkeyKey = DEFAULT_HOTKEY_STOP;
        private int _backupStartHotkeyModifiers = DEFAULT_HOTKEY_MODIFIERS;
        private int _backupStartHotkeyKey = DEFAULT_HOTKEY_START;
        private int _backupStopHotkeyModifiers = DEFAULT_HOTKEY_MODIFIERS;
        private int _backupStopHotkeyKey = DEFAULT_HOTKEY_STOP;
        private HotkeyRegistrationProfile _activeProfile = HotkeyRegistrationProfile.None;

        public string StartHotkeyText => _startHotkeyEnabled ? ToDisplayText(_startHotkeyModifiers, _startHotkeyKey) : "Disabled";
        public string StopHotkeyText => _stopHotkeyEnabled ? ToDisplayText(_stopHotkeyModifiers, _stopHotkeyKey) : "Disabled";
        public string BackupStartHotkeyText => _backupStartHotkeyEnabled ? ToDisplayText(_backupStartHotkeyModifiers, _backupStartHotkeyKey) : "Disabled";
        public string BackupStopHotkeyText => _backupStopHotkeyEnabled ? ToDisplayText(_backupStopHotkeyModifiers, _backupStopHotkeyKey) : "Disabled";
        public string ActiveStartHotkeyText => _activeProfile == HotkeyRegistrationProfile.Backup ? BackupStartHotkeyText : StartHotkeyText;
        public string ActiveStopHotkeyText => _activeProfile == HotkeyRegistrationProfile.Backup ? BackupStopHotkeyText : StopHotkeyText;
        public HotkeyRegistrationProfile ActiveProfile => _activeProfile;

        public void Configure(string? startHotkey, string? stopHotkey, string? backupStartHotkey, string? backupStopHotkey)
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

            _backupStartHotkeyEnabled = TryParseHotkey(
                backupStartHotkey,
                DEFAULT_HOTKEY_MODIFIERS,
                DEFAULT_HOTKEY_START,
                out _backupStartHotkeyModifiers,
                out _backupStartHotkeyKey);

            _backupStopHotkeyEnabled = TryParseHotkey(
                backupStopHotkey,
                DEFAULT_HOTKEY_MODIFIERS,
                DEFAULT_HOTKEY_STOP,
                out _backupStopHotkeyModifiers,
                out _backupStopHotkeyKey);
        }

        /// <summary>Registers global hotkeys for the current window handle.</summary>
        public HotkeyRegistrationProfile Register(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                _registered = false;
                _secondaryRegistered = false;
                _activeProfile = HotkeyRegistrationProfile.None;
                return _activeProfile;
            }

            UnregisterAll(handle);
            if (TryRegisterProfile(handle, HotkeyRegistrationProfile.Primary))
            {
                return _activeProfile;
            }

            UnregisterAll(handle);
            TryRegisterProfile(handle, HotkeyRegistrationProfile.Backup);
            return _activeProfile;
        }

        private bool TryRegisterProfile(IntPtr handle, HotkeyRegistrationProfile profile)
        {
            bool startEnabled = profile == HotkeyRegistrationProfile.Backup ? _backupStartHotkeyEnabled : _startHotkeyEnabled;
            bool stopEnabled = profile == HotkeyRegistrationProfile.Backup ? _backupStopHotkeyEnabled : _stopHotkeyEnabled;
            int startModifiers = profile == HotkeyRegistrationProfile.Backup ? _backupStartHotkeyModifiers : _startHotkeyModifiers;
            int startKey = profile == HotkeyRegistrationProfile.Backup ? _backupStartHotkeyKey : _startHotkeyKey;
            int stopModifiers = profile == HotkeyRegistrationProfile.Backup ? _backupStopHotkeyModifiers : _stopHotkeyModifiers;
            int stopKey = profile == HotkeyRegistrationProfile.Backup ? _backupStopHotkeyKey : _stopHotkeyKey;
            if (!startEnabled && !stopEnabled)
            {
                _registered = false;
                _secondaryRegistered = false;
                _activeProfile = HotkeyRegistrationProfile.None;
                return false;
            }

            bool primaryOk = true;
            if (startEnabled)
            {
                primaryOk = RegisterHotKey(handle, HOTKEY_ID_PRIMARY, startModifiers, startKey);
                if (!primaryOk)
                {
                    _registered = false;
                    _activeProfile = HotkeyRegistrationProfile.None;
                    return false;
                }
            }

            _secondaryRegistered = stopEnabled && (!startEnabled || !IsSameStartStop(startModifiers, startKey, stopModifiers, stopKey));
            bool secondaryOk = true;
            if (_secondaryRegistered)
            {
                secondaryOk = RegisterHotKey(handle, HOTKEY_ID_SECONDARY, stopModifiers, stopKey);
            }

            _registered = primaryOk && secondaryOk;
            if (!_registered)
            {
                UnregisterAll(handle);
                return false;
            }

            _activeProfile = profile;
            return true;
        }

        /// <summary>Unregisters all hotkeys owned by this window handle.</summary>
        public void UnregisterAll(IntPtr handle)
        {
            UnregisterHotKey(handle, HOTKEY_ID_PRIMARY);
            UnregisterHotKey(handle, HOTKEY_ID_SECONDARY);
            _registered = false;
            _secondaryRegistered = false;
            _activeProfile = HotkeyRegistrationProfile.None;
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
                if (IsActiveStartStopSame())
                {
                    return HotkeyAction.StartOrStop;
                }

                return IsActiveStartEnabled() ? HotkeyAction.Start : HotkeyAction.Stop;
            }

            if (id == HOTKEY_ID_SECONDARY)
            {
                return HotkeyAction.Stop;
            }

            return HotkeyAction.None;
        }

        private bool IsActiveStartStopSame()
        {
            if (_activeProfile == HotkeyRegistrationProfile.Backup)
            {
                return _backupStartHotkeyEnabled && _backupStopHotkeyEnabled &&
                    IsSameStartStop(_backupStartHotkeyModifiers, _backupStartHotkeyKey, _backupStopHotkeyModifiers, _backupStopHotkeyKey);
            }

            return _startHotkeyEnabled && _stopHotkeyEnabled &&
                IsSameStartStop(_startHotkeyModifiers, _startHotkeyKey, _stopHotkeyModifiers, _stopHotkeyKey);
        }

        private bool IsActiveStartEnabled()
        {
            return _activeProfile == HotkeyRegistrationProfile.Backup ? _backupStartHotkeyEnabled : _startHotkeyEnabled;
        }

        private static bool IsSameStartStop(int startModifiers, int startKey, int stopModifiers, int stopKey)
        {
            return startModifiers == stopModifiers && startKey == stopKey;
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
