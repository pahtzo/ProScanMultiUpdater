/*

MIT License

Copyright (c) 2026 Nick DeBaggis

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

 */

// ============================================================================
// File: MainForm.Designer.cs
// ============================================================================
namespace ProScanMultiUpdater
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabMain;
        private System.Windows.Forms.TabPage tabLogging;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.TextBox txtOutput;
        private System.Windows.Forms.Button btnScan;
        private System.Windows.Forms.Button btnKillAndUpdate;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Button btnSelectAll;
        private System.Windows.Forms.Button btnDeselectAll;
        private System.Windows.Forms.TextBox txtSetupPath;
        private System.Windows.Forms.Label lblSetupPath;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem saveLogItem;

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
            this.components = new System.ComponentModel.Container();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabMain = new System.Windows.Forms.TabPage();
            this.linkLabelUpdate = new System.Windows.Forms.LinkLabel();
            this.checkBoxRestartProcs = new System.Windows.Forms.CheckBox();
            this.labelProcsFound = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.txtSetupPath = new System.Windows.Forms.TextBox();
            this.lblSetupPath = new System.Windows.Forms.Label();
            this.btnDeselectAll = new System.Windows.Forms.Button();
            this.btnSelectAll = new System.Windows.Forms.Button();
            this.btnKillAndUpdate = new System.Windows.Forms.Button();
            this.btnScan = new System.Windows.Forms.Button();
            this.tabLogging = new System.Windows.Forms.TabPage();
            this.txtOutput = new System.Windows.Forms.TextBox();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.saveLogItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1.SuspendLayout();
            this.tabMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.tabLogging.SuspendLayout();
            this.contextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabMain);
            this.tabControl1.Controls.Add(this.tabLogging);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(884, 601);
            this.tabControl1.TabIndex = 0;
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.linkLabelUpdate);
            this.tabMain.Controls.Add(this.checkBoxRestartProcs);
            this.tabMain.Controls.Add(this.labelProcsFound);
            this.tabMain.Controls.Add(this.dataGridView1);
            this.tabMain.Controls.Add(this.btnBrowse);
            this.tabMain.Controls.Add(this.txtSetupPath);
            this.tabMain.Controls.Add(this.lblSetupPath);
            this.tabMain.Controls.Add(this.btnDeselectAll);
            this.tabMain.Controls.Add(this.btnSelectAll);
            this.tabMain.Controls.Add(this.btnKillAndUpdate);
            this.tabMain.Controls.Add(this.btnScan);
            this.tabMain.Location = new System.Drawing.Point(4, 22);
            this.tabMain.Name = "tabMain";
            this.tabMain.Size = new System.Drawing.Size(876, 575);
            this.tabMain.TabIndex = 0;
            this.tabMain.Text = "Main";
            // 
            // linkLabelUpdate
            // 
            this.linkLabelUpdate.AutoSize = true;
            this.linkLabelUpdate.Location = new System.Drawing.Point(8, 6);
            this.linkLabelUpdate.Name = "linkLabelUpdate";
            this.linkLabelUpdate.Size = new System.Drawing.Size(0, 13);
            this.linkLabelUpdate.TabIndex = 9;
            this.linkLabelUpdate.Visible = false;
            this.linkLabelUpdate.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelUpdate_LinkClicked);
            // 
            // checkBoxRestartProcs
            // 
            this.checkBoxRestartProcs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxRestartProcs.AutoSize = true;
            this.checkBoxRestartProcs.Checked = true;
            this.checkBoxRestartProcs.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxRestartProcs.Location = new System.Drawing.Point(505, 61);
            this.checkBoxRestartProcs.Name = "checkBoxRestartProcs";
            this.checkBoxRestartProcs.Size = new System.Drawing.Size(153, 17);
            this.checkBoxRestartProcs.TabIndex = 4;
            this.checkBoxRestartProcs.Text = "Restart Updated Instances";
            this.checkBoxRestartProcs.UseVisualStyleBackColor = true;
            // 
            // labelProcsFound
            // 
            this.labelProcsFound.AutoSize = true;
            this.labelProcsFound.Location = new System.Drawing.Point(216, 98);
            this.labelProcsFound.Name = "labelProcsFound";
            this.labelProcsFound.Size = new System.Drawing.Size(98, 13);
            this.labelProcsFound.TabIndex = 8;
            this.labelProcsFound.Text = "Processes found: 0";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(3, 123);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(870, 449);
            this.dataGridView1.TabIndex = 7;
            this.dataGridView1.TabStop = false;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Location = new System.Drawing.Point(768, 23);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(100, 23);
            this.btnBrowse.TabIndex = 0;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.Click += new System.EventHandler(this.BtnBrowse_Click);
            // 
            // txtSetupPath
            // 
            this.txtSetupPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSetupPath.Location = new System.Drawing.Point(135, 25);
            this.txtSetupPath.Name = "txtSetupPath";
            this.txtSetupPath.ReadOnly = true;
            this.txtSetupPath.Size = new System.Drawing.Size(628, 20);
            this.txtSetupPath.TabIndex = 6;
            this.txtSetupPath.TabStop = false;
            // 
            // lblSetupPath
            // 
            this.lblSetupPath.AutoSize = true;
            this.lblSetupPath.Location = new System.Drawing.Point(8, 28);
            this.lblSetupPath.Name = "lblSetupPath";
            this.lblSetupPath.Size = new System.Drawing.Size(121, 13);
            this.lblSetupPath.TabIndex = 6;
            this.lblSetupPath.Text = "ProScan Setup Installer:";
            // 
            // btnDeselectAll
            // 
            this.btnDeselectAll.Location = new System.Drawing.Point(83, 91);
            this.btnDeselectAll.Name = "btnDeselectAll";
            this.btnDeselectAll.Size = new System.Drawing.Size(71, 26);
            this.btnDeselectAll.TabIndex = 3;
            this.btnDeselectAll.Text = "Deselect All";
            this.btnDeselectAll.Click += new System.EventHandler(this.BtnDeselectAll_Click);
            // 
            // btnSelectAll
            // 
            this.btnSelectAll.Location = new System.Drawing.Point(6, 91);
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.Size = new System.Drawing.Size(71, 26);
            this.btnSelectAll.TabIndex = 2;
            this.btnSelectAll.Text = "Select All";
            this.btnSelectAll.Click += new System.EventHandler(this.BtnSelectAll_Click);
            // 
            // btnKillAndUpdate
            // 
            this.btnKillAndUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnKillAndUpdate.Enabled = false;
            this.btnKillAndUpdate.Location = new System.Drawing.Point(664, 55);
            this.btnKillAndUpdate.Name = "btnKillAndUpdate";
            this.btnKillAndUpdate.Size = new System.Drawing.Size(204, 26);
            this.btnKillAndUpdate.TabIndex = 5;
            this.btnKillAndUpdate.Text = "Close Selected Instances && Update";
            this.btnKillAndUpdate.Click += new System.EventHandler(this.BtnKillAndUpdate_Click);
            // 
            // btnScan
            // 
            this.btnScan.Location = new System.Drawing.Point(7, 55);
            this.btnScan.Name = "btnScan";
            this.btnScan.Size = new System.Drawing.Size(204, 26);
            this.btnScan.TabIndex = 1;
            this.btnScan.Text = "Scan For Running ProScan Instances";
            this.btnScan.Click += new System.EventHandler(this.BtnScan_Click);
            // 
            // tabLogging
            // 
            this.tabLogging.Controls.Add(this.txtOutput);
            this.tabLogging.Location = new System.Drawing.Point(4, 22);
            this.tabLogging.Name = "tabLogging";
            this.tabLogging.Size = new System.Drawing.Size(876, 575);
            this.tabLogging.TabIndex = 1;
            this.tabLogging.Text = "Logging";
            // 
            // txtOutput
            // 
            this.txtOutput.ContextMenuStrip = this.contextMenu;
            this.txtOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtOutput.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtOutput.Location = new System.Drawing.Point(0, 0);
            this.txtOutput.Multiline = true;
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtOutput.Size = new System.Drawing.Size(876, 575);
            this.txtOutput.TabIndex = 0;
            this.txtOutput.WordWrap = false;
            this.txtOutput.TextChanged += new System.EventHandler(this.txtOutput_TextChanged);
            // 
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveLogItem});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(147, 26);
            // 
            // saveLogItem
            // 
            this.saveLogItem.Name = "saveLogItem";
            this.saveLogItem.Size = new System.Drawing.Size(146, 22);
            this.saveLogItem.Text = "Save Log As...";
            this.saveLogItem.Click += new System.EventHandler(this.SaveLog_Click);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(884, 601);
            this.Controls.Add(this.tabControl1);
            this.MinimumSize = new System.Drawing.Size(620, 300);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ProScanMultiUpdater";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.tabControl1.ResumeLayout(false);
            this.tabMain.ResumeLayout(false);
            this.tabMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.tabLogging.ResumeLayout(false);
            this.tabLogging.PerformLayout();
            this.contextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.Label labelProcsFound;
        private System.Windows.Forms.CheckBox checkBoxRestartProcs;
        private System.Windows.Forms.LinkLabel linkLabelUpdate;
    }
}