namespace Core.DTOs;

public class HomeInfoResponse {
    public int ItemCheck           { get; set; }
    public int BinCheck            { get; set; }
    public int GoodsReceipt        { get; set; }
    public int ReceiptConfirmation { get; set; }
    public int Picking             { get; set; }
    public int Counting            { get; set; }
    public int Transfers           { get; set; }
}