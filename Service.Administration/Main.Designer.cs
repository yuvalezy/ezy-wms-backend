namespace Service.Administration
{
    partial class Main
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
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.notifyMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.txtServer = new System.Windows.Forms.TextBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.dg = new System.Windows.Forms.DataGridView();
            this.gridActive = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.gridName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridDesc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridAccount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridStartStop = new System.Windows.Forms.DataGridViewButtonColumn();
            this.gridRestart = new System.Windows.Forms.DataGridViewButtonColumn();
            this.rightClickMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuAPISettings = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuPrintAPISettings = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuHelloWorldProcess = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuServiceAccount = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuReloadSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.lblServer = new System.Windows.Forms.Label();
            this.chkShowActiveOnly = new System.Windows.Forms.CheckBox();
            this.notifyMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dg)).BeginInit();
            this.rightClickMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon
            // 
            this.notifyIcon.ContextMenuStrip = this.notifyMenu;
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "Light WMS Service Administrator";
            this.notifyIcon.Visible = true;
            this.notifyIcon.DoubleClick += new System.EventHandler(this.NotifyIconDoubleCLick);
            // 
            // notifyMenu
            // 
            this.notifyMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.notifyMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuOpen,
            this.mnuAbout,
            this.toolStripMenuItem1,
            this.mnuExit});
            this.notifyMenu.Name = "notifyMenu";
            this.notifyMenu.Size = new System.Drawing.Size(160, 106);
            this.notifyMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.NotifyMenuItemClick);
            // 
            // mnuOpen
            // 
            this.mnuOpen.Name = "mnuOpen";
            this.mnuOpen.Size = new System.Drawing.Size(159, 32);
            this.mnuOpen.Text = "&Open";
            // 
            // mnuAbout
            // 
            this.mnuAbout.Name = "mnuAbout";
            this.mnuAbout.Size = new System.Drawing.Size(159, 32);
            this.mnuAbout.Text = "&About Us";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(156, 6);
            // 
            // mnuExit
            // 
            this.mnuExit.Name = "mnuExit";
            this.mnuExit.Size = new System.Drawing.Size(159, 32);
            this.mnuExit.Text = "&Exit";
            // 
            // txtServer
            // 
            this.txtServer.Location = new System.Drawing.Point(129, 18);
            this.txtServer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtServer.Name = "txtServer";
            this.txtServer.ReadOnly = true;
            this.txtServer.Size = new System.Drawing.Size(562, 26);
            this.txtServer.TabIndex = 2;
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(22, 494);
            this.btnClose.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(112, 35);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.CloseButtonClicked);
            // 
            // dg
            // 
            this.dg.AllowUserToAddRows = false;
            this.dg.AllowUserToDeleteRows = false;
            this.dg.AllowUserToResizeColumns = false;
            this.dg.AllowUserToResizeRows = false;
            this.dg.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dg.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dg.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.gridActive,
            this.gridName,
            this.gridDesc,
            this.gridVersion,
            this.gridAccount,
            this.gridStatus,
            this.gridStartStop,
            this.gridRestart});
            this.dg.ContextMenuStrip = this.rightClickMenu;
            this.dg.Location = new System.Drawing.Point(18, 58);
            this.dg.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.dg.Name = "dg";
            this.dg.RowHeadersVisible = false;
            this.dg.RowHeadersWidth = 62;
            this.dg.Size = new System.Drawing.Size(1440, 426);
            this.dg.TabIndex = 4;
            this.dg.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dg_CellContentClick);
            this.dg.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dg_CellMouseDown);
            this.dg.CellMouseEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.dg_CellMouseEnter);
            this.dg.CurrentCellDirtyStateChanged += new System.EventHandler(this.dg_CurrentCellDirtyStateChanged);
            // 
            // gridActive
            // 
            this.gridActive.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.gridActive.DataPropertyName = "Active";
            this.gridActive.FalseValue = "N";
            this.gridActive.HeaderText = "Active";
            this.gridActive.MinimumWidth = 8;
            this.gridActive.Name = "gridActive";
            this.gridActive.TrueValue = "Y";
            this.gridActive.Width = 50;
            // 
            // gridName
            // 
            this.gridName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.gridName.DataPropertyName = "Name";
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.gridName.DefaultCellStyle = dataGridViewCellStyle1;
            this.gridName.HeaderText = "Database";
            this.gridName.MinimumWidth = 8;
            this.gridName.Name = "gridName";
            this.gridName.ReadOnly = true;
            this.gridName.Width = 180;
            // 
            // gridDesc
            // 
            this.gridDesc.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.gridDesc.DataPropertyName = "Desc";
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.ControlLight;
            this.gridDesc.DefaultCellStyle = dataGridViewCellStyle2;
            this.gridDesc.HeaderText = "Company Name";
            this.gridDesc.MinimumWidth = 8;
            this.gridDesc.Name = "gridDesc";
            this.gridDesc.ReadOnly = true;
            // 
            // gridVersion
            // 
            this.gridVersion.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.gridVersion.DataPropertyName = "Version";
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.ControlLight;
            this.gridVersion.DefaultCellStyle = dataGridViewCellStyle3;
            this.gridVersion.FillWeight = 50F;
            this.gridVersion.HeaderText = "Version";
            this.gridVersion.MinimumWidth = 8;
            this.gridVersion.Name = "gridVersion";
            this.gridVersion.ReadOnly = true;
            this.gridVersion.Width = 50;
            // 
            // gridAccount
            // 
            this.gridAccount.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.gridAccount.DataPropertyName = "Account";
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.ControlLight;
            this.gridAccount.DefaultCellStyle = dataGridViewCellStyle4;
            this.gridAccount.HeaderText = "Account";
            this.gridAccount.MinimumWidth = 8;
            this.gridAccount.Name = "gridAccount";
            this.gridAccount.ReadOnly = true;
            this.gridAccount.Width = 160;
            // 
            // gridStatus
            // 
            this.gridStatus.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.gridStatus.DataPropertyName = "Status";
            dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.ControlLight;
            this.gridStatus.DefaultCellStyle = dataGridViewCellStyle5;
            this.gridStatus.HeaderText = "Status";
            this.gridStatus.MinimumWidth = 8;
            this.gridStatus.Name = "gridStatus";
            this.gridStatus.ReadOnly = true;
            this.gridStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.gridStatus.Width = 60;
            // 
            // gridStartStop
            // 
            this.gridStartStop.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.gridStartStop.DataPropertyName = "StartStop";
            this.gridStartStop.HeaderText = "";
            this.gridStartStop.MinimumWidth = 8;
            this.gridStartStop.Name = "gridStartStop";
            this.gridStartStop.Width = 60;
            // 
            // gridRestart
            // 
            this.gridRestart.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.gridRestart.DataPropertyName = "Restart";
            this.gridRestart.HeaderText = "";
            this.gridRestart.MinimumWidth = 8;
            this.gridRestart.Name = "gridRestart";
            this.gridRestart.Width = 60;
            // 
            // rightClickMenu
            // 
            this.rightClickMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.rightClickMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuAPISettings,
            this.mnuPrintAPISettings,
            this.mnuHelloWorldProcess,
            this.mnuServiceAccount,
            this.toolStripMenuItem2,
            this.mnuReloadSettings});
            this.rightClickMenu.Name = "rightClickMenu";
            this.rightClickMenu.Size = new System.Drawing.Size(280, 203);
            this.rightClickMenu.Opening += new System.ComponentModel.CancelEventHandler(this.rightClickMenu_Opening);
            // 
            // mnuAPISettings
            // 
            this.mnuAPISettings.Name = "mnuAPISettings";
            this.mnuAPISettings.Size = new System.Drawing.Size(279, 32);
            this.mnuAPISettings.Text = "&Rest API Settings";
            this.mnuAPISettings.Click += new System.EventHandler(this.APISettingsClicked);
            // 
            // mnuPrintAPISettings
            // 
            this.mnuPrintAPISettings.Enabled = false;
            this.mnuPrintAPISettings.Name = "mnuPrintAPISettings";
            this.mnuPrintAPISettings.Size = new System.Drawing.Size(279, 32);
            this.mnuPrintAPISettings.Text = "Print API Settings";
            this.mnuPrintAPISettings.Click += new System.EventHandler(this.PrintAPISettings_Click);
            // 
            // mnuHelloWorldProcess
            // 
            this.mnuHelloWorldProcess.Name = "mnuHelloWorldProcess";
            this.mnuHelloWorldProcess.Size = new System.Drawing.Size(279, 32);
            this.mnuHelloWorldProcess.Text = "Run &Hello World Process";
            // 
            // mnuServiceAccount
            // 
            this.mnuServiceAccount.Name = "mnuServiceAccount";
            this.mnuServiceAccount.Size = new System.Drawing.Size(279, 32);
            this.mnuServiceAccount.Text = "Service Account";
            this.mnuServiceAccount.Click += new System.EventHandler(this.ServiceAccountClicked);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(276, 6);
            // 
            // mnuReloadSettings
            // 
            this.mnuReloadSettings.Name = "mnuReloadSettings";
            this.mnuReloadSettings.Size = new System.Drawing.Size(279, 32);
            this.mnuReloadSettings.Text = "Reload &Settings";
            this.mnuReloadSettings.Click += new System.EventHandler(this.ReloadSettingsClicked);
            // 
            // lblServer
            // 
            this.lblServer.AutoSize = true;
            this.lblServer.Location = new System.Drawing.Point(18, 23);
            this.lblServer.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblServer.Name = "lblServer";
            this.lblServer.Size = new System.Drawing.Size(55, 20);
            this.lblServer.TabIndex = 1;
            this.lblServer.Text = "Server";
            // 
            // chkShowActiveOnly
            // 
            this.chkShowActiveOnly.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkShowActiveOnly.AutoSize = true;
            this.chkShowActiveOnly.Checked = true;
            this.chkShowActiveOnly.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowActiveOnly.Location = new System.Drawing.Point(1306, 23);
            this.chkShowActiveOnly.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.chkShowActiveOnly.Name = "chkShowActiveOnly";
            this.chkShowActiveOnly.Size = new System.Drawing.Size(152, 24);
            this.chkShowActiveOnly.TabIndex = 5;
            this.chkShowActiveOnly.Text = "Show active only";
            this.chkShowActiveOnly.UseVisualStyleBackColor = true;
            this.chkShowActiveOnly.CheckedChanged += new System.EventHandler(this.chkShowActiveOnly_CheckedChanged);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1476, 548);
            this.Controls.Add(this.chkShowActiveOnly);
            this.Controls.Add(this.dg);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.txtServer);
            this.Controls.Add(this.lblServer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Light WMS Administration";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFormClosing);
            this.Load += new System.EventHandler(this.FormLoad);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainKeyDown);
            this.notifyMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dg)).EndInit();
            this.rightClickMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.CheckBox chkShowActiveOnly;
        private System.Windows.Forms.DataGridView dg;
        private System.Windows.Forms.Label lblServer;
        private System.Windows.Forms.ToolStripMenuItem mnuAbout;
        private System.Windows.Forms.ToolStripMenuItem mnuExit;
        private System.Windows.Forms.ToolStripMenuItem mnuOpen;
        private System.Windows.Forms.ToolStripMenuItem mnuAPISettings;
        private System.Windows.Forms.ToolStripMenuItem mnuReloadSettings;
        private System.Windows.Forms.ToolStripMenuItem mnuHelloWorldProcess;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip notifyMenu;
        private System.Windows.Forms.ContextMenuStrip rightClickMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.TextBox txtServer;

        #endregion

        private System.Windows.Forms.ToolStripMenuItem mnuPrintAPISettings;
        private System.Windows.Forms.DataGridViewCheckBoxColumn gridActive;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridName;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridDesc;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridVersion;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridAccount;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridStatus;
        private System.Windows.Forms.DataGridViewButtonColumn gridStartStop;
        private System.Windows.Forms.DataGridViewButtonColumn gridRestart;
        private System.Windows.Forms.ToolStripMenuItem mnuServiceAccount;
    }
}

