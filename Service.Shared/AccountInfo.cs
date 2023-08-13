namespace Service.Shared;

public class AccountInfo {
    public AccountType Type     { get; set; }
    public string      UserName { get; set; }
    public string      Password { get; set; }
}

public enum AccountType {
    LocalSystem,
    Account
}