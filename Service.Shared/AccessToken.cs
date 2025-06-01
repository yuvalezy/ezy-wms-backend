namespace Service.Shared; 

public class AccessToken(string id, string name, string password) {
    public string ID       { get; set; } = id;
    public string Name     { get; set; } = name;
    public string Password { get; set; } = password;
}