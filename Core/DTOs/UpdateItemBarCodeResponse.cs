using Core.Models;

namespace Core.DTOs;

public class UpdateItemBarCodeResponse : ResponseBase {
    public string? ExistItem { get; set; }
}