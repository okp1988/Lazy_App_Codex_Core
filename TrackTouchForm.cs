using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Lazy_App_Codex_Core
{
    public sealed class TrackTouchForm : Form
    {
        private const int DragThresholdPixels = 12;

        private readonly string _adbPath;
        private readonly Func<AdbDeviceStatus?> _getAdbStatus;
        private readonly Func<string, string> _getDeviceDisplayName;
        private readonly ComboBox _deviceBox = new();
        private readonly Label _livePositionLabel = new();
        private readonly Label _gestureLabel = new();
        private readonly Label _mappingLabel = new();
        private readonly Label _statusLabel = new();
        private readonly DataGridView _historyGrid = new();
        private readonly NumericUpDown _testXBox = CreateCoordinateBox();
        private readonly NumericUpDown _testYBox = CreateCoordinateBox();
        private readonly Button _testButton = new();
        private readonly Button _clearButton = new();
        private readonly System.Windows.Forms.Timer _deviceStateTimer = new();

        private AdbShellController _adb;
        private Process? _trackProcess;
        private Dictionary<string, TouchCoordinateMapper> _mappers = new(StringComparer.OrdinalIgnoreCase);
        private TouchCoordinateMapper? _activeMapper;
        private string _activeDevice = "";
        private int? _rawX;
        private int? _rawY;
        private Point? _touchStart;
        private Point? _latestPoint;
        private bool _touchActive;
        private bool _releasePending;
        private bool _gestureMoved;
        private int _historyNumber;
        private bool _closing;
        private bool _changingDevice;
        private string _selectedSerial;

        public TrackTouchForm(
            string adbPath,
            string deviceSerial,
            Func<AdbDeviceStatus?> getAdbStatus,
            Func<string, string> getDeviceDisplayName)
        {
            _adbPath = adbPath;
            _selectedSerial = deviceSerial;
            _adb = new AdbShellController(adbPath, deviceSerial);
            _getAdbStatus = getAdbStatus;
            _getDeviceDisplayName = getDeviceDisplayName;

            Text = "Track Touch - " + _getDeviceDisplayName(deviceSerial);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(620, 470);
            ClientSize = new Size(720, 520);
            ShowIcon = false;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);

            BuildLayout();
            RefreshDeviceChoices(deviceSerial);
            Shown += async (_, _) => await StartTrackingAsync();
            _deviceStateTimer.Interval = 1000;
            _deviceStateTimer.Tick += (_, _) => CheckDeviceState();
            _deviceStateTimer.Start();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _closing = true;
            _deviceStateTimer.Stop();
            StopTracking();
            base.OnFormClosing(e);
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 6
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            var devicePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            devicePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            devicePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            devicePanel.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Device", TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            _deviceBox.Dock = DockStyle.Fill;
            _deviceBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _deviceBox.SelectedIndexChanged += async (_, _) => await ChangeSelectedDeviceAsync();
            devicePanel.Controls.Add(_deviceBox, 1, 0);

            var livePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            livePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            livePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            livePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            livePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

            _livePositionLabel.Dock = DockStyle.Fill;
            _livePositionLabel.Font = new Font(Font.FontFamily, 16F, FontStyle.Bold);
            _livePositionLabel.Text = "Live: --, --";
            _livePositionLabel.TextAlign = ContentAlignment.MiddleLeft;
            _gestureLabel.Dock = DockStyle.Fill;
            _gestureLabel.Font = new Font(Font, FontStyle.Bold);
            _gestureLabel.Text = "Gesture: Waiting";
            _gestureLabel.TextAlign = ContentAlignment.MiddleLeft;
            _mappingLabel.Dock = DockStyle.Fill;
            _mappingLabel.AutoEllipsis = true;
            _mappingLabel.ForeColor = SystemColors.GrayText;
            _mappingLabel.Text = "Loading screen and touch ranges...";
            _mappingLabel.TextAlign = ContentAlignment.MiddleLeft;

            livePanel.Controls.Add(_livePositionLabel, 0, 0);
            livePanel.Controls.Add(_gestureLabel, 1, 0);
            livePanel.Controls.Add(_mappingLabel, 0, 1);
            livePanel.SetColumnSpan(_mappingLabel, 2);

            var historyHeader = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            historyHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            historyHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            var historyLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Point History (completed taps only)",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Font, FontStyle.Bold)
            };
            ConfigureButton(_clearButton, "Clear", (_, _) => ClearHistory());
            historyHeader.Controls.Add(historyLabel, 0, 0);
            historyHeader.Controls.Add(_clearButton, 1, 0);

            ConfigureHistoryGrid();

            var testPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7, Padding = new Padding(0, 8, 0, 4) };
            testPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22));
            testPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            testPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28));
            testPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            testPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            testPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            testPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1));
            testPanel.Controls.Add(CreateTestLabel("X"), 0, 0);
            testPanel.Controls.Add(_testXBox, 1, 0);
            testPanel.Controls.Add(CreateTestLabel("Y"), 2, 0);
            testPanel.Controls.Add(_testYBox, 3, 0);
            ConfigureButton(_testButton, "Test Tap", async (_, _) => await TestTapAsync());
            testPanel.Controls.Add(_testButton, 4, 0);

            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.AutoEllipsis = true;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusLabel.Text = "Starting touch tracking...";

            root.Controls.Add(devicePanel, 0, 0);
            root.Controls.Add(livePanel, 0, 1);
            root.Controls.Add(historyHeader, 0, 2);
            root.Controls.Add(_historyGrid, 0, 3);
            root.Controls.Add(testPanel, 0, 4);
            root.Controls.Add(_statusLabel, 0, 5);
            Controls.Add(root);
        }

        private void ConfigureHistoryGrid()
        {
            _historyGrid.Dock = DockStyle.Fill;
            _historyGrid.AllowUserToAddRows = false;
            _historyGrid.AllowUserToDeleteRows = false;
            _historyGrid.AllowUserToResizeRows = false;
            _historyGrid.ReadOnly = true;
            _historyGrid.MultiSelect = false;
            _historyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _historyGrid.RowHeadersVisible = false;
            _historyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _historyGrid.Columns.Add("number", "#");
            _historyGrid.Columns.Add("x", "X");
            _historyGrid.Columns.Add("y", "Y");
            _historyGrid.Columns.Add("device", "Device");
            _historyGrid.Columns.Add("time", "Time");
            _historyGrid.Columns["number"].FillWeight = 35;
            _historyGrid.Columns["x"].FillWeight = 60;
            _historyGrid.Columns["y"].FillWeight = 60;
            _historyGrid.Columns["device"].FillWeight = 120;
            _historyGrid.Columns["time"].FillWeight = 100;
            _historyGrid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex < 0)
                {
                    return;
                }

                _testXBox.Value = Convert.ToDecimal(_historyGrid.Rows[e.RowIndex].Cells["x"].Value, CultureInfo.InvariantCulture);
                _testYBox.Value = Convert.ToDecimal(_historyGrid.Rows[e.RowIndex].Cells["y"].Value, CultureInfo.InvariantCulture);
            };
        }

        private async Task StartTrackingAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(7000);
                var (width, height) = await GetEffectiveDisplaySizeAsync(cts.Token);
                var (exitCode, output, error) = await _adb.RunCaptureAsync("shell getevent -lp", cts.Token, 6000);
                if (exitCode != 0)
                {
                    SetError("Unable to read touch ranges. " + CleanAdbMessage(error, output));
                    return;
                }

                _mappers = CreateTouchMappers(output, width, height);
                if (_mappers.Count == 0)
                {
                    SetError($"No usable touchscreen range was found for screen {width}x{height}.");
                    return;
                }

                _mappingLabel.Text = $"Screen: {width}x{height} | Touch devices: {_mappers.Count}";
                StartGetEventProcess();
            }
            catch (OperationCanceledException)
            {
                SetError("Timed out while reading the device screen or touch ranges.");
            }
            catch (Exception ex)
            {
                SetError("Failed to start Track Touch. " + ex.Message);
            }
        }

        private async Task ChangeSelectedDeviceAsync()
        {
            if (_changingDevice || _deviceBox.SelectedItem is not DeviceChoice choice ||
                choice.Serial.Equals(_selectedSerial, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _changingDevice = true;
            try
            {
                StopTracking();
                ResetLiveTrackingState();
                _selectedSerial = choice.Serial;
                _adb = new AdbShellController(_adbPath, _selectedSerial);
                Text = "Track Touch - " + _getDeviceDisplayName(_selectedSerial);
                _statusLabel.ForeColor = SystemColors.ControlText;
                _statusLabel.Text = "Switching device and loading touch ranges...";
                await StartTrackingAsync();
            }
            finally
            {
                _changingDevice = false;
            }
        }

        private void StartGetEventProcess()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _adb.AdbPath,
                    Arguments = _adb.DeviceSelector + "shell getevent -l",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null)
                {
                    PostUi(() => HandleProcessEnded(process));
                    return;
                }

                HandleGetEventLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    AppLogger.LogWarning("Track touch stderr: " + e.Data);
                }
            };
            process.Exited += (_, _) => PostUi(() => HandleProcessEnded(process));

            if (!process.Start())
            {
                process.Dispose();
                SetError("Failed to start adb shell getevent -l.");
                return;
            }

            _trackProcess = process;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            _statusLabel.ForeColor = SystemColors.ControlText;
            _statusLabel.Text = "Tracking is active. Touch or drag on the phone screen.";
        }

        private void HandleGetEventLine(string line)
        {
            string? devicePath = ReadGetEventDevicePath(line);
            if (devicePath != null)
            {
                if (!_mappers.TryGetValue(devicePath, out var mapper))
                {
                    return;
                }

                _activeMapper = mapper;
                _activeDevice = devicePath;
            }

            if (TryReadGetEventValue(line, "ABS_MT_POSITION_X", out int x) || TryReadGetEventValue(line, "ABS_X", out x))
            {
                _rawX = x;
            }

            if (TryReadGetEventValue(line, "ABS_MT_POSITION_Y", out int y) || TryReadGetEventValue(line, "ABS_Y", out y))
            {
                _rawY = y;
            }

            if (line.Contains("ABS_MT_TRACKING_ID", StringComparison.OrdinalIgnoreCase) && TryReadLastEventToken(line, out string trackingToken))
            {
                if (IsReleaseToken(trackingToken))
                {
                    _releasePending = true;
                }
                else
                {
                    BeginGesture();
                }
            }
            else if (line.Contains("BTN_TOUCH", StringComparison.OrdinalIgnoreCase) && TryReadLastEventToken(line, out string touchToken))
            {
                if (touchToken.Equals("UP", StringComparison.OrdinalIgnoreCase) || touchToken == "00000000")
                {
                    _releasePending = true;
                }
                else if (touchToken.Equals("DOWN", StringComparison.OrdinalIgnoreCase) || touchToken == "00000001")
                {
                    BeginGesture();
                }
            }

            if (!line.Contains("SYN_REPORT", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            PublishMappedPosition();
            if (_releasePending)
            {
                CompleteGesture();
            }
        }

        private void BeginGesture()
        {
            if (_touchActive)
            {
                return;
            }

            _touchActive = true;
            _releasePending = false;
            _gestureMoved = false;
            _touchStart = null;
            _latestPoint = null;
        }

        private void PublishMappedPosition()
        {
            if (!_rawX.HasValue || !_rawY.HasValue || _activeMapper == null)
            {
                return;
            }

            Point point = _activeMapper.Map(_rawX.Value, _rawY.Value);
            _latestPoint = point;
            if (_touchActive && !_touchStart.HasValue)
            {
                _touchStart = point;
            }

            if (_touchStart.HasValue && (Math.Abs(point.X - _touchStart.Value.X) > DragThresholdPixels || Math.Abs(point.Y - _touchStart.Value.Y) > DragThresholdPixels))
            {
                _gestureMoved = true;
            }

            string gesture = !_touchActive ? "Hover" : _gestureMoved ? "Drag" : "Point";
            string mapping = $"Device: {_activeDevice} | Raw: {_rawX.Value}, {_rawY.Value} | Range: X {_activeMapper.MinX}-{_activeMapper.MaxX}, Y {_activeMapper.MinY}-{_activeMapper.MaxY} | Screen: {_activeMapper.ScreenWidth}x{_activeMapper.ScreenHeight}";
            PostUi(() =>
            {
                _livePositionLabel.Text = $"Live: {point.X}, {point.Y}";
                _gestureLabel.Text = "Gesture: " + gesture;
                _mappingLabel.Text = mapping;
            });
        }

        private void CompleteGesture()
        {
            bool recordPoint = _touchActive && !_gestureMoved && _latestPoint.HasValue;
            Point point = _latestPoint.GetValueOrDefault();
            string deviceName = _getDeviceDisplayName(_selectedSerial);
            _touchActive = false;
            _releasePending = false;
            _touchStart = null;
            _latestPoint = null;

            PostUi(() =>
            {
                _gestureLabel.Text = recordPoint ? "Gesture: Point recorded" : "Gesture: Drag (not recorded)";
                if (recordPoint)
                {
                    _historyNumber++;
                    int rowIndex = _historyGrid.Rows.Add(_historyNumber, point.X, point.Y, deviceName, DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
                    _historyGrid.FirstDisplayedScrollingRowIndex = rowIndex;
                    _testXBox.Value = Math.Clamp(point.X, (int)_testXBox.Minimum, (int)_testXBox.Maximum);
                    _testYBox.Value = Math.Clamp(point.Y, (int)_testYBox.Minimum, (int)_testYBox.Maximum);
                }
            });
        }

        private async Task TestTapAsync()
        {
            if (!IsSelectedDeviceReady())
            {
                SetError("The selected ADB device is no longer ready.");
                return;
            }

            int x = (int)_testXBox.Value;
            int y = (int)_testYBox.Value;
            _testButton.Enabled = false;
            _statusLabel.ForeColor = SystemColors.ControlText;
            _statusLabel.Text = $"Testing tap at X {x}, Y {y}...";
            try
            {
                using var cts = new CancellationTokenSource(8000);
                var (exitCode, output, error) = await _adb.RunCaptureAsync($"shell input tap {x} {y}", cts.Token, 7000);
                if (exitCode != 0)
                {
                    SetError("Test tap failed. " + CleanAdbMessage(error, output));
                    return;
                }

                _statusLabel.ForeColor = Color.DarkGreen;
                _statusLabel.Text = $"Test tap sent at X {x}, Y {y}.";
            }
            catch (Exception ex)
            {
                SetError("Test tap failed. " + ex.Message);
            }
            finally
            {
                _testButton.Enabled = IsSelectedDeviceReady();
            }
        }

        private void CheckDeviceState()
        {
            RefreshDeviceChoices(_selectedSerial);
            if (IsSelectedDeviceReady())
            {
                return;
            }

            StopTracking();
            _testButton.Enabled = false;
            SetError("Tracking stopped because the selected ADB device is no longer ready.");
        }

        private bool IsSelectedDeviceReady()
        {
            return (_getAdbStatus()?.Devices ?? Array.Empty<AdbTrackedDevice>()).Any(device =>
                device.IsReady && device.Serial.Equals(_selectedSerial, StringComparison.OrdinalIgnoreCase));
        }

        private void RefreshDeviceChoices(string? preferredSerial)
        {
            var readySerials = (_getAdbStatus()?.Devices ?? Array.Empty<AdbTrackedDevice>())
                .Where(device => device.IsReady)
                .Select(device => device.Serial)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(serial => serial, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var readyChoices = readySerials
                .Select(serial => new DeviceChoice(serial, _getDeviceDisplayName(serial)))
                .ToList();
            var displayedChoices = _deviceBox.Items.Cast<DeviceChoice>().ToList();
            if (readyChoices.SequenceEqual(displayedChoices))
            {
                return;
            }

            _changingDevice = true;
            try
            {
                string desired = preferredSerial ?? _selectedSerial;
                _deviceBox.BeginUpdate();
                _deviceBox.Items.Clear();
                foreach (var choice in readyChoices)
                {
                    _deviceBox.Items.Add(choice);
                }

                _deviceBox.SelectedItem = _deviceBox.Items.Cast<DeviceChoice>()
                    .FirstOrDefault(item => item.Serial.Equals(desired, StringComparison.OrdinalIgnoreCase));
                _deviceBox.EndUpdate();
            }
            finally
            {
                _changingDevice = false;
            }
        }

        private void ResetLiveTrackingState()
        {
            _mappers.Clear();
            _activeMapper = null;
            _activeDevice = "";
            _rawX = null;
            _rawY = null;
            _touchStart = null;
            _latestPoint = null;
            _touchActive = false;
            _releasePending = false;
            _gestureMoved = false;
            _livePositionLabel.Text = "Live: --, --";
            _gestureLabel.Text = "Gesture: Waiting";
            _mappingLabel.Text = "Loading screen and touch ranges...";
        }

        private void HandleProcessEnded(Process process)
        {
            if (!ReferenceEquals(_trackProcess, process))
            {
                return;
            }

            StopWithError("Touch tracking ended.");
        }

        private void StopWithError(string message)
        {
            if (_closing)
            {
                return;
            }

            StopTracking();
            SetError(message);
        }

        private void StopTracking()
        {
            var process = _trackProcess;
            _trackProcess = null;
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

        private void ClearHistory()
        {
            _historyGrid.Rows.Clear();
            _historyNumber = 0;
        }

        private void SetError(string message)
        {
            if (IsDisposed)
            {
                return;
            }

            _statusLabel.ForeColor = Color.DarkRed;
            _statusLabel.Text = message;
        }

        private void PostUi(Action action)
        {
            if (_closing || IsDisposed || !IsHandleCreated)
            {
                return;
            }

            try
            {
                BeginInvoke(action);
            }
            catch (InvalidOperationException)
            {
            }
        }

        private async Task<(int width, int height)> GetEffectiveDisplaySizeAsync(CancellationToken token)
        {
            var (exitCode, output, _) = await _adb.RunCaptureAsync("shell wm size", token, 6000);
            if (exitCode == 0)
            {
                if (TryReadDisplaySize(output, "Override size", out int overrideWidth, out int overrideHeight))
                {
                    return (overrideWidth, overrideHeight);
                }

                if (TryReadDisplaySize(output, "Physical size", out int physicalWidth, out int physicalHeight))
                {
                    return (physicalWidth, physicalHeight);
                }
            }

            return await _adb.GetDeviceSizeAsync(token);
        }

        private static bool TryReadDisplaySize(string output, string label, out int width, out int height)
        {
            width = height = 0;
            Match match = Regex.Match(output, Regex.Escape(label) + @"\s*:\s*(\d+)x(\d+)", RegexOptions.IgnoreCase);
            return match.Success &&
                int.TryParse(match.Groups[1].Value, out width) &&
                int.TryParse(match.Groups[2].Value, out height) &&
                width > 0 && height > 0;
        }

        private static Dictionary<string, TouchCoordinateMapper> CreateTouchMappers(string output, int screenWidth, int screenHeight)
        {
            var ranges = new Dictionary<string, TouchRangeBuilder>(StringComparer.OrdinalIgnoreCase);
            string? currentDevice = null;

            foreach (string line in output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                string? devicePath = ReadGetEventDevicePath(line);
                if (devicePath != null)
                {
                    currentDevice = devicePath;
                    ranges.TryAdd(currentDevice, new TouchRangeBuilder());
                    continue;
                }

                if (currentDevice == null)
                {
                    continue;
                }

                var range = ranges[currentDevice];
                if (line.Contains("ABS_MT_POSITION_X", StringComparison.OrdinalIgnoreCase) || line.Contains("ABS_X", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryReadAbsRange(line, out int min, out int max))
                    {
                        range.MinX = min;
                        range.MaxX = max;
                    }
                }
                else if (line.Contains("ABS_MT_POSITION_Y", StringComparison.OrdinalIgnoreCase) || line.Contains("ABS_Y", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryReadAbsRange(line, out int min, out int max))
                    {
                        range.MinY = min;
                        range.MaxY = max;
                    }
                }
            }

            return ranges
                .Where(item => item.Value.IsComplete)
                .ToDictionary(
                    item => item.Key,
                    item => new TouchCoordinateMapper(item.Value.MinX!.Value, item.Value.MaxX!.Value, item.Value.MinY!.Value, item.Value.MaxY!.Value, screenWidth, screenHeight),
                    StringComparer.OrdinalIgnoreCase);
        }

        private static string? ReadGetEventDevicePath(string line)
        {
            Match match = Regex.Match(line, @"/dev/input/event\d+", RegexOptions.IgnoreCase);
            return match.Success ? match.Value : null;
        }

        private static bool TryReadAbsRange(string line, out int minimum, out int maximum)
        {
            minimum = maximum = 0;
            Match match = Regex.Match(line, @"min\s+(-?\d+),\s*max\s+(-?\d+)", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out minimum) && int.TryParse(match.Groups[2].Value, out maximum);
        }

        private static bool TryReadGetEventValue(string line, string key, out int value)
        {
            value = 0;
            int keyIndex = line.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (keyIndex < 0)
            {
                return false;
            }

            string tail = line[(keyIndex + key.Length)..].Trim();
            string token = tail.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            return token.Length > 0 && (int.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value) || int.TryParse(token, out value));
        }

        private static bool TryReadLastEventToken(string line, out string token)
        {
            token = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "";
            return token.Length > 0;
        }

        private static bool IsReleaseToken(string token)
        {
            return token.Equals("ffffffff", StringComparison.OrdinalIgnoreCase) || token.Equals("ffffffffffffffff", StringComparison.OrdinalIgnoreCase) || token == "-1";
        }

        private static string CleanAdbMessage(string primary, string secondary)
        {
            string message = string.IsNullOrWhiteSpace(primary) ? secondary : primary;
            return string.IsNullOrWhiteSpace(message) ? "No ADB error output." : message.Trim();
        }

        private static NumericUpDown CreateCoordinateBox()
        {
            return new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100000 };
        }

        private static Label CreateTestLabel(string text)
        {
            return new Label { Dock = DockStyle.Fill, Text = text, TextAlign = ContentAlignment.MiddleLeft };
        }

        private static void ConfigureButton(Button button, string text, EventHandler handler)
        {
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(5, 0, 5, 0);
            button.Text = text;
            button.UseVisualStyleBackColor = true;
            button.Click += handler;
        }

        private sealed record TouchCoordinateMapper(int MinX, int MaxX, int MinY, int MaxY, int ScreenWidth, int ScreenHeight)
        {
            public Point Map(int rawX, int rawY)
            {
                return new Point(Scale(rawX, MinX, MaxX, ScreenWidth), Scale(rawY, MinY, MaxY, ScreenHeight));
            }

            private static int Scale(int raw, int minimum, int maximum, int size)
            {
                double ratio = (raw - minimum) / (double)(maximum - minimum);
                ratio = Math.Clamp(ratio, 0D, 1D);
                return (int)Math.Round(ratio * Math.Max(0, size - 1));
            }
        }

        private sealed class TouchRangeBuilder
        {
            public int? MinX { get; set; }
            public int? MaxX { get; set; }
            public int? MinY { get; set; }
            public int? MaxY { get; set; }
            public bool IsComplete => MinX.HasValue && MaxX > MinX && MinY.HasValue && MaxY > MinY;
        }

        private sealed record DeviceChoice(string Serial, string DisplayName)
        {
            public override string ToString() => DisplayName;
        }
    }
}
