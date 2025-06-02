using System.Collections.Generic;
using Core.Models;

namespace Core.Exceptions;

public class WarehouseSelectionRequiredException(IEnumerable<Warehouse> availableWarehouses) : Exception("Multiple warehouses available. Please select one.") {
    public IEnumerable<Warehouse> AvailableWarehouses { get; } = availableWarehouses;
}