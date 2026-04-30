namespace Lazy_App_Codex_Core
{
    public partial class Form1 : Form
    {
        private readonly HotkeyManager _hotkeys = new HotkeyManager();
        private readonly ScriptConfigRepository _configRepository = new ScriptConfigRepository("config.json");
        private readonly ScriptRunner _runner = new ScriptRunner();

        private Dictionary<string, ScriptModel> _scripts = new Dictionary<string, ScriptModel>();
        private CancellationTokenSource _runCts;
        private Task _runTask;
        private bool _isRunning;
        private bool? _lastHotkeyRegistrationSucceeded;

        private static bool IsAdbActionEnabled = true;

        public Form1()
        {
            InitializeComponent();

            Load += OnLoad;
            Activated += OnActivated;
            Resize += OnResize;

            ddlOffset.SelectedIndex = 2;

            LoadConfig();
            _hotkeys.Configure(_configRepository.Settings.HotkeyStartStopToggle, _configRepository.Settings.HotkeyStop);
            SetRunningState(false);
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
            RegisterHotkeysForWindowState();
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
            }

            _lastHotkeyRegistrationSucceeded = success;
        }

        private void LoadConfig()
        {
            _scripts = _configRepository.Load();

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
                UpdateLabelStatus("ERROR: " + ex.Message, Color.Red);
            }
            finally
            {
                _runTask = null;
                SetRunningState(false);
            }
        }

        private async Task RunSelectedScriptAsync(string scriptName, ScriptModel script, CancellationToken token)
        {
            var (offsetValue, offsetAxis) = GetSelectedOffset(scriptName);
            WriteLog($"OFFSET SELECTED {FormatOffset(offsetValue, offsetAxis)}");
            await _runner.RunAsync(script, offsetValue, offsetAxis, token, UpdateLabelStatus, IsAdbActionEnabled);
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

            if (isRunning)
            {
                UpdateLabelStatus("CLICKING NOW", Color.Red);
            }
            else
            {
                UpdateLabelStatus("STOP WORKING", Color.Blue);
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
            WriteLog(text);
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
            _runCts?.Cancel();
        }
    }
}
