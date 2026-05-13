using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Lazy_App_Codex_Core
{
    public sealed class ConfigEditorForm : Form
    {
        private static readonly string[] OffsetOptions = { "-2:y", "-1:y", "0", "1:y", "2:y", "-2:x", "-1:x", "1:x", "2:x" };

        private readonly ScriptConfigRepository _repository;
        private readonly TabControl _tabs = new();
        private readonly TextBox _searchBox = new();
        private readonly ListBox _entryList = new();
        private readonly Button _addButton = new();
        private readonly Button _cloneButton = new();
        private readonly Button _removeButton = new();
        private readonly Button _moveUpButton = new();
        private readonly Button _moveDownButton = new();
        private readonly Button _saveButton = new();
        private readonly Button _closeButton = new();
        private readonly Label _statusLabel = new();
        private readonly ToolTip _toolTip = new();

        private readonly TextBox _scriptNameBox = new();
        private readonly TextBox _sequenceNameBox = new();
        private readonly TextBox _offsetNameBox = new();
        private readonly TextBox _hotkeyStartBox = new();
        private readonly TextBox _hotkeyStopBox = new();
        private readonly NumericUpDown _offsetXBox = CreateNumberBox();
        private readonly NumericUpDown _offsetYBox = CreateNumberBox();
        private readonly NumericUpDown _loopBox = CreateNumberBox();
        private readonly NumericUpDown _intervalMinBox = CreateNumberBox();
        private readonly NumericUpDown _intervalMaxBox = CreateNumberBox();
        private readonly NumericUpDown _sequenceLoopBox = CreateNumberBox();
        private readonly NumericUpDown _sequenceIntervalMinBox = CreateNumberBox();
        private readonly NumericUpDown _sequenceIntervalMaxBox = CreateNumberBox();
        private readonly CheckBox _defaultOffsetEnabledBox = new();
        private readonly ComboBox _defaultOffsetBox = new();
        private readonly CheckBox _sequenceDefaultOffsetEnabledBox = new();
        private readonly ComboBox _sequenceDefaultOffsetBox = new();
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

        private ConfigLibrary _library;
        private JObject _root;
        private AppSettings _workingSettings;
        private JObject _workingOffsets;
        private ScriptModel? _selectedScript;
        private SequenceModel? _selectedSequence;
        private string? _selectedOffsetKey;
        private int _loadedGroupIndex = -1;
        private bool _dirty;
        private bool _loading;
        private bool _savedAndClosing;
        private int _currentTabIndex;
        private string _activeEditorTab = "scripts";

        public ConfigEditorForm(ScriptConfigRepository repository)
        {
            _repository = repository;
            _root = _repository.LoadRawConfig();
            _library = _repository.LoadLibrary();
            _workingSettings = new AppSettings
            {
                HotkeyStart = _repository.Settings.HotkeyStart,
                HotkeyStop = _repository.Settings.HotkeyStop
            };
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
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
            _searchBox.Dock = DockStyle.Fill;
            _searchBox.PlaceholderText = "Search";
            _searchBox.TextChanged += (_, _) => RefreshEntryList();
            _entryList.Dock = DockStyle.Fill;
            _entryList.IntegralHeight = false;
            _entryList.SelectedIndexChanged += (_, _) => SelectEntryFromList();

            var listButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
            ConfigureButton(_addButton, "Add", (_, _) => AddEntry());
            ConfigureButton(_cloneButton, "Clone", (_, _) => CloneEntry());
            ConfigureButton(_removeButton, "Remove", (_, _) => RemoveEntry());
            ConfigureButton(_moveUpButton, "Move Up", (_, _) => MoveEntry(-1));
            ConfigureButton(_moveDownButton, "Move Down", (_, _) => MoveEntry(1));
            listButtons.Controls.AddRange(new Control[] { _addButton, _cloneButton, _removeButton, _moveUpButton, _moveDownButton });
            left.Controls.Add(_searchBox, 0, 0);
            left.Controls.Add(_entryList, 0, 1);
            left.Controls.Add(listButtons, 0, 2);

            var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(8) };
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
            var editorHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            editorHost.Controls.Add(BuildSequenceEditor());
            editorHost.Controls.Add(BuildScriptEditor());
            editorHost.Controls.Add(BuildOffsetEditor());
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
            var panel = new TableLayoutPanel { Name = "settingsEditor", Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Visible = false };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(Label("Start Hotkey"), 0, 0);
            panel.Controls.Add(CreateHelpButton("Set global start/stop hotkeys. Leave blank to disable a hotkey."), 1, 0);
            panel.Controls.Add(_hotkeyStartBox, 0, 1);
            panel.SetColumnSpan(_hotkeyStartBox, 2);
            panel.Controls.Add(Label("Stop Hotkey"), 0, 2);
            panel.Controls.Add(_hotkeyStopBox, 0, 3);
            panel.SetColumnSpan(_hotkeyStopBox, 2);
            _hotkeyStartBox.Dock = DockStyle.Fill;
            _hotkeyStopBox.Dock = DockStyle.Fill;
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

        private Control BuildScriptEditor()
        {
            var panel = new TableLayoutPanel { Name = "scriptEditor", Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Visible = false };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

            var info = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 4, Padding = new Padding(0, 4, 0, 4) };
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
            info.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            info.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            info.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            info.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            AddTextField(info, "Name", _scriptNameBox, 0);
            AddNumberField(info, "Loop Count", _loopBox, 1);
            AddNumberField(info, "Interval Min", _intervalMinBox, 2);
            AddNumberField(info, "Interval Max", _intervalMaxBox, 3);
            info.Controls.Add(CreateHelpButton("Script info controls the loop count, interval, optional default offset, and action groups saved under config[]."), 4, 0);
            _defaultOffsetEnabledBox.Text = "Enable Default Offset";
            _defaultOffsetEnabledBox.AutoSize = true;
            _defaultOffsetEnabledBox.Dock = DockStyle.Fill;
            _defaultOffsetEnabledBox.Margin = new Padding(0, 4, 4, 2);
            _defaultOffsetEnabledBox.CheckedChanged += (_, _) => _defaultOffsetBox.Enabled = _defaultOffsetEnabledBox.Checked;
            _defaultOffsetBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _defaultOffsetBox.Items.AddRange(OffsetOptions.Cast<object>().ToArray());
            info.Controls.Add(_defaultOffsetEnabledBox, 0, 2);
            info.Controls.Add(Label("Default Offset"), 1, 2);
            info.Controls.Add(_defaultOffsetBox, 1, 3);

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
            _stepGrid.MinimumSize = new Size(0, 120);
            _stepTotalLabel.Dock = DockStyle.Fill;
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
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            var namePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 4, Padding = new Padding(0, 4, 0, 4) };
            namePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            namePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            namePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            namePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            namePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
            namePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            namePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            namePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            namePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            AddTextField(namePanel, "Name", _sequenceNameBox, 0);
            AddNumberField(namePanel, "Loop Count", _sequenceLoopBox, 1);
            AddNumberField(namePanel, "Interval Min", _sequenceIntervalMinBox, 2);
            AddNumberField(namePanel, "Interval Max", _sequenceIntervalMaxBox, 3);
            namePanel.Controls.Add(CreateHelpButton("Sequences run script items and direct action items in order. Sequence items cannot reference another sequence."), 4, 0);
            _sequenceDefaultOffsetEnabledBox.Text = "Enable Default Offset";
            _sequenceDefaultOffsetEnabledBox.AutoSize = true;
            _sequenceDefaultOffsetEnabledBox.Dock = DockStyle.Fill;
            _sequenceDefaultOffsetEnabledBox.Margin = new Padding(0, 4, 4, 2);
            _sequenceDefaultOffsetEnabledBox.CheckedChanged += (_, _) => _sequenceDefaultOffsetBox.Enabled = _sequenceDefaultOffsetEnabledBox.Checked;
            _sequenceDefaultOffsetBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _sequenceDefaultOffsetBox.Items.AddRange(OffsetOptions.Cast<object>().ToArray());
            namePanel.Controls.Add(_sequenceDefaultOffsetEnabledBox, 0, 2);
            namePanel.Controls.Add(Label("Default Offset"), 1, 2);
            namePanel.Controls.Add(_sequenceDefaultOffsetBox, 1, 3);
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

        private void LoadTabs()
        {
            _tabs.TabPages.Add("settings", "Settings");
            _tabs.TabPages.Add("offset", "Offset");
            _tabs.TabPages.Add("scripts", "Scripts");
            _tabs.TabPages.Add("sequences", "Sequences");
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
                "offset" => _selectedOffsetKey,
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
            else if (CurrentTab == "settings")
            {
                _entryList.Items.Add(new EntryRef("Hotkeys", "settings"));
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
            _selectedScript = CurrentTab == "scripts" ? _library.Scripts.FirstOrDefault(s => s.Id == entry.Id) : null;
            _selectedSequence = CurrentTab == "sequences" ? _library.Sequences.FirstOrDefault(s => s.Id == entry.Id) : null;
            _selectedOffsetKey = CurrentTab == "offset" ? entry.Id : null;
        }

        private void LoadCurrentEditor()
        {
            _loading = true;
            ClearEditorValues();
            if (CurrentTab == "settings")
            {
                _hotkeyStartBox.Text = _workingSettings.HotkeyStart;
                _hotkeyStopBox.Text = _workingSettings.HotkeyStop;
            }
            else if (CurrentTab == "offset")
            {
                _offsetNameBox.Text = _selectedOffsetKey ?? "";
                var token = string.IsNullOrWhiteSpace(_selectedOffsetKey) ? null : _workingOffsets[_selectedOffsetKey];
                _offsetXBox.Value = ClampNumeric(ReadOffsetInt(token, 0));
                _offsetYBox.Value = ClampNumeric(ReadOffsetInt(token, 1));
            }
            else if (_selectedScript != null)
            {
                _scriptNameBox.Text = _selectedScript.Name;
                _loopBox.Value = ClampNumeric(_selectedScript.Duration);
                _intervalMinBox.Value = ClampNumeric(_selectedScript.Interval_Min);
                _intervalMaxBox.Value = ClampNumeric(_selectedScript.Interval_Max);
                _defaultOffsetEnabledBox.Checked = _selectedScript.DefaultOffsetEnabled;
                _defaultOffsetBox.Enabled = _defaultOffsetEnabledBox.Checked;
                _defaultOffsetBox.SelectedItem = OffsetOptions.Contains(_selectedScript.DefaultOffset) ? _selectedScript.DefaultOffset : "0";
                RefreshGroupList();
                UpdateGroupButtonStates();
            }
            else if (_selectedSequence != null)
            {
                _sequenceNameBox.Text = _selectedSequence.Name;
                _sequenceLoopBox.Value = ClampNumeric(_selectedSequence.Duration);
                _sequenceIntervalMinBox.Value = ClampNumeric(_selectedSequence.Interval_Min);
                _sequenceIntervalMaxBox.Value = ClampNumeric(_selectedSequence.Interval_Max);
                _sequenceDefaultOffsetEnabledBox.Checked = _selectedSequence.DefaultOffsetEnabled;
                _sequenceDefaultOffsetBox.Enabled = _sequenceDefaultOffsetEnabledBox.Checked;
                _sequenceDefaultOffsetBox.SelectedItem = OffsetOptions.Contains(_selectedSequence.DefaultOffset) ? _selectedSequence.DefaultOffset : "0";
                RefreshSequenceGrid();
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
                "offset" => !string.IsNullOrWhiteSpace(_selectedOffsetKey),
                _ => true
            };

            string editorName = CurrentTab switch
            {
                "scripts" => "scriptEditor",
                "sequences" => "sequenceEditor",
                "offset" => "offsetEditor",
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
            _offsetNameBox.Clear();
            _hotkeyStartBox.Clear();
            _hotkeyStopBox.Clear();
            _offsetXBox.Value = 0;
            _offsetYBox.Value = 0;
            _loopBox.Value = 0;
            _intervalMinBox.Value = 0;
            _intervalMaxBox.Value = 1;
            _sequenceLoopBox.Value = 0;
            _sequenceIntervalMinBox.Value = 0;
            _sequenceIntervalMaxBox.Value = 0;
            _defaultOffsetEnabledBox.Checked = false;
            _defaultOffsetBox.Enabled = false;
            _defaultOffsetBox.SelectedItem = "0";
            _sequenceDefaultOffsetEnabledBox.Checked = false;
            _sequenceDefaultOffsetBox.Enabled = false;
            _sequenceDefaultOffsetBox.SelectedItem = "0";
            _groupList.Items.Clear();
            _loadedGroupIndex = -1;
            _stepGrid.Rows.Clear();
            _sequenceGrid.Rows.Clear();
            UpdateStepTotals();
            UpdateGroupButtonStates();
        }

        private void ShowEditorForCurrentTab()
        {
            foreach (Control control in EditorHost.Controls)
            {
                control.Visible = control.Name switch
                {
                    "settingsEditor" => CurrentTab == "settings",
                    "offsetEditor" => CurrentTab == "offset",
                    "scriptEditor" => CurrentTab == "scripts",
                    "sequenceEditor" => CurrentTab == "sequences",
                    _ => false
                };
            }

            bool listEditable = CurrentTab is "scripts" or "sequences" or "offset";
            _addButton.Enabled = listEditable;
            _cloneButton.Enabled = listEditable;
            _removeButton.Enabled = listEditable;
            _moveUpButton.Enabled = CurrentTab is "scripts" or "sequences";
            _moveDownButton.Enabled = CurrentTab is "scripts" or "sequences";
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
                    Order = _library.Sequences.Count,
                    Duration = 1
                };
                _library.Sequences.Add(sequence);
                _selectedSequence = sequence;
                _dirty = false;
                RefreshEntryList();
                SelectById(sequence.Id);
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
                string message = dependents.Count == 0
                    ? $"Delete script \"{_selectedScript.Name}\"?"
                    : $"Deleting script \"{_selectedScript.Name}\" will also delete:{Environment.NewLine}- " + string.Join(Environment.NewLine + "- ", dependents.Select(seq => seq.Name)) + Environment.NewLine + Environment.NewLine + "Continue?";
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
                if (MessageBox.Show($"Delete sequence \"{_selectedSequence.Name}\"?", "Delete Sequence", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }

                _library.Sequences.Remove(_selectedSequence);
            }
            else if (CurrentTab == "offset" && !string.IsNullOrWhiteSpace(_selectedOffsetKey))
            {
                if (MessageBox.Show($"Delete offset profile \"{_selectedOffsetKey}\"?", "Delete Offset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }

                _workingOffsets.Property(_selectedOffsetKey)?.Remove();
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
                _workingSettings.HotkeyStart = _hotkeyStartBox.Text.Trim();
                _workingSettings.HotkeyStop = _hotkeyStopBox.Text.Trim();
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
                _selectedScript.DefaultOffsetEnabled = _defaultOffsetEnabledBox.Checked;
                _selectedScript.DefaultOffset = _defaultOffsetBox.SelectedItem?.ToString() ?? "0";
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
                _selectedSequence.DefaultOffsetEnabled = _sequenceDefaultOffsetEnabledBox.Checked;
                _selectedSequence.DefaultOffset = _sequenceDefaultOffsetBox.SelectedItem?.ToString() ?? "0";
                _selectedSequence.Items = ReadSequenceItemsFromGrid();
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
            _root["settings"] = new JObject
            {
                ["hotkeyStart"] = _workingSettings.HotkeyStart,
                ["hotkeyStop"] = _workingSettings.HotkeyStop
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

            _root["scripts"] = scripts;
            _root["sequences"] = sequences;
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
                min += ParseInt(row, "sleepMin", 0);
                max += ParseInt(row, "sleepMax", 0);
            }

            int repeat = ReadRepeatBoxValue();
            _stepTotalLabel.Text = $"Group total time: Min {min * repeat}s | Max {max * repeat}s";
        }

        private int ReadRepeatBoxValue()
        {
            return int.TryParse(_groupRepeatBox.Text, out int repeat) ? Math.Max(1, repeat) : Math.Max(1, (int)_groupRepeatBox.Value);
        }

        private void UpdateSequenceTotals()
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
                    min += (stepTotals.min * repeat) + ParseInt(row, "imin", 0);
                    max += (stepTotals.max * repeat) + ParseInt(row, "imax", 0);
                }
                else
                {
                    min += ParseInt(row, "sleepMin", 0);
                    max += ParseInt(row, "sleepMax", 0);
                }
            }

            min += Math.Max(0, (int)_sequenceIntervalMinBox.Value);
            max += Math.Max(0, (int)_sequenceIntervalMaxBox.Value);
            _sequenceTotalLabel.Text = $"Sequence total time: Min {min}s | Max {max}s";
        }

        private static (int min, int max) GetScriptStepTotals(ScriptModel script)
        {
            int min = 0;
            int max = 0;
            foreach (var group in script.Groups)
            {
                int repeat = Math.Max(1, group.Repeat);
                min += group.Steps.Sum(step => step.Sleep_Min) * repeat;
                max += group.Steps.Sum(step => step.Sleep_Max) * repeat;
            }

            return (min, max);
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
                HotkeyStop = _repository.Settings.HotkeyStop
            };
            _workingOffsets = ((JObject)_root["offset"]!).DeepClone() as JObject ?? new JObject();
            _dirty = false;
            RefreshEntryList();
            ConfigSaved = true;
            SetStatus("Config restored.");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
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
            _sequenceNameBox.TextChanged += (_, _) => UpdateSelectedListName(_sequenceNameBox.Text.Trim());
            _sequenceLoopBox.ValueChanged += (_, _) => UpdateSelectedSequenceSettings();
            _sequenceIntervalMinBox.ValueChanged += (_, _) => UpdateSelectedSequenceSettings();
            _sequenceIntervalMaxBox.ValueChanged += (_, _) => UpdateSelectedSequenceSettings();
            _sequenceDefaultOffsetEnabledBox.CheckedChanged += (_, _) => UpdateSelectedSequenceSettings();
            _sequenceDefaultOffsetBox.SelectedIndexChanged += (_, _) => UpdateSelectedSequenceSettings();
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
            _selectedSequence.DefaultOffsetEnabled = _sequenceDefaultOffsetEnabledBox.Checked;
            _selectedSequence.DefaultOffset = _sequenceDefaultOffsetBox.SelectedItem?.ToString() ?? "0";
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
                Order = source.Order,
                Duration = source.Duration,
                Interval_Min = source.Interval_Min,
                Interval_Max = source.Interval_Max,
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
                Order = source.Order,
                Duration = source.Duration,
                Interval_Min = source.Interval_Min,
                Interval_Max = source.Interval_Max,
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
    }
}
