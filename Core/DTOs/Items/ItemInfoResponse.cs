namespace Core.DTOs.Items;

public class ItemInfoResponse(string code, bool isPackage = false) {
    public string  Code      { get; set; } = code;
    public string? Name      { get; set; }
    public string? Father    { get; set; }
    public bool    IsPackage { get; set; } = isPackage;
}