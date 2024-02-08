using System;

namespace Service.API.Picking.Models;

public class PickingParameters {
    public int?         ID      { get; set; }
    public DateTime?    Date    { get; set; }
    internal string       WhsCode { get; set; }
    public PickStatus[] Statues { get; set; } = [PickStatus.Released];
}