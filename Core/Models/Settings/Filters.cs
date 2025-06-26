namespace Core.Models.Settings;

public class Filters {
    public string?      Vendors                 { get; set; }
    public FilterQuery? PickPackOnly            { get; set; }
    public string?      PickReady               { get; set; }
    public int?         InitialCountingBinEntry { get; set; }
    public int          CancelPickingEntry      { get; set; }
}

public class FilterQuery {
    public required string Query   { get; set; }
    public required string GroupBy { get; set; }
}