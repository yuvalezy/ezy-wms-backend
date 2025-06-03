using Core.Enums;

namespace Core.Models;

public class TransfersRequest {
    public DateTime?        Date     { get; set; }
    public ObjectStatus[]?  Status   { get; set; }
    public TransferOrderBy? OrderBy  { get; set; }
    public int?             ID       { get; set; }
    public int?             Number   { get; set; }
    public bool             Desc     { get; set; }
    public bool             Progress { get; set; }
}

public enum TransferOrderBy {
    ID,
    Date
}