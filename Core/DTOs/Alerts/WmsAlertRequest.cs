namespace Core.DTOs.Alerts;

public class WmsAlertRequest {
    public bool? UnreadOnly { get; set; }
    public int? Limit { get; set; }
}

public class MarkAlertReadRequest {
    public Guid AlertId { get; set; }
}
