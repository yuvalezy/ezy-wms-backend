using Service.API.Models;

namespace Service.API.GoodsReceipt.Models;

public class CreateParameters {
    public string Name { get; set; }
}

public class FilterParameters {
    public DocumentStatus[] Statuses { get; set; }
    public OrderBy?         OrderBy  { get; set; }
    public bool             Desc     { get; set; }
}

public enum OrderBy {
    ID,
    Name,
    Date
}