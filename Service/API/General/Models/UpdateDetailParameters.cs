using System.Collections.Generic;

namespace Service.API.General.Models;

public class UpdateDetailParameters {
    public int                  ID              { get; set; }
    public List<int>            RemoveRows      { get; set; }
    public Dictionary<int, int> QuantityChanges { get; set; }
}