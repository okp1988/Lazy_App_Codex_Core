using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Lazy_App_Codex_Core
{
    public sealed class ConfigEditorForm : Form
    {
        private const int ScriptInfoPanelHeight = 132;
        private const int ScriptStepGridMinimumHeight = 120;
        private const int ScriptTotalRowHeight = 36;
        private static readonly OffsetDisplayOption[] OffsetOptions = OffsetDisplayOption.All;

        private readonly ScriptConfigRepository _repository;
        private readonly TabControl _tabs = new();
        private readonly TextBox _searchBox = new();
        private readonly ListBox _entryList = new();
        private readonly Button _addButton = new();
        private readonly Button _cloneButton = new();
        private readonly Button _removeButton = new();
        private readonly Button _moveUpButton = new();
        private readonly Button _moveDownButton = new();
        private readonly Button _trackTouchButton = new();
        private readonly Button _saveButton = new();
        private readonly Button _closeButton = new();
        private readonly Label _statusLabel = new();
        private readonly ToolTip _toolTip = new();
        private readonly TableLayoutPanel _hotkeySettingsPanel = new();
        private readonly TableLayoutPanel _tagSettingsPanel = new();
        private readonly TableLayoutPanel _deviceEditorPanel = new();
        private readonly Func<AdbDeviceStatus>? _getAdbStatus;
        private readonly Func<string?>? _getSelectedDeviceSerial;
        private readonly AdbShellController _adbController = new();
        private readonly System.Windows.Forms.Timer _trackTouchStateTimer = new();

        private readonly TextBox _scriptNameBox = new();
        private readonly TextBox _sequenceNameBox = new();
        private readonly TextBox _runPlanNameBox = new();
        private readonly TextBox _offsetNameBox = new();
        private readonly TextBox _hotkeyStartBox = new();
        private readonly TextBox _hotkeyStopBox = new();
        private readonly TextBox _hotkeyBackupStartBox = new();
        private readonly TextBox _hotkeyBackupStopBox = new();
        private readonly TextBox _tagNameBox = new();
        private readonly ListBox _tagList = new();
        private readonly Button _addTagButton = new();
        private readonly Button _updateTagButton = new();
        private readonly Button _deleteTagButton = new();
        private readonly TextBox _deviceNameBox = new();
        private readonly TextBox _deviceKeyBox = new();
        private readonly TextBox _deviceManufacturerBox = new();
        private readonly TextBox _deviceModelBox = new();
        private readonly TextBox _deviceLastSerialBox = new();
        private readonly TextBox _deviceLastSeenBox = new();
        private readonly Button _updateDeviceButton = new();
        private readonly Button _deleteDeviceButton = new();
        private readonly Button _syncDeviceButton = new();
        private readonly NumericUpDown _offsetXBox = CreateNumberBox();
        private readonly NumericUpDown _offsetYBox = CreateNumberBox();
        private readonly NumericUpDown _loopBox = CreateNumberBox();
        private readonly NumericUpDown _intervalMinBox = CreateNumberBox();
        private readonly NumericUpDown _intervalMaxBox = CreateNumberBox();
        private readonly NumericUpDown _enforceMinBox = CreateNumberBox(0);
        private readonly NumericUpDown _sequenceLoopBox = CreateNumberBox();
        private readonly NumericUpDown _sequenceIntervalMinBox = CreateNumberBox();
        private readonly NumericUpDown _sequenceIntervalMaxBox = CreateNumberBox();
        private readonly NumericUpDown _sequenceEnforceMinBox = CreateNumberBox(0);
        private readonly CheckBox _defaultOffsetEnabledBox = new();
        private readonly ComboBox _defaultOffsetBox = new();
        private readonly ComboBox _scriptTagBox = new();
        private readonly CheckBox _scriptHiddenBox = new();
        private readonly CheckBox _sequenceDefaultOffsetEnabledBox = new();
        private readonly ComboBox _sequenceDefaultOffsetBox = new();
        private readonly ComboBox _sequenceTagBox = new();
        private readonly CheckBox _sequenceHiddenBox = new();
        private readonly ComboBox _runPlanTagBox = new();
        private readonly ListBox _groupList = new();
        private readonly NumericUpDown _groupRepeatBox = CreateNumberBox(1);
        private readonly DataGridView _stepGrid = new();
        private readonly Label _stepTotalLabel = new();
        private readonly Button _addGroupButton = new();
        private readonly Button _removeGroupButton = new();
        private readonly Button _cloneGroupButton = new();
        private readonly Button _groupUpButton = new();
        private readonly Button _groupDownButton = new();
        private readonly Button _cloneRowButton = new();
        private readonly Button _deleteRowButton = new();
        private readonly Button _rowUpButton = new();
        private readonly Button _rowDownButton = new();

        private readonly DataGridView _sequenceGrid = new();
        private readonly Button _addScriptItemButton = new();
        private readonly Button _addActionItemButton = new();
        private readonly Button _removeItemButton = new();
        private readonly Button _cloneItemButton = new();
        private readonly Button _itemUpButton = new();
        private readonly Button _itemDownButton = new();
        private readonly Label _sequenceTotalLabel = new();
        private readonly DataGridView _runPlanGrid = new();
        private readonly Button _addRunPlanScriptButton = new();
        private readonly Button _addRunPlanSequenceButton = new();
        private readonly Button _removeRunPlanItemButton = new();
        private readonly Button _cloneRunPlanItemButton = new();
        private readonly Button _runPlanItemUpButton = new();
        private readonly Button _runPlanItemDownButton = new();
        private readonly Label _runPlanTotalLabel = new();

        private ConfigLibrary _library;
        private JObject _root;
        private AppSettings _workingSettings;
        private JObject _workingOffsets;
        private Dictionary<string, DeviceInfo> _workingDevices = new(StringComparer.OrdinalIgnoreCase);
        private ScriptModel? _selectedScript;
        private SequenceModel? _selectedSequence;
        private RunPlanModel? _selectedRunPlan;
        private string? _selectedDeviceKey;
        private string _selectedSettingsKey = "hotkeys";
        private string? _selectedOffsetKey;
        private int _loadedGroupIndex = -1;
        private bool _dirty;
        private bool _loading;
        private bool _savedAndClosing;
        private int _currentTabIndex;
        private string _activeEditorTab = "scripts";
        private Process? _trackTouchProcess;
        private bool _trackTouchEnabled;
        private int? _trackedTouchX;
        private int? _trackedTouchY;
        private Dictionary<string, TouchCoordinateMapper> _touchMappers = new(StringComparer.OrdinalIgnoreCase);
        private TouchCoordinateMapper? _activeTouchMapper;
        private string _activeTouchDevice = "";

        public ConfigEditorForm(ScriptConfigRepository repository, Func<AdbDeviceStatus>? getAdbStatus = null, Func<string?>? getSelectedDeviceSerial = null)
        {
            _repository = repository;
            _getAdbStatus = getAdbStatus;
            _getSelectedDeviceSerial = getSelectedDeviceSerial;
            _root = _repository.LoadRawConfig();
            _library = _repository.LoadLibrary();
            _workingSettings = new AppSettings
            {
                HotkeyStart = _repository.Settings.HotkeyStart,
                HotkeyStop = _repository.Settings.HotkeyStop,
                HotkeyBackupStart = _repository.Settings.HotkeyBackupStart,
                HotkeyBackupStop = _repository.Settings.HotkeyBackupStop,
                Tags = NormalizeTags(_repository.Settings.Tags),
                Devices = CloneDevices(_repository.Settings.Devices)
            };
            _workingDevices = _workingSettings.Devices;
            _workingOffsets = ((JObject)_root["offset"]!).DeepClone() as JObject ?? new JObject();

            Text = "Lazy App Config";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(760, 520);
            Size = new Size(1000, 720);
            ShowIcon = false;

            BuildLayout();
            LoadTabs();
            RefreshEntryList();
            WireDirtyTracking();
            _trackTouchStateTimer.Interval = 1000;
            _trackTouchStateTimer.Tick += (_, _) => SyncTrackTouchAvailability();
            _trackTouchStateTimer.Start();
            Shown += (_, _) => FitToWorkingArea();
        }


        private void FitToWorkingArea()
        {
            Rectangle workingArea = Screen.FromControl(this).WorkingArea;
            int targetWidth = Math.Min(Width, workingArea.Width - 40);
            int targetHeight = Math.Min(Height, workingArea.Height - 40);

            if (targetWidth > 0 && targetHeight > 0 && (targetWidth != Width || targetHeight != Height))
            {
                Size = new Size(Math.Max(MinimumSize.Width, targetWidth), Math.Max(MinimumSize.Height, targetHeight));
            }

            if (!workingArea.Contains(Bounds))
            {
                Location = new Point(
                    Math.Max(workingArea.Left, workingArea.Left + (workingArea.Width - Width) / 2),
                    Math.Max(workingArea.Top, workingArea.Top + (workingArea.Height - Height) / 2));
            }
        }

        public bool ConfigSaved { get; private set; }

        private string CurrentTab => _tabs.SelectedTab?.Name ?? "scripts";

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                _closeButton.PerformClick();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void BuildLayout()
        {
            _tabs.Dock = DockStyle.Top;
            _tabs.Height = 42;
            _tabs.SelectedIndexChanged += (_, _) =>
            {
                if (_loading)
                {
                    return;
                }

                if (!ApplySelectedFromEditor(_activeEditorTab))
                {
                    _loading = true;
                    _tabs.SelectedIndex = _currentTabIndex;
                    _loading = false;
                    return;
                }

                _currentTabIndex = _tabs.SelectedIndex;
                _activeEditorTab = CurrentTab;
                ClearSelection();
                RefreshEntryList();
            };

            var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(8) };
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
            _searchBox.Dock = DockStyle.Fill;
            _searchBox.PlaceholderText = "Search";
            _searchBox.TextChanged += (_, _) => RefreshEntryList();
            _entryList.Dock = DockStyle.Fill;
            _entryList.IntegralHeight = false;
            _entryList.SelectedIndexChanged += (_, _) => SelectEntryFromList();

            var listButtons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(0, 4, 0, 0) };
            listButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            listButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            listButtons.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            listButtons.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            listButtons.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            ConfigureButton(_addButton, "Add", (_, _) => AddEntry());
            ConfigureButton(_cloneButton, "Clone", (_, _) => CloneEntry());
            ConfigureButton(_removeButton, "Remove", (_, _) => RemoveEntry());
            ConfigureButton(_moveUpButton, "Move Up", (_, _) => MoveEntry(-1));
            ConfigureButton(_moveDownButton, "Move Down", (_, _) => MoveEntry(1));
            ConfigureButton(_trackTouchButton, "Track Touch", (_, _) => ToggleTrackTouch(), 100);
            foreach (Button button in new[] { _addButton, _cloneButton, _removeButton, _moveUpButton, _moveDownButton, _trackTouchButton })
            {
                button.Dock = DockStyle.Fill;
                button.Margin = new Padding(4, 3, 4, 3);
            }

            listButtons.Controls.Add(_addButton, 0, 0);
            listButtons.Controls.Add(_cloneButton, 1, 0);
            listButtons.Controls.Add(_removeButton, 0, 1);
            listButtons.Controls.Add(_moveUpButton, 1, 1);
            listButtons.Controls.Add(_moveDownButton, 0, 2);
            listButtons.Controls.Add(_trackTouchButton, 1, 2);
            left.Controls.Add(_searchBox, 0, 0);
            left.Controls.Add(_entryList, 0, 1);
            left.Controls.Add(listButtons, 0, 2);

            var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(8) };
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
            var editorHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            editorHost.Controls.Add(BuildRunPlanEditor());
            editorHost.Controls.Add(BuildSequenceEditor());
            editorHost.Controls.Add(BuildScriptEditor());
            editorHost.Controls.Add(BuildOffsetEditor());
            editorHost.Controls.Add(BuildDeviceEditor());
            editorHost.Controls.Add(BuildSettingsEditor());
            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.ForeColor = Color.DimGray;
            var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = true, AutoScroll = true };
            ConfigureButton(_closeButton, "Close", (_, _) => Close(), 90);
            ConfigureButton(_saveButton, "Save All && Close", (_, _) => SaveAndClose(), 145);
            var openFolder = new Button();
            ConfigureButton(openFolder, "Open Config Folder", (_, _) => OpenConfigFolder(), 145);
            var backup = new Button();
            ConfigureButton(backup, "Backup Config", (_, _) => BackupConfig(), 120);
            var restore = new Button();
            ConfigureButton(restore, "Restore Config", (_, _) => RestoreConfig(), 120);
            bottom.Controls.AddRange(new Control[] { _closeButton, _saveButton, restore, backup, openFolder });
            right.Controls.Add(editorHost, 0, 0);
            right.Controls.Add(_statusLabel, 0, 1);
            right.Controls.Add(bottom, 0, 2);

            main.Controls.Add(left, 0, 0);
            main.Controls.Add(right, 1, 0);
            Controls.Add(main);
            Controls.Add(_tabs);
        }

        private Control BuildSettingsEditor()
        {
            var panel = new Panel { Name = "settingsEditor", Dock = DockStyle.Fill, Visible = false };

            _hotkeySettingsPanel.Dock = DockStyle.Fill;
            _hotkeySettingsPanel.ColumnCount = 2;
            _hotkeySettingsPanel.RowCount = 9;
            _hotkeySettingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _hotkeySettingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
            _hotkeySettingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            _hotkeySettingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            _hotkeySettingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            _hotkeySettingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            _hotkeySettingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            _hotkeySettingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            _hotkeySettingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            _hotkeySettingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            _hotkeySettingsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _hotkeySettingsPanel.Controls.Add(Label("Start Hotkey"), 0, 0);
            _hotkeySettingsPanel.Controls.Add(CreateHelpButton("Set primary and optional backup global hotkeys. Backups are used when primary hotkeys are already registered by another app."), 1, 0);
            _hotkeySettingsPanel.Controls.Add(_hotkeyStartBox, 0, 1);
            _hotkeySettingsPanel.SetColumnSpan(_hotkeyStartBox, 2);
            _hotkeySettingsPanel.Controls.Add(Label("Stop Hotkey"), 0, 2);
            _hotkeySettingsPanel.Controls.Add(_hotkeyStopBox, 0, 3);
            _hotkeySettingsPanel.SetColumnSpan(_hotkeyStopBox, 2);
            _hotkeySettingsPanel.Controls.Add(Label("Backup Start Hotkey"), 0, 4);
            _hotkeySettingsPanel.Controls.Add(_hotkeyBackupStartBox, 0, 5);
            _hotkeySettingsPanel.SetColumnSpan(_hotkeyBackupStartBox, 2);
            _hotkeySettingsPanel.Controls.Add(Label("Backup Stop Hotkey"), 0, 6);
            _hotkeySettingsPanel.Controls.Add(_hotkeyBackupStopBox, 0, 7);
            _hotkeySettingsPanel.SetColumnSpan(_hotkeyBackupStopBox, 2);

            _tagSettingsPanel.Dock = DockStyle.Fill;
            _tagSettingsPanel.ColumnCount = 2;
            _tagSettingsPanel.RowCount = 3;
            _tagSettingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _tagSettingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
            _tagSettingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            _tagSettingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
            _tagSettingsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _tagSettingsPanel.Controls.Add(Label("Tags"), 0, 0);
            _tagSettingsPanel.Controls.Add(CreateHelpButton("Tags control the main window filter. Scripts and Sequences can also be left untagged."), 1, 0);
            var tagEditor = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(0, 0, 0, 8) };
            tagEditor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tagEditor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
            tagEditor.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            tagEditor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tagEditor.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            _tagList.Dock = DockStyle.Fill;
            _tagList.IntegralHeight = false;
            _tagList.SelectedIndexChanged += (_, _) => SelectTagFromList();
            _tagNameBox.Dock = DockStyle.Fill;
            var tagButtons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(0, 6, 0, 0) };
            tagButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tagButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tagButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            tagButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            ConfigureButton(_addTagButton, "Add", (_, _) => AddTag(), 72);
            ConfigureButton(_updateTagButton, "Update", (_, _) => UpdateTag(), 82);
            ConfigureButton(_deleteTagButton, "Delete", (_, _) => DeleteTag(), 76);
            foreach (Button button in new[] { _addTagButton, _updateTagButton, _deleteTagButton })
            {
                button.Dock = DockStyle.Fill;
                button.Margin = new Padding(4, 0, 4, 0);
            }

            tagButtons.Controls.Add(_addTagButton, 0, 0);
            tagButtons.Controls.Add(_updateTagButton, 1, 0);
            tagButtons.Controls.Add(_deleteTagButton, 2, 0);
            tagEditor.Controls.Add(_tagList, 0, 0);
            tagEditor.SetRowSpan(_tagList, 3);
            tagEditor.Controls.Add(_tagNameBox, 1, 0);
            tagEditor.Controls.Add(tagButtons, 1, 2);
            _tagSettingsPanel.Controls.Add(tagEditor, 0, 1);
            _tagSettingsPanel.SetColumnSpan(tagEditor, 2);
            _hotkeyStartBox.Dock = DockStyle.Fill;
            _hotkeyStopBox.Dock = DockStyle.Fill;
            _hotkeyBackupStartBox.Dock = DockStyle.Fill;
            _hotkeyBackupStopBox.Dock = DockStyle.Fill;
            panel.Controls.Add(_tagSettingsPanel);
            panel.Controls.Add(_hotkeySettingsPanel);
            return panel;
        }

        private Control BuildOffsetEditor()
        {
            var panel = new TableLayoutPanel { Name = "offsetEditor", Dock = DockStyle.Top, ColumnCount = 4, RowCount = 2, Height = 78, Visible = false };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
            AddTextField(panel, "Profile Name", _offsetNameBox, 0);
            AddNumberField(panel, "X", _offsetXBox, 1);
            AddNumberField(panel, "Y", _offsetYBox, 2);
            panel.Controls.Add(CreateHelpButton("Offset profiles are saved as config offset entries such as s26 or s13. Existing profiles stay compatible."), 3, 0);
            return panel;
        }

        private Control BuildDeviceEditor()
        {
            var panel = new TableLayoutPanel { Name = "devicesEditor", Dock = DockStyle.Top, ColumnCount = 4, RowCount = 8, Height = 344, Visible = false, Padding = new Padding(0, 4, 0, 14) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
            for (int i = 0; i < 7; i++)
            {
                panel.RowStyles.Add(new RowStyle(SizeType.Absolute, i % 2 == 0 ? 28 : 36));
            }

            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            AddReadOnlyTextField(panel, "Device Key", _deviceKeyBox, 0, 0);
            AddTextField(panel, "Name", _deviceNameBox, 1);
            panel.Controls.Add(CreateHelpButton("Devices are remembered under settings.devices. Wi-Fi devices use the IP address without the port as the key."), 3, 0);
            AddReadOnlyTextField(panel, "Manufacturer", _deviceManufacturerBox, 0, 2);
            AddReadOnlyTextField(panel, "Model", _deviceModelBox, 1, 2);
            AddReadOnlyTextField(panel, "Last Serial", _deviceLastSerialBox, 0, 4);
            AddReadOnlyTextField(panel, "Last Seen", _deviceLastSeenBox, 1, 4);

            var buttons = new TableLayoutPanel { Dock = DockStyle.Top, Height = 46, ColumnCount = 3, RowCount = 1, Padding = new Padding(0, 8, 0, 4) };
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            ConfigureButton(_updateDeviceButton, "Update Name", (_, _) => UpdateDeviceName(), 105);
            ConfigureButton(_deleteDeviceButton, "Delete", (_, _) => DeleteDevice(), 85);
            ConfigureButton(_syncDeviceButton, "Sync", async (_, _) => await SyncSelectedDeviceAsync(), 85);
            foreach (Button button in new[] { _updateDeviceButton, _deleteDeviceButton, _syncDeviceButton })
            {
                button.Dock = DockStyle.Fill;
                button.Margin = new Padding(4, 0, 4, 0);
            }

            buttons.Controls.Add(_updateDeviceButton, 0, 0);
            buttons.Controls.Add(_deleteDeviceButton, 1, 0);
            buttons.Controls.Add(_syncDeviceButton, 2, 0);
            panel.Controls.Add(buttons, 0, 7);
            panel.SetColumnSpan(buttons, 4);
            return panel;
        }

        private Control BuildScriptEditor()
        {
            var panel = new TableLayoutPanel
            {
                Name = "scriptEditor",
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 5,
                Height = ScriptInfoPanelHeight + 118 + 44 + ScriptStepGridMinimumHeight + ScriptTotalRowHeight,
                Visible = false
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, ScriptInfoPanelHeight));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, ScriptStepGridMinimumHeight));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, ScriptTotalRowHeight));

            var info = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 4, Padding = new Padding(0, 4, 0, 4) };
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
            info.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            info.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            info.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            info.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            AddTextField(info, "Name", _scriptNameBox, 0);
            AddNumberField(info, "Loop Count", _loopBox, 1);
            AddNumberField(info, "Interval Min", _intervalMinBox, 2);
            AddNumberField(info, "Interval Max", _intervalMaxBox, 3);
            info.Controls.Add(CreateHelpButton("Enforce Min keeps each cycle at least that many seconds. It is capped by the displayed max cycle time."), 4, 0);
            _defaultOffsetEnabledBox.Text = "Enable Default Offset";
            _defaultOffsetEnabledBox.AutoSize = true;
            _defaultOffsetEnabledBox.Dock = DockStyle.Fill;
            _defaultOffsetEnabledBox.Margin = new Padding(0, 4, 4, 2);
            _defaultOffsetEnabledBox.CheckedChanged += (_, _) => _defaultOffsetBox.Enabled = _defaultOffsetEnabledBox.Checked;
            _defaultOffsetBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _defaultOffsetBox.Items.AddRange(OffsetOptions.Cast<object>().ToArray());
            _scriptTagBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _scriptHiddenBox.Text = "Hide from Main";
            _scriptHiddenBox.AutoSize = true;
            _scriptHiddenBox.Dock = DockStyle.Fill;
            _scriptHiddenBox.Margin = new Padding(0, 4, 4, 2);
            info.Controls.Add(_defaultOffsetEnabledBox, 0, 2);
            info.Controls.Add(Label("Default Offset"), 1, 2);
            info.Controls.Add(_defaultOffsetBox, 1, 3);
            info.Controls.Add(Label("Tag"), 2, 2);
            info.Controls.Add(_scriptTagBox, 2, 3);
            info.Controls.Add(_scriptHiddenBox, 0, 3);
            info.Controls.Add(Label("Enforce Min"), 3, 2);
            info.Controls.Add(_enforceMinBox, 3, 3);

            var groups = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(0, 4, 0, 4) };
            groups.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            groups.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
            groups.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _groupList.Dock = DockStyle.Fill;
            _groupList.SelectedIndexChanged += (_, _) => SelectGroup();
            var groupButtons = new TableLayoutPanel { Dock = DockStyle.Top, Height = 104, ColumnCount = 2, RowCount = 3, Padding = new Padding(4, 0, 4, 0) };
            groupButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            groupButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            groupButtons.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            groupButtons.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            groupButtons.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            ConfigureButton(_addGroupButton, "Add", (_, _) => AddGroup(), 88);
            ConfigureButton(_removeGroupButton, "Remove", (_, _) => RemoveGroup(), 88);
            ConfigureButton(_cloneGroupButton, "Clone", (_, _) => CloneGroup(), 88);
            ConfigureButton(_groupUpButton, "Up", (_, _) => MoveGroup(-1), 88);
            ConfigureButton(_groupDownButton, "Down", (_, _) => MoveGroup(1), 88);
            groupButtons.Controls.Add(_addGroupButton, 0, 0);
            groupButtons.Controls.Add(_removeGroupButton, 1, 0);
            groupButtons.Controls.Add(_cloneGroupButton, 0, 1);
            groupButtons.Controls.Add(_groupUpButton, 0, 2);
            groupButtons.Controls.Add(_groupDownButton, 1, 2);
            var repeatPanel = new TableLayoutPanel { Dock = DockStyle.Top, Height = 60, ColumnCount = 1, RowCount = 2, Padding = new Padding(6, 0, 0, 0) };
            repeatPanel.Controls.Add(Label("Repeat Count"), 0, 0);
            repeatPanel.Controls.Add(_groupRepeatBox, 0, 1);
            groups.Controls.Add(_groupList, 0, 0);
            groups.Controls.Add(groupButtons, 1, 0);
            groups.Controls.Add(repeatPanel, 2, 0);

            var rowTools = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = false, Padding = new Padding(0, 2, 0, 2) };
            ConfigureButton(_cloneRowButton, "Clone Row", (_, _) => CloneRow(), 92);
            ConfigureButton(_deleteRowButton, "Delete Row", (_, _) => DeleteRow(), 92);
            ConfigureButton(_rowUpButton, "Row Up", (_, _) => MoveRow(-1), 82);
            ConfigureButton(_rowDownButton, "Row Down", (_, _) => MoveRow(1), 92);
            rowTools.Controls.AddRange(new Control[] { _cloneRowButton, _deleteRowButton, _rowUpButton, _rowDownButton });
            ConfigureStepGrid(_stepGrid);
            _stepGrid.MinimumSize = new Size(0, ScriptStepGridMinimumHeight);
            _stepGrid.Margin = new Padding(0, 0, 0, 8);
            _stepTotalLabel.Dock = DockStyle.Fill;
            _stepTotalLabel.Padding = new Padding(2, 0, 0, 0);
            _stepTotalLabel.TextAlign = ContentAlignment.MiddleLeft;
            _stepTotalLabel.AutoEllipsis = true;
            _stepTotalLabel.Font = new Font(_stepTotalLabel.Font, FontStyle.Bold);

            panel.Controls.Add(info, 0, 0);
            panel.Controls.Add(groups, 0, 1);
            panel.Controls.Add(rowTools, 0, 2);
            panel.Controls.Add(_stepGrid, 0, 3);
            panel.Controls.Add(_stepTotalLabel, 0, 4);
            return panel;
        }

        private Control BuildSequenceEditor()
        {
            var panel = new TableLayoutPanel { Name = "sequenceEditor", Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Visible = false };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 184));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            var namePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 6, Padding = new Padding(0, 4, 0, 4) };
            namePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            namePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            namePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            namePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            namePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
            namePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            namePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            namePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            namePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            namePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            namePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            AddTextField(namePanel, "Name", _sequenceNameBox, 0);
            AddNumberField(namePanel, "Loop Count", _sequenceLoopBox, 1);
            AddNumberField(namePanel, "Interval Min", _sequenceIntervalMinBox, 2);
            AddNumberField(namePanel, "Interval Max", _sequenceIntervalMaxBox, 3);
            namePanel.Controls.Add(CreateHelpButton("Enforce Min keeps each sequence cycle at least that many seconds. It is capped by the displayed max cycle time."), 4, 0);
            _sequenceDefaultOffsetEnabledBox.Text = "Enable Default Offset";
            _sequenceDefaultOffsetEnabledBox.AutoSize = true;
            _sequenceDefaultOffsetEnabledBox.Dock = DockStyle.Fill;
            _sequenceDefaultOffsetEnabledBox.Margin = new Padding(0, 4, 4, 2);
            _sequenceDefaultOffsetEnabledBox.CheckedChanged += (_, _) => _sequenceDefaultOffsetBox.Enabled = _sequenceDefaultOffsetEnabledBox.Checked;
            _sequenceDefaultOffsetBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _sequenceDefaultOffsetBox.Items.AddRange(OffsetOptions.Cast<object>().ToArray());
            _sequenceTagBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _sequenceHiddenBox.Text = "Hide from Main";
            _sequenceHiddenBox.AutoSize = true;
            _sequenceHiddenBox.Dock = DockStyle.Fill;
            _sequenceHiddenBox.Margin = new Padding(0, 4, 4, 2);
            namePanel.Controls.Add(_sequenceDefaultOffsetEnabledBox, 0, 2);
            namePanel.Controls.Add(_sequenceHiddenBox, 0, 4);
            namePanel.Controls.Add(Label("Default Offset"), 1, 2);
            namePanel.Controls.Add(_sequenceDefaultOffsetBox, 1, 3);
            namePanel.Controls.Add(Label("Tag"), 2, 2);
            namePanel.Controls.Add(_sequenceTagBox, 2, 3);
            namePanel.Controls.Add(Label("Enforce Min"), 3, 2);
            namePanel.Controls.Add(_sequenceEnforceMinBox, 3, 3);
            var itemTools = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = false, Padding = new Padding(0, 2, 0, 2) };
            ConfigureButton(_addScriptItemButton, "Add Script", (_, _) => AddSequenceItem("script"), 100);
            ConfigureButton(_addActionItemButton, "Add Action", (_, _) => AddSequenceItem("action"), 102);
            ConfigureButton(_removeItemButton, "Remove", (_, _) => RemoveSequenceItem(), 82);
            ConfigureButton(_cloneItemButton, "Clone", (_, _) => CloneSequenceItem(), 76);
            ConfigureButton(_itemUpButton, "Up", (_, _) => MoveSequenceItem(-1), 58);
            ConfigureButton(_itemDownButton, "Down", (_, _) => MoveSequenceItem(1), 70);
            itemTools.Controls.AddRange(new Control[] { _addScriptItemButton, _addActionItemButton, _removeItemButton, _cloneItemButton, _itemUpButton, _itemDownButton });
            ConfigureSequenceGrid();
            _sequenceGrid.MinimumSize = new Size(0, 160);
            _sequenceTotalLabel.Dock = DockStyle.Fill;
            _sequenceTotalLabel.Font = new Font(_sequenceTotalLabel.Font, FontStyle.Bold);
            panel.Controls.Add(namePanel, 0, 0);
            panel.Controls.Add(itemTools, 0, 1);
            panel.Controls.Add(_sequenceGrid, 0, 2);
            panel.Controls.Add(_sequenceTotalLabel, 0, 3);
            return panel;
        }

        private Control BuildRunPlanEditor()
        {
            var panel = new TableLayoutPanel { Name = "runPlanEditor", Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Visible = false };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

            var info = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(0, 4, 0, 4) };
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
            info.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            info.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            AddTextField(info, "Name", _runPlanNameBox, 0);
            _runPlanTagBox.DropDownStyle = ComboBoxStyle.DropDownList;
            info.Controls.Add(Label("Tag"), 1, 0);
            info.Controls.Add(_runPlanTagBox, 1, 1);
            info.Controls.Add(CreateHelpButton("Run Plans execute existing Scripts and Sequences in this exact item order. Item repeat counts override the target duration for that item only."), 2, 0);

            var itemTools = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = false, Padding = new Padding(0, 2, 0, 2) };
            ConfigureButton(_addRunPlanScriptButton, "Add Script", (_, _) => AddRunPlanItem("script"), 100);
            ConfigureButton(_addRunPlanSequenceButton, "Add Sequence", (_, _) => AddRunPlanItem("sequence"), 120);
            ConfigureButton(_removeRunPlanItemButton, "Remove", (_, _) => RemoveRunPlanItem(), 82);
            ConfigureButton(_cloneRunPlanItemButton, "Clone", (_, _) => CloneRunPlanItem(), 76);
            ConfigureButton(_runPlanItemUpButton, "Up", (_, _) => MoveRunPlanItem(-1), 58);
            ConfigureButton(_runPlanItemDownButton, "Down", (_, _) => MoveRunPlanItem(1), 70);
            itemTools.Controls.AddRange(new Control[] { _addRunPlanScriptButton, _addRunPlanSequenceButton, _removeRunPlanItemButton, _cloneRunPlanItemButton, _runPlanItemUpButton, _runPlanItemDownButton });

            ConfigureRunPlanGrid();
            _runPlanGrid.MinimumSize = new Size(0, 160);
            _runPlanTotalLabel.Dock = DockStyle.Fill;
            _runPlanTotalLabel.Font = new Font(_runPlanTotalLabel.Font, FontStyle.Bold);

            panel.Controls.Add(info, 0, 0);
            panel.Controls.Add(itemTools, 0, 1);
            panel.Controls.Add(_runPlanGrid, 0, 2);
            panel.Controls.Add(_runPlanTotalLabel, 0, 3);
            return panel;
        }

        private void ConfigureStepGrid(DataGridView grid)
        {
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = true;
            grid.AllowUserToDeleteRows = true;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.ScrollBars = ScrollBars.Both;
            grid.Columns.Add(CreateActionColumn());
            grid.Columns.Add(TextColumn("x", "X", 58));
            grid.Columns.Add(TextColumn("y", "Y", 58));
            grid.Columns.Add(TextColumn("x2", "X2", 58));
            grid.Columns.Add(TextColumn("y2", "Y2", 58));
            grid.Columns.Add(TextColumn("randX", "RX", 58));
            grid.Columns.Add(TextColumn("randY", "RY", 58));
            grid.Columns.Add(TextColumn("sleepMin", "Min", 58));
            grid.Columns.Add(TextColumn("sleepMax", "Max", 58));
            grid.CellValueChanged += (_, _) => { MarkDirty(); UpdateStepTotals(); };
            grid.RowsAdded += (_, _) => { MarkDirty(); UpdateStepTotals(); };
            grid.RowsRemoved += (_, _) => { MarkDirty(); UpdateStepTotals(); };
            grid.CurrentCellDirtyStateChanged += (_, _) => { if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        }

        private void ConfigureSequenceGrid()
        {
            _sequenceGrid.Dock = DockStyle.Fill;
            _sequenceGrid.AllowUserToAddRows = false;
            _sequenceGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _sequenceGrid.ScrollBars = ScrollBars.Both;
            var typeCol = new DataGridViewComboBoxColumn { Name = "type", HeaderText = "Type", Width = 80, MinimumWidth = 70 };
            typeCol.Items.AddRange("script", "action");
            _sequenceGrid.Columns.Add(typeCol);
            var scriptCol = new DataGridViewComboBoxColumn { Name = "scriptId", HeaderText = "Script", Width = 170, MinimumWidth = 120 };
            _sequenceGrid.Columns.Add(scriptCol);
            _sequenceGrid.Columns.Add(TextColumn("repeat", "Repeat", 70));
            _sequenceGrid.Columns.Add(TextColumn("imin", "Delay Min", 80));
            _sequenceGrid.Columns.Add(TextColumn("imax", "Delay Max", 80));
            _sequenceGrid.Columns.Add(CreateActionColumn());
            _sequenceGrid.Columns.Add(TextColumn("x", "X", 58));
            _sequenceGrid.Columns.Add(TextColumn("y", "Y", 58));
            _sequenceGrid.Columns.Add(TextColumn("x2", "X2", 58));
            _sequenceGrid.Columns.Add(TextColumn("y2", "Y2", 58));
            _sequenceGrid.Columns.Add(TextColumn("randX", "RX", 58));
            _sequenceGrid.Columns.Add(TextColumn("randY", "RY", 58));
            _sequenceGrid.Columns.Add(TextColumn("sleepMin", "Min", 58));
            _sequenceGrid.Columns.Add(TextColumn("sleepMax", "Max", 58));
            _sequenceGrid.CellValueChanged += (_, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    ApplySequenceRowState(_sequenceGrid.Rows[e.RowIndex]);
                }
                UpdateSelectedSequenceFromGrid();
                UpdateSequenceTotals();
                MarkDirty();
            };
            _sequenceGrid.RowsAdded += (_, _) => { UpdateSelectedSequenceFromGrid(); UpdateSequenceTotals(); MarkDirty(); };
            _sequenceGrid.RowsRemoved += (_, _) => { UpdateSelectedSequenceFromGrid(); UpdateSequenceTotals(); MarkDirty(); };
            _sequenceGrid.DataError += (_, e) => e.ThrowException = false;
            _sequenceGrid.CurrentCellDirtyStateChanged += (_, _) => { if (_sequenceGrid.IsCurrentCellDirty) _sequenceGrid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        }

        private void ConfigureRunPlanGrid()
        {
            _runPlanGrid.Dock = DockStyle.Fill;
            _runPlanGrid.AllowUserToAddRows = false;
            _runPlanGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _runPlanGrid.ScrollBars = ScrollBars.Both;
            var targetCol = new DataGridViewComboBoxColumn { Name = "target", HeaderText = "Target", Width = 260, MinimumWidth = 180, FlatStyle = FlatStyle.Flat };
            _runPlanGrid.Columns.Add(targetCol);
            _runPlanGrid.Columns.Add(TextColumn("repeat", "Repeat", 80));
            _runPlanGrid.CellValueChanged += (_, _) =>
            {
                UpdateSelectedRunPlanFromGrid();
                UpdateRunPlanTotals();
                MarkDirty();
            };
            _runPlanGrid.RowsAdded += (_, _) => { UpdateSelectedRunPlanFromGrid(); UpdateRunPlanTotals(); MarkDirty(); };
            _runPlanGrid.RowsRemoved += (_, _) => { UpdateSelectedRunPlanFromGrid(); UpdateRunPlanTotals(); MarkDirty(); };
            _runPlanGrid.DataError += (_, e) => e.ThrowException = false;
            _runPlanGrid.CurrentCellDirtyStateChanged += (_, _) => { if (_runPlanGrid.IsCurrentCellDirty) _runPlanGrid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        }

        private void LoadTabs()
        {
            _tabs.TabPages.Add("settings", "Settings");
            _tabs.TabPages.Add("devices", "Devices");
            _tabs.TabPages.Add("offset", "Offset");
            _tabs.TabPages.Add("scripts", "Scripts");
            _tabs.TabPages.Add("sequences", "Sequences");
            _tabs.TabPages.Add("runPlans", "Run Plans");
            _tabs.SelectedTab = _tabs.TabPages["scripts"];
            _currentTabIndex = _tabs.SelectedIndex;
            _activeEditorTab = CurrentTab;
        }

        private void RefreshEntryList()
        {
            string? selectedId = CurrentTab switch
            {
                "scripts" => _selectedScript?.Id,
                "sequences" => _selectedSequence?.Id,
                "runPlans" => _selectedRunPlan?.Id,
                "offset" => _selectedOffsetKey,
                "settings" => _selectedSettingsKey,
                "devices" => _selectedDeviceKey,
                _ => null
            };

            _loading = true;
            _entryList.Items.Clear();
            string filter = _searchBox.Text.Trim();
            if (CurrentTab == "scripts")
            {
                foreach (var script in _library.Scripts.Where(s => Matches(s.Name, filter)))
                {
                    _entryList.Items.Add(new EntryRef(script.Name, script.Id));
                }
            }
            else if (CurrentTab == "sequences")
            {
                foreach (var sequence in _library.Sequences.Where(s => Matches(s.Name, filter)))
                {
                    _entryList.Items.Add(new EntryRef(sequence.Name, sequence.Id));
                }
            }
            else if (CurrentTab == "runPlans")
            {
                foreach (var runPlan in _library.RunPlans.Where(s => Matches(s.Name, filter)))
                {
                    _entryList.Items.Add(new EntryRef(runPlan.Name, runPlan.Id));
                }
            }
            else if (CurrentTab == "settings")
            {
                _entryList.Items.Add(new EntryRef("Hotkeys", "hotkeys"));
                _entryList.Items.Add(new EntryRef("Tag", "tag"));
            }
            else if (CurrentTab == "devices")
            {
                foreach (var item in _workingDevices.OrderBy(item => item.Value.Name, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
                {
                    string label = string.IsNullOrWhiteSpace(item.Value.Name) ? item.Key : item.Value.Name;
                    if (!label.Equals(item.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        label += " (" + item.Key + ")";
                    }

                    if (Matches(label, filter) || Matches(item.Key, filter))
                    {
                        _entryList.Items.Add(new EntryRef(label, item.Key));
                    }
                }
            }
            else
            {
                foreach (var property in _workingOffsets.Properties().Where(p => Matches(p.Name, filter)))
                {
                    _entryList.Items.Add(new EntryRef(property.Name, property.Name));
                }
            }

            ShowEditorForCurrentTab();
            if (_entryList.Items.Count > 0)
            {
                int indexToSelect = 0;
                if (!string.IsNullOrWhiteSpace(selectedId))
                {
                    for (int i = 0; i < _entryList.Items.Count; i++)
                    {
                        if (_entryList.Items[i] is EntryRef entry && entry.Id == selectedId)
                        {
                            indexToSelect = i;
                            break;
                        }
                    }
                }

                _entryList.SelectedIndex = indexToSelect;
                if (_entryList.Items[indexToSelect] is EntryRef selectedEntry)
                {
                    SetSelectedEntry(selectedEntry);
                }
            }
            else
            {
                ClearSelection();
            }

            _loading = false;
            LoadCurrentEditor();
        }

        private void SelectEntryFromList()
        {
            if (_loading || _entryList.SelectedItem is not EntryRef entry)
            {
                return;
            }

            if (!ApplySelectedFromEditor(CurrentTab))
            {
                _loading = true;
                _entryList.ClearSelected();
                _loading = false;
                return;
            }

            SetSelectedEntry(entry);
            LoadCurrentEditor();
        }

        private void SetSelectedEntry(EntryRef entry)
        {
            _selectedSettingsKey = CurrentTab == "settings" ? entry.Id : _selectedSettingsKey;
            _selectedScript = CurrentTab == "scripts" ? _library.Scripts.FirstOrDefault(s => s.Id == entry.Id) : null;
            _selectedSequence = CurrentTab == "sequences" ? _library.Sequences.FirstOrDefault(s => s.Id == entry.Id) : null;
            _selectedRunPlan = CurrentTab == "runPlans" ? _library.RunPlans.FirstOrDefault(s => s.Id == entry.Id) : null;
            _selectedOffsetKey = CurrentTab == "offset" ? entry.Id : null;
            _selectedDeviceKey = CurrentTab == "devices" ? entry.Id : null;
        }

        private void LoadCurrentEditor()
        {
            _loading = true;
            ClearEditorValues();
            if (CurrentTab == "settings")
            {
                if (_selectedSettingsKey == "tag")
                {
                    RefreshTagList();
                }
                else
                {
                    _hotkeyStartBox.Text = _workingSettings.HotkeyStart;
                    _hotkeyStopBox.Text = _workingSettings.HotkeyStop;
                    _hotkeyBackupStartBox.Text = _workingSettings.HotkeyBackupStart;
                    _hotkeyBackupStopBox.Text = _workingSettings.HotkeyBackupStop;
                }

                ShowSettingsPanel();
            }
            else if (CurrentTab == "offset")
            {
                _offsetNameBox.Text = _selectedOffsetKey ?? "";
                var token = string.IsNullOrWhiteSpace(_selectedOffsetKey) ? null : _workingOffsets[_selectedOffsetKey];
                _offsetXBox.Value = ClampNumeric(ReadOffsetInt(token, 0));
                _offsetYBox.Value = ClampNumeric(ReadOffsetInt(token, 1));
            }
            else if (CurrentTab == "devices" && _selectedDeviceKey != null && _workingDevices.TryGetValue(_selectedDeviceKey, out var device))
            {
                _deviceKeyBox.Text = _selectedDeviceKey;
                _deviceNameBox.Text = device.Name;
                _deviceManufacturerBox.Text = device.Manufacturer;
                _deviceModelBox.Text = device.Model;
                _deviceLastSerialBox.Text = device.LastSerial;
                _deviceLastSeenBox.Text = device.LastSeen;
                _syncDeviceButton.Enabled = IsDeviceKeyConnected(_selectedDeviceKey);
            }
            else if (_selectedScript != null)
            {
                _scriptNameBox.Text = _selectedScript.Name;
                _loopBox.Value = ClampNumeric(_selectedScript.Duration);
                _intervalMinBox.Value = ClampNumeric(_selectedScript.Interval_Min);
                _intervalMaxBox.Value = ClampNumeric(_selectedScript.Interval_Max);
                _enforceMinBox.Value = ClampNumeric(_selectedScript.Enforce_Min);
                _defaultOffsetEnabledBox.Checked = _selectedScript.DefaultOffsetEnabled;
                _defaultOffsetBox.Enabled = _defaultOffsetEnabledBox.Checked;
                SelectOffsetValue(_defaultOffsetBox, _selectedScript.DefaultOffset);
                RefreshTagCombo(_scriptTagBox, _selectedScript.Tag);
                _scriptHiddenBox.Checked = _selectedScript.Hidden;
                RefreshGroupList();
                UpdateGroupButtonStates();
            }
            else if (_selectedSequence != null)
            {
                _sequenceNameBox.Text = _selectedSequence.Name;
                _sequenceLoopBox.Value = ClampNumeric(_selectedSequence.Duration);
                _sequenceIntervalMinBox.Value = ClampNumeric(_selectedSequence.Interval_Min);
                _sequenceIntervalMaxBox.Value = ClampNumeric(_selectedSequence.Interval_Max);
                _sequenceEnforceMinBox.Value = ClampNumeric(_selectedSequence.Enforce_Min);
                _sequenceDefaultOffsetEnabledBox.Checked = _selectedSequence.DefaultOffsetEnabled;
                _sequenceDefaultOffsetBox.Enabled = _sequenceDefaultOffsetEnabledBox.Checked;
                SelectOffsetValue(_sequenceDefaultOffsetBox, _selectedSequence.DefaultOffset);
                RefreshTagCombo(_sequenceTagBox, _selectedSequence.Tag);
                _sequenceHiddenBox.Checked = _selectedSequence.Hidden;
                RefreshSequenceGrid();
            }
            else if (_selectedRunPlan != null)
            {
                _runPlanNameBox.Text = _selectedRunPlan.Name;
                RefreshTagCombo(_runPlanTagBox, _selectedRunPlan.Tag);
                RefreshRunPlanGrid();
            }

            _dirty = false;
            _loading = false;
            SetCurrentEditorAvailability();
            SetStatus("Ready.");
        }

        private void SetCurrentEditorAvailability()
        {
            bool enabled = CurrentTab switch
            {
                "scripts" => _selectedScript != null,
                "sequences" => _selectedSequence != null,
                "runPlans" => _selectedRunPlan != null,
                "offset" => !string.IsNullOrWhiteSpace(_selectedOffsetKey),
                "devices" => !string.IsNullOrWhiteSpace(_selectedDeviceKey),
                _ => true
            };

            string editorName = CurrentTab switch
            {
                "scripts" => "scriptEditor",
                "sequences" => "sequenceEditor",
                "runPlans" => "runPlanEditor",
                "offset" => "offsetEditor",
                "devices" => "devicesEditor",
                _ => "settingsEditor"
            };

            var editor = FindEditor(editorName);
            if (editor != null)
            {
                editor.Enabled = enabled;
            }
        }

        private void ClearEditorValues()
        {
            _scriptNameBox.Clear();
            _sequenceNameBox.Clear();
            _runPlanNameBox.Clear();
            _offsetNameBox.Clear();
            _hotkeyStartBox.Clear();
            _hotkeyStopBox.Clear();
            _hotkeyBackupStartBox.Clear();
            _hotkeyBackupStopBox.Clear();
            _tagNameBox.Clear();
            _tagList.Items.Clear();
            _deviceKeyBox.Clear();
            _deviceNameBox.Clear();
            _deviceManufacturerBox.Clear();
            _deviceModelBox.Clear();
            _deviceLastSerialBox.Clear();
            _deviceLastSeenBox.Clear();
            _offsetXBox.Value = 0;
            _offsetYBox.Value = 0;
            _loopBox.Value = 0;
            _intervalMinBox.Value = 0;
            _intervalMaxBox.Value = 1;
            _enforceMinBox.Value = 0;
            _sequenceLoopBox.Value = 0;
            _sequenceIntervalMinBox.Value = 0;
            _sequenceIntervalMaxBox.Value = 0;
            _sequenceEnforceMinBox.Value = 0;
            _defaultOffsetEnabledBox.Checked = false;
            _defaultOffsetBox.Enabled = false;
            SelectOffsetValue(_defaultOffsetBox, "0");
            _scriptTagBox.Items.Clear();
            _scriptHiddenBox.Checked = false;
            _sequenceDefaultOffsetEnabledBox.Checked = false;
            _sequenceDefaultOffsetBox.Enabled = false;
            SelectOffsetValue(_sequenceDefaultOffsetBox, "0");
            _sequenceTagBox.Items.Clear();
            _sequenceHiddenBox.Checked = false;
            _runPlanTagBox.Items.Clear();
            _groupList.Items.Clear();
            _loadedGroupIndex = -1;
            _stepGrid.Rows.Clear();
            _sequenceGrid.Rows.Clear();
            _runPlanGrid.Rows.Clear();
            UpdateStepTotals();
            UpdateRunPlanTotals();
            UpdateGroupButtonStates();
        }

        private void ShowEditorForCurrentTab()
        {
            foreach (Control control in EditorHost.Controls)
            {
                control.Visible = control.Name switch
                {
                    "settingsEditor" => CurrentTab == "settings",
                    "devicesEditor" => CurrentTab == "devices",
                    "offsetEditor" => CurrentTab == "offset",
                    "scriptEditor" => CurrentTab == "scripts",
                    "sequenceEditor" => CurrentTab == "sequences",
                    "runPlanEditor" => CurrentTab == "runPlans",
                    _ => false
                };
            }

            bool listEditable = CurrentTab is "scripts" or "sequences" or "runPlans" or "offset";
            _addButton.Enabled = listEditable;
            _cloneButton.Enabled = listEditable;
            _removeButton.Enabled = listEditable || CurrentTab == "devices";
            _moveUpButton.Enabled = CurrentTab is "scripts" or "sequences" or "runPlans";
            _moveDownButton.Enabled = CurrentTab is "scripts" or "sequences" or "runPlans";
            SyncTrackTouchAvailability();
        }

        private Panel EditorHost => (Panel)((TableLayoutPanel)Controls[0]).GetControlFromPosition(1, 0)!.Controls[0];

        private Control? FindEditor(string name)
        {
            return EditorHost.Controls.Cast<Control>().FirstOrDefault(control => control.Name == name);
        }

        private void AddEntry()
        {
            if (!ApplySelectedFromEditor(CurrentTab))
            {
                return;
            }

            if (CurrentTab == "scripts")
            {
                var script = new ScriptModel
                {
                    Id = ScriptConfigRepository.NewId("scr"),
                    Name = UniqueName("SCRIPT", _library.Scripts.Select(s => s.Name)),
                    Tag = "",
                    Order = _library.Scripts.Count,
                    Interval_Max = 1,
                    Groups = new List<ActionGroup> { new() }
                };
                _library.Scripts.Add(script);
                _selectedScript = script;
                _dirty = false;
                RefreshEntryList();
                SelectById(script.Id);
            }
            else if (CurrentTab == "sequences")
            {
                var sequence = new SequenceModel
                {
                    Id = ScriptConfigRepository.NewId("seq"),
                    Name = UniqueName("SEQ", _library.Sequences.Select(s => s.Name)),
                    Tag = "",
                    Order = _library.Sequences.Count,
                    Duration = 1
                };
                _library.Sequences.Add(sequence);
                _selectedSequence = sequence;
                _dirty = false;
                RefreshEntryList();
                SelectById(sequence.Id);
            }
            else if (CurrentTab == "runPlans")
            {
                var runPlan = new RunPlanModel
                {
                    Id = ScriptConfigRepository.NewId("plan"),
                    Name = UniqueName("PLAN", _library.RunPlans.Select(s => s.Name)),
                    Tag = "",
                    Order = _library.RunPlans.Count
                };
                _library.RunPlans.Add(runPlan);
                _selectedRunPlan = runPlan;
                _dirty = false;
                RefreshEntryList();
                SelectById(runPlan.Id);
            }
            else if (CurrentTab == "offset")
            {
                string name = UniqueName("s_new", _workingOffsets.Properties().Select(p => p.Name));
                _selectedOffsetKey = name;
                _workingOffsets[name] = new JArray(0, 0);
                _dirty = false;
                RefreshEntryList();
                SelectById(name);
            }

            MarkDirty();
        }

        private void CloneEntry()
        {
            if (!ApplySelectedFromEditor(CurrentTab))
            {
                return;
            }

            if (CurrentTab == "scripts" && _selectedScript != null)
            {
                var clone = CloneScript(_selectedScript);
                clone.Name = UniqueCopyName(_selectedScript.Name, _library.Scripts.Select(s => s.Name));
                clone.Id = ScriptConfigRepository.NewId("scr");
                clone.Order = _library.Scripts.Count;
                _library.Scripts.Add(clone);
                RefreshEntryList();
                SelectById(clone.Id);
                MarkDirty();
            }
            else if (CurrentTab == "sequences" && _selectedSequence != null)
            {
                var clone = CloneSequence(_selectedSequence);
                clone.Name = UniqueCopyName(_selectedSequence.Name, _library.Sequences.Select(s => s.Name));
                clone.Id = ScriptConfigRepository.NewId("seq");
                clone.Order = _library.Sequences.Count;
                _library.Sequences.Add(clone);
                RefreshEntryList();
                SelectById(clone.Id);
                MarkDirty();
            }
            else if (CurrentTab == "runPlans" && _selectedRunPlan != null)
            {
                var clone = CloneRunPlan(_selectedRunPlan);
                clone.Name = UniqueCopyName(_selectedRunPlan.Name, _library.RunPlans.Select(s => s.Name));
                clone.Id = ScriptConfigRepository.NewId("plan");
                clone.Order = _library.RunPlans.Count;
                _library.RunPlans.Add(clone);
                RefreshEntryList();
                SelectById(clone.Id);
                MarkDirty();
            }
            else if (CurrentTab == "offset" && !string.IsNullOrWhiteSpace(_selectedOffsetKey))
            {
                string name = UniqueCopyName(_selectedOffsetKey, _workingOffsets.Properties().Select(p => p.Name));
                _workingOffsets[name] = _workingOffsets[_selectedOffsetKey]!.DeepClone();
                _selectedOffsetKey = name;
                RefreshEntryList();
                SelectById(name);
                MarkDirty();
            }
        }

        private void RemoveEntry()
        {
            if (CurrentTab == "scripts" && _selectedScript != null)
            {
                var dependents = _library.Sequences.Where(seq => seq.Items.Any(item => item.Type == "script" && item.ScriptId == _selectedScript.Id)).ToList();
                var dependentSequenceIds = new HashSet<string>(dependents.Select(sequence => sequence.Id), StringComparer.OrdinalIgnoreCase);
                var runPlanDependents = _library.RunPlans
                    .Where(plan => plan.Items.Any(item =>
                        (item.Type == "script" && item.TargetId == _selectedScript.Id) ||
                        (item.Type == "sequence" && dependentSequenceIds.Contains(item.TargetId))))
                    .ToList();
                string message = $"Delete script \"{_selectedScript.Name}\"?";
                if (dependents.Count > 0)
                {
                    message += $"{Environment.NewLine}{Environment.NewLine}This will also delete Sequences:{Environment.NewLine}- " + string.Join(Environment.NewLine + "- ", dependents.Select(seq => seq.Name));
                }

                if (runPlanDependents.Count > 0)
                {
                    message += $"{Environment.NewLine}{Environment.NewLine}Run Plans that reference deleted targets will show a missing target:{Environment.NewLine}- " + string.Join(Environment.NewLine + "- ", runPlanDependents.Select(plan => plan.Name));
                }

                message += $"{Environment.NewLine}{Environment.NewLine}Continue?";
                if (MessageBox.Show(message, "Delete Script", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }

                _library.Scripts.Remove(_selectedScript);
                foreach (var sequence in dependents)
                {
                    _library.Sequences.Remove(sequence);
                }
            }
            else if (CurrentTab == "sequences" && _selectedSequence != null)
            {
                var dependents = _library.RunPlans.Where(plan => plan.Items.Any(item => item.Type == "sequence" && item.TargetId == _selectedSequence.Id)).ToList();
                string message = dependents.Count == 0
                    ? $"Delete sequence \"{_selectedSequence.Name}\"?"
                    : $"Delete sequence \"{_selectedSequence.Name}\"? Run Plans that reference it will show a missing target:{Environment.NewLine}- " + string.Join(Environment.NewLine + "- ", dependents.Select(plan => plan.Name));
                if (MessageBox.Show(message, "Delete Sequence", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }

                _library.Sequences.Remove(_selectedSequence);
            }
            else if (CurrentTab == "runPlans" && _selectedRunPlan != null)
            {
                if (MessageBox.Show($"Delete run plan \"{_selectedRunPlan.Name}\"?", "Delete Run Plan", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }

                _library.RunPlans.Remove(_selectedRunPlan);
            }
            else if (CurrentTab == "offset" && !string.IsNullOrWhiteSpace(_selectedOffsetKey))
            {
                if (MessageBox.Show($"Delete offset profile \"{_selectedOffsetKey}\"?", "Delete Offset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }

                _workingOffsets.Property(_selectedOffsetKey)?.Remove();
            }
            else if (CurrentTab == "devices")
            {
                DeleteDevice();
                return;
            }

            NormalizeOrders();
            RefreshEntryList();
            MarkDirty();
        }

        private void MoveEntry(int direction)
        {
            if (!ApplySelectedFromEditor(CurrentTab))
            {
                return;
            }

            string? selectedId = null;
            if (CurrentTab == "scripts" && _selectedScript != null)
            {
                selectedId = _selectedScript.Id;
                MoveItem(_library.Scripts, _selectedScript, direction);
            }
            else if (CurrentTab == "sequences" && _selectedSequence != null)
            {
                selectedId = _selectedSequence.Id;
                MoveItem(_library.Sequences, _selectedSequence, direction);
            }
            else if (CurrentTab == "runPlans" && _selectedRunPlan != null)
            {
                selectedId = _selectedRunPlan.Id;
                MoveItem(_library.RunPlans, _selectedRunPlan, direction);
            }

            NormalizeOrders();
            _dirty = false;
            RefreshEntryList();
            if (!string.IsNullOrWhiteSpace(selectedId))
            {
                SelectById(selectedId);
            }
            MarkDirty();
        }

        private bool ApplySelectedFromEditor(string tabKey)
        {
            if (tabKey == "settings")
            {
                if (_selectedSettingsKey == "tag")
                {
                    _workingSettings.Tags = NormalizeTags(_workingSettings.Tags);
                    EnsureTaggedEntriesUseKnownTags();
                }
                else
                {
                    _workingSettings.HotkeyStart = _hotkeyStartBox.Text.Trim();
                    _workingSettings.HotkeyStop = _hotkeyStopBox.Text.Trim();
                    _workingSettings.HotkeyBackupStart = _hotkeyBackupStartBox.Text.Trim();
                    _workingSettings.HotkeyBackupStop = _hotkeyBackupStopBox.Text.Trim();
                }

                return true;
            }

            if (tabKey == "devices")
            {
                if (_selectedDeviceKey == null || !_workingDevices.TryGetValue(_selectedDeviceKey, out var device))
                {
                    return true;
                }

                string name = _deviceNameBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    ShowValidation("Device name is required.");
                    return false;
                }

                device.Name = name;
                return true;
            }

            if (tabKey == "offset")
            {
                string offsetProfileName = _offsetNameBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(offsetProfileName))
                {
                    ShowValidation("Offset profile name is required.");
                    return false;
                }

                var offsets = _workingOffsets;
                string oldName = _selectedOffsetKey ?? "";
                if (!oldName.Equals(offsetProfileName, StringComparison.OrdinalIgnoreCase) &&
                    offsets.Properties().Any(p => p.Name.Equals(offsetProfileName, StringComparison.OrdinalIgnoreCase)))
                {
                    ShowValidation("Duplicate offset profile names are not allowed.");
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(oldName) && !oldName.Equals(offsetProfileName, StringComparison.Ordinal))
                {
                    offsets.Property(oldName)?.Remove();
                }

                offsets[offsetProfileName] = new JArray((int)_offsetXBox.Value, (int)_offsetYBox.Value);
                _selectedOffsetKey = offsetProfileName;
                return true;
            }

            if (tabKey == "scripts" && _selectedScript != null)
            {
                string name = _scriptNameBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    ShowValidation("Name is required.");
                    return false;
                }

                if (_library.Scripts.Any(s => s != _selectedScript && s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    ShowValidation("Duplicate script names are not allowed.");
                    return false;
                }

                SaveCurrentGroupFromGrid();
                _selectedScript.Name = name;
                _selectedScript.Duration = (int)_loopBox.Value;
                _selectedScript.Interval_Min = (int)_intervalMinBox.Value;
                _selectedScript.Interval_Max = (int)_intervalMaxBox.Value;
                int scriptMaxCycle = GetScriptCycleTotals(_selectedScript).max;
                if (!ValidateEnforceMin((int)_enforceMinBox.Value, scriptMaxCycle, "Script"))
                {
                    return false;
                }

                _selectedScript.Enforce_Min = (int)_enforceMinBox.Value;
                _selectedScript.DefaultOffsetEnabled = _defaultOffsetEnabledBox.Checked;
                _selectedScript.DefaultOffset = OffsetDisplayOption.ReadValue(_defaultOffsetBox.SelectedItem);
                _selectedScript.Tag = RequireSelectedTag(_scriptTagBox);
                _selectedScript.Hidden = _scriptHiddenBox.Checked;
                return true;
            }

            if (tabKey == "sequences" && _selectedSequence != null)
            {
                string name = _sequenceNameBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    ShowValidation("Name is required.");
                    return false;
                }

                if (_library.Sequences.Any(s => s != _selectedSequence && s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    ShowValidation("Duplicate sequence names are not allowed.");
                    return false;
                }

                _selectedSequence.Name = name;
                _selectedSequence.Duration = (int)_sequenceLoopBox.Value;
                _selectedSequence.Interval_Min = (int)_sequenceIntervalMinBox.Value;
                _selectedSequence.Interval_Max = (int)_sequenceIntervalMaxBox.Value;
                int sequenceMaxCycle = GetSequenceCycleTotals().max;
                if (!ValidateEnforceMin((int)_sequenceEnforceMinBox.Value, sequenceMaxCycle, "Sequence"))
                {
                    return false;
                }

                _selectedSequence.Enforce_Min = (int)_sequenceEnforceMinBox.Value;
                _selectedSequence.DefaultOffsetEnabled = _sequenceDefaultOffsetEnabledBox.Checked;
                _selectedSequence.DefaultOffset = OffsetDisplayOption.ReadValue(_sequenceDefaultOffsetBox.SelectedItem);
                _selectedSequence.Tag = RequireSelectedTag(_sequenceTagBox);
                _selectedSequence.Hidden = _sequenceHiddenBox.Checked;
                _selectedSequence.Items = ReadSequenceItemsFromGrid();
                return true;
            }

            if (tabKey == "runPlans" && _selectedRunPlan != null)
            {
                string name = _runPlanNameBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    ShowValidation("Name is required.");
                    return false;
                }

                if (_library.RunPlans.Any(s => s != _selectedRunPlan && s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    ShowValidation("Duplicate run plan names are not allowed.");
                    return false;
                }

                _selectedRunPlan.Name = name;
                _selectedRunPlan.Tag = RequireSelectedTag(_runPlanTagBox);
                _selectedRunPlan.Items = ReadRunPlanItemsFromGrid();
                return true;
            }

            return true;
        }

        private void SaveAndClose()
        {
            if (!ApplySelectedFromEditor(CurrentTab))
            {
                return;
            }

            try
            {
                NormalizeOrders();
                SaveLibraryToRoot();
                _repository.SaveRawConfig(_root);
                ConfigSaved = true;
                _savedAndClosing = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save config.json. " + ex.Message, "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveLibraryToRoot()
        {
            _workingSettings.Tags = NormalizeTags(_workingSettings.Tags);
            EnsureTaggedEntriesUseKnownTags();
            _root["settings"] = new JObject
            {
                ["hotkeyStart"] = _workingSettings.HotkeyStart,
                ["hotkeyStop"] = _workingSettings.HotkeyStop,
                ["hotkeyBackupStart"] = _workingSettings.HotkeyBackupStart,
                ["hotkeyBackupStop"] = _workingSettings.HotkeyBackupStop,
                ["tag"] = new JArray(NormalizeTags(_workingSettings.Tags)),
                ["devices"] = JObject.FromObject(_workingDevices)
            };
            _root["offset"] = _workingOffsets.DeepClone();

            var scripts = new JObject();
            foreach (var script in _library.Scripts.OrderBy(s => s.Order))
            {
                scripts[script.Name] = ScriptConfigRepository.BuildScriptJson(script);
            }

            var sequences = new JObject();
            foreach (var sequence in _library.Sequences.OrderBy(s => s.Order))
            {
                sequences[sequence.Name] = ScriptConfigRepository.BuildSequenceJson(sequence);
            }

            var runPlans = new JObject();
            foreach (var runPlan in _library.RunPlans.OrderBy(s => s.Order))
            {
                runPlans[runPlan.Name] = ScriptConfigRepository.BuildRunPlanJson(runPlan);
            }

            _root["scripts"] = scripts;
            _root["sequences"] = sequences;
            _root["runPlans"] = runPlans;
        }

        private void RefreshGroupList()
        {
            SaveCurrentGroupFromGrid();
            _loading = true;
            _groupList.Items.Clear();
            if (_selectedScript != null)
            {
                for (int i = 0; i < _selectedScript.Groups.Count; i++)
                {
                    _groupList.Items.Add($"Group {i + 1}");
                }
            }

            _loading = false;
            if (_groupList.Items.Count > 0)
            {
                _groupList.SelectedIndex = 0;
            }
            else
            {
                _stepGrid.Rows.Clear();
            }
        }

        private void SelectGroup()
        {
            if (_loading || _selectedScript == null || _groupList.SelectedIndex < 0)
            {
                return;
            }

            SaveCurrentGroupFromGrid();
            _loading = true;
            var group = _selectedScript.Groups[_groupList.SelectedIndex];
            _loadedGroupIndex = _groupList.SelectedIndex;
            _groupRepeatBox.Value = ClampNumeric(Math.Max(1, group.Repeat));
            _stepGrid.Rows.Clear();
            foreach (var step in group.Steps)
            {
                AddStepRow(_stepGrid, step);
            }

            _loading = false;
            UpdateStepTotals();
            UpdateGroupButtonStates();
        }

        private void UpdateGroupButtonStates()
        {
            bool hasScript = _selectedScript != null;
            int groupCount = _selectedScript?.Groups.Count ?? 0;
            bool hasSelection = _groupList.SelectedIndex >= 0;
            _removeGroupButton.Enabled = hasScript && hasSelection && groupCount > 1;
            _cloneGroupButton.Enabled = hasScript && hasSelection;
            _groupUpButton.Enabled = hasScript && hasSelection && _groupList.SelectedIndex > 0;
            _groupDownButton.Enabled = hasScript && hasSelection && _groupList.SelectedIndex < groupCount - 1;
        }

        private void SaveCurrentGroupFromGrid()
        {
            if (_loading || _selectedScript == null || _loadedGroupIndex < 0 || _loadedGroupIndex >= _selectedScript.Groups.Count)
            {
                return;
            }

            var group = _selectedScript.Groups[_loadedGroupIndex];
            group.Repeat = Math.Max(1, (int)_groupRepeatBox.Value);
            group.Steps = ReadStepsFromGrid(_stepGrid);
        }

        private void AddGroup()
        {
            if (_selectedScript == null) return;
            SaveCurrentGroupFromGrid();
            _selectedScript.Groups.Add(new ActionGroup());
            RefreshGroupList();
            _groupList.SelectedIndex = _groupList.Items.Count - 1;
            MarkDirty();
        }

        private void RemoveGroup()
        {
            if (_selectedScript == null || _groupList.SelectedIndex < 0) return;
            if (MessageBox.Show("Delete selected group?", "Delete Group", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _selectedScript.Groups.RemoveAt(_groupList.SelectedIndex);
            if (_selectedScript.Groups.Count == 0) _selectedScript.Groups.Add(new ActionGroup());
            RefreshGroupList();
            MarkDirty();
        }

        private void CloneGroup()
        {
            if (_selectedScript == null || _groupList.SelectedIndex < 0) return;
            SaveCurrentGroupFromGrid();
            var source = _selectedScript.Groups[_groupList.SelectedIndex];
            _selectedScript.Groups.Insert(_groupList.SelectedIndex + 1, CloneGroupModel(source));
            RefreshGroupList();
            _groupList.SelectedIndex = Math.Min(_groupList.Items.Count - 1, _groupList.SelectedIndex + 1);
            MarkDirty();
        }

        private void MoveGroup(int direction)
        {
            if (_selectedScript == null || _groupList.SelectedIndex < 0) return;
            SaveCurrentGroupFromGrid();
            int oldIndex = _groupList.SelectedIndex;
            int newIndex = oldIndex + direction;
            if (newIndex < 0 || newIndex >= _selectedScript.Groups.Count) return;
            var group = _selectedScript.Groups[oldIndex];
            _selectedScript.Groups.RemoveAt(oldIndex);
            _selectedScript.Groups.Insert(newIndex, group);
            RefreshGroupList();
            _groupList.SelectedIndex = newIndex;
            MarkDirty();
        }

        private void CloneRow()
        {
            if (_stepGrid.CurrentRow == null || _stepGrid.CurrentRow.IsNewRow) return;
            int rowIndex = _stepGrid.Rows.Add();
            CopyRowValues(_stepGrid.CurrentRow, _stepGrid.Rows[rowIndex]);
            MarkDirty();
            UpdateStepTotals();
        }

        private void DeleteRow()
        {
            if (_stepGrid.CurrentRow == null || _stepGrid.CurrentRow.IsNewRow) return;
            if (MessageBox.Show("Delete selected row?", "Delete Row", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _stepGrid.Rows.Remove(_stepGrid.CurrentRow);
            MarkDirty();
            UpdateStepTotals();
        }

        private void MoveRow(int direction)
        {
            MoveGridRow(_stepGrid, direction);
            UpdateStepTotals();
        }

        private void AddSequenceItem(string type)
        {
            int index = _sequenceGrid.Rows.Add();
            var row = _sequenceGrid.Rows[index];
            row.Cells["type"].Value = type;
            row.Cells["scriptId"].Value = _library.Scripts.FirstOrDefault()?.Name ?? "";
            row.Cells["repeat"].Value = 1;
            row.Cells["imin"].Value = 0;
            row.Cells["imax"].Value = 0;
            row.Cells["act"].Value = "left";
            row.Cells["x"].Value = 0;
            row.Cells["y"].Value = 0;
            row.Cells["randX"].Value = 0;
            row.Cells["randY"].Value = 0;
            row.Cells["sleepMin"].Value = 0;
            row.Cells["sleepMax"].Value = 0;
            ApplySequenceRowState(row);
            MarkDirty();
        }

        private void RemoveSequenceItem()
        {
            if (_sequenceGrid.CurrentRow == null) return;
            if (MessageBox.Show("Delete selected sequence item?", "Delete Item", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _sequenceGrid.Rows.Remove(_sequenceGrid.CurrentRow);
            MarkDirty();
        }

        private void CloneSequenceItem()
        {
            if (_sequenceGrid.CurrentRow == null) return;
            int index = _sequenceGrid.Rows.Add();
            CopyRowValues(_sequenceGrid.CurrentRow, _sequenceGrid.Rows[index]);
            MarkDirty();
        }

        private void MoveSequenceItem(int direction)
        {
            MoveGridRow(_sequenceGrid, direction);
        }

        private void RefreshSequenceGrid()
        {
            _sequenceGrid.Rows.Clear();
            _sequenceTotalLabel.Text = "";
            var scriptColumn = (DataGridViewComboBoxColumn)_sequenceGrid.Columns["scriptId"];
            scriptColumn.Items.Clear();
            foreach (var script in _library.Scripts)
            {
                scriptColumn.Items.Add(script.Name);
            }

            _addScriptItemButton.Enabled = _library.Scripts.Count > 0;
            if (_selectedSequence == null) return;
            foreach (var item in _selectedSequence.Items)
            {
                int index = _sequenceGrid.Rows.Add();
                var row = _sequenceGrid.Rows[index];
                row.Cells["type"].Value = item.Type;
                string scriptName = _library.FindScriptById(item.ScriptId)?.Name ?? "";
                if (!string.IsNullOrWhiteSpace(scriptName))
                {
                    row.Cells["scriptId"].Value = scriptName;
                }
                row.Cells["repeat"].Value = item.Repeat;
                row.Cells["imin"].Value = item.Interval_Min;
                row.Cells["imax"].Value = item.Interval_Max;
                FillStepRow(row, item.Action);
                ApplySequenceRowState(row);
            }
            UpdateSequenceTotals();
        }

        private void UpdateSelectedSequenceFromGrid()
        {
            if (_loading || _selectedSequence == null)
            {
                return;
            }

            _selectedSequence.Items = ReadSequenceItemsFromGrid();
        }

        private void ApplySequenceRowState(DataGridViewRow row)
        {
            if (row.IsNewRow)
            {
                return;
            }

            bool isScript = ReadCell(row, "type", "script") == "script";
            string[] scriptCells = { "scriptId", "repeat", "imin", "imax" };
            string[] actionCells = { "act", "x", "y", "x2", "y2", "randX", "randY", "sleepMin", "sleepMax" };
            foreach (string cellName in scriptCells)
            {
                SetSequenceCellState(row.Cells[cellName], isScript);
            }

            foreach (string cellName in actionCells)
            {
                SetSequenceCellState(row.Cells[cellName], !isScript);
            }
        }

        private static void SetSequenceCellState(DataGridViewCell cell, bool enabled)
        {
            cell.ReadOnly = !enabled;
            cell.Style.BackColor = enabled ? SystemColors.Window : SystemColors.Control;
            cell.Style.ForeColor = enabled ? SystemColors.ControlText : SystemColors.GrayText;
            cell.Style.SelectionBackColor = enabled ? SystemColors.Highlight : SystemColors.ControlDark;
            cell.Style.SelectionForeColor = enabled ? SystemColors.HighlightText : SystemColors.GrayText;
        }

        private List<SequenceItem> ReadSequenceItemsFromGrid()
        {
            var items = new List<SequenceItem>();
            foreach (DataGridViewRow row in _sequenceGrid.Rows)
            {
                if (row.IsNewRow) continue;
                string type = ReadCell(row, "type", "script");
                var item = new SequenceItem
                {
                    Type = type == "action" ? "action" : "script",
                    ScriptId = ResolveScriptId(ReadCell(row, "scriptId", "")),
                    Repeat = Math.Max(1, ParseInt(row, "repeat", 1)),
                    Interval_Min = ParseInt(row, "imin", 0),
                    Interval_Max = ParseInt(row, "imax", 0),
                    Action = ReadStepFromRow(row)
                };
                items.Add(item);
            }

            return items;
        }

        private void AddRunPlanItem(string type)
        {
            RefreshRunPlanTargetChoices();
            string? target = type == "sequence"
                ? _library.Sequences.Select(sequence => FormatRunPlanTarget("sequence", sequence.Id)).FirstOrDefault()
                : _library.Scripts.Select(script => FormatRunPlanTarget("script", script.Id)).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(target))
            {
                ShowValidation(type == "sequence" ? "No Sequences are available." : "No Scripts are available.");
                return;
            }

            int index = _runPlanGrid.Rows.Add();
            var row = _runPlanGrid.Rows[index];
            row.Cells["target"].Value = target;
            row.Cells["repeat"].Value = 1;
            UpdateSelectedRunPlanFromGrid();
            UpdateRunPlanTotals();
            MarkDirty();
        }

        private void RemoveRunPlanItem()
        {
            if (_runPlanGrid.CurrentRow == null) return;
            if (MessageBox.Show("Delete selected run plan item?", "Delete Item", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _runPlanGrid.Rows.Remove(_runPlanGrid.CurrentRow);
            UpdateSelectedRunPlanFromGrid();
            UpdateRunPlanTotals();
            MarkDirty();
        }

        private void CloneRunPlanItem()
        {
            if (_runPlanGrid.CurrentRow == null) return;
            int index = _runPlanGrid.Rows.Add();
            CopyRowValues(_runPlanGrid.CurrentRow, _runPlanGrid.Rows[index]);
            UpdateSelectedRunPlanFromGrid();
            UpdateRunPlanTotals();
            MarkDirty();
        }

        private void MoveRunPlanItem(int direction)
        {
            MoveGridRow(_runPlanGrid, direction);
            UpdateSelectedRunPlanFromGrid();
            UpdateRunPlanTotals();
        }

        private void RefreshRunPlanGrid()
        {
            _runPlanGrid.Rows.Clear();
            _runPlanTotalLabel.Text = "";
            RefreshRunPlanTargetChoices();
            _addRunPlanScriptButton.Enabled = _library.Scripts.Count > 0;
            _addRunPlanSequenceButton.Enabled = _library.Sequences.Count > 0;
            if (_selectedRunPlan == null) return;

            foreach (var item in _selectedRunPlan.Items)
            {
                string target = FormatRunPlanTarget(item.Type, item.TargetId);
                AddRunPlanTargetChoice(target);
                int index = _runPlanGrid.Rows.Add();
                var row = _runPlanGrid.Rows[index];
                row.Cells["target"].Value = target;
                row.Cells["repeat"].Value = Math.Max(1, item.Repeat);
            }

            UpdateRunPlanTotals();
        }

        private void RefreshRunPlanTargetChoices()
        {
            if (_runPlanGrid.Columns["target"] is not DataGridViewComboBoxColumn targetColumn)
            {
                return;
            }

            targetColumn.Items.Clear();
            foreach (var script in _library.Scripts)
            {
                targetColumn.Items.Add(FormatRunPlanTarget("script", script.Id));
            }

            foreach (var sequence in _library.Sequences)
            {
                targetColumn.Items.Add(FormatRunPlanTarget("sequence", sequence.Id));
            }

            foreach (DataGridViewRow row in _runPlanGrid.Rows)
            {
                string existing = row.Cells["target"].Value?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(existing) && !targetColumn.Items.Contains(existing))
                {
                    targetColumn.Items.Add(existing);
                }
            }
        }

        private void AddRunPlanTargetChoice(string target)
        {
            if (_runPlanGrid.Columns["target"] is DataGridViewComboBoxColumn targetColumn &&
                !targetColumn.Items.Contains(target))
            {
                targetColumn.Items.Add(target);
            }
        }

        private void UpdateSelectedRunPlanFromGrid()
        {
            if (_loading || _selectedRunPlan == null)
            {
                return;
            }

            _selectedRunPlan.Items = ReadRunPlanItemsFromGrid();
        }

        private List<RunPlanItem> ReadRunPlanItemsFromGrid()
        {
            var items = new List<RunPlanItem>();
            foreach (DataGridViewRow row in _runPlanGrid.Rows)
            {
                if (row.IsNewRow) continue;
                if (!TryResolveRunPlanTarget(ReadCell(row, "target", ""), out string type, out string targetId))
                {
                    continue;
                }

                items.Add(new RunPlanItem
                {
                    Type = type,
                    TargetId = targetId,
                    Repeat = Math.Max(1, ParseInt(row, "repeat", 1))
                });
            }

            return items;
        }

        private void UpdateRunPlanTotals()
        {
            int itemCount = 0;
            int cycleCount = 0;
            int missingCount = 0;
            int totalMin = 0;
            int totalMax = 0;
            foreach (DataGridViewRow row in _runPlanGrid.Rows)
            {
                if (row.IsNewRow || string.IsNullOrWhiteSpace(ReadCell(row, "target", "")))
                {
                    continue;
                }

                itemCount++;
                int repeat = Math.Max(1, ParseInt(row, "repeat", 1));
                cycleCount += repeat;
                if (!TryResolveRunPlanTarget(ReadCell(row, "target", ""), out string type, out string targetId) ||
                    !TryGetRunPlanTargetCycleTotals(type, targetId, out var cycleTotals))
                {
                    missingCount++;
                    continue;
                }

                totalMin += cycleTotals.min * repeat;
                totalMax += cycleTotals.max * repeat;
            }

            string missingText = missingCount > 0 ? $" | Missing {missingCount}" : "";
            _runPlanTotalLabel.Text = $"Run plan total time: Min {totalMin}s | Max {totalMax}s | Items {itemCount} | Target cycles {cycleCount}{missingText}";
        }

        private bool TryGetRunPlanTargetCycleTotals(string type, string targetId, out (int min, int max) totals)
        {
            if (type == "sequence")
            {
                var sequence = _library.FindSequenceById(targetId);
                if (sequence != null)
                {
                    totals = GetSequenceCycleTotals(sequence);
                    return true;
                }
            }
            else
            {
                var script = _library.FindScriptById(targetId);
                if (script != null)
                {
                    totals = GetScriptCycleTotalsFromModel(script);
                    return true;
                }
            }

            totals = (0, 0);
            return false;
        }

        private string FormatRunPlanTarget(string type, string targetId)
        {
            if (type == "sequence")
            {
                var sequence = _library.FindSequenceById(targetId);
                return sequence == null ? "Missing Sequence: " + targetId : "[Q] " + sequence.Name;
            }

            var script = _library.FindScriptById(targetId);
            return script == null ? "Missing Script: " + targetId : "[S] " + script.Name;
        }

        private bool TryResolveRunPlanTarget(string target, out string type, out string targetId)
        {
            target = (target ?? "").Trim();
            if (target.StartsWith("[Q] ", StringComparison.OrdinalIgnoreCase))
            {
                type = "sequence";
                string name = target[4..].Trim();
                targetId = _library.Sequences.FirstOrDefault(sequence => sequence.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Id ?? "";
                return targetId.Length > 0;
            }

            if (target.StartsWith("[S] ", StringComparison.OrdinalIgnoreCase))
            {
                type = "script";
                string name = target[4..].Trim();
                targetId = _library.Scripts.FirstOrDefault(script => script.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Id ?? "";
                return targetId.Length > 0;
            }

            if (target.StartsWith("Missing Sequence: ", StringComparison.OrdinalIgnoreCase))
            {
                type = "sequence";
                const string prefix = "Missing Sequence: ";
                targetId = target[prefix.Length..].Trim();
                return targetId.Length > 0;
            }

            if (target.StartsWith("Missing Script: ", StringComparison.OrdinalIgnoreCase))
            {
                type = "script";
                const string prefix = "Missing Script: ";
                targetId = target[prefix.Length..].Trim();
                return targetId.Length > 0;
            }

            type = "script";
            targetId = "";
            return false;
        }

        private string ResolveScriptId(string displayedScript)
        {
            return _library.Scripts.FirstOrDefault(script => script.Name.Equals(displayedScript, StringComparison.OrdinalIgnoreCase))?.Id
                ?? displayedScript;
        }

        private List<StepAction> ReadStepsFromGrid(DataGridView grid)
        {
            var steps = new List<StepAction>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow || IsEmptyRow(row)) continue;
                steps.Add(ReadStepFromRow(row));
            }

            return steps;
        }

        private static StepAction ReadStepFromRow(DataGridViewRow row)
        {
            return new StepAction
            {
                Act = ReadCell(row, "act", "left"),
                ScrX = ParseInt(row, "x", 0),
                ScrY = ParseInt(row, "y", 0),
                ScrX2 = ParseNullableInt(row, "x2"),
                ScrY2 = ParseNullableInt(row, "y2"),
                RandX = ParseInt(row, "randX", 0),
                RandY = ParseInt(row, "randY", 0),
                Sleep_Min = ParseInt(row, "sleepMin", 0),
                Sleep_Max = ParseInt(row, "sleepMax", 0)
            };
        }

        private void AddStepRow(DataGridView grid, StepAction step)
        {
            int index = grid.Rows.Add();
            FillStepRow(grid.Rows[index], step);
        }

        private static void FillStepRow(DataGridViewRow row, StepAction step)
        {
            row.Cells["act"].Value = NormalizeGridAction(step.Act);
            row.Cells["x"].Value = step.ScrX;
            row.Cells["y"].Value = step.ScrY;
            row.Cells["x2"].Value = step.ScrX2;
            row.Cells["y2"].Value = step.ScrY2;
            row.Cells["randX"].Value = step.RandX;
            row.Cells["randY"].Value = step.RandY;
            row.Cells["sleepMin"].Value = step.Sleep_Min;
            row.Cells["sleepMax"].Value = step.Sleep_Max;
        }

        private void UpdateStepTotals()
        {
            int min = 0;
            int max = 0;
            foreach (DataGridViewRow row in _stepGrid.Rows)
            {
                if (row.IsNewRow || IsEmptyRow(row)) continue;
                var sleepRange = NormalizeRange(ParseInt(row, "sleepMin", 0), ParseInt(row, "sleepMax", 0));
                min += sleepRange.min;
                max += sleepRange.max;
            }

            int repeat = ReadRepeatBoxValue();
            int groupMin = min * repeat;
            int groupMax = max * repeat;
            string cycleText = _selectedScript == null
                ? ""
                : $" | Cycle Max {GetScriptCycleTotals(_selectedScript).max}s";
            _stepTotalLabel.Text = $"Group total time: Min {groupMin}s | Max {groupMax}s{cycleText}";
        }

        private int ReadRepeatBoxValue()
        {
            return int.TryParse(_groupRepeatBox.Text, out int repeat) ? Math.Max(1, repeat) : Math.Max(1, (int)_groupRepeatBox.Value);
        }

        private void UpdateSequenceTotals()
        {
            var (min, max) = GetSequenceCycleTotals();
            _sequenceTotalLabel.Text = $"Sequence total time: Min {min}s | Max {max}s";
        }

        private (int min, int max) GetSequenceCycleTotals()
        {
            int min = 0;
            int max = 0;
            foreach (DataGridViewRow row in _sequenceGrid.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                string type = ReadCell(row, "type", "script");
                if (type == "script")
                {
                    var script = _library.Scripts.FirstOrDefault(item => item.Name.Equals(ReadCell(row, "scriptId", ""), StringComparison.OrdinalIgnoreCase));
                    if (script == null)
                    {
                        continue;
                    }

                    int repeat = Math.Max(1, ParseInt(row, "repeat", 1));
                    var stepTotals = GetScriptStepTotals(script);
                    var delayRange = NormalizeRange(ParseInt(row, "imin", 0), ParseInt(row, "imax", 0));
                    min += (stepTotals.min * repeat) + delayRange.min;
                    max += (stepTotals.max * repeat) + delayRange.max;
                }
                else
                {
                    var sleepRange = NormalizeRange(ParseInt(row, "sleepMin", 0), ParseInt(row, "sleepMax", 0));
                    min += sleepRange.min;
                    max += sleepRange.max;
                }
            }

            var intervalRange = NormalizeRange((int)_sequenceIntervalMinBox.Value, (int)_sequenceIntervalMaxBox.Value);
            min += intervalRange.min;
            max += intervalRange.max;
            return (min, max);
        }

        private (int min, int max) GetSequenceCycleTotals(SequenceModel sequence)
        {
            int min = 0;
            int max = 0;
            foreach (var item in sequence.Items)
            {
                if (item.Type == "script")
                {
                    var script = _library.FindScriptById(item.ScriptId);
                    if (script == null)
                    {
                        continue;
                    }

                    int repeat = Math.Max(1, item.Repeat);
                    var stepTotals = GetScriptStepTotals(script);
                    var delayRange = NormalizeRange(item.Interval_Min, item.Interval_Max);
                    min += (stepTotals.min * repeat) + delayRange.min;
                    max += (stepTotals.max * repeat) + delayRange.max;
                    continue;
                }

                var sleepRange = NormalizeRange(item.Action.Sleep_Min, item.Action.Sleep_Max);
                min += sleepRange.min;
                max += sleepRange.max;
            }

            var intervalRange = NormalizeRange(sequence.Interval_Min, sequence.Interval_Max);
            min += intervalRange.min;
            max += intervalRange.max;
            return ApplyEnforceMinimum(min, max, sequence.Enforce_Min);
        }

        private (int min, int max) GetScriptCycleTotals(ScriptModel script)
        {
            var stepTotals = GetScriptStepTotals(script);
            var intervalRange = NormalizeRange((int)_intervalMinBox.Value, (int)_intervalMaxBox.Value);
            return (
                stepTotals.min + intervalRange.min,
                stepTotals.max + intervalRange.max);
        }

        private static (int min, int max) GetScriptCycleTotalsFromModel(ScriptModel script)
        {
            var stepTotals = GetScriptStepTotals(script);
            var intervalRange = NormalizeRange(script.Interval_Min, script.Interval_Max);
            return ApplyEnforceMinimum(
                stepTotals.min + intervalRange.min,
                stepTotals.max + intervalRange.max,
                script.Enforce_Min);
        }

        private static (int min, int max) ApplyEnforceMinimum(int min, int max, int enforceMin)
        {
            int target = Math.Clamp(enforceMin, 0, Math.Max(0, max));
            return (Math.Max(min, target), max);
        }

        private bool ValidateEnforceMin(int requested, int max, string label)
        {
            if (requested <= Math.Max(0, max))
            {
                return true;
            }

            ShowValidation($"{label} Enforce Min cannot be larger than max cycle time ({Math.Max(0, max)}s).");
            return false;
        }

        private static (int min, int max) GetScriptStepTotals(ScriptModel script)
        {
            int min = 0;
            int max = 0;
            foreach (var group in script.Groups)
            {
                int repeat = Math.Max(1, group.Repeat);
                min += group.Steps.Sum(step => NormalizeRange(step.Sleep_Min, step.Sleep_Max).min) * repeat;
                max += group.Steps.Sum(step => NormalizeRange(step.Sleep_Min, step.Sleep_Max).max) * repeat;
            }

            return (min, max);
        }

        private static (int min, int max) NormalizeRange(int min, int max)
        {
            min = Math.Max(0, min);
            max = Math.Max(0, max);
            return max < min ? (max, min) : (min, max);
        }

        private void RefreshTagList()
        {
            _tagList.BeginUpdate();
            _tagList.Items.Clear();
            foreach (string tag in NormalizeTags(_workingSettings.Tags))
            {
                _tagList.Items.Add(tag);
            }

            _tagList.EndUpdate();
            if (_tagList.Items.Count > 0 && _tagList.SelectedIndex < 0)
            {
                _tagList.SelectedIndex = 0;
            }
        }

        private static void SelectOffsetValue(ComboBox combo, string value)
        {
            for (int index = 0; index < combo.Items.Count; index++)
            {
                if (combo.Items[index] is OffsetDisplayOption option &&
                    option.Value.Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = index;
                    return;
                }
            }

            combo.SelectedIndex = combo.Items.Count > 0 ? 0 : -1;
        }

        private void ShowSettingsPanel()
        {
            _hotkeySettingsPanel.Visible = _selectedSettingsKey != "tag";
            _tagSettingsPanel.Visible = _selectedSettingsKey == "tag";
            if (_hotkeySettingsPanel.Visible)
            {
                _hotkeySettingsPanel.BringToFront();
            }
            else
            {
                _tagSettingsPanel.BringToFront();
            }
        }

        private void SelectTagFromList()
        {
            if (_tagList.SelectedItem != null)
            {
                _tagNameBox.Text = _tagList.SelectedItem.ToString();
            }
        }

        private void AddTag()
        {
            string tag = _tagNameBox.Text.Trim();
            if (!ValidateNewTag(tag, null))
            {
                return;
            }

            _workingSettings.Tags.Add(tag);
            RefreshTagList();
            SelectTag(tag);
            MarkDirty();
        }

        private void UpdateTag()
        {
            if (_tagList.SelectedItem == null)
            {
                return;
            }

            string oldTag = _tagList.SelectedItem.ToString() ?? "";
            string newTag = _tagNameBox.Text.Trim();
            if (!ValidateNewTag(newTag, oldTag))
            {
                return;
            }

            int index = _workingSettings.Tags.FindIndex(tag => tag.Equals(oldTag, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                _workingSettings.Tags[index] = newTag;
            }

            foreach (var script in _library.Scripts.Where(script => script.Tag.Equals(oldTag, StringComparison.OrdinalIgnoreCase)))
            {
                script.Tag = newTag;
            }

            foreach (var sequence in _library.Sequences.Where(sequence => sequence.Tag.Equals(oldTag, StringComparison.OrdinalIgnoreCase)))
            {
                sequence.Tag = newTag;
            }

            RefreshTagList();
            SelectTag(newTag);
            MarkDirty();
        }

        private void DeleteTag()
        {
                if (_tagList.SelectedItem == null)
                {
                    return;
                }

            string tag = _tagList.SelectedItem.ToString() ?? "";
            if (MessageBox.Show($"Delete tag \"{tag}\"?", "Delete Tag", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            _workingSettings.Tags.RemoveAll(item => item.Equals(tag, StringComparison.OrdinalIgnoreCase));
            foreach (var script in _library.Scripts.Where(script => script.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            {
                script.Tag = "";
            }

            foreach (var sequence in _library.Sequences.Where(sequence => sequence.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            {
                sequence.Tag = "";
            }

            RefreshTagList();
            MarkDirty();
        }

        private bool ValidateNewTag(string tag, string? currentTag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                ShowValidation("Tag name is required.");
                return false;
            }

            if (tag.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                ShowValidation("All is reserved for the main window filter.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(currentTag) && tag.Equals(currentTag, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (_workingSettings.Tags.Any(existing => existing.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            {
                ShowValidation("Duplicate tag names are not allowed.");
                return false;
            }

            return true;
        }

        private void SelectTag(string tag)
        {
            for (int i = 0; i < _tagList.Items.Count; i++)
            {
                if ((_tagList.Items[i]?.ToString() ?? "").Equals(tag, StringComparison.OrdinalIgnoreCase))
                {
                    _tagList.SelectedIndex = i;
                    return;
                }
            }
        }

        private void UpdateDeviceName()
        {
            if (_selectedDeviceKey == null || !_workingDevices.TryGetValue(_selectedDeviceKey, out var device))
            {
                return;
            }

            string name = _deviceNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowValidation("Device name is required.");
                return;
            }

            device.Name = name;
            RefreshEntryList();
            SelectById(_selectedDeviceKey);
            MarkDirty();
        }

        private void DeleteDevice()
        {
            if (_selectedDeviceKey == null)
            {
                return;
            }

            if (MessageBox.Show($"Delete device \"{_selectedDeviceKey}\"?", "Delete Device", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            _workingDevices.Remove(_selectedDeviceKey);
            _selectedDeviceKey = null;
            RefreshEntryList();
            MarkDirty();
        }

        private async Task SyncSelectedDeviceAsync()
        {
            if (_selectedDeviceKey == null)
            {
                return;
            }

            string? serial = GetConnectedSerialForDeviceKey(_selectedDeviceKey);
            if (string.IsNullOrWhiteSpace(serial))
            {
                ShowValidation("Device is not currently connected to ADB.");
                return;
            }

            try
            {
                _syncDeviceButton.Enabled = false;
                using var cts = new CancellationTokenSource(7000);
                DeviceInfo detected = await _adbController.ReadDeviceInfoAsync(serial, cts.Token);
                if (_workingDevices.TryGetValue(_selectedDeviceKey, out var existing) && !string.IsNullOrWhiteSpace(existing.Name))
                {
                    detected.Name = existing.Name;
                }

                detected.LastSerial = serial;
                detected.LastSeen = DateTimeOffset.Now.ToString("O");
                _workingDevices[_selectedDeviceKey] = detected;
                RefreshEntryList();
                SelectById(_selectedDeviceKey);
                MarkDirty();
                SetStatus("Device information synced.");
            }
            catch (Exception ex)
            {
                ShowValidation("Failed to sync device information. " + ex.Message);
            }
            finally
            {
                _syncDeviceButton.Enabled = _selectedDeviceKey != null && IsDeviceKeyConnected(_selectedDeviceKey);
            }
        }

        private bool IsDeviceKeyConnected(string key)
        {
            return GetConnectedSerialForDeviceKey(key) != null;
        }

        private string? GetConnectedSerialForDeviceKey(string key)
        {
            var status = _getAdbStatus?.Invoke();
            return status?.Devices
                .Where(device => device.IsReady)
                .Select(device => device.Serial)
                .FirstOrDefault(serial => AdbShellController.GetDeviceKey(serial).Equals(key, StringComparison.OrdinalIgnoreCase));
        }

        private void RefreshTagCombo(ComboBox combo, string selectedTag)
        {
            combo.BeginUpdate();
            combo.Items.Clear();
            combo.Items.Add("");
            foreach (string tag in NormalizeTags(_workingSettings.Tags))
            {
                combo.Items.Add(tag);
            }

            combo.EndUpdate();
            int index = combo.FindStringExact(selectedTag);
            combo.SelectedIndex = index >= 0 ? index : 0;
        }

        private string RequireSelectedTag(ComboBox combo)
        {
            string? selected = combo.SelectedItem?.ToString();
            return string.IsNullOrWhiteSpace(selected) ? "" : selected;
        }

        private void EnsureTaggedEntriesUseKnownTags()
        {
            var knownTags = new HashSet<string>(NormalizeTags(_workingSettings.Tags), StringComparer.OrdinalIgnoreCase);
            foreach (var script in _library.Scripts)
            {
                if (!string.IsNullOrWhiteSpace(script.Tag) && !knownTags.Contains(script.Tag))
                {
                    script.Tag = "";
                }
            }

            foreach (var sequence in _library.Sequences)
            {
                if (!string.IsNullOrWhiteSpace(sequence.Tag) && !knownTags.Contains(sequence.Tag))
                {
                    sequence.Tag = "";
                }
            }

            foreach (var runPlan in _library.RunPlans)
            {
                if (!string.IsNullOrWhiteSpace(runPlan.Tag) && !knownTags.Contains(runPlan.Tag))
                {
                    runPlan.Tag = "";
                }
            }
        }

        private void OpenConfigFolder()
        {
            Directory.CreateDirectory(_repository.ConfigFolder);
            Process.Start(new ProcessStartInfo { FileName = _repository.ConfigFolder, UseShellExecute = true });
        }

        private void BackupConfig()
        {
            try
            {
                _repository.BackupConfig();
                SetStatus("Config backed up.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Backup Config", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RestoreConfig()
        {
            using var dialog = new OpenFileDialog
            {
                InitialDirectory = Path.Combine(_repository.ConfigFolder, "backup"),
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (MessageBox.Show("Restore this backup and reload settings?", "Restore Config", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            _repository.RestoreConfig(dialog.FileName);
            _root = _repository.LoadRawConfig();
            _library = _repository.LoadLibrary();
            _workingSettings = new AppSettings
            {
                HotkeyStart = _repository.Settings.HotkeyStart,
                HotkeyStop = _repository.Settings.HotkeyStop,
                HotkeyBackupStart = _repository.Settings.HotkeyBackupStart,
                HotkeyBackupStop = _repository.Settings.HotkeyBackupStop,
                Tags = NormalizeTags(_repository.Settings.Tags),
                Devices = CloneDevices(_repository.Settings.Devices)
            };
            _workingDevices = _workingSettings.Devices;
            _workingOffsets = ((JObject)_root["offset"]!).DeepClone() as JObject ?? new JObject();
            _dirty = false;
            RefreshEntryList();
            ConfigSaved = true;
            SetStatus("Config restored.");
        }

        private void ToggleTrackTouch()
        {
            if (_trackTouchEnabled)
            {
                StopTrackTouch("Track touch stopped.");
                return;
            }

            var adbStatus = _getAdbStatus?.Invoke();
            string? selectedSerial = _getSelectedDeviceSerial?.Invoke();
            bool selectedDeviceReady = !string.IsNullOrWhiteSpace(selectedSerial) &&
                adbStatus?.Devices.Any(device =>
                    device.IsReady &&
                    device.Serial.Equals(selectedSerial, StringComparison.OrdinalIgnoreCase)) == true;
            if (!selectedDeviceReady)
            {
                ShowValidation(adbStatus?.Tooltip ?? "Select a ready ADB device before tracking touch.");
                return;
            }

            StartTrackTouch(selectedSerial!);
        }

        private async void StartTrackTouch(string deviceSerial)
        {
            try
            {
                StopTrackTouch(null);
                _trackedTouchX = null;
                _trackedTouchY = null;
                _activeTouchMapper = null;
                _activeTouchDevice = "";
                var adb = new AdbShellController(_adbController.AdbPath, deviceSerial);
                _touchMappers = await LoadTouchCoordinateMappersAsync(adb);
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = adb.AdbPath,
                        Arguments = adb.DeviceSelector + "shell getevent -l",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        StandardErrorEncoding = System.Text.Encoding.UTF8
                    },
                    EnableRaisingEvents = true
                };

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data == null)
                    {
                        BeginInvoke((Action)(() => StopTrackTouch("Track touch ended.")));
                        return;
                    }

                    HandleTrackTouchLine(e.Data);
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        AppLogger.LogWarning("Track touch stderr: " + e.Data);
                    }
                };
                process.Exited += (_, _) =>
                {
                    if (!_trackTouchEnabled)
                    {
                        return;
                    }

                    BeginInvoke((Action)(() => StopTrackTouch("Track touch ended.")));
                };

                if (!process.Start())
                {
                    process.Dispose();
                    ShowValidation("Failed to start adb shell getevent -l.");
                    return;
                }

                _trackTouchProcess = process;
                _trackTouchEnabled = true;
                ApplyTrackTouchButtonState();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                SetStatus("Track touch on. Touch the phone screen to read coordinates.");
            }
            catch (Exception ex)
            {
                ShowValidation("Failed to start track touch. " + ex.Message);
            }
        }

        private void StopTrackTouch(string? statusMessage)
        {
            _trackTouchEnabled = false;
            ApplyTrackTouchButtonState();
            _touchMappers.Clear();
            _activeTouchMapper = null;
            _activeTouchDevice = "";
            var process = _trackTouchProcess;
            _trackTouchProcess = null;
            if (process != null)
            {
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

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                SetStatus(statusMessage);
            }
        }

        private void SyncTrackTouchAvailability()
        {
            bool availableTab = CurrentTab is "scripts" or "sequences";
            var adbStatus = _getAdbStatus?.Invoke();
            string? selectedSerial = _getSelectedDeviceSerial?.Invoke();
            bool adbReady = !string.IsNullOrWhiteSpace(selectedSerial) &&
                adbStatus?.Devices.Any(device =>
                    device.IsReady &&
                    device.Serial.Equals(selectedSerial, StringComparison.OrdinalIgnoreCase)) == true;
            _trackTouchButton.Enabled = availableTab && (adbReady || _trackTouchEnabled);
            if (_trackTouchEnabled && !adbReady)
            {
                StopTrackTouch("Track touch stopped because the selected ADB device is not ready.");
            }
        }

        private void HandleTrackTouchLine(string line)
        {
            string? devicePath = ReadGetEventDevicePath(line);
            if (devicePath != null && _touchMappers.TryGetValue(devicePath, out var mapper))
            {
                _activeTouchMapper = mapper;
                _activeTouchDevice = devicePath;
            }

            if (TryReadGetEventValue(line, "ABS_MT_POSITION_X", out int x) || TryReadGetEventValue(line, "ABS_X", out x))
            {
                _trackedTouchX = x;
            }

            if (TryReadGetEventValue(line, "ABS_MT_POSITION_Y", out int y) || TryReadGetEventValue(line, "ABS_Y", out y))
            {
                _trackedTouchY = y;
            }

            if (line.Contains("SYN_REPORT", StringComparison.OrdinalIgnoreCase) && _trackedTouchX.HasValue && _trackedTouchY.HasValue)
            {
                var activeMapper = _activeTouchMapper;
                if (activeMapper == null)
                {
                    string missingMapperDeviceInfo = string.IsNullOrWhiteSpace(_activeTouchDevice) ? "device unknown" : _activeTouchDevice;
                    BeginInvoke((Action)(() => SetStatus($"Track touch cannot map to screen coordinates: {missingMapperDeviceInfo}, range unknown. Raw X {_trackedTouchX.Value}, Y {_trackedTouchY.Value}.", true)));
                    return;
                }

                var (screenX, screenY) = activeMapper.Map(_trackedTouchX.Value, _trackedTouchY.Value);
                BeginInvoke((Action)(() => SetStatus($"Touch position: X {screenX}, Y {screenY}")));
            }
        }

        private async Task<Dictionary<string, TouchCoordinateMapper>> LoadTouchCoordinateMappersAsync(AdbShellController adb)
        {
            using var cts = new CancellationTokenSource(5000);
            var (width, height) = await adb.GetDeviceSizeAsync(cts.Token);
            var (_, output, _) = await adb.RunCaptureAsync("shell getevent -lp", cts.Token, 5000);
            return CreateTouchMappers(output, width, height);
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
                    if (TryReadAbsRange(line, out var minX, out var maxX))
                    {
                        range.MinX = minX;
                        range.MaxX = maxX;
                    }
                }
                else if (line.Contains("ABS_MT_POSITION_Y", StringComparison.OrdinalIgnoreCase) || line.Contains("ABS_Y", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryReadAbsRange(line, out var minY, out var maxY))
                    {
                        range.MinY = minY;
                        range.MaxY = maxY;
                    }
                }
            }

            var mappers = new Dictionary<string, TouchCoordinateMapper>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in ranges)
            {
                var range = item.Value;
                if (range.MinX.HasValue && range.MaxX.HasValue && range.MinY.HasValue && range.MaxY.HasValue &&
                    range.MaxX > range.MinX && range.MaxY > range.MinY)
                {
                    mappers[item.Key] = new TouchCoordinateMapper(range.MinX.Value, range.MaxX.Value, range.MinY.Value, range.MaxY.Value, screenWidth, screenHeight);
                }
            }

            return mappers;
        }

        private static string? ReadGetEventDevicePath(string line)
        {
            var match = System.Text.RegularExpressions.Regex.Match(line, @"/dev/input/event\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return null;
            }

            return match.Value;
        }

        private static bool TryReadAbsRange(string line, out int? minimum, out int? maximum)
        {
            minimum = null;
            maximum = null;
            var match = System.Text.RegularExpressions.Regex.Match(line, @"min\s+(-?\d+),\s*max\s+(-?\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return false;
            }

            if (int.TryParse(match.Groups[1].Value, out int minValue) && int.TryParse(match.Groups[2].Value, out int maxValue))
            {
                minimum = minValue;
                maximum = maxValue;
                return true;
            }

            return false;
        }

        private void ApplyTrackTouchButtonState()
        {
            if (_trackTouchEnabled)
            {
                _trackTouchButton.Text = "TRACK ON";
                _trackTouchButton.BackColor = Color.LimeGreen;
                _trackTouchButton.ForeColor = Color.Black;
                _trackTouchButton.UseVisualStyleBackColor = false;
            }
            else
            {
                _trackTouchButton.Text = "Track Touch";
                _trackTouchButton.BackColor = SystemColors.Control;
                _trackTouchButton.ForeColor = SystemColors.ControlText;
                _trackTouchButton.UseVisualStyleBackColor = true;
            }
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
            if (token.Length == 0)
            {
                return false;
            }

            return int.TryParse(token, System.Globalization.NumberStyles.HexNumber, null, out value) ||
                int.TryParse(token, out value);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopTrackTouch(null);
            if (!_savedAndClosing && !ConfirmCloseSaveDiscardCancel())
            {
                e.Cancel = true;
                return;
            }

            base.OnFormClosing(e);
        }

        private bool ConfirmCloseSaveDiscardCancel()
        {
            if (!_dirty)
            {
                return true;
            }

            var result = MessageBox.Show(
                "Save changes to config.json?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);
            if (result == DialogResult.Cancel)
            {
                return false;
            }

            if (result == DialogResult.Yes)
            {
                if (!ApplySelectedFromEditor(CurrentTab))
                {
                    return false;
                }

                NormalizeOrders();
                SaveLibraryToRoot();
                _repository.SaveRawConfig(_root);
                ConfigSaved = true;
                return true;
            }

            return true;
        }

        private void WireDirtyTracking()
        {
            foreach (Control control in Controls.OfType<Control>().SelectMany(AllControls))
            {
                switch (control)
                {
                    case TextBox textBox:
                        if (textBox == _searchBox)
                        {
                            break;
                        }

                        textBox.TextChanged += (_, _) => MarkDirty();
                        break;
                    case NumericUpDown numeric:
                        numeric.ValueChanged += (_, _) => MarkDirty();
                        break;
                    case CheckBox checkBox:
                        checkBox.CheckedChanged += (_, _) => MarkDirty();
                        break;
                    case ComboBox combo:
                        combo.SelectedIndexChanged += (_, _) => MarkDirty();
                        break;
                }
            }

            _groupRepeatBox.ValueChanged += (_, _) =>
            {
                SaveCurrentGroupFromGrid();
                UpdateStepTotals();
            };
            _groupRepeatBox.TextChanged += (_, _) => UpdateStepTotals();
            _scriptNameBox.TextChanged += (_, _) => UpdateSelectedListName(_scriptNameBox.Text.Trim());
            _intervalMinBox.ValueChanged += (_, _) => UpdateStepTotals();
            _intervalMaxBox.ValueChanged += (_, _) => UpdateStepTotals();
            _sequenceNameBox.TextChanged += (_, _) => UpdateSelectedListName(_sequenceNameBox.Text.Trim());
            _sequenceLoopBox.ValueChanged += (_, _) => UpdateSelectedSequenceSettings();
            _sequenceIntervalMinBox.ValueChanged += (_, _) => UpdateSelectedSequenceSettings();
            _sequenceIntervalMaxBox.ValueChanged += (_, _) => UpdateSelectedSequenceSettings();
            _sequenceEnforceMinBox.ValueChanged += (_, _) => UpdateSelectedSequenceSettings();
            _sequenceDefaultOffsetEnabledBox.CheckedChanged += (_, _) => UpdateSelectedSequenceSettings();
            _sequenceDefaultOffsetBox.SelectedIndexChanged += (_, _) => UpdateSelectedSequenceSettings();
            _sequenceTagBox.SelectedIndexChanged += (_, _) => UpdateSelectedSequenceSettings();
            _sequenceHiddenBox.CheckedChanged += (_, _) => UpdateSelectedSequenceSettings();
            _runPlanNameBox.TextChanged += (_, _) => UpdateSelectedListName(_runPlanNameBox.Text.Trim());
        }

        private void UpdateSelectedSequenceSettings()
        {
            if (_loading || _selectedSequence == null)
            {
                return;
            }

            _selectedSequence.Duration = (int)_sequenceLoopBox.Value;
            _selectedSequence.Interval_Min = (int)_sequenceIntervalMinBox.Value;
            _selectedSequence.Interval_Max = (int)_sequenceIntervalMaxBox.Value;
            _selectedSequence.Enforce_Min = Math.Min((int)_sequenceEnforceMinBox.Value, GetSequenceCycleTotals().max);
            _selectedSequence.DefaultOffsetEnabled = _sequenceDefaultOffsetEnabledBox.Checked;
            _selectedSequence.DefaultOffset = OffsetDisplayOption.ReadValue(_sequenceDefaultOffsetBox.SelectedItem);
            _selectedSequence.Tag = RequireSelectedTag(_sequenceTagBox);
            _selectedSequence.Hidden = _sequenceHiddenBox.Checked;
            UpdateSequenceTotals();
        }

        private void UpdateSelectedListName(string name)
        {
            if (_loading || string.IsNullOrWhiteSpace(name) || _entryList.SelectedIndex < 0)
            {
                return;
            }

            string? id = CurrentTab switch
            {
                "scripts" => _selectedScript?.Id,
                "sequences" => _selectedSequence?.Id,
                "runPlans" => _selectedRunPlan?.Id,
                _ => null
            };
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            int index = _entryList.SelectedIndex;
            if (CurrentTab == "scripts" && _selectedScript != null)
            {
                _selectedScript.Name = name;
            }
            else if (CurrentTab == "sequences" && _selectedSequence != null)
            {
                _selectedSequence.Name = name;
            }
            else if (CurrentTab == "runPlans" && _selectedRunPlan != null)
            {
                _selectedRunPlan.Name = name;
            }

            _loading = true;
            _entryList.Items[index] = new EntryRef(name, id);
            _entryList.SelectedIndex = index;
            _loading = false;
        }

        private void MarkDirty()
        {
            if (_loading)
            {
                return;
            }

            _dirty = true;
            SetStatus("Modified.");
        }

        private void NormalizeOrders()
        {
            for (int i = 0; i < _library.Scripts.Count; i++) _library.Scripts[i].Order = i;
            for (int i = 0; i < _library.Sequences.Count; i++) _library.Sequences[i].Order = i;
            for (int i = 0; i < _library.RunPlans.Count; i++) _library.RunPlans[i].Order = i;
        }

        private void SelectById(string id)
        {
            for (int i = 0; i < _entryList.Items.Count; i++)
            {
                if (_entryList.Items[i] is EntryRef entry && entry.Id == id)
                {
                    _entryList.SelectedIndex = i;
                    return;
                }
            }
        }

        private void ClearSelection()
        {
            _selectedScript = null;
            _selectedSequence = null;
            _selectedRunPlan = null;
            _selectedDeviceKey = null;
            if (CurrentTab == "settings")
            {
                _selectedSettingsKey = "hotkeys";
            }
            _selectedOffsetKey = null;
            _dirty = false;
        }

        private static void MoveItem<T>(List<T> list, T item, int direction)
        {
            int index = list.IndexOf(item);
            int target = index + direction;
            if (index < 0 || target < 0 || target >= list.Count) return;
            list.RemoveAt(index);
            list.Insert(target, item);
        }

        private void MoveGridRow(DataGridView grid, int direction)
        {
            if (grid.CurrentRow == null || grid.CurrentRow.IsNewRow) return;
            int index = grid.CurrentRow.Index;
            int target = index + direction;
            if (target < 0 || target >= grid.Rows.Count || grid.Rows[target].IsNewRow) return;
            var values = grid.CurrentRow.Cells.Cast<DataGridViewCell>().Select(c => c.Value).ToArray();
            grid.Rows.RemoveAt(index);
            grid.Rows.Insert(target, 1);
            for (int i = 0; i < values.Length; i++) grid.Rows[target].Cells[i].Value = values[i];
            grid.CurrentCell = grid.Rows[target].Cells[0];
            MarkDirty();
        }

        private static void CopyRowValues(DataGridViewRow source, DataGridViewRow target)
        {
            for (int i = 0; i < source.Cells.Count; i++)
            {
                target.Cells[i].Value = source.Cells[i].Value;
            }
        }

        private static ScriptModel CloneScript(ScriptModel source)
        {
            return new ScriptModel
            {
                Id = source.Id,
                Name = source.Name,
                Tag = source.Tag,
                Hidden = source.Hidden,
                Order = source.Order,
                Duration = source.Duration,
                Interval_Min = source.Interval_Min,
                Interval_Max = source.Interval_Max,
                Enforce_Min = source.Enforce_Min,
                DefaultOffsetEnabled = source.DefaultOffsetEnabled,
                DefaultOffset = source.DefaultOffset,
                Groups = source.Groups.Select(CloneGroupModel).ToList()
            };
        }

        private static ActionGroup CloneGroupModel(ActionGroup source)
        {
            return new ActionGroup { Repeat = source.Repeat, Steps = source.Steps.Select(CloneStep).ToList() };
        }

        private static SequenceModel CloneSequence(SequenceModel source)
        {
            return new SequenceModel
            {
                Id = source.Id,
                Name = source.Name,
                Tag = source.Tag,
                Hidden = source.Hidden,
                Order = source.Order,
                Duration = source.Duration,
                Interval_Min = source.Interval_Min,
                Interval_Max = source.Interval_Max,
                Enforce_Min = source.Enforce_Min,
                DefaultOffsetEnabled = source.DefaultOffsetEnabled,
                DefaultOffset = source.DefaultOffset,
                Items = source.Items.Select(item => new SequenceItem
                {
                    Type = item.Type,
                    ScriptId = item.ScriptId,
                    Repeat = item.Repeat,
                    Interval_Min = item.Interval_Min,
                    Interval_Max = item.Interval_Max,
                    Action = CloneStep(item.Action)
                }).ToList()
            };
        }

        private static RunPlanModel CloneRunPlan(RunPlanModel source)
        {
            return new RunPlanModel
            {
                Id = source.Id,
                Name = source.Name,
                Tag = source.Tag,
                Order = source.Order,
                Items = source.Items.Select(item => new RunPlanItem
                {
                    Type = item.Type,
                    TargetId = item.TargetId,
                    Repeat = item.Repeat
                }).ToList()
            };
        }

        private static StepAction CloneStep(StepAction step)
        {
            return new StepAction
            {
                Act = step.Act,
                ScrX = step.ScrX,
                ScrY = step.ScrY,
                ScrX2 = step.ScrX2,
                ScrY2 = step.ScrY2,
                RandX = step.RandX,
                RandY = step.RandY,
                Sleep_Min = step.Sleep_Min,
                Sleep_Max = step.Sleep_Max,
                Offset = step.Offset
            };
        }

        private static string UniqueCopyName(string baseName, IEnumerable<string> existing)
        {
            var used = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
            string first = baseName + "_copy";
            if (!used.Contains(first)) return first;
            int index = 2;
            while (used.Contains(baseName + "_copy" + index)) index++;
            return baseName + "_copy" + index;
        }

        private static string UniqueName(string baseName, IEnumerable<string> existing)
        {
            var used = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
            if (!used.Contains(baseName)) return baseName;
            int index = 2;
            while (used.Contains(baseName + index)) index++;
            return baseName + index;
        }

        private static bool Matches(string value, string filter)
        {
            return string.IsNullOrWhiteSpace(filter) || value.Contains(filter, StringComparison.OrdinalIgnoreCase);
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

        private static IEnumerable<Control> AllControls(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (var nested in AllControls(child))
                {
                    yield return nested;
                }
            }
        }

        private static bool IsEmptyRow(DataGridViewRow row)
        {
            return row.Cells.Cast<DataGridViewCell>().All(cell => string.IsNullOrWhiteSpace(cell.Value?.ToString()));
        }

        private static string ReadCell(DataGridViewRow row, string name, string fallback)
        {
            string? value = row.Cells[name].Value?.ToString();
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static int ParseInt(DataGridViewRow row, string name, int fallback)
        {
            return int.TryParse(row.Cells[name].Value?.ToString(), out int parsed) ? parsed : fallback;
        }

        private static int ReadOffsetInt(JToken? token, int index)
        {
            if (token is JArray array)
            {
                token = array.ElementAtOrDefault(index);
            }

            return int.TryParse(token?.ToString(), out int parsed) ? parsed : 0;
        }

        private static int? ParseNullableInt(DataGridViewRow row, string name)
        {
            return int.TryParse(row.Cells[name].Value?.ToString(), out int parsed) ? parsed : null;
        }

        private static string NormalizeGridAction(string action)
        {
            return action.Trim().ToLowerInvariant() switch
            {
                "leftclick" => "left",
                "rightclick" => "right",
                "back" => "right",
                "drag" => "drag",
                "left" => "left",
                "right" => "right",
                _ => "left"
            };
        }

        private static decimal ClampNumeric(int value)
        {
            return Math.Max(-100000, Math.Min(100000, value));
        }

        private static NumericUpDown CreateNumberBox(int minimum = -100000)
        {
            return new NumericUpDown { Dock = DockStyle.Fill, Minimum = minimum, Maximum = 100000 };
        }

        private static DataGridViewTextBoxColumn TextColumn(string name, string header, int width)
        {
            return new DataGridViewTextBoxColumn { Name = name, HeaderText = header, Width = width, MinimumWidth = 42 };
        }

        private static DataGridViewComboBoxColumn CreateActionColumn()
        {
            var column = new DataGridViewComboBoxColumn { Name = "act", HeaderText = "Act", Width = 70, FlatStyle = FlatStyle.Flat };
            column.Items.AddRange("left", "right", "drag");
            return column;
        }

        private static Label Label(string text)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 0, 2, 0) };
        }

        private static void AddTextField(TableLayoutPanel layout, string label, TextBox input, int column)
        {
            input.Dock = DockStyle.Fill;
            layout.Controls.Add(Label(label), column, 0);
            layout.Controls.Add(input, column, 1);
        }

        private static void AddReadOnlyTextField(TableLayoutPanel layout, string label, TextBox input, int column, int row)
        {
            input.Dock = DockStyle.Fill;
            input.ReadOnly = true;
            layout.Controls.Add(Label(label), column, row);
            layout.Controls.Add(input, column, row + 1);
        }

        private static void AddNumberField(TableLayoutPanel layout, string label, NumericUpDown input, int column)
        {
            input.Dock = DockStyle.Fill;
            layout.Controls.Add(Label(label), column, 0);
            layout.Controls.Add(input, column, 1);
        }

        private void ConfigureButton(Button button, string text, EventHandler handler, int width = 82)
        {
            button.Text = text;
            button.AutoSize = false;
            button.Size = new Size(width, 32);
            button.Margin = new Padding(4, 3, 4, 3);
            button.UseVisualStyleBackColor = true;
            button.Click += handler;
        }

        private Button CreateHelpButton(string helpText)
        {
            var button = new Button
            {
                Dock = DockStyle.Top,
                Size = new Size(24, 24),
                Margin = new Padding(4, 0, 0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.LightGoldenrodYellow,
                ForeColor = Color.Black,
                Font = new Font(Font.FontFamily, 9F, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 0;
            button.Paint += (_, e) => PaintHelpButton(button, e);
            _toolTip.SetToolTip(button, helpText);
            return button;
        }

        private static void PaintHelpButton(Button button, PaintEventArgs e)
        {
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, button.Width - 1, button.Height - 1);
            button.Region = new Region(path);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var bounds = new Rectangle(1, 1, button.Width - 3, button.Height - 3);
            using var fillBrush = new SolidBrush(Color.LightGoldenrodYellow);
            using var shadowPen = new Pen(Color.DarkGoldenrod);
            using var highlightPen = new Pen(Color.White);
            using var textBrush = new SolidBrush(Color.Black);

            e.Graphics.FillEllipse(fillBrush, bounds);
            e.Graphics.DrawArc(highlightPen, bounds, 135, 180);
            e.Graphics.DrawArc(shadowPen, bounds, -45, 180);

            var textSize = e.Graphics.MeasureString("?", button.Font);
            float textX = (button.Width - textSize.Width) / 2F + 0.5F;
            float textY = (button.Height - textSize.Height) / 2F - 0.5F;
            e.Graphics.DrawString("?", button.Font, textBrush, textX, textY);
        }

        private void ShowValidation(string message)
        {
            SetStatus(message, true);
            MessageBox.Show(message, "Lazy App Config", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void SetStatus(string message, bool error = false)
        {
            _statusLabel.Text = message;
            _statusLabel.ForeColor = error ? Color.Firebrick : Color.DimGray;
        }

        private sealed record EntryRef(string Text, string Id)
        {
            public override string ToString() => Text;
        }

        private sealed record TouchCoordinateMapper(int MinX, int MaxX, int MinY, int MaxY, int ScreenWidth, int ScreenHeight)
        {
            public (int x, int y) Map(int rawX, int rawY)
            {
                int x = Scale(rawX, MinX, MaxX, ScreenWidth);
                int y = Scale(rawY, MinY, MaxY, ScreenHeight);
                return (x, y);
            }

            private static int Scale(int raw, int minimum, int maximum, int size)
            {
                double ratio = (raw - minimum) / (double)(maximum - minimum);
                ratio = Math.Max(0D, Math.Min(1D, ratio));
                return (int)Math.Round(ratio * Math.Max(0, size - 1));
            }
        }

        private sealed class TouchRangeBuilder
        {
            public int? MinX { get; set; }
            public int? MaxX { get; set; }
            public int? MinY { get; set; }
            public int? MaxY { get; set; }
        }
    }
}
