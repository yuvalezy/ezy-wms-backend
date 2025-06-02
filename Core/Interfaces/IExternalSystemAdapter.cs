using Core.Models;

namespace Core.Interfaces;

public interface IExternalSystemAdapter {
    Task<ExternalValue?>                GetUserInfoAsync(string id);
    Task<IEnumerable<ExternalValue>>    GetUsersAsync();
    Task<string?>                       GetCompanyNameAsync();
    Task<IEnumerable<Warehouse>>        GetWarehousesAsync(string[]? filter = null);
    Task<Warehouse?>                    GetWarehouseAsync(string     id);
    Task<(int itemCount, int binCount)> GetItemAndBinCount(string    warehouse);
    Task<IEnumerable<ExternalValue>>    GetVendorsAsync();
    Task<bool>                          ValidateVendorsAsync(string id);
    Task<BinLocation?>                  ScanBinLocationAsync(string bin);
}