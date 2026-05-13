namespace Lazy_App_Codex_Core
{
    internal sealed class SearchableDropdown : UserControl
    {
        private const string NoMatchesText = "(No records found)";

        private readonly TextBox _displayTextBox = new TextBox();
        private readonly Button _dropButton = new Button();
        private readonly ToolStripDropDown _dropDown = new ToolStripDropDown();
        private readonly TextBox _searchTextBox = new TextBox();
        private readonly ListBox _listBox = new ListBox();
        private readonly List<object> _items = new List<object>();

        private object? _selectedItem;
        private bool _showingNoMatches;
        private string _placeholderText = "";

        public SearchableDropdown()
        {
            Height = 28;
            MinimumSize = new Size(120, 28);

            _displayTextBox.Dock = DockStyle.Fill;
            _displayTextBox.ReadOnly = true;
            _displayTextBox.TabStop = false;
            _displayTextBox.Cursor = Cursors.Default;
            _displayTextBox.Click += (_, _) => ShowDropDown();

            _dropButton.Dock = DockStyle.Right;
            _dropButton.Width = 28;
            _dropButton.Text = "v";
            _dropButton.UseVisualStyleBackColor = true;
            _dropButton.Click += (_, _) => ShowDropDown();

            Controls.Add(_displayTextBox);
            Controls.Add(_dropButton);

            _searchTextBox.BorderStyle = BorderStyle.FixedSingle;
            _searchTextBox.Margin = new Padding(0);
            _searchTextBox.TextChanged += (_, _) => ApplyFilter();
            _searchTextBox.KeyDown += SearchTextBox_KeyDown;

            _listBox.BorderStyle = BorderStyle.FixedSingle;
            _listBox.IntegralHeight = false;
            _listBox.Margin = new Padding(0);
            _listBox.MouseDoubleClick += (_, _) => CommitHighlightedItem();
            _listBox.Click += (_, _) => CommitHighlightedItem();
            _listBox.KeyDown += ListBox_KeyDown;

            var panel = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
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
            _dropDown.Closed += (_, _) => ClearSearch();
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

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            _displayTextBox.Font = Font;
            _dropButton.Font = Font;
            _searchTextBox.Font = Font;
            _listBox.Font = Font;
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            _displayTextBox.Enabled = Enabled;
            _dropButton.Enabled = Enabled;
            if (!Enabled)
            {
                _dropDown.Close();
            }
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
            if (!Enabled)
            {
                return;
            }

            ClearSearch();
            SizeDropDown();
            ApplyFilter();
            _dropDown.Show(this, new Point(0, Height));
            _searchTextBox.Focus();
        }

        private void SizeDropDown()
        {
            int width = Math.Max(Width, 240);
            int visibleRows = Math.Min(Math.Max(_items.Count, 1), 12);
            int listHeight = Math.Max(32, visibleRows * _listBox.ItemHeight + 4);
            int height = 28 + listHeight;

            if (_dropDown.Items[0] is ToolStripControlHost host && host.Control is Control panel)
            {
                panel.Size = new Size(width, height);
                host.Size = panel.Size;
            }
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
