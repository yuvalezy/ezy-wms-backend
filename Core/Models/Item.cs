namespace Core.Models;

public class Item {
    public string Code { get; set; }
    public string? Name { get; set; }
    public string? Father { get; set; }
    public int? BoxNumber { get; set; }

    public Item() {
    }

    public Item(string code) => Code = code;
}