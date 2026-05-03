using Newtonsoft.Json.Linq;

namespace Lazy_App_Codex_Core
{
    public sealed class ConfigEditorForm : Form
    {
        private sealed record ConfigCategory(string Key, string Text);

        private static readonly ConfigCategory[] Categories =
        {
            new ConfigCategory("settings", "SETTINGS"),
            new ConfigCategory("offset", "OFFSET"),
            new ConfigCategory("scripts", "SCRIPTS")
        };

        private const string StartHotkeyKey = "hotkeyStart";
        private const string StopHotkeyKey = "hotkeyStop";
        private const string StartHotkeyLabel = "1 Start";
        private const string StopHotkeyLabel = "1 Stop";

        private readonly ScriptConfigRepository _repository;
        private readonly TabControl _categoryTabs = new TabControl();
        private readonly ListBox _entryList = new ListBox();
        private readonly TextBox _keyTextBox = new TextBox();
        private readonly TextBox _valueTextBox = new TextBox();
        private readonly NumericUpDown _offsetXBox = new NumericUpDown();
        private readonly NumericUpDown _offsetYBox = new NumericUpDown();
        private readonly NumericUpDown _durationBox = new NumericUpDown();
        private readonly NumericUpDown _intervalMinBox = new NumericUpDown();
        private readonly NumericUpDown _intervalMaxBox = new NumericUpDown();
        private readonly DataGridView _stepGrid = new DataGridView();
        private readonly Label _stepTotalLabel = new Label();
        private readonly Panel _settingsEditor = new Panel();
        private readonly Panel _offsetEditor = new Panel();
        private readonly Panel _scriptEditor = new Panel();
        private readonly Button _newButton = new Button();
        private readonly Button _saveEntryButton = new Button();
        private readonly Button _removeButton = new Button();
        private readonly Button _saveConfigButton = new Button();
        private readonly Button _cancelButton = new Button();
        private readonly Button _settingsHelpButton = new Button();
        private readonly Button _offsetHelpButton = new Button();
        private readonly Button _stepHelpButton = new Button();
        private readonly Label _statusLabel = new Label();
        private readonly ToolTip _toolTip = new ToolTip();

        private JObject _root;
        private string? _selectedKey;

        public bool ConfigSaved { get; private set; }

        public ConfigEditorForm(ScriptConfigRepository repository)
        {
            _repository = repository;
            _root = _repository.LoadRawConfig();

            Text = "Config";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(840, 540);
            Size = new Size(980, 680);
            ShowIcon = false;

            BuildLayout();
            LoadCategories();
            LoadEntries();
        }

        private string CurrentCategory => Categories[Math.Max(0, _categoryTabs.SelectedIndex)].Key;

        private string CurrentCategoryText => Categories[Math.Max(0, _categoryTabs.SelectedIndex)].Text;

        private JObject CurrentCategoryObject => (JObject)_root[CurrentCategory]!;

        private bool IsSettingsCategory => CurrentCategory == "settings";

        private void BuildLayout()
        {
            _categoryTabs.Dock = DockStyle.Top;
            _categoryTabs.Height = 36;
            _categoryTabs.SelectedIndexChanged += (_, _) => LoadEntries();

            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _entryList.Dock = DockStyle.Fill;
            _entryList.IntegralHeight = false;
            _entryList.SelectedIndexChanged += (_, _) => LoadSelectedEntry();

            var leftButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 40,
                Padding = new Padding(6, 4, 6, 4),
                WrapContents = false
            };

            ConfigureButton(_newButton, "Add", "Add a new item in this tab", (_, _) => StartNewEntry(), 76);
            ConfigureButton(_removeButton, "Remove", "Remove the selected item", (_, _) => RemoveSelectedEntry(), 76);
            leftButtons.Controls.Add(_newButton);
            leftButtons.Controls.Add(_removeButton);
            var listPanel = new Panel { Dock = DockStyle.Fill };
            listPanel.Controls.Add(_entryList);
            listPanel.Controls.Add(leftButtons);

