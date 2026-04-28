namespace Lazy_App_Codex_Core
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            splitContainer1 = new SplitContainer();
            btnStatus = new Button();
            lblStatus = new Button();
            ddlScript = new ComboBox();
            ddlOffset = new ComboBox();
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
            splitContainer1.Margin = new Padding(4, 5, 4, 5);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(btnStatus);
            splitContainer1.Panel1.Controls.Add(lblStatus);
            splitContainer1.Panel1.Controls.Add(ddlScript);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(ddlOffset);
            splitContainer1.Panel2.Controls.Add(btnRun);
            splitContainer1.Size = new Size(576, 112);
            splitContainer1.SplitterDistance = 427;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 0;
            // 
            // btnStatus
            // 
            btnStatus.BackColor = Color.Red;
            btnStatus.Enabled = false;
            btnStatus.FlatStyle = FlatStyle.Flat;
            btnStatus.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnStatus.ForeColor = SystemColors.ActiveCaption;
            btnStatus.Location = new Point(375, 60);
            btnStatus.Margin = new Padding(4, 5, 4, 5);
            btnStatus.Name = "btnStatus";
            btnStatus.Size = new Size(32, 28);
            btnStatus.TabIndex = 2;
            btnStatus.TabStop = false;
            btnStatus.UseVisualStyleBackColor = false;
            // 
            // lblStatus
            // 
            lblStatus.BackColor = SystemColors.Window;
            lblStatus.Enabled = false;
            lblStatus.FlatAppearance.BorderSize = 2;
            lblStatus.FlatStyle = FlatStyle.Flat;
            lblStatus.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblStatus.ForeColor = SystemColors.ActiveCaption;
            lblStatus.Location = new Point(16, 60);
            lblStatus.Margin = new Padding(4, 5, 4, 5);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(361, 28);
            lblStatus.TabIndex = 1;
            lblStatus.TabStop = false;
            lblStatus.Text = "STATUS";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.UseVisualStyleBackColor = false;
            // 
            // ddlScript
            // 
            ddlScript.DropDownStyle = ComboBoxStyle.DropDownList;
            ddlScript.FormattingEnabled = true;
            ddlScript.Location = new Point(16, 19);
            ddlScript.Margin = new Padding(4, 5, 4, 5);
            ddlScript.Name = "ddlScript";
            ddlScript.Size = new Size(391, 28);
            ddlScript.TabIndex = 0;
            // 
            // ddlOffset
            // 
            ddlOffset.DropDownStyle = ComboBoxStyle.DropDownList;
            ddlOffset.FormattingEnabled = true;
            ddlOffset.Items.AddRange(new object[] { "-2", "-1", "0", "1", "2" });
            ddlOffset.Location = new Point(4, 60);
            ddlOffset.Margin = new Padding(4, 5, 4, 5);
            ddlOffset.Name = "ddlOffset";
            ddlOffset.Size = new Size(111, 28);
            ddlOffset.TabIndex = 1;
            // 
            // btnRun
            // 
            btnRun.Location = new Point(4, 19);
            btnRun.Margin = new Padding(4, 5, 4, 5);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(111, 35);
            btnRun.TabIndex = 1;
            btnRun.Text = "Run (F3)";
            btnRun.UseVisualStyleBackColor = true;
            btnRun.Click += btnRun_Click;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.FixedPanel = FixedPanel.Panel1;
            splitContainer2.IsSplitterFixed = true;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Margin = new Padding(4, 5, 4, 5);
            splitContainer2.Name = "splitContainer2";
            splitContainer2.Orientation = Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(splitContainer1);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(taLog);
            splitContainer2.Size = new Size(576, 541);
            splitContainer2.SplitterDistance = 112;
            splitContainer2.SplitterWidth = 6;
            splitContainer2.TabIndex = 1;
            // 
            // taLog
            // 
            taLog.Dock = DockStyle.Fill;
            taLog.Location = new Point(0, 0);
            taLog.Margin = new Padding(4, 5, 4, 5);
            taLog.Name = "taLog";
            taLog.Size = new Size(576, 423);
            taLog.TabIndex = 0;
            taLog.Text = "";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(576, 541);
            Controls.Add(splitContainer2);
            Margin = new Padding(4, 5, 4, 5);
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

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.ComboBox ddlScript;
        private System.Windows.Forms.Button lblStatus;
        private System.Windows.Forms.Button btnStatus;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.RichTextBox taLog;
        private System.Windows.Forms.ComboBox ddlOffset;
    }
}

