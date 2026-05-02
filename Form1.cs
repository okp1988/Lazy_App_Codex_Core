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

        private Dictionary<string, ScriptModel> _scripts = new Dictionary<string, ScriptModel>();
        private CancellationTokenSource _runCts;
        private Task _runTask;
        private bool _isRunning;
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
            splitContainer1.Panel1.Resize += (_, _) => ApplyResponsiveLayout();

            ddlOffset.SelectedIndex = 2;

            LoadConfig();
            _hotkeys.Configure(_configRepository.Settings.HotkeyStartStopToggle, _configRepository.Settings.HotkeyStop);
            _clockTimer.Interval = 1000;
            _clockTimer.Tick += (_, _) => UpdateCurrentTimeLabel();
            _clockTimer.Start();
            UpdateCurrentTimeLabel();
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
            if (keyData == Keys.F3)
            {
                HandleHotkeyAction(HotkeyAction.Toggle);
                return true;
            }

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
            int statusGap = 0;
            int rightEdge = Math.Max(leftMargin + 120, panelWidth - rightMargin);
            int statusIndicatorLeft = Math.Max(leftMargin + 120, rightEdge - btnStatus.Width);
            int contentWidth = Math.Max(120, rightEdge - leftMargin);

            btnStatus.Left = statusIndicatorLeft;
            lblStatus.Width = Math.Max(120, statusIndicatorLeft - leftMargin - statusGap);
            ddlScript.Width = contentWidth;
            lblCircleTiming.Width = contentWidth;
            lblCurrentTime.Width = contentWidth;
        }

        private void RegisterHotkeysForWindowState()
        {
            bool success = _hotkeys.Register(Handle);
            UpdateHotkeyStatus(success);

            if (success && _lastHotkeyRegistrationSucceeded != true)
            {
                WriteLog($"GLOBAL HOTKEY REGISTERED ({_hotkeys.ToggleHotkeyText}).");
            }

            if (!success && _lastHotkeyRegistrationSucceeded != false)
            {
                WriteLog($"GLOBAL HOTKEY NOT REGISTERED ({_hotkeys.ToggleHotkeyText}). F3 still works while this window is focused.");
                AppLogger.LogWarning($"Global hotkey was not registered ({_hotkeys.ToggleHotkeyText}).");
            }

            _lastHotkeyRegistrationSucceeded = success;
        }

        private void LoadConfig()
        {
            try
            {
                _scripts = _configRepository.Load();
            }
            catch (Exception ex)
            {
                _scripts = new Dictionary<string, ScriptModel>();
                AppLogger.LogError("Failed to load script configuration.", ex);
                MessageBox.Show("Failed to load config.json. Please check the logs folder.", "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            ddlScript.Items.Clear();
            ddlScript.Items.Insert(0, "Choose a script");
            ddlScript.SelectedIndex = 0;

            foreach (var key in _scripts.Keys)
            {
                ddlScript.Items.Add(key);
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
                MessageBox.Show("Select a script before run");
                return;
            }

            await StartRunAsync();
        }

        private async Task StartRunAsync()
        {
            string selectedScriptName = ddlScript.SelectedItem.ToString();
            if (!_scripts.ContainsKey(selectedScriptName))
            {
                MessageBox.Show($"Missing config ({selectedScriptName})");
                return;
            }

            ScriptModel selectedScript = _scripts[selectedScriptName];
            _runCts = new CancellationTokenSource();
            taLog.Clear();
            SetRunningState(true);
            Text = $"{_baseTitle} - Running: {selectedScriptName}";

            _runTask = RunSelectedScriptAsync(selectedScriptName, selectedScript, _runCts.Token);
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
                UpdateLabelStatus("ERROR: " + ex.Message, Color.Red);
            }
            finally
            {
                _runTask = null;
                SetRunningState(false);
                Text = _baseTitle;
            }
        }

        private async Task RunSelectedScriptAsync(string scriptName, ScriptModel script, CancellationToken token)
        {
            var (offsetValue, offsetAxis) = GetSelectedOffset(scriptName);
            WriteLog($"OFFSET SELECTED {FormatOffset(offsetValue, offsetAxis)}");
            await _runner.RunAsync(script, offsetValue, offsetAxis, token, UpdateLabelStatus, UpdateCircleTimingLabel, IsAdbActionEnabled);
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
            if (action == HotkeyAction.Toggle)
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
            btnRun.Text = isRunning ? "Stop" : "Run (F3)";
            SetTaskbarOverlayIcon(isRunning ? _runningIcon : _stoppedIcon, isRunning ? "Running" : "Stopped");

            if (isRunning)
            {
                UpdateLabelStatus("CLICKING NOW", Color.Red);
                UpdateCircleTimingLabel("Circle: -- | Time: generating... | End: --");
            }
            else
            {
                UpdateLabelStatus("STOP WORKING", Color.Blue);
                UpdateCircleTimingLabel("Circle: -- | Time: -- | End: --");
            }
        }

        private void UpdateLabelStatus(string text, Color color)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => UpdateLabelStatus(text, color)));
                return;
            }

            lblStatus.Text = text;
            lblStatus.ForeColor = color;
            lblStatus.Invalidate();
            WriteLog(text);
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

        private void lblStatus_Paint(object? sender, PaintEventArgs e)
        {
            using var borderPen = new Pen(lblStatus.ForeColor, 2);
            var borderBounds = new Rectangle(0, 0, lblStatus.Width - 1, lblStatus.Height - 1);
            e.Graphics.DrawRectangle(borderPen, borderBounds);
        }

        private void UpdateCircleTimingLabel(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => UpdateCircleTimingLabel(text)));
                return;
            }

            lblCircleTiming.Text = text;
        }

        private void UpdateCurrentTimeLabel()
        {
            lblCurrentTime.Text = "Current time: " + DateTime.Now.ToString("HH:mm:ss");
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
            btnStatus.BackColor = success ? Color.Green : Color.Red;
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
    }
}
