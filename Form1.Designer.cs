namespace Lazy_App_Codex_Core
{
    partial class Form1
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
            mainLayout = new TableLayoutPanel();
            contentLayout = new TableLayoutPanel();
            selectorLayout = new TableLayoutPanel();
            ddlScript = new SearchableDropdown();
            statusDot = new Panel();
            adbStatusDot = new Panel();
            liveStatusLayout = new TableLayoutPanel();
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
            mainLayout.SuspendLayout();
            contentLayout.SuspendLayout();
            selectorLayout.SuspendLayout();
            actionPanel.SuspendLayout();
            SuspendLayout();
            // 
            // mainLayout
            // 
            mainLayout.ColumnCount = 2;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            mainLayout.Controls.Add(contentLayout, 0, 0);
            mainLayout.Controls.Add(actionPanel, 1, 0);
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Location = new Point(0, 0);
            mainLayout.Margin = new Padding(0);
            mainLayout.Name = "mainLayout";
            mainLayout.Padding = new Padding(10);
            mainLayout.RowCount = 1;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.Size = new Size(640, 340);
            mainLayout.TabIndex = 0;
            // 
            // contentLayout
            // 
            contentLayout.ColumnCount = 1;
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contentLayout.Controls.Add(selectorLayout, 0, 0);
            contentLayout.Controls.Add(liveStatusLayout, 0, 1);
            contentLayout.Dock = DockStyle.Fill;
            contentLayout.Location = new Point(16, 16);
            contentLayout.Margin = new Padding(0, 0, 12, 0);
            contentLayout.Name = "contentLayout";
            contentLayout.RowCount = 3;
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 168F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            contentLayout.Size = new Size(458, 320);
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
            selectorLayout.Size = new Size(458, 38);
            selectorLayout.TabIndex = 0;
            // 
            // ddlScript
            // 
            ddlScript.Dock = DockStyle.Fill;
            ddlScript.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ddlScript.Location = new Point(0, 2);
            ddlScript.Margin = new Padding(0, 2, 8, 2);
            ddlScript.MinimumSize = new Size(160, 30);
            ddlScript.Name = "ddlScript";
            ddlScript.PlaceholderText = "Choose script, sequence, or plan";
            ddlScript.Size = new Size(432, 30);
            ddlScript.TabIndex = 0;
            // 
            // statusDot
            // 
            statusDot.Anchor = AnchorStyles.None;
            statusDot.BackColor = Color.Transparent;
            statusDot.Location = new Point(443, 13);
            statusDot.Margin = new Padding(0);
            statusDot.Name = "statusDot";
            statusDot.Size = new Size(12, 12);
            statusDot.TabIndex = 1;
            statusDot.Paint += statusDot_Paint;
            // 
            // adbStatusDot
            // 
            adbStatusDot.Anchor = AnchorStyles.None;
            adbStatusDot.BackColor = Color.Transparent;
            adbStatusDot.Location = new Point(443, 13);
            adbStatusDot.Margin = new Padding(0);
            adbStatusDot.Name = "adbStatusDot";
            adbStatusDot.Size = new Size(12, 12);
            adbStatusDot.TabIndex = 2;
            adbStatusDot.Paint += statusDot_Paint;
            // 
            // liveStatusLayout
            // 
            liveStatusLayout.ColumnCount = 3;
            liveStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126F));
            liveStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 14F));
            liveStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            liveStatusLayout.Dock = DockStyle.Fill;
            liveStatusLayout.Location = new Point(0, 46);
            liveStatusLayout.Margin = new Padding(0, 8, 0, 0);
            liveStatusLayout.Name = "liveStatusLayout";
            liveStatusLayout.RowCount = 6;
            for (int i = 0; i < 6; i++)
            {
                liveStatusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            }
            liveStatusLayout.Size = new Size(458, 156);
            liveStatusLayout.TabIndex = 2;
            AddStatusRow(liveStatusLayout, 0, "Current Action", lblCurrentActionValue);
            AddStatusRow(liveStatusLayout, 1, "Current Step", lblStepValue);
            AddStatusRow(liveStatusLayout, 2, "Current Cycle", lblCycleValue);
            AddStatusRow(liveStatusLayout, 3, "Next Action", lblNextActionValue);
            AddStatusRow(liveStatusLayout, 4, "Next Action At", lblNextAtValue);
            AddStatusRow(liveStatusLayout, 5, "Estimated End", lblEstimatedEndValue);
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
            actionPanel.Location = new Point(480, 10);
            actionPanel.Margin = new Padding(0);
            actionPanel.Name = "actionPanel";
            actionPanel.RowCount = 7;
            actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            actionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            actionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            actionPanel.Size = new Size(150, 252);
            actionPanel.TabIndex = 1;
            // 
            // btnRun
            // 
            btnRun.Dock = DockStyle.Fill;
            btnRun.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnRun.Location = new Point(0, 0);
            btnRun.Margin = new Padding(0, 2, 0, 6);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(150, 32);
            btnRun.TabIndex = 0;
            btnRun.Text = "Run";
            btnRun.UseVisualStyleBackColor = true;
            btnRun.Click += btnRun_Click;
            // 
            // ddlOffset
            // 
            ddlOffset.Dock = DockStyle.Fill;
            ddlOffset.DropDownStyle = ComboBoxStyle.DropDownList;
            ddlOffset.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ddlOffset.FormattingEnabled = true;
            ddlOffset.Items.AddRange(OffsetDisplayOption.All.Cast<object>().ToArray());
            ddlOffset.Location = new Point(0, 40);
            ddlOffset.Margin = new Padding(0, 4, 0, 6);
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
            ddlTagFilter.Location = new Point(0, 80);
            ddlTagFilter.Margin = new Padding(0, 4, 0, 6);
            ddlTagFilter.Name = "ddlTagFilter";
            ddlTagFilter.Size = new Size(150, 28);
            ddlTagFilter.TabIndex = 2;
            ddlTagFilter.SelectedIndexChanged += ddlTagFilter_SelectedIndexChanged;
            // 
            // ddlDevice
            // 
            ddlDevice.Dock = DockStyle.Fill;
            ddlDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            ddlDevice.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ddlDevice.FormattingEnabled = true;
            ddlDevice.Location = new Point(0, 120);
            ddlDevice.Margin = new Padding(0, 4, 0, 6);
            ddlDevice.Name = "ddlDevice";
            ddlDevice.Size = new Size(150, 28);
            ddlDevice.TabIndex = 3;
            ddlDevice.SelectedIndexChanged += ddlDevice_SelectedIndexChanged;
            // 
            // btnConfig
            // 
            btnConfig.Dock = DockStyle.Fill;
            btnConfig.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnConfig.Location = new Point(0, 160);
            btnConfig.Margin = new Padding(0, 2, 0, 6);
            btnConfig.Name = "btnConfig";
            btnConfig.Size = new Size(150, 32);
            btnConfig.TabIndex = 4;
            btnConfig.Text = "Config";
            btnConfig.UseVisualStyleBackColor = true;
            btnConfig.Click += btnConfig_Click;
            // 
            // btnWirelessAdb
            // 
            btnWirelessAdb.Dock = DockStyle.Fill;
            btnWirelessAdb.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnWirelessAdb.Location = new Point(0, 200);
            btnWirelessAdb.Margin = new Padding(0, 2, 0, 6);
            btnWirelessAdb.Name = "btnWirelessAdb";
            btnWirelessAdb.Size = new Size(150, 32);
            btnWirelessAdb.TabIndex = 5;
            btnWirelessAdb.Text = "Pair / Connect";
            btnWirelessAdb.UseVisualStyleBackColor = true;
            btnWirelessAdb.Click += btnWirelessAdb_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(640, 340);
            Controls.Add(mainLayout);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            MinimumSize = new Size(640, 340);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Lazy App";
            FormClosing += Form1_FormClosing;
            mainLayout.ResumeLayout(false);
            contentLayout.ResumeLayout(false);
            selectorLayout.ResumeLayout(false);
            actionPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        private static void AddStatusRow(TableLayoutPanel layout, int row, string name, Label valueLabel)
        {
            var nameLabel = CreateStatusCell(name, ContentAlignment.MiddleLeft);
            var colonLabel = CreateStatusCell(":", ContentAlignment.MiddleCenter);
            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            valueLabel.Margin = new Padding(0);
            valueLabel.Text = "--";
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(nameLabel, 0, row);
            layout.Controls.Add(colonLabel, 1, row);
            layout.Controls.Add(valueLabel, 2, row);
        }

        private static Label CreateStatusCell(string text, ContentAlignment align)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0),
                Margin = new Padding(0),
                Text = text,
                TextAlign = align
            };
        }

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.TableLayoutPanel contentLayout;
        private System.Windows.Forms.TableLayoutPanel selectorLayout;
        private System.Windows.Forms.TableLayoutPanel actionPanel;
        private System.Windows.Forms.Button btnRun;
        private SearchableDropdown ddlScript;
        private System.Windows.Forms.Panel statusDot;
        private System.Windows.Forms.Panel adbStatusDot;
        private System.Windows.Forms.TableLayoutPanel liveStatusLayout;
        private System.Windows.Forms.Label lblCurrentActionValue;
        private System.Windows.Forms.Label lblStepValue;
        private System.Windows.Forms.Label lblCycleValue;
        private System.Windows.Forms.Label lblNextActionValue;
        private System.Windows.Forms.Label lblNextAtValue;
        private System.Windows.Forms.Label lblEstimatedEndValue;
        private System.Windows.Forms.ComboBox ddlOffset;
        private System.Windows.Forms.ComboBox ddlTagFilter;
        private System.Windows.Forms.ComboBox ddlDevice;
        private System.Windows.Forms.Button btnConfig;
        private System.Windows.Forms.Button btnWirelessAdb;
    }
}
