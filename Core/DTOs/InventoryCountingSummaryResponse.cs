namespace Core.DTOs;

public class InventoryCountingSummaryResponse {
    public Guid CountingId { get; set; }
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string WhsCode { get; set; } = string.Empty;
    public int TotalLines { get; set; }
    public int ProcessedLines { get; set; }
    public int VarianceLines { get; set; }
    public decimal TotalSystemValue { get; set; }
    public decimal TotalCountedValue { get; set; }
    public decimal TotalVarianceValue { get; set; }
}