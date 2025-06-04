using Core.Enums;

namespace Core.DTOs;

public class PickListsRequest {
    public int? ID { get; set; }
    public DateTime? Date { get; set; }
    public ObjectStatus[]? Statuses { get; set; }
}