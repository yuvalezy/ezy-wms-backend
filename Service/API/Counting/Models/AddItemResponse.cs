using Service.API.General;

namespace Service.API.Counting.Models;

public class AddItemResponse {
    public int?      LineID         { get; set; }
    public bool      ClosedCounting { get; set; }
    public string    ErrorMessage   { get; set; }
    public UnitType? Unit           { get; set; }
    public int?      NumIn          { get; set; }
    public string    UnitMsr        { get; set; }
    public int?      PackUnit       { get; set; }
    public string    PackMsr        { get; set; }
}