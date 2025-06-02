using Adapters.Windows.SBO.Repositories;
using Core.Interfaces;
using Core.Models;
using Microsoft.VisualBasic;

namespace Adapters.Windows.SBO;

public class SboAdapter(SboEmployeeRepository employeeRepository, SboGeneralRepository generalRepository) : IExternalSystemAdapter {
    public async Task<ExternalUserResponse?>             GetUserInfoAsync(string id) => await employeeRepository.GetByIdAsync(id);
    public async Task<IEnumerable<ExternalUserResponse>> GetUsersAsync()             => await employeeRepository.GetAllAsync();
    public async Task<string?>                           GetCompanyNameAsync()       => await generalRepository.GetCompanyNameAsync();
}