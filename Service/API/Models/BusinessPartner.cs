namespace Service.API.Models;

public class BusinessPartner {

    public string Code { get; set; }
    public string Name { get; set; }

    public BusinessPartner() {
        
    }
    public BusinessPartner(string code, string name) {
        Code = code;
        Name = name;
    }
}