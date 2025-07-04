using Core.DTOs.Items;

namespace Core.Exceptions;

public class WarehouseSelectionRequiredException(IEnumerable<WarehouseResponse> availableWarehouses) : Exception("Multiple warehouses available. Please select one.") {
    public IEnumerable<WarehouseResponse> AvailableWarehouses { get; } = availableWarehouses;
}

public class DeviceRegistrationException(string error) : Exception("Device registration error.") {
    public string Error { get; } = error;
}