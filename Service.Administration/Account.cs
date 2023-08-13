using System;
using System.Windows.Forms;
using Service.Shared;
using Service.Administration.Controllers;
using Service.Administration.Views;

namespace Service.Administration; 

public partial class Account : Form, IAccount {
    private readonly AccountController controller;

    private AccountType accountType;

    #region Implementation of IAccount

    public AccountType AccountType {
        get => accountType;
        set {
            accountType           = value;
            rdLocalSystem.Checked = value == AccountType.LocalSystem;
            rdAccount.Checked     = value == AccountType.Account;
            txtUserName.ReadOnly  = rdLocalSystem.Checked;
            txtPassword.ReadOnly  = rdLocalSystem.Checked;
        }
    }

    public string UserName {
        get => txtUserName.Text;
        set => txtUserName.Text = value;
    }

    public string Password {
        get => txtPassword.Text;
        set => txtPassword.Text = value;
    }

    public Action<AccountType, string, string> AccountChanged { get; set; }

    public void FocusUserName() => txtUserName.Focus();
    public void FocusPassword() => txtPassword.Focus();

    #endregion

    public Account(string db, string dbName, string currentAccount) {
        InitializeComponent();
        controller     = new AccountController(this, this, currentAccount);
        txtDB.Text     = db;
        txtDBName.Text = dbName;
    }

    private void FormLoaded(object sender, EventArgs e) => controller.Load();

    private void LocalSystemChecked(object sender, EventArgs e) => controller.ChangeType(AccountType.LocalSystem);

    private void AccountChecked(object sender, EventArgs e) => controller.ChangeType(AccountType.Account);

    private void AcceptClicked(object sender, EventArgs e) => controller.Accept();

    private void UserPasswordKeyDown(object sender, KeyEventArgs e) {
        if (e.KeyCode == Keys.Enter)
            controller.Accept();
    }


    private void PasswordEnter(object sender, EventArgs e) => controller.ClearMockPassword();
}