using Adapters.Windows.SBO.Repositories;
using Core.Interfaces;
using Core.Models;
using Microsoft.VisualBasic;

namespace Adapters.Windows.SBO;

public class SboAdapter(SboEmployeeRepository employeeRepository, SboGeneralRepository generalRepository) : IExternalSystemAdapter {
    public async Task<ExternalValue?>             GetUserInfoAsync(string id)                 => await employeeRepository.GetByIdAsync(id);
    public async Task<IEnumerable<ExternalValue>> GetUsersAsync()                             => await employeeRepository.GetAllAsync();
    public async Task<string?>                    GetCompanyNameAsync()                       => await generalRepository.GetCompanyNameAsync();
    public async Task<IEnumerable<ExternalValue>> GetWarehousesAsync(string[]? filter = null) => await generalRepository.GetWarehousesAsync(filter);
    public async Task<ExternalValue?>             GetWarehouseAsync(string     id)            => (await generalRepository.GetWarehousesAsync([id])).FirstOrDefault();
}