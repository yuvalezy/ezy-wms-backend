using Adapters.Windows.SBO.Repositories;
using Core.Interfaces;
using Core.Models;

namespace Adapters.Windows.SBO;

public class SapBusinessOneAdapter(SapEmployeeRepository employeeRepository) : IExternalSystemAdapter {
    public async Task<ExternalUserResponse?> GetUserInfoAsync(string id) => await employeeRepository.GetByIdAsync(id);
    public async Task<IEnumerable<ExternalUserResponse>> GetUsersAsync() => await employeeRepository.GetAllAsync();
}