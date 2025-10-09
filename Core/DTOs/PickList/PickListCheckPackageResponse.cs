using Core.Enums;
using Core.Models;
using Core.DTOs.General;

namespace Core.DTOs.PickList;

public class PickListCheckPackageResponse : ResponseBase {
    public bool Success { get; set; }
    public int ItemsChecked { get; set; }
    public int TotalItems { get; set; }
    public string PackageBarcode { get; set; } = string.Empty;
    public List<CheckedPackageItem> CheckedItems { get; set; } = new();
}

public class CheckedPackageItem {
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}