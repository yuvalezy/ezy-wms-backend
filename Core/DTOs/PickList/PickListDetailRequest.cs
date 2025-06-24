namespace Core.DTOs.PickList;

public class PickListDetailRequest {
    public int? Type { get; set; }
    public int? Entry { get; set; }
    public bool? AvailableBins { get; set; }
    public int? BinEntry { get; set; }
}