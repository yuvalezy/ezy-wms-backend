namespace Core.DTOs.Items;

public class BinContentResponse : ItemResponse {
    public          double OnHand  { get; set; }
    public required string BinCode { get; set; }
}