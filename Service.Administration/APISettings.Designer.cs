namespace Service.Administration {
    partial class APISettings {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(APISettings));
            this.btnAccept = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.txtPort = new System.Windows.Forms.NumericUpDown();
            this.lblPort = new System.Windows.Forms.Label();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabTokens = new System.Windows.Forms.TabPage();
            this.gridTokens = new System.Windows.Forms.DataGridView();
            this.colTokenName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTokenID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTokenKey = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ds = new System.Data.DataSet();
            this.dtLayouts = new System.Data.DataTable();
            this.dataColumn1 = new System.Data.DataColumn();
            this.dataColumn2 = new System.Data.DataColumn();
            this.dataColumn3 = new System.Data.DataColumn();
            this.dataColumn4 = new System.Data.DataColumn();
            this.dtTokens = new System.Data.DataTable();
            this.dataColumn5 = new System.Data.DataColumn();
            this.dataColumn6 = new System.Data.DataColumn();
            this.dataColumn7 = new System.Data.DataColumn();
            this.dtNodes = new System.Data.DataTable();
            this.colPort = new System.Data.DataColumn();
            this.dataColumn8 = new System.Data.DataColumn();
            this.tabLB = new System.Windows.Forms.TabPage();
            this.pnlPortsContent = new System.Windows.Forms.Panel();
            this.gridNodes = new System.Windows.Forms.DataGridView();
            this.colNodeID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPortDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlPortsUp = new System.Windows.Forms.Panel();
            this.chkRedisServer = new System.Windows.Forms.CheckBox();
            this.txtRedisServer = new System.Windows.Forms.TextBox();
            this.txtNodesRestart = new System.Windows.Forms.NumericUpDown();
            this.txtOpRestart = new System.Windows.Forms.NumericUpDown();
            this.lblNodesRestart = new System.Windows.Forms.Label();
            this.txtNodes = new System.Windows.Forms.NumericUpDown();
            this.lblNodesHours = new System.Windows.Forms.Label();
            this.lblOpHours = new System.Windows.Forms.Label();
            this.lblOpRestart = new System.Windows.Forms.Label();
            this.lblRedisServer = new System.Windows.Forms.Label();
            this.lblNodes = new System.Windows.Forms.Label();
            this.chkActive = new System.Windows.Forms.CheckBox();
            this.lblTokenAlert = new System.Windows.Forms.Label();
            this.chkLB = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.txtPort)).BeginInit();
            this.tabControl.SuspendLayout();
            this.tabTokens.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridTokens)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ds)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtLayouts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtTokens)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtNodes)).BeginInit();
            this.tabLB.SuspendLayout();
            this.pnlPortsContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridNodes)).BeginInit();
            this.pnlPortsUp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtNodesRestart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtOpRestart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtNodes)).BeginInit();
            this.SuspendLayout();
            // 
            // btnAccept
            // 
            this.btnAccept.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAccept.Location = new System.Drawing.Point(12, 403);
            this.btnAccept.Name = "btnAccept";
            this.btnAccept.Size = new System.Drawing.Size(75, 23);
            this.btnAccept.TabIndex = 90;
            this.btnAccept.Text = "&Accept";
            this.btnAccept.UseVisualStyleBackColor = true;
            this.btnAccept.Click += new System.EventHandler(this.AcceptClicked);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(93, 403);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 100;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(648, 12);
            this.txtPort.Maximum = new decimal(new int[] {
            32727,
            0,
            0,
            0});
            this.txtPort.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(120, 20);
            this.txtPort.TabIndex = 10;
            this.txtPort.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtPort.Value = new decimal(new int[] {
            9000,
            0,
            0,
            0});
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(529, 16);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(60, 13);
            this.lblPort.TabIndex = 4;
            this.lblPort.Text = "Server Port";
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tabTokens);
            this.tabControl.Controls.Add(this.tabLB);
            this.tabControl.Location = new System.Drawing.Point(12, 67);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(760, 330);
            this.tabControl.TabIndex = 7;
            // 
            // tabTokens
            // 
            this.tabTokens.Controls.Add(this.gridTokens);
            this.tabTokens.Location = new System.Drawing.Point(4, 22);
            this.tabTokens.Name = "tabTokens";
            this.tabTokens.Padding = new System.Windows.Forms.Padding(3);
            this.tabTokens.Size = new System.Drawing.Size(752, 304);
            this.tabTokens.TabIndex = 1;
            this.tabTokens.Text = "Access Tokens";
            this.tabTokens.UseVisualStyleBackColor = true;
            // 
            // gridTokens
            // 
            this.gridTokens.AutoGenerateColumns = false;
            this.gridTokens.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridTokens.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colTokenName,
            this.colTokenID,
            this.colTokenKey});
            this.gridTokens.DataMember = "Tokens";
            this.gridTokens.DataSource = this.ds;
            this.gridTokens.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridTokens.Location = new System.Drawing.Point(3, 3);
            this.gridTokens.Name = "gridTokens";
            this.gridTokens.Size = new System.Drawing.Size(746, 298);
            this.gridTokens.TabIndex = 30;
            this.gridTokens.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.TokensCellEdit);
            // 
            // colTokenName
            // 
            this.colTokenName.DataPropertyName = "Name";
            this.colTokenName.HeaderText = "Name";
            this.colTokenName.Name = "colTokenName";
            this.colTokenName.Width = 200;
            // 
            // colTokenID
            // 
            this.colTokenID.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colTokenID.DataPropertyName = "ID";
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.colTokenID.DefaultCellStyle = dataGridViewCellStyle1;
            this.colTokenID.HeaderText = "Client ID";
            this.colTokenID.Name = "colTokenID";
            this.colTokenID.ReadOnly = true;
            // 
            // colTokenKey
            // 
            this.colTokenKey.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colTokenKey.DataPropertyName = "Key";
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.ControlLight;
            this.colTokenKey.DefaultCellStyle = dataGridViewCellStyle2;
            this.colTokenKey.HeaderText = "Key";
            this.colTokenKey.Name = "colTokenKey";
            this.colTokenKey.ReadOnly = true;
            // 
            // ds
            // 
            this.ds.DataSetName = "NewDataSet";
            this.ds.Tables.AddRange(new System.Data.DataTable[] {
            this.dtLayouts,
            this.dtTokens,
            this.dtNodes});
            // 
            // dtLayouts
            // 
            this.dtLayouts.Columns.AddRange(new System.Data.DataColumn[] {
            this.dataColumn1,
            this.dataColumn2,
            this.dataColumn3,
            this.dataColumn4});
            this.dtLayouts.TableName = "Layouts";
            // 
            // dataColumn1
            // 
            this.dataColumn1.ColumnName = "ID";
            this.dataColumn1.DataType = typeof(int);
            // 
            // dataColumn2
            // 
            this.dataColumn2.ColumnName = "Name";
            // 
            // dataColumn3
            // 
            this.dataColumn3.ColumnName = "Printer";
            // 
            // dataColumn4
            // 
            this.dataColumn4.ColumnName = "Active";
            this.dataColumn4.DataType = typeof(bool);
            // 
            // dtTokens
            // 
            this.dtTokens.Columns.AddRange(new System.Data.DataColumn[] {
            this.dataColumn5,
            this.dataColumn6,
            this.dataColumn7});
            this.dtTokens.TableName = "Tokens";
            // 
            // dataColumn5
            // 
            this.dataColumn5.ColumnName = "ID";
            // 
            // dataColumn6
            // 
            this.dataColumn6.ColumnName = "Key";
            // 
            // dataColumn7
            // 
            this.dataColumn7.ColumnName = "Name";
            this.dataColumn7.MaxLength = 100;
            // 
            // dtNodes
            // 
            this.dtNodes.Columns.AddRange(new System.Data.DataColumn[] {
            this.colPort,
            this.dataColumn8});
            this.dtNodes.TableName = "Ports";
            // 
            // colPort
            // 
            this.colPort.Caption = "Port";
            this.colPort.ColumnName = "Port";
            this.colPort.DataType = typeof(short);
            // 
            // dataColumn8
            // 
            this.dataColumn8.ColumnName = "ID";
            this.dataColumn8.DataType = typeof(short);
            // 
            // tabLB
            // 
            this.tabLB.Controls.Add(this.pnlPortsContent);
            this.tabLB.Controls.Add(this.pnlPortsUp);
            this.tabLB.Location = new System.Drawing.Point(4, 22);
            this.tabLB.Name = "tabLB";
            this.tabLB.Padding = new System.Windows.Forms.Padding(3);
            this.tabLB.Size = new System.Drawing.Size(752, 304);
            this.tabLB.TabIndex = 2;
            this.tabLB.Text = "Load Balancing";
            this.tabLB.UseVisualStyleBackColor = true;
            // 
            // pnlPortsContent
            // 
            this.pnlPortsContent.Controls.Add(this.gridNodes);
            this.pnlPortsContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPortsContent.Location = new System.Drawing.Point(3, 103);
            this.pnlPortsContent.Name = "pnlPortsContent";
            this.pnlPortsContent.Size = new System.Drawing.Size(746, 198);
            this.pnlPortsContent.TabIndex = 1;
            // 
            // gridNodes
            // 
            this.gridNodes.AllowUserToAddRows = false;
            this.gridNodes.AllowUserToDeleteRows = false;
            this.gridNodes.AutoGenerateColumns = false;
            this.gridNodes.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridNodes.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colNodeID,
            this.colPortDataGridViewTextBoxColumn});
            this.gridNodes.DataMember = "Ports";
            this.gridNodes.DataSource = this.ds;
            this.gridNodes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridNodes.Location = new System.Drawing.Point(0, 0);
            this.gridNodes.Name = "gridNodes";
            this.gridNodes.Size = new System.Drawing.Size(746, 198);
            this.gridNodes.TabIndex = 8;
            // 
            // colNodeID
            // 
            this.colNodeID.DataPropertyName = "ID";
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.ControlLight;
            this.colNodeID.DefaultCellStyle = dataGridViewCellStyle3;
            this.colNodeID.FillWeight = 40F;
            this.colNodeID.HeaderText = "#";
            this.colNodeID.Name = "colNodeID";
            this.colNodeID.ReadOnly = true;
            // 
            // colPortDataGridViewTextBoxColumn
            // 
            this.colPortDataGridViewTextBoxColumn.DataPropertyName = "Port";
            this.colPortDataGridViewTextBoxColumn.HeaderText = "Port";
            this.colPortDataGridViewTextBoxColumn.Name = "colPortDataGridViewTextBoxColumn";
            // 
            // pnlPortsUp
            // 
            this.pnlPortsUp.Controls.Add(this.chkRedisServer);
            this.pnlPortsUp.Controls.Add(this.txtRedisServer);
            this.pnlPortsUp.Controls.Add(this.txtNodesRestart);
            this.pnlPortsUp.Controls.Add(this.txtOpRestart);
            this.pnlPortsUp.Controls.Add(this.lblNodesRestart);
            this.pnlPortsUp.Controls.Add(this.txtNodes);
            this.pnlPortsUp.Controls.Add(this.lblNodesHours);
            this.pnlPortsUp.Controls.Add(this.lblOpHours);
            this.pnlPortsUp.Controls.Add(this.lblOpRestart);
            this.pnlPortsUp.Controls.Add(this.lblRedisServer);
            this.pnlPortsUp.Controls.Add(this.lblNodes);
            this.pnlPortsUp.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlPortsUp.Location = new System.Drawing.Point(3, 3);
            this.pnlPortsUp.Name = "pnlPortsUp";
            this.pnlPortsUp.Size = new System.Drawing.Size(746, 100);
            this.pnlPortsUp.TabIndex = 0;
            // 
            // chkRedisServer
            // 
            this.chkRedisServer.AutoSize = true;
            this.chkRedisServer.Location = new System.Drawing.Point(7, 10);
            this.chkRedisServer.Name = "chkRedisServer";
            this.chkRedisServer.Size = new System.Drawing.Size(123, 17);
            this.chkRedisServer.TabIndex = 81;
            this.chkRedisServer.Text = "Enable Redis Server";
            this.chkRedisServer.UseVisualStyleBackColor = true;
            this.chkRedisServer.CheckedChanged += new System.EventHandler(this.EnableRedisChanged);
            // 
            // txtRedisServer
            // 
            this.txtRedisServer.Location = new System.Drawing.Point(78, 32);
            this.txtRedisServer.Name = "txtRedisServer";
            this.txtRedisServer.Size = new System.Drawing.Size(277, 20);
            this.txtRedisServer.TabIndex = 50;
            // 
            // txtNodesRestart
            // 
            this.txtNodesRestart.Location = new System.Drawing.Point(479, 32);
            this.txtNodesRestart.Maximum = new decimal(new int[] {
            24,
            0,
            0,
            0});
            this.txtNodesRestart.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.txtNodesRestart.Name = "txtNodesRestart";
            this.txtNodesRestart.ReadOnly = true;
            this.txtNodesRestart.Size = new System.Drawing.Size(120, 20);
            this.txtNodesRestart.TabIndex = 80;
            this.txtNodesRestart.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtNodesRestart.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this.txtNodesRestart.ValueChanged += new System.EventHandler(this.NodesValueChanged);
            // 
            // txtOpRestart
            // 
            this.txtOpRestart.Location = new System.Drawing.Point(479, 6);
            this.txtOpRestart.Maximum = new decimal(new int[] {
            24,
            0,
            0,
            0});
            this.txtOpRestart.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.txtOpRestart.Name = "txtOpRestart";
            this.txtOpRestart.ReadOnly = true;
            this.txtOpRestart.Size = new System.Drawing.Size(120, 20);
            this.txtOpRestart.TabIndex = 70;
            this.txtOpRestart.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtOpRestart.Value = new decimal(new int[] {
            24,
            0,
            0,
            0});
            this.txtOpRestart.ValueChanged += new System.EventHandler(this.NodesValueChanged);
            // 
            // lblNodesRestart
            // 
            this.lblNodesRestart.AutoSize = true;
            this.lblNodesRestart.Location = new System.Drawing.Point(379, 36);
            this.lblNodesRestart.Name = "lblNodesRestart";
            this.lblNodesRestart.Size = new System.Drawing.Size(75, 13);
            this.lblNodesRestart.TabIndex = 4;
            this.lblNodesRestart.Text = "Nodes Restart";
            // 
            // txtNodes
            // 
            this.txtNodes.Location = new System.Drawing.Point(78, 58);
            this.txtNodes.Maximum = new decimal(new int[] {
            32727,
            0,
            0,
            0});
            this.txtNodes.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.txtNodes.Name = "txtNodes";
            this.txtNodes.ReadOnly = true;
            this.txtNodes.Size = new System.Drawing.Size(120, 20);
            this.txtNodes.TabIndex = 60;
            this.txtNodes.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtNodes.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.txtNodes.ValueChanged += new System.EventHandler(this.NodesValueChanged);
            // 
            // lblNodesHours
            // 
            this.lblNodesHours.AutoSize = true;
            this.lblNodesHours.Location = new System.Drawing.Point(605, 34);
            this.lblNodesHours.Name = "lblNodesHours";
            this.lblNodesHours.Size = new System.Drawing.Size(41, 13);
            this.lblNodesHours.TabIndex = 4;
            this.lblNodesHours.Text = "(Hours)";
            // 
            // lblOpHours
            // 
            this.lblOpHours.AutoSize = true;
            this.lblOpHours.Location = new System.Drawing.Point(605, 8);
            this.lblOpHours.Name = "lblOpHours";
            this.lblOpHours.Size = new System.Drawing.Size(41, 13);
            this.lblOpHours.TabIndex = 4;
            this.lblOpHours.Text = "(Hours)";
            // 
            // lblOpRestart
            // 
            this.lblOpRestart.AutoSize = true;
            this.lblOpRestart.Location = new System.Drawing.Point(379, 10);
            this.lblOpRestart.Name = "lblOpRestart";
            this.lblOpRestart.Size = new System.Drawing.Size(95, 13);
            this.lblOpRestart.TabIndex = 4;
            this.lblOpRestart.Text = "Operations Restart";
            // 
            // lblRedisServer
            // 
            this.lblRedisServer.AutoSize = true;
            this.lblRedisServer.Location = new System.Drawing.Point(4, 36);
            this.lblRedisServer.Name = "lblRedisServer";
            this.lblRedisServer.Size = new System.Drawing.Size(68, 13);
            this.lblRedisServer.TabIndex = 4;
            this.lblRedisServer.Text = "Redis Server";
            // 
            // lblNodes
            // 
            this.lblNodes.AutoSize = true;
            this.lblNodes.Location = new System.Drawing.Point(4, 62);
            this.lblNodes.Name = "lblNodes";
            this.lblNodes.Size = new System.Drawing.Size(38, 13);
            this.lblNodes.TabIndex = 4;
            this.lblNodes.Text = "Nodes";
            // 
            // chkActive
            // 
            this.chkActive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkActive.AutoSize = true;
            this.chkActive.Location = new System.Drawing.Point(19, 12);
            this.chkActive.Name = "chkActive";
            this.chkActive.Size = new System.Drawing.Size(56, 17);
            this.chkActive.TabIndex = 0;
            this.chkActive.Text = "Active";
            this.chkActive.UseVisualStyleBackColor = true;
            // 
            // lblTokenAlert
            // 
            this.lblTokenAlert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblTokenAlert.AutoSize = true;
            this.lblTokenAlert.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTokenAlert.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.lblTokenAlert.Location = new System.Drawing.Point(192, 408);
            this.lblTokenAlert.Name = "lblTokenAlert";
            this.lblTokenAlert.Size = new System.Drawing.Size(573, 13);
            this.lblTokenAlert.TabIndex = 8;
            this.lblTokenAlert.Text = "Save your token key. You will not be able to read the token value after Rest API " +
    "Settings is closed.";
            this.lblTokenAlert.Visible = false;
            // 
            // chkLB
            // 
            this.chkLB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkLB.AutoSize = true;
            this.chkLB.Location = new System.Drawing.Point(19, 35);
            this.chkLB.Name = "chkLB";
            this.chkLB.Size = new System.Drawing.Size(136, 17);
            this.chkLB.TabIndex = 20;
            this.chkLB.Text = "Enable Load Balancing";
            this.chkLB.UseVisualStyleBackColor = true;
            this.chkLB.CheckedChanged += new System.EventHandler(this.LoadBalancingCheckedChanged);
            // 
            // APISettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 438);
            this.Controls.Add(this.chkLB);
            this.Controls.Add(this.lblTokenAlert);
            this.Controls.Add(this.chkActive);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.lblPort);
            this.Controls.Add(this.txtPort);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnAccept);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(800, 450);
            this.Name = "APISettings";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Rest API Settings";
            this.Load += new System.EventHandler(this.FormLoad);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.KeyDownEvent);
            ((System.ComponentModel.ISupportInitialize)(this.txtPort)).EndInit();
            this.tabControl.ResumeLayout(false);
            this.tabTokens.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridTokens)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ds)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtLayouts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtTokens)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtNodes)).EndInit();
            this.tabLB.ResumeLayout(false);
            this.pnlPortsContent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridNodes)).EndInit();
            this.pnlPortsUp.ResumeLayout(false);
            this.pnlPortsUp.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtNodesRestart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtOpRestart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtNodes)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnAccept;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.NumericUpDown txtPort;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabTokens;
        private System.Windows.Forms.CheckBox chkActive;
        private System.Data.DataSet ds;
        private System.Data.DataTable dtLayouts;
        private System.Data.DataColumn dataColumn1;
        private System.Data.DataColumn dataColumn2;
        private System.Data.DataColumn dataColumn3;
        private System.Data.DataColumn dataColumn4;
        private System.Data.DataTable dtTokens;
        private System.Data.DataColumn dataColumn5;
        private System.Data.DataColumn dataColumn6;
        private System.Data.DataColumn dataColumn7;
        private System.Windows.Forms.DataGridView gridTokens;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTokenName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTokenID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTokenKey;
        private System.Windows.Forms.Label lblTokenAlert;
        private System.Windows.Forms.TabPage tabLB;
        private System.Windows.Forms.DataGridView gridNodes;
        private System.Data.DataTable dtNodes;
        private System.Data.DataColumn colPort;
        private System.Windows.Forms.CheckBox chkLB;
        private System.Windows.Forms.Panel pnlPortsContent;
        private System.Windows.Forms.Panel pnlPortsUp;
        private System.Windows.Forms.NumericUpDown txtNodes;
        private System.Windows.Forms.Label lblNodes;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNodeID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPortDataGridViewTextBoxColumn;
        private System.Data.DataColumn dataColumn8;
        private System.Windows.Forms.TextBox txtRedisServer;
        private System.Windows.Forms.Label lblRedisServer;
        private System.Windows.Forms.NumericUpDown txtNodesRestart;
        private System.Windows.Forms.NumericUpDown txtOpRestart;
        private System.Windows.Forms.Label lblNodesRestart;
        private System.Windows.Forms.Label lblOpRestart;
        private System.Windows.Forms.Label lblNodesHours;
        private System.Windows.Forms.Label lblOpHours;
        private System.Windows.Forms.CheckBox chkRedisServer;
    }
}