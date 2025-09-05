using Core.DTOs.Items;

namespace Core.DTOs.PickList;

public class PickingDetailResponse : IResponseCustomFields {
    public int PickEntry { get; set; }
    public int Type { get; set; }
    public int Entry { get; set; }
    public int Number { get; set; }
    public DateTime Date { get; set; }
    public string CardCode { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int TotalOpenItems { get; set; }
    public Dictionary<string, object> CustomFields { get; set; } = new();
}