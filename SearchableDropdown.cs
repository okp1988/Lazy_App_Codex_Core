namespace Lazy_App_Codex_Core
{
    internal sealed class SearchableDropdown : UserControl
    {
        private const string NoMatchesText = "(No records found)";

        private readonly TextBox _displayTextBox = new TextBox();
        private readonly Label _dropButton = new Label();
        private readonly ToolStripDropDown _dropDown = new ToolStripDropDown();
        private readonly TextBox _searchTextBox = new TextBox();
        private readonly ListBox _listBox = new ListBox();
        private readonly List<object> _items = new List<object>();

        private object? _selectedItem;
        private bool _showingNoMatches;
        private string _placeholderText = "";
        private bool _suppressNextClickToggle;
        private DateTime _lastDropDownClosedAt = DateTime.MinValue;

        public SearchableDropdown()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            Height = LogicalToDeviceUnits(28);
            MinimumSize = new Size(LogicalToDeviceUnits(160), LogicalToDeviceUnits(28));
            BackColor = SystemColors.Window;
            Padding = new Padding(LogicalToDeviceUnits(5), LogicalToDeviceUnits(5), LogicalToDeviceUnits(1), LogicalToDeviceUnits(1));
            Cursor = Cursors.Hand;
            Click += (_, _) => ToggleDropDownFromClick();
            MouseDown += (_, _) => ToggleDropDownFromMouseDown();

            _displayTextBox.BorderStyle = BorderStyle.None;
            _displayTextBox.Dock = DockStyle.Fill;
            _displayTextBox.ReadOnly = true;
            _displayTextBox.TabStop = false;
            _displayTextBox.Cursor = Cursors.Default;
            _displayTextBox.BackColor = SystemColors.Window;
            _displayTextBox.Click += (_, _) => ToggleDropDownFromClick();
            _displayTextBox.MouseDown += (_, _) => ToggleDropDownFromMouseDown();

            _dropButton.Dock = DockStyle.Right;
            _dropButton.Width = LogicalToDeviceUnits(24);
            _dropButton.Text = "▼";
            _dropButton.TextAlign = ContentAlignment.MiddleCenter;
            _dropButton.BackColor = SystemColors.Window;
            _dropButton.Cursor = Cursors.Hand;
            _dropButton.Margin = Padding.Empty;
            _dropButton.Padding = Padding.Empty;
            _dropButton.Click += (_, _) => ToggleDropDownFromClick();
            _dropButton.MouseDown += (_, _) => ToggleDropDownFromMouseDown();

            // Add the button first, then the fill textbox, so both controls render as one bordered selector.
            Controls.Add(_dropButton);
            Controls.Add(_displayTextBox);

            _searchTextBox.BorderStyle = BorderStyle.FixedSingle;
            _searchTextBox.Margin = new Padding(0);
            _searchTextBox.TextChanged += (_, _) => ApplyFilter();
            _searchTextBox.KeyDown += SearchTextBox_KeyDown;

            _listBox.BorderStyle = BorderStyle.FixedSingle;
            _listBox.IntegralHeight = false;
            _listBox.Margin = new Padding(0);
            _listBox.MouseDoubleClick += (_, _) => CommitHighlightedItem();
            _listBox.Click += (_, _) => CommitHighlightedItem();
            _listBox.MouseMove += ListBox_MouseMove;
            _listBox.KeyDown += ListBox_KeyDown;

            var panel = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Controls.Add(_searchTextBox, 0, 0);
            panel.Controls.Add(_listBox, 0, 1);

            _searchTextBox.Dock = DockStyle.Fill;
            _listBox.Dock = DockStyle.Fill;

            var host = new ToolStripControlHost(panel)
            {
                AutoSize = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            _dropDown.AutoClose = true;
            _dropDown.Padding = Padding.Empty;
            _dropDown.Items.Add(host);
            _dropDown.Closed += (_, _) =>
            {
                _lastDropDownClosedAt = DateTime.UtcNow;
                ClearSearch();
            };
        }

        public event EventHandler? SelectionChanged;

        public object? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (Equals(_selectedItem, value))
                {
                    return;
                }

                _selectedItem = value;
                UpdateDisplayText();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string PlaceholderText
        {
            get => _placeholderText;
            set
            {
                _placeholderText = value;
                UpdateDisplayText();
            }
        }

        public void SetItems(IEnumerable<object> items)
        {
            _items.Clear();
            _items.AddRange(items);

            if (_selectedItem != null && !_items.Contains(_selectedItem))
            {
                _selectedItem = null;
                UpdateDisplayText();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }

            ApplyFilter();
        }

        public void ClearSelection()
        {
            SelectedItem = null;
        }


        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _dropButton.Width = LogicalToDeviceUnits(24);
            Padding = new Padding(LogicalToDeviceUnits(5), LogicalToDeviceUnits(5), LogicalToDeviceUnits(1), LogicalToDeviceUnits(1));
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            _displayTextBox.Font = Font;
            _dropButton.Font = new Font(Font.FontFamily, Math.Max(8F, Font.Size - 1F), FontStyle.Regular);
            _searchTextBox.Font = Font;
            _listBox.Font = Font;
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            _displayTextBox.Enabled = Enabled;
            _dropButton.Enabled = Enabled;
            _displayTextBox.BackColor = Enabled ? SystemColors.Window : SystemColors.Control;
            _dropButton.BackColor = Enabled ? SystemColors.Window : SystemColors.Control;
            BackColor = Enabled ? SystemColors.Window : SystemColors.Control;
            if (!Enabled)
            {
                _dropDown.Close();
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using var pen = new Pen(Enabled ? SystemColors.ActiveBorder : SystemColors.ControlDark);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }


        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            _displayTextBox.BackColor = BackColor;
            _dropButton.BackColor = BackColor;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData is Keys.Enter or Keys.Space or Keys.Down or (Keys.Alt | Keys.Down))
            {
                ShowDropDown();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ShowDropDown()
        {
            if (!Enabled || _dropDown.Visible)
            {
                return;
            }

            ClearSearch();
            SizeDropDown(out Point dropLocation);
            ApplyFilter();
            _dropDown.Show(this, dropLocation, ToolStripDropDownDirection.BelowRight);
            _searchTextBox.Focus();
        }

        private void ToggleDropDownFromMouseDown()
        {
            _suppressNextClickToggle = true;
            if (WasDropDownJustClosed())
            {
                return;
            }

            ToggleDropDown();
        }

        private void ToggleDropDownFromClick()
        {
            if (_suppressNextClickToggle)
            {
                _suppressNextClickToggle = false;
                return;
            }

            ToggleDropDown();
        }

        private void ToggleDropDown()
        {
            if (!Enabled)
            {
                return;
            }

            if (_dropDown.Visible)
            {
                _dropDown.Close();
                return;
            }

            ShowDropDown();
        }

        private bool WasDropDownJustClosed()
        {
            return (DateTime.UtcNow - _lastDropDownClosedAt).TotalMilliseconds < 250;
        }

        private void SizeDropDown(out Point dropLocation)
        {
            Rectangle workingArea = Screen.FromControl(this).WorkingArea;
            Point screenLocation = PointToScreen(Point.Empty);

            int minWidth = LogicalToDeviceUnits(280);
            int width = Math.Max(Width, minWidth);
            width = Math.Min(width, Math.Max(Width, workingArea.Width - 16));

            int searchHeight = LogicalToDeviceUnits(32);
            int maxVisibleRows = 10;
            int visibleRows = Math.Min(Math.Max(_items.Count, 1), maxVisibleRows);
            int requestedListHeight = Math.Max(LogicalToDeviceUnits(32), visibleRows * Math.Max(_listBox.ItemHeight, Font.Height + 6) + 4);
            int requestedHeight = searchHeight + requestedListHeight;

            int spaceBelow = workingArea.Bottom - (screenLocation.Y + Height) - 8;
            int availableHeight = Math.Max(LogicalToDeviceUnits(80), spaceBelow);
            int height = Math.Min(requestedHeight, availableHeight);

            if (_dropDown.Items[0] is ToolStripControlHost host && host.Control is Control panel)
            {
                panel.Size = new Size(width, height);
                host.Size = panel.Size;
            }

            dropLocation = new Point(0, Height);
        }

        private void ApplyFilter()
        {
            string searchText = _searchTextBox.Text.Trim();
            var matches = _items
                .Where(item => string.IsNullOrWhiteSpace(searchText) || item.ToString()?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)
                .Take(20)
                .ToArray();

            _listBox.BeginUpdate();
            _listBox.Items.Clear();
            _showingNoMatches = matches.Length == 0;
            if (_showingNoMatches)
            {
                _listBox.Items.Add(NoMatchesText);
            }
            else
            {
                _listBox.Items.AddRange(matches);
            }

            _listBox.SelectedIndex = _listBox.Items.Count > 0 ? 0 : -1;
            _listBox.EndUpdate();
        }

        private void SearchTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && _listBox.Items.Count > 0)
            {
                _listBox.Focus();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                CommitHighlightedItem();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                _dropDown.Close();
                e.Handled = true;
            }
        }

        private void ListBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CommitHighlightedItem();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                _dropDown.Close();
                e.Handled = true;
            }
        }

        private void ListBox_MouseMove(object? sender, MouseEventArgs e)
        {
            int index = _listBox.IndexFromPoint(e.Location);
            if (index >= 0 && index < _listBox.Items.Count && _listBox.SelectedIndex != index)
            {
                _listBox.SelectedIndex = index;
            }
        }

        private void CommitHighlightedItem()
        {
            if (_showingNoMatches || _listBox.SelectedItem == null)
            {
                return;
            }

            SelectedItem = _listBox.SelectedItem;
            _dropDown.Close();
            ClearSearch();
        }

        private void ClearSearch()
        {
            if (_searchTextBox.Text.Length == 0)
            {
                return;
            }

            _searchTextBox.Clear();
        }

        private void UpdateDisplayText()
        {
            _displayTextBox.Text = _selectedItem?.ToString() ?? _placeholderText;
        }
    }
}
