#nullable disable

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
            SuspendLayout();
            // 
            // mainLayout
            // 
            mainLayout.ColumnCount = 1;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Location = new Point(0, 0);
            mainLayout.Margin = new Padding(0);
            mainLayout.Name = "mainLayout";
            mainLayout.Padding = new Padding(6, 6, 6, 2);
            mainLayout.RowCount = 1;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.Size = new Size(484, 224);
            mainLayout.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(484, 224);
            Controls.Add(mainLayout);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            MinimumSize = new Size(500, 264);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Lazy App";
            FormClosing += Form1_FormClosing;
            ResumeLayout(false);
        }

        private TableLayoutPanel mainLayout;
        private Panel statusDot;
        private Panel adbStatusDot;
        private Button btnConfig;
        private Button btnWirelessAdb;
    }
}
