namespace Lazy_App_Codex_Core
{
    internal sealed partial class RunSetControl : UserControl
    {
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

        private void ApplyStableLayout()
        {
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(472, 216);
            MinimumSize = Size;
            MaximumSize = Size;

            layout.Size = Size;
            layout.ColumnStyles[0].SizeType = SizeType.Absolute;
            layout.ColumnStyles[0].Width = 322F;
            layout.ColumnStyles[1].SizeType = SizeType.Absolute;
            layout.ColumnStyles[1].Width = 150F;

            contentLayout.Size = new Size(310, 216);
            selectorLayout.Size = new Size(310, 34);
            liveStatusLayout.Size = new Size(310, 160);

            actionPanel.Location = new Point(322, 0);
            actionPanel.Size = new Size(150, 216);
            actionPanel.RowStyles.Clear();
            for (int row = 0; row < 6; row++)
            {
                actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            }

            ConfigureButton(btnRun, 0);
            ConfigureCombo(ddlOffset, 36, 20, 28, new Padding(0, 4, 0, 4));
            ConfigureCombo(ddlTagFilter, 72, 20, 28, new Padding(0, 4, 0, 4));
            ConfigureCombo(ddlDevice, 108, 18, 26, new Padding(0, 5, 0, 5));
            ConfigureButton(btnConfig, 144);
            ConfigureButton(btnWirelessAdb, 180);

            ddlOffset.Items.Clear();
            foreach (var option in OffsetDisplayOption.All)
            {
                ddlOffset.Items.Add(option);
            }
        }

        private static void ConfigureButton(Button button, int rowTop)
        {
            button.Dock = DockStyle.Fill;
            button.Location = new Point(0, rowTop + 2);
            button.Margin = new Padding(0, 2, 0, 2);
            button.Size = new Size(150, 32);
        }

        private static void ConfigureCombo(ComboBox combo, int rowTop, int itemHeight, int height, Padding margin)
        {
            combo.Dock = DockStyle.Fill;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.ItemHeight = itemHeight;
            combo.Location = new Point(0, rowTop + margin.Top);
            combo.Margin = margin;
            combo.Size = new Size(150, height);
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
