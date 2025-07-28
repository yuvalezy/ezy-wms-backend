using System.Text.Json.Serialization;

namespace Adapters.CrossPlatform.SBO.Models;

public class PickListSboResponse {
    [JsonPropertyName("Absoluteentry")]
    public int AbsoluteEntry { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("OwnerCode")]
    public int OwnerCode { get; set; }

    [JsonPropertyName("OwnerName")]
    public string? OwnerName { get; set; }

    [JsonPropertyName("PickDate")]
    public DateTime PickDate { get; set; }

    [JsonPropertyName("Remarks")]
    public string? Remarks { get; set; }

    [JsonPropertyName("Status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("ObjectType")]
    public string ObjectType { get; set; } = string.Empty;

    [JsonPropertyName("UseBaseUnits")]
    public string UseBaseUnits { get; set; } = string.Empty;

    public PickListSboLine[] PickListsLines { get; set; }
}

public class PickListSboLine {
    [JsonPropertyName("AbsoluteEntry")]
    public int AbsoluteEntry { get; set; }

    [JsonPropertyName("LineNumber")]
    public int LineNumber { get; set; }

    [JsonPropertyName("OrderEntry")]
    public int OrderEntry { get; set; }

    [JsonPropertyName("OrderRowID")]
    public int OrderRowID { get; set; }

    [JsonPropertyName("PickedQuantity")]
    public double PickedQuantity { get; set; }

    [JsonPropertyName("PickStatus")]
    public string PickStatus { get; set; } = string.Empty;

    [JsonPropertyName("ReleasedQuantity")]
    public double ReleasedQuantity { get; set; }

    [JsonPropertyName("PreviouslyReleasedQuantity")]
    public double PreviouslyReleasedQuantity { get; set; }

    [JsonPropertyName("BaseObjectType")]
    public int BaseObjectType { get; set; }

    public ICollection<PickListLineSboBinAllocation> DocumentLinesBinAllocations { get; set; }
}

public class PickListLineSboBinAllocation {
    [JsonPropertyName("AllowNegativeQuantity")]
    public string AllowNegativeQuantity { get; set; } = "tNO";

    [JsonPropertyName("BaseLineNumber")]
    public int BaseLineNumber { get; set; }

    [JsonPropertyName("BinAbsEntry")]
    public int BinAbsEntry { get; set; }

    [JsonPropertyName("Quantity")]
    public double Quantity { get; set; }

    [JsonPropertyName("SerialAndBatchNumbersBaseLine")]
    public int SerialAndBatchNumbersBaseLine { get; set; } = -1;
}