            var editor = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(10)
            };
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            var keyLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Name",
                TextAlign = ContentAlignment.MiddleLeft
            };
            _keyTextBox.Dock = DockStyle.Fill;

            var editorHost = new Panel { Dock = DockStyle.Fill };
            BuildSettingsEditor();
            BuildOffsetEditor();
            BuildScriptEditor();
            editorHost.Controls.Add(_settingsEditor);
            editorHost.Controls.Add(_offsetEditor);
            editorHost.Controls.Add(_scriptEditor);

            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.ForeColor = Color.DimGray;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;

            var bottomButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 7, 0, 0),
                WrapContents = false
            };
            ConfigureButton(_saveEntryButton, "Save Item", "Save this item inside the popup", (_, _) => SaveEntry(), 108);
            ConfigureButton(_saveConfigButton, "Save All & Close", "Write all changes to config.json", (_, _) => SaveConfig(), 138);
            ConfigureButton(_cancelButton, "Close", "Close without writing unsaved popup changes to config.json", (_, _) => Close(), 70);
            bottomButtons.Controls.Add(_cancelButton);
            bottomButtons.Controls.Add(_saveConfigButton);
            bottomButtons.Controls.Add(_saveEntryButton);

            editor.Controls.Add(keyLabel, 0, 0);
            editor.Controls.Add(_keyTextBox, 0, 1);
            editor.Controls.Add(editorHost, 0, 2);
            editor.Controls.Add(_statusLabel, 0, 3);
            editor.Controls.Add(bottomButtons, 0, 4);
            main.Controls.Add(listPanel, 0, 0);
            main.Controls.Add(editor, 1, 0);

            Controls.Add(main);
            Controls.Add(_categoryTabs);
        }

        private void BuildSettingsEditor()
        {
            _settingsEditor.Dock = DockStyle.Fill;
            var layout = CreateSimpleEditorLayout("Hotkey");
            _valueTextBox.Dock = DockStyle.Top;
            layout.Controls.Add(_valueTextBox, 0, 1);
            ConfigureHelpButton(_settingsHelpButton, GetSettingsHelpText());
            layout.Controls.Add(_settingsHelpButton, 0, 2);
            _settingsEditor.Controls.Add(layout);
        }

        private void BuildOffsetEditor()
        {
            _offsetEditor.Dock = DockStyle.Fill;
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 2,
                Height = 70
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            AddNumberField(layout, "X", _offsetXBox, 0);
            AddNumberField(layout, "Y", _offsetYBox, 1);
            var host = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 2,
                Height = 102
            };
            host.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            host.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            ConfigureHelpButton(_offsetHelpButton, GetOffsetHelpText());
            host.Controls.Add(layout, 0, 0);
            host.Controls.Add(_offsetHelpButton, 0, 1);
            _offsetEditor.Controls.Add(host);
        }

        private void BuildScriptEditor()
        {
            _scriptEditor.Dock = DockStyle.Fill;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3
            };
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            AddNumberField(header, "Loop Count", _durationBox, 0);
            AddNumberField(header, "Interval Min", _intervalMinBox, 1);
            AddNumberField(header, "Interval Max", _intervalMaxBox, 2);
            ConfigureHelpButton(_stepHelpButton, GetStepHelpText());
            header.Controls.Add(_stepHelpButton, 0, 2);
            header.SetColumnSpan(_stepHelpButton, 3);

            _stepGrid.Dock = DockStyle.Fill;
            _stepGrid.AllowUserToAddRows = true;
            _stepGrid.AllowUserToDeleteRows = true;
            _stepGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _stepGrid.RowHeadersWidth = 35;
            _stepGrid.Columns.Add(CreateActionColumn());
            _stepGrid.Columns.Add(CreateTextColumn("x", "X", 58, "Screen X coordinate for click, or drag start X."));
            _stepGrid.Columns.Add(CreateTextColumn("y", "Y", 58, "Screen Y coordinate for click, or drag start Y."));
            _stepGrid.Columns.Add(CreateTextColumn("x2", "X2", 58, "For drag only: drag end X. Leave blank for click."));
            _stepGrid.Columns.Add(CreateTextColumn("y2", "Y2", 58, "For drag only: drag end Y. Leave blank for click."));
            _stepGrid.Columns.Add(CreateTextColumn("randX", "RX", 52, "Random X range. Use 0 if no random movement is needed."));
            _stepGrid.Columns.Add(CreateTextColumn("randY", "RY", 52, "Random Y range. Use 0 if no random movement is needed."));
            _stepGrid.Columns.Add(CreateTextColumn("sleepMin", "Min", 56, "Minimum wait time after this step, in seconds."));
            _stepGrid.Columns.Add(CreateTextColumn("sleepMax", "Max", 56, "Maximum wait time after this step, in seconds."));
            _stepGrid.RowsAdded += (_, _) => UpdateStepTotals();
            _stepGrid.RowsRemoved += (_, _) => UpdateStepTotals();
            _stepGrid.CellValidating += StepGrid_CellValidating;
            _stepGrid.CellValueChanged += StepGrid_CellValueChanged;
            _stepGrid.CellEndEdit += (_, e) =>
            {
                ValidateStepCell(_stepGrid.Rows[e.RowIndex], _stepGrid.Columns[e.ColumnIndex].Name);
                UpdateStepTotals();
            };
            _stepGrid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (_stepGrid.IsCurrentCellDirty)
                {
                    _stepGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };

            _stepTotalLabel.Dock = DockStyle.Fill;
            _stepTotalLabel.TextAlign = ContentAlignment.MiddleLeft;
            _stepTotalLabel.Font = new Font(_stepTotalLabel.Font, FontStyle.Bold);
            UpdateStepTotals();

            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(_stepTotalLabel, 0, 1);
            layout.Controls.Add(_stepGrid, 0, 2);
            _scriptEditor.Controls.Add(layout);
        }

        private static TableLayoutPanel CreateSimpleEditorLayout(string labelText)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 3,
                Height = 94
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = labelText,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            return layout;
        }

        private static void AddNumberField(TableLayoutPanel layout, string label, NumericUpDown input, int column)
        {
            input.Dock = DockStyle.Fill;
            input.Minimum = -100000;
            input.Maximum = 100000;

            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = label,
                TextAlign = ContentAlignment.MiddleLeft
            }, column, 0);
            layout.Controls.Add(input, column, 1);
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(string name, string header, int width, string tooltip)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                ToolTipText = tooltip,
                MinimumWidth = 38,
                Width = width
            };
        }

        private static DataGridViewComboBoxColumn CreateActionColumn()
        {
            var column = new DataGridViewComboBoxColumn
            {
                Name = "act",
                HeaderText = "Act",
                ToolTipText = "Choose left, right, or drag.",
                MinimumWidth = 58,
                Width = 66,
                FlatStyle = FlatStyle.Flat
            };
            column.Items.AddRange("left", "right", "drag");
            return column;
        }

        private void ConfigureButton(Button button, string text, string tooltip, EventHandler handler, int width)
        {
            button.Text = text;
            button.Size = new Size(width, 30);
            button.Margin = new Padding(4, 3, 4, 3);
            button.UseVisualStyleBackColor = true;
            button.Click += handler;
            _toolTip.SetToolTip(button, tooltip);
        }

        private void ConfigureHelpButton(Button button, string helpText)
        {
            button.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            button.Size = new Size(24, 24);
            button.Text = "";
            button.Font = new Font(button.Font.FontFamily, 9F, FontStyle.Bold);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Color.LightGoldenrodYellow;
            button.ForeColor = Color.Black;
            button.UseVisualStyleBackColor = false;
            button.Paint += (_, e) => PaintHelpButton(button, e);
            button.MouseEnter += (_, _) => _toolTip.Show(helpText, button, button.Width + 4, 0, 12000);
            button.MouseLeave += (_, _) => _toolTip.Hide(button);
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

        private static string GetStepHelpText()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "Act: input left, right, or drag",
                "X/Y: click position, or drag start position",
                "X2/Y2: drag end position; leave blank for click",
                "RX/RY: random movement range; use 0 if not needed",
                "Min/Max: wait time after this step, in seconds"
            });
        }

        private static string GetSettingsHelpText()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "1 Start: global hotkey to start running",
                "1 Stop: global hotkey to stop running",
                "Example: CTRL+ALT+S or ALT+S",
                "Leave value empty to disable that hotkey"
            });
        }

        private static string GetOffsetHelpText()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "s<number>: used when script name contains that number",
                "Example: s90 is used by scripts with 90 in the name",
                "x/y or offsetX/offsetY: fallback for other scripts",
                "X/Y: distance moved for each offset step selected in main window"
            });
        }

        private void LoadCategories()
        {
            _categoryTabs.TabPages.Clear();
            foreach (var category in Categories)
            {
                _categoryTabs.TabPages.Add(category.Key, category.Text);
            }

            _categoryTabs.SelectedIndex = 0;
        }

        private void LoadEntries()
        {
            EnsureRequiredSettings();
            _entryList.BeginUpdate();
            _entryList.Items.Clear();
            if (IsSettingsCategory)
            {
                _entryList.Items.Add(StartHotkeyLabel);
                _entryList.Items.Add(StopHotkeyLabel);
            }
            else
            {
                foreach (var property in CurrentCategoryObject.Properties())
                {
                    _entryList.Items.Add(property.Name);
                }
            }
            _entryList.EndUpdate();

            _selectedKey = null;
            _keyTextBox.Clear();
            _keyTextBox.ReadOnly = IsSettingsCategory;
            _newButton.Enabled = !IsSettingsCategory;
            _removeButton.Enabled = !IsSettingsCategory;
            ClearEditors();
            ShowCurrentEditor();
            UpdateActionLabels();
            SetStatus($"Editing {CurrentCategoryText}.");
        }

        private void LoadSelectedEntry()
        {
            if (_entryList.SelectedItem == null)
            {
                return;
            }

            _selectedKey = _entryList.SelectedItem.ToString();
            if (string.IsNullOrWhiteSpace(_selectedKey))
            {
                return;
            }

            _keyTextBox.Text = _selectedKey;
            string configKey = GetConfigKeyFromDisplayKey(_selectedKey);
            JToken? value = CurrentCategoryObject[configKey];
            ClearEditors();

            if (CurrentCategory == "settings")
            {
                _valueTextBox.Text = value?.ToString() ?? "";
            }
            else if (CurrentCategory == "offset")
            {
                LoadOffset(value);
            }
            else
            {
                LoadScript(value as JObject);
            }

            ShowCurrentEditor();
            SetStatus("Edit the fields, then save the entry.");
        }

        private void StartNewEntry()
        {
            if (IsSettingsCategory)
            {
                SetStatus("SETTINGS has only 1 Start and 1 Stop. They cannot be added.");
                return;
            }

            _entryList.ClearSelected();
            _selectedKey = null;
            _keyTextBox.Clear();
            ClearEditors();
            ShowCurrentEditor();
            UpdateActionLabels();

            if (CurrentCategory == "scripts")
            {
                _intervalMaxBox.Value = 1;
            }

            _keyTextBox.Focus();
            SetStatus("Add a new entry inside the selected tab.");
        }

        private void SaveEntry()
        {
            string key = _keyTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                ShowValidation("Name is required.");
                return;
            }

            JToken value;
            try
            {
                if (CurrentCategory == "scripts" && !ValidateStepGridNumbers())
                {
                    SetStatus("Fix highlighted cells before saving.", true);
                    return;
                }

                value = CurrentCategory switch
                {
                    "settings" => new JValue(_valueTextBox.Text),
                    "offset" => new JArray((int)_offsetXBox.Value, (int)_offsetYBox.Value),
                    _ => BuildScriptValue()
                };
            }
            catch (InvalidOperationException ex)
            {
                ShowValidation(ex.Message);
                return;
            }

            var category = CurrentCategoryObject;
            string configKey = IsSettingsCategory ? GetConfigKeyFromDisplayKey(key) : key;
            string? selectedConfigKey = string.IsNullOrWhiteSpace(_selectedKey) ? null : GetConfigKeyFromDisplayKey(_selectedKey);
            if (!IsSettingsCategory && !string.IsNullOrWhiteSpace(selectedConfigKey) && !configKey.Equals(selectedConfigKey, StringComparison.Ordinal))
            {
                category.Property(selectedConfigKey!)?.Remove();
            }

            category[configKey] = value;
            _selectedKey = key;
            LoadEntries();
            SelectEntry(key);
            SetStatus($"{GetEntryName()} saved in this window. Click Save All & Close to update config.json.");
        }

        private void RemoveSelectedEntry()
        {
            if (IsSettingsCategory)
            {
                SetStatus("1 Start and 1 Stop are required settings. They cannot be removed.");
                return;
            }

            string? key = _entryList.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            var result = MessageBox.Show($"Remove {CurrentCategoryText}.{key}?", "Remove Config", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
            {
                return;
            }

            CurrentCategoryObject.Property(key)?.Remove();
            LoadEntries();
            SetStatus("Item removed in this window. Click Save All & Close to update config.json.");
        }

        private void SaveConfig()
        {
            try
            {
                EnsureRequiredSettings();
                _repository.SaveRawConfig(_root);
                ConfigSaved = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save config.json. " + ex.Message, "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearEditors()
        {
            _valueTextBox.Clear();
            _offsetXBox.Value = 0;
            _offsetYBox.Value = 0;
            _durationBox.Value = 0;
            _intervalMinBox.Value = 0;
            _intervalMaxBox.Value = 0;
            _stepGrid.Rows.Clear();
            UpdateStepTotals();
        }

        private void ShowCurrentEditor()
        {
            _settingsEditor.Visible = CurrentCategory == "settings";
            _offsetEditor.Visible = CurrentCategory == "offset";
            _scriptEditor.Visible = CurrentCategory == "scripts";
        }

        private void UpdateActionLabels()
        {
            _saveEntryButton.Text = CurrentCategory switch
            {
                "settings" => "Save Setting",
                "offset" => "Save Offset",
                _ => "Save Script"
            };
            _toolTip.SetToolTip(_saveEntryButton, $"Save the selected {GetEntryName().ToLowerInvariant()} inside this popup");
        }

        private void EnsureRequiredSettings()
        {
            var settings = (JObject)_root["settings"]!;
            if (settings[StartHotkeyKey] == null && settings["hotkeyStartStopToggle"] != null)
            {
                settings[StartHotkeyKey] = settings["hotkeyStartStopToggle"]!.DeepClone();
            }

            settings.Property("hotkeyStartStopToggle")?.Remove();
            settings[StartHotkeyKey] ??= "CTRL+ALT+S";
            settings[StopHotkeyKey] ??= "CTRL+ALT+D";
        }

        private static string GetConfigKeyFromDisplayKey(string key)
        {
            return key switch
            {
                StartHotkeyLabel => StartHotkeyKey,
                StopHotkeyLabel => StopHotkeyKey,
                _ => key
            };
        }

        private string GetEntryName()
        {
            return CurrentCategory switch
            {
                "settings" => "Setting",
                "offset" => "Offset",
                _ => "Script"
            };
        }

        private void LoadOffset(JToken? value)
        {
            if (value is JArray array)
            {
                _offsetXBox.Value = ClampNumeric(ReadInt(array.ElementAtOrDefault(0), 0));
                _offsetYBox.Value = ClampNumeric(ReadInt(array.ElementAtOrDefault(1), 0));
                return;
            }

            _offsetXBox.Value = ClampNumeric(ReadInt(value, 0));
        }

        private void LoadScript(JObject? script)
        {
            if (script == null)
            {
                _intervalMaxBox.Value = 1;
                return;
            }

            _durationBox.Value = ClampNumeric(ReadInt(GetToken(script, "d", "duration"), 0));
            _intervalMinBox.Value = ClampNumeric(ReadInt(GetToken(script, "imin", "interval_min", "interval", "i"), 0, 0));
            _intervalMaxBox.Value = ClampNumeric(ReadInt(GetToken(script, "imax", "interval_max", "interval", "i"), 1, 1));

            var steps = script["config"] as JArray ?? script["steps"] as JArray;
            if (steps == null)
            {
                return;
            }

            foreach (var step in ExpandSteps(steps))
            {
                AddStepRow(step as JObject);
            }
            UpdateStepTotals();
        }

        private JObject BuildScriptValue()
        {
            var config = new JArray();
            foreach (DataGridViewRow row in _stepGrid.Rows)
            {
                if (row.IsNewRow || IsEmptyRow(row))
                {
                    continue;
                }

                string action = NormalizeGridAction(ReadCell(row, "act", "left"));
                var step = new JObject
                {
                    ["a"] = action,
                    ["s"] = new JArray(ParseCellInt(row, "x"), ParseCellInt(row, "y")),
                    ["r"] = new JArray(ParseCellInt(row, "randX"), ParseCellInt(row, "randY")),
                    ["t"] = new JArray(ParseCellInt(row, "sleepMin"), ParseCellInt(row, "sleepMax"))
                };

                int? x2 = ParseOptionalCellInt(row, "x2");
                int? y2 = ParseOptionalCellInt(row, "y2");
                if (action == "drag" && (x2.HasValue || y2.HasValue))
                {
                    step["s2"] = new JArray(x2 ?? 0, y2 ?? 0);
                }

                config.Add(step);
            }

            return new JObject
            {
                ["d"] = (int)_durationBox.Value,
                ["imin"] = (int)_intervalMinBox.Value,
                ["imax"] = (int)_intervalMaxBox.Value,
                ["config"] = config
            };
        }

        private void UpdateStepTotals()
        {
            int totalMin = 0;
            int totalMax = 0;

            foreach (DataGridViewRow row in _stepGrid.Rows)
            {
                if (row.IsNewRow || IsEmptyRow(row))
                {
                    continue;
                }

                totalMin += ReadCellIntOrZero(row, "sleepMin");
                totalMax += ReadCellIntOrZero(row, "sleepMax");
            }

            _stepTotalLabel.Text = $"Grid total time: Min {totalMin}s | Max {totalMax}s";
        }

        private void AddStepRow(JObject? step)
        {
            if (step == null)
            {
                return;
            }

            int rowIndex = _stepGrid.Rows.Add();
            var row = _stepGrid.Rows[rowIndex];
            row.Cells["act"].Value = NormalizeGridAction(ReadString(GetToken(step, "a", "act"), "left"));
            row.Cells["x"].Value = ReadInt(GetToken(step, "s", "scr", "p", "x", "scrX", "posX"), 0, 0);
            row.Cells["y"].Value = ReadInt(GetToken(step, "s", "scr", "p", "y", "scrY", "posY"), 0, 1);
            row.Cells["x2"].Value = ReadNullableInt(GetToken(step, "s2", "scr2", "p2", "x2", "scrX2", "posX2"), 0);
            row.Cells["y2"].Value = ReadNullableInt(GetToken(step, "s2", "scr2", "p2", "y2", "scrY2", "posY2"), 1);
            row.Cells["randX"].Value = ReadInt(GetToken(step, "r", "rand", "rx", "randX"), 0, 0);
            row.Cells["randY"].Value = ReadInt(GetToken(step, "r", "rand", "ry", "randY"), 0, 1);
            row.Cells["sleepMin"].Value = ReadInt(GetToken(step, "t", "sleep", "smin", "sleep_min"), 0, 0);
            row.Cells["sleepMax"].Value = ReadInt(GetToken(step, "t", "sleep", "smax", "sleep_max"), 0, 1);
            ApplyStepRowState(row);
        }

        private void StepGrid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            var row = _stepGrid.Rows[e.RowIndex];
            if (row.IsNewRow || IsEmptyRow(row))
            {
                return;
            }

            string columnName = _stepGrid.Columns[e.ColumnIndex].Name;
            if (!IsNumericStepColumn(columnName) || IsDisabledDragEndCell(row, columnName))
            {
                return;
            }

            ValidateStepCell(row, columnName, e.FormattedValue?.ToString());
        }

        private void StepGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            if (_stepGrid.Columns[e.ColumnIndex].Name == "act")
            {
                ApplyStepRowState(_stepGrid.Rows[e.RowIndex]);
            }

            UpdateStepTotals();
        }

        private bool ValidateStepGridNumbers()
        {
            bool valid = true;
            foreach (DataGridViewRow row in _stepGrid.Rows)
            {
                if (row.IsNewRow || IsEmptyRow(row))
                {
                    continue;
                }

                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (IsNumericStepColumn(cell.OwningColumn.Name) && !ValidateStepCell(row, cell.OwningColumn.Name))
                    {
                        valid = false;
                    }
                }
            }

            return valid;
        }

        private bool ValidateStepCell(DataGridViewRow row, string columnName, string? editedText = null)
        {
            if (row.IsNewRow || IsEmptyRow(row) || !IsNumericStepColumn(columnName))
            {
                return true;
            }

            var cell = row.Cells[columnName];
            if (IsDisabledDragEndCell(row, columnName))
            {
                ClearInvalidCell(cell);
                ClearRowErrorIfValid(row);
                return true;
            }

            string text = editedText ?? cell.Value?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(text) && IsOptionalNumericStepColumn(columnName))
            {
                ClearInvalidCell(cell);
                ClearRowErrorIfValid(row);
                return true;
            }

            if (int.TryParse(text, out _))
            {
                ClearInvalidCell(cell);
                ClearRowErrorIfValid(row);
                return true;
            }

            MarkInvalidCell(cell, $"{cell.OwningColumn.HeaderText} must be a number.");
            row.ErrorText = cell.ErrorText;
            return false;
        }

        private static void ClearRowErrorIfValid(DataGridViewRow row)
        {
            foreach (DataGridViewCell cell in row.Cells)
            {
                if (!string.IsNullOrWhiteSpace(cell.ErrorText))
                {
                    return;
                }
            }

            row.ErrorText = "";
        }

        private void ApplyStepRowState(DataGridViewRow row)
        {
            if (row.IsNewRow)
            {
                return;
            }

            string action = NormalizeGridAction(row.Cells["act"].Value?.ToString() ?? "left");
            bool drag = action == "drag";
            SetDragEndCellState(row.Cells["x2"], drag);
            SetDragEndCellState(row.Cells["y2"], drag);
        }

        private static void SetDragEndCellState(DataGridViewCell cell, bool enabled)
        {
            cell.ReadOnly = !enabled;
            if (!enabled)
            {
                cell.Value = null;
                ClearInvalidCell(cell);
            }

            if (!string.IsNullOrWhiteSpace(cell.ErrorText))
            {
                return;
            }

            cell.Style.BackColor = enabled ? SystemColors.Window : SystemColors.Control;
            cell.Style.ForeColor = enabled ? SystemColors.ControlText : SystemColors.GrayText;
            cell.Style.SelectionBackColor = enabled ? SystemColors.Highlight : SystemColors.ControlDark;
            cell.Style.SelectionForeColor = enabled ? SystemColors.HighlightText : SystemColors.GrayText;
        }

        private static void MarkInvalidCell(DataGridViewCell cell, string message)
        {
            cell.ErrorText = message;
            cell.Style.BackColor = Color.MistyRose;
            cell.Style.ForeColor = Color.DarkRed;
            cell.Style.SelectionBackColor = Color.LightCoral;
            cell.Style.SelectionForeColor = Color.DarkRed;
            cell.ToolTipText = message;
        }

        private static void ClearInvalidCell(DataGridViewCell cell)
        {
            cell.ErrorText = "";
            cell.ToolTipText = "";
            cell.Style.BackColor = SystemColors.Window;
            cell.Style.ForeColor = SystemColors.ControlText;
            cell.Style.SelectionBackColor = SystemColors.Highlight;
            cell.Style.SelectionForeColor = SystemColors.HighlightText;
        }

        private static bool IsDisabledDragEndCell(DataGridViewRow row, string columnName)
        {
            if (columnName != "x2" && columnName != "y2")
            {
                return false;
            }

            string action = NormalizeGridAction(row.Cells["act"].Value?.ToString() ?? "left");
            return action != "drag";
        }

        private static bool IsNumericStepColumn(string columnName)
        {
            return columnName is "x" or "y" or "x2" or "y2" or "randX" or "randY" or "sleepMin" or "sleepMax";
        }

        private static bool IsOptionalNumericStepColumn(string columnName)
        {
            return columnName is "x2" or "y2";
        }

        private static IEnumerable<JToken> ExpandSteps(JArray rawSteps)
        {
            foreach (var item in rawSteps)
            {
                if (item is not JObject stepObj)
                {
                    continue;
                }

                var nested = stepObj["steps"] as JArray;
                int repeat = ReadInt(GetToken(stepObj, "repeat", "rep"), 1);
                if (nested == null)
                {
                    yield return stepObj;
                    continue;
                }

                for (int i = 0; i < Math.Max(1, repeat); i++)
                {
                    foreach (var nestedStep in ExpandSteps(nested))
                    {
                        yield return nestedStep;
                    }
                }
            }
        }

        private static JToken? GetToken(JObject source, params string[] aliases)
        {
            foreach (string alias in aliases)
            {
                var token = source.GetValue(alias, StringComparison.OrdinalIgnoreCase);
                if (token != null)
                {
                    return token;
                }
            }

            return null;
        }

        private static int ReadInt(JToken? value, int fallback, int index = -1)
        {
            if (value is JArray array && index >= 0)
            {
                value = array.ElementAtOrDefault(index);
            }

            return int.TryParse(value?.ToString(), out int parsed) ? parsed : fallback;
        }

        private static int? ReadNullableInt(JToken? value, int index)
        {
            if (value is JArray array)
            {
                value = array.ElementAtOrDefault(index);
            }

            return int.TryParse(value?.ToString(), out int parsed) ? parsed : null;
        }

        private static string ReadString(JToken? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value?.ToString()) ? fallback : value!.ToString();
        }

        private static decimal ClampNumeric(int value)
        {
            return Math.Max(-100000, Math.Min(100000, value));
        }

        private static bool IsEmptyRow(DataGridViewRow row)
        {
            foreach (DataGridViewCell cell in row.Cells)
            {
                if (!string.IsNullOrWhiteSpace(cell.Value?.ToString()))
                {
                    return false;
                }
            }

            return true;
        }

        private static string ReadCell(DataGridViewRow row, string column, string fallback)
        {
            string? value = row.Cells[column].Value?.ToString();
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static string NormalizeGridAction(string action)
        {
            return action.Trim().ToLowerInvariant() switch
            {
                "leftclick" => "left",
                "rightclick" => "right",
                "left" => "left",
                "right" => "right",
                "drag" => "drag",
                "updrag" => "drag",
                "downdrag" => "drag",
                "leftdrag" => "drag",
                "rightdrag" => "drag",
                _ => "left"
            };
        }

        private static int ParseCellInt(DataGridViewRow row, string column)
        {
            string? value = row.Cells[column].Value?.ToString();
            if (int.TryParse(value, out int parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException($"{row.Cells[column].OwningColumn.HeaderText} must be a number.");
        }

        private static int? ParseOptionalCellInt(DataGridViewRow row, string column)
        {
            string? value = row.Cells[column].Value?.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (int.TryParse(value, out int parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException($"{row.Cells[column].OwningColumn.HeaderText} must be a number.");
        }

        private static int ReadCellIntOrZero(DataGridViewRow row, string column)
        {
            string? value = row.Cells[column].Value?.ToString();
            return int.TryParse(value, out int parsed) ? parsed : 0;
        }

        private void SelectEntry(string key)
        {
            for (int i = 0; i < _entryList.Items.Count; i++)
            {
                if (key.Equals(_entryList.Items[i]?.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    _entryList.SelectedIndex = i;
                    return;
                }
            }
        }

        private void ShowValidation(string message)
        {
            SetStatus(message, true);
            MessageBox.Show(message, "Config", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void SetStatus(string message, bool isError = false)
        {
            _statusLabel.Text = message;
            _statusLabel.ForeColor = isError ? Color.Firebrick : Color.DimGray;
        }
    }
}
