using Core.Enums;

namespace Core.DTOs.PickList;

public class PickListsRequest {
    public int?            ID               { get; set; }
    public DateTime?       Date             { get; set; }
    public ObjectStatus[]? Statuses         { get; set; }
    public bool            DisplayCompleted { get; set; }
}