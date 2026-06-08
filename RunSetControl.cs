namespace Lazy_App_Codex_Core
{
    internal sealed partial class RunSetControl : UserControl
    {
        internal const int FixedWidth = 594;
        internal const int FixedHeight = 284;
        internal const int ContentColumnWidth = 410;
        internal const int ActionColumnWidth = 184;

        private const int ActionRowHeight = 34;
        private const int TimelineChipCount = 6;

        private readonly FlowLayoutPanel _timelinePanel = new();
        private readonly CountdownProgressControl _countdownBar = new();
        private readonly Label[] _timelineLabels = CreateTimelineLabels();

        public RunSetControl(bool showStatusDots, bool showSharedButtons)
        {
            InitializeComponent();
            ShowStatusDots = showStatusDots;
            ShowSharedButtons = showSharedButtons;
            ApplyStableLayout();
            ApplyMode();
        }

        public bool ShowStatusDots { get; }
        public bool ShowSharedButtons { get; }
        public SearchableDropdown ScriptBox => ddlScript;
        public SkipPickerControl SkipBox => ddlSkip;
        public ComboBox OffsetBox => ddlOffset;
        public ComboBox TagFilter => ddlTagFilter;
        public ComboBox DeviceBox => ddlDevice;
        public Button RunButton => btnRun;
        public Button? ConfigButton => ShowSharedButtons ? btnConfig : null;
        public Button? WirelessAdbButton => ShowSharedButtons ? btnWirelessAdb : null;
        public Panel? StatusDot => ShowStatusDots ? statusDot : null;
        public Panel? AdbStatusDot => ShowStatusDots ? adbStatusDot : null;
        public TableLayoutPanel LiveStatusLayout => liveStatusLayout;
        public Label CurrentActionLabel => lblCurrentActionValue;
        public Label StepLabel => lblStepValue;
        public Label CycleLabel => lblCycleValue;
        public Label NextActionLabel => lblNextActionValue;
        public Label NextAtLabel => lblNextAtValue;
        public Label EstimatedEndLabel => lblEstimatedEndValue;
        public CountdownProgressControl CountdownBar => _countdownBar;
        public IReadOnlyList<Label> TimelineLabels => _timelineLabels;

        private void ApplyStableLayout()
        {
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(FixedWidth, FixedHeight);
            MinimumSize = Size;
            MaximumSize = Size;

            layout.Size = Size;
            layout.ColumnStyles[0].SizeType = SizeType.Absolute;
            layout.ColumnStyles[0].Width = ContentColumnWidth;
            layout.ColumnStyles[1].SizeType = SizeType.Absolute;
            layout.ColumnStyles[1].Width = ActionColumnWidth;

            contentLayout.Size = new Size(ContentColumnWidth - 12, FixedHeight);
            selectorLayout.Size = new Size(ContentColumnWidth - 12, 34);
            ConfigureContentLayout();
            ConfigureLiveStatusLayout();
            ConfigureTimeline();
            ConfigureCountdownBar();

            actionPanel.Location = new Point(ContentColumnWidth, 0);
            actionPanel.Size = new Size(ActionColumnWidth, FixedHeight);
            actionPanel.RowCount = 8;
            actionPanel.RowStyles.Clear();
            for (int row = 0; row < 7; row++)
            {
                actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, ActionRowHeight));
            }

            actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, FixedHeight - (ActionRowHeight * 7)));

            ConfigureButton(btnRun, ActionRowHeight * 0);
            ConfigureSkipPicker(ddlSkip, ActionRowHeight * 1);
            ConfigureCombo(ddlOffset, ActionRowHeight * 2, 20, 28, new Padding(0, 3, 0, 3));
            ConfigureCombo(ddlTagFilter, ActionRowHeight * 3, 20, 28, new Padding(0, 3, 0, 3));
            ConfigureCombo(ddlDevice, ActionRowHeight * 4, 18, 26, new Padding(0, 4, 0, 4));
            ConfigureButton(btnConfig, ActionRowHeight * 5);
            ConfigureButton(btnWirelessAdb, ActionRowHeight * 6);

            ddlOffset.Items.Clear();
            foreach (var option in OffsetDisplayOption.All)
            {
                ddlOffset.Items.Add(option);
            }
        }

        private void ConfigureContentLayout()
        {
            contentLayout.SuspendLayout();
            contentLayout.Controls.Clear();
            contentLayout.RowStyles.Clear();
            contentLayout.RowCount = 4;
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            contentLayout.Controls.Add(selectorLayout, 0, 0);
            contentLayout.Controls.Add(liveStatusLayout, 0, 1);
            contentLayout.Controls.Add(_timelinePanel, 0, 2);
            contentLayout.Controls.Add(_countdownBar, 0, 3);
            contentLayout.ResumeLayout(false);
        }

        private void ConfigureLiveStatusLayout()
        {
            liveStatusLayout.SuspendLayout();
            liveStatusLayout.Controls.Clear();
            liveStatusLayout.ColumnStyles.Clear();
            liveStatusLayout.RowStyles.Clear();
            liveStatusLayout.ColumnCount = 4;
            liveStatusLayout.RowCount = 3;
            liveStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62F));
            liveStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            liveStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58F));
            liveStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int row = 0; row < 3; row++)
            {
                liveStatusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));
            }

            ConfigureStatusName(lblCurrentActionName, "Now");
            ConfigureStatusName(lblStepName, "Step");
            ConfigureStatusName(lblCycleName, "Cycle");
            ConfigureStatusName(lblNextActionName, "Next");
            ConfigureStatusName(lblNextAtName, "Next At");
            ConfigureStatusName(lblEstimatedEndName, "End");
            foreach (Label valueLabel in new[] { lblCurrentActionValue, lblStepValue, lblCycleValue, lblNextActionValue, lblNextAtValue, lblEstimatedEndValue })
            {
                ConfigureStatusValue(valueLabel);
            }

            foreach (Label colon in new[] { lblCurrentActionColon, lblStepColon, lblCycleColon, lblNextActionColon, lblNextAtColon, lblEstimatedEndColon })
            {
                colon.Visible = false;
            }

            liveStatusLayout.Controls.Add(lblCurrentActionName, 0, 0);
            liveStatusLayout.Controls.Add(lblCurrentActionValue, 1, 0);
            liveStatusLayout.Controls.Add(lblCycleName, 2, 0);
            liveStatusLayout.Controls.Add(lblCycleValue, 3, 0);
            liveStatusLayout.Controls.Add(lblStepName, 0, 1);
            liveStatusLayout.Controls.Add(lblStepValue, 1, 1);
            liveStatusLayout.Controls.Add(lblNextActionName, 2, 1);
            liveStatusLayout.Controls.Add(lblNextActionValue, 3, 1);
            liveStatusLayout.Controls.Add(lblNextAtName, 0, 2);
            liveStatusLayout.Controls.Add(lblNextAtValue, 1, 2);
            liveStatusLayout.Controls.Add(lblEstimatedEndName, 2, 2);
            liveStatusLayout.Controls.Add(lblEstimatedEndValue, 3, 2);
            liveStatusLayout.Dock = DockStyle.Fill;
            liveStatusLayout.Margin = new Padding(0, 8, 0, 0);
            liveStatusLayout.ResumeLayout(false);
        }

        private void ConfigureTimeline()
        {
            _timelinePanel.Dock = DockStyle.Fill;
            _timelinePanel.Margin = new Padding(0, 8, 0, 4);
            _timelinePanel.Padding = new Padding(0, 3, 0, 0);
            _timelinePanel.WrapContents = true;
            _timelinePanel.AutoScroll = false;
            _timelinePanel.BackColor = SystemColors.Control;
            _timelinePanel.Controls.Clear();
            foreach (Label label in _timelineLabels)
            {
                _timelinePanel.Controls.Add(label);
            }
        }

        private void ConfigureCountdownBar()
        {
            _countdownBar.Dock = DockStyle.Fill;
            _countdownBar.Margin = new Padding(0, 4, 0, 8);
            _countdownBar.Font = Font;
            _countdownBar.SetState(0D, "Waiting --", false);
        }

        private static void ConfigureButton(Button button, int rowTop)
        {
            button.Dock = DockStyle.Fill;
            button.Location = new Point(0, rowTop + 1);
            button.Margin = new Padding(0, 1, 0, 1);
            button.Size = new Size(ActionColumnWidth, 32);
        }

        private static void ConfigureSkipPicker(SkipPickerControl picker, int rowTop)
        {
            picker.Dock = DockStyle.Fill;
            picker.Location = new Point(0, rowTop + 4);
            picker.Margin = new Padding(0, 4, 0, 6);
            picker.Size = new Size(ActionColumnWidth, 24);
        }

        private static void ConfigureCombo(ComboBox combo, int rowTop, int itemHeight, int height, Padding margin, int? dropDownWidth = null)
        {
            combo.Dock = DockStyle.Fill;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.FlatStyle = FlatStyle.System;
            combo.BackColor = Color.White;
            combo.ForeColor = Color.FromArgb(25, 28, 32);
            combo.ItemHeight = itemHeight;
            combo.Location = new Point(0, rowTop + margin.Top);
            combo.Margin = margin;
            combo.Size = new Size(ActionColumnWidth, height);
            if (dropDownWidth.HasValue)
            {
                combo.DropDownWidth = dropDownWidth.Value;
            }
        }

        private static void ConfigureStatusName(Label label, string text)
        {
            label.Dock = DockStyle.Fill;
            label.AutoEllipsis = true;
            label.Margin = Padding.Empty;
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = Color.FromArgb(82, 88, 96);
        }

        private static void ConfigureStatusValue(Label label)
        {
            label.Dock = DockStyle.Fill;
            label.AutoEllipsis = true;
            label.Margin = Padding.Empty;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = Color.FromArgb(25, 28, 32);
        }

        private static Label[] CreateTimelineLabels()
        {
            var labels = new Label[TimelineChipCount];
            for (int index = 0; index < labels.Length; index++)
            {
                labels[index] = new Label
                {
                    AutoEllipsis = true,
                    BackColor = Color.FromArgb(234, 237, 241),
                    ForeColor = Color.FromArgb(52, 58, 64),
                    Margin = new Padding(0, 0, 5, 5),
                    Padding = new Padding(4, 0, 4, 0),
                    Size = new Size(60, 24),
                    Text = "--",
                    TextAlign = ContentAlignment.MiddleCenter
                };
            }

            return labels;
        }

        private void ApplyMode()
        {
            statusDot.Visible = ShowStatusDots;
            adbStatusDot.Visible = ShowStatusDots;

            btnConfig.Visible = ShowSharedButtons;
            btnWirelessAdb.Visible = ShowSharedButtons;
            btnConfig.TabStop = ShowSharedButtons;
            btnWirelessAdb.TabStop = ShowSharedButtons;
        }
    }
}
