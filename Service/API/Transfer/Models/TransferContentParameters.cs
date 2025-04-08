using Service.Shared;

namespace Service.API.Transfer.Models;

public class TransferContentParameters {
    public int          ID                { get; set; }
    public int          BinEntry          { get; set; }
    public string       BinCode           { get; set; }
    public SourceTarget Type              { get; set; }
    public bool         TargetBins        { get; set; }
    public bool         TargetBinQuantity { get; set; }
    public string       ItemCode   { get; set; }
}