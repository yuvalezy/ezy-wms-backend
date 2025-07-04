using Core.DTOs.General;

namespace Core.DTOs.Items;

public class UpdateItemBarCodeResponse : ResponseBase {
    public string? ExistItem { get; set; }
}