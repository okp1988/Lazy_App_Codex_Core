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
            splitContainer1 = new SplitContainer();
            statusDot = new Panel();
            liveStatusLayout = new TableLayoutPanel();
            lblCurrentActionValue = new Label();
            lblStepValue = new Label();
            lblCycleValue = new Label();
            lblNextActionValue = new Label();
            lblNextAtValue = new Label();
            lblEstimatedEndValue = new Label();
            ddlScript = new ComboBox();
            ddlOffset = new ComboBox();
            btnConfig = new Button();
            btnRun = new Button();
            splitContainer2 = new SplitContainer();
            taLog = new RichTextBox();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel2;
            splitContainer1.IsSplitterFixed = true;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Panel1MinSize = 340;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(statusDot);
            splitContainer1.Panel1.Controls.Add(liveStatusLayout);
            splitContainer1.Panel1.Controls.Add(ddlScript);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(btnConfig);
            splitContainer1.Panel2.Controls.Add(ddlOffset);
            splitContainer1.Panel2.Controls.Add(btnRun);
            splitContainer1.Panel2MinSize = 125;
            splitContainer1.Size = new Size(620, 278);
            splitContainer1.SplitterDistance = 466;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 0;
            // 
            // statusDot
            // 
            statusDot.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            statusDot.BackColor = Color.Transparent;
            statusDot.Location = new Point(442, 25);
            statusDot.Name = "statusDot";
            statusDot.Size = new Size(12, 12);
            statusDot.TabIndex = 7;
            statusDot.Paint += statusDot_Paint;
            // 
            // liveStatusLayout
            // 
            liveStatusLayout.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            liveStatusLayout.ColumnCount = 3;
            liveStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
            liveStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18F));
            liveStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            liveStatusLayout.Location = new Point(16, 68);
            liveStatusLayout.Name = "liveStatusLayout";
            liveStatusLayout.RowCount = 6;
            for (int i = 0; i < 6; i++)
            {
                liveStatusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            }
            liveStatusLayout.Size = new Size(438, 168);
            liveStatusLayout.TabIndex = 8;
            AddStatusRow(liveStatusLayout, 0, "Current Action", lblCurrentActionValue);
            AddStatusRow(liveStatusLayout, 1, "Current Step", lblStepValue);
            AddStatusRow(liveStatusLayout, 2, "Current Cycle", lblCycleValue);
            AddStatusRow(liveStatusLayout, 3, "Next Action", lblNextActionValue);
            AddStatusRow(liveStatusLayout, 4, "Next Action At", lblNextAtValue);
            AddStatusRow(liveStatusLayout, 5, "Estimated End", lblEstimatedEndValue);
            // 
            // ddlScript
            // 
            ddlScript.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            ddlScript.DropDownStyle = ComboBoxStyle.DropDown;
            ddlScript.FormattingEnabled = true;
            ddlScript.Location = new Point(16, 18);
            ddlScript.Name = "ddlScript";
            ddlScript.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ddlScript.Size = new Size(418, 28);
            ddlScript.TabIndex = 0;
            // 
            // ddlOffset
            // 
            ddlOffset.DropDownStyle = ComboBoxStyle.DropDownList;
            ddlOffset.FormattingEnabled = true;
            ddlOffset.Items.AddRange(new object[] { "-2:y", "-1:y", "0", "1:y", "2:y", "-2:x", "-1:x", "1:x", "2:x" });
            ddlOffset.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ddlOffset.Location = new Point(4, 66);
            ddlOffset.Name = "ddlOffset";
            ddlOffset.Size = new Size(111, 28);
            ddlOffset.TabIndex = 1;
            // 
            // btnConfig
            // 
            btnConfig.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnConfig.Location = new Point(4, 110);
            btnConfig.Name = "btnConfig";
            btnConfig.Size = new Size(111, 35);
            btnConfig.TabIndex = 2;
            btnConfig.Text = "Config";
            btnConfig.UseVisualStyleBackColor = true;
            btnConfig.Click += btnConfig_Click;
            // 
            // btnRun
            // 
            btnRun.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnRun.Location = new Point(4, 17);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(111, 35);
            btnRun.TabIndex = 1;
            btnRun.Text = "Run";
            btnRun.UseVisualStyleBackColor = true;
            btnRun.Click += btnRun_Click;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.FixedPanel = FixedPanel.Panel1;
            splitContainer2.IsSplitterFixed = true;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            splitContainer2.Orientation = Orientation.Horizontal;
            splitContainer2.Panel1.Controls.Add(splitContainer1);
            splitContainer2.Panel2.Controls.Add(taLog);
            splitContainer2.Panel2Collapsed = true;
            splitContainer2.Size = new Size(620, 278);
            splitContainer2.SplitterDistance = 278;
            splitContainer2.TabIndex = 1;
            // 
            // taLog
            // 
            taLog.Dock = DockStyle.Fill;
            taLog.Location = new Point(0, 0);
            taLog.Name = "taLog";
            taLog.Size = new Size(150, 46);
            taLog.TabIndex = 0;
            taLog.Text = "";
            taLog.Visible = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(620, 278);
            Controls.Add(splitContainer2);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            MinimumSize = new Size(560, 320);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Lazy App";
            FormClosing += Form1_FormClosing;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
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

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.ComboBox ddlScript;
        private System.Windows.Forms.Panel statusDot;
        private System.Windows.Forms.TableLayoutPanel liveStatusLayout;
        private System.Windows.Forms.Label lblCurrentActionValue;
        private System.Windows.Forms.Label lblStepValue;
        private System.Windows.Forms.Label lblCycleValue;
        private System.Windows.Forms.Label lblNextActionValue;
        private System.Windows.Forms.Label lblNextAtValue;
        private System.Windows.Forms.Label lblEstimatedEndValue;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.RichTextBox taLog;
        private System.Windows.Forms.ComboBox ddlOffset;
        private System.Windows.Forms.Button btnConfig;
    }
}
