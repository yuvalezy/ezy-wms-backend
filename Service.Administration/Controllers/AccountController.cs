using System;
using Service.Administration.Views;
using System.DirectoryServices.AccountManagement;
using System.Windows.Forms;
using Service.Shared;

namespace Service.Administration.Controllers;

public class AccountController {
    private const    string       MockPassword = "**********";
    private readonly IWin32Window owner;
    private readonly IAccount     view;
    private readonly string       currentAccount;

    private bool loaded;

    public AccountController(IWin32Window owner, IAccount view, string currentAccount) {
        this.owner          = owner;
        this.view           = view;
        this.currentAccount = currentAccount;
    }

    public void Load() {
        view.AccountType = currentAccount == "LocalSystem" ? AccountType.LocalSystem : AccountType.Account;
        loaded           = true;
        if (view.AccountType != AccountType.Account)
            return;
        view.UserName = currentAccount;
        view.Password = MockPassword;
    }

    public void ChangeType(AccountType type) {
        if (!loaded)
            return;
        loaded           = false;
        view.AccountType = type;
        loaded           = true;
        if (type == AccountType.Account)
            view.FocusUserName();
    }

    public void ClearMockPassword() {
        if (view.Password == MockPassword)
            view.Password = string.Empty;
    }

    public void Accept() {
        if (!Validate())
            return;
        string account = view.UserName;
        if (account.StartsWith(".\\"))
            account = $"{Environment.MachineName}\\{account.Substring(2)}";
        view.Close();
        view.AccountChanged?.Invoke(view.AccountType, account, view.Password);
    }

    private bool Validate() => view.AccountType == AccountType.LocalSystem || ValidateAccount();

    private bool ValidateAccount() {
        const string title = "Account Validation";
        if (string.IsNullOrWhiteSpace(view.UserName)) {
            MessageBox.Show(owner, "You must enter the account user name", title, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            view.FocusUserName();
            return false;
        }

        if (string.IsNullOrWhiteSpace(view.Password) || view.Password == MockPassword) {
            if (view.Password == MockPassword)
                view.Password = string.Empty;
            MessageBox.Show(owner, "You must enter the account password", title, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            view.FocusPassword();
            return false;
        }

        var context = new PrincipalContext(ContextType.Machine, Environment.MachineName);
        if (context.ValidateCredentials(view.UserName, view.Password))
            return true;
        MessageBox.Show(owner, "Invalid User Name or Password!", title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }
}