using System.Collections.Generic;
using Core.Models;

namespace Core.Exceptions;

public class WarehouseSelectionRequiredException : Exception
{
    public IEnumerable<ExternalValue> AvailableWarehouses { get; }

    public WarehouseSelectionRequiredException(IEnumerable<ExternalValue> availableWarehouses) 
        : base("Multiple warehouses available. Please select one.")
    {
        AvailableWarehouses = availableWarehouses;
    }
}