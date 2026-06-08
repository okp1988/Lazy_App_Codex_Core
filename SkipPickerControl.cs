namespace Lazy_App_Codex_Core
{
    internal interface ISkipPickerOption
    {
        string Label { get; }
        string Detail { get; }
    }

    internal sealed class SkipPickerControl : UserControl
    {
        private const string PlaceholderText = "No Skip";
        private const int ListItemHeight = 28;

        private readonly Label _displayLabel = new();
        private readonly Label _dropButton = new();
        private readonly ToolStripDropDown _dropDown = new();
        private readonly TableLayoutPanel _popupLayout = new();
        private readonly ListBox _listBox = new();
        private readonly Panel _detailPanel = new();
        private readonly Label _detailHeader = new();
        private readonly TextBox _detailText = new();
        private readonly List<object> _items = new();

        private object? _selectedItem;
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private bool _suppressNextClickToggle;
        private DateTime _lastDropDownClosedAt = DateTime.MinValue;

        public SkipPickerControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            Height = LogicalToDeviceUnits(24);
            MinimumSize = new Size(LogicalToDeviceUnits(120), LogicalToDeviceUnits(24));
            Padding = new Padding(LogicalToDeviceUnits(5), LogicalToDeviceUnits(2), LogicalToDeviceUnits(1), LogicalToDeviceUnits(1));
            BackColor = SystemColors.Window;
            Cursor = Cursors.Hand;
            Click += (_, _) => ToggleDropDownFromClick();
            MouseDown += (_, _) => ToggleDropDownFromMouseDown();

            _dropButton.Dock = DockStyle.Right;
            _dropButton.Width = LogicalToDeviceUnits(24);
            _dropButton.Text = "";
            _dropButton.TextAlign = ContentAlignment.MiddleCenter;
            _dropButton.BackColor = SystemColors.Window;
            _dropButton.ForeColor = Color.FromArgb(68, 74, 80);
            _dropButton.Cursor = Cursors.Hand;
            _dropButton.Margin = Padding.Empty;
            _dropButton.Padding = Padding.Empty;
            _dropButton.Click += (_, _) => ToggleDropDownFromClick();
            _dropButton.MouseDown += (_, _) => ToggleDropDownFromMouseDown();
            _dropButton.Paint += DropButton_Paint;

            _displayLabel.Dock = DockStyle.Fill;
            _displayLabel.AutoEllipsis = true;
            _displayLabel.TextAlign = ContentAlignment.MiddleLeft;
            _displayLabel.BackColor = SystemColors.Window;
            _displayLabel.Cursor = Cursors.Hand;
            _displayLabel.Margin = Padding.Empty;
            _displayLabel.Padding = Padding.Empty;
            _displayLabel.Text = PlaceholderText;
            _displayLabel.Click += (_, _) => ToggleDropDownFromClick();
            _displayLabel.MouseDown += (_, _) => ToggleDropDownFromMouseDown();

            Controls.Add(_dropButton);
            Controls.Add(_displayLabel);

            ConfigurePopup();
        }

        public event EventHandler? SelectedIndexChanged;

        public int ItemCount => _items.Count;

        public object? SelectedItem
        {
            get => _selectedItem;
            set
            {
                int index = value == null ? -1 : _items.FindIndex(item => Equals(item, value));
                SelectedIndex = index;
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value < -1 || value >= _items.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                SetSelectedIndex(value, raiseEvent: true);
            }
        }

        public void SetItems(IEnumerable<object> items)
        {
            _items.Clear();
            _items.AddRange(items);

            _listBox.BeginUpdate();
            _listBox.Items.Clear();
            foreach (object item in _items)
            {
                _listBox.Items.Add(item);
            }

            _listBox.EndUpdate();
            SetSelectedIndex(-1, raiseEvent: false);
            UpdatePreview();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _dropButton.Width = LogicalToDeviceUnits(24);
            Padding = new Padding(LogicalToDeviceUnits(5), LogicalToDeviceUnits(2), LogicalToDeviceUnits(1), LogicalToDeviceUnits(1));
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            _displayLabel.Font = Font;
            _dropButton.Font = new Font(Font.FontFamily, Math.Max(8F, Font.Size - 1F), FontStyle.Regular);
            _listBox.Font = Font;
            _detailHeader.Font = new Font(Font, FontStyle.Bold);
            _detailText.Font = Font;
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            _displayLabel.Enabled = Enabled;
            _dropButton.Enabled = Enabled;
            Color backColor = Enabled ? SystemColors.Window : Color.FromArgb(248, 249, 250);
            _displayLabel.BackColor = backColor;
            _dropButton.BackColor = backColor;
            _displayLabel.ForeColor = Enabled ? Color.FromArgb(25, 28, 32) : Color.FromArgb(134, 142, 150);
            _dropButton.ForeColor = Enabled ? Color.FromArgb(68, 74, 80) : Color.FromArgb(150, 156, 162);
            BackColor = backColor;
            if (!Enabled)
            {
                _dropDown.Close();
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Color border = Enabled ? Color.FromArgb(178, 184, 190) : Color.FromArgb(218, 222, 226);
            using var pen = new Pen(border);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        private void DropButton_Paint(object? sender, PaintEventArgs e)
        {
            Color arrowColor = Enabled ? Color.FromArgb(68, 74, 80) : Color.FromArgb(150, 156, 162);
            int centerX = _dropButton.Width / 2;
            int centerY = _dropButton.Height / 2;
            using var brush = new SolidBrush(arrowColor);
            e.Graphics.FillPolygon(
                brush,
                new[]
                {
                    new Point(centerX - 4, centerY - 2),
                    new Point(centerX + 4, centerY - 2),
                    new Point(centerX, centerY + 3)
                });
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

        private void ConfigurePopup()
        {
            _listBox.BorderStyle = BorderStyle.None;
            _listBox.Dock = DockStyle.Fill;
            _listBox.DrawMode = DrawMode.OwnerDrawFixed;
            _listBox.IntegralHeight = false;
            _listBox.ItemHeight = LogicalToDeviceUnits(ListItemHeight);
            _listBox.Margin = Padding.Empty;
            _listBox.HorizontalScrollbar = false;
            _listBox.DrawItem += ListBox_DrawItem;
            _listBox.MouseMove += ListBox_MouseMove;
            _listBox.MouseLeave += (_, _) => ClearHoverPreview();
            _listBox.MouseDown += ListBox_MouseDown;
            _listBox.KeyDown += ListBox_KeyDown;
            _listBox.SelectedIndexChanged += (_, _) => UpdatePreview();

            _detailHeader.Dock = DockStyle.Top;
            _detailHeader.Height = LogicalToDeviceUnits(28);
            _detailHeader.AutoEllipsis = true;
            _detailHeader.TextAlign = ContentAlignment.MiddleLeft;
            _detailHeader.Margin = Padding.Empty;
            _detailHeader.Padding = new Padding(0, 0, 0, 4);

            _detailText.Dock = DockStyle.Fill;
            _detailText.BorderStyle = BorderStyle.None;
            _detailText.Multiline = true;
            _detailText.ReadOnly = true;
            _detailText.ScrollBars = ScrollBars.None;
            _detailText.TabStop = false;
            _detailText.WordWrap = true;
            _detailText.BackColor = Color.FromArgb(246, 248, 250);
            _detailText.Margin = Padding.Empty;

            _detailPanel.Dock = DockStyle.Fill;
            _detailPanel.BackColor = Color.FromArgb(246, 248, 250);
            _detailPanel.Padding = new Padding(LogicalToDeviceUnits(8), LogicalToDeviceUnits(7), LogicalToDeviceUnits(8), LogicalToDeviceUnits(7));
            _detailPanel.Controls.Add(_detailText);
            _detailPanel.Controls.Add(_detailHeader);

            _popupLayout.ColumnCount = 1;
            _popupLayout.RowCount = 2;
            _popupLayout.Margin = Padding.Empty;
            _popupLayout.Padding = Padding.Empty;
            _popupLayout.BackColor = SystemColors.Window;
            _popupLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _popupLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(140)));
            _popupLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _popupLayout.Controls.Add(_listBox, 0, 0);
            _popupLayout.Controls.Add(_detailPanel, 0, 1);

            var host = new ToolStripControlHost(_popupLayout)
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
            };
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

        private void ShowDropDown()
        {
            if (!Enabled || _dropDown.Visible)
            {
                return;
            }

            SizeDropDown(out Point dropLocation);
            _hoverIndex = -1;
            UpdateListSelection();
            UpdatePreview();
            _dropDown.Show(this, dropLocation, ToolStripDropDownDirection.BelowRight);
            _listBox.Focus();
        }

        private void SizeDropDown(out Point dropLocation)
        {
            Rectangle workingArea = Screen.FromControl(this).WorkingArea;
            Point screenLocation = PointToScreen(Point.Empty);

            int maxWidth = Math.Max(LogicalToDeviceUnits(320), workingArea.Width - 16);
            int width = Math.Min(LogicalToDeviceUnits(360), maxWidth);
            width = Math.Max(width, Math.Min(LogicalToDeviceUnits(300), maxWidth));

            int detailHeight = LogicalToDeviceUnits(112);
            int visibleRows = Math.Min(Math.Max(_items.Count, 1), 6);
            int listHeight = (visibleRows * _listBox.ItemHeight) + 2;
            int requestedHeight = listHeight + detailHeight;
            int spaceBelow = workingArea.Bottom - (screenLocation.Y + Height) - 8;
            int spaceAbove = screenLocation.Y - workingArea.Top - 8;
            bool showAbove = spaceBelow < requestedHeight && spaceAbove > spaceBelow;
            int availableHeight = Math.Max(LogicalToDeviceUnits(180), showAbove ? spaceAbove : spaceBelow);
            int height = Math.Min(requestedHeight, availableHeight);

            int actualDetailHeight = Math.Min(detailHeight, Math.Max(LogicalToDeviceUnits(88), height / 3));
            _popupLayout.RowStyles[0].Height = height - actualDetailHeight;
            _popupLayout.Size = new Size(width, height);

            if (_dropDown.Items[0] is ToolStripControlHost host)
            {
                host.Size = _popupLayout.Size;
            }

            int x = Math.Min(0, workingArea.Right - (screenLocation.X + width) - 8);
            int y = showAbove ? -height : Height;
            dropLocation = new Point(x, y);
        }

        private void SetSelectedIndex(int index, bool raiseEvent)
        {
            bool changed = _selectedIndex != index;
            _selectedIndex = index;
            _selectedItem = index >= 0 && index < _items.Count ? _items[index] : null;
            _hoverIndex = -1;
            UpdateDisplayText();
            UpdateListSelection();
            UpdatePreview();

            if (changed && raiseEvent)
            {
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void UpdateDisplayText()
        {
            string text = _selectedItem is ISkipPickerOption option ? option.Label : _selectedItem?.ToString() ?? PlaceholderText;
            Text = text;
            _displayLabel.Text = text;
        }

        private void UpdateListSelection()
        {
            if (_listBox.Items.Count == 0)
            {
                _listBox.SelectedIndex = -1;
                return;
            }

            int index = _selectedIndex >= 0 && _selectedIndex < _listBox.Items.Count ? _selectedIndex : 0;
            if (_listBox.SelectedIndex != index)
            {
                _listBox.SelectedIndex = index;
            }
        }

        private void UpdatePreview()
        {
            object? item = GetPreviewItem();
            if (item is ISkipPickerOption option)
            {
                _detailHeader.Text = option.Label;
                _detailText.Text = option.Detail;
                return;
            }

            string text = item?.ToString() ?? PlaceholderText;
            _detailHeader.Text = text;
            _detailText.Text = text;
        }

        private object? GetPreviewItem()
        {
            if (_hoverIndex >= 0 && _hoverIndex < _items.Count)
            {
                return _items[_hoverIndex];
            }

            if (_listBox.SelectedIndex >= 0 && _listBox.SelectedIndex < _items.Count)
            {
                return _items[_listBox.SelectedIndex];
            }

            return _selectedItem;
        }

        private void ListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index < 0 || e.Index >= _items.Count)
            {
                return;
            }

            object item = _items[e.Index];
            string text = item is ISkipPickerOption option ? option.Label : item.ToString() ?? "";
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool hover = e.Index == _hoverIndex && !selected;
            if (hover)
            {
                using var brush = new SolidBrush(Color.FromArgb(235, 239, 244));
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            Color textColor = selected ? SystemColors.HighlightText : _listBox.ForeColor;
            Rectangle bounds = Rectangle.Inflate(e.Bounds, -6, 0);
            TextRenderer.DrawText(
                e.Graphics,
                text,
                e.Font ?? _listBox.Font,
                bounds,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            e.DrawFocusRectangle();
        }

        private void ListBox_MouseMove(object? sender, MouseEventArgs e)
        {
            int index = _listBox.IndexFromPoint(e.Location);
            if (index < 0 || index >= _listBox.Items.Count)
            {
                index = -1;
            }

            if (_hoverIndex != index)
            {
                _hoverIndex = index;
                _listBox.Invalidate();
                UpdatePreview();
            }
        }

        private void ClearHoverPreview()
        {
            if (_hoverIndex == -1)
            {
                return;
            }

            _hoverIndex = -1;
            _listBox.Invalidate();
            UpdatePreview();
        }

        private void ListBox_MouseDown(object? sender, MouseEventArgs e)
        {
            int index = _listBox.IndexFromPoint(e.Location);
            if (index < 0 || index >= _items.Count)
            {
                return;
            }

            CommitIndex(index);
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
            if (_listBox.SelectedIndex < 0 || _listBox.SelectedIndex >= _items.Count)
            {
                return;
            }

            CommitIndex(_listBox.SelectedIndex);
        }

        private void CommitIndex(int index)
        {
            SelectedIndex = index;
            _dropDown.Close();
        }
    }
}
