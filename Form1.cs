namespace Lazy_App_Codex_Core
{
    public partial class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
        [System.Runtime.InteropServices.ClassInterface(System.Runtime.InteropServices.ClassInterfaceType.None)]
        private class CTaskbarList
        {
        }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEFAF")]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList3
        {
            void HrInit();
            void AddTab(IntPtr hwnd);
            void DeleteTab(IntPtr hwnd);
            void ActivateTab(IntPtr hwnd);
            void SetActiveAlt(IntPtr hwnd);
            void MarkFullscreenWindow(IntPtr hwnd, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)] bool fullscreen);
            void SetProgressValue(IntPtr hwnd, ulong completed, ulong total);
            void SetProgressState(IntPtr hwnd, int flags);
            void RegisterTab(IntPtr hwndTab, IntPtr hwndMdi);
            void UnregisterTab(IntPtr hwndTab);
            void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
            void SetTabActive(IntPtr hwndTab, IntPtr hwndMdi, int flags);
            void ThumbBarAddButtons(IntPtr hwnd, uint buttonCount, IntPtr buttons);
            void ThumbBarUpdateButtons(IntPtr hwnd, uint buttonCount, IntPtr buttons);
            void ThumbBarSetImageList(IntPtr hwnd, IntPtr imageList);
            void SetOverlayIcon(IntPtr hwnd, IntPtr icon, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string description);
        }

        private readonly HotkeyManager _hotkeys = new HotkeyManager();
        private readonly ScriptConfigRepository _configRepository = new ScriptConfigRepository("config.json");
        private readonly ScriptRunner _runner = new ScriptRunner();
        private readonly AdbShellController _adbController = new AdbShellController();

        private const int SingleSetWindowWidth = 500;
        private const int DualSetWindowWidth = 984;
        private const int WindowHeight = 264;
        private const int Slot1ContentColumnWidth = 322;
        private const int Slot2ContentColumnWidth = 322;
        private const int ActionColumnWidth = 150;
        private const int RunSetGapWidth = 12;

        private ConfigLibrary _library = new ConfigLibrary();
        private readonly List<RunTarget> _runTargets = new List<RunTarget>();
        private Dictionary<string, DeviceInfo> _deviceMetadata = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DeviceInfo> _detectedDeviceMetadata = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _deviceInfoSyncing = new(StringComparer.OrdinalIgnoreCase);
        private RunSlot _slot1 = null!;
        private RunSlot _slot2 = null!;
        private bool _slot2Visible;
        private Color _statusDotColor = Color.Red;
        private Color _adbStatusDotColor = Color.DarkGray;
        private bool? _lastHotkeyRegistrationSucceeded;
        private readonly System.Windows.Forms.Timer _clockTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _adbRetryTimer = new System.Windows.Forms.Timer();
        private readonly ToolTip _statusToolTip = new ToolTip();
        private AdbDeviceStatus _adbDeviceStatus = new AdbDeviceStatus(AdbDeviceState.NoServer, 0, "ADB status has not been checked yet.");
        private System.Diagnostics.Process? _adbTrackProcess;
        private TaskCompletionSource<AdbDeviceStatus>? _adbTrackFirstStatus;
        private bool _adbMonitorStarting;
        private bool _updatingDeviceDropdown;
        private bool _closing;
        private readonly string _baseTitle;
        private readonly Icon _baseIcon;
        private readonly Dictionary<string, Icon> _taskbarStatusIcons = new Dictionary<string, Icon>(StringComparer.Ordinal);
        private ITaskbarList3? _taskbarList;
        private readonly List<string> _debugLog = new List<string>();

        private static bool IsAdbActionEnabled = true;

        public Form1()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ApplyWindowSizeForSetCount(false);
            Icon? appIcon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            _baseIcon = appIcon != null ? (Icon)appIcon.Clone() : (Icon)SystemIcons.Application.Clone();
            appIcon?.Dispose();
            Icon = _baseIcon;
            _baseTitle = Text;
            BuildRunSlots();

            Load += OnLoad;
            Shown += OnShown;
            Activated += OnActivated;
            Resize += OnResize;

            ResetOffsetSelection();

            LoadConfig();
            _hotkeys.Configure(
                _configRepository.Settings.HotkeyStart,
                _configRepository.Settings.HotkeyStop,
                _configRepository.Settings.HotkeyBackupStart,
                _configRepository.Settings.HotkeyBackupStop);
            _clockTimer.Interval = 1000;
            _clockTimer.Tick += (_, _) => UpdateLiveStatusLabels();
            _clockTimer.Start();
            _adbRetryTimer.Interval = 30000;
            _adbRetryTimer.Tick += async (_, _) => await EnsureAdbTrackMonitorAsync("retry timer");
            _statusToolTip.SetToolTip(statusDot, "Global hotkey status has not been checked yet.");
            _statusToolTip.SetToolTip(adbStatusDot, "ADB status has not been checked yet.");
            _statusToolTip.SetToolTip(_slot1.DeviceBox, "Select the ADB device to run Set 1 commands on.");
            _statusToolTip.SetToolTip(_slot2.DeviceBox, "Select the ADB device to run Set 2 commands on.");
            UpdateLiveStatusLabels();
            SetRunningState(_slot1, false);
            SetRunningState(_slot2, false);
            SetSlot2Visible(false);
        }

        private void BuildRunSlots()
        {
            var set1Control = new RunSetControl(showStatusDots: true, showSharedButtons: true);
            var set2Control = new RunSetControl(showStatusDots: false, showSharedButtons: false);
            statusDot = set1Control.StatusDot!;
            adbStatusDot = set1Control.AdbStatusDot!;
            btnConfig = set1Control.ConfigButton!;
            btnWirelessAdb = set1Control.WirelessAdbButton!;
            statusDot.Paint += statusDot_Paint;
            adbStatusDot.Paint += statusDot_Paint;
            btnConfig.Click += (_, e) => btnConfig_Click(btnConfig, e);
            btnWirelessAdb.Click += (_, e) => btnWirelessAdb_Click(btnWirelessAdb, e);

            _slot1 = new RunSlot(
                1,
                set1Control.ScriptBox,
                set1Control.OffsetBox,
                set1Control.TagFilter,
                set1Control.DeviceBox,
                set1Control.RunButton,
                set1Control.LiveStatusLayout,
                set1Control.CurrentActionLabel,
                set1Control.StepLabel,
                set1Control.CycleLabel,
                set1Control.NextActionLabel,
                set1Control.NextAtLabel,
                set1Control.EstimatedEndLabel)
            {
                ContentPanel = set1Control,
                ActionPanel = set1Control
            };

            WireRunSlot(_slot1);

            _slot2 = new RunSlot(
                2,
                set2Control.ScriptBox,
                set2Control.OffsetBox,
                set2Control.TagFilter,
                set2Control.DeviceBox,
                set2Control.RunButton,
                set2Control.LiveStatusLayout,
                set2Control.CurrentActionLabel,
                set2Control.StepLabel,
                set2Control.CycleLabel,
                set2Control.NextActionLabel,
                set2Control.NextAtLabel,
                set2Control.EstimatedEndLabel)
            {
                ContentPanel = set2Control,
                ActionPanel = set2Control
            };

            WireRunSlot(_slot2);

            mainLayout.ColumnCount = 3;
            mainLayout.ColumnStyles.Clear();
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Slot1ContentColumnWidth + ActionColumnWidth));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RunSetGapWidth));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Slot2ContentColumnWidth + ActionColumnWidth));
            mainLayout.RowStyles.Clear();
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            mainLayout.Controls.Clear();
            mainLayout.Controls.Add(set1Control, 0, 0);
            mainLayout.Controls.Add(set2Control, 2, 0);
        }

        private void WireRunSlot(RunSlot slot)
        {
            slot.ScriptBox.SelectionChanged += (_, _) => ApplySelectedDefaultOffset(slot);
            slot.TagFilter.SelectedIndexChanged += (_, _) => SlotTagFilterChanged(slot);
            slot.DeviceBox.SelectedIndexChanged += (_, _) => SlotDeviceChanged(slot);
            slot.DeviceBox.DrawMode = DrawMode.OwnerDrawFixed;
            slot.DeviceBox.DrawItem += ddlDevice_DrawItem;
            slot.RunButton.Click += async (_, _) => await ToggleRunAsync(slot);
        }

        protected override void WndProc(ref Message m)
        {
            HotkeyAction action = _hotkeys.HandleMessage(m);
            if (action != HotkeyAction.None)
            {
                HandleHotkeyAction(action);
            }

            base.WndProc(ref m);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Alt | Keys.D1))
            {
                ToggleSlot2VisibleFromShortcut();
                return true;
            }

            if (keyData == (Keys.Alt | Keys.D2))
            {
                btnConfig.PerformClick();
                return true;
            }

            if (keyData == (Keys.Alt | Keys.D3))
            {
                btnWirelessAdb.PerformClick();
                return true;
            }

            if (keyData == Keys.Escape && AnySlotRunning)
            {
                _ = StopAllRunsAsync();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void OnLoad(object? sender, EventArgs e)
        {
            RegisterHotkeysForWindowState();
            UpdateTaskbarOverlayIcon();
            _ = EnsureAdbTrackMonitorAsync("load");
        }

        private void OnShown(object? sender, EventArgs e)
        {
            BeginInvoke((Action)UpdateTaskbarOverlayIcon);
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized && _lastHotkeyRegistrationSucceeded != true)
            {
                RegisterHotkeysForWindowState();
            }
        }

        private void OnResize(object? sender, EventArgs e)
        {

            if (WindowState == FormWindowState.Minimized)
            {
                if (_lastHotkeyRegistrationSucceeded == null)
                {
                    return;
                }

                _hotkeys.UnregisterAll(Handle);
                UpdateHotkeyStatus(HotkeyRegistrationProfile.None);
                _lastHotkeyRegistrationSucceeded = null;
                WriteLog("GLOBAL HOTKEY DROPPED WHILE MINIMIZED.");
                return;
            }

            if (_lastHotkeyRegistrationSucceeded == null)
            {
                RegisterHotkeysForWindowState();
            }
        }

        private void RegisterHotkeysForWindowState()
        {
            HotkeyRegistrationProfile profile = _hotkeys.Register(Handle, _slot2Visible);
            bool success = profile != HotkeyRegistrationProfile.None;
            UpdateHotkeyStatus(profile);

            if (success && _lastHotkeyRegistrationSucceeded != true)
            {
                WriteLog($"GLOBAL HOTKEY REGISTERED (Set 1: {_hotkeys.StartHotkeyText}/{_hotkeys.StopHotkeyText}; Set 2: {_hotkeys.BackupStartHotkeyText}/{_hotkeys.BackupStopHotkeyText}).");
            }

            if (!success && _lastHotkeyRegistrationSucceeded != false)
            {
                WriteLog($"GLOBAL HOTKEY NOT REGISTERED (Primary Start: {_hotkeys.StartHotkeyText}, Primary Stop: {_hotkeys.StopHotkeyText}; Backup Start: {_hotkeys.BackupStartHotkeyText}, Backup Stop: {_hotkeys.BackupStopHotkeyText}).");
                AppLogger.LogWarning($"Global hotkey was not registered (Primary Start: {_hotkeys.StartHotkeyText}, Primary Stop: {_hotkeys.StopHotkeyText}; Backup Start: {_hotkeys.BackupStartHotkeyText}, Backup Stop: {_hotkeys.BackupStopHotkeyText}).");
            }

            _lastHotkeyRegistrationSucceeded = success;
        }

        private void LoadConfig()
        {
            string? selectedId1 = _slot1.ScriptBox.SelectedItem is RunTarget selected1 ? selected1.Id : null;
            string? selectedId2 = _slot2.ScriptBox.SelectedItem is RunTarget selected2 ? selected2.Id : null;
            string selectedTag1 = _slot1.TagFilter.SelectedItem?.ToString() ?? "All";
            string selectedTag2 = _slot2.TagFilter.SelectedItem?.ToString() ?? "All";

            try
            {
                _library = _configRepository.LoadLibrary();
                _deviceMetadata = CloneDevices(_configRepository.Settings.Devices);
            }
            catch (Exception ex)
            {
                _library = new ConfigLibrary();
                _deviceMetadata = new Dictionary<string, DeviceInfo>(StringComparer.OrdinalIgnoreCase);
                _detectedDeviceMetadata.Clear();
                AppLogger.LogError("Failed to load script configuration.", ex);
                MessageBox.Show("Failed to load config.json. Please check the logs folder.", "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _runTargets.Clear();
            _slot1.ScriptBox.ClearSelection();
            _slot2.ScriptBox.ClearSelection();

            LoadTagFilter(_slot1, selectedTag1);
            LoadTagFilter(_slot2, selectedTag2);
            RebuildRunTargets(_slot1, selectedId1);
            RebuildRunTargets(_slot2, selectedId2);
        }

        private void LoadTagFilter(RunSlot slot, string selectedTag)
        {
            slot.TagFilter.BeginUpdate();
            slot.TagFilter.Items.Clear();
            slot.TagFilter.Items.Add("All");
            foreach (string tag in NormalizeTags(_configRepository.Settings.Tags))
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    slot.TagFilter.Items.Add(tag);
                }
            }

            int selectedIndex = slot.TagFilter.FindStringExact(selectedTag);
            slot.TagFilter.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            slot.TagFilter.EndUpdate();
        }

        private void RebuildRunTargets(RunSlot slot, string? selectedId = null)
        {
            string selectedTag = slot.TagFilter.SelectedItem?.ToString() ?? "All";
            _runTargets.Clear();
            slot.ScriptBox.ClearSelection();

            foreach (var script in _library.Scripts)
            {
                if (!script.Hidden && MatchesSelectedTag(script.Tag, selectedTag))
                {
                    _runTargets.Add(new RunTarget("script", script.Id, script.Name, script.Tag));
                }
            }

            foreach (var sequence in _library.Sequences)
            {
                if (!sequence.Hidden && MatchesSelectedTag(sequence.Tag, selectedTag))
                {
                    _runTargets.Add(new RunTarget("sequence", sequence.Id, sequence.Name, sequence.Tag));
                }
            }

            foreach (var runPlan in _library.RunPlans)
            {
                if (MatchesSelectedTag(runPlan.Tag, selectedTag))
                {
                    _runTargets.Add(new RunTarget("plan", runPlan.Id, runPlan.Name, runPlan.Tag));
                }
            }

            slot.ScriptBox.SetItems(_runTargets.Cast<object>());

            if (string.IsNullOrWhiteSpace(selectedId))
            {
                return;
            }

            foreach (var target in _runTargets)
            {
                if (target.Id == selectedId)
                {
                    slot.ScriptBox.SelectedItem = target;
                    break;
                }
            }
        }

        private static bool MatchesSelectedTag(string itemTag, string selectedTag)
        {
            return selectedTag == "All" ||
                string.IsNullOrWhiteSpace(itemTag) ||
                itemTag.Equals(selectedTag, StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> NormalizeTags(IEnumerable<string> tags)
        {
            var normalized = new List<string>();
            foreach (string tag in tags)
            {
                string value = tag.Trim();
                if (value.Length == 0 ||
                    value.Equals("All", StringComparison.OrdinalIgnoreCase) ||
                    normalized.Any(existing => existing.Equals(value, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                normalized.Add(value);
            }

            return normalized;
        }

        private async Task ToggleRunAsync(RunSlot slot)
        {
            if (slot.IsRunning)
            {
                await StopRunAsync(slot);
                return;
            }

            await StartRunAsync(slot);
        }

        private async Task StartRunAsync(RunSlot slot)
        {
            await RefreshAdbStatusForRunAsync();

            RunTarget? target = slot.ScriptBox.SelectedItem as RunTarget;
            if (target == null)
            {
                MessageBox.Show("Select a script, sequence, or run plan before run");
                return;
            }

            ScriptModel? script = target.Kind == "script" ? _library.FindScriptById(target.Id) : null;
            SequenceModel? sequence = target.Kind == "sequence" ? _library.FindSequenceById(target.Id) : null;
            RunPlanModel? runPlan = target.Kind == "plan" ? _library.FindRunPlanById(target.Id) : null;
            if (script == null && sequence == null && runPlan == null)
            {
                MessageBox.Show($"Missing config ({target.Name})");
                return;
            }

            if (runPlan != null && TryGetRunPlanValidationError(runPlan, out string validationError))
            {
                MessageBox.Show(validationError, "Run Plan Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string? selectedDeviceSerial = GetSelectedReadyDeviceSerial(slot);
            if (selectedDeviceSerial == null)
            {
                string message = _adbDeviceStatus.DeviceCount > 1
                    ? "Select a device before run."
                    : _adbDeviceStatus.Tooltip;
                LogAdbStatus($"Set {slot.Number} run blocked: {_adbDeviceStatus.State}, devices={_adbDeviceStatus.DeviceCount}, selected={(string.IsNullOrWhiteSpace(slot.SelectedDeviceSerial) ? "(none)" : slot.SelectedDeviceSerial)}, message={message}");
                MessageBox.Show(message, "ADB Not Ready", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            LogAdbStatus($"Set {slot.Number} run allowed: {_adbDeviceStatus.State}, devices={_adbDeviceStatus.DeviceCount}, selected={selectedDeviceSerial}.");

            slot.RunCts = new CancellationTokenSource();
            _debugLog.Clear();
            SetRunningState(slot, true);
            UpdateWindowTitle();

            slot.RunTask = RunSelectedTargetAsync(slot, target, script, sequence, runPlan, selectedDeviceSerial, slot.RunCts.Token);
            try
            {
                await slot.RunTask;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Run failed.", ex);
                ShowRunError(slot, ex);
                WriteLog("ERROR: " + ex.Message);
            }
            finally
            {
                slot.RunTask = null;
                slot.RunCts?.Dispose();
                slot.RunCts = null;
                SetRunningState(slot, false);
                UpdateWindowTitle();
            }
        }

        private async Task RefreshAdbStatusForRunAsync()
        {
            LogAdbStatus($"Run refresh requested. Cached={_adbDeviceStatus.State}, tracker={FormatTrackerState()}.");
            if (_adbDeviceStatus.State == AdbDeviceState.NoDevice)
            {
                LogAdbStatus($"Run using cached blocking background status immediately: {_adbDeviceStatus.State}, devices={_adbDeviceStatus.DeviceCount}.");
                return;
            }

            if (_adbDeviceStatus.State is AdbDeviceState.OneDevice or AdbDeviceState.MultipleDevices && _adbTrackProcess != null && !_adbTrackProcess.HasExited)
            {
                LogAdbStatus($"Run using cached background status: {_adbDeviceStatus.State}, devices={_adbDeviceStatus.DeviceCount}.");
                return;
            }

            if (_adbTrackProcess == null || _adbTrackProcess.HasExited)
            {
                await EnsureAdbTrackMonitorAsync("run");
            }

            try
            {
                var status = await WaitForTrackDevicesStatusAsync(3500);
                LogAdbStatus($"Run track-devices result: {status.State}, devices={status.DeviceCount}, message={status.Tooltip}");
                ApplyAdbDeviceStatusOnUi(status);
            }
            catch (Exception ex)
            {
                LogAdbStatus("Run track-devices check failed: " + ex.Message);
                ApplyAdbDeviceStatusOnUi(new AdbDeviceStatus(AdbDeviceState.NoServer, 0, "ADB track-devices check failed: " + ex.Message));
            }
        }

        private async Task RunSelectedTargetAsync(RunSlot slot, RunTarget target, ScriptModel? script, SequenceModel? sequence, RunPlanModel? runPlan, string deviceSerial, CancellationToken token)
        {
            var (offsetValue, offsetAxis) = GetSelectedOffset(slot, target.Name);
            WriteLog($"SET {slot.Number} OFFSET SELECTED {FormatOffset(offsetValue, offsetAxis)}");
            if (script != null)
            {
                await _runner.RunScriptAsync(script, offsetValue, offsetAxis, deviceSerial, token, status => UpdateLiveStatus(slot, status), IsAdbActionEnabled);
            }
            else if (sequence != null)
            {
                (offsetValue, offsetAxis) = GetSelectedOffset(slot, target.Name);
                await _runner.RunSequenceAsync(
                    sequence,
                    _library,
                    offsetValue,
                    offsetAxis,
                    scriptItem => GetSelectedOffset(slot, scriptItem.Name),
                    deviceSerial,
                    token,
                    status => UpdateLiveStatus(slot, status),
                    IsAdbActionEnabled);
            }
            else if (runPlan != null)
            {
                await _runner.RunPlanAsync(
                    runPlan,
                    _library,
                    scriptItem => GetRunPlanScriptOffset(slot, scriptItem),
                    sequenceItem => GetRunPlanSequenceOffset(slot, sequenceItem),
                    (sequenceItem, scriptItem) => GetRunPlanSequenceScriptOffset(slot, sequenceItem, scriptItem),
                    deviceSerial,
                    token,
                    status => UpdateLiveStatus(slot, status),
                    IsAdbActionEnabled);
            }
        }

        private bool TryGetRunPlanValidationError(RunPlanModel runPlan, out string error)
        {
            if (runPlan.Items.Count == 0)
            {
                error = $"Run plan \"{runPlan.Name}\" has no items.";
                return true;
            }

            foreach (var item in runPlan.Items)
            {
                if (item.Type == "sequence")
                {
                    if (_library.FindSequenceById(item.TargetId) == null)
                    {
                        error = $"Run plan \"{runPlan.Name}\" references a missing sequence: {item.TargetId}";
                        return true;
                    }

                    continue;
                }

                if (_library.FindScriptById(item.TargetId) == null)
                {
                    error = $"Run plan \"{runPlan.Name}\" references a missing script: {item.TargetId}";
                    return true;
                }
            }

            error = "";
            return false;
        }

        private (int value, string axis) GetSelectedOffset(RunSlot slot, string scriptName)
        {
            string raw = OffsetDisplayOption.ReadValue(slot.OffsetBox.SelectedItem);
            return ResolveOffset(raw, scriptName);
        }

        private (int value, string axis) GetRunPlanScriptOffset(RunSlot slot, ScriptModel script)
        {
            return script.DefaultOffsetEnabled
                ? ResolveOffset(script.DefaultOffset, script.Name)
                : GetSelectedOffset(slot, script.Name);
        }

        private (int value, string axis) GetRunPlanSequenceOffset(RunSlot slot, SequenceModel sequence)
        {
            return sequence.DefaultOffsetEnabled
                ? ResolveOffset(sequence.DefaultOffset, sequence.Name)
                : GetSelectedOffset(slot, sequence.Name);
        }

        private (int value, string axis) GetRunPlanSequenceScriptOffset(RunSlot slot, SequenceModel sequence, ScriptModel script)
        {
            return sequence.DefaultOffsetEnabled
                ? ResolveOffset(sequence.DefaultOffset, script.Name)
                : GetSelectedOffset(slot, script.Name);
        }

        private (int value, string axis) ResolveOffset(string raw, string scriptName)
        {
            if (raw == "0")
            {
                return (0, "y");
            }

            var parts = raw.Split(':');
            if (parts.Length != 2)
            {
                return (0, "y");
            }

            if (!int.TryParse(parts[0], out int step))
            {
                return (0, "y");
            }

            string axis = parts[1].Trim().Equals("x", StringComparison.OrdinalIgnoreCase) ? "x" : "y";
            int offsetUnit = _configRepository.GetOffsetUnitForScript(scriptName, axis);
            return (step * offsetUnit, axis);
        }

        private static string FormatOffset(int value, string axis)
        {
            string sign = value > 0 ? "+" : "";
            return $"{sign}{value}{axis}";
        }

        private void HandleHotkeyAction(HotkeyAction action)
        {
            if (action == HotkeyAction.Start)
            {
                _ = StartRunFromHotkeyAsync(_slot1);
            }
            else if (action == HotkeyAction.StartOrStop)
            {
                _ = ToggleRunAsync(_slot1);
            }
            else if (action == HotkeyAction.Stop)
            {
                _ = StopRunAsync(_slot1);
            }
            else if (action == HotkeyAction.BackupStart)
            {
                _ = StartRunFromHotkeyAsync(_slot2);
            }
            else if (action == HotkeyAction.BackupStartOrStop)
            {
                _ = ToggleRunAsync(_slot2);
            }
            else if (action == HotkeyAction.BackupStop)
            {
                _ = StopRunAsync(_slot2);
            }
        }

        private async Task StartRunFromHotkeyAsync(RunSlot slot)
        {
            if (slot == _slot2 && !_slot2Visible)
            {
                SetSlot2Visible(true);
            }

            if (!slot.IsRunning)
            {
                await StartRunAsync(slot);
            }
        }

        private async Task StopRunAsync(RunSlot slot)
        {
            if (!slot.IsRunning)
            {
                return;
            }

            slot.RunCts?.Cancel();
            try
            {
                if (slot.RunTask != null)
                {
                    await slot.RunTask;
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task StopAllRunsAsync()
        {
            await Task.WhenAll(StopRunAsync(_slot1), StopRunAsync(_slot2));
        }

        private void SetRunningState(RunSlot slot, bool isRunning)
        {
            slot.IsRunning = isRunning;
            slot.ScriptBox.Enabled = !isRunning;
            slot.OffsetBox.Enabled = !isRunning;
            slot.TagFilter.Enabled = !isRunning;
            slot.DeviceBox.Enabled = !isRunning && slot.DeviceBox.Items.Count > 0;
            slot.RunButton.Text = isRunning ? "Stop" : "Run";
            btnConfig.Enabled = !AnySlotRunning;
            btnWirelessAdb.Enabled = !AnySlotRunning;
            UpdateTaskbarOverlayIcon();

            if (isRunning)
            {
                UpdateLiveStatus(slot, new LiveRunStatus { CurrentAction = "--", CurrentStep = "--", CurrentCycle = "--", NextAction = "--" });
            }
            else
            {
                UpdateLiveStatus(slot, new LiveRunStatus { Idle = true });
            }

            UpdateDeviceDropdown(_adbDeviceStatus, queueSync: false);
        }

        private void UpdateLiveStatus(RunSlot slot, LiveRunStatus status)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => UpdateLiveStatus(slot, status)));
                return;
            }

            slot.LiveStatus = status;
            UpdateLiveStatusLabels();
            WriteLog($"SET {slot.Number}: {status.CurrentAction}");
        }

        private void SetTaskbarOverlayIcon(Icon? overlayIcon, string description)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => SetTaskbarOverlayIcon(overlayIcon, description)));
                return;
            }

            if (!IsHandleCreated)
            {
                return;
            }

            try
            {
                _taskbarList ??= CreateTaskbarList();
                _taskbarList.SetOverlayIcon(Handle, overlayIcon?.Handle ?? IntPtr.Zero, description);
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning($"Taskbar overlay icon could not be updated: {ex.Message}");
            }
        }

        private static ITaskbarList3 CreateTaskbarList()
        {
            var taskbarList = (ITaskbarList3)new CTaskbarList();
            taskbarList.HrInit();
            return taskbarList;
        }

        private void UpdateTaskbarOverlayIcon()
        {
            bool showSlot2Identifier = _slot2Visible || _slot2.IsRunning;
            string iconKey = string.Join(
                "|",
                _slot1.IsRunning,
                _slot2.IsRunning,
                showSlot2Identifier,
                _statusDotColor.ToArgb());
            if (!_taskbarStatusIcons.TryGetValue(iconKey, out Icon? icon))
            {
                icon = CreateTaskbarOverlayIcon(_slot1.IsRunning, _slot2.IsRunning, showSlot2Identifier, _statusDotColor);
                _taskbarStatusIcons.Add(iconKey, icon);
            }

            string description = (_slot1.IsRunning, _slot2.IsRunning) switch
            {
                (true, true) => "Set 1 and Set 2 running",
                (true, false) => "Set 1 running",
                (false, true) => "Set 2 running",
                _ => "Both sets stopped"
            };
            SetTaskbarOverlayIcon(icon, description);
        }

        private static Icon CreateTaskbarOverlayIcon(bool slot1Running, bool slot2Running, bool showSlot2Identifier, Color hotkeyStatusColor)
        {
            using Bitmap bitmap = new Bitmap(32, 32);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            DrawRunSetIdentifiers(graphics, slot1Running, slot2Running, showSlot2Identifier);
            DrawHotkeyStatusIdentifier(graphics, hotkeyStatusColor);

            IntPtr iconHandle = bitmap.GetHicon();
            try
            {
                return (Icon)Icon.FromHandle(iconHandle).Clone();
            }
            finally
            {
                DestroyIcon(iconHandle);
            }
        }

        private static void DrawRunSetIdentifiers(Graphics graphics, bool slot1Running, bool slot2Running, bool showSlot2Identifier)
        {
            const int markerWidth = 7;
            const int markerHeight = 18;
            const int markerTop = 2;
            const int markerRight = 2;
            const int markerGap = 2;

            int slot2X = 32 - markerRight - markerWidth;
            if (showSlot2Identifier)
            {
                DrawOverlaySlot(graphics, new Rectangle(slot2X - markerGap - markerWidth, markerTop, markerWidth, markerHeight), slot1Running);
                DrawOverlaySlot(graphics, new Rectangle(slot2X, markerTop, markerWidth, markerHeight), slot2Running);
                return;
            }

            DrawOverlaySlot(graphics, new Rectangle(slot2X, markerTop, markerWidth, markerHeight), slot1Running);
        }

        private static void DrawOverlaySlot(Graphics graphics, Rectangle bounds, bool running)
        {
            using var fill = new SolidBrush(running ? Color.LimeGreen : Color.DimGray);
            using var border = new Pen(Color.White, 1F);
            graphics.FillRectangle(fill, bounds);
            graphics.DrawRectangle(border, bounds);
        }

        private static void DrawHotkeyStatusIdentifier(Graphics graphics, Color hotkeyStatusColor)
        {
            var bounds = new Rectangle(22, 22, 8, 8);
            using var fill = new SolidBrush(hotkeyStatusColor);
            using var border = new Pen(Color.White, 1F);
            graphics.FillEllipse(fill, bounds);
            graphics.DrawEllipse(border, bounds);
        }

        private void statusDot_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel dot)
            {
                return;
            }

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, dot.Width - 1, dot.Height - 1);
            dot.Region = new Region(path);

            Color dotColor = ReferenceEquals(dot, adbStatusDot) ? _adbStatusDotColor : _statusDotColor;
            using var brush = new SolidBrush(dotColor);
            using var border = new Pen(Color.FromArgb(120, Color.Black));
            var bounds = new Rectangle(0, 0, dot.Width - 1, dot.Height - 1);
            e.Graphics.FillEllipse(brush, bounds);
            e.Graphics.DrawEllipse(border, bounds);
        }

        private void UpdateLiveStatusLabels()
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)UpdateLiveStatusLabels);
                return;
            }

            UpdateLiveStatusLabels(_slot1);
            UpdateLiveStatusLabels(_slot2);
        }

        private static void UpdateLiveStatusLabels(RunSlot slot)
        {
            if (slot.LiveStatus.Idle)
            {
                slot.CurrentActionLabel.Text = "--";
                slot.StepLabel.Text = "--";
                slot.CycleLabel.Text = "--";
                slot.NextActionLabel.Text = "--";
                slot.NextAtLabel.Text = "--";
                slot.EstimatedEndLabel.Text = "--";
                return;
            }

            slot.CurrentActionLabel.Text = slot.LiveStatus.CurrentAction;
            slot.StepLabel.Text = slot.LiveStatus.CurrentStep;
            slot.CycleLabel.Text = slot.LiveStatus.CurrentCycle;
            slot.NextActionLabel.Text = slot.LiveStatus.NextAction;
            slot.NextAtLabel.Text = FormatStatusTime(slot.LiveStatus.NextActionAt);
            slot.EstimatedEndLabel.Text = FormatStatusTime(slot.LiveStatus.EstimatedEnd);
        }

        private void ShowRunError(RunSlot slot, Exception ex)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => ShowRunError(slot, ex)));
                return;
            }

            slot.LiveStatus = new LiveRunStatus
            {
                CurrentAction = "ERROR",
                CurrentStep = "--",
                CurrentCycle = "--",
                NextAction = "--",
                Idle = false
            };
            UpdateLiveStatusLabels();
            slot.NextAtLabel.Text = "Check ADB/device connection";
            slot.EstimatedEndLabel.Text = ShortenError(ex.Message);
            MessageBox.Show(ex.Message, "Lazy App Run Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void WriteLog(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => WriteLog(text)));
                return;
            }

            _debugLog.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + text);
            if (_debugLog.Count > 500)
            {
                _debugLog.RemoveRange(0, _debugLog.Count - 500);
            }
        }

        private void UpdateHotkeyStatus(HotkeyRegistrationProfile profile)
        {
            _statusDotColor = (_hotkeys.PrimaryProfileRegistered, _hotkeys.BackupProfileRegistered) switch
            {
                (true, true) => Color.Gold,
                (true, false) => Color.Green,
                (false, true) => Color.DodgerBlue,
                _ => Color.Red
            };
            string primaryStatus = _hotkeys.PrimaryProfileRegistered ? "registered" : "not registered";
            string backupStatus = _hotkeys.BackupProfileRegistered ? "registered" : "not registered";
            _statusToolTip.SetToolTip(
                statusDot,
                $"Set 1 hotkeys {primaryStatus}. Start: {_hotkeys.StartHotkeyText}, Stop: {_hotkeys.StopHotkeyText}. Set 2 hotkeys {backupStatus}. Start: {_hotkeys.BackupStartHotkeyText}, Stop: {_hotkeys.BackupStopHotkeyText}.");
            statusDot.Invalidate();
            UpdateTaskbarOverlayIcon();
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            _ = EnsureAdbTrackMonitorAsync("config");
            using var editor = new ConfigEditorForm(_configRepository, () => _adbDeviceStatus, () => _slot1.SelectedDeviceSerial);
            if (editor.ShowDialog(this) != DialogResult.OK || !editor.ConfigSaved)
            {
                return;
            }

            LoadConfig();
            _hotkeys.UnregisterAll(Handle);
            _hotkeys.Configure(
                _configRepository.Settings.HotkeyStart,
                _configRepository.Settings.HotkeyStop,
                _configRepository.Settings.HotkeyBackupStart,
                _configRepository.Settings.HotkeyBackupStop);
            _lastHotkeyRegistrationSucceeded = null;
            RegisterHotkeysForWindowState();
            UpdateDeviceDropdown(_adbDeviceStatus, queueSync: false);
            WriteLog("CONFIG UPDATED.");
        }

        private async void btnWirelessAdb_Click(object sender, EventArgs e)
        {
            using var dialog = new WirelessAdbConnectForm(_configRepository, _adbController);
            dialog.ShowDialog(this);
            if (dialog.ConfigChanged)
            {
                LoadConfig();
                WriteLog("WIRELESS ADB DEVICE UPDATED.");
            }

            await EnsureAdbTrackMonitorAsync(dialog.ServerRestarted ? "wireless adb server restart" : "wireless adb");
        }

        private void SlotTagFilterChanged(RunSlot slot)
        {
            if (_library.Scripts.Count == 0 && _library.Sequences.Count == 0)
            {
                return;
            }

            RebuildRunTargets(slot);
            ResetOffsetSelection(slot);
        }

        private void SlotDeviceChanged(RunSlot slot)
        {
            if (_updatingDeviceDropdown)
            {
                return;
            }

            slot.SelectedDeviceSerial = (slot.DeviceBox.SelectedItem as DeviceDisplayItem)?.Serial;
            LogAdbStatus($"Set {slot.Number} selected device changed: " + (slot.SelectedDeviceSerial ?? "(none)"));
            UpdateDeviceDropdown(_adbDeviceStatus, queueSync: false);
        }

        private bool AnySlotRunning => _slot1.IsRunning || _slot2.IsRunning;

        private bool IsSlotDeviceSelectionActive(RunSlot slot)
        {
            return slot == _slot1 || _slot2Visible || slot.IsRunning;
        }

        private void ToggleSlot2VisibleFromShortcut()
        {
            if (_slot2Visible && _slot2.IsRunning)
            {
                WriteLog("STOP SET 2 BEFORE HIDING.");
                return;
            }

            SetSlot2Visible(!_slot2Visible);
        }

        private void SetSlot2Visible(bool visible)
        {
            bool changed = _slot2Visible != visible;
            _slot2Visible = visible;
            if (!_slot2.IsRunning)
            {
                _slot2.SelectedDeviceSerial = null;
            }

            _slot2.ContentPanel.Visible = visible;
            _slot2.ActionPanel.Visible = visible;
            mainLayout.ColumnStyles[1].Width = visible ? RunSetGapWidth : 0F;
            mainLayout.ColumnStyles[2].Width = visible ? Slot2ContentColumnWidth + ActionColumnWidth : 0F;
            ApplyWindowSizeForSetCount(visible);

            UpdateDeviceDropdown(_adbDeviceStatus, queueSync: false);
            UpdateTaskbarOverlayIcon();
            if (changed && WindowState != FormWindowState.Minimized)
            {
                _lastHotkeyRegistrationSucceeded = null;
                RegisterHotkeysForWindowState();
            }
        }

        private void ApplyWindowSizeForSetCount(bool set2Visible)
        {
            var fixedSize = new Size(set2Visible ? DualSetWindowWidth : SingleSetWindowWidth, WindowHeight);
            MaximumSize = Size.Empty;
            MinimumSize = fixedSize;
            if (WindowState == FormWindowState.Normal)
            {
                Size = fixedSize;
            }

            MaximumSize = fixedSize;
        }

        private void UpdateWindowTitle()
        {
            var running = new List<string>();
            if (_slot1.IsRunning && _slot1.ScriptBox.SelectedItem is RunTarget target1)
            {
                running.Add("Set 1: " + target1.Name);
            }

            if (_slot2.IsRunning && _slot2.ScriptBox.SelectedItem is RunTarget target2)
            {
                running.Add("Set 2: " + target2.Name);
            }

            Text = running.Count == 0 ? _baseTitle : $"{_baseTitle} - Running: {string.Join("; ", running)}";
        }

        private void ddlDevice_DrawItem(object? sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (sender is not ComboBox combo || e.Index < 0 || e.Index >= combo.Items.Count)
            {
                return;
            }

            if (combo.Items[e.Index] is not DeviceDisplayItem item)
            {
                return;
            }

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color textColor = selected ? SystemColors.HighlightText : (item.MetadataMismatch ? Color.Firebrick : combo.ForeColor);
            using Font fallbackFont = new Font(combo.Font, combo.Font.Style);
            TextRenderer.DrawText(e.Graphics, item.ToString(), e.Font ?? fallbackFont, e.Bounds, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            e.DrawFocusRectangle();
        }

        private void ResetOffsetSelection()
        {
            ResetOffsetSelection(_slot1);
            ResetOffsetSelection(_slot2);
        }

        private void ResetOffsetSelection(RunSlot slot)
        {
            SelectOffsetValue(slot, "0");
        }

        private void ApplySelectedDefaultOffset(RunSlot slot)
        {
            ResetOffsetSelection(slot);
            RunTarget? target = slot.ScriptBox.SelectedItem as RunTarget;
            if (target == null)
            {
                return;
            }

            bool enabled;
            string defaultOffset;
            if (target.Kind == "script")
            {
                var script = _library.FindScriptById(target.Id);
                enabled = script?.DefaultOffsetEnabled == true;
                defaultOffset = script?.DefaultOffset ?? "0";
            }
            else if (target.Kind == "sequence")
            {
                var sequence = _library.FindSequenceById(target.Id);
                enabled = sequence?.DefaultOffsetEnabled == true;
                defaultOffset = sequence?.DefaultOffset ?? "0";
            }
            else
            {
                return;
            }

            if (!enabled)
            {
                return;
            }

            SelectOffsetValue(slot, defaultOffset);
        }

        private void SelectOffsetValue(RunSlot slot, string value)
        {
            for (int index = 0; index < slot.OffsetBox.Items.Count; index++)
            {
                if (slot.OffsetBox.Items[index] is OffsetDisplayOption option &&
                    option.Value.Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    slot.OffsetBox.SelectedIndex = index;
                    return;
                }
            }

            slot.OffsetBox.SelectedIndex = -1;
        }

        private async Task EnsureAdbTrackMonitorAsync(string trigger)
        {
            if (_adbMonitorStarting || (_adbTrackProcess != null && !_adbTrackProcess.HasExited))
            {
                LogAdbStatus($"Ensure skipped from {trigger}. starting={_adbMonitorStarting}, tracker={FormatTrackerState()}, cached={_adbDeviceStatus.State}.");
                return;
            }

            _adbMonitorStarting = true;
            LogAdbStatus($"Ensure started from {trigger}. cached={_adbDeviceStatus.State}.");
            try
            {
                using var cts = new CancellationTokenSource(3500);
                bool serverRunning = await _adbController.IsServerRunningAsync(cts.Token);
                LogAdbStatus($"Port 5037 check from {trigger}: serverRunning={serverRunning}.");
                if (!serverRunning)
                {
                    ApplyAdbDeviceStatusOnUi(new AdbDeviceStatus(AdbDeviceState.NoServer, 0, "ADB server is not running."));
                    return;
                }

                LogAdbStatus($"Starting track-devices from {trigger}.");
                StartTrackDevicesProcess();
            }
            catch (Exception ex)
            {
                LogAdbStatus($"Ensure failed from {trigger}: {ex.Message}");
                ApplyAdbDeviceStatusOnUi(new AdbDeviceStatus(AdbDeviceState.NoServer, 0, "ADB status check failed: " + ex.Message));
            }
            finally
            {
                _adbMonitorStarting = false;
            }
        }

        private void StartTrackDevicesProcess()
        {
            try
            {
                StopTrackDevicesProcess();
                _adbTrackFirstStatus = new TaskCompletionSource<AdbDeviceStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _adbController.AdbPath,
                        Arguments = "track-devices",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        StandardErrorEncoding = System.Text.Encoding.UTF8
                    },
                    EnableRaisingEvents = true
                };

                var lines = new List<string>();
                process.OutputDataReceived += (_, e) =>
                {
                    if (_closing)
                    {
                        return;
                    }

                    if (e.Data == null)
                    {
                        LogAdbStatus("track-devices stdout closed. Marking ADB server as not running.");
                        var status = new AdbDeviceStatus(AdbDeviceState.NoServer, 0, "ADB server is not running.");
                        _adbTrackFirstStatus?.TrySetResult(status);
                        ApplyAdbDeviceStatusOnUi(status);
                        return;
                    }

                    string line = e.Data.Trim();
                    if (line.Length == 0)
                    {
                        LogAdbStatus("track-devices block: " + (lines.Count == 0 ? "(no devices)" : string.Join(" | ", lines)));
                        ApplyTrackedDeviceLines(lines);
                        lines.Clear();
                        return;
                    }

                    bool startsNewSnapshot = StripTrackDevicesPacketPrefixes(ref line);
                    if (startsNewSnapshot)
                    {
                        lines.Clear();
                    }

                    if (line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase))
                    {
                        LogAdbStatus("track-devices header received.");
                        lines.Clear();
                        return;
                    }

                    if (line.Length == 0)
                    {
                        LogAdbStatus("track-devices block: (no devices)");
                        ApplyTrackedDeviceLines(lines);
                        lines.Clear();
                        return;
                    }

                    LogAdbStatus("track-devices line: " + line);
                    lines.Add(line);
                    ApplyTrackedDeviceLines(lines);
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (_closing || e.Data == null)
                    {
                        return;
                    }

                    LogAdbStatus("track-devices stderr: " + e.Data);
                };
                process.Exited += (_, _) =>
                {
                    if (!_closing)
                    {
                        string exitDetail;
                        try
                        {
                            exitDetail = " exitCode=" + process.ExitCode;
                        }
                        catch
                        {
                            exitDetail = "";
                        }

                        LogAdbStatus("adb track-devices exited." + exitDetail + " Marking ADB server as not running.");
                        var status = new AdbDeviceStatus(AdbDeviceState.NoServer, 0, "ADB server is not running.");
                        _adbTrackFirstStatus?.TrySetResult(status);
                        ApplyAdbDeviceStatusOnUi(status);
                    }
                };

                if (process.Start())
                {
                    _adbTrackProcess = process;
                    LogAdbStatus($"Started adb track-devices. pid={process.Id}.");
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    var initialStatus = new AdbDeviceStatus(AdbDeviceState.NoDevice, 0, "ADB server is running, but no ready device is connected.");
                    ApplyAdbDeviceStatusOnUi(initialStatus);
                }
                else
                {
                    process.Dispose();
                    LogAdbStatus("adb track-devices did not start.");
                    var status = new AdbDeviceStatus(AdbDeviceState.NoServer, 0, "Could not start adb track-devices.");
                    _adbTrackFirstStatus?.TrySetResult(status);
                    ApplyAdbDeviceStatusOnUi(status);
                }
            }
            catch (Exception ex)
            {
                LogAdbStatus("Could not start adb track-devices: " + ex.Message);
                var status = new AdbDeviceStatus(AdbDeviceState.NoServer, 0, "Could not start adb track-devices: " + ex.Message);
                _adbTrackFirstStatus?.TrySetResult(status);
                ApplyAdbDeviceStatusOnUi(status);
            }
        }

        private void ApplyTrackedDeviceLines(List<string> lines)
        {
            var status = AdbShellController.BuildDeviceStatus(AdbShellController.ParseDeviceLines(string.Join(Environment.NewLine, lines)));
            _adbTrackFirstStatus?.TrySetResult(status);
            ApplyAdbDeviceStatusOnUi(status);
        }

        private static bool StripTrackDevicesPacketPrefixes(ref string line)
        {
            bool stripped = false;
            while (line.Length >= 4 && IsHexLengthPrefix(line.AsSpan(0, 4)))
            {
                stripped = true;
                line = line[4..].TrimStart();
            }

            return stripped;
        }

        private static bool IsHexLengthPrefix(ReadOnlySpan<char> value)
        {
            if (value.Length != 4)
            {
                return false;
            }

            int length = 0;
            foreach (char c in value)
            {
                int digit;
                if (c >= '0' && c <= '9')
                {
                    digit = c - '0';
                }
                else if (c >= 'a' && c <= 'f')
                {
                    digit = c - 'a' + 10;
                }
                else if (c >= 'A' && c <= 'F')
                {
                    digit = c - 'A' + 10;
                }
                else
                {
                    return false;
                }

                length = (length << 4) + digit;
            }

            return length <= 1024;
        }

        private async Task<AdbDeviceStatus> WaitForTrackDevicesStatusAsync(int timeoutMs)
        {
            var statusTask = _adbTrackFirstStatus?.Task;
            if (statusTask == null || statusTask.IsCompleted)
            {
                return _adbDeviceStatus;
            }

            var completed = await Task.WhenAny(statusTask, Task.Delay(timeoutMs));
            return completed == statusTask
                ? await statusTask
                : (_adbTrackProcess != null && !_adbTrackProcess.HasExited
                    ? new AdbDeviceStatus(AdbDeviceState.NoDevice, 0, "ADB server is running, but no ready device is connected.")
                    : new AdbDeviceStatus(AdbDeviceState.NoServer, 0, "ADB server is not running."));
        }

        private void ApplyAdbDeviceStatusOnUi(AdbDeviceStatus status)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => ApplyAdbDeviceStatus(status)));
                return;
            }

            ApplyAdbDeviceStatus(status);
        }

        private void StopTrackDevicesProcess()
        {
            var process = _adbTrackProcess;
            _adbTrackProcess = null;
            if (process == null)
            {
                return;
            }

            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        private void ApplyAdbDeviceStatus(AdbDeviceStatus status)
        {
            if (_adbDeviceStatus.State != status.State || _adbDeviceStatus.DeviceCount != status.DeviceCount || _adbDeviceStatus.Tooltip != status.Tooltip)
            {
                LogAdbStatus($"Status changed: {_adbDeviceStatus.State}/{_adbDeviceStatus.DeviceCount} -> {status.State}/{status.DeviceCount}. {status.Tooltip}");
            }

            _adbDeviceStatus = status;
            UpdateDeviceDropdown(status);
            _adbStatusDotColor = status.State switch
            {
                AdbDeviceState.NoServer => Color.DarkGray,
                AdbDeviceState.NoDevice => Color.Red,
                AdbDeviceState.OneDevice => Color.Green,
                AdbDeviceState.MultipleDevices => Color.Goldenrod,
                _ => Color.DarkGray
            };

            _statusToolTip.SetToolTip(adbStatusDot, status.Tooltip);
            adbStatusDot.Invalidate();

            if (status.State == AdbDeviceState.NoServer)
            {
                if (!_adbRetryTimer.Enabled)
                {
                    _adbRetryTimer.Start();
                }
            }
            else
            {
                if (_adbRetryTimer.Enabled)
                {
                    _adbRetryTimer.Stop();
                }
            }
        }

        private void UpdateDeviceDropdown(AdbDeviceStatus status, bool queueSync = true)
        {
            var readyDevices = status.Devices
                .Where(device => device.IsReady)
                .GroupBy(device => device.Serial, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(device => GetDeviceDisplayName(device.Serial), StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (queueSync)
            {
                QueueDeviceInfoSync(readyDevices);
            }

            UpdateDeviceDropdownForSlot(_slot1, _slot2, readyDevices);
            UpdateDeviceDropdownForSlot(_slot2, _slot1, readyDevices);
        }

        private void UpdateDeviceDropdownForSlot(RunSlot slot, RunSlot otherSlot, List<AdbTrackedDevice> readyDevices)
        {
            string? previousSelection = slot.SelectedDeviceSerial;
            bool selectedDeviceRemoved = previousSelection != null &&
                !readyDevices.Any(device => device.Serial.Equals(previousSelection, StringComparison.OrdinalIgnoreCase));

            if (selectedDeviceRemoved)
            {
                slot.SelectedDeviceSerial = null;
            }

            string? desiredSelection = slot.SelectedDeviceSerial;
            string? otherSelection = IsSlotDeviceSelectionActive(otherSlot) ? otherSlot.SelectedDeviceSerial : null;
            if (!slot.IsRunning && SerialEquals(desiredSelection, otherSelection))
            {
                desiredSelection = null;
                slot.SelectedDeviceSerial = null;
            }

            if (desiredSelection == null && readyDevices.Count > 0)
            {
                desiredSelection = readyDevices
                    .FirstOrDefault(device => !SerialEquals(device.Serial, otherSelection))
                    ?.Serial;
            }

            _updatingDeviceDropdown = true;
            try
            {
                slot.DeviceBox.BeginUpdate();
                slot.DeviceBox.Items.Clear();
                foreach (var device in readyDevices)
                {
                    if (!SerialEquals(device.Serial, desiredSelection) && SerialEquals(device.Serial, otherSelection))
                    {
                        continue;
                    }

                    string key = AdbShellController.GetDeviceKey(device.Serial);
                    _deviceMetadata.TryGetValue(key, out var metadata);
                    _detectedDeviceMetadata.TryGetValue(key, out var detected);
                    slot.DeviceBox.Items.Add(new DeviceDisplayItem(device.Serial, GetDeviceDisplayName(device.Serial), HasDeviceMetadataMismatch(metadata, detected)));
                }

                slot.DeviceBox.SelectedIndex = -1;
                if (desiredSelection != null)
                {
                    for (int index = 0; index < slot.DeviceBox.Items.Count; index++)
                    {
                        if (slot.DeviceBox.Items[index] is DeviceDisplayItem item &&
                            item.Serial.Equals(desiredSelection, StringComparison.OrdinalIgnoreCase))
                        {
                            slot.DeviceBox.SelectedIndex = index;
                            slot.SelectedDeviceSerial = item.Serial;
                            break;
                        }
                    }
                }
            }
            finally
            {
                slot.DeviceBox.EndUpdate();
                _updatingDeviceDropdown = false;
            }

            slot.DeviceBox.Enabled = !slot.IsRunning && slot.DeviceBox.Items.Count > 0;
            _statusToolTip.SetToolTip(
                slot.DeviceBox,
                readyDevices.Count == 0
                    ? "No ready ADB device is connected."
                    : $"Select the ADB device to run Set {slot.Number} commands on.");

            if (selectedDeviceRemoved && slot.IsRunning && !slot.DeviceLossStopRequested)
            {
                slot.DeviceLossStopRequested = true;
                string removedDevice = GetDeviceDisplayName(previousSelection ?? "");
                LogAdbStatus($"Set {slot.Number} selected device removed while running: {previousSelection}. Stopping run.");
                _ = StopRunForMissingDeviceAsync(slot, removedDevice);
            }
            else if (!slot.IsRunning)
            {
                slot.DeviceLossStopRequested = false;
            }
        }

        private async Task StopRunForMissingDeviceAsync(RunSlot slot, string removedDevice)
        {
            await StopRunAsync(slot);
            if (!_closing)
            {
                MessageBox.Show(
                    $"Set {slot.Number} selected device disconnected: {removedDevice}. Run stopped.",
                    "ADB Device Removed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            slot.DeviceLossStopRequested = false;
        }

        private string? GetSelectedReadyDeviceSerial(RunSlot slot)
        {
            if (string.IsNullOrWhiteSpace(slot.SelectedDeviceSerial))
            {
                return null;
            }

            return _adbDeviceStatus.Devices.Any(device =>
                device.IsReady &&
                device.Serial.Equals(slot.SelectedDeviceSerial, StringComparison.OrdinalIgnoreCase))
                    ? slot.SelectedDeviceSerial
                    : null;
        }

        private static bool SerialEquals(string? left, string? right)
        {
            return !string.IsNullOrWhiteSpace(left) &&
                !string.IsNullOrWhiteSpace(right) &&
                left.Equals(right, StringComparison.OrdinalIgnoreCase);
        }

        private string GetDeviceDisplayName(string serial)
        {
            string key = AdbShellController.GetDeviceKey(serial);
            if (_deviceMetadata.TryGetValue(key, out var metadata) && !string.IsNullOrWhiteSpace(metadata.Name))
            {
                return metadata.Name;
            }

            return key;
        }

        private void QueueDeviceInfoSync(IEnumerable<AdbTrackedDevice> readyDevices)
        {
            foreach (var device in readyDevices)
            {
                string key = AdbShellController.GetDeviceKey(device.Serial);
                if (string.IsNullOrWhiteSpace(key) || _deviceInfoSyncing.Contains(key))
                {
                    continue;
                }

                _deviceInfoSyncing.Add(key);
                _ = SyncDeviceInfoAsync(device.Serial, key);
            }
        }

        private async Task SyncDeviceInfoAsync(string serial, string key)
        {
            try
            {
                using var cts = new CancellationTokenSource(7000);
                DeviceInfo detected = await _adbController.ReadDeviceInfoAsync(serial, cts.Token);
                detected.LastSerial = serial;
                detected.LastSeen = DateTimeOffset.Now.ToString("O");
                _detectedDeviceMetadata[key] = detected;

                bool changed = false;
                if (!_deviceMetadata.TryGetValue(key, out var existing))
                {
                    _deviceMetadata[key] = detected;
                    changed = true;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(existing.Name))
                    {
                        existing.Name = detected.Name;
                        changed = true;
                    }

                    if (string.IsNullOrWhiteSpace(existing.Manufacturer))
                    {
                        existing.Manufacturer = detected.Manufacturer;
                        changed = true;
                    }

                    if (string.IsNullOrWhiteSpace(existing.Model))
                    {
                        existing.Model = detected.Model;
                        changed = true;
                    }
                }

                if (changed)
                {
                    SaveDeviceMetadata();
                }

                BeginInvoke((Action)(() => UpdateDeviceDropdown(_adbDeviceStatus, queueSync: false)));
            }
            catch (Exception ex)
            {
                LogAdbStatus($"Device info sync failed for {serial}: {ex.Message}");
            }
            finally
            {
                _deviceInfoSyncing.Remove(key);
            }
        }

        private void SaveDeviceMetadata()
        {
            try
            {
                var root = _configRepository.LoadRawConfig();
                var settings = (Newtonsoft.Json.Linq.JObject)root["settings"]!;
                settings["devices"] = Newtonsoft.Json.Linq.JObject.FromObject(_deviceMetadata);
                _configRepository.SaveRawConfig(root);
            }
            catch (Exception ex)
            {
                LogAdbStatus("Failed to save device metadata: " + ex.Message);
            }
        }

        private static bool HasDeviceMetadataMismatch(DeviceInfo? metadata, DeviceInfo? detected)
        {
            if (metadata == null || detected == null)
            {
                return false;
            }

            bool manufacturerMismatch = !string.IsNullOrWhiteSpace(metadata.Manufacturer) &&
                !string.IsNullOrWhiteSpace(detected.Manufacturer) &&
                !metadata.Manufacturer.Equals(detected.Manufacturer, StringComparison.OrdinalIgnoreCase);
            bool modelMismatch = !string.IsNullOrWhiteSpace(metadata.Model) &&
                !string.IsNullOrWhiteSpace(detected.Model) &&
                !metadata.Model.Equals(detected.Model, StringComparison.OrdinalIgnoreCase);
            return manufacturerMismatch || modelMismatch;
        }

        private static Dictionary<string, DeviceInfo> CloneDevices(IDictionary<string, DeviceInfo>? devices)
        {
            var clone = new Dictionary<string, DeviceInfo>(StringComparer.OrdinalIgnoreCase);
            if (devices == null)
            {
                return clone;
            }

            foreach (var item in devices)
            {
                clone[item.Key] = new DeviceInfo
                {
                    Name = item.Value.Name,
                    Manufacturer = item.Value.Manufacturer,
                    Model = item.Value.Model,
                    LastSerial = item.Value.LastSerial,
                    LastSeen = item.Value.LastSeen
                };
            }

            return clone;
        }

        private string FormatTrackerState()
        {
            if (_adbTrackProcess == null)
            {
                return "none";
            }

            try
            {
                return _adbTrackProcess.HasExited ? $"exited pid={_adbTrackProcess.Id}" : $"running pid={_adbTrackProcess.Id}";
            }
            catch
            {
                return "unknown";
            }
        }

        private static void LogAdbStatus(string message)
        {
            AppLogger.LogInfo("[ADB] " + message);
        }

        private static string FormatStatusTime(DateTime? time)
        {
            if (time == null)
            {
                return "--";
            }

            TimeSpan remaining = time.Value - DateTime.Now;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            return $"{time.Value:HH:mm:ss} ({FormatDuration(remaining)})";
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            }

            return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }

        private static string ShortenError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "Unknown error";
            }

            const int maxLength = 72;
            return message.Length <= maxLength ? message : message[..maxLength] + "...";
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _closing = true;
            _hotkeys.UnregisterAll(Handle);
            SetTaskbarOverlayIcon(null, "");
            _clockTimer.Stop();
            _clockTimer.Dispose();
            _adbRetryTimer.Stop();
            _adbRetryTimer.Dispose();
            StopTrackDevicesProcess();
            _statusToolTip.Dispose();
            foreach (Icon icon in _taskbarStatusIcons.Values)
            {
                icon.Dispose();
            }

            _taskbarStatusIcons.Clear();
            _baseIcon.Dispose();
            _slot1.RunCts?.Cancel();
            _slot2.RunCts?.Cancel();
        }

        private sealed class RunSlot
        {
            public RunSlot(
                int number,
                SearchableDropdown scriptBox,
                ComboBox offsetBox,
                ComboBox tagFilter,
                ComboBox deviceBox,
                Button runButton,
                TableLayoutPanel liveStatusLayout,
                Label currentActionLabel,
                Label stepLabel,
                Label cycleLabel,
                Label nextActionLabel,
                Label nextAtLabel,
                Label estimatedEndLabel)
            {
                Number = number;
                ScriptBox = scriptBox;
                OffsetBox = offsetBox;
                TagFilter = tagFilter;
                DeviceBox = deviceBox;
                RunButton = runButton;
                LiveStatusLayout = liveStatusLayout;
                CurrentActionLabel = currentActionLabel;
                StepLabel = stepLabel;
                CycleLabel = cycleLabel;
                NextActionLabel = nextActionLabel;
                NextAtLabel = nextAtLabel;
                EstimatedEndLabel = estimatedEndLabel;
            }

            public int Number { get; }
            public SearchableDropdown ScriptBox { get; }
            public ComboBox OffsetBox { get; }
            public ComboBox TagFilter { get; }
            public ComboBox DeviceBox { get; }
            public Button RunButton { get; }
            public TableLayoutPanel LiveStatusLayout { get; }
            public Label CurrentActionLabel { get; }
            public Label StepLabel { get; }
            public Label CycleLabel { get; }
            public Label NextActionLabel { get; }
            public Label NextAtLabel { get; }
            public Label EstimatedEndLabel { get; }
            public Control ContentPanel { get; init; } = null!;
            public Control ActionPanel { get; init; } = null!;
            public CancellationTokenSource? RunCts { get; set; }
            public Task? RunTask { get; set; }
            public bool IsRunning { get; set; }
            public bool DeviceLossStopRequested { get; set; }
            public string? SelectedDeviceSerial { get; set; }
            public LiveRunStatus LiveStatus { get; set; } = new LiveRunStatus { Idle = true };
        }

        private sealed record RunTarget(string Kind, string Id, string Name, string Tag)
        {
            public override string ToString()
            {
                return Kind switch
                {
                    "sequence" => "[Q] " + Name,
                    "plan" => "[P] " + Name,
                    _ => "[S] " + Name
                };
            }
        }

        private sealed record DeviceDisplayItem(string Serial, string DisplayName, bool MetadataMismatch)
        {
            public override string ToString()
            {
                return DisplayName;
            }
        }
    }
}
