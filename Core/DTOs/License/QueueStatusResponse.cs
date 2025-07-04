namespace Core.DTOs.License;

public class QueueStatusResponse {
    public int      PendingEventCount      { get; set; }
    public bool     CloudServiceAvailable { get; set; }
    public DateTime LastChecked            { get; set; }
}