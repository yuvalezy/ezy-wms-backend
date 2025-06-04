namespace Core.DTOs;

public class ProcessPickListResponse : ResponseBase {
    public int?    DocumentNumber { get; set; }
    public string? ErrorMessage   { get; set; }
    public string? Message        { get; set; }
}