namespace Core.DTOs.Items;

public class ItemResponse {
    public string Code { get; set; }
    public string? Name { get; set; }
    public string? Father { get; set; }
    public int? BoxNumber { get; set; }

    public ItemResponse() {
    }

    public ItemResponse(string code) => Code = code;
}