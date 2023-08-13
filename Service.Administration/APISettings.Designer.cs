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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(APISettings));
            this.btnAccept = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.txtPort = new System.Windows.Forms.NumericUpDown();
            this.lblPort = new System.Windows.Forms.Label();
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
            this.chkActive = new System.Windows.Forms.CheckBox();
            this.chkLB = new System.Windows.Forms.CheckBox();
            this.tabLB = new System.Windows.Forms.TabPage();
            this.pnlPortsUp = new System.Windows.Forms.Panel();
            this.lblNodes = new System.Windows.Forms.Label();
            this.lblRedisServer = new System.Windows.Forms.Label();
            this.lblOpRestart = new System.Windows.Forms.Label();
            this.lblOpHours = new System.Windows.Forms.Label();
            this.lblNodesHours = new System.Windows.Forms.Label();
            this.txtNodes = new System.Windows.Forms.NumericUpDown();
            this.lblNodesRestart = new System.Windows.Forms.Label();
            this.txtOpRestart = new System.Windows.Forms.NumericUpDown();
            this.txtNodesRestart = new System.Windows.Forms.NumericUpDown();
            this.txtRedisServer = new System.Windows.Forms.TextBox();
            this.chkRedisServer = new System.Windows.Forms.CheckBox();
            this.pnlPortsContent = new System.Windows.Forms.Panel();
            this.gridNodes = new System.Windows.Forms.DataGridView();
            this.colPortDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNodeID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabControl = new System.Windows.Forms.TabControl();
            ((System.ComponentModel.ISupportInitialize)(this.txtPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ds)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtLayouts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtTokens)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtNodes)).BeginInit();
            this.tabLB.SuspendLayout();
            this.pnlPortsUp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtNodes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtOpRestart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtNodesRestart)).BeginInit();
            this.pnlPortsContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridNodes)).BeginInit();
            this.tabControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnAccept
            // 
            this.btnAccept.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAccept.Location = new System.Drawing.Point(18, 620);
            this.btnAccept.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnAccept.Name = "btnAccept";
            this.btnAccept.Size = new System.Drawing.Size(112, 35);
            this.btnAccept.TabIndex = 90;
            this.btnAccept.Text = "&Accept";
            this.btnAccept.UseVisualStyleBackColor = true;
            this.btnAccept.Click += new System.EventHandler(this.AcceptClicked);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(140, 620);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(112, 35);
            this.btnCancel.TabIndex = 100;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(972, 18);
            this.txtPort.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            this.txtPort.Size = new System.Drawing.Size(180, 26);
            this.txtPort.TabIndex = 10;
            this.txtPort.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtPort.Value = new decimal(new int[] {
            8000,
            0,
            0,
            0});
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(794, 25);
            this.lblPort.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(88, 20);
            this.lblPort.TabIndex = 4;
            this.lblPort.Text = "Server Port";
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
            // chkActive
            // 
            this.chkActive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkActive.AutoSize = true;
            this.chkActive.Location = new System.Drawing.Point(34, 18);
            this.chkActive.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.chkActive.Name = "chkActive";
            this.chkActive.Size = new System.Drawing.Size(78, 24);
            this.chkActive.TabIndex = 0;
            this.chkActive.Text = "Active";
            this.chkActive.UseVisualStyleBackColor = true;
            // 
            // chkLB
            // 
            this.chkLB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkLB.AutoSize = true;
            this.chkLB.Location = new System.Drawing.Point(33, 54);
            this.chkLB.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.chkLB.Name = "chkLB";
            this.chkLB.Size = new System.Drawing.Size(199, 24);
            this.chkLB.TabIndex = 20;
            this.chkLB.Text = "Enable Load Balancing";
            this.chkLB.UseVisualStyleBackColor = true;
            this.chkLB.CheckedChanged += new System.EventHandler(this.LoadBalancingCheckedChanged);
            // 
            // tabLB
            // 
            this.tabLB.Controls.Add(this.pnlPortsContent);
            this.tabLB.Controls.Add(this.pnlPortsUp);
            this.tabLB.Location = new System.Drawing.Point(4, 29);
            this.tabLB.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabLB.Name = "tabLB";
            this.tabLB.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabLB.Size = new System.Drawing.Size(1132, 475);
            this.tabLB.TabIndex = 2;
            this.tabLB.Text = "Load Balancing";
            this.tabLB.UseVisualStyleBackColor = true;
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
            this.pnlPortsUp.Location = new System.Drawing.Point(4, 5);
            this.pnlPortsUp.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pnlPortsUp.Name = "pnlPortsUp";
            this.pnlPortsUp.Size = new System.Drawing.Size(1124, 154);
            this.pnlPortsUp.TabIndex = 0;
            // 
            // lblNodes
            // 
            this.lblNodes.AutoSize = true;
            this.lblNodes.Location = new System.Drawing.Point(6, 95);
            this.lblNodes.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblNodes.Name = "lblNodes";
            this.lblNodes.Size = new System.Drawing.Size(55, 20);
            this.lblNodes.TabIndex = 4;
            this.lblNodes.Text = "Nodes";
            // 
            // lblRedisServer
            // 
            this.lblRedisServer.AutoSize = true;
            this.lblRedisServer.Location = new System.Drawing.Point(6, 55);
            this.lblRedisServer.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblRedisServer.Name = "lblRedisServer";
            this.lblRedisServer.Size = new System.Drawing.Size(100, 20);
            this.lblRedisServer.TabIndex = 4;
            this.lblRedisServer.Text = "Redis Server";
            // 
            // lblOpRestart
            // 
            this.lblOpRestart.AutoSize = true;
            this.lblOpRestart.Location = new System.Drawing.Point(568, 15);
            this.lblOpRestart.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblOpRestart.Name = "lblOpRestart";
            this.lblOpRestart.Size = new System.Drawing.Size(144, 20);
            this.lblOpRestart.TabIndex = 4;
            this.lblOpRestart.Text = "Operations Restart";
            // 
            // lblOpHours
            // 
            this.lblOpHours.AutoSize = true;
            this.lblOpHours.Location = new System.Drawing.Point(908, 12);
            this.lblOpHours.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblOpHours.Name = "lblOpHours";
            this.lblOpHours.Size = new System.Drawing.Size(62, 20);
            this.lblOpHours.TabIndex = 4;
            this.lblOpHours.Text = "(Hours)";
            // 
            // lblNodesHours
            // 
            this.lblNodesHours.AutoSize = true;
            this.lblNodesHours.Location = new System.Drawing.Point(908, 52);
            this.lblNodesHours.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblNodesHours.Name = "lblNodesHours";
            this.lblNodesHours.Size = new System.Drawing.Size(62, 20);
            this.lblNodesHours.TabIndex = 4;
            this.lblNodesHours.Text = "(Hours)";
            // 
            // txtNodes
            // 
            this.txtNodes.Location = new System.Drawing.Point(117, 89);
            this.txtNodes.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            this.txtNodes.Size = new System.Drawing.Size(180, 26);
            this.txtNodes.TabIndex = 60;
            this.txtNodes.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtNodes.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.txtNodes.ValueChanged += new System.EventHandler(this.NodesValueChanged);
            // 
            // lblNodesRestart
            // 
            this.lblNodesRestart.AutoSize = true;
            this.lblNodesRestart.Location = new System.Drawing.Point(568, 55);
            this.lblNodesRestart.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblNodesRestart.Name = "lblNodesRestart";
            this.lblNodesRestart.Size = new System.Drawing.Size(112, 20);
            this.lblNodesRestart.TabIndex = 4;
            this.lblNodesRestart.Text = "Nodes Restart";
            // 
            // txtOpRestart
            // 
            this.txtOpRestart.Location = new System.Drawing.Point(718, 9);
            this.txtOpRestart.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            this.txtOpRestart.Size = new System.Drawing.Size(180, 26);
            this.txtOpRestart.TabIndex = 70;
            this.txtOpRestart.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtOpRestart.Value = new decimal(new int[] {
            24,
            0,
            0,
            0});
            this.txtOpRestart.ValueChanged += new System.EventHandler(this.NodesValueChanged);
            // 
            // txtNodesRestart
            // 
            this.txtNodesRestart.Location = new System.Drawing.Point(718, 49);
            this.txtNodesRestart.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            this.txtNodesRestart.Size = new System.Drawing.Size(180, 26);
            this.txtNodesRestart.TabIndex = 80;
            this.txtNodesRestart.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtNodesRestart.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this.txtNodesRestart.ValueChanged += new System.EventHandler(this.NodesValueChanged);
            // 
            // txtRedisServer
            // 
            this.txtRedisServer.Location = new System.Drawing.Point(117, 49);
            this.txtRedisServer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtRedisServer.Name = "txtRedisServer";
            this.txtRedisServer.Size = new System.Drawing.Size(414, 26);
            this.txtRedisServer.TabIndex = 50;
            // 
            // chkRedisServer
            // 
            this.chkRedisServer.AutoSize = true;
            this.chkRedisServer.Location = new System.Drawing.Point(10, 15);
            this.chkRedisServer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.chkRedisServer.Name = "chkRedisServer";
            this.chkRedisServer.Size = new System.Drawing.Size(180, 24);
            this.chkRedisServer.TabIndex = 81;
            this.chkRedisServer.Text = "Enable Redis Server";
            this.chkRedisServer.UseVisualStyleBackColor = true;
            this.chkRedisServer.CheckedChanged += new System.EventHandler(this.EnableRedisChanged);
            // 
            // pnlPortsContent
            // 
            this.pnlPortsContent.Controls.Add(this.gridNodes);
            this.pnlPortsContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPortsContent.Location = new System.Drawing.Point(4, 159);
            this.pnlPortsContent.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pnlPortsContent.Name = "pnlPortsContent";
            this.pnlPortsContent.Size = new System.Drawing.Size(1124, 311);
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
            this.gridNodes.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.gridNodes.Name = "gridNodes";
            this.gridNodes.RowHeadersWidth = 62;
            this.gridNodes.Size = new System.Drawing.Size(1124, 311);
            this.gridNodes.TabIndex = 8;
            // 
            // colPortDataGridViewTextBoxColumn
            // 
            this.colPortDataGridViewTextBoxColumn.DataPropertyName = "Port";
            this.colPortDataGridViewTextBoxColumn.HeaderText = "Port";
            this.colPortDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.colPortDataGridViewTextBoxColumn.Name = "colPortDataGridViewTextBoxColumn";
            this.colPortDataGridViewTextBoxColumn.Width = 150;
            // 
            // colNodeID
            // 
            this.colNodeID.DataPropertyName = "ID";
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.colNodeID.DefaultCellStyle = dataGridViewCellStyle1;
            this.colNodeID.FillWeight = 40F;
            this.colNodeID.HeaderText = "#";
            this.colNodeID.MinimumWidth = 8;
            this.colNodeID.Name = "colNodeID";
            this.colNodeID.ReadOnly = true;
            this.colNodeID.Width = 150;
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tabLB);
            this.tabControl.Location = new System.Drawing.Point(18, 103);
            this.tabControl.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1140, 508);
            this.tabControl.TabIndex = 7;
            // 
            // APISettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1176, 674);
            this.Controls.Add(this.chkLB);
            this.Controls.Add(this.chkActive);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.lblPort);
            this.Controls.Add(this.txtPort);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnAccept);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(1189, 662);
            this.Name = "APISettings";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Rest API Settings";
            this.Load += new System.EventHandler(this.FormLoad);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.KeyDownEvent);
            ((System.ComponentModel.ISupportInitialize)(this.txtPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ds)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtLayouts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtTokens)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtNodes)).EndInit();
            this.tabLB.ResumeLayout(false);
            this.pnlPortsUp.ResumeLayout(false);
            this.pnlPortsUp.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtNodes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtOpRestart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtNodesRestart)).EndInit();
            this.pnlPortsContent.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridNodes)).EndInit();
            this.tabControl.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnAccept;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.NumericUpDown txtPort;
        private System.Windows.Forms.Label lblPort;
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
        private System.Data.DataTable dtNodes;
        private System.Data.DataColumn colPort;
        private System.Windows.Forms.CheckBox chkLB;
        private System.Data.DataColumn dataColumn8;
        private System.Windows.Forms.TabPage tabLB;
        private System.Windows.Forms.Panel pnlPortsContent;
        private System.Windows.Forms.DataGridView gridNodes;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNodeID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPortDataGridViewTextBoxColumn;
        private System.Windows.Forms.Panel pnlPortsUp;
        private System.Windows.Forms.CheckBox chkRedisServer;
        private System.Windows.Forms.TextBox txtRedisServer;
        private System.Windows.Forms.NumericUpDown txtNodesRestart;
        private System.Windows.Forms.NumericUpDown txtOpRestart;
        private System.Windows.Forms.Label lblNodesRestart;
        private System.Windows.Forms.NumericUpDown txtNodes;
        private System.Windows.Forms.Label lblNodesHours;
        private System.Windows.Forms.Label lblOpHours;
        private System.Windows.Forms.Label lblOpRestart;
        private System.Windows.Forms.Label lblRedisServer;
        private System.Windows.Forms.Label lblNodes;
        private System.Windows.Forms.TabControl tabControl;
    }
}