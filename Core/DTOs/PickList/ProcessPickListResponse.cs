namespace Core.DTOs.PickList;

public class ProcessPickListResponse : ResponseBase {
    public int?    DocumentNumber { get; set; }
    public string? ErrorMessage   { get; set; }
    public string? Message        { get; set; }
}

public class ProcessPickListCancelResponse : ProcessPickListResponse {
    public Guid? TransferId { get; set; }
}