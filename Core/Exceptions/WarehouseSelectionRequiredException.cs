using Core.DTOs.Items;

namespace Core.Exceptions;

public class WarehouseSelectionRequiredException(IEnumerable<WarehouseResponse> availableWarehouses) : Exception("Multiple warehouses available. Please select one.") {
    public IEnumerable<WarehouseResponse> AvailableWarehouses { get; } = availableWarehouses;
}