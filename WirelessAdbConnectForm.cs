using Newtonsoft.Json.Linq;

namespace Lazy_App_Codex_Core
{
    public sealed class WirelessAdbConnectForm : Form
    {
        private readonly ScriptConfigRepository _repository;
        private readonly AdbShellController _adbController;
        private readonly ComboBox _actionBox = new();
        private readonly ComboBox _deviceBox = new();
        private readonly IpAddressBox _ipBox = new();
        private readonly TextBox _portBox = new();
        private readonly TextBox _pairCodeBox = new();
        private readonly Button _tryConnectButton = new();
        private readonly Button _restartServerButton = new();
        private readonly Label _pairCodeLabel = new();
        private readonly Label _statusLabel = new();
        private readonly List<DeviceChoice> _devices = new();

        public WirelessAdbConnectForm(ScriptConfigRepository repository, AdbShellController adbController)
        {
            _repository = repository;
            _adbController = adbController;

            Text = "Wireless ADB";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(340, 330);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);

            BuildLayout();
            AcceptButton = _tryConnectButton;
            LoadDevices();
            UpdateActionState();
        }

        public bool ConfigChanged { get; private set; }
        public bool ServerRestarted { get; private set; }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void BuildLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 7,
                Padding = new Padding(14),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));

            ConfigureCombo(_actionBox);
            _actionBox.Items.AddRange(new object[] { "Connect", "Pair" });
            _actionBox.SelectedIndex = 0;
            _actionBox.SelectedIndexChanged += (_, _) => UpdateActionState();

            ConfigureCombo(_deviceBox);
            _deviceBox.SelectedIndexChanged += (_, _) => ApplySelectedDeviceIp();

            _ipBox.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            _ipBox.Size = new Size(188, 28);

            ConfigureNumericTextBox(_portBox);
            ConfigureNumericTextBox(_pairCodeBox);

            _tryConnectButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            _tryConnectButton.Margin = new Padding(4, 4, 0, 0);
            _tryConnectButton.Size = new Size(80, 30);
            _tryConnectButton.Text = "Try";
            _tryConnectButton.UseVisualStyleBackColor = true;
            _tryConnectButton.Click += async (_, _) => await TryConnectAsync();

            _restartServerButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            _restartServerButton.Margin = new Padding(92, 4, 0, 0);
            _restartServerButton.Size = new Size(96, 30);
            _restartServerButton.Text = "Restart";
            _restartServerButton.UseVisualStyleBackColor = true;
            _restartServerButton.Click += async (_, _) => await RestartAdbServerAsync();

            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.AutoEllipsis = true;
            _statusLabel.TextAlign = ContentAlignment.TopLeft;

            AddRow(layout, 0, "Action", _actionBox);
            AddRow(layout, 1, "Device", _deviceBox);
            AddRow(layout, 2, "IP", _ipBox);
            AddRow(layout, 3, "Port", _portBox);
            AddRow(layout, 4, _pairCodeLabel, _pairCodeBox);
            layout.Controls.Add(new Label(), 0, 5);
            layout.Controls.Add(_tryConnectButton, 1, 5);
            layout.Controls.Add(_restartServerButton, 1, 5);
            layout.Controls.Add(_statusLabel, 0, 6);
            layout.SetColumnSpan(_statusLabel, 3);

            Controls.Add(layout);
        }

        private static void ConfigureCombo(ComboBox combo)
        {
            combo.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            combo.Width = 188;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.FormattingEnabled = true;
        }

        private static void ConfigureNumericTextBox(TextBox textBox)
        {
            textBox.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            textBox.Width = 188;
            textBox.KeyPress += (_, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            };
        }

        private static void AddRow(TableLayoutPanel layout, int row, string label, Control input)
        {
            AddRow(layout, row, new Label
            {
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                AutoSize = false,
                Height = 24,
                Text = label,
                TextAlign = ContentAlignment.MiddleRight
            }, input);
        }

        private static void AddRow(TableLayoutPanel layout, int row, Label label, Control input)
        {
            label.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            label.AutoSize = false;
            label.Height = 24;
            label.TextAlign = ContentAlignment.MiddleRight;
            label.Margin = new Padding(0, 4, 10, 0);
            input.Margin = new Padding(0, 4, 0, 6);
            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(input, 1, row);
        }

        private void LoadDevices()
        {
            _repository.LoadLibrary();
            _devices.Clear();
            _deviceBox.BeginUpdate();
            _deviceBox.Items.Clear();
            _deviceBox.Items.Add("Manual Input");

            foreach (var item in _repository.Settings.Devices.OrderBy(item => GetDeviceLabel(item.Key, item.Value), StringComparer.OrdinalIgnoreCase))
            {
                if (!TryGetSavedDeviceIp(item.Key, item.Value, out _))
                {
                    continue;
                }

                var choice = new DeviceChoice(item.Key, item.Value);
                _devices.Add(choice);
                _deviceBox.Items.Add(choice);
            }

            _deviceBox.SelectedIndex = 0;
            _deviceBox.EndUpdate();
        }

        private void ApplySelectedDeviceIp()
        {
            if (_deviceBox.SelectedItem is not DeviceChoice choice)
            {
                _ipBox.ClearAddress();
                _portBox.Clear();
                return;
            }

            if (TryGetSavedDeviceIp(choice.Key, choice.Info, out string ip))
            {
                SetIp(ip);
            }
            else
            {
                _ipBox.ClearAddress();
                _portBox.Clear();
            }
        }

        private void UpdateActionState()
        {
            bool isPair = IsPairAction;
            _pairCodeLabel.Text = isPair ? "Pair Code" : "";
            _pairCodeLabel.Visible = isPair;
            _pairCodeBox.Visible = isPair;
            if (!isPair)
            {
                _pairCodeBox.Clear();
            }
        }

        private async Task TryConnectAsync()
        {
            if (!TryReadInputs(out string ip, out int port, out string pairCode))
            {
                return;
            }

            string endpoint = ip + ":" + port;
            _tryConnectButton.Enabled = false;
            _statusLabel.ForeColor = SystemColors.ControlText;
            _statusLabel.Text = IsPairAction ? "Pairing..." : "Connecting...";

            try
            {
                using var cts = new CancellationTokenSource(IsPairAction ? 15000 : 10000);
                var result = IsPairAction
                    ? await _adbController.PairAsync(endpoint, pairCode, cts.Token)
                    : await _adbController.ConnectCaptureAsync(endpoint, cts.Token);

                string output = CombineOutput(result.stdout, result.stderr);
                bool success = result.exitCode == 0 && IsSuccessfulOutput(output);
                if (success)
                {
                    if (!IsPairAction)
                    {
                        SaveConnectedDevice(ip, endpoint);
                        ConfigChanged = true;
                    }

                    _statusLabel.ForeColor = Color.DarkGreen;
                    _statusLabel.Text = string.IsNullOrWhiteSpace(output) ? "ADB Command Succeeded." : ToTitleCaseLines(output);
                    return;
                }

                _statusLabel.ForeColor = Color.Firebrick;
                _statusLabel.Text = string.IsNullOrWhiteSpace(output) ? "ADB Command Failed." : ToTitleCaseLines(output);
            }
            catch (Exception ex)
            {
                _statusLabel.ForeColor = Color.Firebrick;
                _statusLabel.Text = ex.Message;
            }
            finally
            {
                _tryConnectButton.Enabled = true;
            }
        }

        private async Task RestartAdbServerAsync()
        {
            SetBusy(true);
            _statusLabel.ForeColor = SystemColors.ControlText;
            _statusLabel.Text = "Restarting ADB Server...";

            try
            {
                using var cts = new CancellationTokenSource(15000);
                var killResult = await _adbController.KillServerAsync(cts.Token);
                var startResult = await _adbController.StartServerAsync(cts.Token);
                string output = CombineOutput(
                    CombineOutput(killResult.stdout, killResult.stderr),
                    CombineOutput(startResult.stdout, startResult.stderr));

                if (startResult.exitCode == 0)
                {
                    ServerRestarted = true;
                    _statusLabel.ForeColor = Color.DarkGreen;
                    _statusLabel.Text = string.IsNullOrWhiteSpace(output) ? "ADB Server Restarted." : ToTitleCaseLines(output);
                    return;
                }

                _statusLabel.ForeColor = Color.Firebrick;
                _statusLabel.Text = string.IsNullOrWhiteSpace(output) ? "ADB Server Restart Failed." : ToTitleCaseLines(output);
            }
            catch (Exception ex)
            {
                _statusLabel.ForeColor = Color.Firebrick;
                _statusLabel.Text = ex.Message;
            }
            finally
            {
                SetBusy(false);
            }
        }

        private bool TryReadInputs(out string ip, out int port, out string pairCode)
        {
            ip = "";
            port = 0;
            pairCode = "";

            if (!_ipBox.TryGetAddress(out ip))
            {
                ShowValidation("Enter A Valid IPv4 Address.");
                return false;
            }

            if (!int.TryParse(_portBox.Text.Trim(), out port) || port < 1 || port > 65535)
            {
                ShowValidation("Enter A Valid Port From 1 To 65535.");
                return false;
            }

            if (IsPairAction)
            {
                pairCode = _pairCodeBox.Text.Trim();
                if (pairCode.Length == 0 || !pairCode.All(char.IsDigit))
                {
                    ShowValidation("Enter The Numeric Pairing Code.");
                    return false;
                }
            }

            return true;
        }

        private void ShowValidation(string message)
        {
            _statusLabel.ForeColor = Color.Firebrick;
            _statusLabel.Text = message;
        }

        private void SetBusy(bool busy)
        {
            _tryConnectButton.Enabled = !busy;
            _restartServerButton.Enabled = !busy;
        }

        private void SaveConnectedDevice(string ip, string serial)
        {
            var root = _repository.LoadRawConfig();
            var settings = (JObject)root["settings"]!;
            var devices = settings["devices"] as JObject ?? new JObject();
            settings["devices"] = devices;

            var device = devices[ip] as JObject ?? new JObject();
            devices[ip] = device;
            if (string.IsNullOrWhiteSpace(device["name"]?.ToString()))
            {
                device["name"] = GetSelectedDeviceName(ip);
            }

            device["lastSerial"] = serial;
            device["lastSeen"] = DateTimeOffset.Now.ToString("O");
            _repository.SaveRawConfig(root);
        }

        private string GetSelectedDeviceName(string fallback)
        {
            if (_deviceBox.SelectedItem is DeviceChoice choice && !string.IsNullOrWhiteSpace(choice.Info.Name))
            {
                return choice.Info.Name;
            }

            return fallback;
        }

        private bool IsPairAction => (_actionBox.SelectedItem?.ToString() ?? "").Equals("pair", StringComparison.OrdinalIgnoreCase);

        private void SetIp(string ip)
        {
            _ipBox.SetAddress(ip);
        }

        private static string GetDeviceLabel(string key, DeviceInfo info)
        {
            string name = string.IsNullOrWhiteSpace(info.Name) ? key : info.Name;
            return name.Equals(key, StringComparison.OrdinalIgnoreCase) ? key : name + " (" + key + ")";
        }

        private static bool TryNormalizeIp(string value, out string ip)
        {
            ip = "";
            var parts = value.Split('.', StringSplitOptions.None)
                .Select(part => part.Trim())
                .ToArray();

            if (parts.Length != 4)
            {
                return false;
            }

            var normalized = new List<string>();
            foreach (string part in parts)
            {
                if (part.Length == 0 || !part.All(char.IsDigit) || !int.TryParse(part, out int octet) || octet < 0 || octet > 255)
                {
                    return false;
                }

                normalized.Add(octet.ToString());
            }

            ip = string.Join(".", normalized);
            return true;
        }

        private static bool TryGetIp(string value, out string ip)
        {
            ip = "";
            string candidate = (value ?? "").Trim();
            int colonIndex = candidate.LastIndexOf(':');
            if (colonIndex > 0)
            {
                candidate = candidate[..colonIndex];
            }

            return TryNormalizeIp(candidate, out ip);
        }

        private static bool TryGetSavedDeviceIp(string key, DeviceInfo info, out string ip)
        {
            return TryGetIp(key, out ip) ||
                TryGetIp(AdbShellController.GetDeviceKey(info.LastSerial), out ip);
        }

        private static bool IsSuccessfulOutput(string output)
        {
            return output.Contains("connected to", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("already connected", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("successfully paired", StringComparison.OrdinalIgnoreCase);
        }

        private static string CombineOutput(string stdout, string stderr)
        {
            stdout = (stdout ?? "").Trim();
            stderr = (stderr ?? "").Trim();
            if (stdout.Length == 0)
            {
                return stderr;
            }

            return stderr.Length == 0 ? stdout : stdout + Environment.NewLine + stderr;
        }

        private static string ToTitleCaseLines(string text)
        {
            return string.Join(
                Environment.NewLine,
                (text ?? "").Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                    .Select(ToTitleCaseDisplay));
        }

        private static string ToTitleCaseDisplay(string text)
        {
            var words = text.Split(' ');
            for (int index = 0; index < words.Length; index++)
            {
                if (words[index].Length == 0 || words[index].All(char.IsUpper))
                {
                    continue;
                }

                words[index] = char.ToUpperInvariant(words[index][0]) + (words[index].Length == 1 ? "" : words[index][1..]);
            }

            return string.Join(" ", words);
        }

        private sealed record DeviceChoice(string Key, DeviceInfo Info)
        {
            public override string ToString() => GetDeviceLabel(Key, Info);
        }

        private sealed class IpAddressBox : UserControl
        {
            private readonly TextBox[] _octets = Enumerable.Range(0, 4).Select(_ => new TextBox()).ToArray();

            public IpAddressBox()
            {
                Height = 28;
                MinimumSize = new Size(150, 28);
                MaximumSize = new Size(400, 28);
                BackColor = SystemColors.Window;
                BorderStyle = BorderStyle.FixedSingle;

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 7,
                    Margin = Padding.Empty,
                    Padding = new Padding(2, 1, 2, 1)
                };

                for (int index = 0; index < 7; index++)
                {
                    layout.ColumnStyles.Add(index % 2 == 0
                        ? new ColumnStyle(SizeType.Percent, 25F)
                        : new ColumnStyle(SizeType.Absolute, 12F));
                }

                for (int index = 0; index < _octets.Length; index++)
                {
                    var textBox = _octets[index];
                    textBox.BorderStyle = BorderStyle.None;
                    textBox.Dock = DockStyle.Fill;
                    textBox.MaxLength = 3;
                    textBox.TextAlign = HorizontalAlignment.Center;
                    textBox.KeyPress += Octet_KeyPress;
                    textBox.TextChanged += Octet_TextChanged;
                    textBox.KeyDown += Octet_KeyDown;
                    layout.Controls.Add(textBox, index * 2, 0);

                    if (index < _octets.Length - 1)
                    {
                        layout.Controls.Add(new Label
                        {
                            Dock = DockStyle.Fill,
                            Margin = Padding.Empty,
                            Text = ".",
                            TextAlign = ContentAlignment.MiddleCenter
                        }, index * 2 + 1, 0);
                    }
                }

                Controls.Add(layout);
            }

            public void SetAddress(string ip)
            {
                var parts = ip.Split('.');
                for (int index = 0; index < _octets.Length; index++)
                {
                    _octets[index].Text = parts.Length > index ? parts[index] : "";
                }
            }

            public void ClearAddress()
            {
                foreach (var textBox in _octets)
                {
                    textBox.Clear();
                }
            }

            public bool TryGetAddress(out string ip)
            {
                ip = "";
                var parts = new List<string>();
                foreach (var textBox in _octets)
                {
                    string value = textBox.Text.Trim();
                    if (value.Length == 0 || !int.TryParse(value, out int octet) || octet < 0 || octet > 255)
                    {
                        return false;
                    }

                    parts.Add(octet.ToString());
                }

                ip = string.Join(".", parts);
                return true;
            }

            private void Octet_KeyPress(object? sender, KeyPressEventArgs e)
            {
                if (e.KeyChar == '.')
                {
                    if (sender is TextBox { TextLength: > 0 } textBox)
                    {
                        FocusNextOctet(textBox);
                    }

                    e.Handled = true;
                    return;
                }

                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            }

            private void Octet_KeyDown(object? sender, KeyEventArgs e)
            {
                if (e.KeyCode != Keys.Back || sender is not TextBox textBox || textBox.SelectionStart != 0 || textBox.TextLength != 0)
                {
                    return;
                }

                int index = Array.IndexOf(_octets, textBox);
                if (index > 0)
                {
                    _octets[index - 1].Focus();
                    _octets[index - 1].SelectionStart = _octets[index - 1].TextLength;
                    e.Handled = true;
                }
            }

            private void Octet_TextChanged(object? sender, EventArgs e)
            {
                if (sender is not TextBox textBox || !textBox.Focused || textBox.TextLength != 3)
                {
                    return;
                }

                FocusNextOctet(textBox);
            }

            private void FocusNextOctet(TextBox? current)
            {
                int index = Array.IndexOf(_octets, current);
                if (index >= 0 && index < _octets.Length - 1)
                {
                    _octets[index + 1].Focus();
                    _octets[index + 1].SelectAll();
                }
            }
        }
    }
}
