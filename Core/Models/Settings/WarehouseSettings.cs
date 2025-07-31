namespace Core.Models.Settings;

public class WarehouseSettings {
    public int? InitialCountingBinEntry { get; set; }
    public int CancelPickingBinEntry { get; set; }
    public int? StagingBinEntry { get; set; }
}