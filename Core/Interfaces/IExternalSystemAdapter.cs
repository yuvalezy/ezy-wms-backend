using Core.Models;

namespace Core.Interfaces;

public interface IExternalSystemAdapter {
    Task<ExternalValue?>             GetUserInfoAsync(string id);
    Task<IEnumerable<ExternalValue>> GetUsersAsync();
    Task<string?>                    GetCompanyNameAsync();
    Task<IEnumerable<ExternalValue>> GetWarehousesAsync(string[]? filter = null);
    Task<ExternalValue?>             GetWarehouseAsync(string     id);
}