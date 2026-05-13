namespace Lazy_App_Codex_Core
{
    public partial class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int ShowCursor(bool show);

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

        private ConfigLibrary _library = new ConfigLibrary();
        private readonly List<RunTarget> _runTargets = new List<RunTarget>();
        private bool _filteringRunTargets;
        private const string NoRunTargetMatchesText = "(No records found)";
        private CancellationTokenSource? _runCts;
        private Task? _runTask;
        private bool _isRunning;
        private LiveRunStatus _liveStatus = new LiveRunStatus { Idle = true };
        private Color _statusDotColor = Color.Red;
        private bool? _lastHotkeyRegistrationSucceeded;
        private readonly System.Windows.Forms.Timer _clockTimer = new System.Windows.Forms.Timer();
        private readonly string _baseTitle;
        private readonly Icon _baseIcon;
        private readonly Icon _runningIcon;
        private readonly Icon _stoppedIcon;
        private ITaskbarList3? _taskbarList;

        private static bool IsAdbActionEnabled = true;

        public Form1()
        {
            InitializeComponent();
            Icon? appIcon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            _baseIcon = appIcon != null ? (Icon)appIcon.Clone() : (Icon)SystemIcons.Application.Clone();
            appIcon?.Dispose();
            _runningIcon = CreateOverlayIcon(Color.LimeGreen, true);
            _stoppedIcon = CreateOverlayIcon(Color.DodgerBlue, false);
            Icon = _baseIcon;
            _baseTitle = Text;

            Load += OnLoad;
            Activated += OnActivated;
            Resize += OnResize;
            ddlScript.SelectionChangeCommitted += (_, _) => ApplySelectedDefaultOffset();
            ddlScript.TextChanged += ddlScript_TextChanged;
            ddlScript.KeyDown += ddlScript_KeyDown;
            splitContainer1.Panel1.Resize += (_, _) => ApplyResponsiveLayout();

            ResetOffsetSelection();

            LoadConfig();
            _hotkeys.Configure(_configRepository.Settings.HotkeyStart, _configRepository.Settings.HotkeyStop);
            _clockTimer.Interval = 1000;
            _clockTimer.Tick += (_, _) => UpdateLiveStatusLabels();
            _clockTimer.Start();
            UpdateLiveStatusLabels();
            SetRunningState(false);
            ApplyResponsiveLayout();
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
            if (keyData == Keys.Escape && _isRunning)
            {
                HandleHotkeyAction(HotkeyAction.Stop);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void OnLoad(object? sender, EventArgs e)
        {
            ApplyResponsiveLayout();
            RegisterHotkeysForWindowState();
            SetTaskbarOverlayIcon(_stoppedIcon, "Stopped");
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
            ApplyResponsiveLayout();

            if (WindowState == FormWindowState.Minimized)
            {
                if (_lastHotkeyRegistrationSucceeded == null)
                {
                    return;
                }

                _hotkeys.UnregisterAll(Handle);
                UpdateHotkeyStatus(false);
                _lastHotkeyRegistrationSucceeded = null;
                WriteLog("GLOBAL HOTKEY DROPPED WHILE MINIMIZED.");
                return;
            }

            if (_lastHotkeyRegistrationSucceeded == null)
            {
                RegisterHotkeysForWindowState();
            }
        }

        private void ApplyResponsiveLayout()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            int panelWidth = splitContainer1.Panel1.ClientSize.Width;
            if (panelWidth <= 0)
            {
                return;
            }

            int leftMargin = ddlScript.Left;
            int rightMargin = Math.Max(16, leftMargin);
            int statusGap = 8;
            int rightEdge = Math.Max(leftMargin + 120, panelWidth - rightMargin);
            int statusIndicatorLeft = Math.Max(leftMargin + 120, rightEdge - statusDot.Width);
            int contentWidth = Math.Max(120, rightEdge - leftMargin);

            statusDot.Left = statusIndicatorLeft;
            statusDot.Top = ddlScript.Top + (ddlScript.Height - statusDot.Height) / 2;
            ddlScript.Width = Math.Max(120, statusIndicatorLeft - leftMargin - statusGap);
            liveStatusLayout.Width = contentWidth;
        }

        private void RegisterHotkeysForWindowState()
        {
            bool success = _hotkeys.Register(Handle);
            UpdateHotkeyStatus(success);

            if (success && _lastHotkeyRegistrationSucceeded != true)
            {
                WriteLog($"GLOBAL HOTKEY REGISTERED (Start: {_hotkeys.StartHotkeyText}, Stop: {_hotkeys.StopHotkeyText}).");
            }

            if (!success && _lastHotkeyRegistrationSucceeded != false)
            {
                WriteLog($"GLOBAL HOTKEY NOT REGISTERED (Start: {_hotkeys.StartHotkeyText}, Stop: {_hotkeys.StopHotkeyText}).");
                AppLogger.LogWarning($"Global hotkey was not registered (Start: {_hotkeys.StartHotkeyText}, Stop: {_hotkeys.StopHotkeyText}).");
            }

            _lastHotkeyRegistrationSucceeded = success;
        }

        private void LoadConfig()
        {
            string? selectedId = ddlScript.SelectedItem is RunTarget selected ? selected.Id : null;

            try
            {
                _library = _configRepository.LoadLibrary();
            }
            catch (Exception ex)
            {
                _library = new ConfigLibrary();
                AppLogger.LogError("Failed to load script configuration.", ex);
                MessageBox.Show("Failed to load config.json. Please check the logs folder.", "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            ddlScript.Items.Clear();
            _runTargets.Clear();
            ddlScript.Items.Insert(0, "Choose script or sequence");
            ddlScript.SelectedIndex = 0;

            foreach (var script in _library.Scripts)
            {
                _runTargets.Add(new RunTarget("script", script.Id, script.Name));
            }

            foreach (var sequence in _library.Sequences)
            {
                _runTargets.Add(new RunTarget("sequence", sequence.Id, sequence.Name));
            }

            foreach (var target in _runTargets)
            {
                ddlScript.Items.Add(target);
            }

            if (!string.IsNullOrWhiteSpace(selectedId))
            {
                for (int i = 1; i < ddlScript.Items.Count; i++)
                {
                    if (ddlScript.Items[i] is RunTarget target && target.Id == selectedId)
                    {
                        ddlScript.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private async void btnRun_Click(object sender, EventArgs e)
        {
            if (_isRunning)
            {
                await StopRunAsync();
                return;
            }

            if (ddlScript.SelectedIndex <= 0)
            {
                MessageBox.Show("Select a script or sequence before run");
                return;
            }

            await StartRunAsync();
        }

        private async Task StartRunAsync()
        {
            RunTarget? target = ddlScript.SelectedItem as RunTarget ?? FindRunTargetByText(ddlScript.Text);
            if (target == null)
            {
                MessageBox.Show("Select a script or sequence before run");
                return;
            }

            ScriptModel? script = target.Kind == "script" ? _library.FindScriptById(target.Id) : null;
            SequenceModel? sequence = target.Kind == "sequence" ? _library.Sequences.FirstOrDefault(item => item.Id == target.Id) : null;
            if (script == null && sequence == null)
            {
                MessageBox.Show($"Missing config ({target.Name})");
                return;
            }

            _runCts = new CancellationTokenSource();
            taLog.Clear();
            SetRunningState(true);
            Text = $"{_baseTitle} - Running: {target.Name}";

            _runTask = RunSelectedTargetAsync(target, script, sequence, _runCts.Token);
            try
            {
                await _runTask;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Script run failed.", ex);
                ShowRunError(ex);
                WriteLog("ERROR: " + ex.Message);
            }
            finally
            {
                _runTask = null;
                SetRunningState(false);
                Text = _baseTitle;
            }
        }

        private async Task RunSelectedTargetAsync(RunTarget target, ScriptModel? script, SequenceModel? sequence, CancellationToken token)
        {
            var (offsetValue, offsetAxis) = GetSelectedOffset(target.Name);
            WriteLog($"OFFSET SELECTED {FormatOffset(offsetValue, offsetAxis)}");
            if (script != null)
            {
                await _runner.RunScriptAsync(script, offsetValue, offsetAxis, token, UpdateLiveStatus, IsAdbActionEnabled);
            }
            else if (sequence != null)
            {
                (offsetValue, offsetAxis) = GetSelectedOffset(target.Name);
                await _runner.RunSequenceAsync(
                    sequence,
                    _library,
                    offsetValue,
                    offsetAxis,
                    scriptItem => GetSelectedOffset(scriptItem.Name),
                    token,
                    UpdateLiveStatus,
                    IsAdbActionEnabled);
            }
        }

        private (int value, string axis) GetSelectedOffset(string scriptName)
        {
            string raw = ddlOffset.SelectedItem?.ToString() ?? "0";
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
                if (!_isRunning)
                {
                    btnRun.PerformClick();
                }
            }
            else if (action == HotkeyAction.StartOrStop)
            {
                btnRun.PerformClick();
            }
            else if (action == HotkeyAction.Stop)
            {
                _ = StopRunAsync();
            }
        }

        private async Task StopRunAsync()
        {
            if (!_isRunning)
            {
                return;
            }

            _runCts?.Cancel();
            try
            {
                if (_runTask != null)
                {
                    await _runTask;
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void SetRunningState(bool isRunning)
        {
            _isRunning = isRunning;
            ddlScript.Enabled = !isRunning;
            ddlOffset.Enabled = !isRunning;
            btnConfig.Enabled = !isRunning;
            btnRun.Text = isRunning ? "Stop" : "Run";
            SetTaskbarOverlayIcon(isRunning ? _runningIcon : _stoppedIcon, isRunning ? "Running" : "Stopped");

            if (isRunning)
            {
                UpdateLiveStatus(new LiveRunStatus { CurrentAction = "--", CurrentStep = "--", CurrentCycle = "--", NextAction = "--" });
            }
            else
            {
                UpdateLiveStatus(new LiveRunStatus { Idle = true });
            }
        }

        private void UpdateLiveStatus(LiveRunStatus status)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => UpdateLiveStatus(status)));
                return;
            }

            _liveStatus = status;
            UpdateLiveStatusLabels();
            WriteLog(status.CurrentAction);
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

        private static Icon CreateOverlayIcon(Color badgeColor, bool isRunning)
        {
            using Bitmap bitmap = new Bitmap(32, 32);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            int badgeSize = 30;
            int badgeX = 1;
            int badgeY = 1;
            var badgeBounds = new Rectangle(badgeX, badgeY, badgeSize - 1, badgeSize - 1);

            using var badgeBrush = new SolidBrush(badgeColor);
            using var borderPen = new Pen(Color.White, 3);
            graphics.FillEllipse(badgeBrush, badgeBounds);
            graphics.DrawEllipse(borderPen, badgeBounds);

            using var symbolBrush = new SolidBrush(Color.White);
            if (isRunning)
            {
                Point[] play =
                {
                    new Point(badgeX + badgeSize / 3, badgeY + badgeSize / 4),
                    new Point(badgeX + badgeSize / 3, badgeY + badgeSize * 3 / 4),
                    new Point(badgeX + badgeSize * 3 / 4, badgeY + badgeSize / 2)
                };
                graphics.FillPolygon(symbolBrush, play);
            }
            else
            {
                int squareSize = badgeSize / 3;
                int squareX = badgeX + (badgeSize - squareSize) / 2;
                int squareY = badgeY + (badgeSize - squareSize) / 2;
                graphics.FillRectangle(symbolBrush, squareX, squareY, squareSize, squareSize);
            }

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

            using var brush = new SolidBrush(_statusDotColor);
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

            if (_liveStatus.Idle)
            {
                lblCurrentActionValue.Text = "--";
                lblStepValue.Text = "--";
                lblCycleValue.Text = "--";
                lblNextActionValue.Text = "--";
                lblNextAtValue.Text = "--";
                lblEstimatedEndValue.Text = "--";
                return;
            }

            lblCurrentActionValue.Text = _liveStatus.CurrentAction;
            lblStepValue.Text = _liveStatus.CurrentStep;
            lblCycleValue.Text = _liveStatus.CurrentCycle;
            lblNextActionValue.Text = _liveStatus.NextAction;
            lblNextAtValue.Text = FormatStatusTime(_liveStatus.NextActionAt);
            lblEstimatedEndValue.Text = FormatStatusTime(_liveStatus.EstimatedEnd);
        }

        private void ShowRunError(Exception ex)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => ShowRunError(ex)));
                return;
            }

            _liveStatus = new LiveRunStatus
            {
                CurrentAction = "ERROR",
                CurrentStep = "--",
                CurrentCycle = "--",
                NextAction = "--",
                Idle = false
            };
            UpdateLiveStatusLabels();
            lblNextAtValue.Text = "Check ADB/device connection";
            lblEstimatedEndValue.Text = ShortenError(ex.Message);
            MessageBox.Show(ex.Message, "Lazy App Run Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void WriteLog(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => WriteLog(text)));
                return;
            }

            taLog.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + text + Environment.NewLine);
            taLog.ScrollToCaret();
        }

        private void UpdateHotkeyStatus(bool success)
        {
            _statusDotColor = success ? Color.Green : Color.Red;
            statusDot.Invalidate();
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            using var editor = new ConfigEditorForm(_configRepository);
            if (editor.ShowDialog(this) != DialogResult.OK || !editor.ConfigSaved)
            {
                return;
            }

            LoadConfig();
            _hotkeys.UnregisterAll(Handle);
            _hotkeys.Configure(_configRepository.Settings.HotkeyStart, _configRepository.Settings.HotkeyStop);
            _lastHotkeyRegistrationSucceeded = null;
            RegisterHotkeysForWindowState();
            WriteLog("CONFIG UPDATED.");
        }

        private void ResetOffsetSelection()
        {
            int zeroOffsetIndex = ddlOffset.FindStringExact("0");
            ddlOffset.SelectedIndex = zeroOffsetIndex >= 0 ? zeroOffsetIndex : -1;
        }

        private void ApplySelectedDefaultOffset()
        {
            ResetOffsetSelection();
            RunTarget? target = ddlScript.SelectedItem as RunTarget ?? FindRunTargetByText(ddlScript.Text);
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
            else
            {
                var sequence = _library.Sequences.FirstOrDefault(item => item.Id == target.Id);
                enabled = sequence?.DefaultOffsetEnabled == true;
                defaultOffset = sequence?.DefaultOffset ?? "0";
            }

            if (!enabled)
            {
                return;
            }

            int index = ddlOffset.FindStringExact(defaultOffset);
            if (index >= 0)
            {
                ddlOffset.SelectedIndex = index;
            }
        }

        private void ddlScript_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                RunTarget? target = FindRunTargetByText(ddlScript.Text);
                if (target != null)
                {
                    ddlScript.SelectedItem = target;
                    ApplySelectedDefaultOffset();
                    e.Handled = true;
                }
            }
        }

        private void ddlScript_TextChanged(object? sender, EventArgs e)
        {
            if (!_filteringRunTargets && ddlScript.Focused && ddlScript.SelectedItem is not RunTarget)
            {
                FilterRunTargetDropdown(ddlScript.Text);
            }
        }

        private void FilterRunTargetDropdown(string searchText)
        {
            string typed = searchText;
            var matches = _runTargets
                .Where(target => string.IsNullOrWhiteSpace(typed) || target.Name.Contains(typed, StringComparison.OrdinalIgnoreCase) || target.ToString().Contains(typed, StringComparison.OrdinalIgnoreCase))
                .Take(15)
                .Cast<object>()
                .ToArray();

            _filteringRunTargets = true;
            try
            {
                if (ddlScript.DroppedDown)
                {
                    ddlScript.DroppedDown = false;
                }

                ddlScript.BeginUpdate();
                ddlScript.Items.Clear();
                if (matches.Length > 0)
                {
                    ddlScript.Items.AddRange(matches);
                }
                else
                {
                    ddlScript.Items.Add(NoRunTargetMatchesText);
                }
                ddlScript.EndUpdate();
                ddlScript.SelectedIndex = -1;
                ddlScript.Text = typed;
                ddlScript.SelectionStart = ddlScript.Text.Length;
                ddlScript.SelectionLength = 0;
            }
            finally
            {
                _filteringRunTargets = false;
            }

            BeginInvoke((Action)(() => OpenFilteredDropdown(typed)));
        }

        private void OpenFilteredDropdown(string typedText)
        {
            if (!ddlScript.Focused || ddlScript.Text != typedText)
            {
                return;
            }

            ddlScript.DroppedDown = ddlScript.Items.Count > 0;
            RestoreMousePointer();
            RestoreMousePointerAfterDropdownPaint();
        }

        private static void RestoreMousePointer()
        {
            while (ShowCursor(true) < 0)
            {
            }

            var position = Cursor.Position;
            int nudgeX = position.X > 0 ? position.X - 1 : position.X + 1;
            Cursor.Position = new Point(nudgeX, position.Y);
            Cursor.Current = Cursors.Default;
            Cursor.Position = position;
        }

        private void RestoreMousePointerAfterDropdownPaint()
        {
            var timer = new System.Windows.Forms.Timer { Interval = 80 };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                timer.Dispose();
                RestoreMousePointer();
            };
            timer.Start();
        }

        private RunTarget? FindRunTargetByText(string text)
        {
            string normalized = text.Trim();
            return _runTargets.FirstOrDefault(target =>
                target.ToString().Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
                target.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase));
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
            _hotkeys.UnregisterAll(Handle);
            SetTaskbarOverlayIcon(null, "");
            _clockTimer.Stop();
            _clockTimer.Dispose();
            _runningIcon.Dispose();
            _stoppedIcon.Dispose();
            _baseIcon.Dispose();
            _runCts?.Cancel();
        }

        private sealed record RunTarget(string Kind, string Id, string Name)
        {
            public override string ToString()
            {
                return Kind == "sequence" ? "[Q] " + Name : "[S] " + Name;
            }
        }
    }
}
