namespace Core.DTOs.GoodsReceipt;
public class GoodsReceiptCreationResponse {
    public  string? CardCode { get; set; }
    private int     Type     { get; set; }
    private int     Entry    { get; set; }
}

public class GoodsReceiptCreationDataResponse {
    public string                               ItemCode    { get; set; } = string.Empty;
    public string                               BarCode     { get; set; } = string.Empty;
    public decimal                              Quantity    { get; set; }
    public bool                                 UseBaseUnit { get; set; }
    public DateTime                             Date        { get; set; }
    public string?                              Comments    { get; set; }
    public List<GoodsReceiptSourceDataResponse> Sources     { get; set; } = new();
}

public class GoodsReceiptSourceDataResponse {
    public int     SourceType  { get; set; }
    public int     SourceEntry { get; set; }
    public int     SourceLine  { get; set; }
    public decimal Quantity    { get; set; }
}