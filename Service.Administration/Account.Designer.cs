namespace Service.Administration {
    partial class Account {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Account));
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtDB = new System.Windows.Forms.TextBox();
            this.txtDBName = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.rdLocalSystem = new System.Windows.Forms.RadioButton();
            this.rdAccount = new System.Windows.Forms.RadioButton();
            this.txtUserName = new System.Windows.Forms.TextBox();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.btnAccept = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Database";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(82, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Company Name";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 66);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(31, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Type";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 90);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(60, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "User Name";
            // 
            // txtDB
            // 
            this.txtDB.Location = new System.Drawing.Point(110, 12);
            this.txtDB.Name = "txtDB";
            this.txtDB.ReadOnly = true;
            this.txtDB.Size = new System.Drawing.Size(279, 20);
            this.txtDB.TabIndex = 4;
            // 
            // txtDBName
            // 
            this.txtDBName.Location = new System.Drawing.Point(110, 38);
            this.txtDBName.Name = "txtDBName";
            this.txtDBName.ReadOnly = true;
            this.txtDBName.Size = new System.Drawing.Size(529, 20);
            this.txtDBName.TabIndex = 5;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 116);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(53, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "Password";
            // 
            // rdLocalSystem
            // 
            this.rdLocalSystem.AutoSize = true;
            this.rdLocalSystem.Location = new System.Drawing.Point(110, 64);
            this.rdLocalSystem.Name = "rdLocalSystem";
            this.rdLocalSystem.Size = new System.Drawing.Size(131, 17);
            this.rdLocalSystem.TabIndex = 7;
            this.rdLocalSystem.TabStop = true;
            this.rdLocalSystem.Text = "Local System Account";
            this.rdLocalSystem.UseVisualStyleBackColor = true;
            this.rdLocalSystem.CheckedChanged += new System.EventHandler(this.LocalSystemChecked);
            // 
            // rdAccount
            // 
            this.rdAccount.AutoSize = true;
            this.rdAccount.Location = new System.Drawing.Point(247, 64);
            this.rdAccount.Name = "rdAccount";
            this.rdAccount.Size = new System.Drawing.Size(106, 17);
            this.rdAccount.TabIndex = 8;
            this.rdAccount.TabStop = true;
            this.rdAccount.Text = "Specific Account";
            this.rdAccount.UseVisualStyleBackColor = true;
            this.rdAccount.CheckedChanged += new System.EventHandler(this.AccountChecked);
            // 
            // txtUserName
            // 
            this.txtUserName.Location = new System.Drawing.Point(110, 87);
            this.txtUserName.Name = "txtUserName";
            this.txtUserName.Size = new System.Drawing.Size(279, 20);
            this.txtUserName.TabIndex = 9;
            this.txtUserName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.UserPasswordKeyDown);
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(110, 113);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(279, 20);
            this.txtPassword.TabIndex = 10;
            this.txtPassword.Enter += new System.EventHandler(this.PasswordEnter);
            this.txtPassword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.UserPasswordKeyDown);
            // 
            // btnAccept
            // 
            this.btnAccept.Location = new System.Drawing.Point(12, 139);
            this.btnAccept.Name = "btnAccept";
            this.btnAccept.Size = new System.Drawing.Size(75, 23);
            this.btnAccept.TabIndex = 11;
            this.btnAccept.Text = "&Accept";
            this.btnAccept.UseVisualStyleBackColor = true;
            this.btnAccept.Click += new System.EventHandler(this.AcceptClicked);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(94, 139);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 12;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // Account
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(651, 175);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnAccept);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.txtUserName);
            this.Controls.Add(this.rdAccount);
            this.Controls.Add(this.rdLocalSystem);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtDBName);
            this.Controls.Add(this.txtDB);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Account";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Database Account";
            this.Load += new System.EventHandler(this.FormLoaded);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtDB;
        private System.Windows.Forms.TextBox txtDBName;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.RadioButton rdLocalSystem;
        private System.Windows.Forms.RadioButton rdAccount;
        private System.Windows.Forms.TextBox txtUserName;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Button btnAccept;
        private System.Windows.Forms.Button btnCancel;
    }
}