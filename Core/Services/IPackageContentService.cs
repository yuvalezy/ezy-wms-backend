using Core.DTOs.Package;
using Core.Entities;
using Core.Models;

namespace Core.Services;

public interface IPackageContentService {
    // Content Management
    Task<PackageContent> AddItemToPackageAsync(AddItemToPackageRequest request, string warehouse, Guid userId);
    Task<PackageContent>              RemoveItemFromPackageAsync(RemoveItemFromPackageRequest request, Guid        userId);
    Task<IEnumerable<PackageContent>> GetPackageContentsAsync(Guid                            packageId);
    Task<decimal>                     GetItemQuantityInPackageAsync(Guid                      packageId, string itemCode);
    
    // Transaction History
    Task<IEnumerable<PackageTransaction>> GetPackageTransactionHistoryAsync(Guid                  packageId);
    Task                                  LogPackageTransactionAsync(LogPackageTransactionRequest request);
}