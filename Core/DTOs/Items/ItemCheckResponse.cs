namespace Core.DTOs.Items;

public class ItemCheckResponse : ItemResponse {
    public List<string> Barcodes { get; set; } = [];
}