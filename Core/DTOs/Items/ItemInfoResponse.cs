namespace Core.DTOs.Items;

public class ItemInfoResponse(string code) {
    public string  Code   { get; set; } = code;
    public string? Name   { get; set; }
    public string? Father { get; set; }
}