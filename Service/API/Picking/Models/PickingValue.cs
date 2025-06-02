using System.Collections.Generic;
using Service.API.General;
using Service.Shared.Data;

namespace Service.API.Picking.Models;

public class PickingValue {
    [RecordsetReaderColumn]
    public int PickEntry { get; set; }

    [RecordsetReaderColumn]
    public int Quantity { get; set; }

    [RecordsetReaderColumn]
    public UnitType Unit { get; set; }

    [RecordsetReaderColumn]
    public int NumInBuy { get; set; }

    public List<PickingValueBin> BinLocations { get; } = new();
}

public class PickingValueBin {
    [RecordsetReaderColumn]
    public int PickEntry { get; set; }

    [RecordsetReaderColumn]
    public int BinEntry { get; set; }

    [RecordsetReaderColumn]
    public int Quantity { get; set; }
}