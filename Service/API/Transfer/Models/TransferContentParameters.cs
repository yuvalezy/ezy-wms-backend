using Service.Shared;

namespace Service.API.Transfer;

public class TransferContentParameters {
    public int          ID       { get; set; }
    public int          BinEntry { get; set; }
    public SourceTarget Type     { get; set; }
    public bool         Open     { get; set; }
}