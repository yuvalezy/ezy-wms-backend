namespace Service.Administration
{
    partial class Connection
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Connection));
            this.cmbType = new System.Windows.Forms.ComboBox();
            this.lblType = new System.Windows.Forms.Label();
            this.grpServer = new System.Windows.Forms.GroupBox();
            this.lblServer = new System.Windows.Forms.Label();
            this.txtServer = new System.Windows.Forms.TextBox();
            this.txtServerUser = new System.Windows.Forms.TextBox();
            this.lblUser = new System.Windows.Forms.Label();
            this.txtServerPassword = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.btnAccept = new System.Windows.Forms.Button();
            this.exit = new System.Windows.Forms.Button();
            this.grpServer.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmbType
            // 
            this.cmbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbType.FormattingEnabled = true;
            this.cmbType.Items.AddRange(new object[] {
            "HANA",
            "SQL 2012",
            "SQL 2014",
            "SQL 2016",
            "SQL 2017",
            "SQL 2019"});
            this.cmbType.Location = new System.Drawing.Point(165, 26);
            this.cmbType.Name = "cmbType";
            this.cmbType.Size = new System.Drawing.Size(493, 28);
            this.cmbType.TabIndex = 0;
            // 
            // lblType
            // 
            this.lblType.AutoSize = true;
            this.lblType.Location = new System.Drawing.Point(8, 31);
            this.lblType.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(43, 20);
            this.lblType.TabIndex = 14;
            this.lblType.Text = "Type";
            // 
            // grpServer
            // 
            this.grpServer.Controls.Add(this.cmbType);
            this.grpServer.Controls.Add(this.lblType);
            this.grpServer.Controls.Add(this.lblServer);
            this.grpServer.Controls.Add(this.txtServer);
            this.grpServer.Controls.Add(this.txtServerUser);
            this.grpServer.Controls.Add(this.lblUser);
            this.grpServer.Controls.Add(this.txtServerPassword);
            this.grpServer.Controls.Add(this.lblPassword);
            this.grpServer.Location = new System.Drawing.Point(15, 17);
            this.grpServer.Name = "grpServer";
            this.grpServer.Size = new System.Drawing.Size(687, 184);
            this.grpServer.TabIndex = 32;
            this.grpServer.TabStop = false;
            this.grpServer.Text = "Server Information";
            // 
            // lblServer
            // 
            this.lblServer.AutoSize = true;
            this.lblServer.Location = new System.Drawing.Point(8, 69);
            this.lblServer.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblServer.Name = "lblServer";
            this.lblServer.Size = new System.Drawing.Size(55, 20);
            this.lblServer.TabIndex = 14;
            this.lblServer.Text = "Server";
            // 
            // txtServer
            // 
            this.txtServer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtServer.Location = new System.Drawing.Point(165, 65);
            this.txtServer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtServer.Name = "txtServer";
            this.txtServer.Size = new System.Drawing.Size(493, 26);
            this.txtServer.TabIndex = 1;
            this.txtServer.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtKeyDown);
            // 
            // txtServerUser
            // 
            this.txtServerUser.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtServerUser.Location = new System.Drawing.Point(165, 103);
            this.txtServerUser.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtServerUser.Name = "txtServerUser";
            this.txtServerUser.Size = new System.Drawing.Size(493, 26);
            this.txtServerUser.TabIndex = 2;
            this.txtServerUser.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtKeyDown);
            // 
            // lblUser
            // 
            this.lblUser.AutoSize = true;
            this.lblUser.Location = new System.Drawing.Point(8, 106);
            this.lblUser.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblUser.Name = "lblUser";
            this.lblUser.Size = new System.Drawing.Size(43, 20);
            this.lblUser.TabIndex = 15;
            this.lblUser.Text = "User";
            // 
            // txtServerPassword
            // 
            this.txtServerPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtServerPassword.Location = new System.Drawing.Point(165, 140);
            this.txtServerPassword.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtServerPassword.Name = "txtServerPassword";
            this.txtServerPassword.PasswordChar = '*';
            this.txtServerPassword.Size = new System.Drawing.Size(493, 26);
            this.txtServerPassword.TabIndex = 3;
            this.txtServerPassword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtKeyDown);
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(8, 143);
            this.lblPassword.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(78, 20);
            this.lblPassword.TabIndex = 21;
            this.lblPassword.Text = "Password";
            // 
            // btnAccept
            // 
            this.btnAccept.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAccept.Location = new System.Drawing.Point(15, 209);
            this.btnAccept.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnAccept.Name = "btnAccept";
            this.btnAccept.Size = new System.Drawing.Size(112, 37);
            this.btnAccept.TabIndex = 30;
            this.btnAccept.Text = "&Accept";
            this.btnAccept.UseVisualStyleBackColor = true;
            this.btnAccept.Click += new System.EventHandler(this.btnAccept_Click);
            // 
            // exit
            // 
            this.exit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.exit.Location = new System.Drawing.Point(588, 211);
            this.exit.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.exit.Name = "exit";
            this.exit.Size = new System.Drawing.Size(112, 35);
            this.exit.TabIndex = 31;
            this.exit.Text = "E&xit";
            this.exit.UseVisualStyleBackColor = true;
            this.exit.Click += new System.EventHandler(this.exit_Click);
            // 
            // Connection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(717, 262);
            this.Controls.Add(this.btnAccept);
            this.Controls.Add(this.exit);
            this.Controls.Add(this.grpServer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.Name = "Connection";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Light WMS Service Connection Settings";
            this.Load += new System.EventHandler(this.frmConn_Load);
            this.grpServer.ResumeLayout(false);
            this.grpServer.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.Button btnAccept;
        private System.Windows.Forms.ComboBox cmbType;
        private System.Windows.Forms.Button exit;
        private System.Windows.Forms.GroupBox grpServer;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.Label lblServer;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.Label lblUser;
        private System.Windows.Forms.TextBox txtServer;
        private System.Windows.Forms.TextBox txtServerPassword;
        private System.Windows.Forms.TextBox txtServerUser;

        #endregion
    }
}