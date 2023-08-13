namespace Service.Shared; 

public class AccessToken {
    public string ID       { get; set; }
    public string Name     { get; set; }
    public string Password { get; set; }

    public AccessToken(string id, string name, string password) {
        ID       = id;
        Name     = name;
        Password = password;
    }
}