#nullable disable

namespace Lazy_App_Codex_Core
{
    partial class RunSetControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            layout = new TableLayoutPanel();
            contentLayout = new TableLayoutPanel();
            selectorLayout = new TableLayoutPanel();
            ddlScript = new SearchableDropdown();
            statusDot = new Panel();
            adbStatusDot = new Panel();
            liveStatusLayout = new TableLayoutPanel();
            lblCurrentActionName = new Label();
            lblCurrentActionColon = new Label();
            lblStepName = new Label();
            lblStepColon = new Label();
            lblCycleName = new Label();
            lblCycleColon = new Label();
            lblNextActionName = new Label();
            lblNextActionColon = new Label();
            lblNextAtName = new Label();
            lblNextAtColon = new Label();
            lblEstimatedEndName = new Label();
            lblEstimatedEndColon = new Label();
            lblCurrentActionValue = new Label();
            lblStepValue = new Label();
            lblCycleValue = new Label();
            lblNextActionValue = new Label();
            lblNextAtValue = new Label();
            lblEstimatedEndValue = new Label();
            actionPanel = new TableLayoutPanel();
            btnRun = new Button();
            ddlOffset = new ComboBox();
            ddlTagFilter = new ComboBox();
            ddlDevice = new ComboBox();
            btnConfig = new Button();
            btnWirelessAdb = new Button();
            layout.SuspendLayout();
            contentLayout.SuspendLayout();
            selectorLayout.SuspendLayout();
            liveStatusLayout.SuspendLayout();
            actionPanel.SuspendLayout();
            SuspendLayout();
            // 
            // layout
            // 
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 322F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.Controls.Add(contentLayout, 0, 0);
            layout.Controls.Add(actionPanel, 1, 0);
            layout.Dock = DockStyle.Fill;
            layout.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
            layout.Location = new Point(0, 0);
            layout.Margin = new Padding(0);
            layout.Name = "layout";
            layout.RowCount = 1;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.Size = new Size(472, 216);
            layout.TabIndex = 0;
            // 
            // contentLayout
            // 
            contentLayout.ColumnCount = 1;
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contentLayout.Controls.Add(selectorLayout, 0, 0);
            contentLayout.Controls.Add(liveStatusLayout, 0, 1);
            contentLayout.Dock = DockStyle.Fill;
            contentLayout.Location = new Point(0, 0);
            contentLayout.Margin = new Padding(0, 0, 12, 0);
            contentLayout.Name = "contentLayout";
            contentLayout.RowCount = 3;
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 168F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            contentLayout.Size = new Size(310, 216);
            contentLayout.TabIndex = 0;
            // 
            // selectorLayout
            // 
            selectorLayout.ColumnCount = 3;
            selectorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            selectorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18F));
            selectorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18F));
            selectorLayout.Controls.Add(ddlScript, 0, 0);
            selectorLayout.Controls.Add(statusDot, 1, 0);
            selectorLayout.Controls.Add(adbStatusDot, 2, 0);
            selectorLayout.Dock = DockStyle.Fill;
            selectorLayout.Location = new Point(0, 0);
            selectorLayout.Margin = new Padding(0);
            selectorLayout.Name = "selectorLayout";
            selectorLayout.RowCount = 1;
            selectorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            selectorLayout.Size = new Size(310, 34);
            selectorLayout.TabIndex = 0;
            // 
            // ddlScript
            // 
            ddlScript.BackColor = SystemColors.Window;
            ddlScript.Dock = DockStyle.Fill;
            ddlScript.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ddlScript.Location = new Point(0, 2);
            ddlScript.Margin = new Padding(0, 2, 8, 2);
            ddlScript.MinimumSize = new Size(160, 30);
            ddlScript.Name = "ddlScript";
            ddlScript.Padding = new Padding(5, 5, 1, 1);
            ddlScript.PlaceholderText = "Choose script, sequence, or plan";
            ddlScript.SelectedItem = null;
            ddlScript.Size = new Size(266, 30);
            ddlScript.TabIndex = 0;
            // 
            // statusDot
            // 
            statusDot.Anchor = AnchorStyles.None;
            statusDot.BackColor = Color.Transparent;
            statusDot.Location = new Point(277, 11);
            statusDot.Margin = new Padding(0);
            statusDot.Name = "statusDot";
            statusDot.Size = new Size(12, 12);
            statusDot.TabIndex = 1;
            // 
            // adbStatusDot
            // 
            adbStatusDot.Anchor = AnchorStyles.None;
            adbStatusDot.BackColor = Color.Transparent;
            adbStatusDot.Location = new Point(295, 11);
            adbStatusDot.Margin = new Padding(0);
            adbStatusDot.Name = "adbStatusDot";
            adbStatusDot.Size = new Size(12, 12);
            adbStatusDot.TabIndex = 2;
            // 
            // liveStatusLayout
            // 
            liveStatusLayout.ColumnCount = 3;
            liveStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126F));
            liveStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 14F));
            liveStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            liveStatusLayout.Controls.Add(lblCurrentActionName, 0, 0);
            liveStatusLayout.Controls.Add(lblCurrentActionColon, 1, 0);
            liveStatusLayout.Controls.Add(lblCurrentActionValue, 2, 0);
            liveStatusLayout.Controls.Add(lblStepName, 0, 1);
            liveStatusLayout.Controls.Add(lblStepColon, 1, 1);
            liveStatusLayout.Controls.Add(lblStepValue, 2, 1);
            liveStatusLayout.Controls.Add(lblCycleName, 0, 2);
            liveStatusLayout.Controls.Add(lblCycleColon, 1, 2);
            liveStatusLayout.Controls.Add(lblCycleValue, 2, 2);
            liveStatusLayout.Controls.Add(lblNextActionName, 0, 3);
            liveStatusLayout.Controls.Add(lblNextActionColon, 1, 3);
            liveStatusLayout.Controls.Add(lblNextActionValue, 2, 3);
            liveStatusLayout.Controls.Add(lblNextAtName, 0, 4);
            liveStatusLayout.Controls.Add(lblNextAtColon, 1, 4);
            liveStatusLayout.Controls.Add(lblNextAtValue, 2, 4);
            liveStatusLayout.Controls.Add(lblEstimatedEndName, 0, 5);
            liveStatusLayout.Controls.Add(lblEstimatedEndColon, 1, 5);
            liveStatusLayout.Controls.Add(lblEstimatedEndValue, 2, 5);
            liveStatusLayout.Dock = DockStyle.Fill;
            liveStatusLayout.Location = new Point(0, 42);
            liveStatusLayout.Margin = new Padding(0, 8, 0, 0);
            liveStatusLayout.Name = "liveStatusLayout";
            liveStatusLayout.RowCount = 6;
            liveStatusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            liveStatusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            liveStatusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            liveStatusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            liveStatusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            liveStatusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            liveStatusLayout.Size = new Size(310, 160);
            liveStatusLayout.TabIndex = 1;
            // 
            // lblCurrentActionName
            // 
            lblCurrentActionName.Dock = DockStyle.Fill;
            lblCurrentActionName.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblCurrentActionName.Margin = new Padding(0);
            lblCurrentActionName.Name = "lblCurrentActionName";
            lblCurrentActionName.Size = new Size(126, 26);
            lblCurrentActionName.TabIndex = 0;
            lblCurrentActionName.Text = "Current Action";
            lblCurrentActionName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblCurrentActionColon
            // 
            lblCurrentActionColon.Dock = DockStyle.Fill;
            lblCurrentActionColon.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblCurrentActionColon.Margin = new Padding(0);
            lblCurrentActionColon.Name = "lblCurrentActionColon";
            lblCurrentActionColon.Size = new Size(14, 26);
            lblCurrentActionColon.TabIndex = 1;
            lblCurrentActionColon.Text = ":";
            lblCurrentActionColon.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblCurrentActionValue
            // 
            lblCurrentActionValue.Dock = DockStyle.Fill;
            lblCurrentActionValue.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblCurrentActionValue.Margin = new Padding(0);
            lblCurrentActionValue.Name = "lblCurrentActionValue";
            lblCurrentActionValue.Size = new Size(170, 26);
            lblCurrentActionValue.TabIndex = 2;
            lblCurrentActionValue.Text = "--";
            lblCurrentActionValue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblStepName
            // 
            lblStepName.Dock = DockStyle.Fill;
            lblStepName.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblStepName.Margin = new Padding(0);
            lblStepName.Name = "lblStepName";
            lblStepName.Size = new Size(126, 26);
            lblStepName.TabIndex = 3;
            lblStepName.Text = "Current Step";
            lblStepName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblStepColon
            // 
            lblStepColon.Dock = DockStyle.Fill;
            lblStepColon.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblStepColon.Margin = new Padding(0);
            lblStepColon.Name = "lblStepColon";
            lblStepColon.Size = new Size(14, 26);
            lblStepColon.TabIndex = 4;
            lblStepColon.Text = ":";
            lblStepColon.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblStepValue
            // 
            lblStepValue.Dock = DockStyle.Fill;
            lblStepValue.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblStepValue.Margin = new Padding(0);
            lblStepValue.Name = "lblStepValue";
            lblStepValue.Size = new Size(170, 26);
            lblStepValue.TabIndex = 5;
            lblStepValue.Text = "--";
            lblStepValue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblCycleName
            // 
            lblCycleName.Dock = DockStyle.Fill;
            lblCycleName.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblCycleName.Margin = new Padding(0);
            lblCycleName.Name = "lblCycleName";
            lblCycleName.Size = new Size(126, 26);
            lblCycleName.TabIndex = 6;
            lblCycleName.Text = "Current Cycle";
            lblCycleName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblCycleColon
            // 
            lblCycleColon.Dock = DockStyle.Fill;
            lblCycleColon.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblCycleColon.Margin = new Padding(0);
            lblCycleColon.Name = "lblCycleColon";
            lblCycleColon.Size = new Size(14, 26);
            lblCycleColon.TabIndex = 7;
            lblCycleColon.Text = ":";
            lblCycleColon.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblCycleValue
            // 
            lblCycleValue.Dock = DockStyle.Fill;
            lblCycleValue.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblCycleValue.Margin = new Padding(0);
            lblCycleValue.Name = "lblCycleValue";
            lblCycleValue.Size = new Size(170, 26);
            lblCycleValue.TabIndex = 8;
            lblCycleValue.Text = "--";
            lblCycleValue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblNextActionName
            // 
            lblNextActionName.Dock = DockStyle.Fill;
            lblNextActionName.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblNextActionName.Margin = new Padding(0);
            lblNextActionName.Name = "lblNextActionName";
            lblNextActionName.Size = new Size(126, 26);
            lblNextActionName.TabIndex = 9;
            lblNextActionName.Text = "Next Action";
            lblNextActionName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblNextActionColon
            // 
            lblNextActionColon.Dock = DockStyle.Fill;
            lblNextActionColon.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblNextActionColon.Margin = new Padding(0);
            lblNextActionColon.Name = "lblNextActionColon";
            lblNextActionColon.Size = new Size(14, 26);
            lblNextActionColon.TabIndex = 10;
            lblNextActionColon.Text = ":";
            lblNextActionColon.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblNextActionValue
            // 
            lblNextActionValue.Dock = DockStyle.Fill;
            lblNextActionValue.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblNextActionValue.Margin = new Padding(0);
            lblNextActionValue.Name = "lblNextActionValue";
            lblNextActionValue.Size = new Size(170, 26);
            lblNextActionValue.TabIndex = 11;
            lblNextActionValue.Text = "--";
            lblNextActionValue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblNextAtName
            // 
            lblNextAtName.Dock = DockStyle.Fill;
            lblNextAtName.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblNextAtName.Margin = new Padding(0);
            lblNextAtName.Name = "lblNextAtName";
            lblNextAtName.Size = new Size(126, 26);
            lblNextAtName.TabIndex = 12;
            lblNextAtName.Text = "Next Action At";
            lblNextAtName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblNextAtColon
            // 
            lblNextAtColon.Dock = DockStyle.Fill;
            lblNextAtColon.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblNextAtColon.Margin = new Padding(0);
            lblNextAtColon.Name = "lblNextAtColon";
            lblNextAtColon.Size = new Size(14, 26);
            lblNextAtColon.TabIndex = 13;
            lblNextAtColon.Text = ":";
            lblNextAtColon.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblNextAtValue
            // 
            lblNextAtValue.Dock = DockStyle.Fill;
            lblNextAtValue.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblNextAtValue.Margin = new Padding(0);
            lblNextAtValue.Name = "lblNextAtValue";
            lblNextAtValue.Size = new Size(170, 26);
            lblNextAtValue.TabIndex = 14;
            lblNextAtValue.Text = "--";
            lblNextAtValue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblEstimatedEndName
            // 
            lblEstimatedEndName.Dock = DockStyle.Fill;
            lblEstimatedEndName.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblEstimatedEndName.Margin = new Padding(0);
            lblEstimatedEndName.Name = "lblEstimatedEndName";
            lblEstimatedEndName.Size = new Size(126, 30);
            lblEstimatedEndName.TabIndex = 15;
            lblEstimatedEndName.Text = "Estimated End";
            lblEstimatedEndName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblEstimatedEndColon
            // 
            lblEstimatedEndColon.Dock = DockStyle.Fill;
            lblEstimatedEndColon.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblEstimatedEndColon.Margin = new Padding(0);
            lblEstimatedEndColon.Name = "lblEstimatedEndColon";
            lblEstimatedEndColon.Size = new Size(14, 30);
            lblEstimatedEndColon.TabIndex = 16;
            lblEstimatedEndColon.Text = ":";
            lblEstimatedEndColon.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblEstimatedEndValue
            // 
            lblEstimatedEndValue.Dock = DockStyle.Fill;
            lblEstimatedEndValue.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblEstimatedEndValue.Margin = new Padding(0);
            lblEstimatedEndValue.Name = "lblEstimatedEndValue";
            lblEstimatedEndValue.Size = new Size(170, 30);
            lblEstimatedEndValue.TabIndex = 17;
            lblEstimatedEndValue.Text = "--";
            lblEstimatedEndValue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // actionPanel
            // 
            actionPanel.ColumnCount = 1;
            actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            actionPanel.Controls.Add(btnRun, 0, 0);
            actionPanel.Controls.Add(ddlOffset, 0, 1);
            actionPanel.Controls.Add(ddlTagFilter, 0, 2);
            actionPanel.Controls.Add(ddlDevice, 0, 3);
            actionPanel.Controls.Add(btnConfig, 0, 4);
            actionPanel.Controls.Add(btnWirelessAdb, 0, 5);
            actionPanel.Dock = DockStyle.Top;
            actionPanel.Location = new Point(322, 0);
            actionPanel.Margin = new Padding(0);
            actionPanel.Name = "actionPanel";
            actionPanel.RowCount = 6;
            actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            actionPanel.Size = new Size(150, 216);
            actionPanel.TabIndex = 1;
            // 
            // btnRun
            // 
            btnRun.Dock = DockStyle.Fill;
            btnRun.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnRun.Location = new Point(0, 2);
            btnRun.Margin = new Padding(0, 2, 0, 2);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(150, 32);
            btnRun.TabIndex = 0;
            btnRun.Text = "Run";
            btnRun.UseVisualStyleBackColor = true;
            // 
            // ddlOffset
            // 
            ddlOffset.Dock = DockStyle.Fill;
            ddlOffset.DropDownStyle = ComboBoxStyle.DropDownList;
            ddlOffset.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ddlOffset.FormattingEnabled = true;
            ddlOffset.ItemHeight = 20;
            ddlOffset.Location = new Point(0, 40);
            ddlOffset.Margin = new Padding(0, 4, 0, 4);
            ddlOffset.Name = "ddlOffset";
            ddlOffset.Size = new Size(150, 28);
            ddlOffset.TabIndex = 1;
            // 
            // ddlTagFilter
            // 
            ddlTagFilter.Dock = DockStyle.Fill;
            ddlTagFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            ddlTagFilter.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ddlTagFilter.FormattingEnabled = true;
            ddlTagFilter.ItemHeight = 20;
            ddlTagFilter.Location = new Point(0, 76);
            ddlTagFilter.Margin = new Padding(0, 4, 0, 4);
            ddlTagFilter.Name = "ddlTagFilter";
            ddlTagFilter.Size = new Size(150, 28);
            ddlTagFilter.TabIndex = 2;
            // 
            // ddlDevice
            // 
            ddlDevice.Dock = DockStyle.Fill;
            ddlDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            ddlDevice.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ddlDevice.FormattingEnabled = true;
            ddlDevice.ItemHeight = 18;
            ddlDevice.Location = new Point(0, 113);
            ddlDevice.Margin = new Padding(0, 5, 0, 5);
            ddlDevice.Name = "ddlDevice";
            ddlDevice.Size = new Size(150, 26);
            ddlDevice.TabIndex = 3;
            // 
            // btnConfig
            // 
            btnConfig.Dock = DockStyle.Fill;
            btnConfig.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnConfig.Location = new Point(0, 146);
            btnConfig.Margin = new Padding(0, 2, 0, 2);
            btnConfig.Name = "btnConfig";
            btnConfig.Size = new Size(150, 32);
            btnConfig.TabIndex = 4;
            btnConfig.Text = "Config";
            btnConfig.UseVisualStyleBackColor = true;
            // 
            // btnWirelessAdb
            // 
            btnWirelessAdb.Dock = DockStyle.Fill;
            btnWirelessAdb.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnWirelessAdb.Location = new Point(0, 182);
            btnWirelessAdb.Margin = new Padding(0, 2, 0, 2);
            btnWirelessAdb.Name = "btnWirelessAdb";
            btnWirelessAdb.Size = new Size(150, 32);
            btnWirelessAdb.TabIndex = 5;
            btnWirelessAdb.Text = "Pair / Connect";
            btnWirelessAdb.UseVisualStyleBackColor = true;
            // RunSetControl
            // 
            AutoScaleMode = AutoScaleMode.None;
            Controls.Add(layout);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Margin = new Padding(0);
            Name = "RunSetControl";
            Size = new Size(472, 216);
            layout.ResumeLayout(false);
            contentLayout.ResumeLayout(false);
            selectorLayout.ResumeLayout(false);
            liveStatusLayout.ResumeLayout(false);
            actionPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private TableLayoutPanel contentLayout;
        private TableLayoutPanel selectorLayout;
        private SearchableDropdown ddlScript;
        private Panel statusDot;
        private Panel adbStatusDot;
        private TableLayoutPanel liveStatusLayout;
        private Label lblCurrentActionName;
        private Label lblCurrentActionColon;
        private Label lblStepName;
        private Label lblStepColon;
        private Label lblCycleName;
        private Label lblCycleColon;
        private Label lblNextActionName;
        private Label lblNextActionColon;
        private Label lblNextAtName;
        private Label lblNextAtColon;
        private Label lblEstimatedEndName;
        private Label lblEstimatedEndColon;
        private Label lblCurrentActionValue;
        private Label lblStepValue;
        private Label lblCycleValue;
        private Label lblNextActionValue;
        private Label lblNextAtValue;
        private Label lblEstimatedEndValue;
        private TableLayoutPanel actionPanel;
        private Button btnRun;
        private ComboBox ddlOffset;
        private ComboBox ddlTagFilter;
        private ComboBox ddlDevice;
        private Button btnConfig;
        private Button btnWirelessAdb;
    }
}
