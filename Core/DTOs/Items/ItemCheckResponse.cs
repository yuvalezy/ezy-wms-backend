namespace Core.DTOs.Items;

public class ItemCheckResponse : ItemResponse {
    public List<string> Barcodes { get; set; } = [];
    public bool IsBatchManaged { get; set; }
    public bool IsSerialManaged { get; set; }
}