using System;
using Service.Shared;

namespace Service.Administration.Views;

public interface IAccount {
    AccountType                         AccountType    { get; set; }
    string                              UserName       { get; set; }
    string                              Password       { get; set; }
    Action<AccountType, string, string> AccountChanged { get; set; }
    void                                FocusUserName();
    void                                FocusPassword();
    void                                Close();
}