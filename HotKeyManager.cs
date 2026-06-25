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
        StartOrStop = 3,
        BackupStart = 4,
        BackupStop = 5,
        BackupStartOrStop = 6
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
        private const int HOTKEY_ID_BACKUP_PRIMARY = 1003;
        private const int HOTKEY_ID_BACKUP_SECONDARY = 1004;
        private const int HOTKEY_ID_SHIFT = 0x0004;
        private const int HOTKEY_ID_WIN = 0x0008;
        private const int DEFAULT_HOTKEY_MODIFIERS = HOTKEY_ID_CTRL | HOTKEY_ID_ALT;
        private const int DEFAULT_HOTKEY_START = (int)Keys.S;
        private const int DEFAULT_HOTKEY_STOP = (int)Keys.D;
        public const int WM_HOTKEY = 0x0312;

        private bool _primaryProfileRegistered;
        private bool _backupProfileRegistered;
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
        public bool PrimaryProfileRegistered => _primaryProfileRegistered;
        public bool BackupProfileRegistered => _backupProfileRegistered;

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
        public HotkeyRegistrationProfile Register(IntPtr handle, bool registerBackupProfile = true)
        {
            if (handle == IntPtr.Zero)
            {
                _primaryProfileRegistered = false;
                _backupProfileRegistered = false;
                _activeProfile = HotkeyRegistrationProfile.None;
                return _activeProfile;
            }

            UnregisterAll(handle);
            _primaryProfileRegistered = TryRegisterProfile(handle, HotkeyRegistrationProfile.Primary);
            _backupProfileRegistered = registerBackupProfile && TryRegisterProfile(handle, HotkeyRegistrationProfile.Backup);
            _activeProfile = _primaryProfileRegistered
                ? HotkeyRegistrationProfile.Primary
                : (_backupProfileRegistered ? HotkeyRegistrationProfile.Backup : HotkeyRegistrationProfile.None);
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
            int startId = profile == HotkeyRegistrationProfile.Backup ? HOTKEY_ID_BACKUP_PRIMARY : HOTKEY_ID_PRIMARY;
            int stopId = profile == HotkeyRegistrationProfile.Backup ? HOTKEY_ID_BACKUP_SECONDARY : HOTKEY_ID_SECONDARY;
            if (!startEnabled && !stopEnabled)
            {
                return false;
            }

            bool primaryOk = true;
            if (startEnabled)
            {
                primaryOk = RegisterHotKey(handle, startId, startModifiers, startKey);
                if (!primaryOk)
                {
                    return false;
                }
            }

            bool secondaryRegistered = stopEnabled && (!startEnabled || !IsSameStartStop(startModifiers, startKey, stopModifiers, stopKey));
            bool secondaryOk = true;
            if (secondaryRegistered)
            {
                secondaryOk = RegisterHotKey(handle, stopId, stopModifiers, stopKey);
            }

            bool registered = primaryOk && secondaryOk;
            if (!registered)
            {
                UnregisterHotKey(handle, startId);
                UnregisterHotKey(handle, stopId);
                return false;
            }

            return true;
        }

        /// <summary>Unregisters all hotkeys owned by this window handle.</summary>
        public void UnregisterAll(IntPtr handle)
        {
            UnregisterHotKey(handle, HOTKEY_ID_PRIMARY);
            UnregisterHotKey(handle, HOTKEY_ID_SECONDARY);
            UnregisterHotKey(handle, HOTKEY_ID_BACKUP_PRIMARY);
            UnregisterHotKey(handle, HOTKEY_ID_BACKUP_SECONDARY);
            _primaryProfileRegistered = false;
            _backupProfileRegistered = false;
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
                if (IsStartStopSame(HotkeyRegistrationProfile.Primary))
                {
                    return HotkeyAction.StartOrStop;
                }

                return _startHotkeyEnabled ? HotkeyAction.Start : HotkeyAction.Stop;
            }

            if (id == HOTKEY_ID_SECONDARY)
            {
                return HotkeyAction.Stop;
            }

            if (id == HOTKEY_ID_BACKUP_PRIMARY)
            {
                if (IsStartStopSame(HotkeyRegistrationProfile.Backup))
                {
                    return HotkeyAction.BackupStartOrStop;
                }

                return _backupStartHotkeyEnabled ? HotkeyAction.BackupStart : HotkeyAction.BackupStop;
            }

            if (id == HOTKEY_ID_BACKUP_SECONDARY)
            {
                return HotkeyAction.BackupStop;
            }

            return HotkeyAction.None;
        }

        private bool IsStartStopSame(HotkeyRegistrationProfile profile)
        {
            if (profile == HotkeyRegistrationProfile.Backup)
            {
                return _backupStartHotkeyEnabled && _backupStopHotkeyEnabled &&
                    IsSameStartStop(_backupStartHotkeyModifiers, _backupStartHotkeyKey, _backupStopHotkeyModifiers, _backupStopHotkeyKey);
            }

            return _startHotkeyEnabled && _stopHotkeyEnabled &&
                IsSameStartStop(_startHotkeyModifiers, _startHotkeyKey, _stopHotkeyModifiers, _stopHotkeyKey);
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
