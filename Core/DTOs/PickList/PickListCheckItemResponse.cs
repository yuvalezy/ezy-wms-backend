using Core.DTOs.General;

namespace Core.DTOs.PickList;

public class PickListCheckItemResponse : ResponseBase {
    public bool Success { get; set; }
    public int ItemsChecked { get; set; }
    public int TotalItems { get; set; }
}