namespace Core.Models;

public class ProcessPickListResult {
    public bool Success { get; set; }
    public int? DocumentNumber { get; set; }
    public string? ErrorMessage { get; set; }
}