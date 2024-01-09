using Service.Shared.Data;

namespace Service.API.Picking.Models;

public class PickingValue {
    [RecordsetReaderColumn] public int PickEntry { get; set; }
    [RecordsetReaderColumn] public int Quantity  { get; set; }
}