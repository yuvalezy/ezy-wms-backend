namespace Service.API.Picking.Models;

public enum PickStatus {
    Released = 'R',
    Picked   = 'P',
    Closed   = 'C'
}
public enum PickingStatus {
    Open       = 'O',
    Processing = 'P',
    Finished   = 'F',
    Error      = 'E'
}
