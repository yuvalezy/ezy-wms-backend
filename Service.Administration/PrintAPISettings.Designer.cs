namespace Service.Administration {
    partial class PrintAPISettings {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PrintAPISettings));
            this.btnAccept = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.cmbDefaultPrinter = new System.Windows.Forms.ComboBox();
            this.lblDefaultPrinter = new System.Windows.Forms.Label();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPrinters = new System.Windows.Forms.TabPage();
            this.chkPrinters = new System.Windows.Forms.CheckedListBox();
            this.tabObjects = new System.Windows.Forms.TabPage();
            this.gridLayouts = new System.Windows.Forms.DataGridView();
            this.colLayoutName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colActive = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colPrinter = new System.Windows.Forms.DataGridViewComboBoxColumn();
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
            this.lblLayouts = new System.Windows.Forms.Label();
            this.cmbObjectDefaultPrinter = new System.Windows.Forms.ComboBox();
            this.lblObjectDefaultPrinter = new System.Windows.Forms.Label();
            this.cmbObject = new System.Windows.Forms.ComboBox();
            this.lblObject = new System.Windows.Forms.Label();
            this.tabControl.SuspendLayout();
            this.tabPrinters.SuspendLayout();
            this.tabObjects.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridLayouts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ds)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtLayouts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtTokens)).BeginInit();
            this.SuspendLayout();
            // 
            // btnAccept
            // 
            this.btnAccept.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAccept.Location = new System.Drawing.Point(18, 578);
            this.btnAccept.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnAccept.Name = "btnAccept";
            this.btnAccept.Size = new System.Drawing.Size(112, 35);
            this.btnAccept.TabIndex = 0;
            this.btnAccept.Text = "&Accept";
            this.btnAccept.UseVisualStyleBackColor = true;
            this.btnAccept.Click += new System.EventHandler(this.AcceptClicked);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(140, 578);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(112, 35);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // cmbDefaultPrinter
            // 
            this.cmbDefaultPrinter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDefaultPrinter.FormattingEnabled = true;
            this.cmbDefaultPrinter.Location = new System.Drawing.Point(190, 18);
            this.cmbDefaultPrinter.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cmbDefaultPrinter.Name = "cmbDefaultPrinter";
            this.cmbDefaultPrinter.Size = new System.Drawing.Size(386, 28);
            this.cmbDefaultPrinter.TabIndex = 5;
            // 
            // lblDefaultPrinter
            // 
            this.lblDefaultPrinter.AutoSize = true;
            this.lblDefaultPrinter.Location = new System.Drawing.Point(12, 23);
            this.lblDefaultPrinter.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDefaultPrinter.Name = "lblDefaultPrinter";
            this.lblDefaultPrinter.Size = new System.Drawing.Size(111, 20);
            this.lblDefaultPrinter.TabIndex = 6;
            this.lblDefaultPrinter.Text = "Default Printer";
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tabPrinters);
            this.tabControl.Controls.Add(this.tabObjects);
            this.tabControl.Location = new System.Drawing.Point(16, 60);
            this.tabControl.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1140, 509);
            this.tabControl.TabIndex = 7;
            // 
            // tabPrinters
            // 
            this.tabPrinters.Controls.Add(this.chkPrinters);
            this.tabPrinters.Location = new System.Drawing.Point(4, 29);
            this.tabPrinters.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabPrinters.Name = "tabPrinters";
            this.tabPrinters.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabPrinters.Size = new System.Drawing.Size(1132, 476);
            this.tabPrinters.TabIndex = 0;
            this.tabPrinters.Text = "Printers";
            this.tabPrinters.UseVisualStyleBackColor = true;
            // 
            // chkPrinters
            // 
            this.chkPrinters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkPrinters.FormattingEnabled = true;
            this.chkPrinters.Location = new System.Drawing.Point(4, 5);
            this.chkPrinters.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.chkPrinters.Name = "chkPrinters";
            this.chkPrinters.Size = new System.Drawing.Size(1124, 466);
            this.chkPrinters.TabIndex = 0;
            this.chkPrinters.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.chkPrinters_ItemCheck);
            // 
            // tabObjects
            // 
            this.tabObjects.Controls.Add(this.gridLayouts);
            this.tabObjects.Controls.Add(this.lblLayouts);
            this.tabObjects.Controls.Add(this.cmbObjectDefaultPrinter);
            this.tabObjects.Controls.Add(this.lblObjectDefaultPrinter);
            this.tabObjects.Controls.Add(this.cmbObject);
            this.tabObjects.Controls.Add(this.lblObject);
            this.tabObjects.Location = new System.Drawing.Point(4, 29);
            this.tabObjects.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabObjects.Name = "tabObjects";
            this.tabObjects.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabObjects.Size = new System.Drawing.Size(1132, 476);
            this.tabObjects.TabIndex = 2;
            this.tabObjects.Text = "Objects";
            this.tabObjects.UseVisualStyleBackColor = true;
            // 
            // gridLayouts
            // 
            this.gridLayouts.AllowUserToAddRows = false;
            this.gridLayouts.AllowUserToDeleteRows = false;
            this.gridLayouts.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridLayouts.AutoGenerateColumns = false;
            this.gridLayouts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridLayouts.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colLayoutName,
            this.colActive,
            this.colPrinter});
            this.gridLayouts.DataMember = "Layouts";
            this.gridLayouts.DataSource = this.ds;
            this.gridLayouts.Location = new System.Drawing.Point(166, 92);
            this.gridLayouts.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.gridLayouts.Name = "gridLayouts";
            this.gridLayouts.RowHeadersWidth = 62;
            this.gridLayouts.Size = new System.Drawing.Size(933, 308);
            this.gridLayouts.TabIndex = 5;
            this.gridLayouts.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridLayouts_CellValueChanged);
            this.gridLayouts.CurrentCellDirtyStateChanged += new System.EventHandler(this.gridLayouts_CurrentCellDirtyStateChanged);
            // 
            // colLayoutName
            // 
            this.colLayoutName.DataPropertyName = "Name";
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.colLayoutName.DefaultCellStyle = dataGridViewCellStyle1;
            this.colLayoutName.HeaderText = "Name";
            this.colLayoutName.MinimumWidth = 8;
            this.colLayoutName.Name = "colLayoutName";
            this.colLayoutName.ReadOnly = true;
            this.colLayoutName.Width = 200;
            // 
            // colActive
            // 
            this.colActive.DataPropertyName = "Active";
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.ControlLight;
            dataGridViewCellStyle2.NullValue = false;
            this.colActive.DefaultCellStyle = dataGridViewCellStyle2;
            this.colActive.FalseValue = "false";
            this.colActive.HeaderText = "Active";
            this.colActive.MinimumWidth = 8;
            this.colActive.Name = "colActive";
            this.colActive.ReadOnly = true;
            this.colActive.TrueValue = "true";
            this.colActive.Width = 60;
            // 
            // colPrinter
            // 
            this.colPrinter.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colPrinter.DataPropertyName = "Printer";
            this.colPrinter.HeaderText = "Default Printer";
            this.colPrinter.MinimumWidth = 8;
            this.colPrinter.Name = "colPrinter";
            this.colPrinter.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colPrinter.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // ds
            // 
            this.ds.DataSetName = "NewDataSet";
            this.ds.Tables.AddRange(new System.Data.DataTable[] {
            this.dtLayouts,
            this.dtTokens});
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
            // lblLayouts
            // 
            this.lblLayouts.AutoSize = true;
            this.lblLayouts.Location = new System.Drawing.Point(9, 92);
            this.lblLayouts.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblLayouts.Name = "lblLayouts";
            this.lblLayouts.Size = new System.Drawing.Size(65, 20);
            this.lblLayouts.TabIndex = 4;
            this.lblLayouts.Text = "Layouts";
            // 
            // cmbObjectDefaultPrinter
            // 
            this.cmbObjectDefaultPrinter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbObjectDefaultPrinter.FormattingEnabled = true;
            this.cmbObjectDefaultPrinter.Location = new System.Drawing.Point(166, 51);
            this.cmbObjectDefaultPrinter.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cmbObjectDefaultPrinter.Name = "cmbObjectDefaultPrinter";
            this.cmbObjectDefaultPrinter.Size = new System.Drawing.Size(388, 28);
            this.cmbObjectDefaultPrinter.TabIndex = 3;
            this.cmbObjectDefaultPrinter.SelectedIndexChanged += new System.EventHandler(this.cmbObjectDefaultPrinter_SelectedIndexChanged);
            // 
            // lblObjectDefaultPrinter
            // 
            this.lblObjectDefaultPrinter.AutoSize = true;
            this.lblObjectDefaultPrinter.Location = new System.Drawing.Point(9, 55);
            this.lblObjectDefaultPrinter.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblObjectDefaultPrinter.Name = "lblObjectDefaultPrinter";
            this.lblObjectDefaultPrinter.Size = new System.Drawing.Size(111, 20);
            this.lblObjectDefaultPrinter.TabIndex = 2;
            this.lblObjectDefaultPrinter.Text = "Default Printer";
            // 
            // cmbObject
            // 
            this.cmbObject.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbObject.FormattingEnabled = true;
            this.cmbObject.Location = new System.Drawing.Point(166, 9);
            this.cmbObject.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cmbObject.Name = "cmbObject";
            this.cmbObject.Size = new System.Drawing.Size(388, 28);
            this.cmbObject.TabIndex = 1;
            this.cmbObject.SelectedIndexChanged += new System.EventHandler(this.cmbObject_SelectedIndexChanged);
            // 
            // lblObject
            // 
            this.lblObject.AutoSize = true;
            this.lblObject.Location = new System.Drawing.Point(9, 14);
            this.lblObject.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblObject.Name = "lblObject";
            this.lblObject.Size = new System.Drawing.Size(55, 20);
            this.lblObject.TabIndex = 0;
            this.lblObject.Text = "Object";
            // 
            // PrintAPISettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1176, 632);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.lblDefaultPrinter);
            this.Controls.Add(this.cmbDefaultPrinter);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnAccept);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(1189, 662);
            this.Name = "PrintAPISettings";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Rest API Settings";
            this.Load += new System.EventHandler(this.FormLoad);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.KeyDownEvent);
            this.tabControl.ResumeLayout(false);
            this.tabPrinters.ResumeLayout(false);
            this.tabObjects.ResumeLayout(false);
            this.tabObjects.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridLayouts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ds)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtLayouts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtTokens)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnAccept;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ComboBox cmbDefaultPrinter;
        private System.Windows.Forms.Label lblDefaultPrinter;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPrinters;
        private System.Windows.Forms.CheckedListBox chkPrinters;
        private System.Windows.Forms.TabPage tabObjects;
        private System.Windows.Forms.ComboBox cmbObjectDefaultPrinter;
        private System.Windows.Forms.Label lblObjectDefaultPrinter;
        private System.Windows.Forms.ComboBox cmbObject;
        private System.Windows.Forms.Label lblObject;
        private System.Windows.Forms.DataGridView gridLayouts;
        private System.Windows.Forms.Label lblLayouts;
        private System.Data.DataSet ds;
        private System.Data.DataTable dtLayouts;
        private System.Data.DataColumn dataColumn1;
        private System.Data.DataColumn dataColumn2;
        private System.Data.DataColumn dataColumn3;
        private System.Data.DataColumn dataColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLayoutName;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colActive;
        private System.Windows.Forms.DataGridViewComboBoxColumn colPrinter;
        private System.Data.DataTable dtTokens;
        private System.Data.DataColumn dataColumn5;
        private System.Data.DataColumn dataColumn6;
        private System.Data.DataColumn dataColumn7;
    }
}